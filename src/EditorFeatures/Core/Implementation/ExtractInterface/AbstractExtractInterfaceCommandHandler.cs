// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using EditorCommanding = Microsoft.VisualStudio.Text.UI.Commanding;
using EditorCommands = Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceCommandHandler : EditorCommanding.ICommandHandler<EditorCommands.ExtractInterfaceCommandArgs>
    {
        public bool InterestedInReadOnlyBuffer => false;

        public EditorCommanding.CommandState GetCommandState(EditorCommands.ExtractInterfaceCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return EditorCommanding.CommandState.CommandIsUnavailable;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return EditorCommanding.CommandState.CommandIsUnavailable;
            }

            return EditorCommanding.CommandState.CommandIsAvailable; ;
        }

        public bool ExecuteCommand(EditorCommands.ExtractInterfaceCommandArgs args)
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

            var extractInterfaceService = document.GetLanguageService<AbstractExtractInterfaceService>();
            var result = extractInterfaceService.ExtractInterface(
                document,
                caretPoint.Value.Position,
                (errorMessage, severity) => workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity),
                CancellationToken.None);

            if (result == null || !result.Succeeded)
            {
                return false;
            }

            if (!document.Project.Solution.Workspace.TryApplyChanges(result.UpdatedSolution))
            {
                // TODO: handle failure
                return false;
            }

            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToPosition(workspace, result.NavigationDocumentId, 0);
            return true;
        }
    }
}
