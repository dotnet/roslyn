using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeActions
{
    internal abstract class PreviewOperationWithTextView : CodeActionOperation
    {
        internal abstract Task<object> GetPreviewAsync(ITextView textView, CancellationToken cancellationToken);
    }
}
