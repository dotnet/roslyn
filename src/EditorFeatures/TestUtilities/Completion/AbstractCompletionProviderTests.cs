// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion;

[UseExportProvider]
public abstract class AbstractCompletionProviderTests<TWorkspaceFixture> : TestBase
    where TWorkspaceFixture : TestWorkspaceFixture, new()
{
    private static readonly TestComposition s_baseComposition = EditorTestCompositions.EditorFeatures.AddExcludedPartTypes(typeof(CompletionProvider));

    private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();

    protected readonly Mock<ICompletionSession> MockCompletionSession;

    protected bool? ShowTargetTypedCompletionFilter { get; set; }
    protected bool? ShowImportCompletionItemsOptionValue { get; set; }
    protected bool? ForceExpandedCompletionIndexCreation { get; set; }
    protected bool? HideAdvancedMembers { get; set; }
    protected bool? ShowNameSuggestions { get; set; }
    protected bool? ShowNewSnippetExperience { get; set; }
    protected bool? TriggerOnDeletion { get; set; }

    protected AbstractCompletionProviderTests()
    {
        MockCompletionSession = new Mock<ICompletionSession>(MockBehavior.Strict);
    }

    internal virtual OptionsCollection NonCompletionOptions
        => null;

    private CompletionOptions GetCompletionOptions()
    {
        var options = CompletionOptions.Default;

        if (ShowTargetTypedCompletionFilter.HasValue)
            options = options with { TargetTypedCompletionFilter = ShowTargetTypedCompletionFilter.Value };

        if (ShowImportCompletionItemsOptionValue.HasValue)
            options = options with { ShowItemsFromUnimportedNamespaces = ShowImportCompletionItemsOptionValue.Value };

        if (ForceExpandedCompletionIndexCreation.HasValue)
            options = options with { ForceExpandedCompletionIndexCreation = ForceExpandedCompletionIndexCreation.Value };

        if (HideAdvancedMembers.HasValue)
            options = options with { MemberDisplayOptions = new() { HideAdvancedMembers = HideAdvancedMembers.Value } };

        if (ShowNameSuggestions.HasValue)
            options = options with { ShowNameSuggestions = ShowNameSuggestions.Value };

        if (ShowNewSnippetExperience.HasValue)
            options = options with { ShowNewSnippetExperienceUserOption = ShowNewSnippetExperience.Value };

        if (TriggerOnDeletion.HasValue)
            options = options with { TriggerOnDeletion = TriggerOnDeletion.Value };

        return options;
    }

    protected virtual TestComposition GetComposition()
        => s_baseComposition.AddParts(GetCompletionProviderType());

    private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
        => _fixtureHelper.GetOrCreateFixture();

    protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
    {
        var service = document.GetLanguageService<ISyntaxFactsService>();
        var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

        return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
    }

    internal virtual CompletionService GetCompletionService(Project project)
    {
        var completionService = project.Services.GetRequiredService<CompletionService>();
        var completionProviders = completionService.GetTestAccessor().GetImportedAndBuiltInProviders([]);
        Assert.Contains(completionProviders, p => p.GetType() == GetCompletionProviderType());

        return completionService;
    }

    internal static ImmutableHashSet<string> GetRoles(Document document)
        => document.SourceCodeKind == SourceCodeKind.Regular ? [] : [PredefinedInteractiveTextViewRoles.InteractiveTextViewRole];

    protected abstract string ItemPartiallyWritten(string expectedItemOrNull);

    protected abstract EditorTestWorkspace CreateWorkspace(string fileContents);

    private protected abstract Task BaseVerifyWorkerAsync(
        string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options,
        bool skipSpeculation = false);

    private protected abstract Task BaseVerifyWorkerAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false);

    internal Task<CompletionList> GetCompletionListAsync(
        CompletionService service,
        Document document,
        int position,
        RoslynCompletion.CompletionTrigger triggerInfo,
        CompletionOptions options = null)
        => service.GetCompletionsAsync(document, position, options ?? GetCompletionOptions(), TestOptionSet.Empty, triggerInfo, GetRoles(document));

    private protected async Task CheckResultsAsync(
        Document document, int position, string expectedItemOrNull,
        string expectedDescriptionOrNull, bool usePreviousCharAsTrigger,
        char? deletedCharTrigger,
        bool checkForAbsence, Glyph? glyph, int? matchPriority,
        bool? hasSuggestionModeItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription,
        bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters,
        CompletionItemFlags? flags,
        CompletionOptions options)
    {
        var expectedResult = new ItemExpectation(
            Name: expectedItemOrNull,
            IsAbsent: checkForAbsence,
            ExpectedDescription: expectedDescriptionOrNull,
            Glyph: glyph,
            MatchPriority: matchPriority,
            DisplayTextSuffix: displayTextSuffix,
            DisplayTextPrefix: displayTextPrefix,
            InlineDescription: inlineDescription,
            IsComplexTextEdit: isComplexTextEdit);

        await CheckResultsAsync(document, position, usePreviousCharAsTrigger, deletedCharTrigger,
            hasSuggestionModeItem, [expectedResult], matchingFilters, flags, options);
    }

    private protected async Task CheckResultsAsync(
        Document document, int position, bool usePreviousCharAsTrigger,
        char? deletedCharTrigger, bool? hasSuggestionModeItem,
        ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters,
        CompletionItemFlags? flags,
        CompletionOptions options)
    {
        options ??= GetCompletionOptions();

        var code = (await document.GetTextAsync()).ToString();

        var trigger = RoslynCompletion.CompletionTrigger.Invoke;

        if (usePreviousCharAsTrigger)
        {
            trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1));
        }
        else if (deletedCharTrigger.HasValue)
        {
            trigger = RoslynCompletion.CompletionTrigger.CreateDeletionTrigger(deletedCharacter: deletedCharTrigger.Value);
        }

        var displayOptions = SymbolDescriptionOptions.Default;
        var completionService = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(completionService, document, position, trigger, options);
        var items = completionList.ItemsList;

        if (hasSuggestionModeItem != null)
        {
            Assert.Equal(hasSuggestionModeItem.Value, completionList.SuggestionModeItem != null);
        }

        if (expectedResults.Length == 0)
        {
            Assert.Equal(items.Count, 0);
        }

        foreach (var result in expectedResults)
        {
            if (result.SourceCodeKind is not null && result.SourceCodeKind != document.SourceCodeKind)
                continue;

            if (result.IsAbsent)
            {
                if (items == null)
                {
                    return;
                }

                if (result.Name == null)
                {
                    Assert.Empty(items);
                }
                else
                {
                    AssertEx.None(
                        items,
                        c => CompareItems(c.DisplayText, result.Name)
                            && CompareItems(c.DisplayTextSuffix, result.DisplayTextSuffix ?? "")
                            && CompareItems(c.DisplayTextPrefix, result.DisplayTextPrefix ?? "")
                            && CompareItems(c.InlineDescription, result.InlineDescription ?? "")
                            && (result.ExpectedDescription != null ? completionService.GetDescriptionAsync(document, c, options, displayOptions).Result.Text == result.ExpectedDescription : true));
                }
            }
            else
            {
                if (result.Name == null)
                {
                    Assert.NotEmpty(items);
                }
                else
                {
                    AssertEx.Any(items, Predicate);
                }
            }

            bool Predicate(RoslynCompletion.CompletionItem c)
            {
                if (!CompareItems(c.DisplayText, result.Name))
                    return false;
                if (!CompareItems(c.DisplayTextSuffix, result.DisplayTextSuffix ?? ""))
                    return false;
                if (!CompareItems(c.DisplayTextPrefix, result.DisplayTextPrefix ?? ""))
                    return false;
                if (!CompareItems(c.InlineDescription, result.InlineDescription ?? ""))
                    return false;
                if (result.ExpectedDescription != null && completionService.GetDescriptionAsync(document, c, options, displayOptions).Result.Text != result.ExpectedDescription)
                    return false;
                if (result.Glyph.HasValue && !c.Tags.SequenceEqual(GlyphTags.GetTags(result.Glyph.Value)))
                    return false;
                if (result.MatchPriority.HasValue && c.Rules.MatchPriority != result.MatchPriority.Value)
                    return false;
                if (matchingFilters != null && !FiltersMatch(matchingFilters, c))
                    return false;
                if (flags != null && flags.Value != c.Flags)
                    return false;
                if (result.IsComplexTextEdit is bool textEdit && textEdit != c.IsComplexTextEdit)
                    return false;

                return true;
            }
        }
    }

    private protected record ItemExpectation(
        string Name,
        bool IsAbsent,
        string ExpectedDescription = null,
        Glyph? Glyph = null,
        int? MatchPriority = null,
        string DisplayTextSuffix = null,
        string DisplayTextPrefix = null,
        string InlineDescription = null,
        bool? IsComplexTextEdit = null,
        SourceCodeKind? SourceCodeKind = null)
    {
        public static ItemExpectation[] None = CreateGeneralMatchingArray(true);
        public static ItemExpectation[] Any = CreateGeneralMatchingArray(false);

        private static ItemExpectation[] CreateGeneralMatchingArray(bool absent)
        {
            return [new ItemExpectation(Name: null, IsAbsent: absent)];
        }

        public static ItemExpectation Exists(string name)
        {
            return new ItemExpectation(name, false);
        }
        public static ItemExpectation Absent(string name)
        {
            return new ItemExpectation(name, true);
        }
    }

    private static bool FiltersMatch(List<CompletionFilter> expectedMatchingFilters, RoslynCompletion.CompletionItem item)
    {
        var matchingFilters = FilterSet.GetFilters(item);

        // Check that the list has no duplicates.
        Assert.Equal(matchingFilters.Count, matchingFilters.Distinct().Count());
        return expectedMatchingFilters.SetEquals(matchingFilters);
    }

    private async Task VerifyAsync(
        string markup, string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind? sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionModeItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
    {
        SourceCodeKind[] evaluatedSourceCodeKinds = sourceCodeKind.HasValue ? [sourceCodeKind.Value] : [SourceCodeKind.Regular, SourceCodeKind.Script];
        foreach (var sourceKind in evaluatedSourceCodeKinds)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(markup, GetComposition());
            var code = workspaceFixture.Target.Code;
            var position = workspaceFixture.Target.Position;

            // Set options that are not CompletionOptions
            workspace.SetAnalyzerFallbackAndGlobalOptions(NonCompletionOptions);

            await VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionModeItem, displayTextSuffix, displayTextPrefix,
                inlineDescription, isComplexTextEdit, matchingFilters, flags,
                options, skipSpeculation: skipSpeculation).ConfigureAwait(false);
        }
    }

    private async Task VerifyAsync(
        string markup, SourceCodeKind? sourceCodeKind, char? deletedCharTrigger, bool usePreviousCharAsTrigger,
        ItemExpectation[] results, bool? hasSuggestionModeItem,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
    {
        SourceCodeKind[] evaluatedSourceCodeKinds = sourceCodeKind.HasValue ? [sourceCodeKind.Value] : [SourceCodeKind.Regular, SourceCodeKind.Script];
        foreach (var sourceKind in evaluatedSourceCodeKinds)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(markup, GetComposition());
            var code = workspaceFixture.Target.Code;
            var position = workspaceFixture.Target.Position;

            // Set options that are not CompletionOptions
            NonCompletionOptions?.SetGlobalOptions(workspace.GlobalOptions);

            await VerifyWorkerAsync(
                code, position, usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionModeItem,
                sourceKind, results, matchingFilters, flags, options, skipSpeculation);
        }
    }

    protected async Task<CompletionList> GetCompletionListAsync(string markup, string workspaceKind = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();
        var workspace = workspaceFixture.Target.GetWorkspace(markup, GetComposition(), workspaceKind: workspaceKind);

        // Set options that are not CompletionOptions
        workspace.SetAnalyzerFallbackAndGlobalOptions(NonCompletionOptions);

        var currentDocument = workspace.CurrentSolution.GetDocument(workspaceFixture.Target.CurrentDocument.Id);
        var position = workspaceFixture.Target.Position;

        var options = GetCompletionOptions();
        return await GetCompletionListAsync(GetCompletionService(currentDocument.Project), currentDocument, position, RoslynCompletion.CompletionTrigger.Invoke, options).ConfigureAwait(false);
    }

    protected async Task VerifyCustomCommitProviderAsync(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind? sourceCodeKind = null, char? commitChar = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();
        using (workspaceFixture.Target.GetWorkspace(markupBeforeCommit, GetComposition()))
        {
            var code = workspaceFixture.Target.Code;
            var position = workspaceFixture.Target.Position;

            if (sourceCodeKind.HasValue)
            {
                await VerifyCustomCommitProviderWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, sourceCodeKind.Value, commitChar);
            }
            else
            {
                await VerifyCustomCommitProviderWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, SourceCodeKind.Regular, commitChar);
                await VerifyCustomCommitProviderWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, SourceCodeKind.Script, commitChar);
            }
        }
    }

    protected async Task VerifyProviderCommitAsync(
        string markupBeforeCommit,
        string itemToCommit,
        string expectedCodeAfterCommit,
        char? commitChar,
        SourceCodeKind? sourceCodeKind = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        workspaceFixture.Target.GetWorkspace(markupBeforeCommit, GetComposition());

        var code = workspaceFixture.Target.Code;
        var position = workspaceFixture.Target.Position;

        expectedCodeAfterCommit = expectedCodeAfterCommit.NormalizeLineEndings();
        if (sourceCodeKind.HasValue)
        {
            await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, sourceCodeKind.Value);
        }
        else
        {
            await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, SourceCodeKind.Regular);
            await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, SourceCodeKind.Script);
        }
    }

    protected bool CompareItems(string actualItem, string expectedItem)
        => GetStringComparer().Equals(actualItem, expectedItem);

    protected virtual IEqualityComparer<string> GetStringComparer()
        => StringComparer.Ordinal;

    private protected Task VerifyItemExistsAsync(
        string markup, string expectedItem, string expectedDescriptionOrNull = null,
        SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, char? deletedCharTrigger = null,
        Glyph? glyph = null, int? matchPriority = null, bool? hasSuggestionModeItem = null,
        string displayTextSuffix = null, string displayTextPrefix = null, string inlineDescription = null,
        bool? isComplexTextEdit = null, List<CompletionFilter> matchingFilters = null,
        CompletionItemFlags? flags = null, CompletionOptions options = null, bool skipSpeculation = false)
        => VerifyAsync(markup, expectedItem, expectedDescriptionOrNull,
            sourceCodeKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence: false,
            glyph: glyph, matchPriority: matchPriority,
            hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
            displayTextPrefix: displayTextPrefix, inlineDescription: inlineDescription,
            isComplexTextEdit: isComplexTextEdit, matchingFilters: matchingFilters,
            flags: flags, options, skipSpeculation: skipSpeculation);

    private protected Task VerifyItemIsAbsentAsync(
        string markup, string expectedItem, string expectedDescriptionOrNull = null,
        SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, char? deletedCharTrigger = null,
        bool? hasSuggestionModeItem = null, string displayTextSuffix = null,
        string displayTextPrefix = null, string inlineDescription = null,
        bool? isComplexTextEdit = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null)
        => VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, sourceCodeKind,
            usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence: true, glyph: null, matchPriority: null,
            hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
            displayTextPrefix: displayTextPrefix, inlineDescription: inlineDescription,
            isComplexTextEdit: isComplexTextEdit, matchingFilters: matchingFilters, flags: flags, options);

    private protected Task VerifyAnyItemExistsAsync(
        string markup, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
        bool? hasSuggestionModeItem = null, CompletionOptions options = null)
        => VerifyExpectedItemsAsync(
            markup, results: ItemExpectation.Any, sourceCodeKind, usePreviousCharAsTrigger: usePreviousCharAsTrigger,
            hasSuggestionModeItem: hasSuggestionModeItem, options: options);

    private protected Task VerifyNoItemsExistAsync(
        string markup, SourceCodeKind? sourceCodeKind = null,
        bool usePreviousCharAsTrigger = false, bool? hasSuggestionModeItem = null,
        CompletionOptions options = null)
        => VerifyExpectedItemsAsync(
            markup, results: ItemExpectation.None, sourceCodeKind, usePreviousCharAsTrigger: usePreviousCharAsTrigger,
            hasSuggestionModeItem: hasSuggestionModeItem, options: options);

    private protected Task VerifyExpectedItemsAsync(
        string markup, ItemExpectation[] results,
        SourceCodeKind? sourceCodeKind = null,
        char? deletedCharTrigger = null,
        bool usePreviousCharAsTrigger = false,
        bool? hasSuggestionModeItem = null, CompletionOptions options = null)
        => VerifyAsync(markup,
            sourceCodeKind, deletedCharTrigger: deletedCharTrigger, usePreviousCharAsTrigger, results: results,
            hasSuggestionModeItem: hasSuggestionModeItem, matchingFilters: null,
            flags: null, options);

    internal abstract Type GetCompletionProviderType();

    /// <summary>
    /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
    /// </summary>
    /// <param name="code">The source code (not markup).</param>
    /// <param name="expectedItemOrNull">The expected item. If this is null, verifies that *any* item shows up for this CompletionProvider (or no items show up if checkForAbsence is true).</param>
    /// <param name="expectedDescriptionOrNull">If this is null, the Description for the item is ignored.</param>
    /// <param name="usePreviousCharAsTrigger">Whether or not the previous character in markup should be used to trigger IntelliSense for this provider. If false, invokes it through the invoke IntelliSense command.</param>
    /// <param name="checkForAbsence">If true, checks for absence of a specific item (or that no items are returned from this CompletionProvider)</param>
    private protected virtual async Task VerifyWorkerAsync(
        string code, int position,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind,
        bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionModeItem,
        string displayTextSuffix, string displayTextPrefix,
        string inlineDescription, bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags,
        CompletionOptions options,
        bool skipSpeculation = false)
    {
        var expectedResult = new ItemExpectation(
            Name: expectedItemOrNull,
            IsAbsent: checkForAbsence,
            ExpectedDescription: expectedDescriptionOrNull,
            Glyph: glyph,
            MatchPriority: matchPriority,
            DisplayTextSuffix: displayTextSuffix,
            DisplayTextPrefix: displayTextPrefix,
            InlineDescription: inlineDescription,
            IsComplexTextEdit: isComplexTextEdit);

        await VerifyWorkerCoreAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger,
            hasSuggestionModeItem, sourceCodeKind, [expectedResult],
            matchingFilters, flags, options, skipSpeculation);
    }

    /// <summary>
    /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
    /// </summary>
    /// <param name="code">The source code (not markup).</param>
    /// <param name="expectedResults">The expected results. If this is empty, verifies that item shows up for this CompletionProvider (or no items show up if checkForAbsence is true).</param>
    /// <param name="usePreviousCharAsTrigger">Whether or not the previous character in markup should be used to trigger IntelliSense for this provider. If false, invokes it through the invoke IntelliSense command.</param>
    private protected async Task VerifyWorkerCoreAsync(
        string code, int position, bool usePreviousCharAsTrigger,
        char? deletedCharTrigger, bool? hasSuggestionModeItem, SourceCodeKind sourceCodeKind,
        ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters,
        CompletionItemFlags? flags,
        CompletionOptions options,
        bool skipSpeculation = false)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        workspaceFixture.Target.GetWorkspace(GetComposition());
        var document1 = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind);

        await CheckResultsAsync(
            document1, position, usePreviousCharAsTrigger,
            deletedCharTrigger, hasSuggestionModeItem, expectedResults,
            matchingFilters, flags, options);

        if (!skipSpeculation && await CanUseSpeculativeSemanticModelAsync(document1, position))
        {
            var document2 = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
            await CheckResultsAsync(document2, position, usePreviousCharAsTrigger,
                deletedCharTrigger, hasSuggestionModeItem, expectedResults,
                matchingFilters, flags, options);
        }
    }

    /// <summary>
    /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
    /// </summary>
    /// <param name="code">The source code (not markup).</param>
    /// <param name="expectedResults">The expected results. If this is empty, verifies that item shows up for this CompletionProvider (or no items show up if checkForAbsence is true).</param>
    /// <param name="usePreviousCharAsTrigger">Whether or not the previous character in markup should be used to trigger IntelliSense for this provider. If false, invokes it through the invoke IntelliSense command.</param>
    private protected virtual Task VerifyWorkerAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        bool? hasSuggestionModeItem, SourceCodeKind sourceCodeKind,
        ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters,
        CompletionItemFlags? flags,
        CompletionOptions options,
        bool skipSpeculation = false)
    {
        return VerifyWorkerCoreAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionModeItem,
            sourceCodeKind, expectedResults, matchingFilters, flags, options, skipSpeculation);
    }

    /// <summary>
    /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
    /// </summary>
    /// <param name="codeBeforeCommit">The source code (not markup).</param>
    /// <param name="position">Position where intellisense is invoked.</param>
    /// <param name="itemToCommit">The item to commit from the completion provider.</param>
    /// <param name="expectedCodeAfterCommit">The expected code after commit.</param>
    protected virtual async Task VerifyCustomCommitProviderWorkerAsync(string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind sourceCodeKind, char? commitChar = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();
        var workspace = workspaceFixture.Target.GetWorkspace();

        // Set options that are not CompletionOptions
        workspace.SetAnalyzerFallbackAndGlobalOptions(NonCompletionOptions);

        var document1 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind);
        await VerifyCustomCommitProviderCheckResultsAsync(document1, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);

        if (await CanUseSpeculativeSemanticModelAsync(document1, position))
        {
            var document2 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
            await VerifyCustomCommitProviderCheckResultsAsync(document2, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);
        }
    }

    private async Task VerifyCustomCommitProviderCheckResultsAsync(Document document, string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitChar)
    {
        var service = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
        var items = completionList.ItemsList;

        Assert.Contains(itemToCommit, items.Select(x => x.DisplayText), GetStringComparer());
        var firstItem = items.First(i => CompareItems(i.DisplayText, itemToCommit));

        if (service.GetProvider(firstItem, document.Project) is ICustomCommitCompletionProvider customCommitCompletionProvider)
        {
            VerifyCustomCommitWorker(service, customCommitCompletionProvider, firstItem, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
        }
        else
        {
            await VerifyCustomCommitWorkerAsync(service, document, firstItem, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
        }
    }

    private async Task VerifyCustomCommitWorkerAsync(
        CompletionService service,
        Document document,
        RoslynCompletion.CompletionItem completionItem,
        string codeBeforeCommit,
        string expectedCodeAfterCommit,
        char? commitChar = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetPositionAndSpan(expectedCodeAfterCommit, out var actualExpectedCode, out var expectedCaretPosition, out TextSpan? expectedSelectionSpan);

        var options = GetCompletionOptions();

        if (commitChar.HasValue &&
            !CommitManager.IsCommitCharacter(service.GetRules(options), completionItem, commitChar.Value))
        {
            Assert.Equal(codeBeforeCommit, actualExpectedCode);
            return;
        }

        // textview is created lazily, so need to access it before making 
        // changes to document, so the cursor position is tracked correctly.
        var textView = workspaceFixture.Target.CurrentDocument.GetTextView();

        var commit = await service.GetChangeAsync(document, completionItem, commitChar, CancellationToken.None);

        var text = await document.GetTextAsync();
        var newText = text.WithChanges(commit.TextChange);
        var newDoc = document.WithText(newText);
        document.Project.Solution.Workspace.TryApplyChanges(newDoc.Project.Solution);

        var textBuffer = workspaceFixture.Target.CurrentDocument.GetTextBuffer();

        var actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();
        var selectionSpan = commit.NewSelection ?? textView.Selection.StreamSelectionSpan.SnapshotSpan.Span.ToTextSpan();
        var caretPosition = commit.NewPosition ?? textView.Caret.Position.BufferPosition.Position;

        AssertEx.EqualOrDiff(actualExpectedCode, actualCodeAfterCommit);

        if (expectedSelectionSpan != null)
            Assert.Equal(expectedSelectionSpan, selectionSpan);
        else if (expectedCaretPosition != null)
            Assert.Equal(expectedCaretPosition, caretPosition);
    }

    private void VerifyCustomCommitWorker(
        CompletionService service,
        ICustomCommitCompletionProvider customCommitCompletionProvider,
        RoslynCompletion.CompletionItem completionItem,
        string codeBeforeCommit,
        string expectedCodeAfterCommit,
        char? commitChar = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetPosition(expectedCodeAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

        var workspace = workspaceFixture.Target.GetWorkspace();
        var options = GetCompletionOptions();

        if (commitChar.HasValue &&
            !CommitManager.IsCommitCharacter(service.GetRules(options), completionItem, commitChar.Value))
        {
            Assert.Equal(codeBeforeCommit, actualExpectedCode);
            return;
        }

        // textview is created lazily, so need to access it before making 
        // changes to document, so the cursor position is tracked correctly.
        var document = workspace.CurrentSolution.GetRequiredDocument(workspaceFixture.Target.CurrentDocument.Id);
        var textView = workspaceFixture.Target.CurrentDocument.GetTextView();
        var textBuffer = workspaceFixture.Target.CurrentDocument.GetTextBuffer();

        customCommitCompletionProvider.Commit(completionItem, document, textView, textBuffer, textView.TextSnapshot, commitChar);

        var actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();
        var caretPosition = textView.Caret.Position.BufferPosition.Position;

        Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
        Assert.Equal(expectedCaretPosition, caretPosition);
    }

    /// <summary>
    /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
    /// </summary>
    /// <param name="codeBeforeCommit">The source code (not markup).</param>
    /// <param name="position">Position where intellisense is invoked.</param>
    /// <param name="itemToCommit">The item to commit from the completion provider.</param>
    /// <param name="expectedCodeAfterCommit">The expected code after commit.</param>
    private async Task VerifyProviderCommitWorkerAsync(string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit,
        char? commitChar, SourceCodeKind sourceCodeKind)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();
        var workspace = workspaceFixture.Target.GetWorkspace();

        // Set options that are not CompletionOptions
        workspace.SetAnalyzerFallbackAndGlobalOptions(NonCompletionOptions);

        var document1 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind);
        await VerifyProviderCommitCheckResultsAsync(document1, position, itemToCommit, expectedCodeAfterCommit, commitChar);

        if (await CanUseSpeculativeSemanticModelAsync(document1, position))
        {
            var document2 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
            await VerifyProviderCommitCheckResultsAsync(document2, position, itemToCommit, expectedCodeAfterCommit, commitChar);
        }
    }

    private async Task VerifyProviderCommitCheckResultsAsync(
        Document document, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitCharOpt)
    {
        var service = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
        var items = completionList.ItemsList;
        Assert.Contains(items, i => i.DisplayText + i.DisplayTextSuffix == itemToCommit);
        var firstItem = items.First(i => CompareItems(i.DisplayText + i.DisplayTextSuffix, itemToCommit));

        var commitChar = commitCharOpt ?? '\t';

        var text = await document.GetTextAsync();
        var options = GetCompletionOptions();

        if (commitChar == '\t' ||
            CommitManager.IsCommitCharacter(service.GetRules(options), firstItem, commitChar))
        {
            var textChange = (await service.GetChangeAsync(document, firstItem, commitChar, CancellationToken.None)).TextChange;

            // Adjust TextChange to include commit character, so long as it isn't TAB.
            if (commitChar != '\t')
            {
                textChange = new TextChange(textChange.Span, textChange.NewText.TrimEnd(commitChar) + commitChar);
            }

            text = text.WithChanges(textChange);
        }
        else
        {
            // nothing was committed, but we should insert the commit character.
            var textChange = new TextChange(new TextSpan(firstItem.Span.End, 0), commitChar.ToString());
            text = text.WithChanges(textChange);
        }

        Assert.Equal(expectedCodeAfterCommit, text.ToString());
    }

    protected async Task VerifyItemInEditorBrowsableContextsAsync(
        string markup, string referencedCode, string item, int expectedSymbolsSameSolution, int expectedSymbolsMetadataReference,
        string sourceLanguage, string referencedLanguage)
    {
        await VerifyItemWithMetadataReferenceAsync(markup, referencedCode, item, expectedSymbolsMetadataReference, sourceLanguage, referencedLanguage);
        await VerifyItemWithProjectReferenceAsync(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage, referencedLanguage);

        // If the source and referenced languages are different, then they cannot be in the same project
        if (sourceLanguage == referencedLanguage)
        {
            await VerifyItemInSameProjectAsync(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage);
        }
    }

    protected async Task VerifyItemWithMetadataReferenceAsync(string markup, string metadataReferenceCode, string expectedItem, int expectedSymbols,
                                                       string sourceLanguage, string referencedLanguage)
    {
        var xmlString = CreateMarkupForProjectWithMetadataReference(markup, metadataReferenceCode, sourceLanguage, referencedLanguage);

        await VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols);
    }

    protected static string GetMarkupWithReference(string currentFile, string referencedFile, string sourceLanguage, string referenceLanguage, bool isProjectReference)
    {
        return isProjectReference
            ? CreateMarkupForProjectWithProjectReference(currentFile, referencedFile, sourceLanguage, referenceLanguage)
            : CreateMarkupForProjectWithMetadataReference(currentFile, referencedFile, sourceLanguage, referenceLanguage);
    }

    protected static string CreateMarkupForProjectWithMetadataReference(string markup, string metadataReferenceCode, string sourceLanguage, string referencedLanguage)
    {
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" AssemblyName="Project1">
                    <Document FilePath="SourceDocument">{1}</Document>
                    <MetadataReferenceFromSource Language="{2}" CommonReferences="true" IncludeXmlDocComments="true" DocumentationMode="Diagnose" {4}>
                        <Document FilePath="ReferencedDocument">{3}</Document>
                    </MetadataReferenceFromSource>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(metadataReferenceCode), GetLanguageVersionAttribute(referencedLanguage));
    }

    protected async Task VerifyItemWithAliasedMetadataReferencesAsync(string markup, string metadataAlias, string expectedItem, int expectedSymbols,
        string sourceLanguage, string referencedLanguage)
    {
        var xmlString = CreateMarkupForProjectWithAliasedMetadataReference(markup, metadataAlias, "", sourceLanguage, referencedLanguage);

        await VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols);
    }

    private static string GetLanguageVersionAttribute(string languageName)
    {
        return languageName == LanguageNames.CSharp ? """
            LanguageVersion = "preview"
            """ : string.Empty;
    }

    protected static string CreateMarkupForProjectWithAliasedMetadataReference(string markup, string metadataAlias, string referencedCode, string sourceLanguage, string referencedLanguage, bool hasGlobalAlias = true)
    {
        var aliases = hasGlobalAlias ? $"{metadataAlias},{MetadataReferenceProperties.GlobalAlias}" : $"{metadataAlias}";
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" AssemblyName="Project1" {1}>
                    <Document FilePath="SourceDocument">{2}</Document>
                    <MetadataReferenceFromSource Language="{3}" CommonReferences="true" Aliases="{4}" IncludeXmlDocComments="true" DocumentationMode="Diagnose" {5}>
                        <Document FilePath="ReferencedDocument">{6}</Document>
                    </MetadataReferenceFromSource>
                </Project>
            </Workspace>
            """, sourceLanguage, GetLanguageVersionAttribute(sourceLanguage), SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(aliases), GetLanguageVersionAttribute(referencedLanguage), SecurityElement.Escape(referencedCode));
    }

    protected async Task VerifyItemWithProjectReferenceAsync(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage, string referencedLanguage)
    {
        var xmlString = CreateMarkupForProjectWithProjectReference(markup, referencedCode, sourceLanguage, referencedLanguage);

        await VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols);
    }

    protected static string CreateMarkupForProjectWithAliasedProjectReference(string markup, string projectAlias, string referencedCode, string sourceLanguage, string referencedLanguage)
    {
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" AssemblyName="Project1">
                    <ProjectReference Alias="{4}">ReferencedProject</ProjectReference>
                    <Document FilePath="SourceDocument">{1}</Document>
                </Project>
                <Project Language="{2}" CommonReferences="true" AssemblyName="ReferencedProject" IncludeXmlDocComments="true" DocumentationMode="Diagnose">
                    <Document FilePath="ReferencedDocument">{3}</Document>
                </Project>

            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(referencedCode), SecurityElement.Escape(projectAlias));
    }

    protected static string CreateMarkupForProjectWithProjectReference(string markup, string referencedCode, string sourceLanguage, string referencedLanguage)
    {
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" AssemblyName="Project1" {1}>
                    <ProjectReference>ReferencedProject</ProjectReference>
                    <Document FilePath="SourceDocument">{2}</Document>
                </Project>
                <Project Language="{3}" CommonReferences="true" AssemblyName="ReferencedProject" IncludeXmlDocComments="true" DocumentationMode="Diagnose" {4}>
                    <Document FilePath="ReferencedDocument">{5}</Document>
                </Project>

            </Workspace>
            """, sourceLanguage, GetLanguageVersionAttribute(sourceLanguage), SecurityElement.Escape(markup), referencedLanguage, GetLanguageVersionAttribute(referencedLanguage), SecurityElement.Escape(referencedCode));
    }

    protected static string CreateMarkupForProjectWithMultupleProjectReferences(string sourceText, string sourceLanguage, string referencedLanguage, string[] referencedTexts)
    {
        return $"""
            <Workspace>
                <Project Language="{sourceLanguage}" CommonReferences="true" AssemblyName="Project1">
            {GetProjectReferenceElements(referencedTexts)}
                    <Document FilePath="SourceDocument">{SecurityElement.Escape(sourceText)}</Document>
                </Project>
            {GetReferencedProjectElements(referencedLanguage, referencedTexts)}
            </Workspace>
            """;

        static string GetProjectReferenceElements(string[] referencedTexts)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < referencedTexts.Length; ++i)
            {
                builder.AppendLine($"<ProjectReference>ReferencedProject{i}</ProjectReference>");
            }

            return builder.ToString();
        }

        static string GetReferencedProjectElements(string language, string[] referencedTexts)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < referencedTexts.Length; ++i)
            {
                builder.Append($"""
                    <Project Language="{language}" CommonReferences="true" AssemblyName="ReferencedProject{i}" IncludeXmlDocComments="true" DocumentationMode="Diagnose">
                      <Document FilePath="ReferencedDocument{i}">{SecurityElement.Escape(referencedTexts[i])}</Document>
                    </Project>
                    """);
            }

            return builder.ToString();
        }
    }

    protected static string CreateMarkupForProjecWithVBProjectReference(string markup, string referencedCode, string sourceLanguage, string rootNamespace = "")
    {
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" AssemblyName="Project1">
                    <ProjectReference>ReferencedProject</ProjectReference>
                    <Document FilePath="SourceDocument">{1}</Document>
                </Project>
                <Project Language="{2}" CommonReferences="true" AssemblyName="ReferencedProject" IncludeXmlDocComments="true" DocumentationMode="Diagnose">
                    <Document FilePath="ReferencedDocument">{3}</Document>
                    <CompilationOptions RootNamespace="{4}"/>
                </Project>

            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(markup), LanguageNames.VisualBasic, SecurityElement.Escape(referencedCode), rootNamespace);
    }

    private Task VerifyItemInSameProjectAsync(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage)
    {
        var xmlString = CreateMarkupForSingleProject(markup, referencedCode, sourceLanguage);

        return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols);
    }

    protected static string CreateMarkupForSingleProject(
        string sourceCode,
        string referencedCode,
        string sourceLanguage,
        string sourceFileName = "SourceDocument",
        string referencedFileName = "ReferencedDocument")
    {
        return string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true" Name="ProjectName" {5}>
                    <Document FilePath="{3}">{1}</Document>
                    <Document FilePath="{4}">{2}</Document>
                </Project>    
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode), SecurityElement.Escape(referencedCode), sourceFileName, referencedFileName, GetLanguageVersionAttribute(sourceLanguage));
    }

    private async Task VerifyItemWithReferenceWorkerAsync(
        string xmlString, string expectedItem, int expectedSymbols)
    {
        using var testWorkspace = TestWorkspace.Create(xmlString, composition: GetComposition());
        var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
        var solution = testWorkspace.CurrentSolution;
        var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
        var document = solution.GetDocument(documentId);

        var options = GetCompletionOptions();
        var displayOptions = SymbolDescriptionOptions.Default;
        var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;

        var completionService = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo, options);

        if (expectedSymbols >= 1)
        {
            Assert.NotNull(completionList);
            AssertEx.Any(completionList.ItemsList, c => CompareItems(c.DisplayText, expectedItem));

            var item = completionList.ItemsList.First(c => CompareItems(c.DisplayText, expectedItem));
            var description = await completionService.GetDescriptionAsync(document, item, options, displayOptions);

            if (expectedSymbols == 1)
            {
                Assert.DoesNotContain("+", description.Text, StringComparison.Ordinal);
            }
            else
            {
                Assert.Contains(GetExpectedOverloadSubstring(expectedSymbols), description.Text, StringComparison.Ordinal);
            }
        }
        else
        {
            if (completionList != null)
            {
                AssertEx.None(completionList.ItemsList, c => CompareItems(c.DisplayText, expectedItem));
            }
        }
    }

    protected async Task VerifyItemWithMscorlib45Async(string markup, string expectedItem, string expectedDescription, string sourceLanguage)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferencesNet45="true"> 
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(markup));

        await VerifyItemWithMscorlib45WorkerAsync(xmlString, expectedItem, expectedDescription);
    }

    private async Task VerifyItemWithMscorlib45WorkerAsync(
        string xmlString, string expectedItem, string expectedDescription)
    {
        using var testWorkspace = TestWorkspace.Create(xmlString, composition: GetComposition());
        var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
        var solution = testWorkspace.CurrentSolution;
        var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
        var document = solution.GetDocument(documentId);
        var displayOptions = SymbolDescriptionOptions.Default;

        var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
        var completionService = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

        var item = completionList.ItemsList.FirstOrDefault(i => i.DisplayText == expectedItem);
        Assert.Equal(expectedDescription, (await completionService.GetDescriptionAsync(document, item, CompletionOptions.Default, displayOptions)).Text);
    }

    private const char NonBreakingSpace = (char)0x00A0;

    private static string GetExpectedOverloadSubstring(int expectedSymbols)
    {
        if (expectedSymbols <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedSymbols));
        }

        return "+" + NonBreakingSpace + (expectedSymbols - 1) + NonBreakingSpace + FeaturesResources.overload;
    }

    protected async Task VerifyItemInLinkedFilesAsync(string xmlString, string expectedItem, string expectedDescription)
    {
        using var testWorkspace = EditorTestWorkspace.Create(xmlString, composition: GetComposition());
        var position = testWorkspace.Documents.First().CursorPosition.Value;
        var solution = testWorkspace.CurrentSolution;
        var textContainer = testWorkspace.Documents.First().GetTextBuffer().AsTextContainer();
        var currentContextDocumentId = testWorkspace.GetDocumentIdInCurrentContext(textContainer);
        var document = solution.GetDocument(currentContextDocumentId);
        var displayOptions = SymbolDescriptionOptions.Default;

        var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
        var completionService = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

        var item = completionList.ItemsList.Single(c => c.DisplayText == expectedItem);
        Assert.NotNull(item);
        if (expectedDescription != null)
        {
            var actualDescription = (await completionService.GetDescriptionAsync(document, item, CompletionOptions.Default, displayOptions)).Text;
            Assert.Equal(expectedDescription, actualDescription);
        }
    }

    private protected async Task VerifyAtPositionAsync(
        string code, int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem, SourceCodeKind sourceCodeKind,
        ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
    {
        code = code[..position] + insertText + code[position..];
        position += insertText.Length;

        await BaseVerifyWorkerAsync(code, position, usePreviousCharAsTrigger,
            deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options, skipSpeculation: skipSpeculation);
    }

    private protected Task VerifyAtPositionAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
        => VerifyAtPositionAsync(code, position, string.Empty, usePreviousCharAsTrigger,
            deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options, skipSpeculation: skipSpeculation);

    private protected Task VerifyAtPosition_ItemPartiallyWrittenAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults, string partialItem,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
        => VerifyAtPositionAsync(code, position, ItemPartiallyWritten(partialItem),
            usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options, skipSpeculation: skipSpeculation);

    private protected async Task VerifyAtPositionAsync(
        string code, int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionItem,
        string displayTextSuffix, string displayTextPrefix, string inlineDescription = null,
        bool? isComplexTextEdit = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, CompletionOptions options = null, bool skipSpeculation = false)
    {
        code = code[..position] + insertText + code[position..];
        position += insertText.Length;

        await BaseVerifyWorkerAsync(code, position,
            expectedItemOrNull, expectedDescriptionOrNull,
            sourceCodeKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence,
            glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options, skipSpeculation: skipSpeculation);
    }

    private protected Task VerifyAtPositionAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, CompletionOptions options = null, bool skipSpeculation = false)
        => VerifyAtPositionAsync(
            code, position, string.Empty, usePreviousCharAsTrigger, deletedCharTrigger,
            expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
            glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix,
            inlineDescription, isComplexTextEdit, matchingFilters, flags, options, skipSpeculation: skipSpeculation);

    private protected Task VerifyAtPosition_ItemPartiallyWrittenAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null, bool skipSpeculation = false)
        => VerifyAtPositionAsync(
            code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
            deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options,
            skipSpeculation: skipSpeculation);

    private protected async Task VerifyAtEndOfFileAsync(
        string code, int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
    {
        // only do this if the placeholder was at the end of the text.
        if (code.Length != position)
        {
            return;
        }

        code = code[..position] + insertText + code[position..];
        position += insertText.Length;

        await BaseVerifyWorkerAsync(code, position,
            usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options, skipSpeculation: skipSpeculation);
    }

    private protected Task VerifyAtEndOfFileAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
        => VerifyAtEndOfFileAsync(code, position, string.Empty,
            usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options, skipSpeculation: skipSpeculation);

    private protected Task VerifyAtEndOfFileAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options)
        => VerifyAtEndOfFileAsync(code, position, string.Empty,
            usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options);

    private protected async Task VerifyAtEndOfFileAsync(
        string code, int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null)
    {
        // only do this if the placeholder was at the end of the text.
        if (code.Length != position)
        {
            return;
        }

        code = code[..position] + insertText;
        position += insertText.Length;

        await BaseVerifyWorkerAsync(
            code, position, expectedItemOrNull, expectedDescriptionOrNull,
            sourceCodeKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence, glyph,
            matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix,
            inlineDescription, isComplexTextEdit, matchingFilters, flags, options);
    }

    private protected Task VerifyAtEndOfFileAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null)
        => VerifyAtEndOfFileAsync(code, position, string.Empty, usePreviousCharAsTrigger, deletedCharTrigger,
            expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options);

    private protected Task VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults, string partialItem,
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options)
        => VerifyAtEndOfFileAsync(
            code, position, ItemPartiallyWritten(partialItem),
            usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind, expectedResults,
            matchingFilters, flags, options);

    private protected Task VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
        string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null)
        => VerifyAtEndOfFileAsync(
            code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, deletedCharTrigger,
            expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
            glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
            isComplexTextEdit, matchingFilters, flags, options);

    protected void VerifyTextualTriggerCharacter(
        string markup,
        bool shouldTriggerWithTriggerOnLettersEnabled,
        bool shouldTriggerWithTriggerOnLettersDisabled,
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        bool showCompletionInArgumentLists = true)
    {
        VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter: true, sourceCodeKind, showCompletionInArgumentLists);
        VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter: false, sourceCodeKind, showCompletionInArgumentLists: false);
    }

    private void VerifyTextualTriggerCharacterWorker(
        string markup,
        bool expectedTriggerCharacter,
        bool triggerOnLetter,
        SourceCodeKind sourceCodeKind,
        bool showCompletionInArgumentLists)
    {
        using var workspace = CreateWorkspace(markup);
        var hostDocument = workspace.DocumentWithCursor;
        workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

        Assert.Same(hostDocument, workspace.Documents.Single());
        var position = hostDocument.CursorPosition.Value;
        var text = hostDocument.GetTextBuffer().CurrentSnapshot.AsText();
        var trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(text[position]);

        var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
        var service = GetCompletionService(document.Project);

        var options = CompletionOptions.Default with
        {
            TriggerOnTypingLetters = triggerOnLetter,
            TriggerInArgumentLists = showCompletionInArgumentLists
        };

        var isTextualTriggerCharacterResult = service.ShouldTriggerCompletion(document.Project, document.Project.Services, text, position + 1, trigger, options, document.Project.Solution.Options, GetRoles(document));

        if (expectedTriggerCharacter)
        {
            var assertText = "'" + text.ToString(new TextSpan(position, 1)) + "' expected to be textual trigger character";
            Assert.True(isTextualTriggerCharacterResult, assertText);
        }
        else
        {
            var assertText = "'" + text.ToString(new TextSpan(position, 1)) + "' expected to NOT be textual trigger character";
            Assert.False(isTextualTriggerCharacterResult, assertText);
        }
    }

    protected async Task VerifyCommonCommitCharactersAsync(string initialMarkup, string textTypedSoFar)
    {
        var commitCharacters = new[]
        {
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
        };

        await VerifyCommitCharactersAsync(initialMarkup, textTypedSoFar, commitCharacters);
    }

    protected async Task VerifyCommitCharactersAsync(string initialMarkup, string textTypedSoFar, char[] validChars, char[] invalidChars = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
    {
        Assert.NotNull(validChars);
        invalidChars ??= ['x'];

        using var workspace = CreateWorkspace(initialMarkup);
        var hostDocument = workspace.DocumentWithCursor;
        workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

        var documentId = workspace.GetDocumentId(hostDocument);
        var document = workspace.CurrentSolution.GetDocument(documentId);
        var position = hostDocument.CursorPosition.Value;
        var options = GetCompletionOptions();

        var service = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
        var item = completionList.ItemsList.First(i => i.DisplayText.StartsWith(textTypedSoFar));

        foreach (var ch in validChars)
        {
            Assert.True(CommitManager.IsCommitCharacter(
                service.GetRules(options), item, ch), $"Expected '{ch}' to be a commit character");
        }

        foreach (var ch in invalidChars)
        {
            Assert.False(CommitManager.IsCommitCharacter(
                service.GetRules(options), item, ch), $"Expected '{ch}' NOT to be a commit character");
        }
    }

    protected async Task<IReadOnlyList<RoslynCompletion.CompletionItem>> GetCompletionItemsAsync(
        string markup, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger = false)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        workspaceFixture.Target.GetWorkspace(markup, GetComposition());
        var code = workspaceFixture.Target.Code;
        var position = workspaceFixture.Target.Position;
        var document = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind);

        var trigger = usePreviousCharAsTrigger
            ? RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1))
            : RoslynCompletion.CompletionTrigger.Invoke;

        var completionService = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(completionService, document, position, trigger);

        return completionList.ItemsList;
    }
}
