using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    /// <summary>
    /// Arguments for async find references
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class AsyncFindReferencesCommandArgs : CommandArgs
    {
        public AsyncFindReferencesCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
            : base(textView, subjectBuffer)
        {
        }
    }
}