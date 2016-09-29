// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

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

        protected abstract bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, OptionSet options, SemanticModel semanticModel, CancellationToken cancellationToken,  out SyntaxNode newRoot);

        public async Task<IEnumerable<CodeAction>> GenerateVariableAsync(
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
                    return SpecializedCollections.EmptyEnumerable<CodeAction>();
                }

                var actions = new List<CodeAction>();

                var canGenerateMember = CodeGenerator.CanAdd(document.Project.Solution, state.TypeToGenerateIn, cancellationToken);

                // prefer fields over properties (and vice versa) depending on the casing of the member.
                // lowercase -> fields.  title case -> properties.
                var name = state.IdentifierToken.ValueText;
                if (char.IsUpper(name.ToCharArray().FirstOrDefault()))
                {
                    if (canGenerateMember)
                    {
                        AddPropertyCodeActions(actions, document, state);
                        AddFieldCodeActions(actions, document, state);
                    }

                    AddLocalCodeActions(actions, document, state);
                }
                else
                {
                    if (canGenerateMember)
                    {
                        AddFieldCodeActions(actions, document, state);
                        AddPropertyCodeActions(actions, document, state);
                    }

                    AddLocalCodeActions(actions, document, state);
                }

                if (actions.Count > 1)
                {
                    // Wrap the generate variable actions into a single top level suggestion
                    // so as to not clutter the list.
                    return SpecializedCollections.SingletonEnumerable(
                        new MyCodeAction(FeaturesResources.Generate_variable, actions.AsImmutable()));
                }

                return actions;
            }
        }

        protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        {
            return false;
        }

        private void AddPropertyCodeActions(List<CodeAction> result, Document document, State state)
        {
            if (state.IsInRefContext || state.IsInOutContext)
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

            result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: true, isReadonly: false, isConstant: false));

            if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface && !state.IsWrittenTo)
            {
                result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: true, isReadonly: true, isConstant: false));
            }
        }

        private void AddFieldCodeActions(List<CodeAction> result, Document document, State state)
        {
            if (state.TypeToGenerateIn.TypeKind != TypeKind.Interface)
            {
                if (state.IsConstant)
                {
                    result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: false, isConstant: true));
                }
                else
                {
                    result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: false, isConstant: false));

                    // If we haven't written to the field, or we're in the constructor for the type
                    // we're writing into, then we can generate this field read-only.
                    if (!state.IsWrittenTo || state.IsInConstructor)
                    {
                        result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: true, isConstant: false));
                    }
                }
            }
        }

        private void AddLocalCodeActions(List<CodeAction> result, Document document, State state)
        {
            if (state.CanGenerateLocal())
            {
                result.Add(new GenerateLocalCodeAction((TService)this, document, state));
            }
        }

        private class MyCodeAction : CodeAction.SimpleCodeAction
        {
            public MyCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions)
            {
            }
        }
    }
}
