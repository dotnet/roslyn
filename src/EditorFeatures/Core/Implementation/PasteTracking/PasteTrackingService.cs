// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export(typeof(IPasteTrackingService)), Shared]
    internal class PasteTrackingService : IPasteTrackingService
    {
        public bool TryGetPasteTrackingInformation(Document document, out PasteTrackingInformation trackingInformation)
        {
            if (TryGetTextBuffer(document, out var textBuffer) &&
                textBuffer.Properties.ContainsProperty(typeof(PasteTrackingInformation)))
            {
                trackingInformation = textBuffer.Properties.GetProperty<PasteTrackingInformation>(typeof(PasteTrackingInformation));
                return true;
            }

            trackingInformation = null;
            return false;
        }

        public bool ClearPasteTrackingInformation(Document document)
        {
            if (!TryGetTextBuffer(document, out var textBuffer) ||
                !textBuffer.Properties.ContainsProperty(typeof(PasteTrackingInformation)))
            {
                return false;
            }

            textBuffer.Properties.RemoveProperty(typeof(PasteTrackingInformation));

            return true;
        }

        public void RegisterPastedTextSpan(Document document, TextSpan textSpan)
        {
            if (!TryGetTextBuffer(document, out var textBuffer))
            {
                return;
            }

            textBuffer.Changed += TextBuffer_Changed;
            textBuffer.Properties.GetOrCreateSingletonProperty(() => new PasteTrackingInformation(textSpan));

            return;

            void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            {
                textBuffer.Changed -= TextBuffer_Changed;
                ClearPasteTrackingInformation(document);
            }
        }

        private bool TryGetTextBuffer(Document document, out ITextBuffer textBuffer)
        {
            if (document == null ||
                !document.TryGetText(out var text))
            {
                textBuffer = null;
                return false;
            }

            textBuffer = text.Container.TryGetTextBuffer();
            return textBuffer != null;
        }
    }
}
