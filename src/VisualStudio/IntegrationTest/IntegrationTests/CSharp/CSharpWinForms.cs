// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpWinForms()
            : base(nameof(CSharpWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void AddControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.SolutionExplorer.SaveFile(project, "Form1.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"this.SomeButton.Name = ""SomeButton""", actualText);
            ExtendedAssert.Contains(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.cs", saveFile: true);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"this.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = VisualStudioInstance.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.AreEqual(expectedPropertyValue, actualPropertyValue);
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.cs", saveFile: true);
            //  Change the control's text in designer.cs code
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", actualText);
            //  Replace text property with something else
            VisualStudioInstance.Editor.SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            VisualStudioInstance.Editor.SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "Form1.Designer.cs", saveFile: true);
            //  Verify that the control text has changed in the designer
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = VisualStudioInstance.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.AreEqual(expectedPropertyValue, actualPropertyValue);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var designerActualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);", designerActualText);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.cs");
            var codeFileActualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"    public partial class Form1 : Form
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            // Add some control properties and events
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            // Verify that the rename propagated in designer code
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"this.SomeNewButton.Name = ""SomeNewButton"";", actualText);
            ExtendedAssert.Contains(@"this.SomeNewButton.Text = ""ButtonTextValue"";", actualText);
            ExtendedAssert.Contains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);", actualText);
            ExtendedAssert.Contains(@"private System.Windows.Forms.Button SomeNewButton;", actualText);
            // Verify that the old button name goes away
            ExtendedAssert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            VisualStudioInstance.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            //  Verify that the handler is removed
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.DoesNotContain(@"VisualStudioInstance.Editor.SomeButton.Click += new System.EventHandler(VisualStudioInstance.Editor.GooHandler);", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"public System.Windows.Forms.Button SomeButton;", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudioInstance.Editor.AddWinFormButton("SomeButton");
            VisualStudioInstance.Editor.DeleteWinFormButton("SomeButton");
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.DoesNotContain(@"VisualStudioInstance.Editor.SomeButton.Name = ""SomeButton"";", actualText);
            ExtendedAssert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }
    }
}
