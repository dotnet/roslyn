using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    /// <summary>
    /// We have two implementations of ICompletionSet that are specific to different VS versions
    /// because the newer one lights up new functionality from the platform.  This let's the
    /// presenter create the right one.
    /// </summary>
    internal interface ICompletionSetFactory
    {
        ICompletionSet CreateCompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer);
    }
}
