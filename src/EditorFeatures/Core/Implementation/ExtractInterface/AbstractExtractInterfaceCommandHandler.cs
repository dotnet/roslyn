// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceCommandHandler : VSCommanding.ICommandHandler<ExtractInterfaceCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;

        protected AbstractExtractInterfaceCommandHandler(IThreadingContext threadingContext)
        {
            this._threadingContext = threadingContext;
        }

        public string DisplayName => EditorFeaturesResources.Extract_Interface;

        public VSCommanding.CommandState GetCommandState(ExtractInterfaceCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }

        public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var workspace = document.Project.Solution.Workspace;

            if (!workspace.CanApplyChange(ApplyChangesKind.AddDocument) ||
                !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return false;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return false;
            }

            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
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

            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToPosition(workspace, result.NavigationDocumentId, 0);

            return true;
        }
    }
}
