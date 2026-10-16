using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Display;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Elements;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// The translations of the node selected in a designer (KA05): every label property with a
    /// text, such as Label and Help Text, with its translations in the loaded languages.
    /// </summary>
    /// <remarks>
    /// The area follows the selection of the designer and disappears when another document
    /// becomes active. It is redrawn when labels load or change, so an edit in the details shows
    /// there at once. What the Properties window shows is the state of the designer, also when
    /// it is not yet saved.
    /// </remarks>
    internal sealed partial class LabelWindowController
    {
        private DesignerNode? designerNode;

        private void OnDesignerNodeChanged(object? sender, DesignerNode? node)
        {
            this.designerNode = node;
            this.ShowDesignerNode();
        }

        private void ShowDesignerNode()
        {
            DesignerNode? node = this.designerNode;
            if (node == null)
            {
                this.Data.HasDesignerNode = false;
                this.Data.DesignerRows = new ObservableList<DesignerRow>();
                return;
            }

            if (this.languages.Count == 0)
            {
                this.UpdateLanguages();
            }

            var rows = new ObservableList<DesignerRow>(node.Properties.Select(p => this.CreateDesignerRow(node, p)));
            string element = Path.GetFileNameWithoutExtension(node.DocumentPath);
            string title = string.Equals(element, node.Name, System.StringComparison.OrdinalIgnoreCase) ? node.Name : $"{node.Name} in {element}";
            this.Data.DesignerTitle = rows.Count == 0
                ? $"Selected in the designer: {title}, without a label property that has a text"
                : $"Selected in the designer: {title}";
            this.Data.DesignerRows = rows;
            this.Data.HasDesignerNode = true;
        }

        // After the extension wrote a property, the node shown holds the new value; the
        // designer itself may only select the node again after reloading.
        private void UpdateDesignerProperty(string documentPath, string node, string property, string value)
        {
            DesignerNode? shown = this.designerNode;
            if (shown == null || shown.Name != node || !string.Equals(shown.DocumentPath, documentPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            this.designerNode = new DesignerNode(shown.DocumentPath, shown.Name, shown.Properties.Select(p => p.Name == property ? new DesignerProperty(p.Name, value) : p).ToList());
            this.ShowDesignerNode();
        }

        // A label shows its ID and translations and opens in the details; a hardcoded text can
        // be extracted; an unknown ID can be searched.
        private DesignerRow CreateDesignerRow(DesignerNode node, DesignerProperty property)
        {
            string value = property.Value.Trim();
            Label? label = this.store.IsLoaded ? this.store.Find(value) : null;
            if (label != null && !label.IsDeleted)
            {
                string translations = LabelSummary.InlineText(new[] { LabelSummary.Create(label.Id, label, this.languages) }, showCount: false);
                return new DesignerRow(property.Name, $"{label.Id.FullId}  {translations}", "Open", new AsyncCommand((_, _) =>
                {
                    string? problem = this.OpenLabel(label.Id);
                    if (problem != null)
                    {
                        this.Data.StatusText = problem;
                    }

                    return Task.CompletedTask;
                }));
            }

            if (value.StartsWith("@", System.StringComparison.Ordinal) || LabelId.TryParse(value, out _))
            {
                string state = this.store.IsLoaded ? "not a known label" : "labels are being loaded";
                return new DesignerRow(property.Name, $"{value}  {state}", "Search", new AsyncCommand((_, _) =>
                {
                    this.SearchFor(value);
                    return Task.CompletedTask;
                }));
            }

            return new DesignerRow(property.Name, $"hardcoded text: {value}", "Extract", new AsyncCommand((_, cancellationToken) => this.RunAsync(
                "Extract to label",
                token => this.ExtractFromDesignerAsync(node, property, token),
                cancellationToken)));
        }
    }
}
