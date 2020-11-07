// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertStringConcatToInterpolated
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "TODO" /* PredefinedCodeRefactoringProviderNames.AddAwait*/), Shared]
    internal sealed class CSharpConvertStringConcatToInterpolatedRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertStringConcatToInterpolatedRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var binaryExpression = await context.TryGetRelevantNodeAsync<BinaryExpressionSyntax>().ConfigureAwait(false);
            if (binaryExpression?.IsKind(SyntaxKind.AddExpression) == true)
            {
                // Walk up to get the full sequence of conjunctive concatenations
                while (binaryExpression.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } parent)
                {
                    binaryExpression = parent;
                }

                var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (IsStringConcatenation(semanticModel, binaryExpression, cancellationToken))
                {
                    // Collect the expression parts ("a" + "b" + expr) into an array { "a", "b", expr }
                    using var _ = ArrayBuilder<ExpressionSyntax>.GetInstance(out var builder);
                    WalkBinaryExpression(semanticModel, builder, binaryExpression, cancellationToken);
                    var concatParts = builder.ToImmutable();
                    // Is there any expression besides string literals?
                    if (concatParts.Any(expr => expr is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression }))
                    {
                        context.RegisterRefactoring(new MyCodeAction(
                            title: "TODO",
                            c => UpdateDocumentAsync(document, binaryExpression, concatParts, c)),
                            binaryExpression.Span);
                    }
                }
            }
        }

        private static bool IsStringConcatenation(SemanticModel semanticModel, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
        {
            var operation = semanticModel.GetOperation(binaryExpression, cancellationToken);
            if (operation is IBinaryOperation binaryOperation)
            {
                return (binaryOperation.OperatorKind == BinaryOperatorKind.Add && binaryOperation.Type.Equals(semanticModel.Compilation.GetSpecialType(SpecialType.System_String)));
            }

            return false;
        }

        private static void WalkBinaryExpression(SemanticModel semanticModel, ArrayBuilder<ExpressionSyntax> arrayBuilder, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
        {
            WalkBinaryExpression(semanticModel, arrayBuilder, binaryExpression.Left, cancellationToken);
            WalkBinaryExpression(semanticModel, arrayBuilder, binaryExpression.Right, cancellationToken);
        }

        private static void WalkBinaryExpression(SemanticModel semanticModel, ArrayBuilder<ExpressionSyntax> arrayBuilder, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            if (expression is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } potentialStringConcatenation &&
                IsStringConcatenation(semanticModel, potentialStringConcatenation, cancellationToken))
            {
                WalkBinaryExpression(semanticModel, arrayBuilder, potentialStringConcatenation, cancellationToken);
            }
            else
            {
                arrayBuilder.Add(expression);
            }
        }

        private static async Task<Document> UpdateDocumentAsync(Document document, BinaryExpressionSyntax binaryExpression, ImmutableArray<ExpressionSyntax> concatParts, CancellationToken cancellationToken)
        {
            var interpolatedStringContent = ConvertConcatPartsToInterpolatedStringContent(concatParts);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);
            var interpolated = InterpolatedStringExpression(
                Token(SyntaxKind.InterpolatedStringStartToken),
                List(interpolatedStringContent));
            // expressions may have superfluous parenthesis after the conversion to an interpolated string:
            // "a" + (1 + 1) -> $"a{(1 + 1)}"
            // if there is any (InterpolationSyntax.Expression is ParenthesizedExpressionSyntax) we need to
            // recompile and check via parenthesized.CanRemoveParentheses(semanticModel) if it can be removed.
            var anyPotentiallyRemovableParenthesis = interpolated.Contents.Any(IsParenthesizedInterpolation);
            var annotation = new SyntaxAnnotation("InterpolatedStringExpressionWithPotentiallyRemovableParanthesisKind");
            if (anyPotentiallyRemovableParenthesis)
            {
                // annotate "interpolated" because we need to find it after the changes are applied
                interpolated = interpolated.WithAdditionalAnnotations(annotation);
            }
            editor.ReplaceNode(binaryExpression, interpolated);

            if (anyPotentiallyRemovableParenthesis)
            {
                // Take the changed document and go through interpolatedNode.Contents to check if any parenthesis can be removed.
                var newDocument = document.WithSyntaxRoot(editor.GetChangedRoot());
                var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var interpolatedNode = (InterpolatedStringExpressionSyntax)newRoot.GetAnnotatedNodes(annotation).Single();
                var semanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                editor = new SyntaxEditor(newRoot, CSharpSyntaxGenerator.Instance);
                foreach (var content in interpolatedNode.Contents.Where(IsParenthesizedInterpolation).Cast<InterpolationSyntax>())
                {
                    var parenthesized = (ParenthesizedExpressionSyntax)content.Expression;
                    if (parenthesized.CanRemoveParentheses(semanticModel))
                    {
                        editor.ReplaceNode(parenthesized, parenthesized.Expression);
                    }
                }
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static bool IsParenthesizedInterpolation(InterpolatedStringContentSyntax interpolatedStringContent)
            => interpolatedStringContent is InterpolationSyntax { Expression: ParenthesizedExpressionSyntax };

        private static IEnumerable<InterpolatedStringContentSyntax> ConvertConcatPartsToInterpolatedStringContent(ImmutableArray<ExpressionSyntax> concatParts)
        {
            var result = new List<InterpolatedStringContentSyntax>(concatParts.Length);
            foreach (var part in concatParts)
            {
                switch (part)
                {
                    case var expression when IsStringLiteralExpression(expression, out var literal):
                        if (result.LastOrDefault() is InterpolatedStringTextSyntax text)
                        {
                            // Merge the string literal with the last InterpolatedStringTextSyntax
                            var newText = text.TextToken.ValueText + literal.Token.ValueText;
                            result[^1] = InterpolatedStringText(InterpolatedStringTextToken(newText));
                        }
                        else
                        {
                            result.Add(InterpolatedStringText(InterpolatedStringTextToken(literal.Token.ValueText)));
                        }
                        break;
                    case InterpolatedStringExpressionSyntax interpolated when !interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken):
                        // One of the concatParts is itself an interpolated string at should be in-lined:
                        // "a" + $"b{expr}c" -> $"ab{expr}c"
                        // Here "interpolated" is $"b{expr}c"
                        foreach (var contentPart in interpolated.Contents)
                        {
                            if (contentPart is InterpolatedStringTextSyntax textFromOther && result.LastOrDefault() is InterpolatedStringTextSyntax previousText)
                            {
                                // The last added content was a text and the first part in interpolated is a text. We can merge these two.
                                // "a" + $"b{expr}" -> "a" and "b" can be merged
                                var newText = previousText.TextToken.ValueText + textFromOther.TextToken.ValueText;
                                result[^1] = InterpolatedStringText(InterpolatedStringTextToken(newText));
                            }
                            else
                            {
                                result.Add(contentPart);
                            }
                        }
                        break;
                    default:
                        // "part" might be parenthesized here. Superfluous parenthesizes are removed later.
                        // "+" usually has whitespace before and after. We remove that and other trivia.
                        result.Add(Interpolation(part.WithoutTrivia()));
                        break;
                }
            }

            return result;
        }

        private static SyntaxToken InterpolatedStringTextToken(string text)
            => Token(TriviaList(), SyntaxKind.InterpolatedStringTextToken, text, text, TriviaList());

        private static bool IsStringLiteralExpression(ExpressionSyntax? expression, [NotNullWhen(true)] out LiteralExpressionSyntax? literal)
        {
            if (expression is LiteralExpressionSyntax foundLiteral &&
                foundLiteral.IsKind(SyntaxKind.StringLiteralExpression) &&
                !foundLiteral.Token.IsVerbatimStringLiteral())
            {
                literal = foundLiteral;
                return true;
            }

            literal = null;
            return false;
        }

        private sealed class MyCodeAction : CodeActions.CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
