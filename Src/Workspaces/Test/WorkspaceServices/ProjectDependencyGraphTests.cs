// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    public class ProjectDependencyGraphTests : TestBase
    {
        #region GetTopologicallySortedProjects

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_GetTopologicallySortedProjects()
        {
            VerifyTopologicalSort("A", "A");
            VerifyTopologicalSort("A B", "AB", "BA");
            VerifyTopologicalSort("C:A,B B:A A", "ABC");
            VerifyTopologicalSort("B:A A C:A D:C,B", "ABCD", "ACBD");
        }

        private void VerifyTopologicalSort(string projectReferences, params string[] expectedResults)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = ProjectDependencyGraph.From(solution, CancellationToken.None);
            var projectIds = projectDependencyGraph.GetTopologicallySortedProjects(CancellationToken.None);

            var actualResult = string.Concat(projectIds.Select(id => solution.GetProject(id).AssemblyName));
            Assert.Contains<string>(actualResult, expectedResults);
        }

        #endregion

        #region GetConnectedProjects

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [WorkItem(542438)]
        public void ProjectDependencyGraph_GetConnectedProjects()
        {
            VerifyConnectedProjects("A B:A C:A D E:D F:D", "ABC DEF");
            VerifyConnectedProjects("A B:A,C C", "ABC");
            VerifyConnectedProjects("A B", "A B");
            VerifyConnectedProjects("A B C:B", "A BC");
            VerifyConnectedProjects("A B:A C:A D:B,C", "ABCD");
        }

        private void VerifyConnectedProjects(string projectReferences, string expectedResult)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = ProjectDependencyGraph.From(solution, CancellationToken.None);
            var projectIds = projectDependencyGraph.GetConnectedProjects(CancellationToken.None);
            var actualResult = string.Join(" ",
                projectIds.Select(
                    group => string.Concat(
                        group.Select(p => solution.GetProject(p).AssemblyName).OrderBy(n => n))).OrderBy(n => n));
            Assert.Equal(expectedResult, actualResult);
        }

        #endregion

        #region Serialization

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_RoundTripToText()
        {
            string projectReferences = "B:A A C:A D:C,B";

            // due to the way version is serialized and deserialized, we need to make sure there is no version collision during
            // building solution. otherwise, original version will have higher version than deserialized version and make 
            // creating graph from persistance source fail.
            var solution = CreateSolutionFromReferenceMap(projectReferences);

            var graph = ProjectDependencyGraph.From(solution, CancellationToken.None);
            var text = GetGraphText(graph);

            // write graph to stream
            using (var stream = new MemoryStream())
            {
                using (var writer = new ObjectWriter(stream))
                {
                    graph.WriteTo(writer);
                }

                stream.Position = 0;

                ProjectDependencyGraph newGraph;
                using (var reader = new ObjectReader(stream))
                {
                    // read graph back from stream
                    newGraph = ProjectDependencyGraph.ReadGraph(solution, reader, CancellationToken.None);
                }

                var newText = GetGraphText(newGraph);
                Assert.Equal(text, newText);
            }
        }

        private string GetGraphText(string projectReferences = "B:A A C:A D:C,B")
        {
            var graph = CreateGraph(projectReferences);
            return GetGraphText(graph);
        }

        private string GetGraphText(ProjectDependencyGraph graph)
        {
            using (var stream = new MemoryStream())
            using (var writer = new ObjectWriter(stream))
            {
                graph.WriteTo(writer);
                stream.Position = 0;

                return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
            }
        }
        #endregion

        #region From

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_FromExistingGraph()
        {
            var staleSolution = CreateSolution("stale");
            var staleGraph = ProjectDependencyGraph.From(staleSolution, CancellationToken.None);

            var solution = CreateSolutionFromReferenceMap("A B C");
            var projectA = solution.GetProjectsByName("A").FirstOrDefault().Id;
            var projectB = solution.GetProjectsByName("B").FirstOrDefault().Id;
            var projectC = solution.GetProjectsByName("C").FirstOrDefault().Id;
            var graph = ProjectDependencyGraph.From(solution, staleGraph, CancellationToken.None);

            solution = solution.AddProjectReference(projectB, new ProjectReference(projectA));
            solution = solution.RemoveProject(projectC);
            AddProject(ref solution, 4, "D");
            var projectD = solution.GetProjectsByName("D").FirstOrDefault().Id;
            solution = solution.AddProjectReference(projectD, new ProjectReference(projectA));

            graph = ProjectDependencyGraph.From(solution, graph, CancellationToken.None);

            // we don't have an ordering guarantee.  So consider both cases;
            var test1 = new[] { projectB, projectD }.SequenceEqual(graph.GetProjectsThatDirectlyDependOnThisProject(projectA));
            var test2 = new[] { projectD, projectB }.SequenceEqual(graph.GetProjectsThatDirectlyDependOnThisProject(projectA));
            Assert.True(test1 || test2, "test1 == " + test1 + ", test2 == " + test2);
            AssertEx.Equal(new[] { projectA }, graph.GetProjectsThatThisProjectDirectlyDependsOn(projectB));
        }

        #endregion

        #region GetProjectsThatTransitivelyDependOnThisProject

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_GetProjectsThatTransitivelyDependOnThisProject()
        {
            VerifyTransitiveReferences("A", "A", "");
            VerifyTransitiveReferences("B:A A", "B", "A");
            VerifyTransitiveReferences("C:B B:A A", "C", "BA");
            VerifyTransitiveReferences("C:B B:A A", "A", "");
        }

        private void VerifyTransitiveReferences(string projectReferences, string project, params string[] expectedResults)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = ProjectDependencyGraph.From(solution, CancellationToken.None);
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(projectId);

            var actualResult = string.Concat(projectIds.Select(id => solution.GetProject(id).AssemblyName));
            Assert.Contains<string>(actualResult, expectedResults);
        }

        #endregion

        #region Helpers

        private ProjectDependencyGraph CreateGraph(string projectReferences)
        {
            var solution = CreateSolutionFromReferenceMap(projectReferences);
            return ProjectDependencyGraph.From(solution, CancellationToken.None);
        }

        private Solution CreateSolutionFromReferenceMap(string projectReferences)
        {
            Solution solution = CreateSolution();

            var references = new Dictionary<string, List<string>>();
            var projects = new Dictionary<string, ProjectId>();

            var projectDefinitions = projectReferences.Split(' ');
            int index = 0;
            foreach (var projectDefinition in projectDefinitions)
            {
                var projectDefinitionParts = projectDefinition.Split(':');
                string[] referencedProjectNames = null;

                if (projectDefinitionParts.Length == 2)
                {
                    referencedProjectNames = projectDefinitionParts[1].Split(',');
                }
                else if (projectDefinitionParts.Length != 1)
                {
                    throw new ArgumentException("Invalid project definition: " + projectDefinition);
                }

                string projectName = projectDefinitionParts[0];
                if (referencedProjectNames != null)
                {
                    foreach (var referencedProjectName in referencedProjectNames)
                    {
                        List<string> bucket;
                        if (!references.TryGetValue(projectName, out bucket))
                        {
                            bucket = new List<string>();
                            references.Add(projectName, bucket);
                        }

                        bucket.Add(referencedProjectName);
                    }
                }

                ProjectId projectId = AddProject(ref solution, index, projectName);
                index++;
                projects.Add(projectName, projectId);
            }

            foreach (var kvp in references)
            {
                solution = solution.AddProjectReferences(projects[kvp.Key], kvp.Value.Select(name => new ProjectReference(projects[name])));
            }

            return solution;
        }

        private static ProjectId AddProject(ref Solution solution, int index, string projectName)
        {
            ProjectId projectId = ProjectId.CreateNewId(debugName: projectName);
            solution = solution.AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), projectName, projectName, LanguageNames.CSharp, projectName));
            return projectId;
        }

        private Solution CreateSolution(string name = "TestSolution")
        {
            return new CustomWorkspace(SolutionId.CreateNewId(name)).CurrentSolution;
        }

        #endregion
    }
}
