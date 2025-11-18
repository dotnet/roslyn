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

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Manages server initiated work done progress reporting to the client.
/// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#serverInitiatedProgress
/// </summary>
class WorkDoneProgressManager(IClientLanguageServerManager clientLanguageServerManager) : ILspService
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager = clientLanguageServerManager;

    private class WorkDoneProgressReporter : IWorkDoneProgressReporter
    {
        private readonly WorkDoneProgressManager _manager;

        /// <summary>
        /// The token sent to the client identifying this work done progress session.
        /// </summary>
        private readonly string _token;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public WorkDoneProgressReporter(string token, WorkDoneProgressManager manager, CancellationToken serverCancellationToken)
        {
            _token = token;
            _manager = manager;
            // Link the server cancellation token to the source handling client side cancellation.
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);

            // Tell the client to end the work done progress if the server cancels the request.
            // This needs to not observe the linked cancellation token as it will already be cancelled.
            serverCancellationToken.Register(() =>
            {
                // the reporter is already cancelled (linked cancellation token) - but we need to ensure the client is notified of the server requested cancellation.
                // this report should not be cancellable so report with no cancellation token.
                ReportProgressAsync(new WorkDoneProgressEnd()
                {
                    Message = "Cancelled"
                }, CancellationToken.None).ReportNonFatalErrorAsync();
            });
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
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            // Take the lock here to ensure we don't run this concurrently with Cancel.
            lock (_manager._progressLock)
            {
                _manager._progressReporters.Remove(_token);
                _cancellationTokenSource.Dispose();
            }
        }

        private record struct ProgressReportType(
            [property: JsonPropertyName("token")] string Token,
            [property: JsonPropertyName("value")] WorkDoneProgress Value);
    }

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
    /// </summary>
    /// <remarks>
    /// A singe entry is added once to the dictionary when a new work done progress session is initiated.
    /// Multiple threads may create new sessions concurrently.  Additionally, a single entry may have
    /// have a concurrent request from the client to cancel while the server is disposing of it.
    /// </remarks>
    private readonly Dictionary<string, WorkDoneProgressReporter> _progressReporters = [];

    /// <summary>
    /// Initiates a new work done progress reporting session with the client.
    /// This sends the initial `window/workDoneProgress/create` request, but callers are responsible for sending the
    /// begin and end reports.
    /// In the case of server side cancellation, an end report will be sent automatically with a "Cancelled" message.
    /// </summary>
    /// <param name="serverCancellationToken">a cancellation token that signals when the server wants to cancel the operation</param>
    public async Task<IWorkDoneProgressReporter> CreateWorkDoneProgressAsync(bool reportProgressToClient, CancellationToken serverCancellationToken)
    {
        var token = Guid.NewGuid().ToString();
        IWorkDoneProgressReporter reporter;
        if (reportProgressToClient)
        {
            var clientReporter = new WorkDoneProgressReporter(token, this, serverCancellationToken);
            await clientReporter.SendCreateRequestAsync().ConfigureAwait(false);
            lock (_progressLock)
            {
                _progressReporters[token] = clientReporter;
            }

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

    private record struct NoOpProgressReporter(CancellationToken cancellationToken) : IWorkDoneProgressReporter
    {
        public readonly CancellationToken CancellationToken => cancellationToken;
        public readonly void Dispose()
        {
        }

        public readonly void Report(WorkDoneProgress value)
        {
        }
    }
}
