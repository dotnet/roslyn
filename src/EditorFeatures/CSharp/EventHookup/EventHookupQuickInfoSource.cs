// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal sealed class EventHookupQuickInfoSource : IQuickInfoSource
    {
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly ITextBuffer _textBuffer;

        public EventHookupQuickInfoSource(ITextBuffer textBuffer, ClassificationTypeMap classificationTypeMap)
        {
            _textBuffer = textBuffer;
            _classificationTypeMap = classificationTypeMap;
        }

        void IQuickInfoSource.AugmentQuickInfoSession(IQuickInfoSession existingQuickInfoSession, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            // Augmenting quick info isn't cancellable.
            var cancellationToken = CancellationToken.None;
            EventHookupSessionManager eventHookupSessionManager;

            // Ensure this is a quick info session created by event hookup
            if (!existingQuickInfoSession.Properties.TryGetProperty(typeof(EventHookupSessionManager), out eventHookupSessionManager))
            {
                applicableToSpan = null;
                return;
            }

            if (!eventHookupSessionManager.IsTrackingSession())
            {
                existingQuickInfoSession.Dismiss();
                applicableToSpan = null;
                return;
            }

            string eventHandlerName = null;

            // Get the event handler method name. The name was calculated when the quick info
            // session was created, so we do not need to wait for the task here.
            if (eventHookupSessionManager.CurrentSession.GetEventNameTask.Status == TaskStatus.RanToCompletion)
            {
                eventHandlerName = eventHookupSessionManager.CurrentSession.GetEventNameTask.Result;
            }

            if (eventHandlerName == null)
            {
                existingQuickInfoSession.Dismiss();
                applicableToSpan = null;
                return;
            }

            // We should show the quick info session. Calculate the span and create the content.
            var currentSnapshot = _textBuffer.CurrentSnapshot;
            applicableToSpan = currentSnapshot.CreateTrackingSpan(
                start: eventHookupSessionManager.CurrentSession.TrackingPoint.GetPosition(currentSnapshot),
                length: 0,
                trackingMode: SpanTrackingMode.EdgeInclusive);

            // Clear any existing quick info content. This ensures that the event hookup text is
            // the only text in the quick info.
            quickInfoContent.Clear();

            var content = CreateContent(eventHandlerName, _classificationTypeMap);

            quickInfoContent.Add(content);

            // For test purposes only!
            eventHookupSessionManager.TEST_MostRecentQuickInfoContent = content;
        }

        private FrameworkElement CreateContent(string eventName, ClassificationTypeMap classificationTypeMap)
        {
            var textBlock = new TextBlock { TextWrapping = TextWrapping.NoWrap };
            textBlock.SetDefaultTextProperties(classificationTypeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip"));

            var eventNameRun = new Run(eventName + ";");
            eventNameRun.FontWeight = FontWeights.Bold;
            textBlock.Inlines.Add(eventNameRun);

            var pressTabRun = new Run(CSharpEditorResources.Press_TAB_to_insert);
            textBlock.Inlines.Add(pressTabRun);

            return textBlock;
        }

        public void Dispose()
        {
        }
    }
}
