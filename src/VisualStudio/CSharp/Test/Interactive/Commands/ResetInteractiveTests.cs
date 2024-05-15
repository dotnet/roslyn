// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;

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
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    [UseExportProvider]
    public class ResetInteractiveTests
    {
        private const string WorkspaceXmlStr =
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
            using var workspace = TestWorkspace.Create(WorkspaceXmlStr, composition: EditorTestCompositions.InteractiveWindow);

            var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName == "ResetInteractiveTestsAssembly");
            var document = project.Documents.FirstOrDefault(d => d.FilePath == "ResetInteractiveTestsDocument");
            var replReferenceCommands = GetProjectReferences(workspace, project).Select(r => CreateReplReferenceCommand(r));

            Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveTestsAssembly.dll""")));
            Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveVisualBasicSubproject.dll""")));

            var expectedReferences = replReferenceCommands.ToList();
            var expectedUsings = new List<string> { @"using ""System"";", @"using ""ResetInteractiveTestsDocument"";" };
            await AssertResetInteractiveAsync(workspace, project, buildSucceeds: true, expectedReferences: expectedReferences, expectedUsings: expectedUsings);

            // Test that no submissions are executed if the build fails.
            await AssertResetInteractiveAsync(workspace, project, buildSucceeds: false, expectedReferences: []);
        }

        private async Task AssertResetInteractiveAsync(
            TestWorkspace workspace,
            Project project,
            bool buildSucceeds,
            List<string> expectedReferences = null,
            List<string> expectedUsings = null)
        {
            expectedReferences ??= [];
            expectedUsings ??= [];

            var testHost = new InteractiveWindowTestHost(workspace.ExportProvider.GetExportedValue<IInteractiveWindowFactoryService>());
            var executedSubmissionCalls = new List<string>();

            void executeSubmission(object _, string code) => executedSubmissionCalls.Add(code);
            testHost.Evaluator.OnExecute += executeSubmission;

            var uiThreadOperationExecutor = workspace.GetService<IUIThreadOperationExecutor>();
            var editorOptionsService = workspace.GetService<EditorOptionsService>();
            var editorOptions = editorOptionsService.Factory.GetOptions(testHost.Window.CurrentLanguageBuffer);
            var newLineCharacter = editorOptions.GetNewLineCharacter();

            var resetInteractive = new TestResetInteractive(
                uiThreadOperationExecutor,
                editorOptionsService,
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
                Platform = InteractiveHostPlatform.Desktop64,
            };

            await resetInteractive.ExecuteAsync(testHost.Window, "Interactive C#");

            // Validate that the project was rebuilt.
            Assert.Equal(1, resetInteractive.BuildProjectCount);
            Assert.Equal(0, resetInteractive.CancelBuildProjectCount);

            if (buildSucceeds)
            {
                Assert.Equal(InteractiveHostPlatform.Desktop64, testHost.Evaluator.ResetOptions.Platform);
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
        private static IEnumerable<string> GetProjectReferences(TestWorkspace workspace, Project project)
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
