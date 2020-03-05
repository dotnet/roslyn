﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
