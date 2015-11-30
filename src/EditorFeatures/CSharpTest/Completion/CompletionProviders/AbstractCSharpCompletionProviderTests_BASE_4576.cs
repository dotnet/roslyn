// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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

        protected override void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtPosition(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            VerifyInFrontOfComment(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            VerifyAtEndOfFile(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                VerifyAtPosition_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                VerifyInFrontOfComment_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                VerifyAtEndOfFile_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            }
        }

        protected void BaseVerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, int? glyph)
        {
            base.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental: false, glyph: glyph);
        }

        private void VerifyInFrontOfComment(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            code = code.Substring(0, position) + insertText + "/**/" + code.Substring(position);
            position += insertText.Length;

            base.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph: glyph);
        }

        private void VerifyInFrontOfComment(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyInFrontOfComment(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph: glyph);
        }

        protected void VerifyInFrontOfComment_ItemPartiallyWritten(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyInFrontOfComment(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph: glyph);
        }

        protected void VerifyAtPosition(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            code = code.Substring(0, position) + insertText + code.Substring(position);
            position += insertText.Length;

            base.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph: glyph);
        }

        protected void VerifyAtPosition(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtPosition(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        protected void VerifyAtPosition_ItemPartiallyWritten(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtPosition(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        private void VerifyAtEndOfFile(string code, int position, string insertText, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            // only do this if the placeholder was at the end of the text.
            if (code.Length != position)
            {
                return;
            }

            code = code.Substring(startIndex: 0, length: position) + insertText;
            position += insertText.Length;

            base.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph);
        }

        protected void VerifyAtEndOfFile(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtEndOfFile(code, position, string.Empty, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
        }

        protected void VerifyAtEndOfFile_ItemPartiallyWritten(string code, int position, bool usePreviousCharAsTrigger, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtEndOfFile(code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
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

        protected void VerifySendEnterThroughToEnter(string initialMarkup, string textTypedSoFar, bool sendThroughEnterEnabled, bool expected)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var completionList = GetCompletionList(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
                var item = completionList.Items.First(i => i.DisplayText.StartsWith(textTypedSoFar));

                var optionService = workspace.Services.GetService<IOptionService>();
                var options = optionService.GetOptions().WithChangedOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord, sendThroughEnterEnabled);
                optionService.SetOptions(options);

                var completionService = document.Project.LanguageServices.GetService<ICompletionService>();
                var completionRules = completionService.GetCompletionRules();

                Assert.Equal(expected, completionRules.SendEnterThroughToEditor(item, textTypedSoFar, workspace.Options));
            }
        }

        protected void VerifyTextualTriggerCharacter(string markup, bool shouldTriggerWithTriggerOnLettersEnabled, bool shouldTriggerWithTriggerOnLettersDisabled)
        {
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter: true);
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter: shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter: false);
        }

        private void VerifyTextualTriggerCharacterWorker(string markup, bool expectedTriggerCharacter, bool triggerOnLetter)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(markup))
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

        protected void VerifyCommonCommitCharacters(string initialMarkup, string textTypedSoFar)
        {
            var commitCharacters = new[]
            {
                ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
                ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
                '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
            };

            VerifyCommitCharacters(initialMarkup, textTypedSoFar, commitCharacters);
        }

        protected void VerifyCommitCharacters(string initialMarkup, string textTypedSoFar, char[] validChars, char[] invalidChars = null)
        {
            Assert.NotNull(validChars);
            invalidChars = invalidChars ?? new [] { 'x' };

            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(initialMarkup))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var documentId = workspace.GetDocumentId(hostDocument);
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var position = hostDocument.CursorPosition.Value;

                var completionList = GetCompletionList(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
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

        protected void TestCommonIsTextualTriggerCharacter()
        {
            var alwaysTriggerList = new[]
            {
                "foo$$.",
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
                "foo$$x",
                "foo$$_"
            };

            foreach (var markup in neverTriggerList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
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
