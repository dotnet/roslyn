// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct SynthesizedTypeMaps(
    ImmutableSegmentedDictionary<AnonymousTypeKey, AnonymousTypeValue>? anonymousTypeMap,
    ImmutableSegmentedDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>? anonymousDelegates,
    ImmutableSegmentedDictionary<string, AnonymousTypeValue>? anonymousDelegatesWithIndexedNames)
{
    public static readonly SynthesizedTypeMaps Empty = new SynthesizedTypeMaps(null, null, null);

    public bool IsEmpty
        => AnonymousTypes.IsEmpty && AnonymousDelegates.IsEmpty && AnonymousDelegatesWithIndexedNames.IsEmpty;

    /// <summary>
    /// In C#, this is the set of anonymous types only; in VB, this is the set of anonymous types and delegates.
    /// </summary>
    public ImmutableSegmentedDictionary<AnonymousTypeKey, AnonymousTypeValue> AnonymousTypes { get; }
        = anonymousTypeMap ?? ImmutableSegmentedDictionary<AnonymousTypeKey, AnonymousTypeValue>.Empty;

    /// <summary>
    /// In C#, the set of anonymous delegates with name fully determined by signature;
    /// in VB, this set is unused and empty.
    /// </summary>
    public ImmutableSegmentedDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> AnonymousDelegates { get; }
        = anonymousDelegates ?? ImmutableSegmentedDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>.Empty;

    /// <summary>
    /// A map of the assembly identities of the baseline compilation to the identities of the original metadata AssemblyRefs.
    /// Only includes identities that differ between these two.
    /// </summary>
    public ImmutableSegmentedDictionary<string, AnonymousTypeValue> AnonymousDelegatesWithIndexedNames { get; }
        = anonymousDelegatesWithIndexedNames ?? ImmutableSegmentedDictionary<string, AnonymousTypeValue>.Empty;

    public bool IsSubsetOf(SynthesizedTypeMaps other)
        => AnonymousTypes.Keys.All(static (key, other) => other.AnonymousTypes.ContainsKey(key), other) &&
           AnonymousDelegates.Keys.All(static (key, other) => other.AnonymousDelegates.ContainsKey(key), other) &&
           AnonymousDelegatesWithIndexedNames.Keys.All(static (key, other) => other.AnonymousDelegatesWithIndexedNames.ContainsKey(key), other);
}
