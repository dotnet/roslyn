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
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal sealed class EventHookupQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly ITextBuffer _textBuffer;

        public EventHookupQuickInfoSource(ITextBuffer textBuffer, ClassificationTypeMap classificationTypeMap, IClassificationFormatMapService classificationFormatMapService)
        {
            _textBuffer = textBuffer;
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMapService = classificationFormatMapService;
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            // Ensure this is a quick info session created by event hookup
            if (!session.Properties.TryGetProperty(typeof(EventHookupSessionManager), out EventHookupSessionManager eventHookupSessionManager))
            {
                return (QuickInfoItem)null;
            }

            if (!eventHookupSessionManager.IsTrackingSession())
            {
                await session.DismissAsync().ConfigureAwait(false);
                return (QuickInfoItem)null;
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
                await session.DismissAsync().ConfigureAwait(false);
                return (QuickInfoItem)null;
            }

            // We should show the quick info session. Calculate the span and create the content.
            var currentSnapshot = _textBuffer.CurrentSnapshot;
            var applicableToSpan = currentSnapshot.CreateTrackingSpan(
                start: eventHookupSessionManager.CurrentSession.TrackingPoint.GetPosition(currentSnapshot),
                length: 0,
                trackingMode: SpanTrackingMode.EdgeInclusive);

            var content = CreateContent(eventHandlerName, _classificationTypeMap);

            // For test purposes only!
            eventHookupSessionManager.TEST_MostRecentQuickInfoContent = content;

            return new QuickInfoItem(applicableToSpan, content);
        }

        private FrameworkElement CreateContent(string eventName, ClassificationTypeMap classificationTypeMap)
        {
            var textBlock = new TextBlock { TextWrapping = TextWrapping.NoWrap };
            textBlock.SetDefaultTextProperties(_classificationFormatMapService.GetClassificationFormatMap("tooltip"));

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
