// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractObjectInitializerCompletionProvider : CompletionListProvider
    {
        protected abstract TextSpan GetTextChangeSpan(SourceText text, int position);
        protected abstract Tuple<ITypeSymbol, Location> GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, length: 0), cancellationToken).ConfigureAwait(false);
            var typeAndLocation = GetInitializedType(document, semanticModel, position, cancellationToken);

            if (typeAndLocation == null)
            {
                return;
            }

            var initializedType = typeAndLocation.Item1 as INamedTypeSymbol;
            var initializerLocation = typeAndLocation.Item2;
            if (initializedType == null)
            {
                return;
            }

            if (await IsExclusiveAsync(document, position, cancellationToken).ConfigureAwait(false))
            {
                context.MakeExclusive(true);
            }
            var enclosing = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, initializedType);
            members = members.Where(m => IsInitializable(m, initializedType) &&
                                         m.CanBeReferencedByName &&
                                         IsLegalFieldOrProperty(m, enclosing) &&
                                         !m.IsImplicitlyDeclared);

            // Filter out those members that have already been typed
            var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

            uninitializedMembers = uninitializedMembers.Where(m => m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var filterSpan = GetTextChangeSpan(text, position);

            foreach (var uninitializedMember in uninitializedMembers)
            {
                context.AddItem(CreateItem(
                    workspace, 
                    uninitializedMember.Name,
                    filterSpan,
                    CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, initializerLocation.SourceSpan.Start, uninitializedMember),
                    uninitializedMember.GetGlyph()));
            }
        }

        protected abstract Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken);

        private bool IsLegalFieldOrProperty(ISymbol symbol, ISymbol within)
        {
            var type = symbol.GetMemberType();
            if (type != null && type.CanSupportCollectionInitializer(within))
            {
                return true;
            }

            return symbol.IsWriteableFieldOrProperty();
        }

        protected CompletionItem CreateItem(
            Workspace workspace,
            string displayText,
            TextSpan filterSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph)
        {
            return new CompletionItem(this, displayText, filterSpan, descriptionFactory, glyph, rules: ObjectInitializerCompletionItemRules.Instance);
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
