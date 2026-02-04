// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an incoming call to a symbol.
/// </summary>
internal sealed class CallHierarchyIncomingCall
{
    public CallHierarchyIncomingCall(
        CallHierarchyItem from,
        ImmutableArray<Location> callsites)
    {
        From = from;
        Callsites = callsites;
    }

    /// <summary>
    /// The symbol that is calling the target symbol.
    /// </summary>
    public CallHierarchyItem From { get; }

    /// <summary>
    /// The locations where the target symbol is called from within the "from" symbol.
    /// </summary>
    public ImmutableArray<Location> Callsites { get; }
}
