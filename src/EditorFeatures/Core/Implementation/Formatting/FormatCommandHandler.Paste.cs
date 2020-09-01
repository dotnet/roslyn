// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_pasted_text))
            {
                ExecuteCommandWorker(args, nextHandler, context.OperationContext.UserCancellationToken);
            }
        }

        private static void ExecuteCommandWorker(PasteCommandArgs args, Action nextHandler, CancellationToken cancellationToken)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);

            nextHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            if (!args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.FormatOnPaste) ||
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
