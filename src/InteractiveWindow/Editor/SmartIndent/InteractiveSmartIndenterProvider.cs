using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName)]
    internal class InteractiveSmartIndenterProvider : ISmartIndentProvider
    {
        private readonly ITextEditorFactoryService editorFactory;
        private readonly IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> indentProviders;

        [ImportingConstructor]
        public InteractiveSmartIndenterProvider(
            ITextEditorFactoryService editorFactory,
            [ImportMany] IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> indentProviders)
        {
            if (editorFactory == null)
            {
                throw new ArgumentNullException("editorFactory");
            }

            if (indentProviders == null)
            {
                throw new ArgumentNullException("indentProviders");
            }

            this.editorFactory = editorFactory;
            this.indentProviders = indentProviders;
        }

        public ISmartIndent CreateSmartIndent(ITextView view)
        {
            var window = InteractiveWindow.FromBuffer(view.TextBuffer);
            if (window == null || window.CurrentLanguageBuffer == null)
            {
                return null;
            }

            return InteractiveSmartIndenter.Create(this.indentProviders, window.CurrentLanguageBuffer.ContentType, view);
        }
    }
}
