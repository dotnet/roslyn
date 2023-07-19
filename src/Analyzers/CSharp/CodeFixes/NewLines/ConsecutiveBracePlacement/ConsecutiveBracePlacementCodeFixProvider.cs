// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConsecutiveBracePlacement), Shared]
    internal sealed class ConsecutiveBracePlacementCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConsecutiveBracePlacementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpCodeFixesResources.Remove_blank_lines_between_braces,
                    c => UpdateDocumentAsync(document, diagnostic, c),
                    nameof(CSharpCodeFixesResources.Remove_blank_lines_between_braces)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static Task<Document> UpdateDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);

        public static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _ = PooledDictionary<SyntaxToken, SyntaxToken>.GetInstance(out var tokenToToken);

            foreach (var diagnostic in diagnostics)
                FixOne(root, text, tokenToToken, diagnostic, cancellationToken);

            var newRoot = root.ReplaceTokens(tokenToToken.Keys, (t1, _) => tokenToToken[t1]);

            return document.WithSyntaxRoot(newRoot);
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

            if (!ConsecutiveBracePlacementDiagnosticAnalyzer.HasExcessBlankLinesAfter(
                    text, firstBrace, out var secondBrace, out var lastEndOfLineTrivia))
            {
                Debug.Fail("Could not match analyzer pattern");
                return;
            }

            var updatedSecondBrace = secondBrace.WithLeadingTrivia(
                secondBrace.LeadingTrivia.SkipWhile(t => t != lastEndOfLineTrivia).Skip(1));
            tokenToToken[secondBrace] = updatedSecondBrace;
        }

        public override FixAllProvider GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) => await FixAllAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
    }
}
