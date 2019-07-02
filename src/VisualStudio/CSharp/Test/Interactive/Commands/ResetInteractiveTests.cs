// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    [UseExportProvider]
    public class ResetInteractiveTests
    {
        private string WorkspaceXmlStr =>
@"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""ResetInteractiveVisualBasicSubproject"" CommonReferences=""true"">
        <Document FilePath=""VisualBasicDocument""></Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""ResetInteractiveTestsAssembly"" CommonReferences=""true"">
        <ProjectReference>ResetInteractiveVisualBasicSubproject</ProjectReference>
        <Document FilePath=""ResetInteractiveTestsDocument"">
namespace ResetInteractiveTestsDocument
{
    class TestClass
    {
    }
}</Document>
    </Project>
</Workspace>";

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public async Task TestResetREPLWithProjectContext()
        {
            using var workspace = TestWorkspace.Create(WorkspaceXmlStr, exportProvider: InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider());

            var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName == "ResetInteractiveTestsAssembly");
            var document = project.Documents.FirstOrDefault(d => d.FilePath == "ResetInteractiveTestsDocument");
            var replReferenceCommands = GetProjectReferences(workspace, project).Select(r => CreateReplReferenceCommand(r));

            Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveTestsAssembly.dll""")));
            Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveVisualBasicSubproject.dll""")));

            var expectedReferences = replReferenceCommands.ToList();
            var expectedUsings = new List<string> { @"using ""System"";", @"using ""ResetInteractiveTestsDocument"";" };
            await AssertResetInteractiveAsync(workspace, project, buildSucceeds: true, expectedReferences: expectedReferences, expectedUsings: expectedUsings);

            // Test that no submissions are executed if the build fails.
            await AssertResetInteractiveAsync(workspace, project, buildSucceeds: false, expectedReferences: new List<string>());
        }

        private async Task AssertResetInteractiveAsync(
            TestWorkspace workspace,
            Project project,
            bool buildSucceeds,
            List<string> expectedReferences = null,
            List<string> expectedUsings = null)
        {
            expectedReferences ??= new List<string>();
            expectedUsings ??= new List<string>();

            var testHost = new InteractiveWindowTestHost(workspace.ExportProvider);
            var executedSubmissionCalls = new List<string>();

            void executeSubmission(object _, string code) => executedSubmissionCalls.Add(code);
            testHost.Evaluator.OnExecute += executeSubmission;

            var waitIndicator = workspace.GetService<IWaitIndicator>();
            var editorOptionsFactoryService = workspace.GetService<IEditorOptionsFactoryService>();
            var editorOptions = editorOptionsFactoryService.GetOptions(testHost.Window.CurrentLanguageBuffer);
            var newLineCharacter = editorOptions.GetNewLineCharacter();

            var resetInteractive = new TestResetInteractive(
                waitIndicator,
                editorOptionsFactoryService,
                CreateReplReferenceCommand,
                CreateImport,
                buildSucceeds: buildSucceeds)
            {
                References = ImmutableArray.CreateRange(GetProjectReferences(workspace, project)),
                ReferenceSearchPaths = ImmutableArray.Create("rsp1", "rsp2"),
                SourceSearchPaths = ImmutableArray.Create("ssp1", "ssp2"),
                ProjectNamespaces = ImmutableArray.Create("System", "ResetInteractiveTestsDocument", "VisualBasicResetInteractiveTestsDocument"),
                NamespacesToImport = ImmutableArray.Create("System", "ResetInteractiveTestsDocument"),
                ProjectDirectory = "pj",
                Is64Bit = true,
            };

            await resetInteractive.Execute(testHost.Window, "Interactive C#");

            // Validate that the project was rebuilt.
            Assert.Equal(1, resetInteractive.BuildProjectCount);
            Assert.Equal(0, resetInteractive.CancelBuildProjectCount);

            if (buildSucceeds)
            {
                Assert.Equal(true, testHost.Evaluator.ResetOptions.Is64Bit);
            }
            else
            {
                Assert.Null(testHost.Evaluator.ResetOptions);
            }

            var expectedSubmissions = new List<string>();
            if (expectedReferences.Any())
            {
                expectedSubmissions.AddRange(expectedReferences.Select(r => r + newLineCharacter));
            }
            if (expectedUsings.Any())
            {
                expectedSubmissions.Add(string.Join(newLineCharacter, expectedUsings) + newLineCharacter);
            }

            AssertEx.Equal(expectedSubmissions, executedSubmissionCalls);

            testHost.Evaluator.OnExecute -= executeSubmission;
        }

        /// <summary>
        /// Simulates getting all project references.
        /// </summary>
        /// <param name="workspace">Workspace with the solution.</param>
        /// <param name="project">A project that should be built.</param>
        /// <returns>A list of paths that should be referenced.</returns>
        private IEnumerable<string> GetProjectReferences(TestWorkspace workspace, Project project)
        {
            var metadataReferences = project.MetadataReferences.Select(r => r.Display);
            var projectReferences = project.ProjectReferences.SelectMany(p => GetProjectReferences(
                workspace,
                workspace.CurrentSolution.GetProject(p.ProjectId)));
            var outputReference = new string[] { project.OutputFilePath };

            return metadataReferences.Union(projectReferences).Concat(outputReference);
        }

        private string CreateReplReferenceCommand(string referenceName)
        {
            return $@"#r ""{referenceName}""";
        }

        private string CreateImport(string importName)
        {
            return $@"using ""{importName}"";";
        }
    }
}
