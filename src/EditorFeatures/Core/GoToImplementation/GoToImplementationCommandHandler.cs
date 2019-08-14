// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToImplementation)]
    internal partial class GoToImplementationCommandHandler : VSCommanding.ICommandHandler<GoToImplementationCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToImplementationCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public string DisplayName => EditorFeaturesResources.Go_To_Implementation;

        public VSCommanding.CommandState GetCommandState(GoToImplementationCommandArgs args)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var findUsagesService = document?.GetLanguageService<IFindUsagesService>();
            return findUsagesService != null
                ? VSCommanding.CommandState.Available
                : VSCommanding.CommandState.Unavailable;
        }

        public bool ExecuteCommand(GoToImplementationCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Locating_implementations))
            {
                var subjectBuffer = args.SubjectBuffer;
                if (!subjectBuffer.TryGetWorkspace(out var workspace))
                {
                    return false;
                }

                var findUsagesService = workspace.Services.GetLanguageServices(args.SubjectBuffer)?.GetService<IFindUsagesService>();
                if (findUsagesService != null)
                {
                    var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (caret.HasValue)
                    {
                        var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                            context.OperationContext, _threadingContext);
                        if (document != null)
                        {
                            ExecuteCommand(document, caret.Value, findUsagesService, context);
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private void ExecuteCommand(
            Document document, int caretPosition,
            IFindUsagesService streamingService,
            CommandExecutionContext context)
        {
            if (streamingService != null)
            {
                // We have all the cheap stuff, so let's do expensive stuff now
                string messageToShow = null;

                var userCancellationToken = context.OperationContext.UserCancellationToken;
                using (Logger.LogBlock(FunctionId.CommandHandler_GoToImplementation, KeyValueLogMessage.Create(LogType.UserAction), userCancellationToken))
                {
                    StreamingGoToImplementation(
                        document, caretPosition,
                        streamingService, _streamingPresenter,
                        userCancellationToken, out messageToShow);
                }

                if (messageToShow != null)
                {
                    // We are about to show a modal UI dialog so we should take over the command execution
                    // wait context. That means the command system won't attempt to show its own wait dialog 
                    // and also will take it into consideration when measuring command handling duration.
                    context.OperationContext.TakeOwnership();
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(messageToShow,
                        title: EditorFeaturesResources.Go_To_Implementation,
                        severity: NotificationSeverity.Information);
                }
            }
        }

        private void StreamingGoToImplementation(
            Document document, int caretPosition,
            IFindUsagesService findUsagesService,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            out string messageToShow)
        {
            // We create our own context object, simply to capture all the definitions reported by 
            // the individual IFindUsagesService.  Once we get the results back we'll then decide 
            // what to do with them.  If we get only a single result back, then we'll just go 
            // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
            var goToImplContext = new SimpleFindUsagesContext(cancellationToken);
            findUsagesService.FindImplementationsAsync(document, caretPosition, goToImplContext).Wait(cancellationToken);

            // If finding implementations reported a message, then just stop and show that 
            // message to the user.
            messageToShow = goToImplContext.Message;
            if (messageToShow != null)
            {
                return;
            }

            var definitionItems = goToImplContext.GetDefinitions();

            streamingPresenter.TryNavigateToOrPresentItemsAsync(
                document.Project.Solution.Workspace, goToImplContext.SearchTitle, definitionItems).Wait(cancellationToken);
        }
    }
}
