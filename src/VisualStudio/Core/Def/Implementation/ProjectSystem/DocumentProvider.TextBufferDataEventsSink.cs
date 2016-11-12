// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class DocumentProvider
    {
        private class TextBufferDataEventsSink : IVsTextBufferDataEvents
        {
            private readonly DocumentKey _documentKey;
            private readonly string _moniker;
            private readonly DocumentProvider _documentProvider;
            private readonly IVsTextBuffer _textBuffer;

            private IComEventSink _sink;

            /// <summary>
            /// Helper method for creating and hooking up a <c>TextBufferDataEventsSink</c>.
            /// </summary>
            public static void HookupHandler(DocumentProvider documentProvider, IVsTextBuffer textBuffer, DocumentKey documentKey)
            {
                var eventHandler = new TextBufferDataEventsSink(documentProvider, textBuffer, documentKey);

                eventHandler._sink = ComEventSink.Advise<IVsTextBufferDataEvents>(textBuffer, eventHandler);
            }

            public static void HookupHandler(DocumentProvider documentProvider, IVsTextBuffer textBuffer, string moniker)
            {
                var eventHandler = new TextBufferDataEventsSink(documentProvider, textBuffer, moniker);

                eventHandler._sink = ComEventSink.Advise<IVsTextBufferDataEvents>(textBuffer, eventHandler);
            }

            private TextBufferDataEventsSink(DocumentProvider documentProvider, IVsTextBuffer textBuffer, DocumentKey documentKey)
            {
                _documentProvider = documentProvider;
                _textBuffer = textBuffer;
                _documentKey = documentKey;
                _moniker = documentKey.Moniker;
            }

            private TextBufferDataEventsSink(DocumentProvider documentProvider, IVsTextBuffer textBuffer, string moniker)
            {
                _documentProvider = documentProvider;
                _textBuffer = textBuffer;
                _moniker = moniker;
            }

            public void OnFileChanged(uint grfChange, uint dwFileAttrs)
            {
            }

            public int OnLoadCompleted(int fReload)
            {
                _sink.Unadvise();

                _documentProvider.DocumentLoadCompleted(_textBuffer, _documentKey, _moniker);

                return VSConstants.S_OK;
            }
        }
    }
}
