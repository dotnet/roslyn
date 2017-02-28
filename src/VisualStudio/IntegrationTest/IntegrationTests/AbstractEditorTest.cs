// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        protected readonly VisualStudioWorkspace_OutOfProc VisualStudioWorkspaceOutOfProc;
        protected readonly Editor_OutOfProc Editor;

        protected readonly string ProjectName = "TestProj";

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : this(instanceFactory, solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName, string projectTemplate, bool clearEditor = true)
           : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, projectTemplate, LanguageName);

            VisualStudioWorkspaceOutOfProc = VisualStudio.Instance.VisualStudioWorkspace;
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);

            Editor = VisualStudio.Instance.Editor;
            if (clearEditor)
            {
                ClearEditor();
            }
        }

        protected abstract string LanguageName { get; }

        protected void WaitForAsyncOperations(string featuresToWaitFor)
            => VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(featuresToWaitFor);

        protected void ClearEditor()
            => SetUpEditor("$$");

        protected void SetUpEditor(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

            var originalValue = VisualStudioWorkspaceOutOfProc.IsPrettyListingOn(LanguageName);

            VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, false);
            try
            {
                Editor.SetText(code);
                Editor.MoveCaret(caretPosition);
            }
            finally
            {
                VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, originalValue);
            }
        }

        protected void AddFile(string fileName, string contents = null, bool open = false)
            => VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, fileName, contents, open);

        protected void OpenFile(string projectName, string fileName)
            => VisualStudio.Instance.SolutionExplorer.OpenFile(projectName, fileName);

        protected void OpenFileWithDesigner(string projectName, string fileName)
            => VisualStudio.Instance.SolutionExplorer.OpenFileWithDesigner(projectName, fileName);

        protected void CloseFile(string projectName, string fileName, bool saveFile = true)
            => VisualStudio.Instance.SolutionExplorer.CloseFile(projectName, fileName, saveFile);

        protected void SaveFile(string projectName, string fileName)
            => VisualStudio.Instance.SolutionExplorer.SaveFile(projectName, fileName);

        protected void AddWinFormButton(string buttonName)
            => VisualStudio.Instance.Editor.AddWinFormButton(buttonName);

        protected void DeleteWinFormButton(string buttonName)
            => VisualStudio.Instance.Editor.DeleteWinFormButton(buttonName);

        protected void EditWinFormButtonProperty(string buttonName, string propertyName, string propertyValue, string propertyTypeName = null)
            => VisualStudio.Instance.Editor.EditWinFormButtonProperty(buttonName, propertyName, propertyValue, propertyTypeName);

        protected void EditWinFormsButtonEvent(string buttonName, string eventName, string eventHandlerName)
            => VisualStudio.Instance.Editor.EditWinFormButtonEvent(buttonName, eventName, eventHandlerName);

        protected string GetWinFormButtonPropertyValue(string buttonName, string propertyName)
            => VisualStudio.Instance.Editor.GetWinFormButtonPropertyValue(buttonName, propertyName);

        protected void SelectTextInCurrentDocument(string text)
        {
            VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        protected void BuildSolution(bool waitForBuildToFinish)
            => VisualStudio.Instance.SolutionExplorer.BuildSolution(waitForBuildToFinish);

        protected int GetErrorListErrorCount()
            => VisualStudio.Instance.SolutionExplorer.ErrorListErrorCount;

        protected void SendKeys(params object[] keys)
            => Editor.SendKeys(keys);

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
            => new KeyPress(virtualKey, shiftState);

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected void DisableSuggestionMode()
            => VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);

        protected void EnableSuggestionMode()
            => VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);

        protected void InvokeCompletionList()
        {
            ExecuteCommand(WellKnownCommandNames.Edit_ListMembers);
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }

        protected void InvokeSignatureHelp()
        {
            ExecuteCommand(WellKnownCommandNames.Edit_ParameterInfo);
            WaitForAsyncOperations(FeatureAttribute.SignatureHelp);
        }

        protected void InvokeCodeActionList()
        {
            WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            WaitForAsyncOperations(FeatureAttribute.DiagnosticService);

            Editor.ShowLightBulb();
            Editor.WaitForLightBulbSession();
            WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        protected void ExecuteCommand(string commandName)
            => VisualStudio.Instance.ExecuteCommand(commandName);

        private void VerifyCurrentLineTextAndAssertCaretPosition(string expectedText, bool trimWhitespace)
        {
            var caretStartIndex = expectedText.IndexOf("$$");
            if (caretStartIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var caretEndIndex = caretStartIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText.Substring(0, caretStartIndex);
            var expectedTextAfterCaret = expectedText.Substring(caretEndIndex);

            var lineText = Editor.GetCurrentLineText();

            if (trimWhitespace)
            {
                if (caretStartIndex == 0)
                {
                    lineText = lineText.TrimEnd();
                }
                else if (caretEndIndex == expectedText.Length)
                {
                    lineText = lineText.TrimStart();
                }
                else
                {
                    lineText = lineText.Trim();
                }
            }

            var lineTextBeforeCaret = caretStartIndex < lineText.Length
                ? lineText.Substring(0, caretStartIndex)
                : lineText;

            var lineTextAfterCaret = caretStartIndex < lineText.Length
                ? lineText.Substring(caretStartIndex)
                : string.Empty;

            Assert.Equal(expectedTextBeforeCaret, lineTextBeforeCaret);
            Assert.Equal(expectedTextAfterCaret, lineTextAfterCaret);
            Assert.Equal(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
        }

        protected void VerifyCurrentLineText(string expectedText, bool assertCaretPosition = false, bool trimWhitespace = true)
        {
            if (assertCaretPosition)
            {
                VerifyCurrentLineTextAndAssertCaretPosition(expectedText, trimWhitespace);
            }
            else
            {
                var lineText = Editor.GetCurrentLineText();

                if (trimWhitespace)
                {
                    lineText = lineText.Trim();
                }

                Assert.Equal(expectedText, lineText);
            }
        }

        private void VerifyTextContainsAndAssertCaretPosition(string expectedText)
        {
            var caretStartIndex = expectedText.IndexOf("$$");
            if (caretStartIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var caretEndIndex = caretStartIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText.Substring(0, caretStartIndex);
            var expectedTextAfterCaret = expectedText.Substring(caretEndIndex);

            var expectedTextWithoutCaret = expectedTextBeforeCaret + expectedTextAfterCaret;

            var editorText = Editor.GetText();
            Assert.Contains(expectedTextWithoutCaret, editorText);

            var index = editorText.IndexOf(expectedTextWithoutCaret);

            var caretPosition = Editor.GetCaretPosition();
            Assert.Equal(caretStartIndex + index, caretPosition);
        }

        protected void VerifyTextContains(string expectedText, bool assertCaretPosition = false)
        {
            if (assertCaretPosition)
            {
                VerifyTextContainsAndAssertCaretPosition(expectedText);
            }
            else
            {
                var editorText = Editor.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        protected void VerifyTextDoesNotContain(string expectedText)
        {
            var editorText = Editor.GetText();
            Assert.DoesNotContain(expectedText, editorText);
        }

        protected void VerifyCompletionItemExists(params string[] expectedItems)
        {
            var completionItems = Editor.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        protected void VerifyCurrentCompletionItem(string expectedItem)
        {
            var currentItem = Editor.GetCurrentCompletionItem();
            Assert.Equal(expectedItem, currentItem);
        }

        protected void VerifyCurrentSignature(Signature expectedSignature)
        {
            var currentSignature = Editor.GetCurrentSignature();
            Assert.Equal(expectedSignature, currentSignature);
        }

        protected void VerifyCurrentSignature(string content)
        {
            var currentSignature = Editor.GetCurrentSignature();
            Assert.Equal(content, currentSignature.Content);
        }

        protected void VerifyCurrentParameter(string name, string documentation)
        {
            var currentParameter = Editor.GetCurrentSignature().CurrentParameter;
            Assert.Equal(name, currentParameter.Name);
            Assert.Equal(documentation, currentParameter.Documentation);
        }

        protected void VerifyParameters(params (string name, string documentation)[] parameters)
        {
            var currentParameters = Editor.GetCurrentSignature().Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var (expectedName, expectedDocumentation) = parameters[i];
                Assert.Equal(expectedName, currentParameters[i].Name);
                Assert.Equal(expectedDocumentation, currentParameters[i].Documentation);
            }
        }

        protected void VerifyCaretIsOnScreen()
            => Assert.True(Editor.IsCaretOnScreen());

        protected void VerifyCompletionListIsActive(bool expected)
            => Assert.Equal(expected, Editor.IsCompletionActive());

        protected void VerifyFileContents(string fileName, string expectedContents)
        {
            var actualContents = VisualStudio.Instance.SolutionExplorer.GetFileContents(ProjectName, fileName);
            Assert.Equal(expectedContents, actualContents);
        }

        public void VerifyCodeActionsNotShowing()
        {
            if (Editor.IsLightBulbSessionExpanded())
            {
                throw new InvalidOperationException("Expected no light bulb session, but one was found.");
            }
        }

        public void VerifyNoBuildErrors()
        {
            BuildSolution(waitForBuildToFinish: true);
            Assert.Equal(0, GetErrorListErrorCount());
        }

        public void VerifyCodeAction(
            string expectedItem,
            bool applyFix = false,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true)
        {
            var expectedItems = new[] { expectedItem };
            VerifyCodeActions(
                expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                ensureExpectedItemsAreOrdered, fixAllScope, blockUntilComplete);
        }

        public void VerifyCodeActions(
            IEnumerable<string> expectedItems,
            string applyFix = null,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true)
        {
            Editor.ShowLightBulb();
            Editor.WaitForLightBulbSession();

            if (verifyNotShowing)
            {
                VerifyCodeActionsNotShowing();
                return;
            }

            var actions = Editor.GetLightBulbActions();

            if (expectedItems != null && expectedItems.Any())
            {
                if (ensureExpectedItemsAreOrdered)
                {
                    TestUtilities.ThrowIfExpectedItemNotFoundInOrder(
                        actions,
                        expectedItems);
                }
                else
                {
                    TestUtilities.ThrowIfExpectedItemNotFound(
                        actions,
                        expectedItems);
                }
            }

            if (!string.IsNullOrEmpty(applyFix) || fixAllScope.HasValue)
            {
                Editor.ApplyLightBulbAction(applyFix, fixAllScope, blockUntilComplete);

                if (blockUntilComplete)
                {
                    // wait for action to complete
                    WaitForAsyncOperations(FeatureAttribute.LightBulb);
                }
            }
        }

        public void VerifyDialog(string dialogName, bool isOpen)
        {
            Editor.VerifyDialog(dialogName, isOpen);
        }

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
        {
            Editor.PressDialogButton(dialogAutomationName, buttonAutomationName);
        }

        public AutomationElement GetDialog(string dialogAutomationId)
        {
            var dialog = DialogHelpers.FindDialog(VisualStudio.Instance.Shell.GetHWnd(), dialogAutomationId, isOpen: true);
            Assert.NotNull(dialog);
            return dialog;
        }
    }
}
