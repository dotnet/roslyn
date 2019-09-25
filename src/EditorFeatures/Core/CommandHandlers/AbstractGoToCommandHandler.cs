// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractGoToCommandHandler<TLanguageService, TCommandArgs> : ICommandHandler<TCommandArgs>
        where TLanguageService : class, ILanguageService
        where TCommandArgs : EditorCommandArgs
    {
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;
        private readonly IThreadingContext _threadingContext;

        public abstract string DisplayName { get; }

        protected abstract string _scopeDescription { get; }

        protected abstract FunctionId _functionId { get; }

        protected abstract Task FindAction(TLanguageService service, Document document, int caretPosition, IFindUsagesContext context);

        public AbstractGoToCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public CommandState GetCommandState(TCommandArgs args)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var findUsagesService = document?.GetLanguageService<TLanguageService>();
            return findUsagesService != null
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, _scopeDescription))
            {
                var subjectBuffer = args.SubjectBuffer;
                if (!subjectBuffer.TryGetWorkspace(out var workspace))
                {
                    return false;
                }

                var service = workspace.Services.GetLanguageServices(args.SubjectBuffer)?.GetService<TLanguageService>();
                if (service != null)
                {
                    var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (caret.HasValue)
                    {
                        var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                            context.OperationContext, _threadingContext);
                        if (document != null)
                        {
                            ExecuteCommand(document, caret.Value, service, context);
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private void ExecuteCommand(
           Document document, int caretPosition,
           TLanguageService service,
           CommandExecutionContext context)
        {
            if (service != null)
            {
                // We have all the cheap stuff, so let's do expensive stuff now
                string messageToShow = null;

                var userCancellationToken = context.OperationContext.UserCancellationToken;
                using (Logger.LogBlock(_functionId, KeyValueLogMessage.Create(LogType.UserAction), userCancellationToken))
                {
                    StreamingGoTo(
                        document, caretPosition,
                        service, _streamingPresenter,
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

        private void StreamingGoTo(
            Document document, int caretPosition,
            TLanguageService service,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            out string messageToShow)
        {
            // We create our own context object, simply to capture all the definitions reported by 
            // the individual TLanguageService.  Once we get the results back we'll then decide 
            // what to do with them.  If we get only a single result back, then we'll just go 
            // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
            var context = new SimpleFindUsagesContext(cancellationToken);
            FindAction(service, document, caretPosition, context).Wait(cancellationToken);

            // If FindAction reported a message, then just stop and show that 
            // message to the user.
            messageToShow = context.Message;
            if (messageToShow != null)
            {
                return;
            }

            var definitionItems = context.GetDefinitions();

            streamingPresenter.TryNavigateToOrPresentItemsAsync(
                document.Project.Solution.Workspace, context.SearchTitle, definitionItems).Wait(cancellationToken);
        }
    }
}
