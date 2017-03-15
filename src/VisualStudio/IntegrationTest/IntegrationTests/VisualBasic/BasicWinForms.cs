// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

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
        public void Test_My_IntelliSense()
        {
            OpenFile(ProjectName, "Form1.vb");
            SetUpEditor(@"Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        $$
    End Sub
End Class");
            SendKeys("My.");
            VerifyCompletionItemExists("Application");
            VerifyCompletionItemExists("Computer");
            VerifyCompletionItemExists("Forms");
            VerifyCompletionItemExists("MySettings");
            VerifyCompletionItemExists("Resources");
            VerifyCompletionItemExists("Settings");
            VerifyCompletionItemExists("User");
            VerifyCompletionItemExists("WebServices");
            VerifyCompletionItemDoesNotExist("Equals");
            VerifyCompletionItemDoesNotExist("MyApplication");

            SendKeys("Forms.");
            VerifyCompletionItemExists("Form1");
            VerifyCompletionItemDoesNotExist("Equals");
            VerifyCompletionItemDoesNotExist("GetHashCode");
            VerifyCompletionItemDoesNotExist("ToString");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Add_Control()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            CloseFile(ProjectName, "Form1.vb", saveFile: true);
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextContains(@"Me.SomeButton.Name = ""SomeButton""");
            VerifyTextContains(@"Friend WithEvents SomeButton As Button");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Control_Property()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            CloseFile(ProjectName, "Form1.vb", saveFile: true);
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextContains(@"Me.SomeButton.Text = ""NewButtonText""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Control_Property_In_Code()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            CloseFile(ProjectName, "Form1.vb", saveFile: true);
            //  Change the control's text in designer.vb code
            OpenFile(ProjectName, "Form1.Designer.vb");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            VerifyTextContains(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            //  Replace text property with something else
            SelectTextInCurrentDocument(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            SendKeys(@"Me.SomeButton.Text = ""GibberishText""");
            CloseFile(ProjectName, "Form1.Designer.vb", saveFile: true);
            //  Verify that the control text has changed in the designer
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Add_Click_Handler()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            OpenFile(ProjectName, "Form1.vb");
            VerifyTextContains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click");
            SaveFile(ProjectName, "Form1.vb");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Rename_Control()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            // Add some control properties and events
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextContains(@"Me.SomeNewButton.Name = ""SomeNewButton""");
            VerifyTextContains(@"Me.SomeNewButton.Text = ""ButtonTextValue""");
            VerifyTextContains(@"Friend WithEvents SomeNewButton As Button");
            // Verify that the old button name goes away
            VerifyTextDoesNotContain(@"Friend WithEvents SomeButton As Button");
            OpenFile(ProjectName, "Form1.vb");
            VerifyTextContains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click");
            // Rename control from the code behind file (bug 784595)
            SelectTextInCurrentDocument(@"SomeNewButton");
            ExecuteCommand("Refactor.Rename");
            SendKeys("AnotherNewButton", VirtualKey.Enter);
            VerifyTextContains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Remove_Event_Handler()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VerifyNoBuildErrors();
            //  Verify that the handler is removed
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextDoesNotContain(@"Private Sub FooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Accessibility()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextContains(@"Public WithEvents SomeButton As Button");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Delete_Control()
        {
            OpenFileWithDesigner(ProjectName, "Form1.vb");
            AddWinFormButton("SomeButton");
            DeleteWinFormButton("SomeButton");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.vb");
            VerifyTextDoesNotContain(@"Me.SomeButton.Name = ""SomeButton""");
            VerifyTextDoesNotContain(@"Friend WithEvents SomeButton As Button");
        }
    }
}
