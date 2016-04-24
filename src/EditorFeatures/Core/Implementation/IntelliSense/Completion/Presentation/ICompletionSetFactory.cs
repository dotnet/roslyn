using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal interface ICompletionSetFactory
    {
        ICompletionSet CreateCompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer);
    }
}
