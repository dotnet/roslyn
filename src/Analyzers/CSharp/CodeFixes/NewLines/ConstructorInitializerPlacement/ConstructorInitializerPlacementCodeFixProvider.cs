// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConstructorInitializerPlacement), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConstructorInitializerPlacementCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpCodeFixesResources.Place_token_on_following_line,
                c => UpdateDocumentAsync(document, [diagnostic], c),
                nameof(CSharpCodeFixesResources.Place_token_on_following_line)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    private static async Task<Document> UpdateDocumentAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        using var _ = PooledDictionary<SyntaxToken, SyntaxToken>.GetInstance(out var replacementMap);

        foreach (var diagnostic in diagnostics)
        {
            var initializer = (ConstructorInitializerSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var colonToken = initializer.ColonToken;
            var thisBaseKeyword = initializer.ThisOrBaseKeyword;
            var parenToken = colonToken.GetPreviousToken();

            if (text.AreOnSameLine(parenToken, colonToken))
            {
                // something like:
                //
                //      public C() :
                //          base()
                //
                // Move the trivia from the : to the preceding  )  and move the trivia on 'base' to the colon, and
                // add a space after it.
                MoveTriviaWhenOnSameLine(replacementMap, colonToken, thisBaseKeyword);
            }
            else
            {
                // something like:
                //
                //      public C()
                //          :
                //          base()
                //
                // Just add a space after the colon, and remove all leading trivia from this/base
                replacementMap[colonToken] = colonToken.WithLeadingTrivia(colonToken.LeadingTrivia.AddRange(colonToken.TrailingTrivia).AddRange(thisBaseKeyword.LeadingTrivia))
                                                       .WithTrailingTrivia(SyntaxFactory.Space);
                replacementMap[thisBaseKeyword] = thisBaseKeyword.WithoutLeadingTrivia();
            }
        }

        var newRoot = root.ReplaceTokens(replacementMap.Keys, (original, _) => replacementMap[original]);

        return document.WithSyntaxRoot(newRoot);
    }

    private static void MoveTriviaWhenOnSameLine(
        Dictionary<SyntaxToken, SyntaxToken> replacementMap, SyntaxToken colonToken, SyntaxToken thisBaseKeyword)
    {
        // colonToken has the unnecessary newline.  Move all of it's trivia to the previous token so nothing belongs to it.
        var closeParen = colonToken.GetPreviousToken();
        replacementMap[closeParen] = ComputeNewCloseParen(colonToken, closeParen);

        // Now, take all the trivia from the this/base keyword, and move before the colon, and add a space after it
        // this will place it properly before the this/base keyword.
        replacementMap[colonToken] = colonToken.WithLeadingTrivia(thisBaseKeyword.LeadingTrivia).WithTrailingTrivia(SyntaxFactory.Space);

        // Finally, remove all leading trivia from the this/base keyword.  It was moved to the colon
        replacementMap[thisBaseKeyword] = thisBaseKeyword.WithoutLeadingTrivia();

        static SyntaxToken ComputeNewCloseParen(SyntaxToken colonToken, SyntaxToken previousToken)
        {
            var allColonTrivia = colonToken.LeadingTrivia.AddRange(colonToken.TrailingTrivia);

            return previousToken.TrailingTrivia.All(t => t.Kind() == SyntaxKind.WhitespaceTrivia)
                ? previousToken.WithTrailingTrivia(allColonTrivia)
                : previousToken.WithAppendedTrailingTrivia(allColonTrivia);
        }
    }

    public override FixAllProvider? GetFixAllProvider()
        => FixAllProvider.Create(async (context, document, diagnostics) => await UpdateDocumentAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
}
