using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Labels;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;

namespace BE.LabelExtension.Editor
{
    /// <summary>
    /// Starts loading the labels when the first <c>.xpp</c> file opens, as the X++ editor shows
    /// code from <c>.xpp</c> files (concept, sequence Laden). Creating it also creates the label
    /// store and the loader, which the tooltip and the inline display reach through
    /// <see cref="SharedServices"/>.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class XppDocumentListener : ExtensionPart, ITextViewOpenClosedListener
    {
        private readonly LabelLoader loader;

        /// <summary>Creates the listener.</summary>
        /// <param name="extension">The extension.</param>
        /// <param name="extensibility">Entry point to the Visual Studio extensibility API.</param>
        /// <param name="loader">Loads the labels.</param>
        public XppDocumentListener(Extension extension, VisualStudioExtensibility extensibility, LabelLoader loader)
            : base(extension, extensibility)
        {
            this.loader = loader;
        }

        /// <inheritdoc />
        public TextViewExtensionConfiguration TextViewExtensionConfiguration => new()
        {
            AppliesTo = [DocumentFilter.FromGlobPattern("**/*.xpp", relativePath: false)],
        };

        /// <inheritdoc />
        public Task TextViewOpenedAsync(ITextViewSnapshot textView, CancellationToken cancellationToken)
        {
            this.loader.EnsureLoaded();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task TextViewClosedAsync(ITextViewSnapshot textView, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
