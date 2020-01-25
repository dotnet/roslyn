﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Async
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAwait), Shared]
    internal class CSharpAddAwaitCodeFixProvider : AbstractAddAwaitCodeFixProvider
    {
        /// <summary>
        /// Because this call is not awaited, execution of the current method continues before the call is completed.
        /// </summary>
        private const string CS4014 = nameof(CS4014);

        /// <summary>
        /// Since this is an async method, the return expression must be of type 'blah' rather than 'baz'
        /// </summary>
        private const string CS4016 = nameof(CS4016);

        /// <summary>
        /// cannot implicitly convert from 'X' to 'Y'.
        /// </summary>
        private const string CS0029 = nameof(CS0029);

        [ImportingConstructor]
        public CSharpAddAwaitCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0029, CS4014, CS4016);

        protected override async Task<DescriptionAndNode> GetDescriptionAndNodeAsync(
            SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            var newRoot = await GetNewRootAsync(
                root, oldNode, semanticModel, diagnostic, document, cancellationToken).ConfigureAwait(false);
            if (newRoot == null)
            {
                return default;
            }

            return new DescriptionAndNode(CSharpFeaturesResources.Insert_await, newRoot);
        }

        private Task<SyntaxNode> GetNewRootAsync(
            SyntaxNode root,
            SyntaxNode oldNode,
            SemanticModel semanticModel,
            Diagnostic diagnostic,
            Document document,
            CancellationToken cancellationToken)
        {
            if (!(oldNode is ExpressionSyntax expression))
            {
                return SpecializedTasks.Null<SyntaxNode>();
            }

            switch (diagnostic.Id)
            {
                case CS4014:
                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));

                case CS4016:
                    if (!DoesExpressionReturnTask(expression, semanticModel))
                    {
                        return SpecializedTasks.Null<SyntaxNode>();
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));

                case CS0029:
                    if (!DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(expression, semanticModel, document.Project, cancellationToken))
                    {
                        return SpecializedTasks.Null<SyntaxNode>();
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));

                default:
                    return SpecializedTasks.Null<SyntaxNode>();
            }
        }

        private static bool DoesExpressionReturnTask(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (!TryGetTaskType(semanticModel, out var taskType))
            {
                return false;
            }

            return TryGetExpressionType(expression, semanticModel, out var returnType) &&
            semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists;
        }

        private static bool DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(ExpressionSyntax expression, SemanticModel semanticModel, Project project, CancellationToken cancellationToken)
        {
            if (!IsInAsyncFunction(expression))
            {
                return false;
            }

            if (!TryGetTaskType(semanticModel, out var taskType) ||
                !TryGetExpressionType(expression, semanticModel, out var rightSideType))
            {
                return false;
            }

            var compilation = semanticModel.Compilation;
            if (!compilation.ClassifyConversion(taskType, rightSideType).Exists)
            {
                return false;
            }

            if (!rightSideType.IsGenericType)
            {
                return false;
            }

            var typeArguments = rightSideType.TypeArguments;
            var typeInferer = project.LanguageServices.GetService<ITypeInferenceService>();
            var inferredTypes = typeInferer.InferTypes(semanticModel, expression, cancellationToken);
            return typeArguments.Any(ta => inferredTypes.Any(it => compilation.ClassifyConversion(it, ta).Exists));
        }

        private static bool IsInAsyncFunction(ExpressionSyntax expression)
        {
            foreach (var node in expression.Ancestors())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                        return (node as AnonymousFunctionExpressionSyntax)?.AsyncKeyword.IsMissing == false;
                    case SyntaxKind.MethodDeclaration:
                        return (node as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.AsyncKeyword) == true;
                    default:
                        continue;
                }
            }

            return false;
        }

        private static SyntaxNode ConvertToAwaitExpression(ExpressionSyntax expression)
        {
            if ((expression is BinaryExpressionSyntax || expression is ConditionalExpressionSyntax) && expression.HasTrailingTrivia)
            {
                var expWithTrailing = expression.WithoutLeadingTrivia();
                var span = expWithTrailing.GetLocation().GetLineSpan().Span;
                if (span.Start.Line == span.End.Line && !expWithTrailing.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                {
                    return SyntaxFactory.AwaitExpression(SyntaxFactory.ParenthesizedExpression(expWithTrailing))
                                        .WithLeadingTrivia(expression.GetLeadingTrivia())
                                        .WithAdditionalAnnotations(Formatter.Annotation);
                }
            }

            return SyntaxFactory.AwaitExpression(expression.WithoutTrivia().Parenthesize())
                                .WithTriviaFrom(expression)
                                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
        }
    }
}
