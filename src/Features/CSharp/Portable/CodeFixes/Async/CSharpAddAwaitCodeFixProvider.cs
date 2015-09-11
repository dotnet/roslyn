// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;
using Resources = Microsoft.CodeAnalysis.CSharp.CSharpFeaturesResources;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Async
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAwait), Shared]
    internal class CSharpAddAwaitCodeFixProvider : AbstractAddAsyncAwaitCodeFixProvider
    {
        /// <summary>
        /// Since this is an async method, the return expression must be of type 'blah' rather than 'baz'
        /// </summary>
        private const string CS4014 = "CS4014";

        /// <summary>
        /// Because this call is not awaited, execution of the current method continues before the call is completed.
        /// </summary>
        private const string CS4016 = "CS4016";

        /// <summary>
        /// cannot implicitly convert from 'X' to 'Y'.
        /// </summary>
        private const string CS0029 = "CS0029";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0029, CS4014, CS4016);


        protected override string GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken) => Resources.InsertAwait;

        protected override Task<SyntaxNode> GetNewRoot(
            SyntaxNode root,
            SyntaxNode oldNode,
            SemanticModel semanticModel,
            Diagnostic diagnostic,
            Document document,
            CancellationToken cancellationToken)
        {
            var expression = oldNode as ExpressionSyntax;

            switch (diagnostic.Id)
            {
                case CS4014:
                    if (expression == null)
                    {
                        return Task.FromResult<SyntaxNode>(null);
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));

                case CS4016:
                    if (!DoesExpressionReturnTask(expression, semanticModel))
                    {
                        return SpecializedTasks.Default<SyntaxNode>();
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));

                case CS0029:
                    if (!DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(expression, semanticModel, document.Project, cancellationToken))
                    {
                        return SpecializedTasks.Default<SyntaxNode>();
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));
                default:
                    return SpecializedTasks.Default<SyntaxNode>();
            }
        }



        private static bool DoesExpressionReturnTask(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression == null)
            {
                return false;
            }

            INamedTypeSymbol taskType = null;
            INamedTypeSymbol returnType = null;
            return TryGetTaskAndExpressionTypes(expression, semanticModel, out taskType, out returnType) &&
            semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists;
        }

        private static bool DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(ExpressionSyntax expression, SemanticModel semanticModel, Project project, CancellationToken cancellationToken)
        {
            if (expression == null)
            {
                return false;
            }

            if (!IsInAsyncFunction(expression))
            {
                return false;
            }

            INamedTypeSymbol taskType = null;
            INamedTypeSymbol rightSideType = null;
            if (!TryGetTaskAndExpressionTypes(expression, semanticModel, out taskType, out rightSideType))
            {
                return false;
            }

            var compilation = semanticModel.Compilation;
            if (!compilation.ClassifyConversion(taskType, rightSideType).Exists)
            {
                return false;
            }

            if(!rightSideType.IsGenericType)
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
                        return (node as AnonymousFunctionExpressionSyntax)?.AsyncKeyword.IsMissing == true;
                    case SyntaxKind.MethodDeclaration:
                        return (node as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.AsyncKeyword) == true;
                    default:
                        continue;
                }
            }

            return false;
        }

        private static ExpressionSyntax ConvertToAwaitExpression(ExpressionSyntax expression)
        {
            return SyntaxFactory.AwaitExpression(expression)
                                .WithTriviaFrom(expression)
                                .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
