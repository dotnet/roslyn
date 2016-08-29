using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    [ExportVersionSpecific(typeof(ICompletionSetFactory), VisualStudioVersion.Dev14)]
    internal sealed class VisualStudio14CompletionSetFactory : ICompletionSetFactory
    {
        public ICompletionSet CreateCompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            return new VisualStudio14CompletionSet(
                completionPresenterSession, textView, subjectBuffer);
        }
    }
}
