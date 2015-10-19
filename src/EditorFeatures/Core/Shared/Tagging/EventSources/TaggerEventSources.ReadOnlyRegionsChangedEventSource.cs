// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class ReadOnlyRegionsChangedEventSource : AbstractTaggerEventSource
        {
            private readonly ITextBuffer _subjectBuffer;

            public ReadOnlyRegionsChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(delay)
            {
                Contract.ThrowIfNull(subjectBuffer);

                _subjectBuffer = subjectBuffer;
            }

            public override void Connect()
            {
                _subjectBuffer.ReadOnlyRegionsChanged += OnReadOnlyRegionsChanged;
            }

            public override void Disconnect()
            {
                _subjectBuffer.ReadOnlyRegionsChanged -= OnReadOnlyRegionsChanged;
            }

            private void OnReadOnlyRegionsChanged(object sender, SnapshotSpanEventArgs e)
            {
                this.RaiseChanged();
            }
        }
    }
}
