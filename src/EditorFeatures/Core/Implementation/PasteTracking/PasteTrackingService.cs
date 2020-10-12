﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
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
        private readonly object _pastedTextSpanKey = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PasteTrackingService(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
        {
            var textBuffer = sourceTextContainer.TryGetTextBuffer();
            if (textBuffer is null)
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

            // Use the TextBuffer properties to store the pasted text span. 
            // The `PropertiesCollection` is thread-safe and will be cleared
            // when all TextViews that share this buffer are closed.
            // Any change to the TextBuffer will remove the pasted text span.
            // This includes consecutive paste operations which will fire the
            // Changed event prior to the handler registering a new text span.
            textBuffer.Properties[_pastedTextSpanKey] = textSpan;
            textBuffer.Changed += RemovePastedTextSpan;

            return;

            void RemovePastedTextSpan(object sender, TextContentChangedEventArgs e)
            {
                textBuffer.Changed -= RemovePastedTextSpan;
                textBuffer.Properties.RemoveProperty(_pastedTextSpanKey);
            }
        }
    }
}
