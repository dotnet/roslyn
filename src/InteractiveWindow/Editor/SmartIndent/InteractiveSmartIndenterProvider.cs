using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Editor.InteractiveWindow
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(InteractiveContentTypeNames.InteractiveContentType)]
    internal class InteractiveSmartIndenterProvider : ISmartIndentProvider
    {
        private readonly ITextEditorFactoryService editorFactory;
        private readonly IEnumerable<Lazy<ISmartIndentProvider, IContentTypeMetadata>> indentProviders;

        [ImportingConstructor]
        public InteractiveSmartIndenterProvider(
            ITextEditorFactoryService editorFactory,
            [ImportMany] IEnumerable<Lazy<ISmartIndentProvider, IContentTypeMetadata>> indentProviders)
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
