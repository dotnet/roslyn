// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class TextChangedEventSource : AbstractTaggerEventSource
        {
            private readonly ITextBuffer _subjectBuffer;

            public TextChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(delay)
            {
                Contract.ThrowIfNull(subjectBuffer);

                _subjectBuffer = subjectBuffer;
            }

            public override void Connect()
            {
                _subjectBuffer.Changed += OnTextBufferChanged;
            }

            public override void Disconnect()
            {
                _subjectBuffer.Changed -= OnTextBufferChanged;
            }

            private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                if (e.Changes.Count == 0)
                {
                    return;
                }

                this.RaiseChanged();
            }
        }
    }
}
