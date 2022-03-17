// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class CaretPositionChangedEventSource : AbstractTaggerEventSource
        {
            private readonly ITextView _textView;

            public CaretPositionChangedEventSource(ITextView textView, ITextBuffer subjectBuffer)
            {
                Contract.ThrowIfNull(textView);
                Contract.ThrowIfNull(subjectBuffer);

                _textView = textView;
            }

            public override void Connect()
                => _textView.Caret.PositionChanged += OnCaretPositionChanged;

            public override void Disconnect()
                => _textView.Caret.PositionChanged -= OnCaretPositionChanged;

            private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
                => this.RaiseChanged();
        }
    }
}
