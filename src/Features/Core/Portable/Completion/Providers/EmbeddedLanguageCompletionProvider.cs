// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// The singular completion provider that will hook into completion and will
    /// provider all completions across all embedded languages.
    /// 
    /// Completions for an individual language are provided by
    /// <see cref="IEmbeddedLanguageFeatures.CompletionProvider"/>.
    /// </summary>
    internal class EmbeddedLanguageCompletionProvider : CompletionProvider
    {
        public const string EmbeddedProviderName = "EmbeddedProvider";

        private readonly ImmutableArray<IEmbeddedLanguage> _languageProviders;

        public EmbeddedLanguageCompletionProvider(IEmbeddedLanguagesProvider languagesProvider)
        {
            _languageProviders = languagesProvider?.Languages ?? ImmutableArray<IEmbeddedLanguage>.Empty;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            foreach (var language in _languageProviders)
            {
                var completionProvider = (language as IEmbeddedLanguageFeatures)?.CompletionProvider;
                if (completionProvider != null)
                {
                    if (completionProvider.ShouldTriggerCompletion(
                            text, caretPosition, trigger, options))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            foreach (var language in _languageProviders)
            {
                var completionProvider = (language as IEmbeddedLanguageFeatures)?.CompletionProvider;
                if (completionProvider != null)
                {
                    var count = context.Items.Count;
                    await completionProvider.ProvideCompletionsAsync(context).ConfigureAwait(false);

                    if (context.Items.Count > count)
                    {
                        return;
                    }
                }
            }
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
            => GetLanguage(item).CompletionProvider.GetChangeAsync(document, item, commitKey, cancellationToken);

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => GetLanguage(item).CompletionProvider.GetDescriptionAsync(document, item, cancellationToken);

        private IEmbeddedLanguageFeatures GetLanguage(CompletionItem item)
            => (IEmbeddedLanguageFeatures)_languageProviders.Single(lang => (lang as IEmbeddedLanguageFeatures)?.CompletionProvider?.Name == item.Properties[EmbeddedProviderName]);
    }
}
