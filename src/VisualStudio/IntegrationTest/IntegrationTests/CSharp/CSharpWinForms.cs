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
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            SaveFile("Form1.cs", ProjectName);
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextContains(@"this.SomeButton.Name = ""SomeButton""");
            VerifyTextContains(@"private System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            CloseFile("Form1.cs", ProjectName, saveFile: true);
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextContains(@"this.SomeButton.Text = ""NewButtonText""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            CloseFile("Form1.cs", ProjectName, saveFile: true);
            //  Change the control's text in designer.cs code
            OpenFile("Form1.Designer.cs", ProjectName);
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            VerifyTextContains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            //  Replace text property with something else
            SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            CloseFile("Form1.Designer.cs", ProjectName, saveFile: true);
            //  Verify that the control text has changed in the designer
            OpenFileWithDesigner("Form1.cs", ProjectName);
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextContains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);");
            OpenFile("Form1.cs", ProjectName);
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
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            // Add some control properties and events
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code
            OpenFile("Form1.Designer.cs", ProjectName);
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
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler
            EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VerifyNoBuildErrors();
            //  Verify that the handler is removed
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextDoesNotContain(@"this.SomeButton.Click += new System.EventHandler(this.FooHandler);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VerifyNoBuildErrors();
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextContains(@"public System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            OpenFileWithDesigner("Form1.cs", ProjectName);
            AddWinFormButton("SomeButton");
            DeleteWinFormButton("SomeButton");
            VerifyNoBuildErrors();
            OpenFile("Form1.Designer.cs", ProjectName);
            VerifyTextDoesNotContain(@"this.SomeButton.Name = ""SomeButton"";");
            VerifyTextDoesNotContain(@"private System.Windows.Forms.Button SomeButton;");
        }
    }
}
