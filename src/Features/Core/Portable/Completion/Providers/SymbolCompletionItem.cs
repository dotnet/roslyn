// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class SymbolCompletionItem
    {
        private const string InsertionTextProperty = "InsertionText";

        private static readonly Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> s_addSymbolEncoding = AddSymbolEncoding;
        private static readonly Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> s_addSymbolInfo = AddSymbolInfo;
        private static readonly char[] s_projectSeperators = new[] { ';' };

        private static CompletionItem CreateWorker(
            string displayText,
            string? displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            Func<IReadOnlyList<ISymbol>, CompletionItem, CompletionItem> symbolEncoder,
            string? sortText = null,
            string? insertionText = null,
            string? filterText = null,
            SupportedPlatformData? supportedPlatforms = null,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableArray<string> tags = default,
            string? displayTextPrefix = null,
            string? inlineDescription = null,
            Glyph? glyph = null,
            bool isComplexTextEdit = false)
        {
            var props = properties ?? ImmutableDictionary<string, string>.Empty;

            if (insertionText != null)
            {
                props = props.Add(InsertionTextProperty, insertionText);
            }

            props = props.Add("ContextPosition", contextPosition.ToString());

            var firstSymbol = symbols[0];
            var item = CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                displayTextPrefix: displayTextPrefix,
                inlineDescription: inlineDescription,
                rules: rules,
                filterText: filterText ?? (displayText.Length > 0 && displayText[0] == '@' ? displayText : firstSymbol.Name),
                sortText: sortText ?? firstSymbol.Name,
                glyph: glyph ?? firstSymbol.GetGlyph(),
                showsWarningIcon: supportedPlatforms != null,
                properties: props,
                tags: tags,
                isComplexTextEdit: isComplexTextEdit);

            item = WithSupportedPlatforms(item, supportedPlatforms);
            return symbolEncoder(symbols, item);
        }

        public static CompletionItem AddSymbolEncoding(IReadOnlyList<ISymbol> symbols, CompletionItem item)
            => item.AddProperty("Symbols", EncodeSymbols(symbols));

        public static CompletionItem AddSymbolInfo(IReadOnlyList<ISymbol> symbols, CompletionItem item)
        {
            var symbol = symbols[0];
            var isGeneric = symbol.GetArity() > 0;
            item = item
                .AddProperty("SymbolKind", ((int)symbol.Kind).ToString())
                .AddProperty("SymbolName", symbol.Name);

            return isGeneric ? item.AddProperty("IsGeneric", isGeneric.ToString()) : item;
        }

        public static CompletionItem AddShouldProvideParenthesisCompletion(CompletionItem item)
            => item.AddProperty("ShouldProvideParenthesisCompletion", true.ToString());

        public static bool GetShouldProvideParenthesisCompletion(CompletionItem item)
        {
            if (item.Properties.TryGetValue("ShouldProvideParenthesisCompletion", out _))
            {
                return true;
            }

            return false;
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
            => SymbolKey.CreateString(symbol);

        public static bool HasSymbols(CompletionItem item)
            => item.Properties.ContainsKey("Symbols");

        private static readonly char[] s_symbolSplitters = new[] { '|' };

        public static async Task<ImmutableArray<ISymbol>> GetSymbolsAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue("Symbols", out var symbolIds))
            {
                var idList = symbolIds.Split(s_symbolSplitters, StringSplitOptions.RemoveEmptyEntries).ToList();
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                DecodeSymbols(idList, compilation, symbols);

                // merge in symbols from other linked documents
                if (idList.Count > 0)
                {
                    var linkedIds = document.GetLinkedDocumentIds();
                    if (linkedIds.Length > 0)
                    {
                        foreach (var id in linkedIds)
                        {
                            var linkedDoc = document.Project.Solution.GetRequiredDocument(id);
                            var linkedCompilation = await linkedDoc.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                            DecodeSymbols(idList, linkedCompilation, symbols);
                        }
                    }
                }

                return symbols.ToImmutable();
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private static void DecodeSymbols(List<string> ids, Compilation compilation, ArrayBuilder<ISymbol> symbols)
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

        private static ISymbol? DecodeSymbol(string id, Compilation compilation)
            => SymbolKey.ResolveString(id, compilation).GetAnySymbol();

        public static async Task<CompletionDescription> GetDescriptionAsync(
            CompletionItem item, Document document, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var symbols = await GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            return await GetDescriptionForSymbolsAsync(item, document, symbols, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<CompletionDescription> GetDescriptionForSymbolsAsync(
            CompletionItem item, Document document, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            if (symbols.Length == 0)
                return CompletionDescription.Empty;

            var position = GetDescriptionPosition(item);
            if (position == -1)
                position = item.Span.Start;

            var supportedPlatforms = GetSupportedPlatforms(item, document.Project.Solution);
            var contextDocument = FindAppropriateDocumentForDescriptionContext(document, supportedPlatforms);
            var semanticModel = await contextDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var services = document.Project.Solution.Workspace.Services;
            return await CommonCompletionUtilities.CreateDescriptionAsync(services, semanticModel, position, symbols, options, supportedPlatforms, cancellationToken).ConfigureAwait(false);
        }

        private static Document FindAppropriateDocumentForDescriptionContext(Document document, SupportedPlatformData? supportedPlatforms)
        {
            if (supportedPlatforms != null && supportedPlatforms.InvalidProjects.Contains(document.Id.ProjectId))
            {
                var contextId = document.GetLinkedDocumentIds().FirstOrDefault(id => !supportedPlatforms.InvalidProjects.Contains(id.ProjectId));
                if (contextId != null)
                {
                    return document.Project.Solution.GetRequiredDocument(contextId);
                }
            }

            return document;
        }

        private static CompletionItem WithSupportedPlatforms(CompletionItem completionItem, SupportedPlatformData? supportedPlatforms)
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

        public static SupportedPlatformData? GetSupportedPlatforms(CompletionItem item, Solution solution)
        {
            if (item.Properties.TryGetValue("InvalidProjects", out var invalidProjects)
                && item.Properties.TryGetValue("CandidateProjects", out var candidateProjects))
            {
                return new SupportedPlatformData(
                    solution,
                    invalidProjects.Split(s_projectSeperators).Select(s => ProjectId.CreateFromSerialized(Guid.Parse(s))).ToList(),
                    candidateProjects.Split(s_projectSeperators).Select(s => ProjectId.CreateFromSerialized(Guid.Parse(s))).ToList());
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
            => item.Properties[InsertionTextProperty];

        public static bool TryGetInsertionText(CompletionItem item, [NotNullWhen(true)] out string? insertionText)
            => item.Properties.TryGetValue(InsertionTextProperty, out insertionText);

        // COMPAT OVERLOAD: This is used by IntelliCode.
        public static CompletionItem CreateWithSymbolId(
            string displayText,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string? sortText = null,
            string? insertionText = null,
            string? filterText = null,
            SupportedPlatformData? supportedPlatforms = null,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableArray<string> tags = default,
            bool isComplexTextEdit = false)
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
                displayTextPrefix: null,
                inlineDescription: null,
                glyph: null,
                supportedPlatforms,
                properties,
                tags,
                isComplexTextEdit);
        }

        public static CompletionItem CreateWithSymbolId(
            string displayText,
            string? displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string? sortText = null,
            string? insertionText = null,
            string? filterText = null,
            string? displayTextPrefix = null,
            string? inlineDescription = null,
            Glyph? glyph = null,
            SupportedPlatformData? supportedPlatforms = null,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableArray<string> tags = default,
            bool isComplexTextEdit = false)
        {
            return CreateWorker(
                displayText, displayTextSuffix, symbols, rules, contextPosition,
                s_addSymbolEncoding, sortText, insertionText,
                filterText, supportedPlatforms, properties, tags, displayTextPrefix,
                inlineDescription, glyph, isComplexTextEdit);
        }

        public static CompletionItem CreateWithNameAndKind(
            string displayText,
            string displayTextSuffix,
            IReadOnlyList<ISymbol> symbols,
            CompletionItemRules rules,
            int contextPosition,
            string? sortText = null,
            string? insertionText = null,
            string? filterText = null,
            string? displayTextPrefix = null,
            string? inlineDescription = null,
            Glyph? glyph = null,
            SupportedPlatformData? supportedPlatforms = null,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableArray<string> tags = default,
            bool isComplexTextEdit = false)
        {
            return CreateWorker(
                displayText, displayTextSuffix, symbols, rules, contextPosition,
                s_addSymbolInfo, sortText, insertionText,
                filterText, supportedPlatforms, properties, tags,
                displayTextPrefix, inlineDescription, glyph, isComplexTextEdit);
        }

        internal static string? GetSymbolName(CompletionItem item)
            => item.Properties.TryGetValue("SymbolName", out var name) ? name : null;

        internal static SymbolKind? GetKind(CompletionItem item)
            => item.Properties.TryGetValue("SymbolKind", out var kind) ? (SymbolKind?)int.Parse(kind) : null;

        internal static bool GetSymbolIsGeneric(CompletionItem item)
            => item.Properties.TryGetValue("IsGeneric", out var v) && bool.TryParse(v, out var isGeneric) && isGeneric;

        public static async Task<CompletionDescription> GetDescriptionAsync(
            CompletionItem item, IReadOnlyList<ISymbol> symbols, Document document, SemanticModel semanticModel, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var position = GetDescriptionPosition(item);
            var supportedPlatforms = GetSupportedPlatforms(item, document.Project.Solution);

            if (symbols.Count != 0)
            {
                return await CommonCompletionUtilities.CreateDescriptionAsync(document.Project.Solution.Workspace.Services, semanticModel, position, symbols, options, supportedPlatforms, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }
    }
}
