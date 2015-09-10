// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractObjectInitializerCompletionProvider : AbstractCompletionProvider
    {
        protected abstract TextSpan GetTextChangeSpan(SourceText text, int position);
        protected abstract Tuple<ITypeSymbol, Location> GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
            var typeAndLocation = GetInitializedType(document, semanticModel, position, cancellationToken);

            if (typeAndLocation == null)
            {
                return null;
            }

            var initializedType = typeAndLocation.Item1 as INamedTypeSymbol;
            var initializerLocation = typeAndLocation.Item2;
            if (initializedType == null)
            {
                return null;
            }

            // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, initializedType);
            members = members.Where(m => IsInitializable(m, initializedType) &&
                                         m.CanBeReferencedByName &&
                                         IsLegalFieldOrProperty(m) &&
                                         !m.IsImplicitlyDeclared);

            // Filter out those members that have already been typed
            var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

            uninitializedMembers = uninitializedMembers.Where(m => m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var changes = GetTextChangeSpan(text, position);

            // Return the members
            return uninitializedMembers.Select(
                m => CreateItem(workspace, m.Name, changes,
                    CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, initializerLocation.SourceSpan.Start, m),
                    m.GetGlyph()));
        }

        private bool IsLegalFieldOrProperty(ISymbol symbol)
        {
            var type = symbol.GetMemberType();
            if (type != null && type.CanSupportCollectionInitializer())
            {
                return true;
            }

            return symbol.IsWriteableFieldOrProperty();
        }

        protected CompletionItem CreateItem(
            Workspace workspace,
            string displayText,
            TextSpan textSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph)
        {
            return new CompletionItem(this, displayText, textSpan, descriptionFactory, glyph, rules: ObjectInitializerCompletionItemRules.Instance);
        }

        protected virtual bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
        {
            return
                !member.IsStatic &&
                member.MatchesKind(SymbolKind.Field, SymbolKind.Property) &&
                member.IsAccessibleWithin(containingType);
        }
    }
}
