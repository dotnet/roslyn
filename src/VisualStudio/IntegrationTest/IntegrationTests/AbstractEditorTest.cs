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
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        protected readonly Editor_OutOfProc Editor;

        protected readonly string ProjectName = "TestProj";

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, visualStudio => visualStudio.Instance.Editor)
        {
            Editor = (Editor_OutOfProc)TextViewWindow;
        }

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : this(instanceFactory, solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(
            VisualStudioInstanceFactory instanceFactory,
            string solutionName,
            string projectTemplate)
           : base(instanceFactory, visualStudio => visualStudio.Instance.Editor)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, projectTemplate, LanguageName);

            Editor = (Editor_OutOfProc)TextViewWindow;

            // Winforms and XAML do not open text files on creation
            // so these editor tasks will not work if that is the project template being used.
            if (projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                projectTemplate != WellKnownProjectTemplates.WpfApplication)
            {
                VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
                ClearEditor();
            }
        }

        protected abstract string LanguageName { get; }

        protected void WaitForAsyncOperations(params string[] featuresToWaitFor)
            => VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(string.Join(";", featuresToWaitFor));

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

        protected void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => VisualStudio.Instance.SolutionExplorer.CreateSolution(solutionName, saveExistingSolutionIfExists);

        protected void CloseSolution(bool saveFirst = false)
            => VisualStudio.Instance.SolutionExplorer.CloseSolution(saveFirst);

        protected void AddProject(string projectTemplate, string projectName = null, string languageName = null)
            => VisualStudio.Instance.SolutionExplorer.AddProject(projectName ?? ProjectName, projectTemplate, languageName ?? LanguageName);

        protected void AddFile(string fileName, string projectName = null, string contents = null, bool open = false)
            => VisualStudio.Instance.SolutionExplorer.AddFile(projectName ?? ProjectName, fileName, contents, open);

        protected void AddMetadataReference(ProjectUtils.AssemblyReference referenceName, ProjectUtils.Project projectName = null)
        {
            projectName = projectName ?? new ProjectUtils.Project(ProjectName);
            VisualStudio.Instance.SolutionExplorer.AddMetadataReference(referenceName.Name, projectName.Name);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void RemoveMetadataReference(ProjectUtils.AssemblyReference referenceName, ProjectUtils.Project projectName = null)
        {
            projectName = projectName ?? new ProjectUtils.Project(ProjectName);
            VisualStudio.Instance.SolutionExplorer.RemoveMetadataReference(referenceName.Name, projectName.Name);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void AddProjectReference(ProjectUtils.Project fromProjectName, ProjectUtils.ProjectReference toProjectName)
        {
            VisualStudio.Instance.SolutionExplorer.AddProjectReference(fromProjectName.Name, toProjectName.Name);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void RemoveProjectReference(ProjectUtils.ProjectReference projectReferenceName, ProjectUtils.Project projectName = null)
        {
            projectName = projectName ?? new ProjectUtils.Project(ProjectName);
            VisualStudio.Instance.SolutionExplorer.RemoveProjectReference(projectName.Name, projectReferenceName.Name);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void OpenFile(string fileName, string projectName = null)
            => VisualStudio.Instance.SolutionExplorer.OpenFile(projectName ?? ProjectName, fileName);

        protected void OpenFileWithDesigner(string fileName, string projectName = null)
            => VisualStudio.Instance.SolutionExplorer.OpenFileWithDesigner(projectName ?? ProjectName, fileName);

        protected void CloseFile(string fileName, string projectName = null, bool saveFile = true)
            => VisualStudio.Instance.SolutionExplorer.CloseFile(projectName ?? ProjectName, fileName, saveFile);

        protected void SaveFile(string fileName, string projectName = null)
            => VisualStudio.Instance.SolutionExplorer.SaveFile(projectName ?? ProjectName, fileName);

        protected void SaveAll()
            => VisualStudio.Instance.SolutionExplorer.SaveAll();

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

        protected void DeleteText(string text)
        {
            SelectTextInCurrentDocument(text);
            SendKeys(VirtualKey.Delete);
        }

        protected void PlaceCaret(string text, int charsOffset = 0)
            => VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: charsOffset, occurrence: 0, extendSelection: false, selectBlock: false);

        protected void BuildSolution(bool waitForBuildToFinish)
            => VisualStudio.Instance.SolutionExplorer.BuildSolution(waitForBuildToFinish);

        protected int GetErrorListErrorCount()
            => VisualStudio.Instance.SolutionExplorer.ErrorListErrorCount;

        protected void SendKeys(params object[] keys)
            => Editor.SendKeys(keys);

        protected void DisableSuggestionMode()
            => VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);

        protected void EnableSuggestionMode()
            => VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);

        protected void EnableQuickInfo()
        {
            VisualStudioWorkspaceOutOfProc.SetQuickInfo(true);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void DisableQuickInfo()
        {
            VisualStudioWorkspaceOutOfProc.SetQuickInfo(false);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void EnableOptionInfer(string projectName = null)
        {
            projectName = projectName ?? ProjectName;
            VisualStudioWorkspaceOutOfProc.SetOptionInfer(projectName, true);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void DisableOptionInfer(string projectName = null)
        {
            projectName = projectName ?? ProjectName;
            VisualStudioWorkspaceOutOfProc.SetOptionInfer(projectName, false);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void EnablePersistence()
        {
            VisualStudioWorkspaceOutOfProc.SetPersistenceOption(true);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void DisablePersistence()
        {
            VisualStudioWorkspaceOutOfProc.SetPersistenceOption(false);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected void InvokeSignatureHelp()
        {
            ExecuteCommand(WellKnownCommandNames.Edit_ParameterInfo);
            WaitForAsyncOperations(FeatureAttribute.SignatureHelp);
        }

        protected void InvokeQuickInfo()
        {
            ExecuteCommand(WellKnownCommandNames.Edit_QuickInfo);
            WaitForAsyncOperations(FeatureAttribute.QuickInfo);
        }

        protected void InvokeCodeActionList()
        {
            WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            WaitForAsyncOperations(FeatureAttribute.DiagnosticService);

            Editor.ShowLightBulb();
            Editor.WaitForLightBulbSession();
            WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        protected void EnableFullSolutionAnalysis()
        {
            VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.CSharp,
                value: "true");

            VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.VisualBasic,
                value: "true");
        }

        protected void DisableFullSolutionAnalysis()
        {
            VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.CSharp,
                value: "false");

            VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.VisualBasic,
                value: "false");
        }

        protected void EditProjectFile(string projectName)
        {
            VisualStudio.Instance.SolutionExplorer.EditProjectFile(projectName);
        }

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

        protected void VerifyCompletionItemDoesNotExist(params string[] expectedItems)
        {
            var completionItems = Editor.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.DoesNotContain(expectedItem, completionItems);
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
            for (var i = 0; i < parameters.Length; i++)
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

        public void VerifyAssemblyReferencePresent(string projectName, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken)
        {
            var assemblyReferences = VisualStudio.Instance.SolutionExplorer.GetAssemblyReferences(projectName);
            var expectedAssemblyReference = assemblyName + "," + assemblyVersion + "," + assemblyPublicKeyToken.ToUpper();
            Assert.Contains(expectedAssemblyReference, assemblyReferences);
        }

        public void VerifyProjectReferencePresent(string projectName, string referencedProjectName)
        {
            var projectReferences = VisualStudio.Instance.SolutionExplorer.GetProjectReferences(projectName);
            Assert.Contains(referencedProjectName, projectReferences);

        }
        protected void InvokeNavigateToAndPressEnter(string text)
        {
            ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            Editor.NavigateToSendKeys(text);
            WaitForAsyncOperations(FeatureAttribute.NavigateTo);
            Editor.NavigateToSendKeys("{ENTER}");
        }
    }
}