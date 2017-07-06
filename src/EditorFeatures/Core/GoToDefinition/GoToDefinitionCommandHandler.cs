// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
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

        private (Document, IGoToDefinitionService) GetDocumentAndService(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            return (document, document?.GetLanguageService<IGoToDefinitionService>());
        }

        public CommandState GetCommandState(GoToDefinitionCommandArgs args, Func<CommandState> nextHandler)
        {
            var (document, service) = GetDocumentAndService(args.SubjectBuffer.CurrentSnapshot);
            return service != null
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        public void ExecuteCommand(GoToDefinitionCommandArgs args, Action nextHandler)
        {
            var subjectBuffer = args.SubjectBuffer;
            var (document, service) = GetDocumentAndService(subjectBuffer.CurrentSnapshot);
            if (service != null)
            {
                var caretPos = args.TextView.GetCaretPoint(subjectBuffer);
                if (caretPos.HasValue && TryExecuteCommand(document, caretPos.Value, service))
                {
                    return;
                }
            }

            nextHandler();
        }

        // Internal for testing purposes only.
        internal bool TryExecuteCommand(ITextSnapshot snapshot, int caretPosition)
            => TryExecuteCommand(snapshot.GetOpenDocumentInCurrentContextWithChanges(), caretPosition);

        internal bool TryExecuteCommand(Document document, int caretPosition)
            => TryExecuteCommand(document, caretPosition, document.GetLanguageService<IGoToDefinitionService>());

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
