// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an incoming call (caller) to a call hierarchy item.
/// </summary>
internal sealed class CallHierarchyIncomingCall
{
    /// <summary>
    /// The item that is calling the target item.
    /// </summary>
    public CallHierarchyItem From { get; }

    /// <summary>
    /// The locations within the 'From' item where the calls occur.
    /// Each location represents a call site (DocumentId + TextSpan).
    /// </summary>
    public ImmutableArray<(DocumentId DocumentId, TextSpan Span)> FromSpans { get; }

    public CallHierarchyIncomingCall(
        CallHierarchyItem from,
        ImmutableArray<(DocumentId DocumentId, TextSpan Span)> fromSpans)
    {
        From = from;
        FromSpans = fromSpans;
    }
}
