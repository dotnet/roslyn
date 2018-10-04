// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export(typeof(IPasteTrackingService)), Shared]
    [Export(typeof(PasteTrackingService))]
    internal class PasteTrackingService : IPasteTrackingService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ILightBulbBroker2 _lightBulbBroker;
        private readonly ISuggestedActionCategorySet _refactoringsCategorySet;

        [ImportingConstructor]
        public PasteTrackingService(IThreadingContext threadingContext, ILightBulbBroker2 lightBulbBroker, ISuggestedActionCategoryRegistryService categoryRegistryService)
        {
            _threadingContext = threadingContext;
            _lightBulbBroker = lightBulbBroker;
            _refactoringsCategorySet = categoryRegistryService.AllRefactorings;
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

        internal void RegisterPastedTextSpan(ITextView textView, ITextBuffer textBuffer, TextSpan textSpan)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            textBuffer.Changed += RemovePastedTestSpan;
            textBuffer.Properties.AddProperty(this, textSpan);

            // Try to start the light bulb session in an unexpanded state now
            // so that it displayed to the user sooner.
            TryShowLightBulb(textView, textSpan);

            return;

            void RemovePastedTestSpan(object sender, TextContentChangedEventArgs e)
            {
                textBuffer.Changed -= RemovePastedTestSpan;
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

        private void TryShowLightBulb(ITextView textView, TextSpan textSpan)
        {
            // Use the current caret positon as the trigger point and span
            var caretPosition = textView.Caret.Position.BufferPosition;
            var triggerPoint = textView.TextBuffer.CurrentSnapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Negative);
            var triggerSpan = textView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(caretPosition.Position, 0, SpanTrackingMode.EdgeExclusive);

            // Start a suggested refactoring session. Session ends if no suggested actions are found.
            var session = _lightBulbBroker.CreateSession(
                _refactoringsCategorySet, textView, triggerPoint, triggerSpan, _refactoringsCategorySet, trackMouse: false);
            session.Start();
        }
    }
}
