// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class TextChangedEventSource : AbstractTaggerEventSource
        {
            private readonly ITextBuffer2 _subjectBuffer;

            public TextChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(delay)
            {
                Contract.ThrowIfNull(subjectBuffer);

                _subjectBuffer = (ITextBuffer2)subjectBuffer;
            }

            public override void Connect()
            {
                _subjectBuffer.ChangedAsync += OnTextBufferChanged;
            }

            public override void Disconnect()
            {
                _subjectBuffer.ChangedAsync -= OnTextBufferChanged;
            }

            private Task OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                if (e.Changes.Count != 0)
                {
                    this.RaiseChanged();
                }

                return Task.CompletedTask;
            }
        }
    }
}
