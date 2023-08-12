// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct SynthesizedTypeMaps(
    ImmutableDictionary<AnonymousTypeKey, AnonymousTypeValue>? anonymousTypeMap,
    ImmutableDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>? anonymousDelegates,
    ImmutableDictionary<string, AnonymousTypeValue>? anonymousDelegatesWithIndexedNames)
{
    public static readonly SynthesizedTypeMaps Empty = new SynthesizedTypeMaps(null, null, null);

    public bool IsEmpty
        => AnonymousTypes.IsEmpty && AnonymousDelegates.IsEmpty && AnonymousDelegatesWithIndexedNames.IsEmpty;

    /// <summary>
    /// In C#, this is the set of anonymous types only; in VB, this is the set of anonymous types and delegates.
    /// </summary>
    public ImmutableDictionary<AnonymousTypeKey, AnonymousTypeValue> AnonymousTypes { get; }
        = anonymousTypeMap ?? ImmutableDictionary<AnonymousTypeKey, AnonymousTypeValue>.Empty;

    /// <summary>
    /// In C#, the set of anonymous delegates with name fully determined by signature;
    /// in VB, this set is unused and empty.
    /// </summary>
    public ImmutableDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> AnonymousDelegates { get; }
        = anonymousDelegates ?? ImmutableDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>.Empty;

    /// <summary>
    /// A map of the assembly identities of the baseline compilation to the identities of the original metadata AssemblyRefs.
    /// Only includes identities that differ between these two.
    /// </summary>
    public ImmutableDictionary<string, AnonymousTypeValue> AnonymousDelegatesWithIndexedNames { get; }
        = anonymousDelegatesWithIndexedNames ?? ImmutableDictionary<string, AnonymousTypeValue>.Empty;

    public bool IsSubsetOf(SynthesizedTypeMaps other)
        => AnonymousTypes.All(p => other.AnonymousTypes.ContainsKey(p.Key)) &&
           AnonymousDelegates.All(p => other.AnonymousDelegates.ContainsKey(p.Key)) &&
           AnonymousDelegatesWithIndexedNames.All(p => other.AnonymousDelegatesWithIndexedNames.ContainsKey(p.Key));
}
