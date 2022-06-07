// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            public ReadOnlyRegionsChangedEventSource(ITextBuffer subjectBuffer)
            {
                Contract.ThrowIfNull(subjectBuffer);
                _subjectBuffer = subjectBuffer;
            }

            public override void Connect()
                => _subjectBuffer.ReadOnlyRegionsChanged += OnReadOnlyRegionsChanged;

            public override void Disconnect()
                => _subjectBuffer.ReadOnlyRegionsChanged -= OnReadOnlyRegionsChanged;

            private void OnReadOnlyRegionsChanged(object? sender, SnapshotSpanEventArgs e)
                => this.RaiseChanged();
        }
    }
}
