// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public class CSharpSemanticSnippets : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpSemanticSnippets()
        : base(nameof(CSharpSemanticSnippets))
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(true);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageName, true);
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/71570")]
    [CombinatorialData]
    public async Task ForSnippetObeysIdeVarPreference(bool preferVar)
    {
        await TestServices.Workspace.SetVarPreferenceForBuiltInTypesAsync(preferVar, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""
            class C
            {
                void M()
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("for", HangMitigatingCancellationToken);

        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Editor.WaitForCompletionSessionsAsync(HangMitigatingCancellationToken);

        // Snippet completion item is always 1 position below a keyword
        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);

        var completionItem = await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken);
        Assert.Equal("for", completionItem.DisplayText);

        // Commit snippet
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        // Skip all placeholders
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextEqualsAsync($$"""
            class C
            {
                void M()
                {
                    for ({{(preferVar ? "var" : "int")}} i = 0; i < length; i++)
                    {
                        $$
                    }
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/71570")]
    [CombinatorialData]
    public async Task InlineForSnippetObeysIdeVarPreference(bool preferVar)
    {
        await TestServices.Workspace.SetVarPreferenceForBuiltInTypesAsync(preferVar, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""
            class C
            {
                void M(int intVar)
                {
                    intVar.$$
                }
            }
            """, HangMitigatingCancellationToken);

        // Inline statement snippets are currently can only be triggered explicitly
        // TODO: Remove explicit invocation when https://github.com/dotnet/roslyn/issues/75072 is fixed
        await TestServices.Editor.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("for", HangMitigatingCancellationToken);

        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Editor.WaitForCompletionSessionsAsync(HangMitigatingCancellationToken);

        var completionItem = await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken);
        Assert.Equal("for", completionItem.DisplayText);

        // Commit snippet
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        // Skip all placeholders
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextEqualsAsync($$"""
            class C
            {
                void M(int intVar)
                {
                    for ({{(preferVar ? "var" : "int")}} i = 0; i < intVar; i++)
                    {
                        $$
                    }
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/71570")]
    [CombinatorialData]
    public async Task InlineReversedForSnippetObeysIdeVarPreference(bool preferVar)
    {
        await TestServices.Workspace.SetVarPreferenceForBuiltInTypesAsync(preferVar, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""
            class C
            {
                void M(int intVar)
                {
                    intVar.$$
                }
            }
            """, HangMitigatingCancellationToken);

        // Inline statement snippets are currently can only be triggered explicitly
        // TODO: Remove explicit invocation when https://github.com/dotnet/roslyn/issues/75072 is fixed
        await TestServices.Editor.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("forr", HangMitigatingCancellationToken);

        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Editor.WaitForCompletionSessionsAsync(HangMitigatingCancellationToken);

        var completionItem = await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken);
        Assert.Equal("forr", completionItem.DisplayText);

        // Commit snippet
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        // Skip all placeholders
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextEqualsAsync($$"""
            class C
            {
                void M(int intVar)
                {
                    for ({{(preferVar ? "var" : "int")}} i = intVar - 1; i >= 0; i--)
                    {
                        $$
                    }
                }
            }
            """, HangMitigatingCancellationToken);
    }
}
