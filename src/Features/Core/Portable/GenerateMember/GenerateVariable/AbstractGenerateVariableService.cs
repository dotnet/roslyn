// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
    internal abstract partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax> :
        AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>, IGenerateVariableService
        where TService : AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
    {
        protected AbstractGenerateVariableService()
        {
        }

        protected abstract bool IsExplicitInterfaceGeneration(SyntaxNode node);
        protected abstract bool IsIdentifierNameGeneration(SyntaxNode node);

        protected abstract bool TryInitializeExplicitInterfaceState(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IPropertySymbol propertySymbol, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeIdentifierNameState(SemanticDocument document, TSimpleNameSyntax identifierName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression, out bool isInExecutableBlock, out bool isinConditionalAccessExpression);

        protected abstract bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, OptionSet options, SemanticModel semanticModel, CancellationToken cancellationToken, out SyntaxNode newRoot);

        public async Task<ImmutableArray<CodeAction>> GenerateVariableAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateVariable, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    return ImmutableArray<CodeAction>.Empty;
                }

                var actions = ArrayBuilder<CodeAction>.GetInstance();

                var canGenerateMember = CodeGenerator.CanAdd(document.Project.Solution, state.TypeToGenerateIn, cancellationToken);

                if (canGenerateMember)
                {
                    // prefer fields over properties (and vice versa) depending on the casing of the member.
                    // lowercase -> fields.  title case -> properties.
                    var name = state.IdentifierToken.ValueText;
                    if (char.IsUpper(name.ToCharArray().FirstOrDefault()))
                    {
                        AddPropertyCodeActions(actions, semanticDocument, state);
                        AddFieldCodeActions(actions, semanticDocument, state);
                    }
                    else
                    {

                        AddFieldCodeActions(actions, semanticDocument, state);
                        AddPropertyCodeActions(actions, semanticDocument, state);
                    }
                }

                AddLocalCodeActions(actions, document, state);
                AddParameterCodeActions(actions, document, state);

                if (actions.Count > 1)
                {
                    // Wrap the generate variable actions into a single top level suggestion
                    // so as to not clutter the list.
                    return ImmutableArray.Create<CodeAction>(new MyCodeAction(
                        string.Format(FeaturesResources.Generate_variable_0, state.IdentifierToken.ValueText),
                        actions.ToImmutableAndFree()));
                }

                return actions.ToImmutableAndFree();
            }
        }

        protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        {
            return false;
        }

        private void AddPropertyCodeActions(
            ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            if (state.IsInOutContext)
            {
                return;
            }

            if (state.IsConstant)
            {
                return;
            }

            if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface && state.IsStatic)
            {
                return;
            }

            var isOnlyReadAndIsInInterface = state.TypeToGenerateIn.TypeKind == TypeKind.Interface && !state.IsWrittenTo;

            if (isOnlyReadAndIsInInterface || state.IsInConstructor)
            {
                result.Add(new GenerateVariableCodeAction(
                    (TService)this, document, state, generateProperty: true,
                    isReadonly: true, isConstant: false, refKind: GetRefKindFromContext(state)));
            }

            GenerateWritableProperty(result, document, state);
        }

        private void GenerateWritableProperty(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            result.Add(new GenerateVariableCodeAction(
                (TService)this, document, state, generateProperty: true,
                isReadonly: false, isConstant: false, refKind: GetRefKindFromContext(state)));
        }

        private void AddFieldCodeActions(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            if (state.TypeToGenerateIn.TypeKind != TypeKind.Interface)
            {
                if (state.IsConstant)
                {
                    result.Add(new GenerateVariableCodeAction(
                        (TService)this, document, state, generateProperty: false,
                        isReadonly: false, isConstant: true, refKind: RefKind.None));
                }
                else
                {
                    if (!state.OfferReadOnlyFieldFirst)
                    {
                        GenerateWriteableField(result, document, state);
                    }

                    // If we haven't written to the field, or we're in the constructor for the type
                    // we're writing into, then we can generate this field read-only.
                    if (!state.IsWrittenTo || state.IsInConstructor)
                    {
                        result.Add(new GenerateVariableCodeAction(
                            (TService)this, document, state, generateProperty: false,
                            isReadonly: true, isConstant: false, refKind: RefKind.None));
                    }

                    if (state.OfferReadOnlyFieldFirst)
                    {
                        GenerateWriteableField(result, document, state);
                    }
                }
            }
        }

        private void GenerateWriteableField(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            result.Add(new GenerateVariableCodeAction(
                (TService)this, document, state, generateProperty: false,
                isReadonly: false, isConstant: false, refKind: RefKind.None));
        }

        private void AddLocalCodeActions(ArrayBuilder<CodeAction> result, Document document, State state)
        {
            if (state.CanGenerateLocal())
            {
                result.Add(new GenerateLocalCodeAction((TService)this, document, state));
            }
        }

        private void AddParameterCodeActions(ArrayBuilder<CodeAction> result, Document document, State state)
        {
            if (state.CanGenerateParameter())
            {
                result.Add(new GenerateParameterCodeAction(document, state, includeOverridesAndImplementations: false));

                if (AddParameterService.Instance.HasCascadingDeclarations(state.ContainingMethod))
                    result.Add(new GenerateParameterCodeAction(document, state, includeOverridesAndImplementations: true));
            }
        }

        private RefKind GetRefKindFromContext(State state)
        {
            if (state.IsInRefContext)
            {
                return RefKind.Ref;
            }
            else if (state.IsInInContext)
            {
                return RefKind.RefReadOnly;
            }
            else
            {
                return RefKind.None;
            }
        }

        private class MyCodeAction : CodeAction.CodeActionWithNestedActions
        {
            public MyCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions, isInlinable: true)
            {
            }
        }
    }
}
