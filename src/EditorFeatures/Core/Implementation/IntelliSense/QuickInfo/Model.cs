// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class Model
    {
        public ITextVersion TextVersion { get; }
        public QuickInfoItem Item { get; }
        public bool TrackMouse { get; }

        public Model(
            ITextVersion textVersion,
            QuickInfoItem item,
            bool trackMouse)
        {
            Contract.ThrowIfNull(item);

            this.TextVersion = textVersion;
            this.Item = item;
            this.TrackMouse = trackMouse;
        }

        internal SnapshotSpan GetCurrentSpanInSnapshot(TextSpan originalSpan, ITextSnapshot textSnapshot)
        {
            var trackingSpan = this.TextVersion.CreateTrackingSpan(originalSpan.ToSpan(), SpanTrackingMode.EdgeInclusive);
            return trackingSpan.GetSpan(textSnapshot);
        }
    }
}
