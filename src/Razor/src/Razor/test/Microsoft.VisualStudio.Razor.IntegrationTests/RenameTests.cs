// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class RenameTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task Rename_ComponentAttribute_FromRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Title=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await Task.Delay(500);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        // The rename operation causes SurveyPrompt.razor to be opened
        await TestServices.Editor.WaitForActiveWindowByFileAsync("SurveyPrompt.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public string? ZooperDooper { get; set; }", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("@ZooperDooper", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("<SurveyPrompt ZooperDooper=", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentAttribute_FromCSharpInRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.SurveyPromptFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Title", charsOffset: 0, occurrence: 2, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        await Task.Delay(1500);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        // The rename operation causes Index.razor to be opened
        await TestServices.Editor.WaitForActiveWindowByFileAsync("Index.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("<SurveyPrompt ZooperDooper=", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.SurveyPromptFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public string? ZooperDooper { get; set; }", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("@ZooperDooper", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/10820")]
    public async Task Rename_ComponentAttribute_FromCSharpInCSharp()
    {
        // Create the file
        const string MyComponentRazorPath = "MyComponent.razor";
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentRazorPath,
            """
                @MyProperty
                """,
            open: true, // We create these open and then close them to try to force Component initialization while testing edits of closed documents
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, MyComponentRazorPath, saveFile: true, ControlledHangMitigatingCancellationToken);

        const string MyComponentCSharpPath = "MyComponent.razor.cs";
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentCSharpPath,
            """
                namespace BlazorProject;

                public partial class MyComponent
                {
                    [Microsoft.AspNetCore.Components.ParameterAttribute]
                    public string? MyProperty { get; set; }
                }
            
                """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, MyComponentCSharpPath, saveFile: true, ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
                <MyComponent MyProperty="123" />
                """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.razor.cs", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty", charsOffset: 0, occurrence: 2, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        await Task.Delay(500);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        // The rename operation causes MyPage.razor to be opened
        await TestServices.Editor.WaitForActiveWindowByFileAsync("MyComponent.razor.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public string? ZooperDooper { get; set; }", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("@ZooperDooper", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyPage.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("<MyComponent ZooperDooper=", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentAttribute_BoundAttribute()
    {
        // Create the files
        const string MyComponentPath = "MyComponent.razor";
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentPath,
            """
            <div></div>

            @code
            {
                [Parameter]
                public string? Value { get; set; }

                [Parameter]
                public EventCallback<string?> ValueChanged { get; set; }
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, MyComponentPath, saveFile: true, ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <MyComponent @bind-Value="value"></MyComponent>

            @code{
                string? value = "";
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Value=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await Task.Delay(500);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        // The rename operation causes MyPage.razor to be opened
        await TestServices.Editor.WaitForActiveWindowByFileAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public string? ZooperDooper { get; set; }", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyPage.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("<MyComponent @bind-ZooperDooper=\"value\"></MyComponent>", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentDefinedInCSharp_FromCSharp()
    {
        // Create the files
        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            public class MyComp$$onent : ComponentBase
            {
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <MyComponent></MyComponent>
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.cs", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // For some reason, this particular rename exercise is particularly flaky so we have a few hail marys to recite
        await TestServices.Shell.WaitForOperationProgressAsync(ControlledHangMitigatingCancellationToken);
        await WaitForRoslynRenameReadyAsync(ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);

        // Even though we waited for the command to be ready and available, it can still be a bit slow to come up
        await Task.Delay(500);

        TestServices.Input.Send("ZooperDooper{ENTER}");

        await TestServices.Editor.WaitForCurrentLineTextAsync("public class ZooperDooper : ComponentBase", ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyPage.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForTextContainsAsync("<ZooperDooper></ZooperDooper>", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentDefinedInCSharp_FromRazor()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            namespace My.Fancy.Namespace;

            public class MyComponent : ComponentBase
            {
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            @using My.Fancy.Namespace

            <MyComp$$onent></MyComponent>
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        await TestServices.Editor.WaitForCurrentLineTextAsync("<ZooperDooper></ZooperDooper>", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public class ZooperDooper : ComponentBase", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentDefinedInCSharp_FromRazor_GlobalNamespace()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            public class MyComponent : ComponentBase
            {
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <MyComp$$onent></MyComponent>
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        await TestServices.Editor.WaitForCurrentLineTextAsync("<ZooperDooper></ZooperDooper>", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public class ZooperDooper : ComponentBase", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Rename_ComponentDefinedInCSharp_FromCSharpInRazor()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            public class MyComponent : ComponentBase
            {
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <MyComponent></MyComponent>

            @nameof(MyComp$$onent)
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeRenameAsync(ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("ZooperDooper{ENTER}");

        // Assert
        await TestServices.Editor.WaitForTextContainsAsync("<ZooperDooper></ZooperDooper>", ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyTextContainsAsync("public class ZooperDooper : ComponentBase", ControlledHangMitigatingCancellationToken);
    }

    private async Task WaitForRoslynRenameReadyAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var buffer = view.GetBufferContainingCaret();

        var commandArgs = new RenameCommandArgs(view, buffer);

        // We don't have EA from this project, so we have to resort to reflection. Fortunately it's pretty simple
        var roslynHandler = Type.GetType("Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.AbstractRenameCommandHandler, Microsoft.CodeAnalysis.EditorFeatures");
        var canRename = roslynHandler.GetMethod("CanRename", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        await Helper.RetryAsync(ct =>
        {
            return Task.FromResult((bool)canRename.Invoke(null, [commandArgs]));
        }, TimeSpan.FromMilliseconds(100), cancellationToken);
    }
}
