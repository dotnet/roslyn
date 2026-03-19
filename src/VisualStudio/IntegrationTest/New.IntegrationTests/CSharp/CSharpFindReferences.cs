// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.FindReferences)]
public class CSharpFindReferences : AbstractEditorTest
{
    public CSharpFindReferences()
        : base(nameof(CSharpFindReferences))
    {
    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact]
    public async Task FindReferencesToCtor()
    {
        await SetUpEditorAsync("""

            class Program
            {
            }$$

            """, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "File2.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "File2.cs", HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            class SomeOtherClass
            {
                void M()
                {
                    Program p = new Progr$$am();
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        Assert.Collection(
            results,
            [
                reference =>
                {
                    Assert.Equal(expected: "class Program", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    Assert.Equal(expected: 1, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    Assert.Equal(expected: 6, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                },
                reference =>
                {
                    Assert.Equal(expected: "Program p = new Program();", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    Assert.Equal(expected: 5, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    Assert.Equal(expected: 24, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                }
            ]);

        results[0].NavigateTo(isPreview: false, shouldActivate: true);
        await WaitForNavigateAsync(HangMitigatingCancellationToken);

        // Assert we are in the right file now
        Assert.Equal($"Class1.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        Assert.Equal("Program", await TestServices.Editor.GetLineTextAfterCaretAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task FindReferencesToLocals()
    {
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);
        await SetUpEditorAsync("""

            class Program
            {
                static void Main()
                {
                    int local = 1;
                    Console.WriteLine(local$$);
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        Assert.Collection(
            results,
            [
                reference =>
                {
                    Assert.Equal(expected: "int local = 1;", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    Assert.Equal(expected: 5, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    Assert.Equal(expected: 12, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                },
                reference =>
                {
                    Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    Assert.Equal(expected: 6, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    Assert.Equal(expected: 26, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                }
            ]);

        await telemetry.VerifyFiredAsync(["vs/platform/findallreferences/search", "vs/ide/vbcs/commandhandler/findallreference"], HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FindReferencesToString()
    {
        await SetUpEditorAsync("""

            class Program
            {
                static void Main()
                {
                     string local = "1"$$;
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        Assert.Collection(
            results,
            [
                reference =>
                {
                    Assert.Equal(expected: "string local = \"1\";", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    Assert.Equal(expected: 5, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    Assert.Equal(expected: 24, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                }
            ]);
    }

    [IdeFact]
    public async Task VerifyWorkingFolder()
    {
        await SetUpEditorAsync(@"class EmptyContent {$$}", HangMitigatingCancellationToken);

        var visualStudioWorkspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(HangMitigatingCancellationToken);
        var persistentStorageConfiguration = visualStudioWorkspace.Services.GetRequiredService<IPersistentStorageConfiguration>();

        // verify working folder has set
        Assert.NotNull(persistentStorageConfiguration.TryGetStorageLocation(SolutionKey.ToSolutionKey(visualStudioWorkspace.CurrentSolution)));

        await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);

        // Since we no longer have an open solution, we don't have a storage location for it, since that
        // depends on the open solution.
        Assert.Null(persistentStorageConfiguration.TryGetStorageLocation(SolutionKey.ToSolutionKey(visualStudioWorkspace.CurrentSolution)));
    }

    private async Task WaitForNavigateAsync(CancellationToken cancellationToken)
    {
        // Navigation operations handled by Roslyn are tracked by FeatureAttribute.FindReferences
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.FindReferences, cancellationToken);

        // Navigation operations handled by the editor are tracked within its own JoinableTaskFactory instance
        await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
    }
}
