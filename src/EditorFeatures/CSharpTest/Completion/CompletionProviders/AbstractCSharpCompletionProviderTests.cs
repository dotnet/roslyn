// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public abstract class AbstractCSharpCompletionProviderTests : AbstractCompletionProviderTests<CSharpTestWorkspaceFixture>
    {
        protected AbstractCSharpCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        protected override Task<TestWorkspace> CreateWorkspaceAsync(string fileContents)
        {
            return TestWorkspace.CreateCSharpAsync(fileContents);
        }

        internal override CompletionServiceWithProviders CreateCompletionService(
            Workspace workspace, ImmutableArray<CompletionProvider> exclusiveProviders)
        {
            return new CSharpCompletionService(workspace, exclusiveProviders);
        }

        protected override Task BaseVerifyWorkerAsync(
            string code, int position,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority)
        {
            return base.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority);
        }

        protected override async Task VerifyWorkerAsync(
            string code, int position, 
            string expectedItemOrNull, string expectedDescriptionOrNull, 
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, 
            bool checkForAbsence, int? glyph, int? matchPriority)
        {
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
            await VerifyInFrontOfCommentAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
                await VerifyInFrontOfComment_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
            }
        }

        protected override string ItemPartiallyWritten(string expectedItemOrNull)
        {
            return expectedItemOrNull[0] == '@' ? expectedItemOrNull.Substring(1, 1) : expectedItemOrNull.Substring(0, 1);
        }

        private Task VerifyInFrontOfCommentAsync(
            string code, int position, string insertText, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph, int? matchPriority)
        {
            code = code.Substring(0, position) + insertText + "/**/" + code.Substring(position);
            position += insertText.Length;

            return base.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, matchPriority);
        }

        private Task VerifyInFrontOfCommentAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph, int? matchPriority)
        {
            return VerifyInFrontOfCommentAsync(
                code, position, string.Empty, usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority);
        }

        protected Task VerifyInFrontOfComment_ItemPartiallyWrittenAsync(
            string code, int position, bool usePreviousCharAsTrigger,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool checkForAbsence, int? glyph, int? matchPriority)
        {
            return VerifyInFrontOfCommentAsync(
                code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger,
                expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority);
        }

        protected string AddInsideMethod(string text)
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

        protected string AddUsingDirectives(string usingDirectives, string text)
        {
            return
usingDirectives +
@"


" +
text;
        }

        protected async Task VerifySendEnterThroughToEnterAsync(string initialMarkup, string textTypedSoFar, EnterKeyRule sendThroughEnterOption, bool expected)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                workspace.Options = workspace.Options.WithChangedOption(
                    CompletionOptions.EnterKeyBehavior,
                    LanguageNames.CSharp,
                    sendThroughEnterOption);

                var service = GetCompletionService(workspace);
                var completionList = await GetCompletionListAsync(service, document, position, CompletionTrigger.Default);
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                Assert.Equal(expected, Controller.SendEnterThroughToEditor(service.GetRules(), item, textTypedSoFar));
            }
        }

        protected async Task TestCommonIsTextualTriggerCharacterAsync()
        {
            var alwaysTriggerList = new[]
            {
                "foo$$.",
            };

            foreach (var markup in alwaysTriggerList)
            {
                await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            }

            var triggerOnlyWithLettersList = new[]
            {
                "$$a",
                "$$_"
            };

            foreach (var markup in triggerOnlyWithLettersList)
            {
                await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            }

            var neverTriggerList = new[]
            {
                "foo$$x",
                "foo$$_"
            };

            foreach (var markup in neverTriggerList)
            {
                await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            }
        }
    }
}
