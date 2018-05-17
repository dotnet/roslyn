// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal sealed class TrackingSpanIntrospector : IIntervalIntrospector<ITrackingSpan>
    {
        private readonly ITextSnapshot _snapshot;

        public TrackingSpanIntrospector(ITextSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int GetStart(ITrackingSpan value)
        {
            return value.GetStartPoint(_snapshot);
        }

        public int GetLength(ITrackingSpan value)
        {
            return value.GetSpan(_snapshot).Length;
        }
    }
}
