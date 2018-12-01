// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicWinForms() : base(nameof(BasicWinForms), WellKnownProjectTemplates.WinFormsApplication) { }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void TestMyIntelliSense()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.vb");
            SetUpEditor(@"Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        $$
    End Sub
End Class");
            VisualStudioInstance.Editor.SendKeys("My.");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Application");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Computer");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Forms");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("MySettings");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Resources");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Settings");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("User");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("WebServices");
            VisualStudioInstance.Editor.Verify.CompletionItemDoNotExist("Equals");
            VisualStudioInstance.Editor.Verify.CompletionItemDoNotExist("MyApplication");

            VisualStudioInstance.Editor.SendKeys("Forms.");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Form1");
            VisualStudioInstance.Editor.Verify.CompletionItemDoNotExist("Equals");
            VisualStudioInstance.Editor.Verify.CompletionItemDoNotExist("GetHashCode");
            VisualStudioInstance.Editor.Verify.CompletionItemDoNotExist("ToString");
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void AddControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.vb", saveFile: true);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            ExtendedAssert.Contains(@"Friend WithEvents SomeButton As Button", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.vb", saveFile: true);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Me.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = VisualStudioInstance.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.AreEqual(expectedPropertyValue, actualPropertyValue);
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.vb", saveFile: true);
            //  Change the control's text in designer.vb code
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Me.SomeButton.Text = ""ButtonTextGoesHere""", actualText);
            //  Replace text property with something else
            VisualStudioInstance.Editor.SelectTextInCurrentDocument(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            VisualStudioInstance.Editor.SendKeys(@"Me.SomeButton.Text = ""GibberishText""");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.Designer.vb", saveFile: true);
            //  Verify that the control text has changed in the designer
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = VisualStudioInstance.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.AreEqual(expectedPropertyValue, actualPropertyValue);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
            VisualStudioInstance.SolutionExplorer.SaveFile(project, "Form1.vb");
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            // Add some control properties and events
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            // Verify that the rename propagated in designer code
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var formDesignerActualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Me.SomeNewButton.Name = ""SomeNewButton""", formDesignerActualText);
            ExtendedAssert.Contains(@"Me.SomeNewButton.Text = ""ButtonTextValue""", formDesignerActualText);
            ExtendedAssert.Contains(@"Friend WithEvents SomeNewButton As Button", formDesignerActualText);
            // Verify that the old button name goes away
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.vb");
            var formActualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click", formActualText);
            // Rename control from the code behind file (bug 784595)
            VisualStudioInstance.Editor.SelectTextInCurrentDocument(@"SomeNewButton");
            VisualStudioInstance.ExecuteCommand("Refactor.Rename");
            VisualStudioInstance.Editor.SendKeys("AnotherNewButton", VirtualKey.Enter);
            formActualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click", formActualText);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            //  Verify that the handler is removed
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.DoesNotContain(@"Private Sub GooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Public WithEvents SomeButton As Button", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.DeleteWinFormButton("SomeButton");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.DoesNotContain(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            ExtendedAssert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
        }
    }
}
