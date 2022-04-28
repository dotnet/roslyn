// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Tagger event that fires once the compilation is available in the remote OOP process for a particular project.
    /// Used to trigger things such as:
    /// <list type="bullet">
    /// <item>reclassification pass as classification may show either cached classifications (from a  previous session),
    /// or incomplete classifications due to frozen-partial compilations being used.</item>
    /// <item>recomputation of navigation bar items due to frozen-partial compilations being used.</item>
    /// <item>recomputation of inheritance margin items due to frozen-partial compilations being used.</item>
    /// </list>
    /// </summary>
    internal sealed class CompilationAvailableTaggerEventSource : ITaggerEventSource
    {
        private readonly ITextBuffer _subjectBuffer;

        /// <summary>
        /// Other event sources we're composing over.  If they fire, we should reclassify.  However, after they fire, we
        /// should also refire an event once we get the next full compilation ready.
        /// </summary>
        private readonly ITaggerEventSource _underlyingSource;

        private readonly CompilationAvailableEventSource _eventSource;

        private readonly Action _onCompilationAvailable;

        public CompilationAvailableTaggerEventSource(
            ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            params ITaggerEventSource[] eventSources)
        {
            _subjectBuffer = subjectBuffer;
            _eventSource = new CompilationAvailableEventSource(asyncListener);
            _underlyingSource = TaggerEventSources.Compose(eventSources);
            _onCompilationAvailable = () => this.Changed?.Invoke(this, new TaggerEventArgs());
        }

        public event EventHandler<TaggerEventArgs>? Changed;

        public void Connect()
        {
            // When we are connected to, connect to all our underlying sources and have them notify us when they've changed.
            _underlyingSource.Connect();
            _underlyingSource.Changed += OnUnderlyingSourceChanged;
        }

        public void Disconnect()
        {
            _underlyingSource.Changed -= OnUnderlyingSourceChanged;
            _underlyingSource.Disconnect();
            _eventSource.Dispose();
        }

        public void Pause()
            => _underlyingSource.Pause();

        public void Resume()
            => _underlyingSource.Resume();

        private void OnUnderlyingSourceChanged(object? sender, TaggerEventArgs args)
        {
            // First, notify anyone listening to us that something definitely changed.
            this.Changed?.Invoke(this, args);

            var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            _eventSource.EnsureCompilationAvailability(document.Project, _onCompilationAvailable);
        }
    }
}
