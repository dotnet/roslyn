// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// Pins the trigger character sets. These flow straight into the LSP registration, so widening one
/// changes how often the editor asks for completions across every Razor document, and narrowing one
/// silently stops a feature from ever being offered on typing.
/// </summary>
public class CompletionTriggerAndCommitCharactersTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static CompletionTriggerAndCommitCharacters Create(bool supportsVisualStudioExtensions)
        => new(new TestClientCapabilitiesService(
            new VSInternalClientCapabilities() { SupportsVisualStudioExtensions = supportsVisualStudioExtensions }));

    [Theory]
    [InlineData("@")]
    [InlineData("<")]
    [InlineData(":")]
    [InlineData(" ")]
    // '~' and '/' let the asset path list appear as '~/' is typed inside an opted-in attribute.
    [InlineData("~")]
    [InlineData("/")]
    public void RazorTriggerCharacters(string character)
    {
        Assert.True(Create(supportsVisualStudioExtensions: true).IsValidRazorTrigger(TypingContext(character)));
    }

    [Theory]
    [InlineData("!")]
    [InlineData(".")]
    [InlineData("#")]
    [InlineData("(")]
    [InlineData("\"")]
    public void NonRazorTriggerCharacters(string character)
    {
        Assert.False(Create(supportsVisualStudioExtensions: true).IsValidRazorTrigger(TypingContext(character)));
    }

    [Fact]
    public void ExplicitInvocationIsAlwaysAValidRazorTrigger()
    {
        var context = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Explicit,
            TriggerKind = CompletionTriggerKind.Invoked
        };

        Assert.True(Create(supportsVisualStudioExtensions: true).IsValidRazorTrigger(context));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllTriggerCharactersAreUnchangedByTheRazorSet(bool supportsVisualStudioExtensions)
    {
        // '~' and '/' were already registered through the C# set, so adding them to the Razor set
        // must not change what the editor is told to trigger on.
        var all = Create(supportsVisualStudioExtensions).AllTriggerCharacters;

        Assert.Contains("~", all);
        Assert.Contains("/", all);
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HtmlAndCSharpTriggersAreUnaffected(bool supportsVisualStudioExtensions)
    {
        var triggers = Create(supportsVisualStudioExtensions);

        // '/' reaching the Razor providers must not also make it an HTML trigger in VS Code, where
        // the HTML set is deliberately small.
        Assert.Equal(supportsVisualStudioExtensions, triggers.IsHtmlTriggerCharacter("/"));
        Assert.False(triggers.IsHtmlTriggerCharacter("~"));

        Assert.True(triggers.IsCSharpTriggerCharacter("/"));
        Assert.True(triggers.IsCSharpTriggerCharacter("~"));
    }

    private static VSInternalCompletionContext TypingContext(string triggerCharacter)
        => new()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerCharacter = triggerCharacter,
            TriggerKind = CompletionTriggerKind.TriggerCharacter
        };
}
