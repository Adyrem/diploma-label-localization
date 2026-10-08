#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Collects the result of a probe and reports it as one message in the Output Window,
    /// so the user can copy it from there after a pass on the test environment.
    /// </summary>
    /// <remarks>
    /// The report leaves the test environment. Paths, names and values therefore appear only
    /// as their shape: generic folder names, versions, language codes, element folders and
    /// file extensions stay, every other name becomes &lt;Name&gt;. Type names of Visual
    /// Studio and the Developer Tools stay as they are.
    /// </remarks>
    internal sealed class ProbeReport
    {
        private static readonly HashSet<string> GenericSegments = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft", "Dynamics365", "PackagesLocalDirectory", "Metadata", "XppSource", "XPPConfig",
            "AOSService", "bin", "Resources", "Descriptor", "AxLabelFile", "LabelResources",
            "AppData", "Local", "Roaming", "Temp",
        };

        private static readonly HashSet<string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "xml", "xpp", "label", "txt", "dll", "resources", "rnrproj", "axproj", "sln", "json",
        };

        private static readonly HashSet<string> Cultures = new(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name).Where(n => n.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        private static readonly Regex ElementFolder = new("^Ax[A-Z][A-Za-z]+$");
        private static readonly Regex ElementFile = new("^(Ax[A-Z][A-Za-z]+)_.+$");
        private static readonly Regex Version = new(@"^\d+(\.\d+)+$");
        private static readonly Regex LanguageCode = new("^[a-z]{2,3}(-[A-Za-z]{2,4})?$");

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

        /// <summary>Shape of a path or moniker, for example <c>C:\&lt;Name&gt;\&lt;Name&gt;\Metadata\&lt;Name&gt;\&lt;Name&gt;\AxClass\&lt;Name&gt;.xml</c>.</summary>
        public static string PathShape(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "<none>";
            }

            string rest = path!.Trim();
            string? head = null;
            foreach (var (token, folder) in new[]
            {
                ("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                ("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
                ("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
            })
            {
                if (folder.Length > 0 && rest.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                {
                    head = token + "\\";
                    rest = rest.Substring(folder.Length);
                    break;
                }
            }

            if (head == null)
            {
                int scheme = rest.IndexOf("://", StringComparison.Ordinal);
                if (scheme > 0 && rest.Substring(0, scheme).All(char.IsLetter))
                {
                    head = rest.Substring(0, scheme + 3);
                    rest = rest.Substring(scheme + 3);
                }
                else if (rest.Length >= 2 && char.IsLetter(rest[0]) && rest[1] == ':')
                {
                    head = char.ToUpperInvariant(rest[0]) + ":\\";
                    rest = rest.Substring(2);
                }
                else if (rest.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    head = @"\\<Server>\";
                    rest = rest.TrimStart('\\');
                    int slash = rest.IndexOf('\\');
                    rest = slash < 0 ? string.Empty : rest.Substring(slash);
                }
                else
                {
                    return $"<not a path, {rest.Length} characters>";
                }
            }

            string[] parts = rest.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            return head + string.Join("\\", parts.Select((part, i) => i == parts.Length - 1 ? FileShape(part) : SegmentShape(part)));
        }

        /// <summary>Shape of a file name, for example <c>&lt;Name&gt;.de.label.txt</c> or <c>AxClass_&lt;Name&gt;.xpp</c>.</summary>
        public static string FileShape(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "<none>";
            }

            string[] parts = name!.Trim().Split('.');
            Match element = ElementFile.Match(parts[0]);
            string first = element.Success ? element.Groups[1].Value + "_<Name>" : SegmentShape(parts[0]);
            return string.Join(".", new[] { first }.Concat(parts.Skip(1).Select(p => KnownExtensions.Contains(p) || IsLanguage(p) ? p : "<Name>")));
        }

        /// <summary>
        /// Kind of a property value: empty, yes or no, a number, a label ID of either form, or
        /// a text with its length. Names and texts do not appear.
        /// </summary>
        public static string ValueShape(string? value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return "<empty>";
            }

            if (new[] { "Yes", "No", "True", "False" }.Contains(trimmed, StringComparer.OrdinalIgnoreCase)
                || decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                return trimmed;
            }

            if (LabelId.TryParse(trimmed, out LabelId id))
            {
                return id.IsLegacy ? "<label ID, old form>" : "<label ID @<file>:<label>>";
            }

            return $"<text, {trimmed.Length} characters>";
        }

        private static string SegmentShape(string segment)
        {
            if (GenericSegments.Contains(segment) || ElementFolder.IsMatch(segment) || IsLanguage(segment))
            {
                return segment;
            }

            return Version.IsMatch(segment) ? "<Version>" : "<Name>";
        }

        private static bool IsLanguage(string segment) => LanguageCode.IsMatch(segment) && Cultures.Contains(segment);
    }
}
#endif
