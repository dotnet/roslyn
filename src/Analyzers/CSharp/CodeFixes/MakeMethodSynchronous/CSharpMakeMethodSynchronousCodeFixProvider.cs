// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMethodSynchronous), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpMakeMethodSynchronousCodeFixProvider : AbstractMakeMethodSynchronousCodeFixProvider
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeMethodSynchronousCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbol, SyntaxNode node, KnownTaskTypes knownTypes)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(methodSymbol, method, knownTypes);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(methodSymbol, localFunction, knownTypes);
                case AnonymousMethodExpressionSyntax method: return RemoveAsyncModifierHelpers.WithoutAsyncModifier(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda);
                case SimpleLambdaExpressionSyntax lambda: return RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda);
                default: return node;
            }
        }
        private static SyntaxNode FixMethod(IMethodSymbol methodSymbol, MethodDeclarationSyntax method, KnownTaskTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, method.ReturnType, knownTypes);
            return RemoveAsyncModifierHelpers.WithoutAsyncModifier(method, newReturnType);
        }

        private static SyntaxNode FixLocalFunction(IMethodSymbol methodSymbol, LocalFunctionStatementSyntax localFunction, KnownTaskTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, localFunction.ReturnType, knownTypes);
            return RemoveAsyncModifierHelpers.WithoutAsyncModifier(localFunction, newReturnType);
        }

        private static TypeSyntax FixMethodReturnType(IMethodSymbol methodSymbol, TypeSyntax returnTypeSyntax, KnownTaskTypes knownTypes)
        {
            var newReturnType = returnTypeSyntax;

            var returnType = methodSymbol.ReturnType;
            if (returnType.OriginalDefinition.Equals(knownTypes.TaskType))
            {
                // If the return type is Task, then make the new return type "void".
                newReturnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).WithTriviaFrom(returnTypeSyntax);
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes.TaskOfTType))
            {
                // If the return type is Task<T>, then make the new return type "T".
                newReturnType = returnType.GetTypeArguments()[0].GenerateTypeSyntax().WithTriviaFrom(returnTypeSyntax);
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes.IAsyncEnumerableOfTType) &&
                knownTypes.IEnumerableOfTType != null)
            {
                // If the return type is IAsyncEnumerable<T>, then make the new return type IEnumerable<T>.
                newReturnType = knownTypes.IEnumerableOfTType.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes.IAsyncEnumeratorOfTType) &&
                knownTypes.IEnumeratorOfTType != null)
            {
                // If the return type is IAsyncEnumerator<T>, then make the new return type IEnumerator<T>.
                newReturnType = knownTypes.IEnumeratorOfTType.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }

            return newReturnType;
        }
    }
}
