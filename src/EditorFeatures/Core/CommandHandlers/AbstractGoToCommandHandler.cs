// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractGoToCommandHandler<TLanguageService, TCommandArgs> : ICommandHandler<TCommandArgs>
        where TLanguageService : class, ILanguageService
        where TCommandArgs : EditorCommandArgs
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly IAsynchronousOperationListener _listener;

        public AbstractGoToCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IAsynchronousOperationListener listener)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _listener = listener;
        }

        public abstract string DisplayName { get; }
        protected abstract string ScopeDescription { get; }
        protected abstract FunctionId FunctionId { get; }

        protected abstract Task FindActionAsync(TLanguageService service, Document document, int caretPosition, IFindUsagesContext context, CancellationToken cancellationToken);

        private static TLanguageService? GetService(ITextBuffer buffer)
        {
            var document = buffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            return document?.GetLanguageService<TLanguageService>();
        }

        public CommandState GetCommandState(TCommandArgs args)
        {
            var service = AbstractGoToCommandHandler<TLanguageService, TCommandArgs>.GetService(args.SubjectBuffer);
            return service != null
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
        {
            // Should only be called on the UI thread.
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            var subjectBuffer = args.SubjectBuffer;
            var caret = args.TextView.GetCaretPoint(subjectBuffer);
            if (!caret.HasValue)
                return false;

            var service = AbstractGoToCommandHandler<TLanguageService, TCommandArgs>.GetService(subjectBuffer);
            if (service == null)
                return false;

            var position = caret.Value.Position;
            var snapshot = subjectBuffer.CurrentSnapshot;

            // we're going to return immediately from ExecuteCommand and kick off our own async work to invoke the
            // operation. Once this returns, the editor will close the threaded wait dialog it created.
            // So we need to take ownership of it and start our own TWD instead to track this.
            context.OperationContext.TakeOwnership();
            _ = ExecuteCommandAsync(service, snapshot, position);
            return true;
        }

        private async Task ExecuteCommandAsync(TLanguageService service, ITextSnapshot snapshot, int position)
        {
            // Should only be called on the UI thread.
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
            try
            {
                // Create an async token to track this work for any integration tests that need to wait on this.
                // Then, create a threaded-wait-dialog to let the user know this work is happening and to allow
                // them to cancel it if they no longer care about the results.
                using var token = _listener.BeginAsyncOperation($"{this.GetType().Name}.{nameof(ExecuteCommandAsync)}");
                using var context = _uiThreadOperationExecutor.BeginExecute(
                    this.DisplayName, this.ScopeDescription, allowCancellation: true, showProgress: false);

                await this.ExecuteCommandAsync(service, snapshot, position, context).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        }

        private async Task ExecuteCommandAsync(
            TLanguageService service,
            ITextSnapshot textSnapshot,
            int position,
            IUIThreadOperationContext context)
        {
            // Switch to the BG immediately so we can keep as much work off the UI thread.
            await TaskScheduler.Default;

            var document = await textSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context).ConfigureAwait(false);
            if (document == null)
                return;

            // We have all the cheap stuff, so let's do expensive stuff now
            string? messageToShow = null;

            var cancellationToken = context.UserCancellationToken;
            using (Logger.LogBlock(FunctionId, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                // We create our own context object, simply to capture all the definitions reported by 
                // the individual TLanguageService.  Once we get the results back we'll then decide 
                // what to do with them.  If we get only a single result back, then we'll just go 
                // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
                var findContext = new SimpleFindUsagesContext();

                await FindActionAsync(service, document, position, findContext, cancellationToken).ConfigureAwait(false);
                if (findContext.Message == null)
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
                context.TakeOwnership();
                var notificationService = document.Project.Solution.Workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(messageToShow, title: DisplayName, NotificationSeverity.Information);
            }
        }
    }
}
