// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceCommandHandler : ICommandHandler<ExtractInterfaceCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;

        protected AbstractExtractInterfaceCommandHandler(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public string DisplayName => EditorFeaturesResources.Extract_Interface;

        public CommandState GetCommandState(ExtractInterfaceCommandArgs args)
            => IsAvailable(args.SubjectBuffer, out _) ? CommandState.Available : CommandState.Unspecified;

        public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Extract_Interface))
            {
                var subjectBuffer = args.SubjectBuffer;
                if (!IsAvailable(subjectBuffer, out var workspace))
                {
                    return false;
                }

                var caretPoint = args.TextView.GetCaretPoint(subjectBuffer);
                if (!caretPoint.HasValue)
                {
                    return false;
                }

                var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document == null)
                {
                    return false;
                }

                // We are about to show a modal UI dialog so we should take over the command execution
                // wait context. That means the command system won't attempt to show its own wait dialog 
                // and also will take it into consideration when measuring command handling duration.
                context.OperationContext.TakeOwnership();
                var extractInterfaceService = document.GetLanguageService<AbstractExtractInterfaceService>();
                var result = _threadingContext.JoinableTaskFactory.Run(() =>
                    extractInterfaceService.ExtractInterfaceAsync(
                        document,
                        caretPoint.Value.Position,
                        (errorMessage, severity) => workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity),
                        CancellationToken.None));

                if (result == null || !result.Succeeded)
                {
                    return true;
                }

                if (!document.Project.Solution.Workspace.TryApplyChanges(result.UpdatedSolution))
                {
                    // TODO: handle failure
                    return true;
                }

                // TODO: Use a threaded-wait-dialog here so we can cancel navigation.
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
                _threadingContext.JoinableTaskFactory.Run(() =>
                    navigationService.TryNavigateToPositionAsync(workspace, result.NavigationDocumentId, 0, CancellationToken.None));

                return true;
            }
        }

        private static bool IsAvailable(ITextBuffer subjectBuffer, out Workspace workspace)
            => subjectBuffer.TryGetWorkspace(out workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.AddDocument) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) &&
               subjectBuffer.SupportsRefactorings();
    }
}
