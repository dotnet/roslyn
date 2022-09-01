// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal abstract class AbstractEncapsulateFieldCommandHandler : ICommandHandler<EncapsulateFieldCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IAsynchronousOperationListener _listener;

        public string DisplayName => EditorFeaturesResources.Encapsulate_Field;

        public AbstractEncapsulateFieldCommandHandler(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _undoManager = undoManager;
            _listener = listenerProvider.GetListener(FeatureAttribute.EncapsulateField);
        }

        public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
        {
            if (!args.SubjectBuffer.SupportsRefactorings())
            {
                return false;
            }

            using var waitScope = context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Applying_Encapsulate_Field_refactoring);

            return Execute(args, waitScope);
        }

        private bool Execute(EncapsulateFieldCommandArgs args, IUIThreadOperationScope waitScope)
        {
            using var token = _listener.BeginAsyncOperation("EncapsulateField");

            var cancellationToken = waitScope.Context.UserCancellationToken;
            var document = args.SubjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                waitScope.Context, _threadingContext);
            if (document == null)
            {
                return false;
            }

            var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            var service = document.GetLanguageService<AbstractEncapsulateFieldService>();

            var result = service.EncapsulateFieldsInSpanAsync(document, spans.First().Span.ToTextSpan(), true, cancellationToken).WaitAndGetResult(cancellationToken);

            // We are about to show a modal UI dialog so we should take over the command execution
            // wait context. That means the command system won't attempt to show its own wait dialog 
            // and also will take it into consideration when measuring command handling duration.
            waitScope.Context.TakeOwnership();

            var workspace = document.Project.Solution.Workspace;
            if (result == null)
            {
                var notificationService = workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.Please_select_the_definition_of_the_field_to_encapsulate, severity: NotificationSeverity.Error);
                return false;
            }

            waitScope.AllowCancellation = false;
            cancellationToken = waitScope.Context.UserCancellationToken;

            var finalSolution = result.GetSolutionAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var previewService = workspace.Services.GetService<IPreviewDialogService>();
            if (previewService != null)
            {
                finalSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Encapsulate_Field),
                     "vs.csharp.refactoring.preview",
                    EditorFeaturesResources.Encapsulate_Field_colon,
                    result.Name,
                    result.Glyph,
                    finalSolution,
                    document.Project.Solution);
            }

            if (finalSolution == null)
            {
                // User clicked cancel.
                return true;
            }

            using (var undoTransaction = _undoManager.GetTextBufferUndoManager(args.SubjectBuffer).TextBufferUndoHistory.CreateTransaction(EditorFeaturesResources.Encapsulate_Field))
            {
                if (!workspace.TryApplyChanges(finalSolution))
                {
                    undoTransaction.Cancel();
                    return false;
                }

                undoTransaction.Complete();
            }

            return true;
        }

        public CommandState GetCommandState(EncapsulateFieldCommandArgs args)
            => args.SubjectBuffer.SupportsRefactorings() ? CommandState.Available : CommandState.Unspecified;
    }
}
