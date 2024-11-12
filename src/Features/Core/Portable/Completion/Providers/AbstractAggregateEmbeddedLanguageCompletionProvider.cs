// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

/// <summary>
/// The singular completion provider that will hook into completion and will
/// provide all completions across all embedded languages.
/// 
/// Completions for an individual language are provided by
/// <see cref="IEmbeddedLanguage.CompletionProvider"/>.
/// </summary>
internal abstract class AbstractAggregateEmbeddedLanguageCompletionProvider : LSPCompletionProvider
{
    public const string EmbeddedProviderName = "EmbeddedProvider";

    private ImmutableArray<IEmbeddedLanguage> _languageProviders;

    protected AbstractAggregateEmbeddedLanguageCompletionProvider(IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices, string languageName)
    {
        var embeddedLanguageServiceType = typeof(IEmbeddedLanguagesProvider).AssemblyQualifiedName;
        TriggerCharacters = languageServices
            .Where(lazyLanguageService => IsEmbeddedLanguageProvider(lazyLanguageService, languageName, embeddedLanguageServiceType))
            .SelectMany(lazyLanguageService => ((IEmbeddedLanguagesProvider)lazyLanguageService.Value).Languages)
            .SelectMany(GetTriggerCharactersForEmbeddedLanguage)
            .ToImmutableHashSet();
    }

    private static ImmutableHashSet<char> GetTriggerCharactersForEmbeddedLanguage(IEmbeddedLanguage language)
    {
        var completionProvider = language.CompletionProvider;
        if (completionProvider != null)
        {
            return completionProvider.TriggerCharacters;
        }

        return [];
    }

    private static bool IsEmbeddedLanguageProvider(Lazy<ILanguageService, LanguageServiceMetadata> lazyLanguageService, string languageName, string? embeddedLanguageServiceType)
    {
        return lazyLanguageService.Metadata.Language == languageName && lazyLanguageService.Metadata.ServiceType == embeddedLanguageServiceType;
    }

    protected ImmutableArray<IEmbeddedLanguage> GetLanguageProviders(Host.LanguageServices? languageServices)
    {
        if (_languageProviders.IsDefault)
        {
            var languagesProvider = languageServices?.GetService<IEmbeddedLanguagesProvider>();
            ImmutableInterlocked.InterlockedInitialize(ref _languageProviders, languagesProvider?.Languages ?? []);
        }

        return _languageProviders;
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; }

    internal sealed override bool ShouldTriggerCompletion(LanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, OptionSet passThroughOptions)
    {
        foreach (var language in GetLanguageProviders(languageServices))
        {
            var completionProvider = language.CompletionProvider;
            if (completionProvider != null)
            {
                if (completionProvider.ShouldTriggerCompletion(text, caretPosition, trigger))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        foreach (var language in GetLanguageProviders(context.Document.Project.Services))
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

    public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        => GetLanguage(item).CompletionProvider!.GetChangeAsync(document, item, commitKey, cancellationToken);

    internal override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => GetLanguage(item).CompletionProvider!.GetDescriptionAsync(document, item, cancellationToken);

    private IEmbeddedLanguage GetLanguage(CompletionItem item)
    {
        if (_languageProviders.IsDefault)
            throw ExceptionUtilities.Unreachable();

        return _languageProviders.Single(lang => lang.CompletionProvider?.Name == item.GetProperty(EmbeddedProviderName));
    }
}
