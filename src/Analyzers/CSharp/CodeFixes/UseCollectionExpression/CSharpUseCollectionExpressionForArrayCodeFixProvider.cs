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
using Microsoft.CodeAnalysis.Text;
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
            var expression = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
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

        static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        void RewriteInitializerExpression(InitializerExpressionSyntax initializer)
        {
            editor.ReplaceNode(
                initializer,
                (current, _) => ConvertInitializerToCollectionExpression(
                    (InitializerExpressionSyntax)current,
                    IsOnSingleLine(sourceText, initializer)));
        }

        bool ShouldReplaceExistingExpressionEntirely(ExpressionSyntax explicitOrImplicitArray, InitializerExpressionSyntax initializer)
        {
            // Any time we have `{ x, y, z }` in any form, then always just replace the whole original expression
            // with `[x, y, z]`.
            if (IsOnSingleLine(sourceText, initializer))
                return true;

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
            var newKeyword = explicitOrImplicitArray.GetFirstToken();
            if (sourceText.AreOnSameLine(newKeyword, initializer.OpenBraceToken))
                return true;

            // Initializer was on multiple lines, and was not on the same line as the 'new' keyword, and the 'new' is on a newline:
            //
            //      var v2 =
            //          new[]
            //          {
            //              1, 2, 3
            //          };
            //
            // For this latter, we want to just remove the new portion and move the collection to subsume it.
            var previousToken = newKeyword.GetPreviousToken();
            if (previousToken == default)
                return true;

            if (!sourceText.AreOnSameLine(previousToken, newKeyword))
                return true;

            // All that is left is:
            //
            //      var v2 = new[]
            //      {
            //          1, 2, 3
            //      };
            //
            // For this we want to remove the 'new' portion, but keep the collection on its own line.
            return false;
        }

        void RewriteArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            Contract.ThrowIfNull(arrayCreation.Initializer);
            var shouldReplaceExpressionEntirely = ShouldReplaceExistingExpressionEntirely(arrayCreation, arrayCreation.Initializer);

            editor.ReplaceNode(
                arrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ArrayCreationExpressionSyntax)current;
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);

                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer,
                        IsOnSingleLine(sourceText, arrayCreation.Initializer));

                    return shouldReplaceExpressionEntirely
                        ? collectionExpression.WithTriviaFrom(currentArrayCreation)
                        : collectionExpression
                            .WithPrependedLeadingTrivia(currentArrayCreation.Type.GetTrailingTrivia())
                            .WithPrependedLeadingTrivia(ElasticMarker);
                });
        }

        void RewriteImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
        {
            Contract.ThrowIfNull(implicitArrayCreation.Initializer);
            var shouldReplaceExpressionEntirely = ShouldReplaceExistingExpressionEntirely(implicitArrayCreation, implicitArrayCreation.Initializer);

            editor.ReplaceNode(
                implicitArrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ImplicitArrayCreationExpressionSyntax)current;
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);

                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer,
                        IsOnSingleLine(sourceText, implicitArrayCreation));

                    return shouldReplaceExpressionEntirely
                        ? collectionExpression.WithTriviaFrom(currentArrayCreation)
                        : collectionExpression
                            .WithPrependedLeadingTrivia(currentArrayCreation.CloseBracketToken.TrailingTrivia)
                            .WithPrependedLeadingTrivia(ElasticMarker);
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
