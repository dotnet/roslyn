using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    [ExportLanguageService(typeof(IReplacePropertyWithMethodsService), LanguageNames.CSharp), Shared]
    internal class CSharpReplacePropertyWithMethodsService : IReplacePropertyWithMethodsService
    {
        public SyntaxNode GetPropertyDeclaration(SyntaxToken token)
        {
            var containingProperty = token.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (containingProperty == null)
            {
                return null;
            }

            var start = containingProperty.AttributeLists.Count > 0
                ? containingProperty.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingProperty.SpanStart;

            // Offer this refactoring anywhere in the signature of the method.
            var position = token.SpanStart;
            if (position < start || position > containingProperty.Identifier.Span.End)
            {
                return null;
            }

            return containingProperty;
        }

        public void ReplacePropertyWithMethod(
            SyntaxEditor editor,
            SemanticModel semanticModel,
            IPropertySymbol property,
            SyntaxNode declaration)
        {
            var propertyDeclaration = declaration as PropertyDeclarationSyntax;
            if (propertyDeclaration == null)
            {
                return;
            }

            var methods = ConvertPropertyToMethods(semanticModel, editor.Generator, property, propertyDeclaration);

            if (methods.Count > 0)
            {
                editor.ReplaceNode(propertyDeclaration, methods[0]);
                if (methods.Count > 1)
                {
                    editor.InsertAfter(propertyDeclaration, methods[1]);
                }
            }
        }

        private List<SyntaxNode> ConvertPropertyToMethods(
            SemanticModel semanticModel, SyntaxGenerator generator, IPropertySymbol property, PropertyDeclarationSyntax propertyDeclaration)
        {
            var result = new List<SyntaxNode>();
            if (property.GetMethod != null)
            {
                var getAccessorDeclaration = propertyDeclaration.AccessorList.Accessors.First(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                result.Add(ConvertGetAccessorToMethod(
                    semanticModel, generator, property.GetMethod, getAccessorDeclaration));
            }

            if (property.SetMethod != null)
            {
                var setAccessorDeclaration = propertyDeclaration.AccessorList.Accessors.First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
                result.Add(ConvertSetAccessorToMethod(
                    semanticModel, generator, property.SetMethod, setAccessorDeclaration));
            }

            return result;
        }

        private SyntaxNode ConvertGetAccessorToMethod(
            SemanticModel semanticModel, SyntaxGenerator generator, IMethodSymbol getMethod, AccessorDeclarationSyntax getAccessorDeclaration)
        {
            return generator.MethodDeclaration(
                getMethod, "Get" + getMethod.AssociatedSymbol.Name, getAccessorDeclaration.Body.Statements);
        }

        private SyntaxNode ConvertSetAccessorToMethod(
            SemanticModel semanticModel, SyntaxGenerator generator, IMethodSymbol setMethod, AccessorDeclarationSyntax setAccessorDeclaration)
        {
            return generator.MethodDeclaration(setMethod, setAccessorDeclaration.Body.Statements);
        }

        public void ReplaceReference(SyntaxEditor editor, SyntaxToken nameToken)
        {
            var identifierName = (IdentifierNameSyntax)nameToken.Parent;
            var expression = (ExpressionSyntax)identifierName;
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }

            if (identifierName.IsOnlyWrittenTo())
            {
                // We're only being written to here.  This is safe to replace with a call to the 
                // setter.

            }
            else if (identifierName.IsWrittenTo())
            {
                // We're being read from and written to (i.e. Prop++), we need to replace with a
                // Get and a Set call.
            }
            else if (expression.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
            {
                // We have:   new { this.Prop }.  We need ot convert it to:
                //            new { Prop = this.GetProp() }
                var declarator = (AnonymousObjectMemberDeclaratorSyntax)expression.Parent;
                var getInvocation = GetGetInvocationExpression(nameToken);

                if (declarator.NameEquals != null)
                {
                    editor.ReplaceNode(
                        expression,
                        GetGetInvocationExpression(nameToken));
                }
                else
                {
                    var newDeclarator =
                        declarator.WithNameEquals(SyntaxFactory.NameEquals(identifierName.WithoutTrivia()))
                                  .WithExpression(getInvocation);
                    editor.ReplaceNode(declarator, newDeclarator);
                }
            }
            else
            {
                // No writes.  Replace this with a call to the getter.
                editor.ReplaceNode(
                    expression,
                    GetGetInvocationExpression(nameToken));
            }
        }

        private static ExpressionSyntax GetGetInvocationExpression(SyntaxToken nameToken)
        {
            var identifierName = (IdentifierNameSyntax)nameToken.Parent;
            var expression = (ExpressionSyntax)identifierName;
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }

            var updatedExpression = expression.ReplaceNode(
                identifierName,
                SyntaxFactory.IdentifierName("Get" + nameToken.ValueText)
                             .WithLeadingTrivia(identifierName.GetLeadingTrivia()));

            updatedExpression = SyntaxFactory.InvocationExpression(updatedExpression)
                                             .WithTrailingTrivia(identifierName.GetTrailingTrivia());
            return updatedExpression;
        }
    }
}