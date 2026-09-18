// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class DiagnosticTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact/*(Skip = "https://github.com/dotnet/roslyn/issues/85699")*/]
    public async Task Diagnostics_ShowErrors_Razor()
    {
        // Arrange
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: open Counter.razor");
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: set Razor errors text");
        await TestServices.Editor.SetTextAsync(@"
<h1>
<PageTitle>

@code{
    public void Function(){
        return """"
    }
}
", ControlledHangMitigatingCancellationToken);

        // Act
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: wait for Counter.razor errors");
        var errors = await TestServices.ErrorList.WaitForErrorsAsync("Counter.razor", expectedCount: 3, ControlledHangMitigatingCancellationToken);

        // Assert
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: assert Razor diagnostics");
        Assert.Collection(errors,
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: assert unclosed h1 diagnostic");
                AssertEx.EqualOrDiff("Counter.razor(2, 1): error RZ9980: Unclosed tag 'h1' with no matching end tag.", error);
            },
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: assert malformed PageTitle diagnostic");
                AssertEx.EqualOrDiff("Counter.razor(3, 2): error RZ1034: Found a malformed 'PageTitle' tag helper. Tag helpers must have a start and end tag or be self closing.", error);
            },
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: assert missing semicolon diagnostic");
                AssertEx.EqualOrDiff("Counter.razor(7, 18): error CS1002: ; expected", error);
            },
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Razor: assert void return diagnostic");
                AssertEx.EqualOrDiff("Counter.razor(7, 9): error CS0127: Since 'Counter.Function()' returns void, a return keyword must not be followed by an object expression", error);
            });
    }

    [IdeFact/*(Skip = "https://github.com/dotnet/roslyn/issues/85699")*/]
    public async Task Diagnostics_ShowErrors_Html()
    {
        // Arrange
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: open Error.cshtml");
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: set Html errors text");
        await TestServices.Editor.SetTextAsync(@"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang=""en"">
<head>
</head>

<body>
    <p
</body>
</html>

", ControlledHangMitigatingCancellationToken);

        // Act
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: wait for Error.cshtml errors");
        var errors = await TestServices.ErrorList.WaitForErrorsAsync("Error.cshtml", expectedCount: 1, ControlledHangMitigatingCancellationToken);

        // Assert
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: assert Html diagnostics");
        Assert.Collection(errors,
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: assert missing angle bracket diagnostic");
                AssertEx.EqualOrDiff("Error.cshtml(10, 6): warning HTML0001: Element start tag is missing closing angle bracket.", error);
            },
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_Html: assert unnecessary addTagHelper diagnostic");
                AssertEx.EqualOrDiff("Error.cshtml(2, 1): warning RZ0005: @addTagHelper directive is unnecessary.", error);
            });
    }

    [IdeFact/*(Skip = "https://github.com/dotnet/roslyn/issues/85699")*/]
    public async Task Diagnostics_ShowErrors_CSharp()
    {
        // Arrange
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp: open Error.cshtml");
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp: set CSharp errors text");
        await TestServices.Editor.SetTextAsync(@"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang=""en"">
<head>
</head>

<body>
    <input asp-for=""Test"" />
</body>
</html>

", ControlledHangMitigatingCancellationToken);

        // Act
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp: wait for Error.cshtml CSharp errors");
        var errors = await TestServices.ErrorList.WaitForErrorsAsync("Error.cshtml", expectedCount: 1, ControlledHangMitigatingCancellationToken);

        // Assert
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp: assert CSharp diagnostics");
        Assert.Collection(errors,
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp: assert dynamic expression tree diagnostic");
                AssertEx.EqualOrDiff("Error.cshtml(10, 21): error CS1963: An expression tree may not contain a dynamic operation", error);
            });
    }

    [IdeFact/*(Skip = "https://github.com/dotnet/roslyn/issues/85699")*/]
    public async Task Diagnostics_ShowErrors_CSharp_NoDocType()
    {
        // Why this test, when we have the above test, and they seem so similar, and we also have Diagnostics_ShowErrors_CSharpAndHtml you ask? Well I'll tell you!
        //
        // In the above test, with a doctype, the Html LSP server returns a diagnostic result containing one item, which has an empty array of actual diagnostics within it.
        // In Diagnostics_ShowErrors_CSharpAndHtml, the Html LSP server returns a diagnostic result containing one item, which has one actual diagnostic inside it.
        // In this test, with no doctype, the Html LSP server returns a diagnostic result containing one item, which has null for the actual diagnostics within it.
        //
        // The Visual Studio error system (squiggles and error list) didn't like that last case, so given that quirkiness and the slight differences in results, there
        // is value in having a range of tests to make sure nothing regresses.

        // Arrange
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp_NoDocType: open Error.cshtml");
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp_NoDocType: set no-doctype CSharp errors text");
        await TestServices.Editor.SetTextAsync(@"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<html lang=""en"">
<head>
</head>

<body>
    <input asp-for=""Test"" />
</body>
</html>

", ControlledHangMitigatingCancellationToken);

        // Act
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp_NoDocType: wait for Error.cshtml no-doctype CSharp errors");
        var errors = await TestServices.ErrorList.WaitForErrorsAsync("Error.cshtml", expectedCount: 1, ControlledHangMitigatingCancellationToken);

        // Assert
        testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp_NoDocType: assert no-doctype CSharp diagnostics");
        Assert.Collection(errors,
            (error) =>
            {
                testOutputHelper.WriteLine("Diagnostics_ShowErrors_CSharp_NoDocType: assert dynamic expression tree diagnostic");
                AssertEx.EqualOrDiff("Error.cshtml(9, 21): error CS1963: An expression tree may not contain a dynamic operation", error);
            });
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/12372")]
    public async Task Diagnostics_ShowErrors_CSharpAndHtml()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync(@"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang=""en"">
<head>
</head>

<body>
    <input asp-for=""Test"" />
    <li>
</body>
</html>

", ControlledHangMitigatingCancellationToken);

        // Act
        var errors = await TestServices.ErrorList.WaitForErrorsAsync("Error.cshtml", expectedCount: 2, ControlledHangMitigatingCancellationToken);

        // Assert
        Assert.Collection(errors,
            (error) =>
            {
                AssertEx.EqualOrDiff("Error.cshtml(10, 21): error CS1963: An expression tree may not contain a dynamic operation", error);
            },
            (error) =>
            {
                AssertEx.EqualOrDiff("Error.cshtml(11, 6): warning HTML0204: Element 'li' cannot be nested inside element 'body'.", error);
            });
    }
}
