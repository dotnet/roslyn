using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Editor;
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

            var ivsTextview = _editorAdaptersFactoryService.GetViewAdapter(textView);
            if (ivsTextview != null)
            {
                if (ivsTextview.GetBuffer(out var textLines) != VSConstants.S_OK)
                {
                    return false;
                }

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
            if (stream.EnumMarkers(line.Start, line.Length, (int)markerType, (int)ENUMMARKERFLAGS.EM_DEFAULT, out var enumerator) == VSConstants.S_OK &&
                enumerator.GetCount(out var count) == VSConstants.S_OK)
            {
                for (int i = 0; i < count; i++)
                {
                    if (enumerator.Next(out var marker) == VSConstants.S_OK &&
                        marker.GetCurrentSpan(out var start, out var len) == VSConstants.S_OK)
                    {
                        if (start <= caretPosition && caretPosition <= start + len)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
