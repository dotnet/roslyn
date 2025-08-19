// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct SynthesizedTypeMaps(
    ImmutableSegmentedDictionary<AnonymousTypeKey, AnonymousTypeValue>? anonymousTypeMap,
    ImmutableSegmentedDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>? anonymousDelegates,
    ImmutableSegmentedDictionary<AnonymousDelegateWithIndexedNamePartialKey, ImmutableArray<AnonymousTypeValue>>? anonymousDelegatesWithIndexedNames)
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
    /// In C#, the set of anonymous delegates with name that is not determined by parameter types only 
    /// and need to be suffixed by an index (e.g. delegates may have same parameter types but differ in default parameter values);
    /// in VB, this set is unused and empty.
    /// </summary>
    public ImmutableSegmentedDictionary<AnonymousDelegateWithIndexedNamePartialKey, ImmutableArray<AnonymousTypeValue>> AnonymousDelegatesWithIndexedNames { get; }
        = anonymousDelegatesWithIndexedNames ?? ImmutableSegmentedDictionary<AnonymousDelegateWithIndexedNamePartialKey, ImmutableArray<AnonymousTypeValue>>.Empty;
}
