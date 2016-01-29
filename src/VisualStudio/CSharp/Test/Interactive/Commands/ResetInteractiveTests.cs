// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Editor.Host;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    public class ResetInteractiveTests
    {
        private string WorkspaceXmlStr =>
@"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""ResetInteractiveVisualBasicSubproject"" CommonReferences=""true"">
        <Document FilePath=""VisualBasicDocument""></Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""ResetInteractiveTestsAssembly"" CommonReferences=""true"">
        <ProjectReference>ResetInteractiveVisualBasicSubproject</ProjectReference>
        <Document FilePath=""ResetInteractiveTestsDocument""></Document>
    </Project>
</Workspace>";

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public async void TestResetREPLWithProjectContext()
        {
            using (var workspace = await TestWorkspace.CreateAsync(WorkspaceXmlStr))
            {
                var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName == "ResetInteractiveTestsAssembly");
                var document = project.Documents.FirstOrDefault(d => d.FilePath == "ResetInteractiveTestsDocument");
                var replReferenceCommands = GetProjectReferences(workspace, project).Select(r => CreateReplReferenceCommand(r));

                Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveTestsAssembly.dll""")));
                Assert.True(replReferenceCommands.Any(rc => rc.EndsWith(@"ResetInteractiveVisualBasicSubproject.dll""")));

                var expectedSubmissions = new List<string>();
                expectedSubmissions.AddRange(replReferenceCommands.Select(r => r + "\r\n"));
                expectedSubmissions.Add(string.Join("\r\n", @"using ""ns1"";", @"using ""ns2"";") + "\r\n");
                AssertResetInteractive(workspace, project, buildSucceeds: true, expectedSubmissions: expectedSubmissions);

                // Test that no submissions are executed if the build fails.
                AssertResetInteractive(workspace, project, buildSucceeds: false, expectedSubmissions: new List<string>());
            }
        }

        private async void AssertResetInteractive(
            TestWorkspace workspace,
            Project project,
            bool buildSucceeds,
            List<string> expectedSubmissions)
        {
            InteractiveWindowTestHost testHost = new InteractiveWindowTestHost();
            List<string> executedSubmissionCalls = new List<string>();
            EventHandler<string> ExecuteSubmission = (_, code) => { executedSubmissionCalls.Add(code); };

            testHost.Evaluator.OnExecute += ExecuteSubmission;

            IWaitIndicator waitIndicator = workspace.GetService<IWaitIndicator>();

            TestResetInteractive resetInteractive = new TestResetInteractive(
                waitIndicator,
                CreateReplReferenceCommand,
                CreateImport,
                buildSucceeds: buildSucceeds)
            {
                References = ImmutableArray.CreateRange(GetProjectReferences(workspace, project)),
                ReferenceSearchPaths = ImmutableArray.Create("rsp1", "rsp2"),
                SourceSearchPaths = ImmutableArray.Create("ssp1", "ssp2"),
                NamespacesToImport = ImmutableArray.Create("ns1", "ns2"),
                ProjectDirectory = "pj",
            };

            await resetInteractive.Execute(testHost.Window, "Interactive C#");

            // Validate that the project was rebuilt.
            Assert.Equal(1, resetInteractive.BuildProjectCount);
            Assert.Equal(0, resetInteractive.CancelBuildProjectCount);

            AssertEx.Equal(expectedSubmissions, executedSubmissionCalls);

            testHost.Evaluator.OnExecute -= ExecuteSubmission;
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
