// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicWinForms(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void TestMyIntelliSense()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.vb");
            SetUpEditor(@"Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        $$
    End Sub
End Class");
            VisualStudio.Editor.SendKeys("My.");
            VisualStudio.Editor.Verify.CompletionItemsExist("Application");
            VisualStudio.Editor.Verify.CompletionItemsExist("Computer");
            VisualStudio.Editor.Verify.CompletionItemsExist("Forms");
            VisualStudio.Editor.Verify.CompletionItemsExist("MySettings");
            VisualStudio.Editor.Verify.CompletionItemsExist("Resources");
            VisualStudio.Editor.Verify.CompletionItemsExist("Settings");
            VisualStudio.Editor.Verify.CompletionItemsExist("User");
            VisualStudio.Editor.Verify.CompletionItemsExist("WebServices");
            VisualStudio.Editor.Verify.CompletionItemDoNotExist("Equals");
            VisualStudio.Editor.Verify.CompletionItemDoNotExist("MyApplication");

            VisualStudio.Editor.SendKeys("Forms.");
            VisualStudio.Editor.Verify.CompletionItemsExist("Form1");
            VisualStudio.Editor.Verify.CompletionItemDoNotExist("Equals");
            VisualStudio.Editor.Verify.CompletionItemDoNotExist("GetHashCode");
            VisualStudio.Editor.Verify.CompletionItemDoNotExist("ToString");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.SolutionExplorer.CloseDesignerFile(project, "Form1.vb", saveFile: true);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"Friend WithEvents SomeButton As Button", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            VisualStudio.SolutionExplorer.CloseDesignerFile(project, "Form1.vb", saveFile: true);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = VisualStudio.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            VisualStudio.SolutionExplorer.CloseDesignerFile(project, "Form1.vb", saveFile: true);
            //  Change the control's text in designer.vb code
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Text = ""ButtonTextGoesHere""", actualText);
            //  Replace text property with something else
            VisualStudio.Editor.SelectTextInCurrentDocument(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            VisualStudio.Editor.SendKeys(@"Me.SomeButton.Text = ""GibberishText""");
            VisualStudio.SolutionExplorer.CloseCodeFile(project, "Form1.Designer.vb", saveFile: true);
            //  Verify that the control text has changed in the designer
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = VisualStudio.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
            VisualStudio.SolutionExplorer.SaveFile(project, "Form1.vb");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            // Add some control properties and events
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            // Verify that the rename propagated in designer code
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var formDesignerActualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Me.SomeNewButton.Name = ""SomeNewButton""", formDesignerActualText);
            Assert.Contains(@"Me.SomeNewButton.Text = ""ButtonTextValue""", formDesignerActualText);
            Assert.Contains(@"Friend WithEvents SomeNewButton As Button", formDesignerActualText);
            // Verify that the old button name goes away
            var actualText = VisualStudio.Editor.GetText();
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.vb");
            var formActualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click", formActualText);
            // Rename control from the code behind file (bug 784595)
            VisualStudio.Editor.SelectTextInCurrentDocument(@"SomeNewButton");
            VisualStudio.ExecuteCommand("Refactor.Rename");
            VisualStudio.Editor.SendKeys("AnotherNewButton", VirtualKey.Enter);
            formActualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click", formActualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            //  Verify that the handler is removed
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.DoesNotContain(@"Private Sub GooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Public WithEvents SomeButton As Button", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.vb");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.DeleteWinFormButton("SomeButton");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.DoesNotContain(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
        }
    }
}
