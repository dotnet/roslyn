// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal partial class QuickInfoCommandHandlerAndSourceProvider
    {
        private class QuickInfoSource : IAsyncQuickInfoSource
        {
            private readonly QuickInfoCommandHandlerAndSourceProvider _commandHandler;
            private readonly ITextBuffer _subjectBuffer;

            public QuickInfoSource(QuickInfoCommandHandlerAndSourceProvider commandHandler, ITextBuffer subjectBuffer)
            {
                _commandHandler = commandHandler;
                _subjectBuffer = subjectBuffer;
            }

            public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                var triggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
                if (triggerPoint != null && triggerPoint.HasValue)
                {
                    var textView = session.TextView;
                    var args = new InvokeQuickInfoCommandArgs(textView, _subjectBuffer);
                    if (_commandHandler.TryGetController(args, out var controller))
                    {
                        return controller.GetQuickInfoItemAsync(triggerPoint.Value, cancellationToken);
                    }
                }

                return Task.FromResult<QuickInfoItem>(null);
            }

            public void Dispose()
            {
            }
        }
    }
}
