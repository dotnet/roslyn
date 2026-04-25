// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class OnTypeFormattingTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/8625")]
    public async Task TypeScript_Semicolon()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForSemanticClassificationAsync("RazorTagHelperElement", ControlledHangMitigatingCancellationToken, count: 2);

        // Change text to refer back to Program class
        await TestServices.Editor.SetTextAsync(@"
<script>
    function F()
    {
        var    x     =   3
    }
</script>
", ControlledHangMitigatingCancellationToken);

        await Task.Delay(1000);

        await TestServices.Editor.PlaceCaretAsync("3", charsOffset: 1, ControlledHangMitigatingCancellationToken);

        // Act
        TestServices.Input.Send(";");

        // Assert
        await TestServices.Editor.WaitForCurrentLineTextAsync("var x = 3;", ControlledHangMitigatingCancellationToken);
    }
}
