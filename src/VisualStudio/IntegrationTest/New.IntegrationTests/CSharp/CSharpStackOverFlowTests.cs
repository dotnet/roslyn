// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpStackOverFlowTests : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpStackOverFlowTests()
        : base(nameof(CSharpStackOverFlowTests))
    {
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63349")]
    public async Task TestDevenvDoNotCrash()
    {
        var sampleCode = await GetSampleCodeAsync();
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);
        await SetUpEditorAsync(sampleCode, HangMitigatingCancellationToken);

        // Try to compute the light bulb. The content of the light bulb is not important because here we want to make sure
        // the special crafted code don't crash VS.
        await TestServices.Editor.ShowLightBulbAsync(HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63349")]
    public async Task TestSyntaxIndex()
    {
        var sampleCode = await GetSampleCodeAsync();
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);
        await SetUpEditorAsync(sampleCode, HangMitigatingCancellationToken);

        // Call FAR to create syntax index. The goal is to verify we don't hit StackOverFlow during the creation.
        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
        var contents = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
        Assert.Equal(18, contents.Length);

        // Make sure all the references are found.
        foreach (var content in contents)
        {
            content.TryGetValue(StandardTableKeyNames.Text, out string code);
            Assert.Contains("Tree82", code);
        }
    }

    private static async Task<string> GetSampleCodeAsync()
    {
        var resourceStream = typeof(CSharpStackOverFlowTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.NewIntegrationTests.Resources.LongClass.txt");
        using var reader = new StreamReader(resourceStream);
        // This is a special crafted code which many Roslyn functions won't work.
        var sampleCode = await reader.ReadToEndAsync();
        return sampleCode;
    }
}
