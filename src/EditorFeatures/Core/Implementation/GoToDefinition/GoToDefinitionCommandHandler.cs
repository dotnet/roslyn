// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToDefinition,
       ContentTypeNames.RoslynContentType)]
    internal class GoToDefinitionCommandHandler :
        ICommandHandler<GoToDefinitionCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public GoToDefinitionCommandHandler(
            IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public CommandState GetCommandState(GoToDefinitionCommandArgs args, Func<CommandState> nextHandler)
        {
            return CommandState.Available;
        }

        public void ExecuteCommand(GoToDefinitionCommandArgs args, Action nextHandler)
        {
            var caretPos = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (caretPos.HasValue && TryExecuteCommand(args.SubjectBuffer.CurrentSnapshot, caretPos.Value))
            {
                return;
            }

            nextHandler();
        }

        internal bool TryExecuteCommand(ITextSnapshot snapshot, int caretPosition)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var goToDefinitionService = document.Project.LanguageServices.GetService<IGoToDefinitionService>();
                return TryExecuteCommand(document, caretPosition, goToDefinitionService);
            }
            else
            {
                // We didn't even have a workspace, so we can let somebody else try to handle this if they can
                return false;
            }
        }

        // Internal for testing purposes only.
        internal bool TryExecuteCommand(Document document, int caretPosition, IGoToDefinitionService goToDefinitionService)
        {
            string errorMessage = null;

            var result = _waitIndicator.Wait(
                title: EditorFeaturesResources.Go_to_Definition,
                message: EditorFeaturesResources.Navigating_to_definition,
                allowCancel: true,
                action: waitContext =>
                {
                    if (goToDefinitionService != null &&
                        goToDefinitionService.TryGoToDefinition(document, caretPosition, waitContext.CancellationToken))
                    {
                        return;
                    }

                    errorMessage = EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret;
                });

            if (result == WaitIndicatorResult.Completed && errorMessage != null)
            {
                var workspace = document.Project.Solution.Workspace;
                var notificationService = workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(errorMessage, title: EditorFeaturesResources.Go_to_Definition, severity: NotificationSeverity.Information);
            }

            return true;
        }
    }
}
