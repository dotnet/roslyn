// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal partial class QuickInfoCommandHandlerAndSourceProvider
    {
        private class QuickInfoSource : IQuickInfoSource
        {
            private readonly QuickInfoCommandHandlerAndSourceProvider _commandHandler;
            private readonly ITextBuffer _subjectBuffer;

            public QuickInfoSource(QuickInfoCommandHandlerAndSourceProvider commandHandler, ITextBuffer subjectBuffer)
            {
                _commandHandler = commandHandler;
                _subjectBuffer = subjectBuffer;
            }

            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                applicableToSpan = null;

                object eventHookupValue;
                if (quickInfoContent.Count != 0 ||
                    session.Properties.TryGetProperty(QuickInfoUtilities.EventHookupKey, out eventHookupValue))
                {
                    // No quickinfo if it's the event hookup popup.
                    return;
                }

                var position = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
                if (position.HasValue)
                {
                    var textView = session.TextView;
                    var args = new InvokeQuickInfoCommandArgs(textView, _subjectBuffer);

                    Controller controller;
                    if (_commandHandler.TryGetController(args, out controller))
                    {
                        controller.InvokeQuickInfo(position.Value, trackMouse: true, augmentSession: session);
                    }
                }
            }

            public void Dispose()
            {
            }
        }
    }
}
