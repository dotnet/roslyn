using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
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

        protected abstract TExpressionSyntax UnwrapCompoundAssignment(SyntaxNode compoundAssignment, TExpressionSyntax readExpression);

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
                    ReplaceRead(
                        FeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);
                }
                else if (_syntaxFacts.IsAttributeNamedArgumentIdentifier(_expression))
                {
                    // Can't replace a property used in an attribute argument.
                    var newIdentifierName = AddConflictAnnotation(_identifierName,
                        FeaturesResources.Property_cannot_safely_be_replaced_with_a_method_call);

                    _editor.ReplaceNode(_identifierName, newIdentifierName);
                }
                else if (_syntaxFacts.IsLeftSideOfAssignment(_expression))
                {
                    // We're only being written to here.  This is safe to replace with a call to the 
                    // setter.
                    ReplaceWrite(writeValue: (TExpressionSyntax)_syntaxFacts.GetRightHandSideOfAssignment(_expression.Parent));
                }
                else if (_syntaxFacts.IsLeftSideOfAnyAssignment(_expression))
                {
                    HandleCompoundAssignExpression();
                }
                else if (_syntaxFacts.IsOperandOfIncrementOrDecrementExpression(_expression))
                {
                    // We're being read from and written to (i.e. Prop++), we need to replace with a
                    // Get and a Set call.
                    var readExpression = GetReadExpression(conflictMessage: null);
                    var literalOne = Generator.LiteralExpression(1);

                    var writeValue = _syntaxFacts.IsOperandOfIncrementExpression(_expression)
                        ? Generator.AddExpression(readExpression, literalOne)
                        : Generator.SubtractExpression(readExpression, literalOne);

                    ReplaceWrite((TExpressionSyntax)writeValue);
                }
                else if (_syntaxFacts.IsInferredAnonymousObjectMemberDeclarator(_expression.Parent)) //.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
                {
                    // If we have:   new { this.Prop }.  We need ot convert it to:
                    //               new { Prop = this.GetProp() }
                    var declarator = _expression.Parent;
                    var readExpression = GetReadExpression(conflictMessage: null);

                    var newDeclarator = Generator.NamedAnonymousObjectMemberDeclarator(
                        _identifierName.WithoutTrivia(),
                        readExpression);

                    _editor.ReplaceNode(declarator, newDeclarator);
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

            private void HandleCompoundAssignExpression()
            {
                // We're being read from and written to from a compound assignment 
                // (i.e. Prop *= X), we need to replace with a Get and a Set call.

                var readExpression = GetReadExpression(conflictMessage: null);

                // Convert "Prop *= X" into "Prop * X".
                var writeValue = _service.UnwrapCompoundAssignment(_expression.Parent, readExpression);

                ReplaceWrite(writeValue);
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
