// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

#pragma warning disable xUnit1013 // currently there are public virtual methods that are overridden by derived types

namespace Roslyn.VisualStudio.NewIntegrationTests.Workspaces;

public abstract class WorkspaceBase : AbstractIntegrationTest
{
    private readonly string _defaultProjectTemplate;
    private readonly string _defaultlanguageName = LanguageNames.CSharp;

    protected WorkspaceBase(string projectTemplate) : base()
    {
        _defaultProjectTemplate = projectTemplate;
    }

    protected async Task InitializeWithDefaultSolution()
    {
        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync(ProjectName, _defaultProjectTemplate, _defaultlanguageName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(ProjectName, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task OpenCSharpThenVBSolution()
    {
        await InitializeWithDefaultSolution();
        if (_defaultProjectTemplate == WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
            // The CSharpNetCoreClassLibrary template does not open a file automatically
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, WellKnownProjectTemplates.CSharpNetCoreClassLibraryClassFileName, HangMitigatingCancellationToken);
        }

        await TestServices.Editor.SetTextAsync(@"using System; class Program { Exception e; }", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Exception", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(WorkspaceBase), HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""
            Imports System
            Class Program
                Private e As Exception
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Exception", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
    }

    public virtual async Task MetadataReference()
    {
        await TestServices.SolutionExplorer.AddMetadataReferenceAsync("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", "TestProj", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync("class C { System.Windows.Point p; }", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Point", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync("struct name", HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.RemoveDllReferenceAsync("TestProj", typeof(System.Windows.Point).Assembly.GetName().Name, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentTokenTypeAsync("identifier", HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72018")]
    public async Task ProjectReference()
    {
        await InitializeWithDefaultSolution();
        var csProj2 = "CSProj2";
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddProjectAsync(csProj2, projectTemplate: _defaultProjectTemplate, languageName: _defaultlanguageName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectReferenceAsync(projectName: csProj2, project, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(project, "Program.cs", open: true, contents: "public class Class1 { }", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(csProj2, "Program.cs", open: true, contents: "public class Class2 { Class1 c; }", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(csProj2, "Program.cs", HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Class1", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync("class name", HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.RemoveProjectReferenceAsync(csProj2, projectReferenceName: project, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync("identifier", HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/64672")]
    public async Task ProjectProperties()
    {
        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(WorkspaceBase), HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.SetFullSolutionAnalysisAsync(true, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync("""
            Module Program
                Sub Main()
                    Dim x = 42
                    M(x)
                End Sub
                Sub M(p As Integer)
                End Sub
                Sub M(p As Object)
                End Sub
            End Module
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("(x)", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SetProjectInferAsync(ProjectName, true, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
        var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
        Assert.Equal("Sub Program.M(p As Integer) (+ 1 overload)", quickInfo);

        await TestServices.SolutionExplorer.SetProjectInferAsync(ProjectName, false, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
        quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
        Assert.Equal("Sub Program.M(p As Object) (+ 1 overload)", quickInfo);
    }

    [IdeFact]
    public virtual async Task RenamingOpenFiles()
    {
        await InitializeWithDefaultSolution();
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "BeforeRename.cs", open: true, cancellationToken: HangMitigatingCancellationToken);

        // Verify we are connected to the project before...
        Assert.Equal(ProjectName, (await TestServices.Editor.GetActiveDocumentAsync(HangMitigatingCancellationToken))!.Project.Name);

        await TestServices.SolutionExplorer.RenameFileAsync(ProjectName, "BeforeRename.cs", "AfterRename.cs", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        // ...and after.
        Assert.Equal(ProjectName, (await TestServices.Editor.GetActiveDocumentAsync(HangMitigatingCancellationToken))!.Project.Name);
    }
}

