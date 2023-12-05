// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractObjectInitializerCompletionProvider : LSPCompletionProvider
    {
        protected abstract Tuple<ITypeSymbol, Location>? GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);
        protected abstract string EscapeIdentifier(ISymbol symbol);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            if (GetInitializedType(document, semanticModel, position, cancellationToken) is not var (type, initializerLocation))
            {
                return;
            }

            if (type is ITypeParameterSymbol typeParameterSymbol)
            {
                type = typeParameterSymbol.GetNamedTypeSymbolConstraint();
            }

            if (type is not INamedTypeSymbol initializedType)
            {
                return;
            }

            if (await IsExclusiveAsync(document, position, cancellationToken).ConfigureAwait(false))
            {
                context.IsExclusive = true;
            }

            var enclosing = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            Contract.ThrowIfNull(enclosing);

            // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, initializedType);
            members = members.Where(m => IsInitializable(m, enclosing) &&
                                         m.CanBeReferencedByName &&
                                         IsLegalFieldOrProperty(m) &&
                                         !m.IsImplicitlyDeclared);

            // Filter out those members that have already been typed
            var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

            // Sort the members by name so if we preselect one, it'll be stable
            uninitializedMembers = uninitializedMembers.Where(m => m.IsEditorBrowsable(context.CompletionOptions.HideAdvancedMembers, semanticModel.Compilation))
                                                       .OrderBy(m => m.Name);

            var firstUnitializedRequiredMember = true;

            foreach (var uninitializedMember in uninitializedMembers)
            {
                var rules = s_rules;

                // We'll hard select the first required member to make it a bit easier to type out an object initializer
                // with a bunch of members.
                if (firstUnitializedRequiredMember && uninitializedMember.IsRequired())
                {
                    rules = rules.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection).WithMatchPriority(MatchPriority.Preselect);
                    firstUnitializedRequiredMember = false;
                }

                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: EscapeIdentifier(uninitializedMember),
                    displayTextSuffix: "",
                    insertionText: null,
                    symbols: ImmutableArray.Create(uninitializedMember),
                    contextPosition: initializerLocation.SourceSpan.Start,
                    inlineDescription: uninitializedMember.IsRequired() ? FeaturesResources.Required : null,
                    rules: rules));
            }
        }

        internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

        protected abstract Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken);

        private static bool IsLegalFieldOrProperty(ISymbol symbol)
        {
            return symbol.IsWriteableFieldOrProperty()
                || symbol.ContainingType.IsAnonymousType
                || CanSupportObjectInitializer(symbol);
        }

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        protected virtual bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
        {
            return
                !member.IsStatic &&
                member.MatchesKind(SymbolKind.Field, SymbolKind.Property) &&
                member.IsAccessibleWithin(containingType);
        }

        private static bool CanSupportObjectInitializer(ISymbol symbol)
        {
            Debug.Assert(!symbol.IsWriteableFieldOrProperty(), "Assertion failed - expected writable field/property check before calling this method.");

            if (symbol is IFieldSymbol fieldSymbol)
            {
                return !fieldSymbol.Type.IsStructType();
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return !propertySymbol.Type.IsStructType();
            }

            throw ExceptionUtilities.Unreachable();
        }
    }
}
