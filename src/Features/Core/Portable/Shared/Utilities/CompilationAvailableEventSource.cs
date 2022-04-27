// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class CompilationAvailableEventSource : IDisposable
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// Cancellation tokens controlling background computation of the compilation.
        /// </summary>
        private readonly ReferenceCountedDisposable<CancellationSeries> _cancellationSeries = new(new CancellationSeries());

        public event Action? OnCompilationAvailable;

        public CompilationAvailableEventSource(
            IAsynchronousOperationListener asyncListener)
        {
            _asyncListener = asyncListener;
        }

        public void Dispose()
            => _cancellationSeries.Dispose();

        public void EnsureCompilationAvailability(Project project)
        {
            if (project == null)
                return;

            if (!project.SupportsCompilation)
                return;

            using var cancellationSeries = _cancellationSeries.TryAddReference();
            if (cancellationSeries is null)
            {
                // Already in the process of disposing this instance
                return;
            }

            // Cancel any existing tasks that are computing the compilation and spawn a new one to compute
            // it and notify any listening clients.
            var cancellationToken = cancellationSeries.Target.CreateNext();

            var token = _asyncListener.BeginAsyncOperation(nameof(EnsureCompilationAvailability));
            var task = Task.Run(async () =>
            {
                // Support cancellation without throwing.
                //
                // We choose a long delay here so that we can avoid this work as long as the user is continually making
                // changes to their code.  During that time, features that use this are already kicking off fast work
                // with frozen-partial semantics and we'd like that to not have to contend with more expensive work
                // kicked off in OOP to compute full compilations.
                await _asyncListener.Delay(DelayTimeSpan.NonFocus, cancellationToken).NoThrowAwaitableInternal(captureContext: false);
                if (cancellationToken.IsCancellationRequested)
                    return;

                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.TryInvokeAsync<IRemoteCompilationAvailableService>(
                        project,
                        (service, solutionInfo, cancellationToken) => service.ComputeCompilationAsync(solutionInfo, project.Id, cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    if (!result)
                        return;
                }
                else
                {
                    // if we can't get the client, just compute the compilation locally and fire the event once we have it.
                    await CompilationAvailableHelpers.ComputeCompilationInCurrentProcessAsync(project, cancellationToken).ConfigureAwait(false);
                }

                // now that we know we have an full compilation, retrigger the tagger so it can show accurate results with the 
                // full information about this project.
                this.OnCompilationAvailable?.Invoke();
            }, cancellationToken);
            task.CompletesAsyncOperation(token);
        }
    }
}
