﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
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
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// Other event sources we're composing over.  If they fire, we should reclassify.  However, after they fire, we
        /// should also refire an event once we get the next full compilation ready.
        /// </summary>
        private readonly ITaggerEventSource _underlyingSource;

        /// <summary>
        /// Cancellation tokens controlling background computation of the compilation.
        /// </summary>
        private readonly ReferenceCountedDisposable<CancellationSeries> _cancellationSeries = new(new CancellationSeries());

        public CompilationAvailableTaggerEventSource(
            ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            params ITaggerEventSource[] eventSources)
        {
            _subjectBuffer = subjectBuffer;
            _asyncListener = asyncListener;
            _underlyingSource = TaggerEventSources.Compose(eventSources);
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
            _cancellationSeries.Dispose();
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

            using var cancellationSeries = _cancellationSeries.TryAddReference();
            if (cancellationSeries is null)
            {
                // Already in the process of disposing this instance
                return;
            }

            // Cancel any existing tasks that are computing the compilation and spawn a new one to compute
            // it and notify any listening clients.
            var cancellationToken = cancellationSeries.Target.CreateNext();

            var token = _asyncListener.BeginAsyncOperation(nameof(OnEventSourceChanged));
            var task = Task.Run(async () =>
            {
                // Support cancellation without throwing
                await _asyncListener.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).NoThrowAwaitable(captureContext: false);
                if (cancellationToken.IsCancellationRequested)
                    return;

                await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                this.Changed?.Invoke(this, new TaggerEventArgs());
            }, cancellationToken);
            task.CompletesAsyncOperation(token);
        }
    }
}
