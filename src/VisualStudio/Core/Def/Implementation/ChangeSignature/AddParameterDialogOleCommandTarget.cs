using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal sealed class AddParameterDialogOleCommandTarget : AbstractOleCommandTarget
    {
        internal AddParameterDialogOleCommandTarget(
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            IServiceProvider serviceProvider)
            : base(wpfTextView, editorAdaptersFactory, serviceProvider)
        {
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            return this.WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.RoslynContentType);
        }
    }
}
