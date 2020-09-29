// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// The singular completion provider that will hook into completion and will
    /// provider all completions across all embedded languages.
    /// 
    /// Completions for an individual language are provided by
    /// <see cref="IEmbeddedLanguageFeatures.CompletionProvider"/>.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageCompletionProvider : CompletionProvider
    {
        public const string EmbeddedProviderName = "EmbeddedProvider";

        private ImmutableArray<IEmbeddedLanguage> _languageProviders;

        protected AbstractEmbeddedLanguageCompletionProvider()
        {
        }

        protected ImmutableArray<IEmbeddedLanguage> GetLanguageProviders<T>(Func<T, Document?> documentProvider, T state)
        {
            if (_languageProviders.IsDefault)
            {
                var languagesProvider = documentProvider(state)?.Project.LanguageServices.GetService<IEmbeddedLanguagesProvider>();
                ImmutableInterlocked.InterlockedInitialize(ref _languageProviders, languagesProvider?.Languages ?? ImmutableArray<IEmbeddedLanguage>.Empty);
            }

            return _languageProviders;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            foreach (var language in GetLanguageProviders(text => text.GetOpenDocumentInCurrentContextWithChanges(), text))
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
            foreach (var language in GetLanguageProviders(context => context.Document, context))
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
        {
            if (_languageProviders.IsDefault)
                throw ExceptionUtilities.Unreachable;

            return (IEmbeddedLanguageFeatures)_languageProviders.Single(lang => (lang as IEmbeddedLanguageFeatures)?.CompletionProvider?.Name == item.Properties[EmbeddedProviderName]);
        }
    }
}
