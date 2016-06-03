using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    internal abstract class AbstractReplacePropertyWithMethodsService<TIdentifierNameSyntax, TExpressionSyntax, TStatementSyntax>
        : IReplacePropertyWithMethodsService
        where TIdentifierNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
    {
        public abstract SyntaxNode GetPropertyDeclaration(SyntaxToken token);
        public abstract SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
        public abstract IList<SyntaxNode> GetReplacementMembers(Document document, IPropertySymbol property, SyntaxNode propertyDeclaration, IFieldSymbol propertyBackingField, string desiredGetMethodName, string desiredSetMethodName, CancellationToken cancellationToken);

        protected static SyntaxNode GetFieldReference(SyntaxGenerator generator, IFieldSymbol propertyBackingField)
        {
            var through = propertyBackingField.IsStatic
                ? generator.TypeExpression(propertyBackingField.ContainingType)
                : generator.ThisExpression();

            return generator.MemberAccessExpression(through, propertyBackingField.Name);
        }

        public async Task ReplaceReferenceAsync(
            Document document,
            SyntaxEditor editor, SyntaxToken nameToken,
            IPropertySymbol property, IFieldSymbol propertyBackingField,
            string desiredGetMethodName, string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var referenceReplacer = new ReferenceReplacer(
                this, semanticModel, syntaxFacts, semanticFacts,
                editor, nameToken, property, propertyBackingField, 
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken);
            referenceReplacer.Do();
        }

        private struct ReferenceReplacer
        {
            private readonly AbstractReplacePropertyWithMethodsService<TIdentifierNameSyntax, TExpressionSyntax, TStatementSyntax> _service;
            private readonly SemanticModel _semanticModel;
            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly ISemanticFactsService _semanticFacts;
            private readonly SyntaxEditor _editor;
            private readonly SyntaxToken _nameToken;
            private readonly IPropertySymbol _property;
            private readonly IFieldSymbol _propertyBackingField;
            private readonly string _desiredGetMethodName;
            private readonly string _desiredSetMethodName;

            private readonly TIdentifierNameSyntax _identifierName;
            private readonly TExpressionSyntax _expression;
            private readonly CancellationToken _cancellationToken;

            public ReferenceReplacer(
                AbstractReplacePropertyWithMethodsService<TIdentifierNameSyntax, TExpressionSyntax, TStatementSyntax> service,
                SemanticModel semanticModel,
                ISyntaxFactsService syntaxFacts,
                ISemanticFactsService semanticFacts,
                SyntaxEditor editor, SyntaxToken nameToken,
                IPropertySymbol property, IFieldSymbol propertyBackingField,
                string desiredGetMethodName,
                string desiredSetMethodName,
                CancellationToken cancellationToken)
            {
                _service = service;
                _semanticModel = semanticModel;
                _syntaxFacts = syntaxFacts;
                _semanticFacts = semanticFacts;
                _editor = editor;
                _nameToken = nameToken;
                _property = property;
                _propertyBackingField = propertyBackingField;
                _desiredGetMethodName = desiredGetMethodName;
                _desiredSetMethodName = desiredSetMethodName;
                _cancellationToken = cancellationToken;

                _identifierName = (TIdentifierNameSyntax)nameToken.Parent;
                _expression = _identifierName;
                if (_syntaxFacts.IsMemberAccessExpressionName(_expression))
                {
                    _expression = _expression.Parent as TExpressionSyntax;
                }
            }

            private SyntaxGenerator Generator => _editor.Generator;

            public void Do()
            {
                if (_semanticFacts.IsInOutContext(_semanticModel, _expression, _cancellationToken) ||
                    _semanticFacts.IsInRefContext(_semanticModel, _expression, _cancellationToken))
                {
                    // Code wasn't legal (you can't reference a property in an out/ref position in C#).
                    // Just replace this with a simple GetCall, but mark it so it's clear there's an error.
                    ReplaceRead(FeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);
                    return;
                }

                if (_syntaxFacts.IsAttributeNamedArgumentIdentifier(_expression))
                {
                    // Can't replace a property used in an attribute argument.
                    var newIdentifierName = AddConflictAnnotation(_identifierName,
                        FeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);

                    _editor.ReplaceNode(_identifierName, newIdentifierName);
                    return;
                }

                var incrementOrDecrementExpression = _semanticModel.GetOperation(_expression.Parent) as IIncrementExpression;
                if (incrementOrDecrementExpression != null)
                {
                    // We're being read from and written to (i.e. Prop++), we need to replace with a
                    // Get and a Set call.
                    var readExpression = GetReadExpression(conflictMessage: null);
                    var literalOne = Generator.LiteralExpression(1);

                    var writeValue = IsIncrementExpression(incrementOrDecrementExpression)
                        ? Generator.AddExpression(readExpression, literalOne)
                        : Generator.SubtractExpression(readExpression, literalOne);

                    ReplaceWrite((TExpressionSyntax)writeValue);
                    return;
                }

                var compoundAssignment = _semanticModel.GetOperation(_expression.Parent) as ICompoundAssignmentExpression;
                if (compoundAssignment != null)
                {
                    HandleCompoundAssignExpression(compoundAssignment);
                    return;
                }

                var assigmentExpression = _semanticModel.GetOperation(_expression.Parent) as IAssignmentExpression;
                if (assigmentExpression != null)
                {
                    // We're only being written to here.  This is safe to replace with a call to the 
                    // setter.
                    ReplaceWrite(writeValue: (TExpressionSyntax)assigmentExpression.Value.Syntax);
                    return;
                }

                if (_syntaxFacts.IsInferredAnonymousObjectMemberDeclarator(_expression.Parent)) //.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
                {
                    // If we have:   new { this.Prop }.  We need ot convert it to:
                    //               new { Prop = this.GetProp() }
                    var declarator = _expression.Parent;
                    var readExpression = GetReadExpression(conflictMessage: null);

                    var newDeclarator = Generator.NamedAnonymousObjectMemberDeclarator(
                        _identifierName.WithoutTrivia(),
                        readExpression);

                    _editor.ReplaceNode(declarator, newDeclarator);
                    return;
                }

                // No writes.  Replace this with an appropriate read.
                ReplaceRead(conflictMessage: null);
            }

#if false
            private bool IsOperandOfIncrementOrDecrementExpression(TExpressionSyntax expression)
            {
                var operation = _semanticModel.GetOperation(expression.Parent) as IIncrementExpression;
                if (operation != null)
                {
                    switch (operation.GetSimpleUnaryOperationKind())
                    {
                        case SimpleUnaryOperationKind.PostfixDecrement:
                        case SimpleUnaryOperationKind.PostfixIncrement:
                        case SimpleUnaryOperationKind.PrefixDecrement:
                        case SimpleUnaryOperationKind.PrefixIncrement:
                            return true;
                    }
                }

                return false;
            }
#endif

            private bool IsIncrementExpression(IIncrementExpression expression)
            {
                switch (expression.GetSimpleUnaryOperationKind())
                {
                    case SimpleUnaryOperationKind.PostfixIncrement:
                    case SimpleUnaryOperationKind.PrefixIncrement:
                        return true;
                }

                return false;
            }

            private void ReplaceRead(string conflictMessage)
            {
                var readExpression = GetReadExpression(conflictMessage);
                _editor.ReplaceNode(_expression, readExpression);
            }

            private void ReplaceWrite(TExpressionSyntax writeValue)
            {
                var writeExpression = GetWriteExpression(writeValue);
                if (_expression.Parent is TStatementSyntax)
                {
                    writeExpression = Generator.ExpressionStatement(writeExpression);
                }

                _editor.ReplaceNode(_expression.Parent, writeExpression);
            }

            private TExpressionSyntax GetReadExpression(
                string conflictMessage)
            {
                if (ShouldReadFromBackingField())
                {
                    var newIdentifierToken = AddConflictAnnotation(Generator.Identifier(_propertyBackingField.Name), conflictMessage);
                    var newIdentifierName = Generator.IdentifierName(newIdentifierToken).WithTriviaFrom(_identifierName);

                    return _expression.ReplaceNode(_identifierName, newIdentifierName);
                }
                else
                {
                    return GetGetInvocationExpression(conflictMessage);
                }
            }

            private SyntaxNode GetWriteExpression(TExpressionSyntax writeValue)
            {
                if (ShouldWriteToBackingField())
                {
                    var newIdentifierName = 
                        Generator.IdentifierName(_propertyBackingField.Name)
                                 .WithTriviaFrom(_identifierName);

                    return Generator.AssignmentStatement(
                        _expression.ReplaceNode(_identifierName, newIdentifierName),
                        writeValue);
                }
                else
                {
                    return GetSetInvocationExpression(writeValue);
                }
            }

            private TExpressionSyntax GetGetInvocationExpression(
                string conflictMessage)
            {
                return GetInvocationExpression(_desiredGetMethodName, argument: null, conflictMessage: conflictMessage);
            }

            private TExpressionSyntax GetInvocationExpression(
                string desiredName, SyntaxNode argument, string conflictMessage)
            {
                var newIdentifier = AddConflictAnnotation(
                    Generator.Identifier(desiredName), conflictMessage);

                var updatedExpression = _expression.ReplaceNode(
                    _identifierName,
                    Generator.IdentifierName(newIdentifier)
                             .WithLeadingTrivia(_identifierName.GetLeadingTrivia()));

                var arguments = argument == null
                    ? SpecializedCollections.EmptyEnumerable<SyntaxNode>()
                    : SpecializedCollections.SingletonEnumerable(argument);
                var invocation = Generator.InvocationExpression(updatedExpression, arguments)
                                          .WithTrailingTrivia(_identifierName.GetTrailingTrivia());

                return (TExpressionSyntax)invocation;
            }

            private bool ShouldReadFromBackingField()
            {
                return _propertyBackingField != null && _property.GetMethod == null;
            }

            private SyntaxNode GetSetInvocationExpression(
                TExpressionSyntax writeValue, string conflictMessage = null)
            {
                return GetInvocationExpression(_desiredSetMethodName, 
                    argument: Generator.Argument(writeValue), conflictMessage: conflictMessage);
            }

            private bool ShouldWriteToBackingField()
            {
                return _propertyBackingField != null && _property.SetMethod == null;
            }

            private void HandleCompoundAssignExpression(ICompoundAssignmentExpression compoundExpression)
            {
                // We're being read from and written to from a compound assignment 
                // (i.e. Prop *= X), we need to replace with a Get and a Set call.

                var readExpression = GetReadExpression(conflictMessage: null);

                // Convert "Prop *= X" into "Prop * X".
                var writeValue = (TExpressionSyntax)UnwrapCompoundExpression(compoundExpression, readExpression);

                // Now write "Prop * X" back into "Prop".
                ReplaceWrite(writeValue);
            }

            private SyntaxNode UnwrapCompoundExpression(
                ICompoundAssignmentExpression compoundExpression,
                TExpressionSyntax readExpression)
            {
                var right = _syntaxFacts.Parenthesize(compoundExpression.Value.Syntax);

                switch (compoundExpression.GetSimpleBinaryOperationKind())
                {
                    default:
                    case SimpleBinaryOperationKind.Add: return Generator.AddExpression(readExpression, right);
                    case SimpleBinaryOperationKind.Subtract: return Generator.SubtractExpression(readExpression, right);
                    case SimpleBinaryOperationKind.Multiply: return Generator.MultiplyExpression(readExpression, right);
                    case SimpleBinaryOperationKind.Divide:return Generator.DivideExpression(readExpression, right);
                    case SimpleBinaryOperationKind.IntegerDivide:return Generator.DivideExpression(readExpression, right);
                    case SimpleBinaryOperationKind.Remainder: return Generator.ModuloExpression(readExpression, right);
                    case SimpleBinaryOperationKind.LeftShift: return Generator.LeftShiftExpression(readExpression, right);
                    case SimpleBinaryOperationKind.RightShift: return Generator.RightShiftExpression(readExpression, right);
                    case SimpleBinaryOperationKind.And: return Generator.BitwiseAndExpression(readExpression, right);
                    case SimpleBinaryOperationKind.Or: return Generator.BitwiseOrExpression(readExpression, right);
                    case SimpleBinaryOperationKind.ExclusiveOr: return Generator.ExclusiveOrExpression(readExpression, right);
                    case SimpleBinaryOperationKind.ConditionalAnd: return Generator.LogicalAndExpression(readExpression, right);
                    case SimpleBinaryOperationKind.ConditionalOr: return Generator.LogicalOrExpression(readExpression, right);
                }
            }

            private static TIdentifierNameSyntax AddConflictAnnotation(TIdentifierNameSyntax name, string conflictMessage)
            {
                return name.ReplaceToken(
                    name.GetFirstToken(),
                    AddConflictAnnotation(name.GetFirstToken(), conflictMessage));
            }

            private static SyntaxToken AddConflictAnnotation(SyntaxToken token, string conflictMessage)
            {
                if (conflictMessage != null)
                {
                    token = token.WithAdditionalAnnotations(ConflictAnnotation.Create(conflictMessage));
                }

                return token;
            }
        }
    }
}
