// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectIds = projectDependencyGraph.GetTopologicallySortedProjects(CancellationToken.None);

            var actualResult = string.Concat(projectIds.Select(id => solution.GetProject(id).AssemblyName));
            Assert.Contains<string>(actualResult, expectedResults);
        }

        #endregion

        #region Dependency Sets

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [WorkItem(542438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542438")]
        public void ProjectDependencyGraph_GetDependencySets()
        {
            VerifyDependencySets("A B:A C:A D E:D F:D", "ABC DEF");
            VerifyDependencySets("A B:A,C C", "ABC");
            VerifyDependencySets("A B", "A B");
            VerifyDependencySets("A B C:B", "A BC");
            VerifyDependencySets("A B:A C:A D:B,C", "ABCD");
        }

        private void VerifyDependencySets(string projectReferences, string expectedResult)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectIds = projectDependencyGraph.GetDependencySets(CancellationToken.None);
            var actualResult = string.Join(" ",
                projectIds.Select(
                    group => string.Concat(
                        group.Select(p => solution.GetProject(p).AssemblyName).OrderBy(n => n))).OrderBy(n => n));
            Assert.Equal(expectedResult, actualResult);
        }

        #endregion

        #region GetProjectsThatThisProjectTransitivelyDependsOn

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_GetProjectsThatThisProjectTransitivelyDependsOn()
        {
            VerifyTransitiveReferences("A", "A", new string[] { });
            VerifyTransitiveReferences("B:A A", "B", new string[] { "A" });
            VerifyTransitiveReferences("C:B B:A A", "C", new string[] { "B", "A" });
            VerifyTransitiveReferences("C:B B:A A", "A", new string[] { });
        }

        private void VerifyTransitiveReferences(string projectReferences, string project, string[] expectedResults)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(projectId);

            var actualResults = projectIds.Select(id => solution.GetProject(id).Name);

            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        #endregion

        #region GetProjectsThatTransitivelyDependOnThisProject

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectDependencyGraph_GetProjectsThatTransitivelyDependOnThisProject()
        {
            VerifyReverseTransitiveReferences("A", "A", new string[] { });
            VerifyReverseTransitiveReferences("B:A A", "A", new string[] { "B" });
            VerifyReverseTransitiveReferences("C:B B:A A", "A", new string[] { "B", "C" });
            VerifyReverseTransitiveReferences("C:B B:A A", "C", new string[] { });
            VerifyReverseTransitiveReferences("D:C,B B:A C A", "A", new string[] { "D", "B" });
        }

        private void VerifyReverseTransitiveReferences(string projectReferences, string project, string[] expectedResults)
        {
            Solution solution = CreateSolutionFromReferenceMap(projectReferences);
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectId);

            var actualResults = projectIds.Select(id => solution.GetProject(id).Name);

            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        #endregion

        #region Helpers

        private ProjectDependencyGraph CreateGraph(string projectReferences)
        {
            var solution = CreateSolutionFromReferenceMap(projectReferences);
            return solution.GetProjectDependencyGraph();
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

        private Solution CreateSolution()
        {
            return new AdhocWorkspace().CurrentSolution;
        }

        #endregion
    }
}
