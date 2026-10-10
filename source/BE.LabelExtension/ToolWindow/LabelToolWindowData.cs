using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Data context of the tool window. Remote UI binds the XAML in Visual Studio to the
    /// members marked with <see cref="DataMemberAttribute"/>; the user's input arrives as
    /// changed properties.
    /// </summary>
    /// <remarks>
    /// The commands are set once by <see cref="LabelWindowController"/> before the window shows.
    /// </remarks>
    [DataContract]
    internal sealed class LabelToolWindowData : NotifyPropertyChangedObject
    {
        private string statusText = "Labels are not loaded yet.";
        private string searchText = string.Empty;
        private int selectedModeIndex;
        private IReadOnlyList<string> columns = new List<string>();
        private ObservableList<HitRow> hits = new();
        private int selectedHitIndex = -1;
        private LabelDetailData detail = LabelDetailData.Empty;
        private string pendingText = string.Empty;
        private IReadOnlyList<string> labelFiles = new List<string>();
        private int selectedLabelFileIndex = -1;
        private string copyText = "Copy to";
        private string moveText = "Move to";

        /// <summary>Creates the data context.</summary>
        /// <param name="searchModes">Names of the search modes, as in the existing tool.</param>
        /// <param name="selectedModeIndex">The mode selected at first.</param>
        public LabelToolWindowData(IReadOnlyList<string> searchModes, int selectedModeIndex)
        {
            this.SearchModes = searchModes;
            this.selectedModeIndex = selectedModeIndex;
        }

        /// <summary>Names of the search modes.</summary>
        [DataMember]
        public IReadOnlyList<string> SearchModes { get; }

        /// <summary>Index of the selected search mode, set by the user.</summary>
        [DataMember]
        public int SelectedModeIndex
        {
            get => this.selectedModeIndex;
            set => this.SetProperty(ref this.selectedModeIndex, value);
        }

        /// <summary>The search term, set by the user while typing.</summary>
        [DataMember]
        public string SearchText
        {
            get => this.searchText;
            set => this.SetProperty(ref this.searchText, value ?? string.Empty);
        }

        /// <summary>Searches at once.</summary>
        [DataMember]
        public AsyncCommand SearchCommand { get; set; } = null!;

        /// <summary>Saves all changes; disabled while there are none.</summary>
        [DataMember]
        public AsyncCommand SaveCommand { get; set; } = null!;

        /// <summary>Saves all changes and inserts the ID of the label in the editor (FA14).</summary>
        [DataMember]
        public AsyncCommand SaveAndInsertCommand { get; set; } = null!;

        /// <summary>Inserts the ID of the label at the cursor of the active editor (FA14).</summary>
        [DataMember]
        public AsyncCommand InsertCommand { get; set; } = null!;

        /// <summary>Starts a new label in the selected label file (FA02).</summary>
        [DataMember]
        public AsyncCommand NewCommand { get; set; } = null!;

        /// <summary>Opens the options page (FA11).</summary>
        [DataMember]
        public AsyncCommand SettingsCommand { get; set; } = null!;

        /// <summary>Creates the new label and gives it its ID.</summary>
        [DataMember]
        public AsyncCommand CreateCommand { get; set; } = null!;

        /// <summary>Discards the new label.</summary>
        [DataMember]
        public AsyncCommand CancelNewCommand { get; set; } = null!;

        /// <summary>Deletes the label once saved (FA03).</summary>
        [DataMember]
        public AsyncCommand DeleteCommand { get; set; } = null!;

        /// <summary>Copies the label into the selected label file (FA03).</summary>
        [DataMember]
        public AsyncCommand CopyCommand { get; set; } = null!;

        /// <summary>Moves the label into the selected label file (FA03).</summary>
        [DataMember]
        public AsyncCommand MoveCommand { get; set; } = null!;

        /// <summary>Replaces the uses of the label by another label (FA13).</summary>
        [DataMember]
        public AsyncCommand ReplaceCommand { get; set; } = null!;

        /// <summary>How many labels are not saved, next to the buttons.</summary>
        [DataMember]
        public string PendingText
        {
            get => this.pendingText;
            set => this.SetProperty(ref this.pendingText, value);
        }

        /// <summary>The label files new labels can be created in, copied and moved to.</summary>
        [DataMember]
        public IReadOnlyList<string> LabelFiles
        {
            get => this.labelFiles;
            set => this.SetProperty(ref this.labelFiles, value);
        }

        /// <summary>Index of the selected label file, -1 for none, set by the user.</summary>
        [DataMember]
        public int SelectedLabelFileIndex
        {
            get => this.selectedLabelFileIndex;
            set => this.SetProperty(ref this.selectedLabelFileIndex, value);
        }

        /// <summary>Caption of the button Copy to, with the selected label file.</summary>
        [DataMember]
        public string CopyText
        {
            get => this.copyText;
            set => this.SetProperty(ref this.copyText, value);
        }

        /// <summary>Caption of the button Move to, with the selected label file.</summary>
        [DataMember]
        public string MoveText
        {
            get => this.moveText;
            set => this.SetProperty(ref this.moveText, value);
        }

        /// <summary>Headers of the language columns of the hit list, one per loaded language.</summary>
        [DataMember]
        public IReadOnlyList<string> Columns
        {
            get => this.columns;
            set => this.SetProperty(ref this.columns, value);
        }

        /// <summary>The hits of the last search, the most relevant first.</summary>
        [DataMember]
        public ObservableList<HitRow> Hits
        {
            get => this.hits;
            set => this.SetProperty(ref this.hits, value);
        }

        /// <summary>Index of the selected hit, -1 for none, set by the user.</summary>
        [DataMember]
        public int SelectedHitIndex
        {
            get => this.selectedHitIndex;
            set => this.SetProperty(ref this.selectedHitIndex, value);
        }

        /// <summary>The selected label with its translations, or a new label.</summary>
        [DataMember]
        public LabelDetailData Detail
        {
            get => this.detail;
            set => this.SetProperty(ref this.detail, value);
        }

        /// <summary>Result of the last search or action, or the state of loading.</summary>
        [DataMember]
        public string StatusText
        {
            get => this.statusText;
            set => this.SetProperty(ref this.statusText, value);
        }
    }

    /// <summary>One label in the hit list.</summary>
    [DataContract]
    internal sealed class HitRow : NotifyPropertyChangedObject
    {
        private bool isModified;
        private bool isDeleted;

        /// <summary>Creates a row.</summary>
        /// <param name="id">The complete label ID.</param>
        /// <param name="isReadOnly">Whether the label cannot be changed.</param>
        /// <param name="isModified">Whether the label has changes not yet saved.</param>
        /// <param name="isDeleted">Whether the label is deleted, not yet saved.</param>
        /// <param name="cells">The texts in the loaded languages.</param>
        public HitRow(string id, bool isReadOnly, bool isModified, bool isDeleted, ObservableList<HitCell> cells)
        {
            this.Id = id;
            this.IsReadOnly = isReadOnly;
            this.isModified = isModified;
            this.isDeleted = isDeleted;
            this.Cells = cells;
        }

        /// <summary>The complete label ID.</summary>
        [DataMember]
        public string Id { get; }

        /// <summary>Whether the label cannot be changed; shown as a lock.</summary>
        [DataMember]
        public bool IsReadOnly { get; }

        /// <summary>Whether the label has changes not yet saved; shown in bold.</summary>
        [DataMember]
        public bool IsModified
        {
            get => this.isModified;
            set => this.SetProperty(ref this.isModified, value);
        }

        /// <summary>Whether the label is deleted, not yet saved; shown struck through.</summary>
        [DataMember]
        public bool IsDeleted
        {
            get => this.isDeleted;
            set => this.SetProperty(ref this.isDeleted, value);
        }

        /// <summary>The texts in the loaded languages, in the order of the columns.</summary>
        [DataMember]
        public ObservableList<HitCell> Cells { get; }
    }

    /// <summary>The text of a label in one language in the hit list.</summary>
    [DataContract]
    internal sealed class HitCell : NotifyPropertyChangedObject
    {
        private string text;
        private bool isMissing;

        /// <summary>Creates a cell.</summary>
        /// <param name="text">The text, <c>null</c> if the translation is missing.</param>
        public HitCell(string? text)
        {
            this.text = text ?? "missing";
            this.isMissing = text == null;
        }

        /// <summary>The text, or "missing".</summary>
        [DataMember]
        public string Text
        {
            get => this.text;
            private set => this.SetProperty(ref this.text, value);
        }

        /// <summary>Whether the translation is missing; shown in italics.</summary>
        [DataMember]
        public bool IsMissing
        {
            get => this.isMissing;
            private set => this.SetProperty(ref this.isMissing, value);
        }

        /// <summary>Shows a changed text.</summary>
        /// <param name="text">The text, <c>null</c> if the translation is missing.</param>
        public void Update(string? text)
        {
            this.Text = text ?? "missing";
            this.IsMissing = text == null;
        }
    }
}
