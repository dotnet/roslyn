// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.RemoveAsyncModifier;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider;

namespace Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMethodSynchronous), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal partial class CSharpRemoveAsyncModifierCodeFixProvider : AbstractRemoveAsyncModifierCodeFixProvider
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

        protected override bool CanFix(KnownTypes knownTypes, IMethodSymbol methodSymbolOpt)
        => !methodSymbolOpt.ReturnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumerableOfTTypeOpt) &&
           !methodSymbolOpt.ReturnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumeratorOfTTypeOpt);

        protected override SyntaxNode ChangeReturnStatements(SyntaxNode node, SyntaxGenerator generator, KnownTypes knownTypes)
        {
            var converter = new ConvertReturnsToTaskRewriter(generator, knownTypes._taskType);

            return node switch
            {
                MethodDeclarationSyntax method => FixMethodReturns(method, converter),
                LocalFunctionStatementSyntax localFunction => FixLocalFunctionReturns(localFunction, converter),
                AnonymousMethodExpressionSyntax method => method.WithBody((CSharpSyntaxNode)converter.Visit(method.Body)),
                ParenthesizedLambdaExpressionSyntax lambda => FixParenthesizedLamdaExpressionReturn(lambda, converter),
                SimpleLambdaExpressionSyntax lambda => lambda.WithBody((CSharpSyntaxNode)converter.Visit(lambda.Body)),
                _ => node,
            };
        }

        private static ParenthesizedLambdaExpressionSyntax FixParenthesizedLamdaExpressionReturn(ParenthesizedLambdaExpressionSyntax lambda, ConvertReturnsToTaskRewriter converter)
        {
            if (lambda.Block != null)
            {
                return lambda.WithBlock((BlockSyntax)converter.Visit(lambda.Block));
            }
            else
            {
                return lambda.WithExpressionBody(converter.WrapWithTaskFromResult(lambda.ExpressionBody));
            }
        }

        private static MethodDeclarationSyntax FixMethodReturns(MethodDeclarationSyntax method, ConvertReturnsToTaskRewriter converter)
        {
            if (method.Body != null)
            {
                return method.WithBody((BlockSyntax)converter.Visit(method.Body));
            }
            else
            {
                return method.WithExpressionBody((ArrowExpressionClauseSyntax)converter.Visit(method.ExpressionBody));
            }
        }

        private static LocalFunctionStatementSyntax FixLocalFunctionReturns(LocalFunctionStatementSyntax localFunction, ConvertReturnsToTaskRewriter converter)
        {
            if (localFunction.Body != null)
            {
                return localFunction.WithBody((BlockSyntax)converter.Visit(localFunction.Body));
            }
            else
            {
                return localFunction.WithExpressionBody((ArrowExpressionClauseSyntax)converter.Visit(localFunction.ExpressionBody));
            }
        }

        protected override SyntaxNode RemoveAsyncModifier(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes) => node switch
        {
            MethodDeclarationSyntax method => FixMethod(method),
            LocalFunctionStatementSyntax localFunction => FixLocalFunction(localFunction),
            AnonymousMethodExpressionSyntax method => CSharpMakeMethodSynchronousCodeFixProvider.FixAnonymousMethod(method),
            ParenthesizedLambdaExpressionSyntax lambda => CSharpMakeMethodSynchronousCodeFixProvider.FixParenthesizedLambda(lambda),
            SimpleLambdaExpressionSyntax lambda => CSharpMakeMethodSynchronousCodeFixProvider.FixSimpleLambda(lambda),
            _ => node,
        };

        private static SyntaxNode FixMethod(MethodDeclarationSyntax method)
        {
            var returnType = method.ReturnType;
            var newModifiers = CSharpMakeMethodSynchronousCodeFixProvider.FixMethodModifiers(method.Modifiers, ref returnType);
            return method.WithReturnType(returnType).WithModifiers(newModifiers);
        }

        private static SyntaxNode FixLocalFunction(LocalFunctionStatementSyntax localFunction)
        {
            var returnType = localFunction.ReturnType;
            var newModifiers = CSharpMakeMethodSynchronousCodeFixProvider.FixMethodModifiers(localFunction.Modifiers, ref returnType);
            return localFunction.WithReturnType(returnType).WithModifiers(newModifiers);
        }
    }
}
