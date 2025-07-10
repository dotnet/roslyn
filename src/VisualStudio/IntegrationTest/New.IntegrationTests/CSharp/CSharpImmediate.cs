// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpImmediate : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpImmediate()
        : base()
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CSharpImmediate), HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task DumpLocalVariableValue()
    {
        await TestServices.Editor.SetTextAsync("""

            class Program
            {
                static void Main(string[] args)
                {
                    int n1Var = 42;
                    int n2Var = 43;
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, "Program.cs", "}", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.ImmediateWindow.ShowAsync(HangMitigatingCancellationToken);
        var existingText = await TestServices.ImmediateWindow.GetTextAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync("?", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["n1", VirtualKeyCode.TAB, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        var immediateText = await TestServices.ImmediateWindow.GetTextAsync(HangMitigatingCancellationToken);
        // Skip checking the EE result "42" (see https://github.com/dotnet/roslyn/issues/75456), without
        // skipping the test completely (see https://github.com/dotnet/roslyn/issues/75478).
        Assert.Contains("?n1Var\r\n", immediateText.Substring(existingText.Length));
    }
}
