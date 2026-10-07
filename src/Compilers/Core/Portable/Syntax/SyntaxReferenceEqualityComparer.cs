// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Compares syntax references by syntax tree identity and span.
/// </summary>
internal sealed class SyntaxReferenceEqualityComparer : IEqualityComparer<SyntaxReference>
{
    internal static readonly SyntaxReferenceEqualityComparer Instance = new SyntaxReferenceEqualityComparer();

    private SyntaxReferenceEqualityComparer()
    {
    }

    public bool Equals(SyntaxReference? x, SyntaxReference? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        return x is { SyntaxTree: var xSyntaxTree, Span: var xSpan } &&
               y is { SyntaxTree: var ySyntaxTree, Span: var ySpan } &&
               xSyntaxTree == ySyntaxTree &&
               xSpan == ySpan;
    }

    public int GetHashCode(SyntaxReference obj)
        => Hash.Combine(RuntimeHelpers.GetHashCode(obj.SyntaxTree), obj.Span.GetHashCode());
}
