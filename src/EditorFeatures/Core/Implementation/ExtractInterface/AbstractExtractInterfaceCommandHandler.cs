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

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceCommandHandler : ICommandHandler<ExtractInterfaceCommandArgs>
    {
        public CommandState GetCommandState(ExtractInterfaceCommandArgs args, Func<CommandState> nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return nextHandler();
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return nextHandler();
            }

            return CommandState.Available;
        }

        public void ExecuteCommand(ExtractInterfaceCommandArgs args, Action nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                nextHandler();
                return;
            }

            var workspace = document.Project.Solution.Workspace;

            if (!workspace.CanApplyChange(ApplyChangesKind.AddDocument) ||
                !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                nextHandler();
                return;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                nextHandler();
                return;
            }

            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                nextHandler();
                return;
            }

            var extractInterfaceService = document.GetLanguageService<AbstractExtractInterfaceService>();
            var result = extractInterfaceService.ExtractInterface(
                document,
                caretPoint.Value.Position,
                (errorMessage, severity) => workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity),
                CancellationToken.None);

            if (result == null || !result.Succeeded)
            {
                return;
            }

            if (!document.Project.Solution.Workspace.TryApplyChanges(result.UpdatedSolution))
            {
                // TODO: handle failure
                return;
            }

            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToPosition(workspace, result.NavigationDocumentId, 0);
        }
    }
}
