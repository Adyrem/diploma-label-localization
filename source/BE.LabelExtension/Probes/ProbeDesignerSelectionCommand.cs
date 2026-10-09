#if DEBUG
// The probes run on the main thread from start to end. The analyzer does not follow that
// into the lambdas passed to ProbeReport.Safe, which are called synchronously.
#pragma warning disable VSTHRD010

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Elements;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Probe for D1: reads the node selected in the designer through the selection
    /// tracking of Visual Studio and looks for it in the element's XML file, by the name
    /// of the node and by the values of its label properties. Basis for the extraction
    /// from the Properties window (FA09). Reads only and never sets a value.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ProbeDesignerSelectionCommand : Command
    {
        private const string Title = "Probe: Find selected designer node in XML";

        // ISelectionContainer.GetObjects: the selected objects, not all selectable ones.
        private const uint GetObjectsSelected = 2;

        private static readonly string[] LabelProperties = { "Label", "HelpText", "Caption", "DeveloperDocumentation" };

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;

        public ProbeDesignerSelectionCommand(ErrorBoundary errorBoundary, IMessageSink sink)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeDesignerSelectionCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.SelectObject, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var report = new ProbeReport(Title);
            IVsMonitorSelection monitor = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            string? moniker = ActiveDocumentMoniker(monitor);
            report.Line($"active document: {ProbeReport.PathShape(moniker)}");

            List<object> selected = SelectedObjects(monitor);
            report.Line($"selected objects: {selected.Count}");

            var nodes = new List<(string Name, Dictionary<string, string> LabelValues)>();
            foreach (object item in selected.Take(3))
            {
                nodes.Add(Describe(item, report));
            }

            if (nodes.Count == 0)
            {
                report.Send(this.sink);
                return;
            }

            // The designer's document is probably the element's XML file; if not, ask.
            string? xmlPath = moniker;
            if (xmlPath == null || !xmlPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || !File.Exists(xmlPath))
            {
                xmlPath = await this.Extensibility.Shell().ShowPromptAsync(
                    "The active document is not an XML file. Full path of the element's XML file:",
                    new InputPromptOptions { DefaultText = moniker ?? string.Empty },
                    cancellationToken);
                xmlPath = xmlPath?.Trim().Trim('"');
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }

            if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
            {
                report.Line($"XML file not found: {ProbeReport.PathShape(xmlPath)}");
                report.Send(this.sink);
                return;
            }

            report.Line($"XML file: {ProbeReport.PathShape(xmlPath)}");
            string xml = File.ReadAllText(xmlPath);
            foreach (var (name, labelValues) in nodes)
            {
                Locate(xml, "Name", name, report);
                foreach (KeyValuePair<string, string> property in labelValues)
                {
                    Locate(xml, property.Key, property.Value, report);
                }
            }

            report.Send(this.sink);
        }

        private static string? ActiveDocumentMoniker(IVsMonitorSelection monitor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (monitor.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out object value) == VSConstants.S_OK
                && value is IVsWindowFrame frame
                && frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object moniker) == VSConstants.S_OK)
            {
                return moniker as string;
            }

            return null;
        }

        private static List<object> SelectedObjects(IVsMonitorSelection monitor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var objects = new List<object>();
            ErrorHandler.ThrowOnFailure(monitor.GetCurrentSelection(out IntPtr hierarchy, out _, out _, out IntPtr containerPointer));
            try
            {
                if (containerPointer != IntPtr.Zero
                    && Marshal.GetObjectForIUnknown(containerPointer) is ISelectionContainer container
                    && container.CountObjects(GetObjectsSelected, out uint count) == VSConstants.S_OK
                    && count > 0)
                {
                    var buffer = new object[count];
                    ErrorHandler.ThrowOnFailure(container.GetObjects(GetObjectsSelected, count, buffer));
                    objects.AddRange(buffer.Where(o => o != null));
                }
            }
            finally
            {
                if (hierarchy != IntPtr.Zero)
                {
                    Marshal.Release(hierarchy);
                }

                if (containerPointer != IntPtr.Zero)
                {
                    Marshal.Release(containerPointer);
                }
            }

            return objects;
        }

        /// <summary>
        /// Reports the selected object with all properties the Properties window shows,
        /// and returns the node name and the values of its label properties.
        /// </summary>
        private static (string Name, Dictionary<string, string> LabelValues) Describe(object item, ProbeReport report)
        {
            // For a designer node the class name is the name of the node (D1), so it appears
            // only when it is a type name.
            string className = ProbeReport.Safe(() => TypeDescriptor.GetClassName(item) ?? string.Empty);
            string classShape = className == item.GetType().FullName ? className : ProbeReport.ValueShape(className);
            string componentName = ProbeReport.Safe(() => TypeDescriptor.GetComponentName(item) ?? string.Empty);
            report.Line($"object {ProbeReport.TypeName(item)}, class name {classShape}, component {(componentName.Length == 0 ? "without name" : "with a name")}");

            // The Properties window filters with BrowsableAttribute.Yes; without the filter
            // a designer node shows only ModelElement (findings log, section "Nachtrag: das
            // Objekt hinter ModelElement").
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(item, new Attribute[] { BrowsableAttribute.Yes });
            report.Line($"  browsable properties: {properties.Count}");

            string name = className;
            var labelValues = new Dictionary<string, string>();
            foreach (PropertyDescriptor property in properties)
            {
                string value = ProbeReport.Safe(() => Convert.ToString(property.GetValue(item)) ?? string.Empty);
                report.Line($"    {property.Name} = {ProbeReport.ValueShape(value)}{(property.IsReadOnly ? " (read-only)" : string.Empty)}");

                if (property.Name == "Name" && !string.IsNullOrEmpty(value))
                {
                    name = value;
                }
                else if (LabelProperties.Contains(property.Name) && !string.IsNullOrEmpty(value))
                {
                    labelValues[property.Name] = value;
                }
            }

            PropertyDescriptor? modelElement = TypeDescriptor.GetProperties(item).Find("ModelElement", false);
            report.Line($"  ModelElement: {ProbeReport.Safe(() => ProbeReport.TypeName(modelElement?.GetValue(item)))}");

            return (name, labelValues);
        }

        private static void Locate(string xml, string elementName, string value, ProbeReport report)
        {
            try
            {
                IReadOnlyList<XmlNodeLocation> found = ElementXmlLocator.FindElements(new StringReader(xml), elementName, value);
                string verdict = found.Count == 1 ? "unique" : found.Count == 0 ? "not found" : "NOT unique";
                report.Line($"  <{elementName}>{ProbeReport.ValueShape(value)}</{elementName}>: {found.Count} found, {verdict}");
                foreach (XmlNodeLocation location in found.Take(10))
                {
                    report.Line($"    {location}");
                }
            }
            catch (Exception exception)
            {
                report.Line($"  <{elementName}>: XML could not be read: {exception.GetType().Name}: {ProbeReport.Shorten(exception.Message, 120)}");
            }
        }
    }
}
#endif
