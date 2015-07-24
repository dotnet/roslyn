// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class SymbolCompletionItem : CompletionItem
    {
        public readonly AbstractSyntaxContext Context;
        public readonly string InsertionText;
        public readonly int Position;
        public readonly List<ISymbol> Symbols;
        private readonly SupportedPlatformData _supportedPlatforms;

        public SymbolCompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            string insertionText,
            TextSpan filterSpan,
            int position,
            List<ISymbol> symbols,
            AbstractSyntaxContext context,
            bool preselect = false,
            SupportedPlatformData supportedPlatforms = null,
            CompletionItemRules rules = null)
            : this(completionProvider,
                  displayText,
                  insertionText,
                  filterText: displayText.Length > 0 && displayText[0] == '@' ? displayText : symbols[0].Name,
                  filterSpan: filterSpan,
                  position: position, symbols: symbols, context: context, preselect: preselect, supportedPlatforms: supportedPlatforms, rules: rules)
        {
        }

        public SymbolCompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            string insertionText,
            string filterText,
            TextSpan filterSpan,
            int position,
            List<ISymbol> symbols,
            AbstractSyntaxContext context,
            bool preselect = false,
            SupportedPlatformData supportedPlatforms = null,
            CompletionItemRules rules = null)
        : base(completionProvider, displayText, filterSpan,
           descriptionFactory: null, glyph: symbols[0].GetGlyph(),
           sortText: symbols[0].Name, filterText: filterText, preselect: preselect, showsWarningIcon: supportedPlatforms != null, rules: rules)
        {
            this.InsertionText = insertionText;
            this.Position = position;
            this.Symbols = symbols;
            this.Context = context;
            _supportedPlatforms = supportedPlatforms;
        }

        public SymbolCompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            string insertionText,
            string filterText,
            TextSpan filterSpan,
            int position,
            List<ISymbol> symbols,
            string sortText,
            AbstractSyntaxContext context,
            Glyph glyph,
            bool preselect = false,
            SupportedPlatformData supportedPlatforms = null,
            CompletionItemRules rules = null)
        : base(completionProvider, displayText, filterSpan,
           descriptionFactory: null, glyph: glyph,
           sortText: sortText, filterText: filterText, preselect: preselect, showsWarningIcon: supportedPlatforms != null, rules: rules)
        {
            this.InsertionText = insertionText;
            this.Position = position;
            this.Symbols = symbols;
            this.Context = context;
            _supportedPlatforms = supportedPlatforms;
        }

        public override async Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.LazyDescription == null)
            {
                Interlocked.CompareExchange(
                    ref this.LazyDescription,
                    new AsyncLazy<ImmutableArray<SymbolDisplayPart>>(
                        CommonCompletionUtilities.CreateDescriptionFactory(this.Context.Workspace, this.Context.SemanticModel, this.Position, this.Symbols, _supportedPlatforms), cacheResult: true),
                    null);
            }

            return await base.GetDescriptionAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
