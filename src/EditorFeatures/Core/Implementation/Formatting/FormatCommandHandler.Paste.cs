// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            _waitIndicator.Wait(
                title: EditorFeaturesResources.FormatPaste,
                message: EditorFeaturesResources.FormattingPastedText,
                allowCancel: true,
                action: c => ExecuteCommandWorker(args, nextHandler, c.CancellationToken));
        }

        private void ExecuteCommandWorker(PasteCommandArgs args, Action nextHandler, CancellationToken cancellationToken)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);

            nextHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            if (!args.SubjectBuffer.GetOption(FeatureOnOffOptions.FormatOnPaste) ||
                !caretPosition.HasValue)
            {
                return;
            }

            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var formattingRuleService = document.Project.Solution.Workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            if (formattingRuleService != null && formattingRuleService.ShouldNotFormatOrCommitOnPaste(document))
            {
                return;
            }

            var formattingService = document.GetLanguageService<IEditorFormattingService>();
            if (formattingService == null || !formattingService.SupportsFormatOnPaste)
            {
                return;
            }

            var span = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot).Span.ToTextSpan();
            var changes = formattingService.GetFormattingChangesOnPasteAsync(document, span, cancellationToken).WaitAndGetResult(cancellationToken);
            if (changes.Count == 0)
            {
                return;
            }

            document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
        }
    }
}
