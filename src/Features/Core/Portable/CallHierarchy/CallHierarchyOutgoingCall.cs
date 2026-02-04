// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an outgoing call from a symbol.
/// </summary>
internal sealed class CallHierarchyOutgoingCall
{
    public CallHierarchyOutgoingCall(
        CallHierarchyItem to,
        ImmutableArray<Location> callsites)
    {
        To = to;
        Callsites = callsites;
    }

    /// <summary>
    /// The symbol that is being called.
    /// </summary>
    public CallHierarchyItem To { get; }

    /// <summary>
    /// The locations where the "to" symbol is called from within the source symbol.
    /// </summary>
    public ImmutableArray<Location> Callsites { get; }
}
