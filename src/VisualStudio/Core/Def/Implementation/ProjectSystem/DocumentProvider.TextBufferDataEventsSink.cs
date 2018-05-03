// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class DocumentProvider
    {
        private class TextBufferDataEventsSink : IVsTextBufferDataEvents
        {
            private readonly Action _onDocumentLoadCompleted;

            private ComEventSink _sink;

            /// <summary>
            /// Helper method for creating and hooking up a <c>TextBufferDataEventsSink</c>.
            /// </summary>
            public static void HookupHandler(IVsTextBuffer textBuffer, Action onDocumentLoadCompleted)
            {
                var eventHandler = new TextBufferDataEventsSink(onDocumentLoadCompleted);

                eventHandler._sink = ComEventSink.Advise<IVsTextBufferDataEvents>(textBuffer, eventHandler);
            }

            private TextBufferDataEventsSink(Action onDocumentLoadCompleted)
            {
                _onDocumentLoadCompleted = onDocumentLoadCompleted;
            }

            public void OnFileChanged(uint grfChange, uint dwFileAttrs)
            {
            }

            public int OnLoadCompleted(int fReload)
            {
                _sink.Unadvise();
                _onDocumentLoadCompleted();

                return VSConstants.S_OK;
            }
        }
    }
}
