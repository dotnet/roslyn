// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion
{
    [UseExportProvider]
    public abstract class AbstractCompletionProviderTests<TWorkspaceFixture> : TestBase, IClassFixture<TWorkspaceFixture>
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        private static readonly TestComposition s_baseComposition = EditorTestCompositions.EditorFeatures.AddExcludedPartTypes(typeof(CompletionProvider));

        protected readonly Mock<ICompletionSession> MockCompletionSession;
        protected TWorkspaceFixture WorkspaceFixture;
        private ExportProvider _lazyExportProvider;

        protected AbstractCompletionProviderTests(TWorkspaceFixture workspaceFixture)
        {
            MockCompletionSession = new Mock<ICompletionSession>(MockBehavior.Strict);

            this.WorkspaceFixture = workspaceFixture;
        }

        protected ExportProvider ExportProvider
            => _lazyExportProvider ??= GetComposition().ExportProviderFactory.CreateExportProvider();

        protected virtual TestComposition GetComposition()
            => s_baseComposition.AddParts(GetCompletionProviderType());

        public override void Dispose()
        {
            this.WorkspaceFixture.DisposeAfterTest();
            base.Dispose();
        }

        protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.GetLanguageService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        internal virtual CompletionServiceWithProviders GetCompletionService(Project project)
        {
            var completionService = project.LanguageServices.GetRequiredService<CompletionService>();

            var completionServiceWithProviders = Assert.IsAssignableFrom<CompletionServiceWithProviders>(completionService);

            var completionProviders = ((IMefHostExportProvider)project.Solution.Workspace.Services.HostServices).GetExports<CompletionProvider>();
            var completionProvider = Assert.Single(completionProviders).Value;
            Assert.IsType(GetCompletionProviderType(), completionProvider);

            return completionServiceWithProviders;
        }

        internal static ImmutableHashSet<string> GetRoles(Document document)
            => document.SourceCodeKind == SourceCodeKind.Regular ? ImmutableHashSet<string>.Empty : ImmutableHashSet.Create(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole);

        protected abstract string ItemPartiallyWritten(string expectedItemOrNull);

        protected abstract TestWorkspace CreateWorkspace(string fileContents);

        private protected abstract Task BaseVerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription, List<CompletionFilter> matchingFilters, CompletionItemFlags? flags);

        internal static Task<RoslynCompletion.CompletionList> GetCompletionListAsync(
            CompletionService service,
            Document document,
            int position,
            RoslynCompletion.CompletionTrigger triggerInfo,
            OptionSet options = null)
        {
            return service.GetCompletionsAsync(document, position, triggerInfo, GetRoles(document), options);
        }

        private protected async Task CheckResultsAsync(
            Document document, int position, string expectedItemOrNull,
            string expectedDescriptionOrNull, bool usePreviousCharAsTrigger,
            bool checkForAbsence, int? glyph, int? matchPriority,
            bool? hasSuggestionModeItem, string displayTextSuffix, string inlineDescription,
            List<CompletionFilter> matchingFilters, CompletionItemFlags? flags)
        {
            var code = (await document.GetTextAsync()).ToString();

            var trigger = RoslynCompletion.CompletionTrigger.Invoke;

            if (usePreviousCharAsTrigger)
            {
                trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1));
            }

            var completionService = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(completionService, document, position, trigger);
            var items = completionList == null ? ImmutableArray<RoslynCompletion.CompletionItem>.Empty : completionList.Items;

            if (hasSuggestionModeItem != null)
            {
                Assert.Equal(hasSuggestionModeItem.Value, completionList.SuggestionModeItem != null);
            }

            if (checkForAbsence)
            {
                if (items == null)
                {
                    return;
                }

                if (expectedItemOrNull == null)
                {
                    Assert.Empty(items);
                }
                else
                {
                    AssertEx.None(
                        items,
                        c => CompareItems(c.DisplayText, expectedItemOrNull)
                                && CompareItems(c.DisplayTextSuffix, displayTextSuffix ?? "")
                                && CompareItems(c.InlineDescription, inlineDescription ?? "")
                                && (expectedDescriptionOrNull != null ? completionService.GetDescriptionAsync(document, c).Result.Text == expectedDescriptionOrNull : true));
                }
            }
            else
            {
                if (expectedItemOrNull == null)
                {
                    Assert.NotEmpty(items);
                }
                else
                {
                    Func<RoslynCompletion.CompletionItem, bool> predicate = c
                        => CompareItems(c.DisplayText, expectedItemOrNull)
                              && CompareItems(c.DisplayTextSuffix, displayTextSuffix ?? "")
                              && CompareItems(c.InlineDescription, inlineDescription ?? "")
                              && (expectedDescriptionOrNull != null ? completionService.GetDescriptionAsync(document, c).Result.Text == expectedDescriptionOrNull : true)
                              && (glyph.HasValue ? c.Tags.SequenceEqual(GlyphTags.GetTags((Glyph)glyph.Value)) : true)
                              && (matchPriority.HasValue ? (int)c.Rules.MatchPriority == matchPriority.Value : true)
                              && (matchingFilters != null ? FiltersMatch(matchingFilters, c) : true)
                              && (flags != null ? flags.Value == c.Flags : true);

                    AssertEx.Any(items, predicate);
                }
            }
        }

        protected void SetExperimentOption(string experimentName, bool enabled)
        {
            var mockExperimentService = ExportProvider.GetExportedValue<TestExperimentationService>();
            mockExperimentService.SetExperimentOption(experimentName, enabled);
        }

        private static bool FiltersMatch(List<CompletionFilter> expectedMatchingFilters, RoslynCompletion.CompletionItem item)
        {
            var matchingFilters = FilterSet.GetFilters(item);

            // Check that the list has no duplicates.
            Assert.Equal(matchingFilters.Count, matchingFilters.Distinct().Count());
            return expectedMatchingFilters.SetEquals(matchingFilters);
        }

        private Task VerifyAsync(
            string markup, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionModeItem, string displayTextSuffix,
            string inlineDescription, List<CompletionFilter> matchingFilters, CompletionItemFlags? flags)
        {
            var workspace = WorkspaceFixture.GetWorkspace(markup, ExportProvider);
            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;

            workspace.SetOptions(WithChangedOptions(workspace.Options));

            return VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                matchingFilters, flags);
        }

        protected async Task<CompletionList> GetCompletionListAsync(string markup, string workspaceKind = null)
        {
            var workspace = WorkspaceFixture.GetWorkspace(markup, ExportProvider, workspaceKind: workspaceKind);
            var currentDocument = workspace.CurrentSolution.GetDocument(WorkspaceFixture.CurrentDocument.Id);
            var position = WorkspaceFixture.Position;
            currentDocument = WithChangedOptions(currentDocument);

            return await GetCompletionListAsync(GetCompletionService(currentDocument.Project), currentDocument, position, RoslynCompletion.CompletionTrigger.Invoke, options: workspace.Options).ConfigureAwait(false);
        }

        protected async Task VerifyCustomCommitProviderAsync(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind? sourceCodeKind = null, char? commitChar = null)
        {
            using (WorkspaceFixture.GetWorkspace(markupBeforeCommit, ExportProvider))
            {
                var code = WorkspaceFixture.Code;
                var position = WorkspaceFixture.Position;

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
            WorkspaceFixture.GetWorkspace(markupBeforeCommit, ExportProvider);

            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;

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

        private protected async Task VerifyItemExistsAsync(
            string markup, string expectedItem, string expectedDescriptionOrNull = null,
            SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            int? glyph = null, int? matchPriority = null, bool? hasSuggestionModeItem = null,
            string displayTextSuffix = null, string inlineDescription = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull,
                    sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: false,
                    glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);
            }
            else
            {
                await VerifyAsync(
                    markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger,
                    checkForAbsence: false, glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);

                await VerifyAsync(
                    markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger,
                    checkForAbsence: false, glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);
            }
        }

        private protected async Task VerifyItemIsAbsentAsync(
            string markup, string expectedItem, string expectedDescriptionOrNull = null,
            SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            bool? hasSuggestionModeItem = null, string displayTextSuffix = null, string inlineDescription = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);
            }
            else
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters, flags: flags);
            }
        }

        protected async Task VerifyAnyItemExistsAsync(
            string markup, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            bool? hasSuggestionModeItem = null, string displayTextSuffix = null, string inlineDescription = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
            }
            else
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
            }
        }

        protected async Task VerifyNoItemsExistAsync(
            string markup, SourceCodeKind? sourceCodeKind = null,
            bool usePreviousCharAsTrigger = false, bool? hasSuggestionModeItem = null,
            string displayTextSuffix = null, string inlineDescription = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
            }
            else
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null, flags: null);
            }
        }

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
            bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionModeItem,
            string displayTextSuffix,
            string inlineDescription,
            List<CompletionFilter> matchingFilters, CompletionItemFlags? flags)
        {
            WorkspaceFixture.GetWorkspace(ExportProvider);
            var document1 = WorkspaceFixture.UpdateDocument(code, sourceCodeKind);

            await CheckResultsAsync(
                document1, position, expectedItemOrNull,
                expectedDescriptionOrNull, usePreviousCharAsTrigger,
                checkForAbsence, glyph, matchPriority,
                hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                matchingFilters, flags);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = WorkspaceFixture.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
                await CheckResultsAsync(
                    document2, position, expectedItemOrNull, expectedDescriptionOrNull,
                    usePreviousCharAsTrigger, checkForAbsence, glyph, matchPriority,
                    hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                    matchingFilters, flags);
            }
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
            var document1 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            await VerifyCustomCommitProviderCheckResultsAsync(document1, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                await VerifyCustomCommitProviderCheckResultsAsync(document2, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);
            }
        }

        private async Task VerifyCustomCommitProviderCheckResultsAsync(Document document, string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitChar)
        {
            var workspace = WorkspaceFixture.GetWorkspace();
            document = WithChangedOptions(document);

            var service = GetCompletionService(document.Project);
            var completionLlist = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
            var items = completionLlist.Items;

            Assert.Contains(itemToCommit, items.Select(x => x.DisplayText), GetStringComparer());
            var firstItem = items.First(i => CompareItems(i.DisplayText, itemToCommit));

            if (service.GetProvider(firstItem) is ICustomCommitCompletionProvider customCommitCompletionProvider)
            {
                VerifyCustomCommitWorker(service, customCommitCompletionProvider, firstItem, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
            }
            else
            {
                await VerifyCustomCommitWorkerAsync(service, document, firstItem, completionLlist.Span, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
            }
        }

        protected virtual OptionSet WithChangedOptions(OptionSet options) => options;
        private Document WithChangedOptions(Document document)
        {
            var workspace = document.Project.Solution.Workspace;
            var newOptions = WithChangedOptions(workspace.Options);
            workspace.TryApplyChanges(document.Project.Solution.WithOptions(newOptions));
            return workspace.CurrentSolution.GetDocument(document.Id);
        }

        private static Document WithChangedOption(Document document, OptionKey optionKey, object value)
        {
            var workspace = document.Project.Solution.Workspace;
            var newOptions = workspace.Options.WithChangedOption(optionKey, value);
            workspace.TryApplyChanges(document.Project.Solution.WithOptions(newOptions));
            return workspace.CurrentSolution.GetDocument(document.Id);
        }

        internal async Task VerifyCustomCommitWorkerAsync(
            CompletionServiceWithProviders service,
            Document document,
            RoslynCompletion.CompletionItem completionItem,
            TextSpan completionListSpan,
            string codeBeforeCommit,
            string expectedCodeAfterCommit,
            char? commitChar = null)
        {
            MarkupTestFile.GetPosition(expectedCodeAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

            if (commitChar.HasValue &&
                !CommitManager.IsCommitCharacter(service.GetRules(), completionItem, commitChar.Value))
            {
                Assert.Equal(codeBeforeCommit, actualExpectedCode);
                return;
            }

            // textview is created lazily, so need to access it before making 
            // changes to document, so the cursor position is tracked correctly.
            var textView = WorkspaceFixture.CurrentDocument.GetTextView();

            var options = await document.GetOptionsAsync().ConfigureAwait(false);
            var disallowAddingImports = options.GetOption(CompletionServiceOptions.DisallowAddingImports);

            var commit = await service.GetChangeAsync(document, completionItem, completionListSpan, commitChar, disallowAddingImports, CancellationToken.None);

            var text = await document.GetTextAsync();
            var newText = text.WithChanges(commit.TextChange);
            var newDoc = document.WithText(newText);
            document.Project.Solution.Workspace.TryApplyChanges(newDoc.Project.Solution);

            var textBuffer = WorkspaceFixture.CurrentDocument.GetTextBuffer();

            var actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();
            var caretPosition = commit.NewPosition ?? textView.Caret.Position.BufferPosition.Position;

            AssertEx.EqualOrDiff(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }

        internal virtual void VerifyCustomCommitWorker(
            CompletionService service,
            ICustomCommitCompletionProvider customCommitCompletionProvider,
            RoslynCompletion.CompletionItem completionItem,
            string codeBeforeCommit,
            string expectedCodeAfterCommit,
            char? commitChar = null)
        {
            MarkupTestFile.GetPosition(expectedCodeAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

            if (commitChar.HasValue &&
                !CommitManager.IsCommitCharacter(service.GetRules(), completionItem, commitChar.Value))
            {
                Assert.Equal(codeBeforeCommit, actualExpectedCode);
                return;
            }

            // textview is created lazily, so need to access it before making 
            // changes to document, so the cursor position is tracked correctly.
            var textView = WorkspaceFixture.CurrentDocument.GetTextView();
            var textBuffer = WorkspaceFixture.CurrentDocument.GetTextBuffer();

            customCommitCompletionProvider.Commit(completionItem, textView, textBuffer, textView.TextSnapshot, commitChar);

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
        protected virtual async Task VerifyProviderCommitWorkerAsync(string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit,
            char? commitChar, SourceCodeKind sourceCodeKind)
        {
            var document1 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            await VerifyProviderCommitCheckResultsAsync(document1, position, itemToCommit, expectedCodeAfterCommit, commitChar);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                await VerifyProviderCommitCheckResultsAsync(document2, position, itemToCommit, expectedCodeAfterCommit, commitChar);
            }
        }

        private async Task VerifyProviderCommitCheckResultsAsync(
            Document document, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitCharOpt)
        {
            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
            var items = completionList.Items;
            var firstItem = items.First(i => CompareItems(i.DisplayText + i.DisplayTextSuffix, itemToCommit));

            var commitChar = commitCharOpt ?? '\t';

            var text = await document.GetTextAsync();

            if (commitChar == '\t' ||
                CommitManager.IsCommitCharacter(service.GetRules(), firstItem, commitChar))
            {
                var textChange = (await service.GetChangeAsync(document, firstItem, completionList.Span, commitChar, disallowAddingImports: false, CancellationToken.None)).TextChange;

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
            string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers = false)
        {
            await VerifyItemWithMetadataReferenceAsync(markup, referencedCode, item, expectedSymbolsMetadataReference, sourceLanguage, referencedLanguage, hideAdvancedMembers);
            await VerifyItemWithProjectReferenceAsync(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage, referencedLanguage, hideAdvancedMembers);

            // If the source and referenced languages are different, then they cannot be in the same project
            if (sourceLanguage == referencedLanguage)
            {
                await VerifyItemInSameProjectAsync(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage, hideAdvancedMembers);
            }
        }

        protected Task VerifyItemWithMetadataReferenceAsync(string markup, string metadataReferenceCode, string expectedItem, int expectedSymbols,
                                                           string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = CreateMarkupForProjectWithMetadataReference(markup, metadataReferenceCode, sourceLanguage, referencedLanguage);

            return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected static string GetMarkupWithReference(string currentFile, string referencedFile, string sourceLanguage, string referenceLanguage, bool isProjectReference)
        {
            return isProjectReference
                ? CreateMarkupForProjectWithProjectReference(currentFile, referencedFile, sourceLanguage, referenceLanguage)
                : CreateMarkupForProjectWithMetadataReference(currentFile, referencedFile, sourceLanguage, referenceLanguage);
        }

        protected static string CreateMarkupForProjectWithMetadataReference(string markup, string metadataReferenceCode, string sourceLanguage, string referencedLanguage)
        {
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"" AssemblyName=""Project1"">
        <Document FilePath=""SourceDocument"">{1}</Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
            <Document FilePath=""ReferencedDocument"">{3}</Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(metadataReferenceCode));
        }

        protected Task VerifyItemWithAliasedMetadataReferencesAsync(string markup, string metadataAlias, string expectedItem, int expectedSymbols,
                                                   string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = CreateMarkupForProjectWithAliasedMetadataReference(markup, metadataAlias, "", sourceLanguage, referencedLanguage);

            return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected static string CreateMarkupForProjectWithAliasedMetadataReference(string markup, string metadataAlias, string referencedCode, string sourceLanguage, string referencedLanguage, bool hasGlobalAlias = true)
        {
            var aliases = hasGlobalAlias ? $"{metadataAlias},{MetadataReferenceProperties.GlobalAlias}" : $"{metadataAlias}";
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"" AssemblyName=""Project1"">
        <Document FilePath=""SourceDocument"">{1}</Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" Aliases=""{3}"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
            <Document FilePath=""ReferencedDocument"">{4}</Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(aliases), SecurityElement.Escape(referencedCode));
        }

        protected Task VerifyItemWithProjectReferenceAsync(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = CreateMarkupForProjectWithProjectReference(markup, referencedCode, sourceLanguage, referencedLanguage);

            return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected static string CreateMarkupForProjectWithAliasedProjectReference(string markup, string projectAlias, string referencedCode, string sourceLanguage, string referencedLanguage)
        {
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"" AssemblyName=""Project1"">
        <ProjectReference Alias=""{4}"">ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">{1}</Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
        <Document FilePath=""ReferencedDocument"">{3}</Document>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(referencedCode), SecurityElement.Escape(projectAlias));
        }

        protected static string CreateMarkupForProjectWithProjectReference(string markup, string referencedCode, string sourceLanguage, string referencedLanguage)
        {
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"" AssemblyName=""Project1"">
        <ProjectReference>ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">{1}</Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
        <Document FilePath=""ReferencedDocument"">{3}</Document>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(referencedCode));
        }

        protected static string CreateMarkupForProjectWithMultupleProjectReferences(string sourceText, string sourceLanguage, string referencedLanguage, string[] referencedTexts)
        {
            return $@"
<Workspace>
    <Project Language=""{sourceLanguage}"" CommonReferences=""true"" AssemblyName=""Project1"">
{GetProjectReferenceElements(referencedTexts)}
        <Document FilePath=""SourceDocument"">{SecurityElement.Escape(sourceText)}</Document>
    </Project>
{GetReferencedProjectElements(referencedLanguage, referencedTexts)}
</Workspace>";

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
                    builder.Append($@"
<Project Language=""{language}"" CommonReferences=""true"" AssemblyName=""ReferencedProject{i}"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
  <Document FilePath=""ReferencedDocument{i}"">{SecurityElement.Escape(referencedTexts[i])}</Document>
</Project>");
                }

                return builder.ToString();
            }
        }

        protected static string CreateMarkupForProjecWithVBProjectReference(string markup, string referencedCode, string sourceLanguage, string rootNamespace = "")
        {
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"" AssemblyName=""Project1"">
        <ProjectReference>ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">{1}</Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
        <Document FilePath=""ReferencedDocument"">{3}</Document>
        <CompilationOptions RootNamespace=""{4}""/>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), LanguageNames.VisualBasic, SecurityElement.Escape(referencedCode), rootNamespace);
        }

        private Task VerifyItemInSameProjectAsync(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage, bool hideAdvancedMembers)
        {
            var xmlString = CreateMarkupForSingleProject(markup, referencedCode, sourceLanguage);

            return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected static string CreateMarkupForSingleProject(string markup, string referencedCode, string sourceLanguage)
        {
            return string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">{1}</Document>
        <Document FilePath=""ReferencedDocument"">{2}</Document>
    </Project>    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), SecurityElement.Escape(referencedCode));
        }

        private async Task VerifyItemWithReferenceWorkerAsync(
            string xmlString, string expectedItem, int expectedSymbols, bool hideAdvancedMembers)
        {
            using (var testWorkspace = TestWorkspace.Create(xmlString, exportProvider: ExportProvider))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                var document = solution.GetDocument(documentId);

                var optionKey = new OptionKey(CompletionOptions.HideAdvancedMembers, document.Project.Language);
                document = WithChangedOption(document, optionKey, hideAdvancedMembers);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;

                var completionService = GetCompletionService(document.Project);
                var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

                if (expectedSymbols >= 1)
                {
                    Assert.NotNull(completionList);
                    AssertEx.Any(completionList.Items, c => CompareItems(c.DisplayText, expectedItem));

                    var item = completionList.Items.First(c => CompareItems(c.DisplayText, expectedItem));
                    var description = await completionService.GetDescriptionAsync(document, item);

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
                        AssertEx.None(completionList.Items, c => CompareItems(c.DisplayText, expectedItem));
                    }
                }
            }
        }

        protected Task VerifyItemWithMscorlib45Async(string markup, string expectedItem, string expectedDescription, string sourceLanguage)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferencesNet45=""true""> 
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup));

            return VerifyItemWithMscorlib45WorkerAsync(xmlString, expectedItem, expectedDescription);
        }

        private async Task VerifyItemWithMscorlib45WorkerAsync(
            string xmlString, string expectedItem, string expectedDescription)
        {
            using (var testWorkspace = TestWorkspace.Create(xmlString, exportProvider: ExportProvider))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                var document = solution.GetDocument(documentId);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
                var completionService = GetCompletionService(document.Project);
                var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

                var item = completionList.Items.FirstOrDefault(i => i.DisplayText == expectedItem);
                Assert.Equal(expectedDescription, (await completionService.GetDescriptionAsync(document, item)).Text);
            }
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
            using (var testWorkspace = TestWorkspace.Create(xmlString, exportProvider: ExportProvider))
            {
                var position = testWorkspace.Documents.First().CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var textContainer = testWorkspace.Documents.First().GetTextBuffer().AsTextContainer();
                var currentContextDocumentId = testWorkspace.GetDocumentIdInCurrentContext(textContainer);
                var document = solution.GetDocument(currentContextDocumentId);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
                var completionService = GetCompletionService(document.Project);
                var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

                var item = completionList.Items.Single(c => c.DisplayText == expectedItem);
                Assert.NotNull(item);
                if (expectedDescription != null)
                {
                    var actualDescription = (await completionService.GetDescriptionAsync(document, item)).Text;
                    Assert.Equal(expectedDescription, actualDescription);
                }
            }
        }

        private protected Task VerifyAtPositionAsync(
            string code, int position, string insertText, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem,
            string displayTextSuffix, string inlineDescription = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            code = code.Substring(0, position) + insertText + code.Substring(position);
            position += insertText.Length;

            return BaseVerifyWorkerAsync(code, position,
                expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters, flags);
        }

        private protected Task VerifyAtPositionAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return VerifyAtPositionAsync(
                code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters, flags);
        }

        private protected async Task VerifyAtEndOfFileAsync(
            string code, int position, string insertText, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            // only do this if the placeholder was at the end of the text.
            if (code.Length != position)
            {
                return;
            }

            code = code.Substring(startIndex: 0, length: position) + insertText;
            position += insertText.Length;

            await BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters, flags);
        }

        private protected Task VerifyAtPosition_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return VerifyAtPositionAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters, flags);
        }

        private protected Task VerifyAtEndOfFileAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return VerifyAtEndOfFileAsync(code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters, flags);
        }

        private protected Task VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return VerifyAtEndOfFileAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters, flags);
        }

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
            using (var workspace = CreateWorkspace(markup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

                Assert.Same(hostDocument, workspace.Documents.Single());
                var position = hostDocument.CursorPosition.Value;
                var text = hostDocument.GetTextBuffer().CurrentSnapshot.AsText();
                var options = workspace.Options
                    .WithChangedOption(CompletionOptions.TriggerOnTypingLetters2, hostDocument.Project.Language, triggerOnLetter)
                    .WithChangedOption(CompletionOptions.TriggerInArgumentLists, hostDocument.Project.Language, showCompletionInArgumentLists);
                var trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(text[position]);

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var service = GetCompletionService(document.Project);
                var isTextualTriggerCharacterResult = service.ShouldTriggerCompletion(text, position + 1, trigger, GetRoles(document), options);

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
            invalidChars = invalidChars ?? new[] { 'x' };

            using (var workspace = CreateWorkspace(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var service = GetCompletionService(document.Project);
                var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                foreach (var ch in validChars)
                {
                    Assert.True(CommitManager.IsCommitCharacter(
                        service.GetRules(), item, ch), $"Expected '{ch}' to be a commit character");
                }

                foreach (var ch in invalidChars)
                {
                    Assert.False(CommitManager.IsCommitCharacter(
                        service.GetRules(), item, ch), $"Expected '{ch}' NOT to be a commit character");
                }
            }
        }

        protected async Task<ImmutableArray<RoslynCompletion.CompletionItem>> GetCompletionItemsAsync(
            string markup, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger = false)
        {
            WorkspaceFixture.GetWorkspace(markup, ExportProvider);
            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;
            var document = WorkspaceFixture.UpdateDocument(code, sourceCodeKind);

            var trigger = usePreviousCharAsTrigger
                ? RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1))
                : RoslynCompletion.CompletionTrigger.Invoke;

            var completionService = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(completionService, document, position, trigger);

            return completionList == null ? ImmutableArray<RoslynCompletion.CompletionItem>.Empty : completionList.Items;
        }
    }
}
