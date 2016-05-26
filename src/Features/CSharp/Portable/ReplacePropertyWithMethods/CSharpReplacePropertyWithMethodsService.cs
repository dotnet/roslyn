using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
            SyntaxNode declaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName)
        {
            var propertyDeclaration = declaration as PropertyDeclarationSyntax;
            if (propertyDeclaration == null)
            {
                return;
            }

            var members = ConvertPropertyToMembers(
                semanticModel, editor.Generator, property, 
                propertyDeclaration, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName);

            if (property.ContainingType.TypeKind == TypeKind.Interface)
            {
                members = members.OfType<MethodDeclarationSyntax>()
                                 .Select(editor.Generator.AsInterfaceMember).ToList();
            }

            editor.InsertAfter(propertyDeclaration, members);
            editor.RemoveNode(propertyDeclaration);
        }

        private List<SyntaxNode> ConvertPropertyToMembers(
            SemanticModel semanticModel, SyntaxGenerator generator, 
            IPropertySymbol property, PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName)
        {
            var result = new List<SyntaxNode>();

            if (propertyBackingField != null)
            {
                var initializer = propertyDeclaration.Initializer?.Value;
                result.Add(generator.FieldDeclaration(propertyBackingField, initializer));
            }

            var getMethod = property.GetMethod;
            if (getMethod != null)
            {
                result.Add(GetGetMethod(
                    generator, propertyDeclaration, propertyBackingField, getMethod, desiredGetMethodName));
            }

            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                result.Add(GetSetMethod(generator, propertyDeclaration, setMethod, desiredSetMethodName));
            }

            return result;
        }

        private static SyntaxNode GetSetMethod(
            SyntaxGenerator generator, 
            PropertyDeclarationSyntax propertyDeclaration, 
            IMethodSymbol setMethod,
            string desiredSetMethodName)
        {
            var setAccessorDeclaration = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(
                a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

            var method = generator.MethodDeclaration(
                setMethod, desiredSetMethodName, setAccessorDeclaration?.Body?.Statements);
            return method;
        }

        private static SyntaxNode GetGetMethod(
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName)
        {
            var statements = new List<SyntaxNode>();

            if (propertyBackingField != null)
            {
                statements.Add(generator.ReturnStatement(
                    generator.MemberAccessExpression(
                        generator.ThisExpression(), propertyBackingField.Name)));
            }
            else if (propertyDeclaration.ExpressionBody != null)
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

            return generator.MethodDeclaration(getMethod, desiredGetMethodName, statements);
        }

        public void ReplaceReference(
            SyntaxEditor editor, SyntaxToken nameToken, 
            IPropertySymbol property, IFieldSymbol propertyBackingField,
            string desiredGetMethodName, string desiredSetMethodName)
        {
            var referenceReplacer = new ReferenceReplacer(
                editor, nameToken, property, propertyBackingField, desiredGetMethodName, desiredSetMethodName);
            referenceReplacer.Do();
        }

        private static IdentifierNameSyntax AddConflictAnnotation(IdentifierNameSyntax name, string conflictMessage)
        {
            return name.WithIdentifier(AddConflictAnnotation(name.Identifier, conflictMessage));
        }

        private static SyntaxToken AddConflictAnnotation(SyntaxToken token, string conflictMessage)
        {
            if (conflictMessage != null)
            {
                token = token.WithAdditionalAnnotations(ConflictAnnotation.Create(conflictMessage));
            }

            return token;
        }

        private struct ReferenceReplacer
        {
            private readonly SyntaxEditor _editor;
            private readonly SyntaxToken _nameToken;
            private readonly IPropertySymbol _property;
            private readonly IFieldSymbol _propertyBackingField;
            private readonly string _desiredGetMethodName;
            private readonly string _desiredSetMethodName;

            private readonly IdentifierNameSyntax _identifierName;
            private readonly ExpressionSyntax _expression;

            public ReferenceReplacer(
                SyntaxEditor editor, SyntaxToken nameToken, 
                IPropertySymbol property, IFieldSymbol propertyBackingField,
                string desiredGetMethodName,
                string desiredSetMethodName)
            {
                _editor = editor;
                _nameToken = nameToken;
                _property = property;
                _propertyBackingField = propertyBackingField;
                _desiredGetMethodName = desiredGetMethodName;
                _desiredSetMethodName = desiredSetMethodName;

                _identifierName = (IdentifierNameSyntax)nameToken.Parent;
                _expression = _identifierName;
                if (_expression.IsRightSideOfDotOrArrow())
                {
                    _expression = _expression.Parent as ExpressionSyntax;
                }
            }

            public void Do()
            {
                if (_expression.IsInOutContext() || _expression.IsInRefContext())
                {
                    // Code wasn't legal (you can't reference a property in an out/ref position in C#).
                    // Just replace this with a simple GetCall, but mark it so it's clear there's an error.
                    ReplaceRead(
                        CSharpFeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);
                }
                else if (_expression.IsAttributeNamedArgumentIdentifier())
                {
                    // Can't replace a property used in an attribute argument.
                    var newIdentifierName = AddConflictAnnotation(_identifierName,
                        CSharpFeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);

                    _editor.ReplaceNode(_identifierName, newIdentifierName);
                }
                else if (_expression.IsLeftSideOfAssignExpression())
                {
                    // We're only being written to here.  This is safe to replace with a call to the 
                    // setter.
                    ReplaceWrite(writeValue: ((AssignmentExpressionSyntax)_expression.Parent).Right);
                }
                else if (_expression.IsLeftSideOfAnyAssignExpression())
                {
                    HandleAssignExpression();
                }
                else if (_expression.IsOperandOfIncrementOrDecrementExpression())
                {
                    // We're being read from and written to (i.e. Prop++), we need to replace with a
                    // Get and a Set call.
                    var parent = _expression.Parent;
                    var operatorKind = parent.IsKind(SyntaxKind.PreIncrementExpression) || parent.IsKind(SyntaxKind.PostIncrementExpression)
                        ? SyntaxKind.AddExpression
                        : SyntaxKind.SubtractExpression;

                    ReplaceReadAndWrite(operatorKind,
                        writeValue: SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
                        conflictMessage: null);
                }
                else if (_expression.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
                {
                    // If we have:   new { this.Prop }.  We need ot convert it to:
                    //               new { Prop = this.GetProp() }
                    var declarator = (AnonymousObjectMemberDeclaratorSyntax)_expression.Parent;
                    var readExpression = GetReadExpression(conflictMessage: null);

                    if (declarator.NameEquals != null)
                    {
                        // We already have the form: new { Prop = ... }
                        // We only need to replace the right side of the equals.
                        _editor.ReplaceNode(_expression, readExpression);
                    }
                    else
                    {
                        var newDeclarator =
                            declarator.WithNameEquals(SyntaxFactory.NameEquals(_identifierName.WithoutTrivia()))
                                      .WithExpression(readExpression);
                        _editor.ReplaceNode(declarator, newDeclarator);
                    }
                }
                else
                {
                    // No writes.  Replace this with an appropriate read.
                    ReplaceRead(conflictMessage: null);
                }
            }

            private void ReplaceRead(string conflictMessage)
            {
                var readExpression = GetReadExpression(conflictMessage);
                _editor.ReplaceNode(_expression, readExpression);
            }

            private void ReplaceWrite(ExpressionSyntax writeValue)
            {
                var writeExpression = GetWriteExpression(writeValue);
                _editor.ReplaceNode(_expression.Parent, writeExpression);
            }

            private ExpressionSyntax GetReadExpression(
                string conflictMessage)
            {
                if (ShouldReadFromBackingField())
                {
                    var newIdentifierToken = AddConflictAnnotation(SyntaxFactory.Identifier(_propertyBackingField.Name), conflictMessage);
                    var newIdentifierName = _identifierName.WithIdentifier(newIdentifierToken);

                    return _expression.ReplaceNode(_identifierName, newIdentifierName);
                }
                else
                {
                    return GetGetInvocationExpression(conflictMessage);
                }
            }

            private ExpressionSyntax GetWriteExpression(ExpressionSyntax writeValue)
            {
                if (ShouldWriteToBackingField())
                {
                    var newIdentifierName = _identifierName.WithIdentifier(
                        SyntaxFactory.Identifier(_propertyBackingField.Name));

                    return SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        _expression.ReplaceNode(_identifierName, newIdentifierName),
                        writeValue);
                }
                else
                {
                    return GetSetInvocationExpression(writeValue);
                }
            }

            private ExpressionSyntax GetGetInvocationExpression(
                string conflictMessage)
            {
                return GetInvocationExpression(_desiredGetMethodName, argument: null, conflictMessage: conflictMessage);
            }

            private ExpressionSyntax GetInvocationExpression(
                string desiredName, ArgumentSyntax argument, string conflictMessage)
            {
                var newIdentifier = AddConflictAnnotation(
                    SyntaxFactory.Identifier(desiredName), conflictMessage);

                var updatedExpression = _expression.ReplaceNode(
                    _identifierName,
                    SyntaxFactory.IdentifierName(newIdentifier)
                                 .WithLeadingTrivia(_identifierName.GetLeadingTrivia()));

                var invocation = SyntaxFactory.InvocationExpression(updatedExpression)
                                              .WithTrailingTrivia(_identifierName.GetTrailingTrivia());

                if (argument != null)
                {
                    invocation = invocation.AddArgumentListArguments(argument);
                }

                return invocation;
            }

            private bool ShouldReadFromBackingField()
            {
                return _propertyBackingField != null && _property.GetMethod == null;
            }

            private ExpressionSyntax GetSetInvocationExpression(
                ExpressionSyntax writeValue, string conflictMessage = null)
            {
                return GetInvocationExpression(_desiredSetMethodName, argument: SyntaxFactory.Argument(writeValue), conflictMessage: conflictMessage);
            }

            private bool ShouldWriteToBackingField()
            {
                return _propertyBackingField != null && _property.SetMethod == null;
            }

            private void HandleAssignExpression()
            {
                // We're being read from and written to from a compound assignment 
                // (i.e. Prop *= X), we need to replace with a Get and a Set call.
                var parent = (AssignmentExpressionSyntax)_expression.Parent;

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

                ReplaceReadAndWrite(
                    operatorKind, writeValue: parent.Right.Parenthesize(), conflictMessage: null);
            }

            private void ReplaceReadAndWrite(
                SyntaxKind operatorKind,
                ExpressionSyntax writeValue,
                string conflictMessage)
            {
                ReplaceWrite(writeValue: SyntaxFactory.BinaryExpression(
                    kind: operatorKind,
                    left: GetReadExpression(conflictMessage),
                    right: writeValue));
            }
        }
    }
}