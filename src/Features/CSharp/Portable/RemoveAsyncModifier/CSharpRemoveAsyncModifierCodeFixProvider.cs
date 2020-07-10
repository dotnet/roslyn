// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.RemoveAsyncModifier;
using Roslyn.Utilities;
using KnownTypes = Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider.KnownTypes;

namespace Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMethodSynchronous), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal partial class CSharpRemoveAsyncModifierCodeFixProvider : AbstractRemoveAsyncModifierCodeFixProvider<ReturnStatementSyntax, ExpressionSyntax>
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveAsyncModifierCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override bool ShouldOfferFix(ISymbol declaredSymbol, KnownTypes knownTypes)
        {
            // Lambdas and anonymous functions don't have a declared symbol so there is nothing more to check
            if (declaredSymbol == null)
            {
                return true;
            }

            if (declaredSymbol is IMethodSymbol methodSymbolOpt)
            {
                // For async void methods this fixer does the same as Make Method Synchronous so we don't need to offer both
                if (methodSymbolOpt.ReturnsVoid)
                {
                    return false;
                }

                // IAsyncEnumerable iterators cannot be made non-async even if they don't have awaits
                return !methodSymbolOpt.ReturnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumerableOfTTypeOpt) &&
                       !methodSymbolOpt.ReturnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumeratorOfTTypeOpt);
            }

            return false;
        }

        protected override SyntaxNode GetBlockBody(SyntaxNode node)
            => node switch
            {
                MethodDeclarationSyntax method => method.Body,
                LocalFunctionStatementSyntax localFunction => localFunction.Body,
                AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Block,
                _ => throw ExceptionUtilities.Unreachable
            };

        protected override bool TryGetExpressionBody(SyntaxNode node, out SyntaxNode expression)
        {
            expression = node switch
            {
                // For methods and local functions ExpressionBody is an ArrowExpressionClauseSyntax so we pull the real expression out
                MethodDeclarationSyntax method => method.ExpressionBody?.Expression,
                LocalFunctionStatementSyntax localFunction => localFunction.ExpressionBody?.Expression,
                AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.ExpressionBody,
                _ => throw ExceptionUtilities.Unreachable
            };

            return expression != null;
        }

        protected override SyntaxNode ConvertToBlockBody(SyntaxNode node, SyntaxNode expressionBody, SyntaxEditor editor)
        {
            if (InitializeParameterHelpers.TryConvertExpressionBodyToStatement(expressionBody, SyntaxFactory.Token(SyntaxKind.SemicolonToken), false, out var statement))
            {
                var block = SyntaxFactory.Block(statement);
                return node switch
                {
                    MethodDeclarationSyntax method => method.WithBody(block).WithExpressionBody(null).WithSemicolonToken(default),
                    LocalFunctionStatementSyntax localFunction => localFunction.WithBody(block).WithExpressionBody(null).WithSemicolonToken(default),
                    AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.WithBody(block).WithExpressionBody(null),
                    _ => throw ExceptionUtilities.Unreachable
                };
            }
            return null;
        }

        protected override SyntaxNode RemoveAsyncModifier(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes)
            => node switch
            {
                MethodDeclarationSyntax method => RemoveAsyncModifierHelpers.WithoutAsyncModifier(method, method.ReturnType),
                LocalFunctionStatementSyntax localFunction => RemoveAsyncModifierHelpers.WithoutAsyncModifier(localFunction, localFunction.ReturnType),
                AnonymousMethodExpressionSyntax method => RemoveAsyncModifierHelpers.WithoutAsyncModifier(method),
                ParenthesizedLambdaExpressionSyntax lambda => RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda),
                SimpleLambdaExpressionSyntax lambda => RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda),
                _ => node,
            };
    }
}
