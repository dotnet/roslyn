// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpWinForms : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpWinForms()
            : base(nameof(CSharpWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task AddControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.SolutionExplorer.SaveFileAsync(ProjectName, "Form1.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"this.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeControlPropertyAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.cs", saveFile: true);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"this.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeControlPropertyInCodeAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = await VisualStudio.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.cs", saveFile: true);
            //  Change the control's text in designer.cs code
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", actualText);
            //  Replace text property with something else
            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            await VisualStudio.Editor.SendKeysAsync(@"this.SomeButton.Text = ""GibberishText"";");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.Designer.cs",  saveFile: true);
            //  Verify that the control text has changed in the designer
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = await VisualStudio.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task AddClickHandlerAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var designerActualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);", designerActualText);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.cs");
            var codeFileActualText = await VisualStudio.Editor.GetTextAsync();
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task RenameControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            // Add some control properties and events
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            // Verify that the rename propagated in designer code
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"this.SomeNewButton.Name = ""SomeNewButton"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Text = ""ButtonTextValue"";", actualText);
            Assert.Contains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);", actualText);
            Assert.Contains(@"private System.Windows.Forms.Button SomeNewButton;", actualText);
            // Verify that the old button name goes away
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task RemoveEventHandlerAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            //  Verify that the handler is removed
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Click += new System.EventHandler(VisualStudio.Editor.GooHandler);", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeAccessibilityAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"public System.Windows.Forms.Button SomeButton;", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task DeleteControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.cs");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.DeleteWinFormButtonAsync("SomeButton");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.cs");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Name = ""SomeButton"";", actualText);
            Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
        }
    }
}
