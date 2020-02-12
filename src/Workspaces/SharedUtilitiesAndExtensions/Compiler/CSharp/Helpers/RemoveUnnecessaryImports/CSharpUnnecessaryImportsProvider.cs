// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal sealed class CSharpUnnecessaryImportsProvider
        : AbstractUnnecessaryImportsProvider<UsingDirectiveSyntax>
    {
        public static readonly CSharpUnnecessaryImportsProvider Instance = new CSharpUnnecessaryImportsProvider();

        private CSharpUnnecessaryImportsProvider()
        {
        }

        protected override ImmutableArray<SyntaxNode> GetUnnecessaryImports(
            SemanticModel model, SyntaxNode root,
            Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
        {
            predicate ??= Functions<SyntaxNode>.True;
            var diagnostics = model.GetDiagnostics(cancellationToken: cancellationToken);
            if (diagnostics.IsEmpty)
            {
                return ImmutableArray<SyntaxNode>.Empty;
            }

            var unnecessaryImports = new HashSet<SyntaxNode>();

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == "CS8019")
                {
                    if (root.FindNode(diagnostic.Location.SourceSpan) is UsingDirectiveSyntax node && predicate(node))
                    {
                        unnecessaryImports.Add(node);
                    }
                }
            }

            return unnecessaryImports.ToImmutableArray();
        }
    }
}
