// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private abstract partial class SymbolReference : Reference
        {
            public readonly SymbolResult<INamespaceOrTypeSymbol> SymbolResult;

            protected abstract bool ShouldAddWithExistingImport(Document document);

            public SymbolReference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult)
                : base(provider, new SearchResult(symbolResult))
            {
                this.SymbolResult = symbolResult;
            }

            protected abstract ImmutableArray<string> GetTags(Document document);

            public override bool Equals(object obj)
            {
                var equals = base.Equals(obj);
                if (!equals)
                {
                    return false;
                }

                var name1 = this.SymbolResult.DesiredName;
                var name2 = (obj as SymbolReference)?.SymbolResult.DesiredName;
                return StringComparer.Ordinal.Equals(name1, name2);
            }

            public override int GetHashCode()
                => Hash.Combine(this.SymbolResult.DesiredName, base.GetHashCode());

            private async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
                Document document, SyntaxNode contextNode,
                bool placeSystemNamespaceFirst, bool hasExistingImport,
                CancellationToken cancellationToken)
            {
                // Defer to the language to add the actual import/using.
                if (hasExistingImport)
                {
                    return ImmutableArray<TextChange>.Empty;
                }

                (var newContextNode, var newDocument) = await ReplaceNameNodeAsync(
                    contextNode, document, cancellationToken).ConfigureAwait(false);

                var updatedDocument = await provider.AddImportAsync(
                    newContextNode, this.SymbolResult.Symbol, newDocument,
                    placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                var cleanedDocument = await CodeAction.CleanupDocumentAsync(
                    updatedDocument, cancellationToken).ConfigureAwait(false);

                var textChanges = await cleanedDocument.GetTextChangesAsync(
                    document, cancellationToken).ConfigureAwait(false);

                return textChanges.ToImmutableArray();
            }

            public sealed override async Task<AddImportFixData> TryGetFixDataAsync(
                Document document, SyntaxNode node,
                bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var (description, hasExistingImport) = GetDescription(document, node, semanticModel, cancellationToken);
                if (description == null)
                {
                    return null;
                }

                if (hasExistingImport && !this.ShouldAddWithExistingImport(document))
                {
                    return null;
                }

                var isFuzzy = !this.SearchResult.DesiredNameMatchesSourceName(document);
                var tags = GetTags(document);
                if (isFuzzy)
                {
                    // The name is going to change.  Make it clear in the description that this is
                    // going to happen.
                    description = $"{this.SearchResult.DesiredName} - {description}";

                    // if we were a fuzzy match, and we didn't have any glyph to show, then add the
                    // namespace-glyph to this item. This helps indicate that not only are we fixing
                    // the spelling of this name we are *also* adding a namespace.  This helps as we
                    // have gotten feedback in the past that the 'using/import' addition was
                    // unexpected.
                    if (tags.IsDefaultOrEmpty)
                    {
                        tags = WellKnownTagArrays.Namespace;
                    }
                }

                var textChanges = await GetTextChangesAsync(
                    document, node, placeSystemNamespaceFirst, hasExistingImport, cancellationToken).ConfigureAwait(false);

                return GetFixData(
                    document, textChanges, description,
                    tags, GetPriority(document));
            }

            protected abstract AddImportFixData GetFixData(
                Document document, ImmutableArray<TextChange> textChanges,
                string description, ImmutableArray<string> tags, CodeActionPriority priority);

            protected abstract CodeActionPriority GetPriority(Document document);

            protected virtual (string description, bool hasExistingImport) GetDescription(
                Document document, SyntaxNode node,
                SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return provider.GetDescription(
                    document, SymbolResult.Symbol, semanticModel, node, cancellationToken);
            }
        }
    }
}
