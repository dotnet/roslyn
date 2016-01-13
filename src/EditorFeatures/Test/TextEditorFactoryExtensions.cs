using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    internal static class TextEditorFactoryExtensions
    {
        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory)
        {
            return new DisposableTextView(textEditorFactory.CreateTextView());
        }

        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory, ITextBuffer buffer)
        {
            return new DisposableTextView(textEditorFactory.CreateTextView(buffer));
        }
    }

    public class DisposableTextView : IDisposable
    {
        public DisposableTextView(IWpfTextView textView)
        {
            this.TextView = textView;
        }

        public IWpfTextView TextView { get; private set; }

        public void Dispose()
        {
            TextView.Close();
        }
    }
}
