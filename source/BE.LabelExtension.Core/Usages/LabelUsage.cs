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
        public LabelUsage(ModelInfo model, string path, int line, int column, string lineText)
        {
            this.Model = model;
            this.Path = path;
            this.Line = line;
            this.Column = column;
            this.LineText = lineText;
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
    }
}
