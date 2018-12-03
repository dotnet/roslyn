// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractEmbeddedLanguageCompletionProvider : CompletionProvider
    {
        public const string EmbeddedProviderName = "EmbeddedProvider";

        private readonly IEmbeddedLanguageFeaturesProvider _languagesProvider;

        protected AbstractEmbeddedLanguageCompletionProvider(IEmbeddedLanguageFeaturesProvider languagesProvider)
        {
            _languagesProvider = languagesProvider;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (_languagesProvider != null)
            {
                foreach (var language in _languagesProvider.Languages)
                {
                    var completionProvider = language.CompletionProvider;
                    if (completionProvider != null)
                    {
                        if (completionProvider.ShouldTriggerCompletion(
                                text, caretPosition, trigger, options))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (_languagesProvider != null)
            {
                foreach (var language in _languagesProvider.Languages)
                {
                    var completionProvider = language.CompletionProvider;
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
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
            => GetLanguage(item).CompletionProvider.GetChangeAsync(document, item, commitKey, cancellationToken);

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => GetLanguage(item).CompletionProvider.GetDescriptionAsync(document, item, cancellationToken);

        private IEmbeddedLanguageFeatures GetLanguage(CompletionItem item)
            => _languagesProvider.Languages.Single(lang => lang.CompletionProvider?.Name == item.Properties[EmbeddedProviderName]);
    }
}
