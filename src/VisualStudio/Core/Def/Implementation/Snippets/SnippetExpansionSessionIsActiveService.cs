using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    [Shared]
    [ExportWorkspaceService(typeof(ICaretIsInSnippetExpansionFieldService), ServiceLayer.Host)]
    internal class CaretIsInSnippetExpansionFieldService : ForegroundThreadAffinitizedObject, ICaretIsInSnippetExpansionFieldService
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        [ImportingConstructor]
        public CaretIsInSnippetExpansionFieldService(IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public bool CaretIsInSnippetExpansionField(ITextView textView)
        {
            AssertIsForeground();

            var ivsTextview =_editorAdaptersFactoryService.GetViewAdapter(textView);
            if (ivsTextview != null)
            {
                ivsTextview.GetBuffer(out var textLines);
                var stream = (IVsTextStream)textLines;
                var caretLine = textView.Caret.ContainingTextViewLine;
                var caretPosition = textView.Caret.Position.BufferPosition.Position;

                // Does the caret intersect with a snippet field or the selected snippet field?
                // The text marker types are unrelated so we have to search twice, once for
                // a normal snippet field and once for the selected one.
                return CaretIntersectsWithSnippetTag(caretLine, caretPosition, stream, MARKERTYPE2.MARKER_EXSTENCIL) ||
                    CaretIntersectsWithSnippetTag(caretLine, caretPosition, stream, MARKERTYPE2.MARKER_EXSTENCIL_SELECTED);
            }

            return false;
        }

        private static bool CaretIntersectsWithSnippetTag(
            ITextViewLine line, 
            int caretPosition, 
            IVsTextStream stream,
            MARKERTYPE2 markerType)
        {
            // Enumerating over only snippet fields on the current line
            stream.EnumMarkers(line.Start, line.Length, (int)markerType, (int)ENUMMARKERFLAGS.EM_DEFAULT, out var enumerator);
            enumerator.GetCount(out var count);
            for (int i = 0; i < count; i++)
            {
                enumerator.Next(out var marker);
                marker.GetCurrentSpan(out var start, out var len);

                if (start <= caretPosition && caretPosition <= start + len)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
