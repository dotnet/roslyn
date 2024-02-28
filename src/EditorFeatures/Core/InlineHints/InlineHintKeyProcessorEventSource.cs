// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

internal partial class InlineHintsDataTaggerProvider
{
    private sealed class InlineHintKeyProcessorEventSource(IInlineHintKeyProcessor? inlineHintKeyProcessor) : AbstractTaggerEventSource
    {
        private readonly IInlineHintKeyProcessor? _inlineHintKeyProcessor = inlineHintKeyProcessor;

        public override void Connect()
        {
            if (_inlineHintKeyProcessor != null)
                _inlineHintKeyProcessor.StateChanged += this.RaiseChanged;
        }

        public override void Disconnect()
        {
            if (_inlineHintKeyProcessor != null)
                _inlineHintKeyProcessor.StateChanged -= this.RaiseChanged;
        }
    }
}
