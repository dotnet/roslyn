// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

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
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.SaveFile("Form1.cs", project);
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"this.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            this.CloseFile("Form1.cs", project, saveFile: true);
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"this.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = this.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            this.CloseFile("Form1.cs", project, saveFile: true);
            //  Change the control's text in designer.cs code
            this.OpenFile("Form1.Designer.cs", project);
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = Editor.GetText();
            Assert.Contains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", actualText);
            //  Replace text property with something else
            this.SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            this.SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            this.CloseFile("Form1.Designer.cs", project, saveFile: true);
            //  Verify that the control text has changed in the designer
            this.OpenFileWithDesigner("Form1.cs", project);
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = this.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            this.OpenFile("Form1.Designer.cs", project);
            var designerActualText = Editor.GetText();
            Assert.Contains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);", designerActualText);
            this.OpenFile("Form1.cs", project);
            var codeFileActualText = Editor.GetText();
            Assert.Contains(@"    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ExecuteWhenButtonClicked(object sender, EventArgs e)
        {

        }
    }", codeFileActualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            // Add some control properties and events
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            this.VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"this.SomeNewButton.Name = ""SomeNewButton"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Text = ""ButtonTextValue"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeNewButton;", actualText);
            // Verify that the old button name goes away
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler
            this.EditWinFormsButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            this.VerifyNoBuildErrors();
            //  Verify that the handler is removed
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.DoesNotContain(@"this.SomeButton.Click += new System.EventHandler(this.FooHandler);", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            this.VerifyNoBuildErrors();
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.Contains(@"public System.Windows.Forms.Button SomeButton;", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.OpenFileWithDesigner("Form1.cs", project);
            this.AddWinFormButton("SomeButton");
            this.DeleteWinFormButton("SomeButton");
            this.VerifyNoBuildErrors();
            this.OpenFile("Form1.Designer.cs", project);
            var actualText = Editor.GetText();
            Assert.DoesNotContain(@"this.SomeButton.Name = ""SomeButton"";", actualText);
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }
    }
}
