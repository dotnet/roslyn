// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(FormatDocumentCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        public void ExecuteCommand(FormatDocumentCommandArgs args, Action nextHandler)
        {
            if (!TryExecuteCommand(args))
            {
                nextHandler();
            }
        }

        private bool TryExecuteCommand(FormatDocumentCommandArgs args)
        {
            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return false;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var formattingService = document.GetLanguageService<IEditorFormattingService>();
            if (formattingService == null || !formattingService.SupportsFormatDocument)
            {
                return false;
            }

            var result = false;
            _waitIndicator.Wait(
                title: EditorFeaturesResources.FormatDocument,
                message: EditorFeaturesResources.FormattingDocument,
                allowCancel: true,
                action: waitContext =>
                {
                    Format(args.TextView, document, null, waitContext.CancellationToken);
                    result = true;
                });

            // We don't call nextHandler, since we have handled this command.
            return result;
        }
    }
}
