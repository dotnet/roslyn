// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS4014, CS4016); }
        }

        protected override string GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return Resources.InsertAwait;
        }

        protected override Task<SyntaxNode> GetNewRoot(SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
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
                    if (expression == null)
                    {
                        return SpecializedTasks.Default<SyntaxNode>();
                    }

                    if (!IsCorrectReturnType(expression, semanticModel))
                    {
                        return SpecializedTasks.Default<SyntaxNode>();
                    }

                    return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));
                default:
                    return SpecializedTasks.Default<SyntaxNode>();
            }
        }

        private bool IsCorrectReturnType(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            INamedTypeSymbol taskType = null;
            INamedTypeSymbol returnType = null;
            return TryGetTypes(expression, semanticModel, out taskType, out returnType) &&
            semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists;
        }

        private static ExpressionSyntax ConvertToAwaitExpression(ExpressionSyntax expression)
        {
            return SyntaxFactory.AwaitExpression(expression)
                                .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
