// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private sealed class TextChangedEventSource : AbstractTaggerEventSource
    {
        private readonly ITextBuffer2 _subjectBuffer;

        public TextChangedEventSource(ITextBuffer2 subjectBuffer)
        {
            Contract.ThrowIfNull(subjectBuffer);
            _subjectBuffer = subjectBuffer;
        }

        public override void Connect()
            => _subjectBuffer.ChangedOnBackground += OnTextBufferChanged;

        public override void Disconnect()
            => _subjectBuffer.ChangedOnBackground -= OnTextBufferChanged;

        private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
                return;

            this.RaiseChanged();
        }
    }
}
