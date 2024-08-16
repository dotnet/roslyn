// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// A per language service for constructing context dependent list of completions that 
/// can be presented to a user during typing in an editor. It aggregates completions from
/// one or more <see cref="CompletionProvider"/>s.
/// </summary>
public abstract partial class CompletionService : ILanguageService
{
    private readonly SolutionServices _services;
    private readonly ProviderManager _providerManager;

    /// <summary>
    /// Test-only switch.
    /// </summary>
    private bool _suppressPartialSemantics;

    // Prevent inheritance outside of Roslyn.
    internal CompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider)
    {
        _services = services;
        _providerManager = new(this, listenerProvider);
    }

    /// <summary>
    /// Gets the service corresponding to the specified document.
    /// </summary>
    public static CompletionService? GetService(Document? document)
        => document?.GetLanguageService<CompletionService>();

    /// <summary>
    /// Returns the providers always available to the service.
    /// This does not included providers imported via MEF composition.
    /// </summary>
    [Obsolete("Built-in providers will be ignored in a future release, please make them MEF exports instead.")]
    protected virtual ImmutableArray<CompletionProvider> GetBuiltInProviders()
        => [];

    /// <summary>
    /// The language from <see cref="LanguageNames"/> this service corresponds to.
    /// </summary>
    public abstract string Language { get; }

    /// <summary>
    /// Gets the current presentation and behavior rules.
    /// </summary>
    /// <remarks>
    /// Backward compatibility only.
    /// </remarks>
    public CompletionRules GetRules()
    {
        Debug.Fail("For backwards API compat only, should not be called");

        // Publicly available options do not affect this API.
        return GetRules(CompletionOptions.Default);
    }

    internal abstract CompletionRules GetRules(CompletionOptions options);

    /// <summary>
    /// Returns true if the character recently inserted or deleted in the text should trigger completion.
    /// </summary>
    /// <param name="text">The document text to trigger completion within </param>
    /// <param name="caretPosition">The position of the caret after the triggering action.</param>
    /// <param name="trigger">The potential triggering action.</param>
    /// <param name="roles">Optional set of roles associated with the editor state.</param>
    /// <param name="options">Optional options that override the default options.</param>
    /// <remarks>
    /// This API uses SourceText instead of Document so implementations can only be based on text, not syntax or semantics.
    /// </remarks>
    public bool ShouldTriggerCompletion(
        SourceText text,
        int caretPosition,
        CompletionTrigger trigger,
        ImmutableHashSet<string>? roles = null,
        OptionSet? options = null)
    {
        var document = text.GetOpenDocumentInCurrentContextWithChanges();
        var languageServices = document?.Project.Services ?? _services.GetLanguageServices(Language);

        // Publicly available options do not affect this API. Force complete results from this public API since
        // external consumers do not have access to Roslyn's waiters.
        var completionOptions = CompletionOptions.Default with { ForceExpandedCompletionIndexCreation = true };
        var passThroughOptions = options ?? document?.Project.Solution.Options ?? OptionSet.Empty;

        return ShouldTriggerCompletion(document?.Project, languageServices, text, caretPosition, trigger, completionOptions, passThroughOptions, roles);
    }

    internal virtual bool SupportsTriggerOnDeletion(CompletionOptions options)
        => options.TriggerOnDeletion == true;

    /// <summary>
    /// Returns true if the character recently inserted or deleted in the text should trigger completion.
    /// </summary>
    /// <param name="project">The project containing the document and text</param>
    /// <param name="languageServices">Language services</param>
    /// <param name="text">The document text to trigger completion within </param>
    /// <param name="caretPosition">The position of the caret after the triggering action.</param>
    /// <param name="trigger">The potential triggering action.</param>
    /// <param name="options">Options.</param>
    /// <param name="passThroughOptions">Options originating either from external caller of the <see cref="CompletionService"/> or set externally to <see cref="Solution.Options"/>.</param>
    /// <param name="roles">Optional set of roles associated with the editor state.</param>
    /// <remarks>
    /// We pass the project here to retrieve information about the <see cref="Project.AnalyzerReferences"/>,
    /// <see cref="WorkspaceKind"/> and <see cref="Project.Language"/> which are fast operations.
    /// It should not be used for syntactic or semantic operations.
    /// </remarks>
    internal virtual bool ShouldTriggerCompletion(
        Project? project,
        LanguageServices languageServices,
        SourceText text,
        int caretPosition,
        CompletionTrigger trigger,
        CompletionOptions options,
        OptionSet passThroughOptions,
        ImmutableHashSet<string>? roles = null)
    {
        // The trigger kind guarantees that user wants a completion.
        if (trigger.Kind is CompletionTriggerKind.Invoke or CompletionTriggerKind.InvokeAndCommitIfUnique)
            return true;

        if (!options.TriggerOnTyping)
            return false;

        // Enter does not trigger completion.
        if (trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character == '\n')
        {
            return false;
        }

        if (trigger.Kind == CompletionTriggerKind.Deletion && SupportsTriggerOnDeletion(options))
        {
            return char.IsLetterOrDigit(trigger.Character) || trigger.Character == '.';
        }

        var extensionManager = languageServices.SolutionServices.GetRequiredService<IExtensionManager>();

        var providers = _providerManager.GetFilteredProviders(project, roles, trigger, options);
        return providers.Any(p =>
            extensionManager.PerformFunction(p,
                () => p.ShouldTriggerCompletion(languageServices, text, caretPosition, trigger, options, passThroughOptions),
                defaultValue: false));
    }

    /// <summary>
    /// Gets the span of the syntax element at the caret position.
    /// This is the most common value used for <see cref="CompletionItem.Span"/>.
    /// </summary>
    /// <param name="text">The document text that completion is occurring within.</param>
    /// <param name="caretPosition">The position of the caret within the text.</param>
    [Obsolete("Not used anymore. CompletionService.GetDefaultCompletionListSpan is used instead.", error: true)]
    public virtual TextSpan GetDefaultItemSpan(SourceText text, int caretPosition)
        => GetDefaultCompletionListSpan(text, caretPosition);

    public virtual TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition)
    {
        return CommonCompletionUtilities.GetWordSpan(
            text, caretPosition, char.IsLetter, char.IsLetterOrDigit);
    }

    /// <summary>
    /// Gets the description of the item.
    /// </summary>
    /// <param name="document">This will be the  original document that
    /// <paramref name="item"/> was created against.</param>
    /// <param name="item">The item to get the description for.</param>
    /// <param name="cancellationToken"></param>
    public Task<CompletionDescription?> GetDescriptionAsync(
        Document document,
        CompletionItem item,
        CancellationToken cancellationToken = default)
    {
        // Publicly available options do not affect this API.
        return GetDescriptionAsync(document, item, CompletionOptions.Default, SymbolDescriptionOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Gets the description of the item.
    /// </summary>
    /// <param name="document">This will be the  original document that
    /// <paramref name="item"/> was created against.</param>
    /// <param name="item">The item to get the description for.</param>
    /// <param name="options">Completion options</param>
    /// <param name="displayOptions">Display options</param>
    /// <param name="cancellationToken"></param>
    internal virtual async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(item, document.Project);
        if (provider is null)
            return CompletionDescription.Empty;

        var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

        // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
        (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
        var description = await extensionManager.PerformFunctionAsync(
            provider,
            cancellationToken => provider.GetDescriptionAsync(document, item, options, displayOptions, cancellationToken),
            defaultValue: null,
            cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(semanticModel);
        return description;
    }

    /// <summary>
    /// Gets the change to be applied when the item is committed.
    /// </summary>
    /// <param name="document">The document that completion is occurring within.</param>
    /// <param name="item">The item to get the change for.</param>
    /// <param name="commitCharacter">The typed character that caused the item to be committed. 
    /// This character may be used as part of the change. 
    /// This value is null when the commit was caused by the [TAB] or [ENTER] keys.</param>
    /// <param name="cancellationToken"></param>
    public virtual async Task<CompletionChange> GetChangeAsync(
        Document document,
        CompletionItem item,
        char? commitCharacter = null,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(item, document.Project);
        if (provider != null)
        {
            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

            // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
            (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);

            var change = await extensionManager.PerformFunctionAsync(
                provider,
                cancellationToken => provider.GetChangeAsync(document, item, commitCharacter, cancellationToken),
                defaultValue: null!,
                cancellationToken).ConfigureAwait(false);
            if (change == null)
                return CompletionChange.Create(new TextChange(new TextSpan(), ""));

            GC.KeepAlive(semanticModel);
            Debug.Assert(item.Span == change.TextChange.Span || item.IsComplexTextEdit);
            return change;
        }
        else
        {
            return CompletionChange.Create(new TextChange(item.Span, item.DisplayText));
        }
    }

    // The FilterItems method might need to handle a large list of items when import completion is enabled and filter text is
    // very short, i.e. <= 1. Therefore, use pooled list to avoid repeated (potentially LOH) allocations.
    private static readonly ObjectPool<List<MatchResult>> s_listOfMatchResultPool = new(factory: () => []);

    /// <summary>
    /// Given a list of completion items that match the current code typed by the user,
    /// returns the item that is considered the best match, and whether or not that
    /// item should be selected or not.
    /// 
    /// itemToFilterText provides the values that each individual completion item should
    /// be filtered against.
    /// </summary>
    public virtual ImmutableArray<CompletionItem> FilterItems(
        Document document,
        ImmutableArray<CompletionItem> items,
        string filterText)
    {
        using var helper = new PatternMatchHelper(filterText);
        var filterDataList = new SegmentedList<MatchResult>(
            items.Select(item => helper.GetMatchResult(item, includeMatchSpans: false, CultureInfo.CurrentCulture)));

        var builder = s_listOfMatchResultPool.Allocate();
        try
        {
            FilterItems(CompletionHelper.GetHelper(document), filterDataList, filterText, builder);
            return builder.SelectAsArray(result => result.CompletionItem);
        }
        finally
        {
            // Don't call ClearAndFree, which resets the capacity to a default value.
            builder.Clear();
            s_listOfMatchResultPool.Free(builder);
        }
    }

    internal virtual void FilterItems(
       Document document,
       IReadOnlyList<MatchResult> matchResults,
       string filterText,
       IList<MatchResult> builder)
    {
#pragma warning disable RS0030 // Do not used banned APIs
        // Default implementation just drops the pattern matches and builder, and calls the public overload of FilterItems instead for compatibility.
        var filteredItems = FilterItems(document, matchResults.SelectAsArray(item => item.CompletionItem), filterText);
#pragma warning restore RS0030 // Do not used banned APIs

        using var completionPatternMatchers = new PatternMatchHelper(filterText);
        builder.AddRange(filteredItems.Select(item => completionPatternMatchers.GetMatchResult(item, includeMatchSpans: false, CultureInfo.CurrentCulture)));
    }

    /// <summary>
    /// Determine among the provided items the best match w.r.t. the given filter text, 
    /// those returned would be considered equally good candidates for selection by controller.
    /// </summary>
    internal static void FilterItems(
        CompletionHelper completionHelper,
        IReadOnlyList<MatchResult> matchResults,
        string filterText,
        IList<MatchResult> builder)
    {
        // It's very common for people to type expecting completion to fix up their casing,
        // so if no uppercase characters were typed so far, we'd loosen our standard on comparing items
        // in terms of case-sensitivity and take into consideration the MatchPriority in certain scenarios.
        // i.e. when everything else is equal, if item1 is a better case-sensitive match but has
        // MatchPriority.Deprioritize, and item2 is not MatchPriority.Deprioritize, then we consider
        // item2 a better match.
        var filterTextHasNoUpperCase = !filterText.Any(char.IsUpper);

        foreach (var matchResult in matchResults)
        {
            if (!matchResult.ShouldBeConsideredMatchingFilterText)
                continue;

            if (builder.Count == 0)
            {
                // We've found no good items yet.  So this is the best item currently.
                builder.Add(matchResult);
                continue;
            }

            var comparison = completionHelper.CompareMatchResults(matchResult, builder[0], filterTextHasNoUpperCase);

            if (comparison == 0)
            {
                // This item is as good as the items we've been collecting.  We'll return it and let the controller
                // decide what to do.  (For example, it will pick the one that has the best MRU index).
                builder.Add(matchResult);
            }
            else if (comparison < 0)
            {
                // This item is strictly better than the best items we've found so far.
                builder.Clear();
                builder.Add(matchResult);
            }
        }
    }

    /// <summary>
    /// Don't call. Used for pre-populating MEF providers only.
    /// </summary>
    internal void LoadImportedProviders()
        => _providerManager.LoadProviders();

    /// <summary>
    /// Don't call. Used for pre-load project providers only.
    /// </summary>
    internal void TriggerLoadProjectProviders(Project project, CompletionOptions options)
            => _providerManager.GetCachedProjectCompletionProvidersOrQueueLoadInBackground(project, options);

    internal CompletionProvider? GetProvider(CompletionItem item, Project? project)
        => _providerManager.GetProvider(item, project);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(CompletionService completionServiceWithProviders)
    {
        private readonly CompletionService _completionServiceWithProviders = completionServiceWithProviders;

        public ImmutableArray<CompletionProvider> GetImportedAndBuiltInProviders(ImmutableHashSet<string> roles)
            => _completionServiceWithProviders._providerManager.GetTestAccessor().GetImportedAndBuiltInProviders(roles);

        public ImmutableArray<CompletionProvider> GetProjectProviders(Project project)
            => _completionServiceWithProviders._providerManager.GetTestAccessor().GetProjectProviders(project);

        public async Task<CompletionContext> GetContextAsync(
            CompletionProvider provider,
            Document document,
            int position,
            CompletionTrigger triggerInfo,
            CompletionOptions options,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = _completionServiceWithProviders.GetDefaultCompletionListSpan(text, position);

            return await CompletionService.GetContextAsync(
                provider,
                document,
                position,
                triggerInfo,
                options,
                defaultItemSpan,
                sharedContext: null,
                cancellationToken).ConfigureAwait(false);
        }

        public void SuppressPartialSemantics()
            => _completionServiceWithProviders._suppressPartialSemantics = true;
    }
}
