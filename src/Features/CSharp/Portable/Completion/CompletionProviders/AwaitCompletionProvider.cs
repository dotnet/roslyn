// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
        {
            var declaration = token.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            // In a case like
            //   someTask.$$
            //   await Test();
            // someTask.await Test() is parsed as a local function statement.
            // We skip this and look further up in the hierarchy.
            if (declaration is LocalFunctionStatementSyntax localFunction && localFunction.ReturnType.ChildTokens().Contains(token))
            {
                return localFunction.Parent?.FirstAncestorOrSelf<SyntaxNode>(node => node.IsAsyncSupportingFunctionSyntax());
            }

            return declaration;
        }

        private protected override SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
            return dotToken switch
            {
                // Don't support conditional access someTask?.$$ or c?.TaskReturning().$$ because there is no good completion until
                // await? is supported by the language https://github.com/dotnet/csharplang/issues/35
                { Parent: MemberAccessExpressionSyntax memberAccess } when memberAccess.GetParentConditionalAccessExpression() is null => memberAccess,
                // someTask.$$.
                { Parent: RangeExpressionSyntax range } => range.LeftOperand,
                // special cases, where parsing is misleading. Such cases are handled in GetTypeSymbolOfExpression.
                { Parent: QualifiedNameSyntax qualifiedName } => qualifiedName.Left,
                _ => null,
            };
        }

        private protected override SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            // TODO: Parts of this code are taken from UnnamedSymbolCompletionProvider.GetDotAndExpressionStart -> Unify
            var tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position);
            // Has to be a . or a .. token
            if (!CompletionUtilities.TreatAsDot(dotToken, position - 1))
                return null;

            // don't want to trigger after a number. All other cases after dot are ok.
            if (dotToken.GetPreviousToken().Kind() == SyntaxKind.NumericLiteralToken)
                return null;

            return dotToken;
        }

        private protected override ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken)
        {
            if (potentialAwaitableExpression is MemberAccessExpressionSyntax memberAccess)
            {
                var memberAccessExpression = memberAccess.Expression.WalkDownParentheses();
                var symbol = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
                if (symbol is INamedTypeSymbol) // e.g. Task.$$
                {
                    return null;
                }

                return
                    symbol?.GetSymbolType() ??
                    symbol?.GetMemberType() ??
                    // Some expressions don't have a symbol (e.g. (o as Task).$$), but GetTypeInfo finds the right type.
                    semanticModel.GetTypeInfo(memberAccessExpression, cancellationToken).Type;
            }

            if (potentialAwaitableExpression is ExpressionSyntax expression)
            {
                if (expression.ShouldBeTreatedAsTypeInsteadOfExpression(semanticModel, out _, out var container))
                    return container;
            }

            return semanticModel.GetTypeInfo(potentialAwaitableExpression, cancellationToken).Type;
        }
    }
}
