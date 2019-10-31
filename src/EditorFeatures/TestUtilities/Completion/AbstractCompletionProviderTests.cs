// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
        protected readonly Mock<ICompletionSession> MockCompletionSession;
        protected TWorkspaceFixture WorkspaceFixture;

        protected AbstractCompletionProviderTests(TWorkspaceFixture workspaceFixture)
        {
            MockCompletionSession = new Mock<ICompletionSession>(MockBehavior.Strict);

            this.WorkspaceFixture = workspaceFixture;
        }

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

        internal CompletionServiceWithProviders GetCompletionService(Workspace workspace)
        {
            return CreateCompletionService(workspace, ImmutableArray.Create(CreateCompletionProvider()));
        }

        internal abstract CompletionServiceWithProviders CreateCompletionService(
            Workspace workspace, ImmutableArray<CompletionProvider> exclusiveProviders);

        protected abstract string ItemPartiallyWritten(string expectedItemOrNull);

        protected abstract TestWorkspace CreateWorkspace(string fileContents);

        private protected abstract Task BaseVerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription, List<CompletionFilter> matchingFilters);

        internal static CompletionHelper GetCompletionHelper(Document document)
        {
            return CompletionHelper.GetHelper(document);
        }

        internal Task<RoslynCompletion.CompletionList> GetCompletionListAsync(
            CompletionService service,
            Document document, int position, RoslynCompletion.CompletionTrigger triggerInfo, OptionSet options = null)
        {
            return service.GetCompletionsAsync(document, position, triggerInfo, options: options);
        }

        private protected async Task CheckResultsAsync(
            Document document, int position, string expectedItemOrNull,
            string expectedDescriptionOrNull, bool usePreviousCharAsTrigger,
            bool checkForAbsence, int? glyph, int? matchPriority,
            bool? hasSuggestionModeItem, string displayTextSuffix, string inlineDescription,
            List<CompletionFilter> matchingFilters)
        {
            var code = (await document.GetTextAsync()).ToString();

            var trigger = RoslynCompletion.CompletionTrigger.Invoke;

            if (usePreviousCharAsTrigger)
            {
                trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1));
            }

            var completionService = GetCompletionService(document.Project.Solution.Workspace);
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
                              && (matchingFilters != null ? FiltersMatch(matchingFilters, c) : true);

                    AssertEx.Any(items, predicate);
                }
            }
        }

        private ExportProvider _exportProvider = null;

        private ExportProvider ExportProvider
        {
            get
            {
                if (_exportProvider == null)
                {
                    _exportProvider = GetExportProvider();
                }

                return _exportProvider;
            }
        }

        protected void SetExperimentOption(string experimentName, bool enabled)
        {
            var mockExperimentService = ExportProvider.GetExportedValue<TestExperimentationService>();
            mockExperimentService.SetExperimentOption(experimentName, enabled);
        }

        protected virtual ExportProvider GetExportProvider() => null;

        private bool FiltersMatch(List<CompletionFilter> expectedMatchingFilters, RoslynCompletion.CompletionItem item)
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
            string inlineDescription, List<CompletionFilter> matchingFilters)
        {
            var workspace = WorkspaceFixture.GetWorkspace(markup, ExportProvider);
            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;
            SetWorkspaceOptions(workspace);

            return VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                matchingFilters);
        }

        protected async Task<CompletionList> GetCompletionListAsync(string markup, string workspaceKind = null)
        {
            var workspace = WorkspaceFixture.GetWorkspace(markup, workspaceKind: workspaceKind);
            var currentDocument = workspace.CurrentSolution.GetDocument(WorkspaceFixture.CurrentDocument.Id);
            var position = WorkspaceFixture.Position;
            SetWorkspaceOptions(workspace);

            return await GetCompletionListAsync(GetCompletionService(workspace), currentDocument, position, RoslynCompletion.CompletionTrigger.Invoke, workspace.Options).ConfigureAwait(false);
        }

        protected async Task VerifyCustomCommitProviderAsync(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind? sourceCodeKind = null, char? commitChar = null)
        {
            using (WorkspaceFixture.GetWorkspace(markupBeforeCommit))
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

        protected async Task VerifyProviderCommitAsync(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit,
            char? commitChar, string textTypedSoFar, SourceCodeKind? sourceCodeKind = null)
        {
            WorkspaceFixture.GetWorkspace(markupBeforeCommit);

            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;

            expectedCodeAfterCommit = expectedCodeAfterCommit.NormalizeLineEndings();
            if (sourceCodeKind.HasValue)
            {
                await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, sourceCodeKind.Value);
            }
            else
            {
                await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, SourceCodeKind.Regular);
                await VerifyProviderCommitWorkerAsync(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, SourceCodeKind.Script);
            }
        }

        protected bool CompareItems(string actualItem, string expectedItem)
        {
            return GetStringComparer().Equals(actualItem, expectedItem);
        }

        protected virtual IEqualityComparer<string> GetStringComparer()
        {
            return StringComparer.Ordinal;
        }

        private protected async Task VerifyItemExistsAsync(
            string markup, string expectedItem, string expectedDescriptionOrNull = null,
            SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            int? glyph = null, int? matchPriority = null, bool? hasSuggestionModeItem = null,
            string displayTextSuffix = null, string inlineDescription = null,
            List<CompletionFilter> matchingFilters = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull,
                    sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: false,
                    glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters);
            }
            else
            {
                await VerifyAsync(
                    markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger,
                    checkForAbsence: false, glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters);

                await VerifyAsync(
                    markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger,
                    checkForAbsence: false, glyph: glyph, matchPriority: matchPriority,
                    hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix,
                    inlineDescription: inlineDescription, matchingFilters: matchingFilters);
            }
        }

        private protected async Task VerifyItemIsAbsentAsync(
            string markup, string expectedItem, string expectedDescriptionOrNull = null,
            SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            bool? hasSuggestionModeItem = null, string displayTextSuffix = null, string inlineDescription = null,
            List<CompletionFilter> matchingFilters = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters);
            }
            else
            {
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters);
                await VerifyAsync(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: matchingFilters);
            }
        }

        protected async Task VerifyAnyItemExistsAsync(
            string markup, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false,
            bool? hasSuggestionModeItem = null, string displayTextSuffix = null, string inlineDescription = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
            }
            else
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
            }
        }

        protected async Task VerifyNoItemsExistAsync(
            string markup, SourceCodeKind? sourceCodeKind = null,
            bool usePreviousCharAsTrigger = false, bool? hasSuggestionModeItem = null,
            string displayTextSuffix = null, string inlineDescription = null)
        {
            if (sourceCodeKind.HasValue)
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
            }
            else
            {
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
                await VerifyAsync(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, glyph: null, matchPriority: null, hasSuggestionModeItem: hasSuggestionModeItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription, matchingFilters: null);
            }
        }

        internal abstract CompletionProvider CreateCompletionProvider();

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
            List<CompletionFilter> matchingFilters)
        {
            var document1 = WorkspaceFixture.UpdateDocument(code, sourceCodeKind);

            await CheckResultsAsync(
                document1, position, expectedItemOrNull,
                expectedDescriptionOrNull, usePreviousCharAsTrigger,
                checkForAbsence, glyph, matchPriority,
                hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                matchingFilters);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = WorkspaceFixture.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
                await CheckResultsAsync(
                    document2, position, expectedItemOrNull, expectedDescriptionOrNull,
                    usePreviousCharAsTrigger, checkForAbsence, glyph, matchPriority,
                    hasSuggestionModeItem, displayTextSuffix, inlineDescription,
                    matchingFilters);
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
            SetWorkspaceOptions(workspace);
            var textBuffer = WorkspaceFixture.CurrentDocument.GetTextBuffer();

            var service = GetCompletionService(workspace);
            var completionLlist = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
            var items = completionLlist.Items;

            Assert.Contains(itemToCommit, items.Select(x => x.DisplayText), GetStringComparer());
            var firstItem = items.First(i => CompareItems(i.DisplayText, itemToCommit));

            if (service.GetTestAccessor().ExclusiveProviders?[0] is ICustomCommitCompletionProvider customCommitCompletionProvider)
            {
                var completionRules = GetCompletionHelper(document);
                var textView = WorkspaceFixture.CurrentDocument.GetTextView();
                VerifyCustomCommitWorker(service, customCommitCompletionProvider, firstItem, completionRules, textView, textBuffer, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
            }
            else
            {
                await VerifyCustomCommitWorkerAsync(service, document, firstItem, completionLlist.Span, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
            }
        }

        protected virtual void SetWorkspaceOptions(TestWorkspace workspace)
        {
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
                !CommitManager.IsCommitCharacter(service.GetRules(), completionItem, commitChar.Value, commitChar.Value.ToString()))
            {
                Assert.Equal(codeBeforeCommit, actualExpectedCode);
                return;
            }

            // textview is created lazily, so need to access it before making 
            // changes to document, so the cursor position is tracked correctly.
            var textView = WorkspaceFixture.CurrentDocument.GetTextView();

            var commit = await service.GetChangeAsync(document, completionItem, completionListSpan, commitChar, CancellationToken.None);

            var text = await document.GetTextAsync();
            var newText = text.WithChanges(commit.TextChange);
            var newDoc = document.WithText(newText);
            document.Project.Solution.Workspace.TryApplyChanges(newDoc.Project.Solution);

            var textBuffer = WorkspaceFixture.CurrentDocument.GetTextBuffer();

            var actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();
            var caretPosition = commit.NewPosition != null ? commit.NewPosition.Value : textView.Caret.Position.BufferPosition.Position;

            Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }

        internal virtual void VerifyCustomCommitWorker(
            CompletionService service,
            ICustomCommitCompletionProvider customCommitCompletionProvider,
            RoslynCompletion.CompletionItem completionItem,
            CompletionHelper completionRules,
            ITextView textView,
            ITextBuffer textBuffer,
            string codeBeforeCommit,
            string expectedCodeAfterCommit,
            char? commitChar = null)
        {
            MarkupTestFile.GetPosition(expectedCodeAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

            if (commitChar.HasValue &&
                !CommitManager.IsCommitCharacter(service.GetRules(), completionItem, commitChar.Value, commitChar.Value.ToString()))
            {
                Assert.Equal(codeBeforeCommit, actualExpectedCode);
                return;
            }

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
            char? commitChar, string textTypedSoFar, SourceCodeKind sourceCodeKind)
        {
            var document1 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            await VerifyProviderCommitCheckResultsAsync(document1, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = WorkspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                await VerifyProviderCommitCheckResultsAsync(document2, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar);
            }
        }

        private async Task VerifyProviderCommitCheckResultsAsync(
            Document document, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitCharOpt, string textTypedSoFar)
        {
            var workspace = WorkspaceFixture.GetWorkspace();
            var textBuffer = WorkspaceFixture.CurrentDocument.GetTextBuffer();
            var textSnapshot = textBuffer.CurrentSnapshot.AsText();

            var service = GetCompletionService(workspace);
            var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
            var items = completionList.Items;
            var firstItem = items.First(i => CompareItems(i.DisplayText + i.DisplayTextSuffix, itemToCommit));

            var completionRules = GetCompletionHelper(document);
            var commitChar = commitCharOpt ?? '\t';

            var text = await document.GetTextAsync();

            if (commitChar == '\t' ||
                CommitManager.IsCommitCharacter(service.GetRules(), firstItem, commitChar, textTypedSoFar + commitChar))
            {
                var textChange = (await service.GetChangeAsync(document, firstItem, completionList.Span, commitChar, CancellationToken.None)).TextChange;

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
            var xmlString = CreateMarkupForProjecWithProjectReference(markup, referencedCode, sourceLanguage, referencedLanguage);

            return VerifyItemWithReferenceWorkerAsync(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected static string CreateMarkupForProjecWithAliasedProjectReference(string markup, string projectAlias, string referencedCode, string sourceLanguage, string referencedLanguage)
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

        protected static string CreateMarkupForProjecWithProjectReference(string markup, string referencedCode, string sourceLanguage, string referencedLanguage)
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

        protected static string CreateMarkupForProjecWithVBProjectReference(string markup, string referencedCode, string sourceLanguage, string rootnamespace = "")
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
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), LanguageNames.VisualBasic, SecurityElement.Escape(referencedCode), rootnamespace);
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
            using (var testWorkspace = TestWorkspace.Create(xmlString))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                var document = solution.GetDocument(documentId);

                testWorkspace.Options = testWorkspace.Options.WithChangedOption(CompletionOptions.HideAdvancedMembers, document.Project.Language, hideAdvancedMembers);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;

                var completionService = GetCompletionService(testWorkspace);
                var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

                if (expectedSymbols >= 1)
                {
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
            using (var testWorkspace = TestWorkspace.Create(xmlString))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                var document = solution.GetDocument(documentId);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
                var completionService = GetCompletionService(testWorkspace);
                var completionList = await GetCompletionListAsync(completionService, document, position, triggerInfo);

                var item = completionList.Items.FirstOrDefault(i => i.DisplayText == expectedItem);
                Assert.Equal(expectedDescription, (await completionService.GetDescriptionAsync(document, item)).Text);
            }
        }

        private const char NonBreakingSpace = (char)0x00A0;

        private string GetExpectedOverloadSubstring(int expectedSymbols)
        {
            if (expectedSymbols <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedSymbols));
            }

            return "+" + NonBreakingSpace + (expectedSymbols - 1) + NonBreakingSpace + FeaturesResources.overload;
        }

        protected async Task VerifyItemInLinkedFilesAsync(string xmlString, string expectedItem, string expectedDescription)
        {
            using (var testWorkspace = TestWorkspace.Create(xmlString))
            {
                var position = testWorkspace.Documents.First().CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var textContainer = testWorkspace.Documents.First().GetTextBuffer().AsTextContainer();
                var currentContextDocumentId = testWorkspace.GetDocumentIdInCurrentContext(textContainer);
                var document = solution.GetDocument(currentContextDocumentId);

                var triggerInfo = RoslynCompletion.CompletionTrigger.Invoke;
                var completionService = GetCompletionService(testWorkspace);
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
            List<CompletionFilter> matchingFilters = null)
        {
            code = code.Substring(0, position) + insertText + code.Substring(position);
            position += insertText.Length;

            return BaseVerifyWorkerAsync(code, position,
                expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters);
        }

        private protected Task VerifyAtPositionAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
        {
            return VerifyAtPositionAsync(
                code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters);
        }

        private protected async Task VerifyAtEndOfFileAsync(
            string code, int position, string insertText, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
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
                matchingFilters);
        }

        private protected Task VerifyAtPosition_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
        {
            return VerifyAtPositionAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters);
        }

        private protected Task VerifyAtEndOfFileAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
        {
            return VerifyAtEndOfFileAsync(code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters);
        }

        private protected Task VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
        {
            return VerifyAtEndOfFileAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters);
        }

        protected void VerifyTextualTriggerCharacter(
            string markup, bool shouldTriggerWithTriggerOnLettersEnabled, bool shouldTriggerWithTriggerOnLettersDisabled)
        {
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter: true);
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter: false);
        }

        private void VerifyTextualTriggerCharacterWorker(
            string markup, bool expectedTriggerCharacter, bool triggerOnLetter)
        {
            using (var workspace = CreateWorkspace(markup))
            {
                var document = workspace.Documents.Single();
                var position = document.CursorPosition.Value;
                var text = document.GetTextBuffer().CurrentSnapshot.AsText();
                var options = workspace.Options.WithChangedOption(
                    CompletionOptions.TriggerOnTypingLetters, document.Project.Language, triggerOnLetter);
                var trigger = RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(text[position]);

                var service = GetCompletionService(workspace);
                var isTextualTriggerCharacterResult = service.ShouldTriggerCompletion(text, position + 1, trigger, options: options);

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

        protected async Task VerifyCommitCharactersAsync(string initialMarkup, string textTypedSoFar, char[] validChars, char[] invalidChars = null)
        {
            Assert.NotNull(validChars);
            invalidChars = invalidChars ?? new[] { 'x' };

            using (var workspace = CreateWorkspace(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var service = GetCompletionService(workspace);
                var completionList = await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke);
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                foreach (var ch in validChars)
                {
                    Assert.True(CommitManager.IsCommitCharacter(
                        service.GetRules(), item, ch, textTypedSoFar + ch), $"Expected '{ch}' to be a commit character");
                }

                foreach (var ch in invalidChars)
                {
                    Assert.False(CommitManager.IsCommitCharacter(
                        service.GetRules(), item, ch, textTypedSoFar + ch), $"Expected '{ch}' NOT to be a commit character");
                }
            }
        }

        protected async Task<ImmutableArray<RoslynCompletion.CompletionItem>> GetCompletionItemsAsync(
            string markup, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger = false)
        {
            WorkspaceFixture.GetWorkspace(markup);
            var code = WorkspaceFixture.Code;
            var position = WorkspaceFixture.Position;
            var document = WorkspaceFixture.UpdateDocument(code, sourceCodeKind);

            var trigger = usePreviousCharAsTrigger
                ? RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(insertedCharacter: code.ElementAt(position - 1))
                : RoslynCompletion.CompletionTrigger.Invoke;

            var completionService = GetCompletionService(document.Project.Solution.Workspace);
            var completionList = await GetCompletionListAsync(completionService, document, position, trigger);

            return completionList == null ? ImmutableArray<RoslynCompletion.CompletionItem>.Empty : completionList.Items;
        }
    }
}
