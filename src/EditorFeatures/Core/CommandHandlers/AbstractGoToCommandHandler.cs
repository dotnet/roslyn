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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractGoToCommandHandler<TLanguageService, TCommandArgs> : ICommandHandler<TCommandArgs>
        where TLanguageService : class, ILanguageService
        where TCommandArgs : EditorCommandArgs
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;
        private readonly IAsynchronousOperationListener _listener;

        public AbstractGoToCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            IAsynchronousOperationListener listener)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
            _listener = listener;
        }

        public abstract string DisplayName { get; }
        protected abstract string ScopeDescription { get; }
        protected abstract FunctionId FunctionId { get; }
        protected abstract Task FindActionAsync(TLanguageService service, Document document, int caretPosition, IFindUsagesContext context, CancellationToken cancellationToken);

        public CommandState GetCommandState(TCommandArgs args)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var findUsagesService = GetService(document);
                if (findUsagesService != null)
                    return CommandState.Available;
            }

            return CommandState.Unspecified;
        }

        protected abstract TLanguageService? GetService(Document document);

        public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
        {
            var subjectBuffer = args.SubjectBuffer;
            var caret = args.TextView.GetCaretPoint(subjectBuffer);
            if (!caret.HasValue)
                return false;

            var snapshot = subjectBuffer.CurrentSnapshot;

            var scope = context.OperationContext.AddScope(allowCancellation: true, ScopeDescription);
            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
            ExecuteCommandAsync(snapshot, caret.Value, context)
                .CompletesTrackingOperation(scope)
                .CompletesAsyncOperation(token);

            return true;
        }

        private async Task ExecuteCommandAsync(
            ITextSnapshot textSnapshot, int position, CommandExecutionContext context)
        {
            // Switch to the BG immediately so we can keep as much work off the UI thread.
            await TaskScheduler.Default;

            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            var service = GetService(document);
            if (service == null)
                return;

            document = await textSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context.OperationContext).ConfigureAwait(false);
            if (document == null)
                return;

            // We have all the cheap stuff, so let's do expensive stuff now
            string? messageToShow = null;

            var cancellationToken = context.OperationContext.UserCancellationToken;
            using (Logger.LogBlock(FunctionId, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                // We create our own context object, simply to capture all the definitions reported by 
                // the individual TLanguageService.  Once we get the results back we'll then decide 
                // what to do with them.  If we get only a single result back, then we'll just go 
                // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
                var findContext = new SimpleFindUsagesContext();

                await FindActionAsync(service, document, position, findContext, cancellationToken).ConfigureAwait(false);
                if (findContext.Message != null)
                {
                    // Find succeeded.  Show the results to the user and immediately return.
                    await _streamingPresenter.TryNavigateToOrPresentItemsAsync(
                        _threadingContext, document.Project.Solution.Workspace, findContext.SearchTitle, findContext.GetDefinitions(), cancellationToken).ConfigureAwait(false);
                    return;
                }

                // Find failed.  Pop up dialog telling the user why.
                messageToShow = findContext.Message;
            }

            if (messageToShow != null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                context.OperationContext.TakeOwnership();
                var notificationService = document.Project.Solution.Workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(messageToShow, title: DisplayName, NotificationSeverity.Information);
            }
        }
    }
}
