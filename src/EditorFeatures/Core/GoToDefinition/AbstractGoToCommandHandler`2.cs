// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GoToDefinition;

internal abstract class AbstractGoToCommandHandler<TLanguageService, TCommandArgs> : ICommandHandler<TCommandArgs>
    where TLanguageService : class, ILanguageService
    where TCommandArgs : EditorCommandArgs
{
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingPresenter;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly IAsynchronousOperationListener _listener;
    private readonly IGlobalOptionService _globalOptions;

    /// <summary>
    /// The current go-to command that is in progress.  Tracked so that if we issue multiple find-impl commands that
    /// they properly run after each other.  This is necessary so none of them accidentally stomp on one that is still
    /// in progress and is interacting with the UI.  Only valid to read or write to this on the UI thread.
    /// </summary>
    private Task _inProgressCommand = Task.CompletedTask;

    /// <summary>
    /// CancellationToken governing the current <see cref="_inProgressCommand"/>.  Only valid to read or write to this
    /// on the UI thread.
    /// </summary>
    /// <remarks>
    /// Cancellation is complicated with this feature.  There are two things that can cause us to cancel.  The first is
    /// if the user kicks off another actual go-to-impl command.  In that case, we just attempt to cancel the prior
    /// command (if it is still running), then wait for it to complete, then run our command.  The second is if we have
    /// switched over to the streaming presenter and then the user starts some other command (like FAR) that takes over
    /// the presenter.  In that case, the presenter will notify us that it has be re-purposed and we will also cancel
    /// this source.
    /// </remarks>
    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// This hook allows for stabilizing the asynchronous nature of this command handler for integration testing.
    /// </summary>
    private Func<CancellationToken, Task>? _delayHook;

    public AbstractGoToCommandHandler(
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingPresenter,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        IAsynchronousOperationListener listener,
        IGlobalOptionService globalOptions)
    {
        _threadingContext = threadingContext;
        _streamingPresenter = streamingPresenter;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _listener = listener;
        _globalOptions = globalOptions;
    }

    public abstract string DisplayName { get; }
    protected abstract string ScopeDescription { get; }
    protected abstract FunctionId FunctionId { get; }

    protected abstract Task FindActionAsync(IFindUsagesContext context, Document document, int caretPosition, CancellationToken cancellationToken);

    public CommandState GetCommandState(TCommandArgs args)
    {
        var document = args.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
        return document?.GetLanguageService<TLanguageService>() is null
            ? CommandState.Unspecified
            : CommandState.Available;
    }

    public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
    {
        // Should only be called on the UI thread.
        Contract.ThrowIfFalse(_threadingContext.HasMainThread);

        var subjectBuffer = args.SubjectBuffer;
        var caret = args.TextView.GetCaretPoint(subjectBuffer);
        if (!caret.HasValue)
            return false;

        var document = subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
        if (document == null)
            return false;

        var service = document.GetLanguageService<TLanguageService>();
        if (service == null)
            return false;

        // cancel any prior find-refs that might be in progress.
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new();

        // we're going to return immediately from ExecuteCommand and kick off our own async work to invoke the
        // operation. Once this returns, the editor will close the threaded wait dialog it created.
        _inProgressCommand = ExecuteCommandAsync(document, caret.Value.Position, _cancellationTokenSource);
        return true;
    }

    private async Task ExecuteCommandAsync(
        Document document,
        int position,
        CancellationTokenSource cancellationTokenSource)
    {
        // This is a fire-and-forget method (nothing guarantees observing it).  As such, we have to handle cancellation
        // and failure ourselves.
        try
        {
            // Should only be called on the UI thread.
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            // Make an tracking token so that integration tests can wait until we're complete.
            using var token = _listener.BeginAsyncOperation($"{GetType().Name}.{nameof(ExecuteCommandAsync)}");

            // Only start running once the previous command has finished.  That way we don't have results from both
            // potentially interleaving with each other.  Note: this should ideally always be fast as long as the prior
            // task respects cancellation.
            //
            // Note: we just need to make sure we run after that prior command finishes.  We do not want to propagate
            // any failures from it.  Technically this should not be possible as it should be inside this same
            // try/catch. however this code wants to be very resilient to any prior mistakes infecting later operations.
            await _inProgressCommand.NoThrowAwaitable(captureContext: false);
            await ExecuteCommandWorkerAsync(document, position, cancellationTokenSource).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }
    }

    private async Task ExecuteCommandWorkerAsync(
        Document document,
        int position,
        CancellationTokenSource cancellationTokenSource)
    {
        // Switch to the BG immediately so we can keep as much work off the UI thread.
        await TaskScheduler.Default;

        // We kick off the work to find the impl/base in the background.  If we get the results for it within 1.5
        // seconds, we then either navigate directly to it (in the case of one result), or we show all the results in
        // the presenter (in the case of multiple).
        //
        // However, if the results don't come back in 1.5 seconds, we just pop open the presenter and continue the
        // search there.  That way the user is not blocked and can go do other work if they want.

        // We create our own context object, simply to capture all the definitions reported by the individual
        // TLanguageService.  Once we get the results back we'll then decide what to do with them.  If we get only a
        // single result back, then we'll just go directly to it.  Otherwise, we'll present the results in the
        // IStreamingFindUsagesPresenter.
        var findContext = new BufferedFindUsagesContext(_globalOptions);

        var cancellationToken = cancellationTokenSource.Token;
        var delayTask = DelayAsync(cancellationToken);
        var findTask = Task.Run(() => FindResultsAsync(findContext, document, position, cancellationToken), cancellationToken);

        var firstFinishedTask = await Task.WhenAny(delayTask, findTask).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
            // we bailed out because another command was issued.  Immediately stop everything we're doing and return
            // back so the next operation can run.
            return;

        if (firstFinishedTask == findTask)
        {
            // We completed the search within 1.5 seconds.  If we had at least one result then Navigate to it directly
            // (if there is just one) or present them all if there are many.
            var definitions = await findContext.GetDefinitionsAsync(cancellationToken).ConfigureAwait(false);
            if (definitions.Length > 0)
            {
                var title = await findContext.GetSearchTitleAsync(cancellationToken).ConfigureAwait(false);
                await _streamingPresenter.TryNavigateToOrPresentItemsAsync(
                    _threadingContext,
                    document.Project.Solution.Workspace,
                    title ?? DisplayName,
                    definitions,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        // We either got no results, or 1.5 has passed and we didn't figure out the symbols to navigate to or
        // present.  So pop up the presenter to show the user that we're involved in a longer search, without
        // blocking them.
        await PresentResultsInStreamingPresenterAsync(findContext, findTask, cancellationTokenSource).ConfigureAwait(false);
    }

    private Task DelayAsync(CancellationToken cancellationToken)
    {
        if (_delayHook is { } delayHook)
        {
            return delayHook(cancellationToken);
        }

        return Task.Delay(TaggerDelay.OnIdle.ComputeTimeDelay(), cancellationToken);
    }

    private async Task PresentResultsInStreamingPresenterAsync(
        BufferedFindUsagesContext findContext,
        Task findTask,
        CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var (presenterContext, presenterCancellationToken) = _streamingPresenter.StartSearch(DisplayName, supportsReferences: false);

        try
        {
            await TaskScheduler.Default;

            // Now, tell our find-context (which has been collecting intermediary results) to swap over to using the
            // actual presenter context.  It will push all results it's been collecting into that, and from that
            // point onwards will just forward any new results directly to the presenter.
            await findContext.AttachToStreamingPresenterAsync(presenterContext, cancellationToken).ConfigureAwait(false);

            // Hook up the presenter's cancellation token to our overall governing cancellation token.  In other
            // words, if something else decides to present in the presenter (like a find-refs call) we'll hear about
            // that and can cancel all our work.
            presenterCancellationToken.Register(() => cancellationTokenSource.Cancel());

            // now actually wait for the find work to be done.
            await findTask.ConfigureAwait(false);
        }
        finally
        {
            // Ensure that once we pop up the presenter, we always make sure to force it to the completed stage in
            // case some other find operation happens (either through this handler or another handler using the
            // presenter) and we don't actually finish the search.
            await presenterContext.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FindResultsAsync(
        IFindUsagesContext findContext, Document document, int position, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
        {
            await findContext.SetSearchTitleAsync(DisplayName, cancellationToken).ConfigureAwait(false);

            // Let the user know in the FAR window if results may be inaccurate because this is running prior to the 
            // solution being fully loaded.
            var service = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await service.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (!isFullyLoaded)
            {
                await findContext.ReportInformationalMessageAsync(
                    EditorFeaturesResources.The_results_may_be_incomplete_due_to_the_solution_still_loading_projects, cancellationToken).ConfigureAwait(false);
            }

            // We were able to find the doc prior to loading the workspace (or else we would not have the service).
            // So we better be able to find it afterwards.
            await FindActionAsync(findContext, document, position, cancellationToken).ConfigureAwait(false);
        }
    }

    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly AbstractGoToCommandHandler<TLanguageService, TCommandArgs> _instance;

        internal TestAccessor(AbstractGoToCommandHandler<TLanguageService, TCommandArgs> instance)
            => _instance = instance;

        internal ref Func<CancellationToken, Task>? DelayHook
            => ref _instance._delayHook;
    }
}
