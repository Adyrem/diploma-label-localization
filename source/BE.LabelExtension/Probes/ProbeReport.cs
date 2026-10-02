#if DEBUG
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using BE.LabelExtension.Core.Diagnostics;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Collects the result of a probe and reports it as one message in the Output Window,
    /// so the user can copy it from there after a pass on the test environment.
    /// </summary>
    internal sealed class ProbeReport
    {
        private readonly StringBuilder text = new();

        public ProbeReport(string title)
        {
            this.text.Append("[Probe] ").Append(title);
        }

        public void Line(string line) => this.text.Append("\r\n").Append(line);

        public void Send(IMessageSink sink) => sink.Report(MessageSeverity.Message, this.text.ToString());

        public static string Shorten(object? value, int max = 80)
        {
            string text = Convert.ToString(value) ?? string.Empty;
            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length <= max ? text : text.Substring(0, max) + "...";
        }

        public static string TypeName(object? value) => value?.GetType().FullName ?? "null";

        /// <summary>
        /// Lists the interfaces of an object and its public methods whose name hints at
        /// selecting or navigating. Shows how a designer could be steered to a node.
        /// </summary>
        public void DescribeObject(string label, object? value)
        {
            if (value == null)
            {
                this.Line($"{label}: null");
                return;
            }

            Type type = value.GetType();
            string interfaces = Safe(() => string.Join(", ", type.GetInterfaces().Select(i => i.Name).OrderBy(n => n).Take(25)));
            string methods = Safe(() => string.Join(", ", type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .Where(n => n.IndexOf("Select", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("Navigate", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("GoTo", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("Show", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("Find", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("Focus", StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct()
                .OrderBy(n => n)
                .Take(30)));

            this.Line($"{label}: {type.FullName} ({type.Assembly.GetName().Name})");
            this.Line($"  interfaces: {interfaces}");
            this.Line($"  methods for selecting or navigating: {methods}");
        }

        public static string Safe(Func<string> get)
        {
            try
            {
                return get();
            }
            catch (Exception exception)
            {
                return $"<{exception.GetType().Name}: {Shorten(exception.Message, 60)}>";
            }
        }
    }
}
#endif
