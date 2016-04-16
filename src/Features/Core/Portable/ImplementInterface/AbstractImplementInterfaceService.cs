// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService : IImplementInterfaceService
    {
        protected AbstractImplementInterfaceService()
        {
        }

        protected abstract bool CanImplementImplicitly { get; }
        protected abstract bool HasHiddenExplicitImplementation { get; }
        protected abstract bool TryInitializeState(Document document, SemanticModel model, SyntaxNode interfaceNode, CancellationToken cancellationToken, out SyntaxNode classOrStructDecl, out INamedTypeSymbol classOrStructType, out IEnumerable<INamedTypeSymbol> interfaceTypes);
        protected abstract bool CanImplementDisposePattern(INamedTypeSymbol symbol, SyntaxNode classDecl);
        protected abstract Document ImplementDisposePattern(Document document, SyntaxNode root, INamedTypeSymbol symbol, int position, bool explicitly);

        public async Task<Document> ImplementInterfaceAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_ImplementInterface, cancellationToken))
            {
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var state = State.Generate(this, document, model, node, cancellationToken);
                if (state == null)
                {
                    return document;
                }

                // While implementing just one default action, like in the case of pressing enter after interface name in VB,
                // choose to implement with the dispose pattern as that's the Dev12 behavior.
                var action = ShouldImplementDisposePattern(document, state, explicitly: false) ?
                             ImplementInterfaceWithDisposePatternCodeAction.CreateImplementWithDisposePatternCodeAction(this, document, state) :
                             ImplementInterfaceCodeAction.CreateImplementCodeAction(this, document, state);

                return await action.GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public IEnumerable<CodeAction> GetCodeActions(Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            var state = State.Generate(this, document, model, node, cancellationToken);
            return GetActions(document, state);
        }

        private IEnumerable<CodeAction> GetActions(Document document, State state)
        {
            if (state == null)
            {
                yield break;
            }

            if (state.UnimplementedMembers != null && state.UnimplementedMembers.Count > 0)
            {
                yield return ImplementInterfaceCodeAction.CreateImplementCodeAction(this, document, state);

                if (ShouldImplementDisposePattern(document, state, explicitly: false))
                {
                    yield return ImplementInterfaceWithDisposePatternCodeAction.CreateImplementWithDisposePatternCodeAction(this, document, state);
                }

                var delegatableMembers = GetDelegatableMembers(state);
                foreach (var member in delegatableMembers)
                {
                    yield return ImplementInterfaceCodeAction.CreateImplementThroughMemberCodeAction(this, document, state, member);
                }

                if (state.ClassOrStructType.IsAbstract)
                {
                    yield return ImplementInterfaceCodeAction.CreateImplementAbstractlyCodeAction(this, document, state);
                }
            }

            if (state.UnimplementedExplicitMembers != null && state.UnimplementedExplicitMembers.Count > 0)
            {
                yield return ImplementInterfaceCodeAction.CreateImplementExplicitlyCodeAction(this, document, state);

                if (ShouldImplementDisposePattern(document, state, explicitly: true))
                {
                    yield return ImplementInterfaceWithDisposePatternCodeAction.CreateImplementExplicitlyWithDisposePatternCodeAction(this, document, state);
                }
            }
        }

        private IList<ISymbol> GetDelegatableMembers(State state)
        {
            var fields =
                state.ClassOrStructType.GetMembers()
                                       .OfType<IFieldSymbol>()
                                       .Where(f => !f.IsImplicitlyDeclared)
                                       .Where(f => f.Type.GetAllInterfacesIncludingThis().Contains(state.InterfaceTypes.First()))
                                       .OfType<ISymbol>();

            // Select all properties with zero parameters that also have a getter
            var properties =
                state.ClassOrStructType.GetMembers()
                                       .OfType<IPropertySymbol>()
                                       .Where(p => (!p.IsImplicitlyDeclared) && (p.Parameters.Length == 0) && (p.GetMethod != null))
                                       .Where(p => p.Type.GetAllInterfacesIncludingThis().Contains(state.InterfaceTypes.First()))
                                       .OfType<ISymbol>();

            return fields.Concat(properties).ToList();
        }
    }
}
