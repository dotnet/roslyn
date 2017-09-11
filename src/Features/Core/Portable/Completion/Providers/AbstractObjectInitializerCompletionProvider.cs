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
    internal abstract class AbstractObjectInitializerCompletionProvider : CommonCompletionProvider
    {
        protected abstract Tuple<ITypeSymbol, Location> GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
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
                context.IsExclusive = true;
            }

            var enclosing = semanticModel.GetEnclosingNamedType(position, cancellationToken);

            // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, initializedType);
            members = members.Where(m => IsInitializable(m, enclosing) &&
                                         m.CanBeReferencedByName &&
                                         IsLegalFieldOrProperty(m, enclosing) &&
                                         !m.IsImplicitlyDeclared);

            // Filter out those members that have already been typed
            var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

            uninitializedMembers = uninitializedMembers.Where(m => m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var uninitializedMember in uninitializedMembers)
            {
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: uninitializedMember.Name,
                    insertionText: null,
                    symbols: ImmutableArray.Create(uninitializedMember),
                    contextPosition: initializerLocation.SourceSpan.Start,
                    rules: s_rules));
            }
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        protected abstract Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken);

        private bool IsLegalFieldOrProperty(ISymbol symbol, ISymbol within)
            => CanSupportCollectionInitializer(symbol, within) || symbol.IsWriteableFieldOrProperty() || CanSupportObjectInitializer(symbol, within);

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        protected virtual bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
        {
            return
                !member.IsStatic &&
                member.MatchesKind(SymbolKind.Field, SymbolKind.Property) &&
                member.IsAccessibleWithin(containingType);
        }

        private static bool CanSupportObjectInitializer(ISymbol symbol, ISymbol within)
        {
            if (symbol is IPropertySymbol propertySymbol)
            {
                return propertySymbol.GetMethod != null && !propertySymbol.Type.IsStructType() && HasAccesseblePropertiesOrFields(propertySymbol.Type, within);
            }

            return false;
        }

        private static bool HasAccesseblePropertiesOrFields(ITypeSymbol type, ISymbol within)
        {
            var types = new HashSet<ITypeSymbol>();
            return HasAccesseblePropertiesOrFields(type, types);

            bool HasAccesseblePropertiesOrFields(ITypeSymbol typeToCheck, HashSet<ITypeSymbol> alreadyCheckedTypes)
            {
                if (alreadyCheckedTypes.Contains(typeToCheck))
                {
                    return false;
                }

                alreadyCheckedTypes.Add(typeToCheck);
                return typeToCheck.GetBaseTypesAndThis().SelectMany(x => x.GetMembers())
                    .Where(member => member is IPropertySymbol || member is IFieldSymbol)
                    .Any(member => member.IsWriteableFieldOrProperty() || 
                            CanSupportCollectionInitializer(member, within) || 
                            HasAccesseblePropertiesOrFields(member.GetMemberType(), alreadyCheckedTypes));
            }
        }

        private static bool CanSupportCollectionInitializer(ISymbol symbol, ISymbol within)
        {
            var type = symbol.GetMemberType();
            return type != null && type.CanSupportCollectionInitializer(within);
        }
    }
}
