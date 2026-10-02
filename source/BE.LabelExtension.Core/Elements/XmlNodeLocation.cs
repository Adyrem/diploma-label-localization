namespace BE.LabelExtension.Core.Elements
{
    /// <summary>
    /// Place of an XML element in a file.
    /// </summary>
    public sealed class XmlNodeLocation
    {
        /// <summary>Creates a location.</summary>
        /// <param name="path">Names of the element and its ancestors, separated by slashes.</param>
        /// <param name="line">Line of the start tag, starting at 1.</param>
        /// <param name="column">Column of the start tag, starting at 1.</param>
        public XmlNodeLocation(string path, int line, int column)
        {
            this.Path = path;
            this.Line = line;
            this.Column = column;
        }

        /// <summary>Names of the element and its ancestors, for example <c>AxTable/Fields/AxTableField/Name</c>.</summary>
        public string Path { get; }

        /// <summary>Line of the start tag, starting at 1.</summary>
        public int Line { get; }

        /// <summary>Column of the start tag, starting at 1.</summary>
        public int Column { get; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Path} (line {this.Line}, column {this.Column})";
    }
}
