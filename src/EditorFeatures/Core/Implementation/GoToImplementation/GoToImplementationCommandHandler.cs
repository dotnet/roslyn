// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToImplementation,
        ContentTypeNames.RoslynContentType)]
    internal sealed class GoToImplementationCommandHandler : ICommandHandler<GoToImplementationCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public GoToImplementationCommandHandler(
            IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public CommandState GetCommandState(GoToImplementationCommandArgs args, Func<CommandState> nextHandler)
        {
            // Because this is expensive to compute, we just always say yes
            return CommandState.Available;
        }

        public void ExecuteCommand(GoToImplementationCommandArgs args, Action nextHandler)
        {
            var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);

            if (caret.HasValue)
            {
                var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    var service = document.Project.LanguageServices.GetService<IGoToImplementationService>();

                    if (service != null)
                    {
                        // We have all the cheap stuff, so let's do expensive stuff now
                        string messageToShow = null;
                        bool succeeded = false;
                        _waitIndicator.Wait(
                            EditorFeaturesResources.GoToImplementationTitle,
                            EditorFeaturesResources.GoToImplementationMessage,
                            allowCancel: true,
                            action: context => succeeded = service.TryGoToImplementation(document, caret.Value, context.CancellationToken, out messageToShow));

                        if (messageToShow != null)
                        {
                            var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                            notificationService.SendNotification(messageToShow,
                                title: EditorFeaturesResources.GoToImplementationTitle,
                                severity: NotificationSeverity.Information);
                        }
                    }

                    return;
                }
            }

            nextHandler();
        }

    }
}
