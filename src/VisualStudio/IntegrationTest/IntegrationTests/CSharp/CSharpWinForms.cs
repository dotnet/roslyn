// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpWinForms(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddControl()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            SaveFile(ProjectName, "Form1.cs");
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Name = ""SomeButton""");
            VerifyTextContains(@"private System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            CloseFile(ProjectName, "Form1.cs", saveFile: true);
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Text = ""NewButtonText""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            CloseFile(ProjectName, "Form1.cs", saveFile: true);
            //  Change the control's text in designer.cs code
            OpenFile(ProjectName, "Form1.Designer.cs");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            VerifyTextContains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            //  Replace text property with something else
            SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            CloseFile(ProjectName, "Form1.Designer.cs", saveFile: true);
            //  Verify that the control text has changed in the designer
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);");
            OpenFile(ProjectName, "Form1.cs");
            VerifyTextContains(@"    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ExecuteWhenButtonClicked(object sender, EventArgs e)
        {

        }
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            // Add some control properties and events
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeNewButton.Name = ""SomeNewButton"";");
            VerifyTextContains(@"this.SomeNewButton.Text = ""ButtonTextValue"";");
            VerifyTextContains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);");
            VerifyTextContains(@"private System.Windows.Forms.Button SomeNewButton;");
            // Verify that the old button name goes away
            VerifyTextDoesNotContain(@"private System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VerifyNoBuildErrors();
            //  Verify that the handler is removed
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextDoesNotContain(@"this.SomeButton.Click += new System.EventHandler(this.FooHandler);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"public System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            AddWinFormButton("SomeButton");
            DeleteWinFormButton("SomeButton");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextDoesNotContain(@"this.SomeButton.Name = ""SomeButton"";");
            VerifyTextDoesNotContain(@"private System.Windows.Forms.Button SomeButton;");
        }
    }
}
