// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.WinForms)]
public class CSharpWinForms : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpWinForms()
        : base(nameof(CSharpWinForms), WellKnownProjectTemplates.WinFormsApplication)
    {
    }

    [IdeFact]
    public async Task AddControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"this.SomeButton.Name = ""SomeButton""", actualText);
        Assert.Contains(@"private System.Windows.Forms.Button SomeButton;", actualText);
    }

    [IdeFact]
    public async Task ChangeControlProperty()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseDesignerFileAsync(project, "Form1.cs", saveFile: true, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"this.SomeButton.Text = ""NewButtonText""", actualText);
    }

    [IdeFact]
    public async Task ChangeControlPropertyInCode()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere", HangMitigatingCancellationToken);
        var expectedPropertyValue = "ButtonTextGoesHere";
        var actualPropertyValue = await TestServices.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text", HangMitigatingCancellationToken);
        Assert.Equal(expectedPropertyValue, actualPropertyValue);
        await TestServices.SolutionExplorer.CloseDesignerFileAsync(project, "Form1.cs", saveFile: true, HangMitigatingCancellationToken);
        //  Change the control's text in designer.cs code
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", actualText);
        //  Replace text property with something else
        await TestServices.Editor.SelectTextInCurrentDocumentAsync(@"this.SomeButton.Text = ""ButtonTextGoesHere"";", HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(@"this.SomeButton.Text = ""GibberishText"";", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "Form1.Designer.cs", saveFile: true, HangMitigatingCancellationToken);
        //  Verify that the control text has changed in the designer
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        expectedPropertyValue = "GibberishText";
        actualPropertyValue = await TestServices.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text", HangMitigatingCancellationToken);
        Assert.Equal(expectedPropertyValue, actualPropertyValue);
    }

    [IdeFact]
    public async Task AddClickHandler()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var designerActualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);", designerActualText);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        var codeFileActualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

    [IdeFact]
    public async Task RenameControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        // Add some control properties and events
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler", HangMitigatingCancellationToken);
        // Rename the control
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));
        // Verify that the rename propagated in designer code
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"this.SomeNewButton.Name = ""SomeNewButton"";", actualText);
        Assert.Contains(@"this.SomeNewButton.Text = ""ButtonTextValue"";", actualText);
        Assert.Contains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);", actualText);
        Assert.Contains(@"private System.Windows.Forms.Button SomeNewButton;", actualText);
        // Verify that the old button name goes away
        Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
    }

    [IdeFact]
    public async Task RemoveEventHandler()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler", HangMitigatingCancellationToken);
        //  Remove the event handler
        await TestServices.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));
        //  Verify that the handler is removed
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Click += new System.EventHandler(VisualStudio.Editor.GooHandler);", actualText);
    }

    [IdeFact]
    public async Task ChangeAccessibility()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Modifiers",
            propertyTypeName: "System.CodeDom.MemberAttributes",
            propertyValue: "Public",
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"public System.Windows.Forms.Button SomeButton;", actualText);
    }

    [IdeFact]
    public async Task DeleteControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.DeleteWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.cs", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.DoesNotContain(@"VisualStudio.Editor.SomeButton.Name = ""SomeButton"";", actualText);
        Assert.DoesNotContain(@"private System.Windows.Forms.Button SomeButton;", actualText);
    }
}
