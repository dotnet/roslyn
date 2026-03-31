// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an outgoing call (callee) from a call hierarchy item.
/// </summary>
internal sealed class CallHierarchyOutgoingCall
{
    /// <summary>
    /// The item that is being called by the source item.
    /// </summary>
    public CallHierarchyItem To { get; }

    /// <summary>
    /// The locations within the source item where the calls occur.
    /// Each location represents a call site (DocumentId + TextSpan).
    /// </summary>
    public ImmutableArray<(DocumentId DocumentId, TextSpan Span)> FromSpans { get; }

    public CallHierarchyOutgoingCall(
        CallHierarchyItem to,
        ImmutableArray<(DocumentId DocumentId, TextSpan Span)> fromSpans)
    {
        To = to;
        FromSpans = fromSpans;
    }
}
