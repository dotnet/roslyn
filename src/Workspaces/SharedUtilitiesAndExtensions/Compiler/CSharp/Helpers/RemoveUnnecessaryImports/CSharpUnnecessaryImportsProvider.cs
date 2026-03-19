// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal sealed class CSharpUnnecessaryImportsProvider
    : AbstractUnnecessaryImportsProvider<UsingDirectiveSyntax>
{
    public static readonly CSharpUnnecessaryImportsProvider Instance = new();

    private CSharpUnnecessaryImportsProvider()
    {
    }

    public override ImmutableArray<UsingDirectiveSyntax> GetUnnecessaryImports(
        SemanticModel model,
        Func<SyntaxNode, bool>? predicate,
        CancellationToken cancellationToken)
    {
        var root = model.SyntaxTree.GetRoot(cancellationToken);
        predicate ??= Functions<SyntaxNode>.True;
        var diagnostics = model.GetDiagnostics(cancellationToken: cancellationToken);

        using var _ = ArrayBuilder<UsingDirectiveSyntax>.GetInstance(out var result);
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Id == "CS8019" &&
                root.FindNode(diagnostic.Location.SourceSpan) is UsingDirectiveSyntax node && predicate(node))
            {
                result.Add(node);
            }
        }

        result.RemoveDuplicates();
        return result.ToImmutableArray();
    }
}
