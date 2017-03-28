// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicWinForms(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void TestMyIntelliSense()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFile("Form1.vb", project);
            SetUpEditor(@"Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        $$
    End Sub
End Class");
            this.SendKeys("My.");
            this.VerifyCompletionItemExists("Application");
            this.VerifyCompletionItemExists("Computer");
            this.VerifyCompletionItemExists("Forms");
            this.VerifyCompletionItemExists("MySettings");
            this.VerifyCompletionItemExists("Resources");
            this.VerifyCompletionItemExists("Settings");
            this.VerifyCompletionItemExists("User");
            this.VerifyCompletionItemExists("WebServices");
            this.VerifyCompletionItemDoNotExist("Equals");
            this.VerifyCompletionItemDoNotExist("MyApplication");

            this.SendKeys("Forms.");
            this.VerifyCompletionItemExists("Form1");
            this.VerifyCompletionItemDoNotExist("Equals");
            this.VerifyCompletionItemDoNotExist("GetHashCode");
            this.VerifyCompletionItemDoNotExist("ToString");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.CloseFile("Form1.vb", project, saveFile: true);
            this.OpenFile("Form1.Designer.vb", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"Friend WithEvents SomeButton As Button", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            this.CloseFile("Form1.vb", project, saveFile: true);
            this.OpenFile("Form1.Designer.vb", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = this.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            this.CloseFile("Form1.vb", project, saveFile: true);
            //  Change the control's text in designer.vb code
            this.OpenFile("Form1.Designer.vb", project);
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = Editor.GetText();
            Assert.Contains(@"Me.SomeButton.Text = ""ButtonTextGoesHere""", actualText);
            //  Replace text property with something else
            this.SelectTextInCurrentDocument(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            this.SendKeys(@"Me.SomeButton.Text = ""GibberishText""");
            this.CloseFile("Form1.Designer.vb", project, saveFile: true);
            //  Verify that the control text has changed in the designer
            this.OpenFileWithDesigner("Form1.vb", project);
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = this.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            this.OpenFile("Form1.vb", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
            this.SaveFile("Form1.vb", project);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            // Add some control properties and events
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            this.VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code
            this.OpenFile("Form1.Designer.vb", project);
            var formDesignerActualText = Editor.GetText();
            Assert.Contains(@"Me.SomeNewButton.Name = ""SomeNewButton""", formDesignerActualText);
            Assert.Contains(@"Me.SomeNewButton.Text = ""ButtonTextValue""", formDesignerActualText);
            Assert.Contains(@"Friend WithEvents SomeNewButton As Button", formDesignerActualText);
            // Verify that the old button name goes away
            var actualText = Editor.GetText();
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
            this.OpenFile("Form1.vb", project);
            var formActualText = Editor.GetText();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click", formActualText);
            // Rename control from the code behind file (bug 784595)
            this.SelectTextInCurrentDocument(@"SomeNewButton");
            this.ExecuteCommand("Refactor.Rename");
            this.SendKeys("AnotherNewButton", VirtualKey.Enter);
            formActualText = Editor.GetText();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click", formActualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            this.VerifyNoBuildErrors();
            //  Verify that the handler is removed
            this.OpenFile("Form1.Designer.vb", project);
            var actualText = Editor.GetText();
            Assert.DoesNotContain(@"Private Sub FooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            this.VerifyNoBuildErrors();
            this.OpenFile("Form1.Designer.vb", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"Public WithEvents SomeButton As Button", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.vb", project);
            this.AddWinFormButton("SomeButton");
            this.DeleteWinFormButton("SomeButton");
            this.VerifyNoBuildErrors();
            this.OpenFile("Form1.Designer.vb", project);
            var actualText = Editor.GetText();
            Assert.DoesNotContain(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
        }
    }
}
