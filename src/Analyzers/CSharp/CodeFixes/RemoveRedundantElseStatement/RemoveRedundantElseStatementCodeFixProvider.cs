// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveRedundantElseStatement), Shared]
    internal class RemoveRedundantElseStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveRedundantElseStatementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveRedundantElseStatementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Remove_redundant_else_statement, nameof(CSharpAnalyzersResources.Remove_redundant_else_statement));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var topMostIf = (IfStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var elseClause = (ElseClauseSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);
                var block = topMostIf.Parent as BlockSyntax;

                var root = editor.OriginalRoot;
                var updatedRoot = root.ReplaceNode(block, RewriteBlock(block, topMostIf, elseClause));

                editor.ReplaceNode(root, updatedRoot);
            }

            return Task.CompletedTask;
        }

        private static BlockSyntax RewriteBlock(BlockSyntax block, IfStatementSyntax topMostIf, ElseClauseSyntax elseClause)
        {
            var topMostIfIdx = block.Statements.IndexOf(topMostIf);

            var newTopMostIf = topMostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newBlock = block.ReplaceNode(topMostIf, newTopMostIf);
            var newStatements = newBlock.Statements.InsertRange(topMostIfIdx + 1, Expand(elseClause));

            return newBlock.WithStatements(newStatements);
        }

        private static ImmutableArray<StatementSyntax> Expand(ElseClauseSyntax elseClause)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

            switch (elseClause.Statement)
            {
                case BlockSyntax block:
                    result.AddRange(block.Statements);
                    break;

                case StatementSyntax anythingElse:
                    result.Add(anythingElse);
                    break;
            }

            return result
                .Select(statement => statement.WithAdditionalAnnotations(Formatter.Annotation))
                .AsImmutable();
        }
    }
}
