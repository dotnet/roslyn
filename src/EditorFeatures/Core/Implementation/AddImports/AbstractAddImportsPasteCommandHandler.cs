﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AddImports
{
    internal abstract class AbstractAddImportsPasteCommandHandler : IChainedCommandHandler<PasteCommandArgs>
    {
        /// <summary>
        /// The command handler display name
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// The thread await dialog text shown to the user if the operation takes a long time
        /// </summary>
        protected abstract string DialogText { get; }

        private readonly IThreadingContext _threadingContext;

        public AbstractAddImportsPasteCommandHandler(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Check that the feature is enabled before doing any work
            var optionValue = args.SubjectBuffer.GetOptionalFeatureOnOffOption(FeatureOnOffOptions.AddImportsOnPaste);

            // If the feature is explicitly disabled we can exit early
            if (optionValue.HasValue && !optionValue.Value)
            {
                nextCommandHandler();
                return;
            }

            // Capture the pre-paste caret position
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPosition.HasValue)
            {
                nextCommandHandler();
                return;
            }

            // Create a tracking span from the pre-paste caret position that will grow as text is inserted.
            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            // Perform the paste command before adding imports
            nextCommandHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            // Don't perform work if we're inside the interactive window
            if (args.TextView.IsNotSurfaceBufferOfTextView(args.SubjectBuffer))
            {
                return;
            }

            // Applying the post-paste snapshot to the tracking span gives us the span of pasted text.
            var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);
            var textSpan = snapshotSpan.Span.ToTextSpan();

            var sourceTextContainer = args.SubjectBuffer.AsTextContainer();
            if (!Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
            {
                return;
            }

            var document = sourceTextContainer.GetOpenDocumentInCurrentContext();
            if (document is null)
            {
                return;
            }

            var experimentationService = document.Project.Solution.Workspace.Services.GetRequiredService<IExperimentationService>();
            var enabled = optionValue.HasValue && optionValue.Value
                || experimentationService.IsExperimentEnabled(WellKnownExperimentNames.ImportsOnPasteDefaultEnabled);

            if (!enabled)
            {
                return;
            }

            using var _ = executionContext.OperationContext.AddScope(allowCancellation: true, DialogText);
            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

            // We're going to log the same thing on success or failure since this blocks the UI thread. This measurement is 
            // intended to tell us how long we're blocking the user from typing with this action. 
            using var blockLogger = Logger.LogBlock(FunctionId.CommandHandler_Paste_ImportsOnPaste, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);

            var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            var updatedDocument = _threadingContext.JoinableTaskFactory.Run(() => addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken));
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
            if (updatedDocument is null)
            {
                return;
            }

            workspace.TryApplyChanges(updatedDocument.Project.Solution);
        }
    }
}
