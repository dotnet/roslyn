using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    /// NOTE(cyrusn): This type is temporary while we simultaneously support both FindAllRefs
    /// experiences. It will not be necessary when we only have one experience per VS.
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