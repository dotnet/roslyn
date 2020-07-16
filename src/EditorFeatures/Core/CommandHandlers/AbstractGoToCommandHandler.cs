// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public AbstractGoToCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public abstract string DisplayName { get; }
        protected abstract string ScopeDescription { get; }
        protected abstract FunctionId FunctionId { get; }
        protected abstract Task FindActionAsync(TLanguageService service, Document document, int caretPosition, IFindUsagesContext context);

        public CommandState GetCommandState(TCommandArgs args)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var findUsagesService = document?.GetLanguageService<TLanguageService>();
            return findUsagesService != null
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, ScopeDescription))
            {
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (!caret.HasValue)
                    return false;

                var subjectBuffer = args.SubjectBuffer;
                if (!subjectBuffer.TryGetWorkspace(out var workspace))
                    return false;

                var service = workspace.Services.GetLanguageServices(args.SubjectBuffer)?.GetService<TLanguageService>();
                if (service == null)
                    return false;

                var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document == null)
                    return false;

                ExecuteCommand(document, caret.Value, service, context);
                return true;
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
                using (Logger.LogBlock(FunctionId, KeyValueLogMessage.Create(LogType.UserAction), userCancellationToken))
                {
                    messageToShow = _threadingContext.JoinableTaskFactory.Run(() =>
                        NavigateToOrPresentResultsAsync(document, caretPosition, service, userCancellationToken));
                }

                if (messageToShow != null)
                {
                    // We are about to show a modal UI dialog so we should take over the command execution
                    // wait context. That means the command system won't attempt to show its own wait dialog 
                    // and also will take it into consideration when measuring command handling duration.
                    context.OperationContext.TakeOwnership();
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(
                        message: messageToShow,
                        title: DisplayName,
                        severity: NotificationSeverity.Information);
                }
            }
        }

        private async Task<string> NavigateToOrPresentResultsAsync(
            Document document,
            int caretPosition,
            TLanguageService service,
            CancellationToken cancellationToken)
        {
            // We create our own context object, simply to capture all the definitions reported by 
            // the individual TLanguageService.  Once we get the results back we'll then decide 
            // what to do with them.  If we get only a single result back, then we'll just go 
            // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
            var context = new SimpleFindUsagesContext(cancellationToken);

            await FindActionAsync(service, document, caretPosition, context).ConfigureAwait(false);
            if (context.Message != null)
                return context.Message;

            await _streamingPresenter.TryNavigateToOrPresentItemsAsync(
                _threadingContext, document.Project.Solution.Workspace, context.SearchTitle, context.GetDefinitions()).ConfigureAwait(false);
            return null;
        }
    }
}
