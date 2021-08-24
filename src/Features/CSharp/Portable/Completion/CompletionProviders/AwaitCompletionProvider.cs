// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
    [Shared]
    internal sealed class AwaitCompletionProvider : AbstractAwaitCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AwaitCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private protected override int GetSpanStart(SyntaxNode declaration)
        {
            return declaration switch
            {
                MethodDeclarationSyntax method => method.ReturnType.SpanStart,
                LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
                AnonymousMethodExpressionSyntax anonymous => anonymous.DelegateKeyword.SpanStart,
                // If we have an explicit lambda return type, async should go just before it. Otherwise, it should go before parameter list.
                // static [|async|] (a) => ....
                // static [|async|] ExplicitReturnType (a) => ....
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => (parenthesizedLambda.ReturnType as SyntaxNode ?? parenthesizedLambda.ParameterList).SpanStart,
                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.SpanStart,
                _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
            };
        }

        private protected override SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token)
            => token.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());

        private protected override SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position);
            // Don't support conditional access someTask?.$$ or c?.TaskReturning().$$ because there is no good completion until
            // await? is supported by the language https://github.com/dotnet/csharplang/issues/35
            if (dotToken.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.GetParentConditionalAccessExpression() is null)
            {
                return memberAccess;
            }

            return null;
        }

        private protected override bool IsDotAwaitKeywordContext(SyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var position = syntaxContext.Position;
            var syntaxTree = syntaxContext.SyntaxTree;
            var potentialAwaitableExpression = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);
            // TODO: someTask.$$. Middle of DotDotToken // see UnnamedSymbolCompletionProvider.GetDotAndExpressionStart
            // TODO: Support corner cases like: someTask.$$ int i = 0; // see CSharpRecommendationServiceRunner.ShouldBeTreatedAsTypeInsteadOfExpression
            if (potentialAwaitableExpression is not null)
            {
                var semanticModel = syntaxContext.SemanticModel;
                var symbol = GetTypeSymbolOfExpression(semanticModel, potentialAwaitableExpression, cancellationToken);
                var isAwaitable = symbol?.IsAwaitableNonDynamic(semanticModel, position);
                if (isAwaitable == true)
                {
                    var parentOfAwaitable = potentialAwaitableExpression.Parent;
                    if (parentOfAwaitable is not AwaitExpressionSyntax)
                    {
                        // We have a awaitable type left of the dot, that is not yet awaited.
                        // We need to check if await is valid at the insertion position.
                        var syntaxContextAtInsertationPosition = syntaxContext.GetLanguageService<ISyntaxContextService>().CreateContext(
                            syntaxContext.Workspace, syntaxContext.SemanticModel, potentialAwaitableExpression.SpanStart, cancellationToken);
                        return syntaxContextAtInsertationPosition.IsAwaitKeywordContext();
                    }
                }
            }

            return false;

            static ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken)
            {
                return potentialAwaitableExpression switch
                {
                    MemberAccessExpressionSyntax memberAccess => semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol?.GetSymbolType(),
                    _ => semanticModel.GetSymbolInfo(potentialAwaitableExpression, cancellationToken).Symbol?.GetSymbolType(),
                };
            }
        }

    }
}
