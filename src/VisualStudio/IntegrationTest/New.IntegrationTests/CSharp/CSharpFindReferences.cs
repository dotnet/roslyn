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
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.FindReferences)]
public class CSharpFindReferences : AbstractEditorTest
{
    private readonly ITestOutputHelper _output;

    public CSharpFindReferences(ITestOutputHelper output)
        : base(nameof(CSharpFindReferences))
    {
        _output = output;
    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/85704")]
    public async Task FindReferencesToCtor()
    {
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 1");
        await SetUpEditorAsync("""

            class Program
            {
            }$$

            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 2");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "File2.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 3");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "File2.cs", HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 4");
        await SetUpEditorAsync("""

            class SomeOtherClass
            {
                void M()
                {
                    Program p = new Progr$$am();
                }
            }

            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 5");
        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 6");
        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 7");
        Assert.Collection(
            results,
            [
                reference =>
                {
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 8");
                    Assert.Equal(expected: "class Program", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 9");
                    Assert.Equal(expected: 1, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 10");
                    Assert.Equal(expected: 6, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                },
                reference =>
                {
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 11");
                    Assert.Equal(expected: "Program p = new Program();", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 12");
                    Assert.Equal(expected: 5, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 13");
                    Assert.Equal(expected: 24, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                }
            ]);

        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 14");
        results[0].NavigateTo(isPreview: false, shouldActivate: true);
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 15");
        await WaitForNavigateAsync(HangMitigatingCancellationToken);

        // Assert we are in the right file now
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 16");
        Assert.Equal($"Class1.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        _output.WriteLine("CSharpFindReferences.FindReferencesToCtor: action 17");
        Assert.Equal("Program", await TestServices.Editor.GetLineTextAfterCaretAsync(HangMitigatingCancellationToken));
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/85704")]
    public async Task FindReferencesToLocals()
    {
        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 1");
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 2");
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

        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 3");
        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 4");
        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 5");
        Assert.Collection(
            results,
            [
                reference =>
                {
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 6");
                    Assert.Equal(expected: "int local = 1;", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 7");
                    Assert.Equal(expected: 5, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 8");
                    Assert.Equal(expected: 12, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                },
                reference =>
                {
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 9");
                    Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.TryGetValue(StandardTableKeyNames.Text, out string code) ? code : null);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 10");
                    Assert.Equal(expected: 6, actual: reference.TryGetValue(StandardTableKeyNames.Line, out int line) ? line : -1);
                    _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 11");
                    Assert.Equal(expected: 26, actual: reference.TryGetValue(StandardTableKeyNames.Column, out int column) ? column : -1);
                }
            ]);

        _output.WriteLine("CSharpFindReferences.FindReferencesToLocals: action 12");
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
