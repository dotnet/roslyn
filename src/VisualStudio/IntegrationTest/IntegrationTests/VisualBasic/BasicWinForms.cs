// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicWinForms : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicWinForms()
            : base(nameof(BasicWinForms), WellKnownProjectTemplates.WinFormsApplication)
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task TestMyIntelliSenseAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.vb");
            await SetUpEditorAsync(@"Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        $$
    End Sub
End Class");
            await VisualStudio.Editor.SendKeysAsync("My.");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Application");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Computer");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Forms");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("MySettings");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Resources");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Settings");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("User");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("WebServices");
            await VisualStudio.Editor.Verify.CompletionItemsDoNotExistAsync("Equals");
            await VisualStudio.Editor.Verify.CompletionItemsDoNotExistAsync("MyApplication");

            await VisualStudio.Editor.SendKeysAsync("Forms.");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Form1");
            await VisualStudio.Editor.Verify.CompletionItemsDoNotExistAsync("Equals");
            await VisualStudio.Editor.Verify.CompletionItemsDoNotExistAsync("GetHashCode");
            await VisualStudio.Editor.Verify.CompletionItemsDoNotExistAsync("ToString");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task AddControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.vb", saveFile: true);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.Contains(@"Friend WithEvents SomeButton As Button", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeControlPropertyAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.vb", saveFile: true);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Me.SomeButton.Text = ""NewButtonText""", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeControlPropertyInCodeAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            var expectedPropertyValue = "ButtonTextGoesHere";
            var actualPropertyValue = await VisualStudio.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.vb", saveFile: true);
            //  Change the control's text in designer.vb code
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Me.SomeButton.Text = ""ButtonTextGoesHere""", actualText);
            //  Replace text property with something else
            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync(@"Me.SomeButton.Text = ""ButtonTextGoesHere""");
            await VisualStudio.Editor.SendKeysAsync(@"Me.SomeButton.Text = ""GibberishText""");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "Form1.Designer.vb", saveFile: true);
            //  Verify that the control text has changed in the designer
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            expectedPropertyValue = "GibberishText";
            actualPropertyValue = await VisualStudio.Editor.GetWinFormButtonPropertyValueAsync(buttonName: "SomeButton", propertyName: "Text");
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task AddClickHandlerAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Private Sub ExecuteWhenButtonClicked(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
            await VisualStudio.SolutionExplorer.SaveFile(ProjectName, "Form1.vb");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task RenameControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            // Add some control properties and events
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            // Verify that the rename propagated in designer code
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var formDesignerActualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Me.SomeNewButton.Name = ""SomeNewButton""", formDesignerActualText);
            Assert.Contains(@"Me.SomeNewButton.Text = ""ButtonTextValue""", formDesignerActualText);
            Assert.Contains(@"Friend WithEvents SomeNewButton As Button", formDesignerActualText);
            // Verify that the old button name goes away
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.vb");
            var formActualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles SomeNewButton.Click", formActualText);
            // Rename control from the code behind file (bug 784595)
            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync(@"SomeNewButton");
            await VisualStudio.VisualStudio.ExecuteCommandAsync("Refactor.Rename");
            await VisualStudio.Editor.SendKeysAsync("AnotherNewButton", VirtualKey.Enter);
            formActualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Private Sub SomeButtonHandler(sender As Object, e As EventArgs) Handles AnotherNewButton.Click", formActualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task RemoveEventHandlerAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "GooHandler");
            //  Remove the event handler
            await VisualStudio.Editor.EditWinFormButtonEventAsync(buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            //  Verify that the handler is removed
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.DoesNotContain(@"Private Sub GooHandler(sender As Object, e As EventArgs) Handles SomeButton.Click", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task ChangeAccessibilityAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.EditWinFormButtonPropertyAsync(
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Public WithEvents SomeButton As Button", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public async Task DeleteControlAsync()
        {
            await VisualStudio.SolutionExplorer.OpenFileWithDesignerAsync(ProjectName, "Form1.vb");
            await VisualStudio.Editor.AddWinFormButtonAsync("SomeButton");
            await VisualStudio.Editor.DeleteWinFormButtonAsync("SomeButton");
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Form1.Designer.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.DoesNotContain(@"Me.SomeButton.Name = ""SomeButton""", actualText);
            Assert.DoesNotContain(@"Friend WithEvents SomeButton As Button", actualText);
        }
    }
}
