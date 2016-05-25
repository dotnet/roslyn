using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
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

            var methods = ConvertPropertyToMembers(semanticModel, editor.Generator, property, propertyDeclaration);

            editor.InsertAfter(propertyDeclaration, methods);
            editor.RemoveNode(propertyDeclaration);
        }

        private List<SyntaxNode> ConvertPropertyToMembers(
            SemanticModel semanticModel, SyntaxGenerator generator, IPropertySymbol property, PropertyDeclarationSyntax propertyDeclaration)
        {
            var result = new List<SyntaxNode>();

            var getMethod = property.GetMethod;
            if (getMethod != null)
            {
                result.Add(GetGetMethod(generator, propertyDeclaration, getMethod));
            }

            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                result.Add(GetSetMethod(generator, propertyDeclaration, setMethod));
            }

            return result;
        }

        private static SyntaxNode GetSetMethod(SyntaxGenerator generator, PropertyDeclarationSyntax propertyDeclaration, IMethodSymbol setMethod)
        {
            var setAccessorDeclaration = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(
                a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

            var method = generator.MethodDeclaration(
                setMethod, "Set" + setMethod.AssociatedSymbol.Name, setAccessorDeclaration?.Body?.Statements);
            return method;
        }

        private static SyntaxNode GetGetMethod(SyntaxGenerator generator, PropertyDeclarationSyntax propertyDeclaration, IMethodSymbol getMethod)
        {
            var statements = new List<SyntaxNode>();
            if (propertyDeclaration.ExpressionBody != null)
            {
                statements.Add(generator.ReturnStatement(propertyDeclaration.ExpressionBody.Expression));
            }
            else
            {
                var getAccessorDeclaration = propertyDeclaration.AccessorList?.Accessors.FirstOrDefault(
                    a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                if (getAccessorDeclaration?.Body != null)
                {
                    statements.AddRange(getAccessorDeclaration.Body.Statements);
                }
            }

            var methodDeclaration = generator.MethodDeclaration(
                getMethod, "Get" + getMethod.AssociatedSymbol.Name, statements);
            return methodDeclaration;
        }

        public void ReplaceReference(SyntaxEditor editor, SyntaxToken nameToken)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            if (expression.IsInOutContext() || expression.IsInRefContext())
            {
                // Code wasn't legal (you can't reference a property in an out/ref position in C#).
                // Just replace this with a simple GetCall, but mark it so it's clear there's an error.
                ReplaceWithGetInvocation(editor, nameToken, CSharpFeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);
            }
            else if (expression.IsAttributeNamedArgumentIdentifier())
            {
                // Can't replace a property used in an attribute argument.
                var newIdentifier = identifierName.Identifier.WithAdditionalAnnotations(
                    ConflictAnnotation.Create(CSharpFeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call));

                editor.ReplaceNode(identifierName, identifierName.WithIdentifier(newIdentifier));
            }
            else if (expression.IsLeftSideOfAssignExpression())
            {
                // We're only being written to here.  This is safe to replace with a call to the 
                // setter.
                var value = ((AssignmentExpressionSyntax)expression.Parent).Right;
                ReplaceWithSetInvocation(editor, nameToken, value);
            }
            else if (expression.IsLeftSideOfAnyAssignExpression())
            {
                HandleAssignExpression(editor, nameToken);
            }
            else if (expression.IsOperandOfIncrementOrDecrementExpression())
            {
                // We're being read from and written to (i.e. Prop++), we need to replace with a
                // Get and a Set call.
                var parent = expression.Parent;
                var operatorKind = parent.IsKind(SyntaxKind.PreIncrementExpression) || parent.IsKind(SyntaxKind.PostIncrementExpression)
                    ? SyntaxKind.AddExpression
                    : SyntaxKind.SubtractExpression;

                ReplaceWithGetAndSetInvocation(
                    editor, nameToken, operatorKind,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
            }
            else if (expression.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
            {
                // We have:   new { this.Prop }.  We need ot convert it to:
                //            new { Prop = this.GetProp() }
                var declarator = (AnonymousObjectMemberDeclaratorSyntax)expression.Parent;
                var getInvocation = GetGetInvocationExpression(nameToken);

                if (declarator.NameEquals != null)
                {
                    ReplaceWithGetInvocation(editor, nameToken);
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
                ReplaceWithGetInvocation(editor, nameToken);
            }
        }

        private void HandleAssignExpression(SyntaxEditor editor, SyntaxToken nameToken)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            // We're being read from and written to from a compound assignment 
            // (i.e. Prop *= X), we need to replace with a Get and a Set call.
            var parent = (AssignmentExpressionSyntax)expression.Parent;

            var operatorKind =
                parent.IsKind(SyntaxKind.OrAssignmentExpression) ? SyntaxKind.BitwiseOrExpression :
                parent.IsKind(SyntaxKind.AndAssignmentExpression) ? SyntaxKind.BitwiseAndExpression :
                parent.IsKind(SyntaxKind.ExclusiveOrAssignmentExpression) ? SyntaxKind.ExclusiveOrExpression :
                parent.IsKind(SyntaxKind.LeftShiftAssignmentExpression) ? SyntaxKind.LeftShiftExpression :
                parent.IsKind(SyntaxKind.RightShiftAssignmentExpression) ? SyntaxKind.RightShiftExpression :
                parent.IsKind(SyntaxKind.AddAssignmentExpression) ? SyntaxKind.AddExpression :
                parent.IsKind(SyntaxKind.SubtractAssignmentExpression) ? SyntaxKind.SubtractExpression :
                parent.IsKind(SyntaxKind.MultiplyAssignmentExpression) ? SyntaxKind.MultiplyExpression :
                parent.IsKind(SyntaxKind.DivideAssignmentExpression) ? SyntaxKind.DivideExpression :
                parent.IsKind(SyntaxKind.ModuloAssignmentExpression) ? SyntaxKind.ModuloExpression : SyntaxKind.None;

            ReplaceWithGetAndSetInvocation(
                editor, nameToken, operatorKind, parent.Right.Parenthesize());
        }

        private static void ReplaceWithGetAndSetInvocation(
            SyntaxEditor editor, SyntaxToken nameToken, 
            SyntaxKind operatorKind, ExpressionSyntax value,
            string conflictMessage = null)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            var getInvocation = GetGetInvocationExpression(nameToken, conflictMessage);
            var binaryExpression = SyntaxFactory.BinaryExpression(
                operatorKind, getInvocation, value);

            var setInvocation = GetSetInvocationExpression(
                nameToken, binaryExpression);

            editor.ReplaceNode(expression.Parent, setInvocation);
        }

        private static void ReplaceWithGetInvocation(
            SyntaxEditor editor, SyntaxToken nameToken, string conflictMessage = null)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            var invocation = GetGetInvocationExpression(nameToken, conflictMessage);
            editor.ReplaceNode(expression, invocation);
        }

        private static void ReplaceWithSetInvocation(
            SyntaxEditor editor, SyntaxToken nameToken, ExpressionSyntax value)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            var invocation = GetSetInvocationExpression(nameToken, value);
            editor.ReplaceNode(expression.Parent, invocation);
        }

        private static ExpressionSyntax GetGetInvocationExpression(
            SyntaxToken nameToken, string conflictMessage = null)
        {
            return GetInvocationExpression(nameToken, "Get" + nameToken.ValueText,
                argument: null, conflictMessage: conflictMessage);
        }

        private static ExpressionSyntax GetSetInvocationExpression(
            SyntaxToken nameToken, ExpressionSyntax expression, string conflictMessage = null)
        {
            return GetInvocationExpression(nameToken, "Set" + nameToken.ValueText,
                argument: SyntaxFactory.Argument(expression), conflictMessage: conflictMessage);
        }

        private static ExpressionSyntax GetInvocationExpression(
            SyntaxToken nameToken, string name, ArgumentSyntax argument, string conflictMessage = null)
        {
            IdentifierNameSyntax identifierName;
            ExpressionSyntax expression;
            GetIdentifierAndContextExpression(nameToken, out identifierName, out expression);

            var newIdentifier = SyntaxFactory.Identifier(name);

            if (conflictMessage != null)
            {
                newIdentifier = newIdentifier.WithAdditionalAnnotations(ConflictAnnotation.Create(conflictMessage));
            }

            var updatedExpression = expression.ReplaceNode(
                identifierName,
                SyntaxFactory.IdentifierName(newIdentifier)
                             .WithLeadingTrivia(identifierName.GetLeadingTrivia()));

            var invocation = SyntaxFactory.InvocationExpression(updatedExpression)
                                          .WithTrailingTrivia(identifierName.GetTrailingTrivia());

            if (argument != null)
            {
                invocation = invocation.AddArgumentListArguments(argument);
            }

            return invocation;
        }

        private static void GetIdentifierAndContextExpression(SyntaxToken nameToken, out IdentifierNameSyntax identifierName, out ExpressionSyntax expression)
        {
            identifierName = (IdentifierNameSyntax)nameToken.Parent;
            expression = (ExpressionSyntax)identifierName;
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }
        }
    }
}