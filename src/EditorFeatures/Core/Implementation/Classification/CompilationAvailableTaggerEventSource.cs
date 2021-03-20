// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private readonly ITextBuffer _subjectBuffer;
        private readonly TaggerDelay _delay;
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// Other event sources we're composing over.  If they fire, we should reclassify.  However, after they fire, we
        /// should also refire an event once we get the next full compilation ready.
        /// </summary>
        private readonly ITaggerEventSource _underlyingSource;

        /// <summary>
        /// Queue of work to go compute the compilations.
        /// </summary>
        private readonly AsynchronousSerialWorkQueue _workQueue;

        public CompilationAvailableTaggerEventSource(
            ITextBuffer subjectBuffer,
            TaggerDelay delay,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            params ITaggerEventSource[] eventSources)
        {
            _subjectBuffer = subjectBuffer;
            _delay = delay;
            _asyncListener = asyncListener;
            _underlyingSource = TaggerEventSources.Compose(eventSources);

            _workQueue = new AsynchronousSerialWorkQueue(threadingContext, asyncListener);
        }

        public event EventHandler<TaggerEventArgs>? Changed;

        public void Connect()
        {
            // When we are connected to, connect to all our underlying sources and have them notify us when they've changed.
            _underlyingSource.Connect();
            _underlyingSource.Changed += OnEventSourceChanged;
        }

        public void Disconnect()
        {
            _underlyingSource.Changed -= OnEventSourceChanged;
            _underlyingSource.Disconnect();
            _workQueue.CancelCurrentWork();
        }

        public event EventHandler UIUpdatesPaused
        {
            add { _underlyingSource.UIUpdatesPaused += value; }
            remove { _underlyingSource.UIUpdatesPaused -= value; }
        }

        public event EventHandler UIUpdatesResumed
        {
            add { _underlyingSource.UIUpdatesResumed += value; }
            remove { _underlyingSource.UIUpdatesResumed -= value; }
        }

        private void OnEventSourceChanged(object? sender, TaggerEventArgs args)
        {
            // First, notify anyone listening to us that something definitely changed.
            this.Changed?.Invoke(this, args);

            var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            if (!document.SupportsSemanticModel)
                return;

            // Now, attempt to cancel any existing work to get the compilation for this project, and kick off a new
            // piece of work in the future to do so.  Do this after a delay so we can appropriately throttle ourselves
            // if we hear about a flurry of notifications.
            _workQueue.CancelCurrentWork();
            _workQueue.EnqueueBackgroundTask(async c =>
                {
                    await document.Project.GetCompilationAsync(c).ConfigureAwait(false);
                    this.Changed?.Invoke(this, new TaggerEventArgs(_delay));
                },
                $"{nameof(CompilationAvailableTaggerEventSource)}.{nameof(OnEventSourceChanged)}",
                500,
                _workQueue.CancellationToken);
        }
    }
}
