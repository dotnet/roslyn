// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an incoming call in a call hierarchy (a caller of a method).
/// </summary>
internal readonly struct CallHierarchyIncomingCall
{
    /// <summary>
    /// The calling method.
    /// </summary>
    public CallHierarchyItem Caller { get; }

    /// <summary>
    /// The locations within the caller where the calls occur.
    /// These are relative to the caller's document.
    /// </summary>
    public ImmutableArray<Text.TextSpan> CallSites { get; }

    public CallHierarchyIncomingCall(CallHierarchyItem caller, ImmutableArray<Text.TextSpan> callSites)
    {
        Caller = caller;
        CallSites = callSites;
    }
}

/// <summary>
/// Represents an outgoing call in a call hierarchy (a callee of a method).
/// </summary>
internal readonly struct CallHierarchyOutgoingCall
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public CallHierarchyItem Callee { get; }

    /// <summary>
    /// The locations within the current method where the calls to the callee occur.
    /// </summary>
    public ImmutableArray<Text.TextSpan> CallSites { get; }

    public CallHierarchyOutgoingCall(CallHierarchyItem callee, ImmutableArray<Text.TextSpan> callSites)
    {
        Callee = callee;
        CallSites = callSites;
    }
}
