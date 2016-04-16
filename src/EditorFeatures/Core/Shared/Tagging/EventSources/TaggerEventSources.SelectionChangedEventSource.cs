// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class SelectionChangedEventSource : AbstractTaggerEventSource
        {
            private readonly ITextView _textView;

            public SelectionChangedEventSource(ITextView textView, TaggerDelay delay)
                : base(delay)
            {
                _textView = textView;
            }

            public override void Connect()
            {
                _textView.Selection.SelectionChanged += OnSelectionChanged;
            }

            public override void Disconnect()
            {
                _textView.Selection.SelectionChanged -= OnSelectionChanged;
            }

            private void OnSelectionChanged(object sender, EventArgs args)
            {
                RaiseChanged();
            }
        }
    }
}
