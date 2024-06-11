// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal sealed class TrackingSpanIntrospector(ITextSnapshot snapshot) : IIntervalIntrospector<ITrackingSpan>
    {
        private readonly ITextSnapshot _snapshot = snapshot;

        public int GetStart(ITrackingSpan value)
            => value.GetStartPoint(_snapshot);

        public int GetLength(ITrackingSpan value)
            => value.GetSpan(_snapshot).Length;
    }
}
