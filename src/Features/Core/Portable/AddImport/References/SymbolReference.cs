// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private abstract partial class SymbolReference(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
        SymbolResult<INamespaceOrTypeSymbol> symbolResult) : Reference(provider, new SearchResult(symbolResult))
    {
        public readonly SymbolResult<INamespaceOrTypeSymbol> SymbolResult = symbolResult;

        protected abstract bool ShouldAddWithExistingImport(Document document);

        protected abstract ImmutableArray<string> GetTags(Document document);

        public override bool Equals(object obj)
        {
            var equals = base.Equals(obj);
            if (!equals)
            {
                return false;
            }

            var name1 = SymbolResult.DesiredName;
            var name2 = (obj as SymbolReference)?.SymbolResult.DesiredName;
            return StringComparer.Ordinal.Equals(name1, name2);
        }

        public override int GetHashCode()
            => Hash.Combine(SymbolResult.DesiredName, base.GetHashCode());

        private async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
            Document document, SyntaxNode contextNode,
            CodeCleanupOptions options, bool hasExistingImport,
            CancellationToken cancellationToken)
        {
            // Defer to the language to add the actual import/using.
            if (hasExistingImport)
            {
                return [];
            }

            (var newContextNode, var newDocument) = await ReplaceNameNodeAsync(
                contextNode, document, cancellationToken).ConfigureAwait(false);

            var updatedDocument = await provider.AddImportAsync(
                newContextNode, SymbolResult.Symbol, newDocument,
                options.AddImportOptions, cancellationToken).ConfigureAwait(false);

            var cleanedDocument = await CodeAction.CleanupDocumentAsync(
                updatedDocument, options, cancellationToken).ConfigureAwait(false);

            var textChanges = await cleanedDocument.GetTextChangesAsync(
                document, cancellationToken).ConfigureAwait(false);

            return textChanges.ToImmutableArray();
        }

        public sealed override async Task<AddImportFixData> TryGetFixDataAsync(
            Document document, SyntaxNode node,
            CodeCleanupOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (description, hasExistingImport) = GetDescription(document, options, node, semanticModel, cancellationToken);
            if (description == null)
            {
                return null;
            }

            if (hasExistingImport && !ShouldAddWithExistingImport(document))
            {
                return null;
            }

            var isFuzzy = !SearchResult.DesiredNameMatchesSourceName(document);
            var tags = GetTags(document);
            if (isFuzzy)
            {
                // The name is going to change.  Make it clear in the description that this is
                // going to happen.
                description = $"{SearchResult.DesiredName} - {description}";

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
                document, node, options, hasExistingImport, cancellationToken).ConfigureAwait(false);

            return GetFixData(
                document, textChanges, description,
                tags, GetPriority(document));
        }

        protected abstract AddImportFixData GetFixData(
            Document document, ImmutableArray<TextChange> textChanges,
            string description, ImmutableArray<string> tags, CodeActionPriority priority);

        protected abstract CodeActionPriority GetPriority(Document document);

        protected virtual (string description, bool hasExistingImport) GetDescription(
            Document document, CodeCleanupOptions options, SyntaxNode node,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return provider.GetDescription(
                document, options.AddImportOptions, SymbolResult.Symbol, semanticModel, node, cancellationToken);
        }
    }
}
