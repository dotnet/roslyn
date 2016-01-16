// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public abstract class AbstractCSharpCompletionProviderTests : AbstractCompletionProviderTests<CSharpTestWorkspaceFixture>
    {
        protected AbstractCSharpCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        protected override async Task VerifyWorkerAsync(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            await VerifyInFrontOfCommentAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                await VerifyInFrontOfComment_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            }
        }

        protected Task BaseVerifyWorkerAsync(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, int? glyph)
        {
            return base.VerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental: false, glyph: glyph);
        }

        private Task VerifyInFrontOfCommentAsync(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            code = code.Substring(0, position) + insertText + "/**/" + code.Substring(position);
            position += insertText.Length;

            return base.VerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph: glyph);
        }

        private Task VerifyInFrontOfCommentAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyInFrontOfCommentAsync(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph: glyph);
        }

        protected Task VerifyInFrontOfComment_ItemPartiallyWrittenAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyInFrontOfCommentAsync(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph: glyph);
        }

        protected Task VerifyAtPositionAsync(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            code = code.Substring(0, position) + insertText + code.Substring(position);
            position += insertText.Length;

            return base.VerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph: glyph);
        }

        protected Task VerifyAtPositionAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyAtPositionAsync(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        protected Task VerifyAtPosition_ItemPartiallyWrittenAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyAtPositionAsync(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        private async Task VerifyAtEndOfFileAsync(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            // only do this if the placeholder was at the end of the text.
            if (code.Length != position)
            {
                return;
            }

            code = code.Substring(startIndex: 0, length: position) + insertText;
            position += insertText.Length;

            await base.VerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph);
        }

        protected Task VerifyAtEndOfFileAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyAtEndOfFileAsync(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        protected Task VerifyAtEndOfFile_ItemPartiallyWrittenAsync(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            return VerifyAtEndOfFileAsync(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        private static string ItemPartiallyWritten(string expectedItemOrNull)
        {
            return expectedItemOrNull[0] == '@' ? expectedItemOrNull.Substring(1, 1) : expectedItemOrNull.Substring(0, 1);
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

        protected async Task VerifySendEnterThroughToEnterAsync(string initialMarkup, string textTypedSoFar, bool sendThroughEnterEnabled, bool expected)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var completionList = await GetCompletionListAsync(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                var optionService = workspace.Services.GetService<IOptionService>();
                var options = optionService.GetOptions().WithChangedOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord, sendThroughEnterEnabled);
                optionService.SetOptions(options);

                var completionService = document.Project.LanguageServices.GetService<ICompletionService>();
                var completionRules = completionService.GetCompletionRules();

                Assert.Equal(expected, completionRules.SendEnterThroughToEditor(item, textTypedSoFar, workspace.Options));
            }
        }

        protected async Task VerifyTextualTriggerCharacterAsync(string markup, bool shouldTriggerWithTriggerOnLettersEnabled, bool shouldTriggerWithTriggerOnLettersDisabled)
        {
            await VerifyTextualTriggerCharacterWorkerAsync(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter: true);
            await VerifyTextualTriggerCharacterWorkerAsync(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter: false);
        }

        private async Task VerifyTextualTriggerCharacterWorkerAsync(string markup, bool expectedTriggerCharacter, bool triggerOnLetter)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(markup))
            {
                var document = workspace.Documents.Single();
                var position = document.CursorPosition.Value;
                var text = document.TextBuffer.CurrentSnapshot.AsText();
                var options = workspace.Options.WithChangedOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp, triggerOnLetter);

                var isTextualTriggerCharacterResult = CompletionProvider.IsTriggerCharacter(text, position, options);

                if (expectedTriggerCharacter)
                {
                    var assertText = "'" + text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(position, 1)) + "' expected to be textual trigger character";
                    Assert.True(isTextualTriggerCharacterResult, assertText);
                }
                else
                {
                    var assertText = "'" + text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(position, 1)) + "' expected to NOT be textual trigger character";
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

            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var completionList = await GetCompletionListAsync(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                var completionService = document.Project.LanguageServices.GetService<ICompletionService>();
                var completionRules = completionService.GetCompletionRules();

                foreach (var ch in validChars)
                {
                    Assert.True(completionRules.IsCommitCharacter(item, ch, textTypedSoFar), $"Expected '{ch}' to be a commit character");
                }

                foreach (var ch in invalidChars)
                {
                    Assert.False(completionRules.IsCommitCharacter(item, ch, textTypedSoFar), $"Expected '{ch}' NOT to be a commit character");
                }
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

        protected override ParseOptions CreateExperimentalParseOptions(ParseOptions parseOptions)
        {
            var options = (CSharpParseOptions)parseOptions;
            var experimentalFeatures = new Dictionary<string, string>(); // no experimental features to enable
            return options.WithFeatures(experimentalFeatures);
        }
    }
}
