// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Manages server initiated work done progress reporting to the client.
/// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#serverInitiatedProgress
/// </summary>
class WorkDoneProgressManager(IClientLanguageServerManager clientLanguageServerManager, IInitializeManager initializeManager) : ILspService
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager = clientLanguageServerManager;
    private readonly IInitializeManager _initializeManager = initializeManager;

    /// <summary>
    /// Guards access to <see cref="_progressReporters"/>.
    /// While generally a single thread acts on a single <see cref="WorkDoneProgressReporter"/>,
    /// a single reporter may get concurrent requests to cancel while the server is disposing of it.
    /// Additionally multiple threads may create separate reporters concurrently.
    /// </summary>
    private readonly object _progressLock = new();

    /// <summary>
    /// Tracks active work done progress reporters by their token.
    /// Required so we can cancel them when the client requests us to.
    /// Guarded by <see cref="_progressLock"/> 
    /// </summary>
    /// <remarks>
    /// A singe entry is added once to the dictionary when a new work done progress session is initiated.
    /// Multiple threads may create new sessions concurrently.  Additionally, a single entry may have
    /// have a concurrent request from the client to cancel while the server is disposing of it.
    /// </remarks>
    private readonly Dictionary<string, WorkDoneProgressReporter> _progressReporters = [];

    /// <summary>
    /// Initiates a new work done progress reporting session with the client.
    /// This sends the initial `window/workDoneProgress/create` request and begin report to the client.
    /// In the case of server side cancellation, an end report will be sent automatically with a "Cancelled" message.
    /// On dispose of the <see cref="IWorkDoneProgressReporter"/>, an end report will be sent with the provided end message.
    /// </summary>
    /// <param name="serverCancellationToken">a cancellation token that signals when the server wants to cancel the operation</param>
    public async Task<IWorkDoneProgressReporter> CreateWorkDoneProgressAsync(
        bool reportProgressToClient,
        string title,
        string startMessage,
        string endMessage,
        bool clientCanCancel,
        CancellationToken serverCancellationToken)
    {
        var token = Guid.NewGuid().ToString();
        IWorkDoneProgressReporter reporter;

        // Only report progress to the client if both the client advertised support for progress reporting and the caller requested it.
        var reportProgress = reportProgressToClient && _initializeManager.GetClientCapabilities().Window?.WorkDoneProgress == true;

        if (reportProgress)
        {
            var clientReporter = new WorkDoneProgressReporter(token, endMessage, this, serverCancellationToken);
            await clientReporter.SendCreateRequestAsync().ConfigureAwait(false);

            // Tell the client to end the work done progress if the server cancels the request.
            // Note - no need to observe client cancellation - the client does not expect an end report when it cancels.
            serverCancellationToken.Register(() =>
            {
                clientReporter.TryReportEndAsync(LanguageServerProtocolResources.Cancelled).ReportNonFatalErrorAsync();
            });

            lock (_progressLock)
            {
                _progressReporters[token] = clientReporter;
            }

            clientReporter.Report(new WorkDoneProgressBegin()
            {
                Title = title,
                Message = startMessage,
                Cancellable = clientCanCancel,
                Percentage = 0,
            });

            return clientReporter;
        }
        else
        {
            reporter = new NoOpProgressReporter(serverCancellationToken);
        }

        return reporter;
    }

    public void CancelWorkDoneProgress(string token)
    {
        lock (_progressLock)
        {
            // We may be handling a client cancellation request after the server already completed and disposed of the progress.
            // Check that we still have a non-disposed reporter for this token.
            if (_progressReporters.TryGetValue(token, out var reporter))
            {
                reporter.CancelSource_NoLock();
            }
        }
    }

    private class WorkDoneProgressReporter : IWorkDoneProgressReporter
    {
        private int _ended = 0;

        private readonly WorkDoneProgressManager _manager;

        /// <summary>
        /// The token sent to the client identifying this work done progress session.
        /// </summary>
        private readonly string _token;

        /// <summary>
        /// The message to send to the client when the work done progress session ends.
        /// </summary>
        private readonly string _endMessage;

        private readonly CancellationTokenSource _linkedCancellationSource;

        public CancellationToken CancellationToken { get; }

        public WorkDoneProgressReporter(string token, string endMessage, WorkDoneProgressManager manager, CancellationToken serverCancellationToken)
        {
            _token = token;
            _endMessage = endMessage;
            _manager = manager;
            // Link the server cancellation token to the source handling client side cancellation.
            _linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
            CancellationToken = _linkedCancellationSource.Token;
        }

        public async Task SendCreateRequestAsync()
        {
            var workDoneParams = new WorkDoneProgressCreateParams()
            {
                Token = _token
            };

            CancellationToken.ThrowIfCancellationRequested();
            await _manager._clientLanguageServerManager.SendRequestAsync(Methods.WindowWorkDoneProgressCreateName, workDoneParams, CancellationToken).ConfigureAwait(false);
        }

        public void Report(WorkDoneProgress progress)
        {
            ReportProgressAsync(progress, CancellationToken).ReportNonFatalErrorUnlessCancelledAsync(CancellationToken);
        }

        private async Task ReportProgressAsync(WorkDoneProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _manager._clientLanguageServerManager.SendNotificationAsync(Methods.ProgressNotificationName, new ProgressReportType(_token, progress), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Expected to be called under <see cref="_progressLock"/> 
        /// </summary>
        public void CancelSource_NoLock()
        {
            _linkedCancellationSource.Cancel();
        }

        public async ValueTask DisposeAsync()
        {
            // Take the lock here to ensure we don't run this concurrently with Cancel.
            var isDisposedByCancellation = false;
            lock (_manager._progressLock)
            {
                isDisposedByCancellation = _linkedCancellationSource.IsCancellationRequested;
                _manager._progressReporters.Remove(_token);
                _linkedCancellationSource.Dispose();
            }

            // Do not send an end report if the reporter was disposed due to cancellation.
            // If the client cancelled, the client is not expecting an end report.
            // If the server cancelled, we have/will send a report in the cancellation callback registered in CreateWorkDoneProgressAsync.
            if (!isDisposedByCancellation)
            {
                await TryReportEndAsync(_endMessage).ConfigureAwait(false);
            }
        }

        public async Task TryReportEndAsync(string message)
        {
            // There is an inherent race in that the server can complete while we get a cancellation from another source.
            // We'll just respond based on whatever we see first.
            if (Interlocked.CompareExchange(ref _ended, 1, 0) != 0)
                return;

            try
            {
                await ReportProgressAsync(new WorkDoneProgressEnd()
                {
                    Message = message
                }, CancellationToken.None /* do not observe cancellation as this may be a report triggered by cancellation. */).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
            {
                // It is entirely possible that we're shutting down and the connection is lost while we're trying to send a notification.
                // These are safely ignored as there is no client to recieve the notification.
            }
        }

        private record struct ProgressReportType(
            [property: JsonPropertyName("token")] string Token,
            [property: JsonPropertyName("value")] WorkDoneProgress Value);
    }

    private record struct NoOpProgressReporter(CancellationToken cancellationToken) : IWorkDoneProgressReporter
    {
        public readonly CancellationToken CancellationToken => cancellationToken;
        public readonly ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public readonly void Report(WorkDoneProgress value)
        {
        }
    }
}
