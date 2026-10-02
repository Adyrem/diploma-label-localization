using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Remote UI content of the tool window. The XAML comes from the embedded resource
    /// LabelToolWindowContent.xaml next to this class.
    /// </summary>
    internal sealed class LabelToolWindowContent : RemoteUserControl
    {
        /// <summary>Creates the content bound to the given data.</summary>
        /// <param name="data">Data context of the tool window.</param>
        public LabelToolWindowContent(LabelToolWindowData data)
            : base(dataContext: data)
        {
        }
    }
}
