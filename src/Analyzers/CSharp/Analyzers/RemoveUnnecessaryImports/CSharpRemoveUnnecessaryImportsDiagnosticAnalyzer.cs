﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer :
        AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer
    {
        private static readonly LocalizableString s_TitleAndMessageFormat =
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Using_directive_is_unnecessary), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        protected override LocalizableString GetTitleAndMessageFormatForClassificationIdDescriptor()
            => s_TitleAndMessageFormat;

        // C# has no need to do any merging of using statements.  Only VB needs to
        // merge import clauses to an import statement if it all the import clauses
        // are unnecessary.
        protected override ImmutableArray<SyntaxNode> MergeImports(ImmutableArray<SyntaxNode> unnecessaryImports)
            => unnecessaryImports;

        protected override IUnnecessaryImportsProvider UnnecessaryImportsProvider
            => CSharpUnnecessaryImportsProvider.Instance;

        protected override bool IsRegularCommentOrDocComment(SyntaxTrivia trivia)
            => trivia.IsRegularComment() || trivia.IsDocComment();

        protected override IEnumerable<TextSpan> GetFixableDiagnosticSpans(
            IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(nodes.Any());

            var nodesContainingUnnecessaryUsings = new HashSet<SyntaxNode>();
            foreach (var node in nodes)
            {
                var nodeContainingUnnecessaryUsings = node.GetAncestors().First(n => n is NamespaceDeclarationSyntax || n is CompilationUnitSyntax);
                if (!nodesContainingUnnecessaryUsings.Add(nodeContainingUnnecessaryUsings))
                {
                    continue;
                }

                yield return nodeContainingUnnecessaryUsings is NamespaceDeclarationSyntax ?
                    ((NamespaceDeclarationSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan() :
                    ((CompilationUnitSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan();
            }
        }
    }
}
