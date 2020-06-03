// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal readonly struct ActiveStatementTrackingSpan
    {
        public readonly ITrackingSpan Span;
        public readonly ActiveStatementFlags Flags;

        public ActiveStatementTrackingSpan(ITrackingSpan trackingSpan, ActiveStatementFlags flags)
        {
            Span = trackingSpan;
            Flags = flags;
        }

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf => (Flags & ActiveStatementFlags.IsLeafFrame) != 0;
    }
}
