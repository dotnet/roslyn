// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export(typeof(IPasteTrackingService)), Shared]
    [Export(typeof(PasteTrackingService))]
    internal class PasteTrackingService : IPasteTrackingService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly object _pastedTextSpanKey = new object();

        [ImportingConstructor]
        public PasteTrackingService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public bool TryGetPastedTextSpan(Document document, out TextSpan textSpan)
        {
            if (!TryGetTextBuffer(document, out var textBuffer))
            {
                textSpan = default;
                return false;
            }

            // `PropertiesCollection` is thread-safe
            return textBuffer.Properties.TryGetProperty(_pastedTextSpanKey, out textSpan);
        }

        internal void RegisterPastedTextSpan(ITextBuffer textBuffer, TextSpan textSpan)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            textBuffer.Changed += RemovePastedTextSpan;
            textBuffer.Properties.AddProperty(_pastedTextSpanKey, textSpan);

            return;

            void RemovePastedTextSpan(object sender, TextContentChangedEventArgs e)
            {
                textBuffer.Changed -= RemovePastedTextSpan;
                textBuffer.Properties.RemoveProperty(_pastedTextSpanKey);
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
