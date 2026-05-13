// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class OnEnterRulesTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task OnEnterRules_BetweenStartAndEnd()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class='classifier'></button>
");
        // Act
        await TestServices.Editor.PlaceCaretAsync(">", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");
        TestServices.Input.Send("A");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button class='classifier'>
    A
</button>
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_AtEndOfTag()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button stuff></button>
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("</button>", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");
        TestServices.Input.Send("A");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button stuff></button>
A
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_BeforeAttribute()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class='thing' stuff></button>
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("button", charsOffset: 1, ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button
    class='thing' stuff></button>
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_EmptyAttribute()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class='someclass' @onclick='thing' stuff ></button>
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("stuff", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button class='someclass' @onclick='thing' stuff
    ></button>
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_DoubleQuote()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class=""someclass"" @onclick='thing' stuff ></button>
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("button", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button
    class=""someclass"" @onclick='thing' stuff ></button>
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_DirectiveAttribute()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class='someclass' @onclick='thing' stuff ></button>
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("@onclick='thing'", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");
        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button class='someclass' @onclick='thing'
    stuff ></button>
", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OnEnterRules_UnfinishedTag()
    {
        // Arrange
        await PrepareDocumentAsync(@"
<button class='thing' stuff
");

        // Act
        await TestServices.Editor.PlaceCaretAsync("button", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");

        // Assert
        await TestServices.Editor.VerifyTextContainsAsync(@"
<button
    class='thing' stuff
", ControlledHangMitigatingCancellationToken);
    }

    private async Task PrepareDocumentAsync(string content)
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(content, ControlledHangMitigatingCancellationToken);
    }
}

