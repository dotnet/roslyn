// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class FindAllReferencesTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task FindAllReferences_CSharpInRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("IncrementCount", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 2);

        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            new Action<TableEntry>[]
            {
                reference =>
                {
                    Assert.Equal(expected: "<button class=\"btn btn-primary\" @onclick=\"IncrementCount\">Click me</button>", actual: reference.Code);
                    Assert.Equal(expected: "Counter.razor", Path.GetFileName(reference.DocumentName));
                },
                reference =>
                {
                    Assert.Equal(expected: "private void IncrementCount()", actual: reference.Code);
                    Assert.Equal(expected: "Counter.razor", Path.GetFileName(reference.DocumentName));
                },
            });

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindAllReferences_Component_FromRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Prompt", charsOffset: 0, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 2);

        // Don't care about order, but Assert.Collection does
        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            reference =>
            {
                Assert.Equal("Index.razor", reference.DocumentName);
                Assert.Equal("<SurveyPrompt Title=\"How is Blazor working for you?\" />", reference.Code);
            },
            reference =>
            {
                Assert.Equal("SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal("<div class=\"alert alert-secondary mt-4\">", reference.Code);
            }
        );

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindAllReferences_Component_FromCSharp()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "Program.cs", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync("""
            using BlazorProject.Shared;

            typeof(Surv$$eyPrompt).ToString();
            """, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 2);

        // Don't care about order, but Assert.Collection does
        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            reference =>
            {
                Assert.Equal("Index.razor", reference.DocumentName);
                Assert.Equal("<SurveyPrompt Title=\"How is Blazor working for you?\" />", reference.Code);
            },
            reference =>
            {
                Assert.Equal("Program.cs", reference.DocumentName);
                Assert.Equal("typeof(SurveyPrompt).ToString();", reference.Code);
            },
            reference =>
            {
                Assert.Equal("SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal("<div class=\"alert alert-secondary mt-4\">", reference.Code);
            }
        );

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindAllReferences_ComponentAttribute_FromRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Title=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 3);

        // Don't care about order, but Assert.Collection does
        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            reference =>
            {
                Assert.Equal("Index.razor", reference.DocumentName);
                Assert.Equal("<SurveyPrompt Title=\"How is Blazor working for you?\" />", reference.Code);
            },
            reference =>
            {
                Assert.Equal("SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal("<strong>@Title</strong>", reference.Code);
            },
            reference =>
            {
                Assert.Equal("SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal("public string? Title { get; set; }", reference.Code);
            }
        );

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindAllReferences_ComponentAttribute_FromCSharpInRazor()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.SurveyPromptFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Title", charsOffset: 0, occurrence: 2, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // This is annoying, but if we do the FAR too quickly, we just get results from the current file
        await Task.Delay(500);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 3);

        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            reference =>
            {
                Assert.Equal(expected: "Index.razor", reference.DocumentName);
                Assert.Equal(expected: "<SurveyPrompt Title=\"How is Blazor working for you?\" />", reference.Code);
            },
            reference =>
            {
                Assert.Equal(expected: "SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal(expected: "<strong>@Title</strong>", reference.Code);
            },
            reference =>
            {
                Assert.Equal(expected: "SurveyPrompt.razor", reference.DocumentName);
                Assert.Equal(expected: "public string? Title { get; set; }", reference.Code);
            }
        );

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindAllReferences_ComponentAttribute_FromCSharpInCSharp()
    {
        // Create the file
        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor",
            """
                @MyProperty
                """,
            open: false,
            cancellationToken: ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(RazorProjectConstants.BlazorProjectName,
            "MyComponent.razor.cs",
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

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, "MyComponent.razor.cs", ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MyProperty", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        // This is annoying, but if we do the FAR too quickly, we just get one result from the current file
        await Task.Delay(500);

        // Act
        await TestServices.Editor.InvokeFindAllReferencesAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var results = await TestServices.FindReferencesWindow.WaitForContentsAsync(ControlledHangMitigatingCancellationToken, expected: 3);

        var orderedResults = OrderResults(results);

        Assert.Collection(
            orderedResults,
            reference =>
            {
                Assert.Equal(expected: "@MyProperty", actual: reference.Code);
                Assert.Equal(expected: "MyComponent.razor", Path.GetFileName(reference.DocumentName));
            },
            reference =>
            {
                Assert.Equal(expected: "public string? MyProperty { get; set; }", actual: reference.Code);
                Assert.Equal(expected: "MyComponent.razor.cs", Path.GetFileName(reference.DocumentName));
            },
            reference =>
            {
                Assert.Equal(expected: "<MyComponent MyProperty=\"123\" />", actual: reference.Code);
                Assert.Equal(expected: "MyPage.razor", Path.GetFileName(reference.DocumentName));
            });

        await TestServices.FindReferencesWindow.CloseToolWindowAsync(ControlledHangMitigatingCancellationToken);
    }

    private static IEnumerable<TableEntry> OrderResults(ImmutableArray<ITableEntryHandle2> results)
    {
        var orderedResults = results.Select(r =>
        {
            Assert.True(r.TryGetValue(StandardTableKeyNames.Text, out string code));
            Assert.True(r.TryGetValue(StandardTableKeyNames.DocumentName, out string documentName));

            return new TableEntry(code, Path.GetFileName(documentName));
        }).OrderBy(r => r.DocumentName).ThenBy(r => r.Code).ToArray();

        return orderedResults;
    }

    internal record TableEntry(string Code, string DocumentName);
}
