// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class SynchronizationTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/8114")]
    public async Task CSharpComponentBacking_UpdatesComponents()
    {
        // Create the file
        const string MyComponentRazorPath = "MyComponent.razor";
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentRazorPath,
            """
                @MyProperty
                """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);
        await WaitForComponentInitializeAsync(ControlledHangMitigatingCancellationToken);
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
        await WaitForComponentInitializeAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, MyComponentCSharpPath, saveFile: true, ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
                <MyComponent MyProperty="123" />
                """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        // Sometimes hang waiting for classification.
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/8114")]
    public async Task BlindDocumentCreation_InitializesComponents()
    {
        // Create the file
        const string MyComponentRazorPath = "MyComponent.razor";
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentRazorPath,
            """
                @MyProperty
                """,
            open: false,
            cancellationToken: ControlledHangMitigatingCancellationToken);

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
            open: false,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
                <MyComponent MyProperty="123" />
                """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
    }

    private async Task WaitForComponentInitializeAsync(CancellationToken cancellationToken)
    {
        // Wait for it to initialize by building
        await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(cancellationToken);
    }
}
