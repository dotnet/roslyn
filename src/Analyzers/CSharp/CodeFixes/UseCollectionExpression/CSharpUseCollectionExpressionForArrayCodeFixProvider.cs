// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForArray), Shared]
internal partial class CSharpUseCollectionExpressionForArrayCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForArrayCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId);

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, nameof(CSharpCodeFixesResources.Use_collection_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            var expression = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (expression is InitializerExpressionSyntax initializer)
            {
                RewriteInitializerExpression(initializer);
            }
            else if (expression is ArrayCreationExpressionSyntax arrayCreation)
            {
                RewriteArrayCreationExpression(arrayCreation);
            }
            else if (expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                RewriteImplicitArrayCreationExpression(implicitArrayCreation);
            }
        }

        return;

        bool IsOnSingleLine(SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        void RewriteInitializerExpression(InitializerExpressionSyntax initializer)
        {
            editor.ReplaceNode(
                initializer,
                (current, _) => ConvertInitializerToCollectionExpression(
                    (InitializerExpressionSyntax)current,
                    IsOnSingleLine(initializer)));
        }

        void RewriteArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            editor.ReplaceNode(
                arrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ArrayCreationExpressionSyntax)current;

                    Contract.ThrowIfNull(arrayCreation.Initializer);
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);

                    var isOnSingleLine = IsOnSingleLine(arrayCreation.Initializer);

                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer,
                        isOnSingleLine);

                    // Any time we have `{ x, y, z }` in any form, then always just replace the whole original expression
                    // with `[x, y, z]`.
                    if (isOnSingleLine)
                        return collectionExpression.WithTriviaFrom(currentArrayCreation);

                    // initializer was on multiple lines, but started on the same line as the 'new' keyword.  e.g.:
                    //
                    //      var v = new[] {
                    //          1, 2, 3
                    //      };
                    //
                    // Just remove the `new...` section entirely, but otherwise keep the initialize multiline:
                    //
                    //      var v = [
                    //          1, 2, 3
                    //      ];
                    if (sourceText.AreOnSameLine(arrayCreation.NewKeyword, arrayCreation.Initializer.OpenBraceToken))
                        return collectionExpression.WithTriviaFrom(currentArrayCreation);

                    // Initializer was on multiple lines, and was not on the same line as the 'new' keyword. e.g.:
                    //
                    //      var v2 = new[]
                    //      {
                    //          1, 2, 3
                    //      };
                    //
                    //  or
                    //
                    //      var v2 =
                    //          new[]
                    //          {
                    //              1, 2, 3
                    //          };
                    //
                    // For the former, we want to remove the 'new' portion, but keep the collection on its own line.
                    // For the latter, we want to just remove the new and move the collection to subsume it.
                    var previousToken = arrayCreation.NewKeyword.GetPreviousToken();
                    if (previousToken == default || !sourceText.AreOnSameLine(previousToken, arrayCreation.NewKeyword))
                        return collectionExpression.WithTriviaFrom(currentArrayCreation);

                    return collectionExpression.WithLeadingTrivia(currentArrayCreation.Type.GetTrailingTrivia());
                });
        }

        void RewriteImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
        {
            editor.ReplaceNode(
                implicitArrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ImplicitArrayCreationExpressionSyntax)current;
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);
                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer,
                        IsOnSingleLine(implicitArrayCreation));

                    collectionExpression = collectionExpression.WithLeadingTrivia(currentArrayCreation.GetLeadingTrivia());
                    return collectionExpression;
                });
        }
    }

    private static CollectionExpressionSyntax ConvertInitializerToCollectionExpression(
        InitializerExpressionSyntax initializer, bool wasOnSingleLine)
    {
        // if the initializer is already on multiple lines, keep it that way.  otherwise, squash from `{ 1, 2, 3 }` to `[1, 2, 3]`
        var openBracket = Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(initializer.OpenBraceToken);
        var elements = initializer.Expressions.GetWithSeparators().SelectAsArray(
            i => i.IsToken ? i : ExpressionElement((ExpressionSyntax)i.AsNode()!));
        var closeBracket = Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(initializer.CloseBraceToken);

        if (wasOnSingleLine)
        {
            // convert '{ ' to '['
            if (openBracket.TrailingTrivia is [(kind: SyntaxKind.WhitespaceTrivia), ..])
                openBracket = openBracket.WithTrailingTrivia(openBracket.TrailingTrivia.Skip(1));

            if (elements is [.., var lastNodeOrToken] && lastNodeOrToken.GetTrailingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia)] trailingTrivia)
                elements = elements.Replace(lastNodeOrToken, lastNodeOrToken.WithTrailingTrivia(trailingTrivia.Take(trailingTrivia.Count - 1)));
        }

        return CollectionExpression(openBracket, SeparatedList<CollectionElementSyntax>(elements), closeBracket);
    }
}
