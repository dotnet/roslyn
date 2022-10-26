// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNewDocumentFormatting : AbstractIntegrationTest
    {
        public CSharpNewDocumentFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.SolutionExplorer.CreateSolution(nameof(CSharpNewDocumentFormatting));
        }

        [WpfFact]
        [WorkItem(1411721, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
        public void CreateLegacyProjectWithFileScopedNamespaces()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();
        }

        [WpfFact]
        [WorkItem(1411721, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
        public void CreateSDKProjectWithFileScopedNamespaces()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetCoreConsoleApplication, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();
        }

        [WpfFact]
        [WorkItem(60449, "https://github.com/dotnet/roslyn/issues/60449")]
        public void CreateSDKProjectWithBlockScopedNamespacesFromEditorConfig()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            var editorConfigFilePath = Path.Combine(VisualStudio.SolutionExplorer.DirectoryName, ".editorconfig");
            File.WriteAllText(editorConfigFilePath,
@"
root = true

[*.cs]
csharp_style_namespace_declarations = block_scoped
");

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();

            VisualStudio.Editor.Verify.TextContains("namespace TestProj\r\n{");
        }

        [WpfFact]
        [WorkItem(60449, "https://github.com/dotnet/roslyn/issues/60449")]
        public void CreateSDKProjectWithBlockScopedNamespacesFromIrrelevantEditorConfigH()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            var editorConfigFilePath = Path.Combine(VisualStudio.SolutionExplorer.DirectoryName, ".editorconfig");
            File.WriteAllText(editorConfigFilePath,
@"
root = true
");

            // This editor config file should be ignored
            editorConfigFilePath = Path.Combine(VisualStudio.SolutionExplorer.DirectoryName, "..", ".editorconfig");
            File.WriteAllText(editorConfigFilePath,
@"
[*.cs]
csharp_style_namespace_declarations = block_scoped
");

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();

            VisualStudio.Editor.Verify.TextContains("namespace TestProj;");
        }

        [WpfFact]
        [WorkItem(60449, "https://github.com/dotnet/roslyn/issues/60449")]
        public void CreateSDKProjectWithFileScopedNamespacesFromEditorConfig()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(false);

            var editorConfigFilePath = Path.Combine(VisualStudio.SolutionExplorer.DirectoryName, ".editorconfig");
            File.WriteAllText(editorConfigFilePath,
@"
root = true

[*.cs]
csharp_style_namespace_declarations = file_scoped
");

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();

            VisualStudio.Editor.Verify.TextContains("namespace TestProj;");
        }
    }
}
