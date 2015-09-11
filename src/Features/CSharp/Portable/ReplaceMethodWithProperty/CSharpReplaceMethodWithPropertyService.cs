using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty
{
    [ExportLanguageService(typeof(IReplaceMethodWithPropertyService), LanguageNames.CSharp), Shared]
    internal class CSharpReplaceMethodWithPropertyService : IReplaceMethodWithPropertyService
    {
        public string GetMethodName(SyntaxNode methodNode)
        {
            return ((MethodDeclarationSyntax)methodNode).Identifier.ValueText;
        }

        public SyntaxNode GetMethodDeclaration(SyntaxToken token)
        {
            var containingMethod = token.Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
            {
                return null;
            }

            var start = containingMethod.AttributeLists.Count > 0
                ? containingMethod.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingMethod.SpanStart;

            // Offer this refactoring anywhere in the signature of the method.
            var position = token.SpanStart;
            if (position < start || position > containingMethod.ParameterList.Span.End)
            {
                return null;
            }

            return containingMethod;
        }

        public SyntaxNode ConvertMethodToProperty(SyntaxNode methodNode, string propertyName)
        {
            var method = methodNode as MethodDeclarationSyntax;
            if (method == null)
            {
                return methodNode;
            }

            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            accessor = method.Body == null
                ? accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                : accessor.WithBody(method.Body);

            var property = SyntaxFactory.PropertyDeclaration(method.AttributeLists, method.Modifiers, method.ReturnType,
                method.ExplicitInterfaceSpecifier, identifier: SyntaxFactory.Identifier(propertyName),
                accessorList: SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(accessor)));

            if (method.ExpressionBody != null)
            {
                property = property.WithExpressionBody(method.ExpressionBody);
                property = property.WithSemicolonToken(method.SemicolonToken);
            }

            return property;
        }

        public void ReplaceReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
        {
            if (nameToken.Kind() == SyntaxKind.IdentifierToken)
            {
                var nameNode = nameToken.Parent as IdentifierNameSyntax;
                var newName = nameChanged
                    ? SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName).WithTriviaFrom(nameToken))
                    : nameNode;

                var invocation = nameNode?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                var invocationExpression = invocation?.Expression;
                if (IsInvocationName(nameNode, invocationExpression))
                {
                    // It was invoked.  Remove the invocation, and also change the name if necessary.
                    editor.ReplaceNode(invocation, invocation.Expression.ReplaceNode(nameNode, newName));
                }
                else
                {
                    // Wasn't invoked.  Change the name, but report a conflict.
                    var annotation = ConflictAnnotation.Create(CSharpFeaturesResources.NonInvokedMethodCannotBeReplacedWithProperty);
                    editor.ReplaceNode(nameNode, newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)));
                }
            }
        }

        private static bool IsInvocationName(IdentifierNameSyntax nameNode, ExpressionSyntax invocationExpression)
        {
            if (invocationExpression == nameNode)
            {
                return true;
            }

            if (nameNode.IsAnyMemberAccessExpressionName() && nameNode.Parent == invocationExpression)
            {
                return true;
            }

            return false;
        }
    }
}
