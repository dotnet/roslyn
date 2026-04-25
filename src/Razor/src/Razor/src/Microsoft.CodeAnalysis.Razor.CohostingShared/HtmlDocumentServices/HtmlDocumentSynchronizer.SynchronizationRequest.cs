// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed partial class HtmlDocumentSynchronizer
{
    private class SynchronizationRequest(RazorDocumentVersion requestedVersion) : IDisposable
    {
        private readonly RazorDocumentVersion _requestedVersion = requestedVersion;
        private readonly TaskCompletionSource<SynchronizationResult> _tcs = new();
        private CancellationTokenSource? _cts;

        public Task<SynchronizationResult> Task => _tcs.Task;

        public RazorDocumentVersion RequestedVersion => _requestedVersion;

        internal static SynchronizationRequest CreateAndStart(TextDocument document, RazorDocumentVersion requestedVersion, Func<TextDocument, RazorDocumentVersion, CancellationToken, Task<SynchronizationResult>> syncFunction, CancellationToken cancellationToken)
        {
            var request = new SynchronizationRequest(requestedVersion);
            request.Start(document, syncFunction, cancellationToken);
            return request;
        }

        private void Start(TextDocument document, Func<TextDocument, RazorDocumentVersion, CancellationToken, Task<SynchronizationResult>> syncFunction, CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;
            _cts.Token.Register(Dispose);
            _ = syncFunction.Invoke(document, _requestedVersion, token).ContinueWith((t, state) =>
            {
                var tcs = (TaskCompletionSource<SynchronizationResult>)state.AssumeNotNull();
                if (t.IsCanceled)
                {
                    tcs.SetResult(default);
                }
                else if (t.Exception is { } ex)
                {
                    tcs.SetException(ex);
                }
                else
                {
                    tcs.SetResult(t.Result);
                }

                _cts?.Dispose();
                _cts = null;
            }, _tcs, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _tcs.TrySetResult(default);
        }
    }
}
