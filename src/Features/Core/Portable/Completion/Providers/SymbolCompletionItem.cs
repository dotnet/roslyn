// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class SymbolCompletionItem
    {
        private static CompletionItem CreateWorker(
            string displayText,
            string displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> symbolEncoder,
            string sortText = null,
            string insertionText = null,
            string filterText = null,
            SupportedPlatformData supportedPlatforms = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
        {
            var props = properties ?? ImmutableDictionary<string, string>.Empty;

            if (insertionText != null)
            {
                props = props.Add("InsertionText", insertionText);
            }

            props = props.Add("ContextPosition", contextPosition.ToString());

            var firstSymbol = symbols[0];
            var item = CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                rules: rules,
                filterText: filterText ?? (displayText.Length > 0 && displayText[0] == '@' ? displayText : firstSymbol.Name),
                sortText: sortText ?? firstSymbol.Name,
                glyph: firstSymbol.GetGlyph(),
                showsWarningIcon: supportedPlatforms != null,
                properties: props,
                tags: tags);

            item = WithSupportedPlatforms(item, supportedPlatforms);
            return symbolEncoder(symbols, item);
        }

        public static CompletionItem AddSymbolEncoding(IReadOnlyList<ISymbol> symbols, CompletionItem item)
        {
            return item.AddProperty("Symbols", EncodeSymbols(symbols));
        }

        public static CompletionItem AddSymbolNameAndKind(IReadOnlyList<ISymbol> symbols, CompletionItem item)
        {
            var symbol = symbols[0];
            return item.AddProperty("SymbolKind", ((int)symbol.Kind).ToString())
                       .AddProperty("SymbolName", symbol.Name);
        }

        public static string EncodeSymbols(IReadOnlyList<ISymbol> symbols)
        {
            if (symbols.Count > 1)
            {
                return string.Join("|", symbols.Select(s => EncodeSymbol(s)));
            }
            else if (symbols.Count == 1)
            {
                return EncodeSymbol(symbols[0]);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string EncodeSymbol(ISymbol symbol)
        {
            return SymbolKey.ToString(symbol);
        }

        public static bool HasSymbols(CompletionItem item)
        {
            return item.Properties.ContainsKey("Symbols");
        }

        private static readonly char[] s_symbolSplitters = new[] { '|' };

        public static async Task<ImmutableArray<ISymbol>> GetSymbolsAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue("Symbols", out var symbolIds))
            {
                var idList = symbolIds.Split(s_symbolSplitters, StringSplitOptions.RemoveEmptyEntries).ToList();
                var symbols = new List<ISymbol>();

                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                DecodeSymbols(idList, compilation, symbols);

                // merge in symbols from other linked documents
                if (idList.Count > 0)
                {
                    var linkedIds = document.GetLinkedDocumentIds();
                    if (linkedIds.Length > 0)
                    {
                        foreach (var id in linkedIds)
                        {
                            var linkedDoc = document.Project.Solution.GetDocument(id);
                            var linkedCompilation = await linkedDoc.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                            DecodeSymbols(idList, linkedCompilation, symbols);
                        }
                    }
                }

                return symbols.ToImmutableArray();
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private static void DecodeSymbols(List<string> ids, Compilation compilation, List<ISymbol> symbols)
        {
            for (var i = 0; i < ids.Count;)
            {
                var id = ids[i];
                var symbol = DecodeSymbol(id, compilation);
                if (symbol != null)
                {
                    ids.RemoveAt(i); // consume id from the list
                    symbols.Add(symbol); // add symbol to the results
                }
                else
                {
                    i++;
                }
            }
        }

        private static ISymbol DecodeSymbol(string id, Compilation compilation)
        {
            return SymbolKey.Resolve(id, compilation).GetAnySymbol();
        }

        public static async Task<CompletionDescription> GetDescriptionAsync(
            CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var position = GetDescriptionPosition(item);
            if (position == -1)
            {
                position = item.Span.Start;
            }

            var supportedPlatforms = GetSupportedPlatforms(item, workspace);

            var contextDocument = FindAppropriateDocumentForDescriptionContext(document, supportedPlatforms);

            var semanticModel = await contextDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbols = await GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            if (symbols.Length > 0)
            {
                return await CommonCompletionUtilities.CreateDescriptionAsync(workspace, semanticModel, position, symbols, supportedPlatforms, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }

        private static Document FindAppropriateDocumentForDescriptionContext(Document document, SupportedPlatformData supportedPlatforms)
        {
            var contextDocument = document;
            if (supportedPlatforms != null && supportedPlatforms.InvalidProjects.Contains(document.Id.ProjectId))
            {
                var contextId = document.GetLinkedDocumentIds().FirstOrDefault(id => !supportedPlatforms.InvalidProjects.Contains(id.ProjectId));
                if (contextId != null)
                {
                    contextDocument = document.Project.Solution.GetDocument(contextId);
                }
            }

            return contextDocument;
        }

        private static CompletionItem WithSupportedPlatforms(CompletionItem completionItem, SupportedPlatformData supportedPlatforms)
        {
            if (supportedPlatforms != null)
            {
                return completionItem
                    .AddProperty("InvalidProjects", string.Join(";", supportedPlatforms.InvalidProjects.Select(id => id.Id)))
                    .AddProperty("CandidateProjects", string.Join(";", supportedPlatforms.CandidateProjects.Select(id => id.Id)));
            }
            else
            {
                return completionItem;
            }
        }

        private static readonly char[] projectSeperators = new[] { ';' };
        public static SupportedPlatformData GetSupportedPlatforms(CompletionItem item, Workspace workspace)
        {
            if (item.Properties.TryGetValue("InvalidProjects", out var invalidProjects)
                && item.Properties.TryGetValue("CandidateProjects", out var candidateProjects))
            {
                return new SupportedPlatformData(
                    invalidProjects.Split(projectSeperators).Select(s => ProjectId.CreateFromSerialized(Guid.Parse(s))).ToList(),
                    candidateProjects.Split(projectSeperators).Select(s => ProjectId.CreateFromSerialized(Guid.Parse(s))).ToList(),
                    workspace);
            }

            return null;
        }

        public static int GetContextPosition(CompletionItem item)
        {
            if (item.Properties.TryGetValue("ContextPosition", out var text) &&
                int.TryParse(text, out var number))
            {
                return number;
            }
            else
            {
                return -1;
            }
        }

        public static int GetDescriptionPosition(CompletionItem item)
            => GetContextPosition(item);

        public static string GetInsertionText(CompletionItem item)
        {
            item.Properties.TryGetValue("InsertionText", out var text);
            return text;
        }

        // COMPAT OVERLOAD: This is used by IntelliCode.
        public static CompletionItem CreateWithSymbolId(
            string displayText,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string sortText = null,
            string insertionText = null,
            string filterText = null,
            SupportedPlatformData supportedPlatforms = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
        {
            return CreateWithSymbolId(
                displayText,
                displayTextSuffix: null,
                symbols,
                rules,
                contextPosition,
                sortText,
                insertionText,
                filterText,
                supportedPlatforms,
                properties,
                tags);
        }

        public static CompletionItem CreateWithSymbolId(
            string displayText,
            string displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string sortText = null,
            string insertionText = null,
            string filterText = null,
            SupportedPlatformData supportedPlatforms = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
        {
            return CreateWorker(
                displayText, displayTextSuffix, symbols, rules, contextPosition,
                AddSymbolEncoding, sortText, insertionText,
                filterText, supportedPlatforms, properties, tags);
        }

        public static CompletionItem CreateWithNameAndKind(
            string displayText,
            string displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string sortText = null,
            string insertionText = null,
            string filterText = null,
            SupportedPlatformData supportedPlatforms = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
        {
            return CreateWorker(
                displayText, displayTextSuffix, symbols, rules, contextPosition,
                AddSymbolNameAndKind, sortText, insertionText,
                filterText, supportedPlatforms, properties, tags);
        }

        internal static string GetSymbolName(CompletionItem item)
        {
            if (item.Properties.TryGetValue("SymbolName", out var name))
            {
                return name;
            }

            return null;
        }

        internal static SymbolKind? GetKind(CompletionItem item)
        {
            if (item.Properties.TryGetValue("SymbolKind", out var kind))
            {
                return (SymbolKind)int.Parse(kind);
            }

            return null;
        }

        public static async Task<CompletionDescription> GetDescriptionAsync(
            CompletionItem item, ImmutableArray<ISymbol> symbols, Document document, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var position = GetDescriptionPosition(item);
            var supportedPlatforms = GetSupportedPlatforms(item, workspace);

            var contextDocument = FindAppropriateDocumentForDescriptionContext(document, supportedPlatforms);

            if (symbols.Length != 0)
            {
                return await CommonCompletionUtilities.CreateDescriptionAsync(workspace, semanticModel, position, symbols, supportedPlatforms, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }
    }
}
