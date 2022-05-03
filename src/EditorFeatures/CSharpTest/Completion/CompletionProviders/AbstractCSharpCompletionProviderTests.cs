// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Xunit;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public abstract class AbstractCSharpCompletionProviderTests : AbstractCSharpCompletionProviderTests<CSharpTestWorkspaceFixture>
    {
    }

    public abstract class AbstractCSharpCompletionProviderTests<TWorkspaceFixture> : AbstractCompletionProviderTests<TWorkspaceFixture>
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected const string NonBreakingSpaceString = "\x00A0";

        protected static string GetMarkup(string source, LanguageVersion languageVersion)
            => $@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" LanguageVersion=""{languageVersion.ToDisplayString()}"">
        <Document FilePath=""Test2.cs"">
<![CDATA[
{source}
]]>
        </Document>
    </Project>
</Workspace>";

        protected override TestWorkspace CreateWorkspace(string fileContents)
            => TestWorkspace.CreateCSharp(fileContents, exportProvider: ExportProvider);

        internal override CompletionServiceWithProviders GetCompletionService(Project project)
            => Assert.IsType<CSharpCompletionService>(base.GetCompletionService(project));

        private protected override Task BaseVerifyWorkerAsync(
            string code, int position,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, bool skipSpeculation = false)
        {
            return base.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, skipSpeculation: skipSpeculation);
        }

        private protected override async Task VerifyWorkerAsync(
            string code, int position,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger,
            bool checkForAbsence, int? glyph, int? matchPriority,
            bool? hasSuggestionItem, string displayTextSuffix, string displayTextPrefix, string inlineDescription = null,
            bool? isComplexTextEdit = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, bool skipSpeculation = false)
        {
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
            await VerifyInFrontOfCommentAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
                await VerifyInFrontOfComment_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters);
            }
        }

        protected override string ItemPartiallyWritten(string expectedItemOrNull)
            => expectedItemOrNull[0] == '@' ? expectedItemOrNull.Substring(1, 1) : expectedItemOrNull.Substring(0, 1);

        private async Task VerifyInFrontOfCommentAsync(
            string code, int position, string insertText, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit, List<CompletionFilter> matchingFilters, bool skipSpeculation = false)
        {
            code = code.Substring(0, position) + insertText + "/**/" + code.Substring(position);
            position += insertText.Length;

            await base.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix,
                inlineDescription, isComplexTextEdit, matchingFilters, flags: null, skipSpeculation: skipSpeculation);
        }

        private async Task VerifyInFrontOfCommentAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
            List<CompletionFilter> matchingFilters, bool skipSpeculation = false)
        {
            await VerifyInFrontOfCommentAsync(
                code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
        }

        private protected async Task VerifyInFrontOfComment_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph,
            int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
            List<CompletionFilter> matchingFilters, bool skipSpeculation = false)
        {
            await VerifyInFrontOfCommentAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, skipSpeculation: skipSpeculation);
        }

        protected static string AddInsideMethod(string text)
        {
            return
@"class C
{
  void F()
  {
    " + text +
@"  }
}";
        }

        protected static string AddUsingDirectives(string usingDirectives, string text)
        {
            return
usingDirectives +
@"


" +
text;
        }

        protected async Task VerifySendEnterThroughToEnterAsync(string initialMarkup, string textTypedSoFar, EnterKeyRule sendThroughEnterOption, bool expected)
        {
            using var workspace = CreateWorkspace(initialMarkup);
            var hostDocument = workspace.DocumentWithCursor;

            var documentId = workspace.GetDocumentId(hostDocument);
            var document = workspace.CurrentSolution.GetDocument(documentId);
            var position = hostDocument.CursorPosition.Value;
            var options = CompletionOptions.Default with { EnterKeyBehavior = sendThroughEnterOption };

            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, RoslynTrigger.Invoke);
            var item = completionList.Items.First(i => (i.DisplayText + i.DisplayTextSuffix).StartsWith(textTypedSoFar));

            Assert.Equal(expected, CommitManager.SendEnterThroughToEditor(service.GetRules(options), item, textTypedSoFar));
        }

        protected void TestCommonIsTextualTriggerCharacter()
        {
            var alwaysTriggerList = new[]
            {
                "goo$$.",
            };

            foreach (var markup in alwaysTriggerList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            }

            var triggerOnlyWithLettersList = new[]
            {
                "$$a",
                "$$_"
            };

            foreach (var markup in triggerOnlyWithLettersList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            }

            var neverTriggerList = new[]
            {
                "goo$$x",
                "goo$$_"
            };

            foreach (var markup in neverTriggerList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            }
        }
    }
}
