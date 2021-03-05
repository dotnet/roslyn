// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    /// <summary>
    /// Tagger event that fires once the compilation is ready for a particular project.  Used to trigger a
    /// reclassification pass as classification may show either cached classifications (from a previous session), or
    /// incomplete classifications due to frozen-partial compilations being used.
    /// </summary>
    internal class CompilationAvailableTaggerEventSource : ITaggerEventSource
    {
        private readonly object _gate = new();
        private readonly ITextBuffer _subjectBuffer;
        private readonly TaggerDelay _delay;
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// Other event sources we're composing over.  If they fire, we should reclassify.  However, after they fire, we
        /// should also refire an event once we get the next full compilation ready.
        /// </summary>
        private readonly ITaggerEventSource[] _eventSources;

        private bool _disconnected;

        private readonly AsynchronousSerialWorkQueue _workQueue;
        /// <summary>
        /// Outstanding task we issue when we get an even 
        /// </summary>
        private Task<Compilation?> _getCompilationTask = Task.FromResult<Compilation?>(null);
        private CancellationTokenSource _tokenSource = new();

        public CompilationAvailableTaggerEventSource(
            ITextBuffer subjectBuffer,
            TaggerDelay delay,
            IAsynchronousOperationListener asyncListener,
            params ITaggerEventSource[] eventSources)
        {
            _subjectBuffer = subjectBuffer;
            _delay = delay;
            _asyncListener = asyncListener;
            _eventSources = eventSources;
        }

        public event EventHandler<TaggerEventArgs>? Changed;

        public void Connect()
        {
            _eventSources.Do(p =>
            {
                p.Connect();
                p.Changed += OnEventSourceChanged;
            });
        }

        public void Disconnect()
        {
            _disconnected = true;
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _eventSources.Do(p =>
            {
                p.Changed -= OnEventSourceChanged;
                p.Disconnect();
            });
        }

        public event EventHandler UIUpdatesPaused
        {
            add { _eventSources.Do(p => p.UIUpdatesPaused += value); }
            remove { _eventSources.Do(p => p.UIUpdatesPaused -= value); }
        }

        public event EventHandler UIUpdatesResumed
        {
            add { _eventSources.Do(p => p.UIUpdatesResumed += value); }
            remove { _eventSources.Do(p => p.UIUpdatesResumed -= value); }
        }

        private void OnEventSourceChanged(object? sender, TaggerEventArgs args)
        {
            // First, notify anyone listening to us that something definitely changed.
            this.Changed?.Invoke(this, args);

            if (_disconnected)
                return;

            var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            if (!document.SupportsSemanticModel)
                return;

            // Now, attempt to cancel any existing work to get the compilation for this project, and kick off a new
            // piece of work in the future to do so.
            lock (_gate)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
                var cancellationToken = _tokenSource.Token;

                // Keep track that we're doing async work in tests.
                var asyncToken = _asyncListener.BeginAsyncOperation(nameof(OnEventSourceChanged));
                _getCompilationTask = _getCompilationTask.ContinueWithAfterDelay(
                    _ => GetCompilationAndFireChangedEventAsync(document, asyncToken, cancellationToken),
                    cancellationToken,
                    500,
                    // Ensure we run the callback outside of the lock.
                    TaskContinuationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.Default).Unwrap();
            }
        }

        private async Task<Compilation?> GetCompilationAndFireChangedEventAsync(
            Document document, IAsyncToken asyncToken, CancellationToken cancellationToken)
        {
            using (asyncToken)
            {
                // Wait for the actual compilation to be available.
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                // And then fire an event so we know to reclassify.
                this.Changed?.Invoke(this, new TaggerEventArgs(_delay));
                return compilation;
            }|
        }
    }
}
