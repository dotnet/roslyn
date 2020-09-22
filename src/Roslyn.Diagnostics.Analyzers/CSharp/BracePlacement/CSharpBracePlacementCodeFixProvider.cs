// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BracePlacement
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class CSharpBracePlacementCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(RoslynDiagnosticIds.BracePlacementRuleId);

        public override FixAllProvider GetFixAllProvider()
            => new CSharpBracePlacementFixAllProvider();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.Remove_blank_lines_between_braces,
                    c => UpdateDocumentAsync(document, diagnostic, c),
                    RoslynDiagnosticsAnalyzersResources.Remove_blank_lines_between_braces),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static async Task<Document> UpdateDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => document.WithSyntaxRoot(await FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken).ConfigureAwait(false));

        public static async Task<SyntaxNode> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var tokenToToken = PooledDictionary<SyntaxToken, SyntaxToken>.GetInstance();

            foreach (var diagnostic in diagnostics)
                FixOne(root, text, tokenToToken, diagnostic, cancellationToken);

            var newRoot = root.ReplaceTokens(tokenToToken.Keys, (t1, _) => tokenToToken[t1]);

            return newRoot;
        }

        private static void FixOne(
            SyntaxNode root, SourceText text,
            Dictionary<SyntaxToken, SyntaxToken> tokenToToken,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!token.IsKind(SyntaxKind.CloseBraceToken))
            {
                Debug.Fail("Could not find close brace in fixer");
                return;
            }

            var firstBrace = token.GetPreviousToken();
            if (!firstBrace.IsKind(SyntaxKind.CloseBraceToken))
            {
                Debug.Fail("Could not find previous close brace in fixer");
                return;
            }

            if (!CSharpBracePlacementDiagnosticAnalyzer.HasExcessBlankLinesAfter(
                    text, firstBrace, out var secondBrace, out var lastEndOfLineTrivia))
            {
                Debug.Fail("Could not match analyzer pattern");
                return;
            }

            var updatedSecondBrace = secondBrace.WithLeadingTrivia(
                secondBrace.LeadingTrivia.SkipWhile(t => t != lastEndOfLineTrivia).Skip(1));
            tokenToToken[secondBrace] = updatedSecondBrace;
        }
    }
}
