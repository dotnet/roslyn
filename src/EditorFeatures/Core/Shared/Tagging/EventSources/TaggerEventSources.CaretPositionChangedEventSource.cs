// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Tagging;
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

            public CaretPositionChangedEventSource(ITextView textView, ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(delay)
            {
                Contract.ThrowIfNull(textView);
                Contract.ThrowIfNull(subjectBuffer);

                _textView = textView;
            }

            public override void Connect()
            {
                _textView.Caret.PositionChanged += OnCaretPositionChanged;
            }

            public override void Disconnect()
            {
                _textView.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                this.RaiseChanged();
            }
        }
    }
}
