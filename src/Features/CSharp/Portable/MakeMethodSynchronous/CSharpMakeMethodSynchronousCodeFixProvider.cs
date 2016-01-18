using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeMethodSynchronousCodeFixProvider : AbstractMakeMethodSynchronousCodeFixProvider
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsMethodOrAnonymousFunction(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration) || node.IsAnyLambdaOrAnonymousMethod();
        }

        protected override SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbolOpt, SyntaxNode node, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            return node.TypeSwitch(
                (MethodDeclarationSyntax method) => FixMethod(methodSymbolOpt, method, taskType, taskOfTType),
                (AnonymousMethodExpressionSyntax method) => FixAnonymousMethod(method),
                (ParenthesizedLambdaExpressionSyntax lambda) => FixParenthesizedLambda(lambda),
                (SimpleLambdaExpressionSyntax lambda) => FixSimpleLambda(lambda),
                _ => node);
        }

        private SyntaxNode FixMethod(IMethodSymbol methodSymbol, MethodDeclarationSyntax method, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            var newReturnType = method.ReturnType;

            // If the return type is Task<T>, then make the new return type "T".
            // If it is Task, then make the new return type "void".
            if (methodSymbol.ReturnType.OriginalDefinition.Equals(taskType))
            {
                newReturnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).WithTriviaFrom(method.ReturnType);
            }
            else if (methodSymbol.ReturnType.OriginalDefinition.Equals(taskOfTType))
            {
                newReturnType = methodSymbol.ReturnType.GetTypeArguments()[0].GenerateTypeSyntax().WithTriviaFrom(method.ReturnType);
            }

            var asyncTokenIndex = method.Modifiers.IndexOf(SyntaxKind.AsyncKeyword);
            SyntaxTokenList newModifiers;
            if (asyncTokenIndex == 0)
            {
                // Have to move the trivia on teh async token appropriately.
                var asyncLeadingTrivia = method.Modifiers[0].LeadingTrivia;

                if (method.Modifiers.Count > 1)
                {
                    // Move the trivia to the next modifier;
                    newModifiers = method.Modifiers.Replace(
                        method.Modifiers[1],
                        method.Modifiers[1].WithPrependedLeadingTrivia(asyncLeadingTrivia));
                    newModifiers = newModifiers.RemoveAt(0);
                }
                else
                {
                    // move it to the return type.
                    newModifiers = method.Modifiers.RemoveAt(0);
                    newReturnType = newReturnType.WithPrependedLeadingTrivia(asyncLeadingTrivia);
                }
            }
            else
            {
                newModifiers = method.Modifiers.RemoveAt(asyncTokenIndex);
            }

            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixParenthesizedLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            return lambda.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixSimpleLambda(SimpleLambdaExpressionSyntax lambda)
        {
            return lambda.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixAnonymousMethod(AnonymousMethodExpressionSyntax method)
        {
            return method.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(method.AsyncKeyword.LeadingTrivia);
        }
    }
}
