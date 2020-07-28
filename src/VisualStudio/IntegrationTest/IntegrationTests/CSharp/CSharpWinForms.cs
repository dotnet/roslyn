// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.SolutionExplorer.SaveFile(project, "Form1.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"this.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlProperty()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            VisualStudio.SolutionExplorer.CloseDesignerFile(project, "Form1.cs", saveFile: true);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"this.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeControlPropertyInCode()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = VisualStudio.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            VisualStudio.SolutionExplorer.CloseDesignerFile(project, "Form1.cs", saveFile: true);
            //  Change the control's text in designer.cs code
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", actualText);
            //  Replace text property with something else
            VisualStudio.Editor.SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            VisualStudio.Editor.SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            VisualStudio.SolutionExplorer.CloseCodeFile(project, "Form1.Designer.cs", saveFile: true);
            //  Verify that the control text has changed in the designer
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = VisualStudio.Editor.GetWinFormButtonPropertyValue(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void AddClickHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var designerActualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);", designerActualText);
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.cs");
            var codeFileActualText = VisualStudio.Editor.GetText();
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RenameControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            // Add some control properties and events
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            // Verify that the rename propagated in designer code
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"this.SomeNewButton.Name = ""SomeNewButton"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Text = ""ButtonTextValue"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeNewButton;", actualText);
            // Verify that the old button name goes away
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void RemoveEventHandler()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            VisualStudio.Editor.EditWinFormButtonEvent(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            //  Verify that the handler is removed
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Click += new System.EventHandler(VisualStudio.Editor.GooHandler);", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void ChangeAccessibility()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.EditWinFormButtonProperty(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"public System.Windows.Forms.Button SomeButton;", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void DeleteControl()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFileWithDesigner(project, "Form1.cs");
            VisualStudio.Editor.AddWinFormButton("SomeButton");
            VisualStudio.Editor.DeleteWinFormButton("SomeButton");
            VisualStudio.ErrorList.Verify.NoBuildErrors();
            VisualStudio.SolutionExplorer.OpenFile(project, "Form1.Designer.cs");
            var actualText = VisualStudio.Editor.GetText();
            Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Name = ""SomeButton"";", actualText);
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }
    }
}
