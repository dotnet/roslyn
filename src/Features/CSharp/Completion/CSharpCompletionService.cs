// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    [ExportLanguageService(typeof(ICompletionService), LanguageNames.CSharp), Shared]
    internal class CSharpCompletionService : AbstractCompletionService
    {
        private readonly IEnumerable<CompletionListProvider> _defaultCompletionProviders =
            new CompletionListProvider[]
            {
                new AttributeNamedParameterCompletionProvider(),
                new NamedParameterCompletionProvider(),
                new SpeculativeTCompletionProvider(),
                new KeywordCompletionProvider(),
                new SymbolCompletionProvider(),
                new ExplicitInterfaceCompletionProvider(),
                new ObjectCreationCompletionProvider(),
                new ObjectInitializerCompletionProvider(),
                new SpeculativeTCompletionProvider(),
                new CSharpSuggestionModeCompletionProvider(),
                new EnumAndCompletionListTagCompletionProvider(),
                new CrefCompletionProvider(),
                new SnippetCompletionProvider(),
                new ExternAliasCompletionProvider(),
            }.ToImmutableArray();

        private CSharpCompletionService()
            : base(LanguageNames.CSharp)
        {
        }

        public override IEnumerable<CompletionListProvider> GetDefaultCompletionProviders()
        {
            return _defaultCompletionProviders;
        }

        public override async Task<TextSpan> GetDefaultTrackingSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        protected override CompletionRules CreateCompletionRules(MostRecentlyUsedList mostRecentlyUsedList)
        {
            return new CSharpCompletionRules(mostRecentlyUsedList);
        }
    }
}
