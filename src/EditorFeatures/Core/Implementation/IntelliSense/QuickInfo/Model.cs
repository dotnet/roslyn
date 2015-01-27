// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class Model
    {
        public ITextVersion TextVersion { get; private set; }
        public QuickInfoItem Item { get; private set; }
        public IQuickInfoProvider Provider { get; private set; }
        public bool TrackMouse { get; private set; }

        public Model(
            ITextVersion textVersion,
            QuickInfoItem item,
            IQuickInfoProvider provider,
            bool trackMouse)
        {
            Contract.ThrowIfNull(item);

            this.TextVersion = textVersion;
            this.Item = item;
            this.Provider = provider;
            this.TrackMouse = trackMouse;
        }

        internal SnapshotSpan GetCurrentSpanInSnapshot(TextSpan originalSpan, ITextSnapshot textSnapshot)
        {
            var trackingSpan = this.TextVersion.CreateTrackingSpan(originalSpan.ToSpan(), SpanTrackingMode.EdgeInclusive);
            return trackingSpan.GetSpan(textSnapshot);
        }
    }
}
