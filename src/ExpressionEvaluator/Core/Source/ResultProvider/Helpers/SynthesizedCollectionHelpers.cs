// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Symbols;
using System;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal enum SynthesizedCollectionKind
{
    SingleElement,
    Array,
    List,
}

internal static class SynthesizedCollectionHelpers
{
    public static bool TryGetKind(Type t, out SynthesizedCollectionKind kind)
    {
        if (t is { IsGenericType: true } && t.GetGenericArguments() is { Length: 1 })
        {
            if (t.Name.StartsWith(WellKnownGeneratedNames.SynthesizedReadOnlyList_SingleElementPrefix, StringComparison.Ordinal))
            {
                kind = SynthesizedCollectionKind.SingleElement;
                return true;
            }

            if (t.Name.StartsWith(WellKnownGeneratedNames.SynthesizedReadOnlyList_ReadOnlyArrayPrefix, StringComparison.Ordinal))
            {
                kind = SynthesizedCollectionKind.Array;
                return true;
            }

            if (t.Name.StartsWith(WellKnownGeneratedNames.SynthesizedReadOnlyList_ReadOnlyListPrefix, StringComparison.Ordinal))
            {
                kind = SynthesizedCollectionKind.List;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
