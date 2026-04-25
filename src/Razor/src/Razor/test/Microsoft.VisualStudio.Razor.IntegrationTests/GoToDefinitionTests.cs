// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class GoToDefinitionTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task GoToDefinition_MethodInSameFile()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("IncrementCount", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForCurrentLineTextAsync("private void IncrementCount()", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_CSharpClass()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        // Change text to refer back to Program class
        await TestServices.Editor.SetTextAsync(@"<SurveyPrompt Title=""@nameof(Program)", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("Program.cs", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, "Program.cs", saveFile: false, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_Component()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("SurveyPrompt", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("SurveyPrompt.razor", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_InOtherRazorFile()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Title=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("SurveyPrompt.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string? Title { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_InSameRazorFile()
    {
        // Create the file
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
            """
            <MyComponent MyProperty="123" />

            @code {
                [Microsoft.AspNetCore.Components.ParameterAttribute]
                public string? MyProperty { get; set; }
            }

            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string? MyProperty { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_Minimized()
    {
        // Create the file
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
            """
            <MyComponent MyProperty />

            @code {
                [Microsoft.AspNetCore.Components.ParameterAttribute]
                public bool MyProperty { get; set; }
            }

            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public bool MyProperty { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_InCSharpFile()
    {
        // Create the files
        const string MyComponentPath = "MyComponent.cs";
        const string MyPagePath = "MyPage.razor";

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyComponentPath,
            """
            using Microsoft.AspNetCore.Components;

            namespace BlazorProject;

            public class MyComponent : ComponentBase
            {
                [Parameter] public string? MyProperty { get; set; }
            }
            """,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            MyPagePath,
            """
            <MyComponent MyProperty="123" />
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync(MyComponentPath, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("[Parameter] public string? MyProperty { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_InReferencedAssembly()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.NavMenuFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Match=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowByFileAsync("NavLink.cs", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_GenericComponent()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            namespace BlazorProject
            {
                public class MyComponent<TItem> : ComponentBase
                {
                    [Parameter] public TItem Item { get; set; }
                }
            }
            """,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <MyComponent TItem=string It$$em="@("hi")"/>
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("[Parameter] public TItem Item { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_CascadingGenericComponentWithConstraints()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.cs",
            """
            using Microsoft.AspNetCore.Components;

            namespace BlazorProject
            {
                [CascadingTypeParameter(nameof(TItem))]
                public class Grid<TItem> : ComponentBase
                {
                    [Parameter] public RenderFragment ColumnsTemplate { get; set; }
                }

                public abstract partial class BaseColumn<TItem> : ComponentBase where TItem : class
                {
                    [CascadingParameter]
                    internal Grid<TItem> Grid { get; set; }
                }

                public class Column<TItem> : BaseColumn<TItem>, IGridFieldColumn<TItem> where TItem : class
                {
                    [Parameter]
                    public string FieldName { get; set; }
                }

                internal interface IGridFieldColumn<TItem> where TItem : class
                {
                }

                public class WeatherForecast { }
            }
            """,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        var position = await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyPage.razor",
            """
            <Grid TItem="WeatherForecast" Items="@(Array.Empty<WeatherForecast>())">
                <ColumnsTemplate>
                    <Column Title="Date" Fie$$ldName="Date" Format="d" Width="10rem" />
                </ColumnsTemplate>
            </Grid>
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.cs", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string FieldName { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_BoundAttribute()
    {
        // Create the files
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
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
            cancellationToken: ControlledHangMitigatingCancellationToken);

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

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string? Value { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentAttribute_WriteOnlyProperty()
    {
        // Create the file
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
            """
            <MyComponent MyProperty="123" />

            @code {
                [Microsoft.AspNetCore.Components.ParameterAttribute]
                public string? MyProperty { set { } }
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string? MyProperty { set { } }", ControlledHangMitigatingCancellationToken);
    }

    [IdeTheory]
    [InlineData("MyProperty:get")]
    [InlineData("MyProperty:set")]
    [InlineData("MyProperty:after")]
    public async Task GoToDefinition_ComponentAttribute_BindSet(string attribute)
    {
        // Create the file
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
            """
            <MyComponent @bind-MyProperty:get="value" @bind-MyProperty:set="(value) => { text = value; }" @bind-MyProperty:after="After" />
            @code {
                private string value = "";
                [Microsoft.AspNetCore.Components.ParameterAttribute]
                public string? MyProperty { get; set; }
                public void After()
                {
                }
            }
            """,
            open: true,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(attribute, charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("MyComponent.razor", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForCurrentLineTextAsync("public string? MyProperty { get; set; }", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentFromCSharp()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "Program.cs", ControlledHangMitigatingCancellationToken);

        // Change text to refer back to Program class
        await TestServices.Editor.SetTextAsync("""
            using BlazorProject.Shared;

            typeof(Surv$$eyPrompt).ToString();
            """, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("SurveyPrompt.razor", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, "Program.cs", saveFile: false, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task GoToDefinition_ComponentFromCSharpInRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        // Change text to refer back to Program class
        await TestServices.Editor.SetTextAsync("@nameof(Surv$$eyPrompt)", ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeGoToDefinitionAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        await TestServices.Editor.WaitForActiveWindowAsync("SurveyPrompt.razor", ControlledHangMitigatingCancellationToken);
    }
}
