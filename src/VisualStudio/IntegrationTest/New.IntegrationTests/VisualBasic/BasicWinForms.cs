// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.WinForms)]
public class BasicWinForms : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicWinForms()
        : base(nameof(BasicWinForms), WellKnownProjectTemplates.WinFormsApplication)
    {
    }

    [IdeFact]
    public async Task TestMyIntelliSense()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await SetUpEditorAsync("""
            Public Class Form1
                Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
                    $$
                End Sub
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("My.", HangMitigatingCancellationToken);

        var completionItems = (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        Assert.Contains("Application", completionItems);
        Assert.Contains("Computer", completionItems);
        Assert.Contains("Forms", completionItems);
        Assert.Contains("MySettings", completionItems);
        Assert.Contains("Resources", completionItems);
        Assert.Contains("Settings", completionItems);
        Assert.Contains("User", completionItems);
        Assert.Contains("WebServices", completionItems);
        Assert.DoesNotContain("Equals", completionItems);
        Assert.DoesNotContain("MyApplication", completionItems);

        await TestServices.Input.SendAsync("Forms.", HangMitigatingCancellationToken);
        completionItems = (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        Assert.Contains("Form1", completionItems);
        Assert.DoesNotContain("Equals", completionItems);
        Assert.DoesNotContain("GetHashCode", completionItems);
        Assert.DoesNotContain("ToString", completionItems);
    }

    [IdeFact]
    public async Task AddControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseDesignerFileAsync(project, "Form1.vb", saveFile: true, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            Me.SomeButton.Name = "SomeButton"
            """, actualText);
        Assert.Contains(@"Friend WithEvents SomeButton As Button", actualText);
    }

    [IdeFact]
    public async Task ChangeControlProperty()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseDesignerFileAsync(project, "Form1.vb", saveFile: true, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            Me.SomeButton.Text = "NewButtonText"
            """, actualText);
    }

    [IdeFact]
    public async Task ChangeControlPropertyInCode()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere", HangMitigatingCancellationToken);
        var expectedPropertyValue = "ButtonTextGoesHere";
        var actualPropertyValue = await TestServices.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text", HangMitigatingCancellationToken);
        Assert.Equal(expectedPropertyValue, actualPropertyValue);
        await TestServices.SolutionExplorer.CloseDesignerFileAsync(project, "Form1.vb", saveFile: true, HangMitigatingCancellationToken);
        //  Change the control's text in designer.vb code
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            Me.SomeButton.Text = "ButtonTextGoesHere"
            """, actualText);
        //  Replace text property with something else
        await TestServices.Editor.SelectTextInCurrentDocumentAsync("""
            Me.SomeButton.Text = "ButtonTextGoesHere"
            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("""
            Me.SomeButton.Text = "GibberishText"
            """, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "Form1.Designer.vb", saveFile: true, HangMitigatingCancellationToken);
        //  Verify that the control text has changed in the designer
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        expectedPropertyValue = "GibberishText";
        actualPropertyValue = await TestServices.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text", HangMitigatingCancellationToken);
        Assert.Equal(expectedPropertyValue, actualPropertyValue);
    }

    [IdeFact]
    public async Task AddClickHandler()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task RenameControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
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
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var formDesignerActualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            Me.SomeNewButton.Name = "SomeNewButton"
            """, formDesignerActualText);
        Assert.Contains("""
            Me.SomeNewButton.Text = "ButtonTextValue"
            """, formDesignerActualText);
        Assert.Contains(@"Friend WithEvents SomeNewButton As Button", formDesignerActualText);
        // Verify that the old button name goes away
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        var formActualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click", formActualText);
        // Rename control from the code behind file (bug 784595)
        await TestServices.Editor.SelectTextInCurrentDocumentAsync(@"SomeNewButton", HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Refactor.Rename, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(["AnotherNewButton", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        formActualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click", formActualText);
    }

    [IdeFact]
    public async Task RemoveEventHandler()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
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
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.DoesNotContain(@"Private Sub GooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
    }

    [IdeFact]
    public async Task ChangeAccessibility()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.resx", HangMitigatingCancellationToken);
        await TestServices.Editor.EditWinFormButtonPropertyAsync(
            buttonName: "SomeButton",
            propertyName: "Modifiers",
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
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Public WithEvents SomeButton As Button", actualText);
    }

    [IdeFact]
    public async Task DeleteControl()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileWithDesignerAsync(project, "Form1.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.AddWinFormButtonAsync("SomeButton", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveFileAsync(project, "Form1.vb", HangMitigatingCancellationToken);
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
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Form1.Designer.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.DoesNotContain("""
            Me.SomeButton.Name = "SomeButton"
            """, actualText);
        Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
    }
}
