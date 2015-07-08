// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    [ExportLanguageService(typeof(ICompletionService), LanguageNames.CSharp), Shared]
    internal class CSharpCompletionService : AbstractCompletionService
    {
        private readonly IEnumerable<ICompletionProvider> _defaultCompletionProviders =
            new ICompletionProvider[]
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

        public override IEnumerable<ICompletionProvider> GetDefaultCompletionProviders()
        {
            return _defaultCompletionProviders;
        }

        public override async Task<TextSpan> GetDefaultTrackingSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        protected override bool TriggerOnBackspace(SourceText text, int position, CompletionTriggerInfo triggerInfo, OptionSet options)
        {
            return false;
        }

        protected override string GetLanguageName()
        {
            return LanguageNames.CSharp;
        }
    }
}
