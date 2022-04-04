// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

        protected abstract bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, SemanticModel semanticModel, CancellationToken cancellationToken, out SyntaxNode newRoot);

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

                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);

                var canGenerateMember = CodeGenerator.CanAdd(document.Project.Solution, state.TypeToGenerateIn, cancellationToken);

                if (canGenerateMember && state.CanGeneratePropertyOrField())
                {
                    // prefer fields over properties (and vice versa) depending on the casing of the member.
                    // lowercase -> fields.  title case -> properties.
                    var name = state.IdentifierToken.ValueText;
                    if (char.IsUpper(name.ToCharArray().FirstOrDefault()))
                    {
                        await AddPropertyCodeActionsAsync(actions, semanticDocument, state, cancellationToken).ConfigureAwait(false);
                        AddFieldCodeActions(actions, semanticDocument, state);
                    }
                    else
                    {
                        AddFieldCodeActions(actions, semanticDocument, state);
                        await AddPropertyCodeActionsAsync(actions, semanticDocument, state, cancellationToken).ConfigureAwait(false);
                    }
                }

                await AddLocalCodeActionsAsync(actions, document, state, cancellationToken).ConfigureAwait(false);
                await AddParameterCodeActionsAsync(actions, document, state, cancellationToken).ConfigureAwait(false);

                if (actions.Count > 1)
                {
                    // Wrap the generate variable actions into a single top level suggestion
                    // so as to not clutter the list.
                    return ImmutableArray.Create<CodeAction>(new MyCodeAction(
                        string.Format(FeaturesResources.Generate_variable_0, state.IdentifierToken.ValueText),
                        actions.ToImmutable()));
                }

                return actions.ToImmutable();
            }
        }

        protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => false;

        private static async Task AddPropertyCodeActionsAsync(
            ArrayBuilder<CodeAction> result, SemanticDocument document, State state, CancellationToken cancellationToken)
        {
            if (state.IsInOutContext)
                return;

            if (state.IsConstant)
                return;

            if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface && state.IsStatic)
                return;

            // Don't generate properties with a `_` prefix unless that's what the user really wants as their naming style.
            if (await NameIsHighlyUnlikelyToWarrantSymbolAsync(
                    document.Document, state, SymbolKind.Property, state.DetermineMaximalAccessibility(), cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var isOnlyReadAndIsInInterface = state.TypeToGenerateIn.TypeKind == TypeKind.Interface && !state.IsWrittenTo;
            if (isOnlyReadAndIsInInterface || state.IsInConstructor)
            {
                result.Add(new GenerateVariableCodeAction(
                    document, state, generateProperty: true, isReadonly: true, isConstant: false, refKind: GetRefKindFromContext(state)));
            }

            GenerateWritableProperty(result, document, state);
        }

        private static async Task<bool> NameIsHighlyUnlikelyToWarrantSymbolAsync(
            Document document, State state, SymbolKind kind, Accessibility accessibility, CancellationToken cancellationToken)
        {
            // Check If the user explicitly used _ as the start of the name they're generating.  Don't offer to generate
            // a non-field symbol unless that's genuinely the naming style they have setup.
            if (state.IdentifierToken.ValueText.StartsWith("_"))
            {
                var namingStyle = await document.GetApplicableNamingRuleAsync(kind, accessibility, cancellationToken).ConfigureAwait(false);
                if (namingStyle.NamingStyle.Prefix != "_")
                    return true;
            }

            return false;
        }

        private static void GenerateWritableProperty(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            result.Add(new GenerateVariableCodeAction(
                document, state, generateProperty: true, isReadonly: false, isConstant: false,
                refKind: GetRefKindFromContext(state)));
        }

        private static void AddFieldCodeActions(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            if (state.TypeToGenerateIn.TypeKind != TypeKind.Interface)
            {
                if (state.IsConstant)
                {
                    result.Add(new GenerateVariableCodeAction(
                        document, state, generateProperty: false, isReadonly: false, isConstant: true, refKind: RefKind.None));
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
                            document, state, generateProperty: false, isReadonly: true, isConstant: false, refKind: RefKind.None));
                    }

                    if (state.OfferReadOnlyFieldFirst)
                    {
                        GenerateWriteableField(result, document, state);
                    }
                }
            }
        }

        private static void GenerateWriteableField(ArrayBuilder<CodeAction> result, SemanticDocument document, State state)
        {
            result.Add(new GenerateVariableCodeAction(
                document, state, generateProperty: false, isReadonly: false, isConstant: false, refKind: RefKind.None));
        }

        private async Task AddLocalCodeActionsAsync(
            ArrayBuilder<CodeAction> result, Document document, State state, CancellationToken cancellationToken)
        {
            if (state.CanGenerateLocal())
            {
                // Don't generate locals with a `_` prefix unless that's what the user really wants as their naming style.
                if (await NameIsHighlyUnlikelyToWarrantSymbolAsync(
                        document, state, SymbolKind.Local, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                result.Add(new GenerateLocalCodeAction((TService)this, document, state));
            }
        }

        private static async Task AddParameterCodeActionsAsync(
            ArrayBuilder<CodeAction> result, Document document, State state, CancellationToken cancellationToken)
        {
            if (state.CanGenerateParameter())
            {
                // Don't generate parameters with a `_` prefix unless that's what the user really wants as their naming style.
                if (await NameIsHighlyUnlikelyToWarrantSymbolAsync(
                        document, state, SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                result.Add(new GenerateParameterCodeAction(document, state, includeOverridesAndImplementations: false));

                if (AddParameterService.HasCascadingDeclarations(state.ContainingMethod))
                    result.Add(new GenerateParameterCodeAction(document, state, includeOverridesAndImplementations: true));
            }
        }

        private static RefKind GetRefKindFromContext(State state)
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
