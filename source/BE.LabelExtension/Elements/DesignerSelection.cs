using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Elements;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Elements
{
    /// <summary>
    /// The node selected in a designer of the Developer Tools, for the extraction from the
    /// Properties window (FA09). The designer passes its selection to the selection tracking of
    /// Visual Studio as an object with the properties of the Properties window, among them Name
    /// and Label (D1).
    /// </summary>
    /// <remarks>
    /// The button Extract stands in the tool window, and clicking it may hand the selection to
    /// the tool window. The last node a designer selected is therefore kept, by a weak
    /// reference, and used when the current selection holds none.
    /// </remarks>
    internal sealed class DesignerSelection : IVsSelectionEvents
    {
        // ISelectionContainer.GetObjects: the selected objects, not all selectable ones.
        private const uint GetObjectsSelected = 2;

        private static DesignerSelection? current;

        private readonly IVsMonitorSelection monitor;
        private uint cookie;
        private WeakReference<object>? lastNode;
        private string? lastDocument;

        private DesignerSelection(IVsMonitorSelection monitor)
        {
            this.monitor = monitor;
        }

        /// <summary>Receives an error of the tracking, which runs outside every command.</summary>
        public static Action<Exception>? ReportError { get; set; }

        /// <summary>The tracking, once started.</summary>
        public static DesignerSelection? Current => Volatile.Read(ref current);

        /// <summary>Starts following the selection. Called once by the package.</summary>
        /// <param name="provider">Provides the selection service.</param>
        /// <param name="cancellationToken">Cancels the start.</param>
        /// <returns>A task that completes when the tracking runs.</returns>
        public static async Task StartAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider provider, CancellationToken cancellationToken)
        {
            IVsMonitorSelection monitor = await provider.GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var selection = new DesignerSelection(monitor);
            ErrorHandler.ThrowOnFailure(monitor.AdviseSelectionEvents(selection, out selection.cookie));
            Volatile.Write(ref current, selection);
        }

        /// <summary>Stops following the selection, when the package goes.</summary>
        public static void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DesignerSelection? selection = Interlocked.Exchange(ref current, null);
            if (selection != null && selection.cookie != 0)
            {
                selection.monitor.UnadviseSelectionEvents(selection.cookie);
            }
        }

        /// <summary>The path of the active document, such as the XML file a designer shows, or the <c>.xpp</c> of the X++ editor.</summary>
        /// <returns>The path, or <c>null</c> without an active document.</returns>
        public string? ActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.monitor.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out object value) == VSConstants.S_OK
                && value is IVsWindowFrame frame
                && frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object moniker) == VSConstants.S_OK)
            {
                return moniker as string;
            }

            return null;
        }

        /// <summary>
        /// The node selected in the designer of the active document: the current selection if it
        /// is one, otherwise the last node the designer of this document selected.
        /// </summary>
        /// <returns>The node, or <c>null</c> if the active document is no designer with a selected node.</returns>
        public DesignerNode? Read()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string? document = this.ActiveDocument();
            if (document == null || !document.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            object? node = this.CurrentNode();
            if (node == null
                && string.Equals(this.lastDocument, document, StringComparison.OrdinalIgnoreCase)
                && this.lastNode != null
                && this.lastNode.TryGetTarget(out object? last))
            {
                node = last;
            }

            return node == null ? null : Describe(node, document);
        }

        /// <inheritdoc />
        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                object? node = SelectedNode(pSCNew);
                string? document = node == null ? null : this.ActiveDocument();
                if (node != null && document != null && document.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    this.lastNode = new WeakReference<object>(node);
                    this.lastDocument = document;
                }
            }
            catch (Exception exception)
            {
                ReportError?.Invoke(exception);
            }

            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew) => VSConstants.S_OK;

        /// <inheritdoc />
        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) => VSConstants.S_OK;

        // A single selected object with a Name in the Properties window, as a designer node.
        private static object? SelectedNode(ISelectionContainer? container)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (container == null || container.CountObjects(GetObjectsSelected, out uint count) != VSConstants.S_OK || count != 1)
            {
                return null;
            }

            var objects = new object[1];
            if (container.GetObjects(GetObjectsSelected, 1, objects) != VSConstants.S_OK || objects[0] == null)
            {
                return null;
            }

            return TypeDescriptor.GetProperties(objects[0], new Attribute[] { BrowsableAttribute.Yes }).Find("Name", ignoreCase: false) != null ? objects[0] : null;
        }

        // Label properties with a text, read as the Properties window shows them.
        private static DesignerNode? Describe(object node, string document)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(node, new Attribute[] { BrowsableAttribute.Yes });
            string? name = Convert.ToString(properties.Find("Name", ignoreCase: false)?.GetValue(node));
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var values = new List<DesignerProperty>();
            foreach (string property in ElementPropertyEditor.LabelProperties)
            {
                string? value = Convert.ToString(properties.Find(property, ignoreCase: false)?.GetValue(node));
                if (!string.IsNullOrEmpty(value))
                {
                    values.Add(new DesignerProperty(property, value!));
                }
            }

            return new DesignerNode(document, name!, values);
        }

        private object? CurrentNode()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.monitor.GetCurrentSelection(out IntPtr hierarchy, out _, out _, out IntPtr containerPointer) != VSConstants.S_OK)
            {
                return null;
            }

            try
            {
                return containerPointer == IntPtr.Zero ? null : SelectedNode(Marshal.GetObjectForIUnknown(containerPointer) as ISelectionContainer);
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
        }
    }

    /// <summary>A node selected in a designer, with its label properties that hold a text.</summary>
    internal sealed class DesignerNode
    {
        /// <summary>Creates the node.</summary>
        /// <param name="documentPath">The XML file of the element the designer shows.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="properties">Its label properties with a text, in the order of <see cref="ElementPropertyEditor.LabelProperties"/>.</param>
        public DesignerNode(string documentPath, string name, IReadOnlyList<DesignerProperty> properties)
        {
            this.DocumentPath = documentPath;
            this.Name = name;
            this.Properties = properties;
        }

        /// <summary>The XML file of the element the designer shows.</summary>
        public string DocumentPath { get; }

        /// <summary>The name of the node.</summary>
        public string Name { get; }

        /// <summary>Its label properties with a text.</summary>
        public IReadOnlyList<DesignerProperty> Properties { get; }
    }

    /// <summary>A label property of a designer node and its text.</summary>
    internal sealed class DesignerProperty
    {
        /// <summary>Creates the property.</summary>
        /// <param name="name">The property, such as Label.</param>
        /// <param name="value">Its text as the Properties window shows it.</param>
        public DesignerProperty(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>The property, such as Label.</summary>
        public string Name { get; }

        /// <summary>Its text as the Properties window shows it.</summary>
        public string Value { get; }
    }
}
