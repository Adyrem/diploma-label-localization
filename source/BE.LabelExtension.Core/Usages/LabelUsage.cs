using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Usages
{
    /// <summary>A use of a label ID in the XML file of an element (FA04).</summary>
    public sealed class LabelUsage
    {
        /// <summary>Creates a use.</summary>
        /// <param name="model">The model of the element.</param>
        /// <param name="path">The XML file of the element.</param>
        /// <param name="line">The line, starting at 1.</param>
        /// <param name="column">The column of the ID, starting at 1.</param>
        /// <param name="lineText">The line, without leading and trailing spaces.</param>
        /// <param name="codeOccurrence">Which use of the ID in the code of the element it is, starting at 1; 0 for a use outside the code.</param>
        public LabelUsage(ModelInfo model, string path, int line, int column, string lineText, int codeOccurrence = 0)
        {
            this.Model = model;
            this.Path = path;
            this.Line = line;
            this.Column = column;
            this.LineText = lineText;
            this.CodeOccurrence = codeOccurrence;
        }

        /// <summary>The model of the element.</summary>
        public ModelInfo Model { get; }

        /// <summary>The XML file of the element.</summary>
        public string Path { get; }

        /// <summary>The line, starting at 1.</summary>
        public int Line { get; }

        /// <summary>The column of the ID, starting at 1.</summary>
        public int Column { get; }

        /// <summary>The line, without leading and trailing spaces.</summary>
        public string LineText { get; }

        /// <summary>
        /// Which use of the ID in the code of the element it is, starting at 1, counted over the
        /// declaration and the methods in the order of the XML file; 0 for a use in a property.
        /// The X++ editor shows the same code, so the same use there is found by this number.
        /// </summary>
        public int CodeOccurrence { get; }

        /// <summary>Whether the use stands in the code of the element rather than in a property.</summary>
        public bool IsInCode => this.CodeOccurrence > 0;
    }
}
