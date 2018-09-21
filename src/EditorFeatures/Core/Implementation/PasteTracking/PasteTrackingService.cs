// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export(typeof(IPasteTrackingService)), Shared]
    internal class PasteTrackingService : IPasteTrackingService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;

        [ImportingConstructor]
        public PasteTrackingService(IThreadingContext threadingContext, ITextBufferAssociatedViewService textBufferAssociatedViewService)
        {
            _threadingContext = threadingContext;
            _textBufferAssociatedViewService = textBufferAssociatedViewService;
        }

        public bool TryGetPastedTextSpan(Document document, out TextSpan textSpan)
        {
            if (!TryGetTextBuffer(document, out var textBuffer))
            {
                textSpan = default;
                return false;
            }

            // `PropertiesCollection` is thread-safe
            return textBuffer.Properties.TryGetProperty(this, out textSpan);
        }

        internal void RegisterPastedTextSpan(Document document, TextSpan textSpan)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            if (!TryGetTextBuffer(document, out var textBuffer))
            {
                return;
            }

            var textView = _textBufferAssociatedViewService
                .GetAssociatedTextViews(textBuffer)
                .FirstOrDefault(view => view.HasAggregateFocus);

            if (textView is null)
            {
                return;
            }

            textView.Closed += ClearTracking;
            textBuffer.Changed += ClearTracking;

            textBuffer.Properties.AddProperty(this, textSpan);

            return;

            void ClearTracking(object sender, EventArgs e)
            {
                textView.Closed -= ClearTracking;
                textBuffer.Changed -= ClearTracking;

                ClearPastedTextSpan(document);
            }
        }

        private void ClearPastedTextSpan(Document document)
        {
            if (TryGetTextBuffer(document, out var textBuffer))
            {
                textBuffer.Properties.RemoveProperty(this);
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
