// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class SymbolCompletionItem
    {
        private static CompletionItem CreateWorker(
            string displayText,
            TextSpan span,
            IReadOnlyList<ISymbol> symbols,
            Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> symbolEncoder,
            int contextPosition = -1,
            int descriptionPosition = -1,
            string sortText = null,
            string insertionText = null,
            Glyph? glyph = null,
            string filterText = null,
            int? matchPriority = null,
            SupportedPlatformData supportedPlatforms = null,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            var props = properties ?? ImmutableDictionary<string, string>.Empty;

            if (insertionText != null)
            {
                props = props.Add("InsertionText", insertionText);
            }

            if (contextPosition >= 0)
            {
                props = props.Add("ContextPosition", contextPosition.ToString());
            }

            if (descriptionPosition >= 0)
            {
                props = props.Add("DescriptionPosition", descriptionPosition.ToString());
            }

            var item = CommonCompletionItem.Create(
                displayText: displayText,
                span: span,
                filterText: filterText ?? (displayText.Length > 0 && displayText[0] == '@' ? displayText : symbols[0].Name),
                sortText: sortText ?? symbols[0].Name,
                glyph: glyph ?? symbols[0].GetGlyph(),
                matchPriority: matchPriority.GetValueOrDefault(),
                isArgumentName: isArgumentName,
                showsWarningIcon: supportedPlatforms != null,
                properties: props,
                tags: tags,
                rules: rules);

            item = WithSupportedPlatforms(item, supportedPlatforms);
            return symbolEncoder(symbols, item);
        }

        private static CompletionItem CreateWorker(
            string displayText,
            TextSpan span,
            ISymbol symbol,
            Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> symbolEncoder,
            int contextPosition = -1,
            int descriptionPosition = -1,
            string sortText = null,
            string insertionText = null,
            Glyph? glyph = null,
            string filterText = null,
            int? matchPriority = null,
            SupportedPlatformData supportedPlatforms = null,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            CompletionItemRules rules = null)
        {
            return CreateWorker(
                displayText: displayText,
                span: span,
                symbols: ImmutableArray.Create(symbol),
                symbolEncoder: symbolEncoder,
                contextPosition: contextPosition,
                descriptionPosition: descriptionPosition,
                sortText: sortText,
                insertionText: insertionText,
                glyph: glyph,
                filterText: filterText,
                matchPriority: matchPriority.GetValueOrDefault(),
                supportedPlatforms: supportedPlatforms,
                isArgumentName: isArgumentName,
                properties: properties,
                rules: rules);
        }

        public static CompletionItem AddSymbolEncoding(IReadOnlyList<ISymbol> symbols, CompletionItem item)
        {
            return item.AddProperty("Symbols", EncodeSymbols(symbols));
        }

        public static CompletionItem AddSymbolEncoding(ISymbol symbol, CompletionItem item)
        {
            return item.AddProperty("Symbols", EncodeSymbol(symbol));
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
            return SymbolId.CreateId(symbol);
        }

        public static bool HasSymbols(CompletionItem item)
        {
            return item.Properties.ContainsKey("Symbols");
        }

        private static readonly char[] s_symbolSplitters = new[] { '|' };

        public static async Task<ImmutableArray<ISymbol>> GetSymbolsAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            string symbolIds;
            if (item.Properties.TryGetValue("Symbols", out symbolIds))
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
            for (int i = 0; i < ids.Count;)
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
            return SymbolId.GetFirstSymbolForId(id, compilation);
        }

        public static async Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var position = GetDescriptionPosition(item);
            if (position == -1)
            {
                position = item.Span.Start;
            }

            var supportedPlatforms = GetSupportedPlatforms(item, workspace);

            // find appropriate document for descripton context
            var contextDocument = document;
            if (supportedPlatforms != null && supportedPlatforms.InvalidProjects.Contains(document.Id.ProjectId))
            {
                var contextId = document.GetLinkedDocumentIds().FirstOrDefault(id => !supportedPlatforms.InvalidProjects.Contains(id.ProjectId));
                if (contextId != null)
                {
                    contextDocument = document.Project.Solution.GetDocument(contextId);
                }
            }

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
            string invalidProjects;
            string candidateProjects;

            if (item.Properties.TryGetValue("InvalidProjects", out invalidProjects) 
                && item.Properties.TryGetValue("CandidateProjects", out candidateProjects))
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
            string text;
            int number;
            if (item.Properties.TryGetValue("ContextPosition", out text) && int.TryParse(text, out number))
            {
                return number;
            }
            else
            {
                return -1;
            }
        }

        public static int GetDescriptionPosition(CompletionItem item)
        {
            string text;
            int number;
            if (item.Properties.TryGetValue("DescriptionPosition", out text) && int.TryParse(text, out number))
            {
                return number;
            }
            else
            {
                return -1;
            }
        }

        public static string GetInsertionText(CompletionItem item)
        {
            string text;
            item.Properties.TryGetValue("InsertionText", out text);
            return text;
        }

        public static CompletionItem CreateWithSymbolId(
            string displayText,
            TextSpan span,
            IReadOnlyList<ISymbol> symbols,
            int contextPosition = -1,
            int descriptionPosition = -1,
            string sortText = null,
            string insertionText = null,
            Glyph? glyph = null,
            string filterText = null,
            int? matchPriority = null,
            SupportedPlatformData supportedPlatforms = null,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            return CreateWorker(displayText, span, symbols, AddSymbolEncoding, contextPosition, descriptionPosition, sortText,
                insertionText, glyph, filterText, matchPriority, supportedPlatforms, isArgumentName, properties, tags, rules);
        }

        public static CompletionItem CreateWithNameAndKind(
            string displayText,
            TextSpan span,
            IReadOnlyList<ISymbol> symbols,
            int contextPosition = -1,
            int descriptionPosition = -1,
            string sortText = null,
            string insertionText = null,
            Glyph? glyph = null,
            string filterText = null,
            int? matchPriority = null,
            SupportedPlatformData supportedPlatforms = null,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            return CreateWorker(displayText, span, symbols, AddSymbolNameAndKind, contextPosition, descriptionPosition, sortText,
                insertionText, glyph, filterText, matchPriority, supportedPlatforms, isArgumentName, properties, tags, rules);
        }

        internal static string GetSymbolName(CompletionItem item)
        {
            string name;
            if (item.Properties.TryGetValue("SymbolName", out name))
            {
                return name;
            }

            return null;
        }

        internal static SymbolKind? GetKind(CompletionItem item)
        {
            string kind;
            if (item.Properties.TryGetValue("SymbolKind", out kind))
            {
                return (SymbolKind)int.Parse(kind);
            }

            return null;
        }

        public static async Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, ImmutableArray<ISymbol> symbols, Document document, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var position = SymbolCompletionItem.GetDescriptionPosition(item);
            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(item, workspace);

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
    }
}
