// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class ReiteratedVersionSnapshotTracker
    {
        /// <summary>
        /// tracking text buffer
        /// </summary>
        private ITextBuffer _trackingBuffer;

        /// <summary>
        /// hold onto latest ReiteratedVersionNumber snapshot of a textbuffer
        /// there is a bug where many of our features just assume that if they wait, they will end up get the latest snapshot in some ways. 
        /// but, unfortunately that is actually not true. they will, at the end, get latest reiterated version snapshot but 
        /// not the latest version snapshot since we might have skipped/swallowed the latest snapshot since its content didn't change.
        /// this is especially unfortunate for features that want to move back and forth between source text and ITextSnapshot since holding
        /// on the latest snapshot won't guarantee that. so, in VS, we hold onto right latest snapshot in VS workspace so that all feature under it
        /// doesn't need to worry about it.
        /// this could be moved down to workspace_editor if it actually move up to editor layer. 
        /// but for now, I am putting it here. we can think about moving it down to workspace_editor later.
        /// </summary>
        private ITextSnapshot _latestReiteratedVersionSnapshot;

        public ReiteratedVersionSnapshotTracker(ITextBuffer buffer)
        {
            if (buffer != null)
            {
                StartTracking(buffer);
            }
        }

        public void StartTracking(ITextBuffer buffer)
        {
            // buffer has changed. stop tracking old buffer
            if (_trackingBuffer != null && buffer != _trackingBuffer)
            {
                _trackingBuffer.ChangedHighPriority -= OnTextBufferChanged;

                _trackingBuffer = null;
                _latestReiteratedVersionSnapshot = null;
            }

            // start tracking new buffer
            if (buffer != null && _latestReiteratedVersionSnapshot == null)
            {
                _latestReiteratedVersionSnapshot = buffer.CurrentSnapshot;
                _trackingBuffer = buffer;

                buffer.ChangedHighPriority += OnTextBufferChanged;
            }
        }

        public void StopTracking(ITextBuffer buffer)
        {
            if (_trackingBuffer == buffer && buffer != null && _latestReiteratedVersionSnapshot != null)
            {
                buffer.ChangedHighPriority -= OnTextBufferChanged;

                _trackingBuffer = null;
                _latestReiteratedVersionSnapshot = null;
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (sender is ITextBuffer)
            {
                var snapshot = _latestReiteratedVersionSnapshot;
                if (snapshot != null && snapshot.Version != null && e.AfterVersion != null &&
                    snapshot.Version.ReiteratedVersionNumber < e.AfterVersion.ReiteratedVersionNumber)
                {
                    _latestReiteratedVersionSnapshot = e.After;
                }
            }
        }
    }
}
