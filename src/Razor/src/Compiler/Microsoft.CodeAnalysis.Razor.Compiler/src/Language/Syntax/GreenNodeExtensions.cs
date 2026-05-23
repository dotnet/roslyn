// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class GreenNodeExtensions
{
    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : GreenNode
    {
        return node != null ?
            ToGreenList<T>(node.Green) :
            default(InternalSyntax.SyntaxList<T>);
    }

    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : GreenNode
    {
        return new InternalSyntax.SyntaxList<T>(node);
    }

    public static TNode WithDiagnosticsGreen<TNode>(this TNode node, RazorDiagnostic[] diagnostics)
        where TNode : GreenNode
    {
        return (TNode)node.SetDiagnostics(diagnostics);
    }

    public static TNode WithDiagnosticsGreen<TNode>(this TNode node, params ImmutableArray<RazorDiagnostic> diagnostics)
        where TNode : GreenNode
    {
        var array = ImmutableCollectionsMarshal.AsArray(diagnostics);
        return node.WithDiagnosticsGreen(array);
    }
}
