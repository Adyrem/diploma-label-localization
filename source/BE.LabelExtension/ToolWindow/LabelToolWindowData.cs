using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Data context of the tool window. Remote UI binds the XAML in Visual Studio to the
    /// members marked with <see cref="DataMemberAttribute"/>.
    /// </summary>
    [DataContract]
    internal sealed class LabelToolWindowData : NotifyPropertyChangedObject
    {
        private string statusText = "Labels are not loaded yet.";

        /// <summary>Hint shown while there is nothing to search, for example before loading.</summary>
        [DataMember]
        public string StatusText
        {
            get => this.statusText;
            set => this.SetProperty(ref this.statusText, value);
        }
    }
}
