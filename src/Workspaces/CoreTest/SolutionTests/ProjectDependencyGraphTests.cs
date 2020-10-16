﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    [UseExportProvider]
    public class ProjectDependencyGraphTests : TestBase
    {
        #region GetTopologicallySortedProjects

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetTopologicallySortedProjects()
        {
            VerifyTopologicalSort(CreateSolutionFromReferenceMap("A"), "A");
            VerifyTopologicalSort(CreateSolutionFromReferenceMap("A B"), "AB", "BA");
            VerifyTopologicalSort(CreateSolutionFromReferenceMap("C:A,B B:A A"), "ABC");
            VerifyTopologicalSort(CreateSolutionFromReferenceMap("B:A A C:A D:C,B"), "ABCD", "ACBD");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTopologicallySortedProjectsIncrementalUpdate()
        {
            var solution = CreateSolutionFromReferenceMap("A");

            VerifyTopologicalSort(solution, "A");

            solution = AddProject(solution, "B");

            VerifyTopologicalSort(solution, "AB", "BA");
        }

        /// <summary>
        /// Verifies that <see cref="ProjectDependencyGraph.GetTopologicallySortedProjects(CancellationToken)"/> 
        /// returns one of the correct results.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="expectedResults">A list of possible results. Because topological sorting is ambiguous
        /// in that a graph could have multiple topological sorts, this helper lets you give all the possible
        /// results and it asserts that one of them does match.</param>
        private static void VerifyTopologicalSort(Solution solution, params string[] expectedResults)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectIds = projectDependencyGraph.GetTopologicallySortedProjects(CancellationToken.None);

            var actualResult = string.Concat(projectIds.Select(id => solution.GetRequiredProject(id).AssemblyName));
            Assert.Contains<string>(actualResult, expectedResults);
        }

        #endregion

        #region Dependency Sets

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [WorkItem(542438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542438")]
        public void TestGetDependencySets()
        {
            VerifyDependencySets(CreateSolutionFromReferenceMap("A B:A C:A D E:D F:D"), "ABC DEF");
            VerifyDependencySets(CreateSolutionFromReferenceMap("A B:A,C C"), "ABC");
            VerifyDependencySets(CreateSolutionFromReferenceMap("A B"), "A B");
            VerifyDependencySets(CreateSolutionFromReferenceMap("A B C:B"), "A BC");
            VerifyDependencySets(CreateSolutionFromReferenceMap("A B:A C:A D:B,C"), "ABCD");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestDependencySetsIncrementalUpdate()
        {
            var solution = CreateSolutionFromReferenceMap("A");

            VerifyDependencySets(solution, "A");

            solution = AddProject(solution, "B");

            VerifyDependencySets(solution, "A B");
        }

        private static void VerifyDependencySets(Solution solution, string expectedResult)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectIds = projectDependencyGraph.GetDependencySets(CancellationToken.None);
            var actualResult = string.Join(" ",
                projectIds.Select(
                    group => string.Concat(
                        group.Select(p => solution.GetRequiredProject(p).AssemblyName).OrderBy(n => n))).OrderBy(n => n));
            Assert.Equal(expectedResult, actualResult);
        }

        #endregion

        #region GetProjectsThatThisProjectTransitivelyDependsOn

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetProjectsThatThisProjectTransitivelyDependsOn()
        {
            VerifyTransitiveReferences(CreateSolutionFromReferenceMap("A"), "A", new string[] { });
            VerifyTransitiveReferences(CreateSolutionFromReferenceMap("B:A A"), "B", new string[] { "A" });
            VerifyTransitiveReferences(CreateSolutionFromReferenceMap("C:B B:A A"), "C", new string[] { "B", "A" });
            VerifyTransitiveReferences(CreateSolutionFromReferenceMap("C:B B:A A"), "A", new string[] { });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetProjectsThatThisProjectTransitivelyDependsOnThrowsArgumentNull()
        {
            var solution = CreateSolutionFromReferenceMap("");

            Assert.Throws<ArgumentNullException>("projectId",
                () => solution.GetProjectDependencyGraph().GetProjectsThatThisProjectDirectlyDependsOn(null!));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesIncrementalUpdateInMiddle()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // but we will add the B -> C link last, to verify that when we add the B to C link we update the references of A.

            var solution = CreateSolutionFromReferenceMap("A B C D");
            VerifyTransitiveReferences(solution, "A", new string[] { });
            VerifyTransitiveReferences(solution, "B", new string[] { });
            VerifyTransitiveReferences(solution, "C", new string[] { });
            VerifyTransitiveReferences(solution, "D", new string[] { });

            solution = AddProjectReferences(solution, "A", new string[] { "B" });
            solution = AddProjectReferences(solution, "C", new string[] { "D" });

            VerifyDirectReferences(solution, "A", new string[] { "B" });
            VerifyDirectReferences(solution, "C", new string[] { "D" });

            VerifyTransitiveReferences(solution, "A", new string[] { "B" });
            VerifyTransitiveReferences(solution, "B", new string[] { });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });

            solution = AddProjectReferences(solution, "B", new string[] { "C" });

            VerifyDirectReferences(solution, "B", new string[] { "C" });

            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C", "D" });
            VerifyTransitiveReferences(solution, "B", new string[] { "C", "D" });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesIncrementalUpdateInMiddleLongerChain()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C   D -> E -> F
            //
            // but we will add the C-> D link last, to verify that when we add the C to D link we update the references of A. This is similar
            // to the previous test but with a longer chain.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C D:E E:F F");
            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C" });
            VerifyTransitiveReferences(solution, "B", new string[] { "C" });
            VerifyTransitiveReferences(solution, "D", new string[] { "E", "F" });
            VerifyTransitiveReferences(solution, "E", new string[] { "F" });

            solution = AddProjectReferences(solution, "C", new string[] { "D" });

            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C", "D", "E", "F" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesIncrementalUpdateWithReferencesAlreadyTransitivelyIncluded()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C
            //
            // and then we'll add a reference from A -> C, and transitive references should be different

            var solution = CreateSolutionFromReferenceMap("A:B B:C C");

            void VerifyAllTransitiveReferences()
            {
                VerifyTransitiveReferences(solution, "A", new string[] { "B", "C" });
                VerifyTransitiveReferences(solution, "B", new string[] { "C" });
                VerifyTransitiveReferences(solution, "C", new string[] { });
            }

            VerifyAllTransitiveReferences();
            VerifyDirectReferences(solution, "A", new string[] { "B" });

            solution = AddProjectReferences(solution, "A", new string[] { "C" });

            VerifyAllTransitiveReferences();
            VerifyDirectReferences(solution, "A", new string[] { "B", "C" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesIncrementalUpdateWithProjectThatHasUnknownReferences()
        {
            // We are going to create a solution with the references:
            //
            // A  B  C -> D
            //
            // and then we will add a link from A to B. We won't ask for transitive references first,
            // so we shouldn't have any information for A, B, or C and have to deal with that.

            var solution = CreateSolutionFromReferenceMap("A B C:D D");
            solution = solution.WithProjectReferences(solution.GetProjectsByName("C").Single().Id,
                SpecializedCollections.EmptyEnumerable<ProjectReference>());

            VerifyTransitiveReferences(solution, "A", new string[] { });

            // At this point, we know the references for "A" (it's empty), but B and C's are still unknown.
            // At this point, we're also going to directly use the underlying project graph APIs;
            // the higher level solution APIs often call and ask for transitive information as well, which makes
            // this particularly hard to test -- it turns out the data we think is uncomputed might be computed prior
            // to adding the reference.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            var projectAId = solution.GetProjectsByName("A").Single().Id;
            var projectBId = solution.GetProjectsByName("B").Single().Id;
            dependencyGraph = dependencyGraph.WithAdditionalProjectReferences(projectAId, new[] { new ProjectReference(projectBId) });

            VerifyTransitiveReferences(solution, dependencyGraph, project: "A", expectedResults: new string[] { "B" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesWithDanglingProjectReference()
        {
            // We are going to create a solution with the references:
            //
            // A -> B
            //
            // but we're going to add A as a reference with B not existing yet. Then we'll add in B and ask.

            var solution = CreateSolution();
            var projectAId = ProjectId.CreateNewId("A");
            var projectBId = ProjectId.CreateNewId("B");

            var projectAInfo = ProjectInfo.Create(projectAId, VersionStamp.Create(), "A", "A", LanguageNames.CSharp,
                                    projectReferences: new[] { new ProjectReference(projectBId) });

            solution = solution.AddProject(projectAInfo);

            VerifyDirectReferences(solution, "A", new string[] { });
            VerifyTransitiveReferences(solution, "A", new string[] { });

            solution = solution.AddProject(projectBId, "B", "B", LanguageNames.CSharp);

            VerifyTransitiveReferences(solution, "A", new string[] { "B" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTransitiveReferencesWithMultipleReferences()
        {
            // We are going to create a solution with the references:
            //
            // A    B -> C    D -> E
            //
            // and then add A referencing B and D in one call, to make sure that works.

            var solution = CreateSolutionFromReferenceMap("A B:C C D:E E");
            VerifyTransitiveReferences(solution, "A", new string[] { });

            solution = AddProjectReferences(solution, "A", new string[] { "B", "D" });

            VerifyDirectReferences(solution, "A", new string[] { "B", "D" });
            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C", "D", "E" });
        }

        private static void VerifyDirectReferences(Solution solution, string project, string[] expectedResults)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatThisProjectDirectlyDependsOn(projectId);

            var actualResults = projectIds.Select(id => solution.GetRequiredProject(id).Name);
            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        private static void VerifyTransitiveReferences(Solution solution, string project, string[] expectedResults)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            VerifyTransitiveReferences(solution, projectDependencyGraph, project, expectedResults);
        }

        private static void VerifyTransitiveReferences(Solution solution, ProjectDependencyGraph projectDependencyGraph, string project, string[] expectedResults)
        {
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(projectId);

            var actualResults = projectIds.Select(id => solution.GetRequiredProject(id).Name);
            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestDirectAndReverseDirectReferencesAfterWithProjectReferences()
        {
            var solution = CreateSolutionFromReferenceMap("A:B B");

            VerifyDirectReverseReferences(solution, "B", new string[] { "A" });

            solution = solution.WithProjectReferences(solution.GetProjectsByName("A").Single().Id,
                Enumerable.Empty<ProjectReference>());

            VerifyDirectReferences(solution, "A", new string[] { });
            VerifyDirectReverseReferences(solution, "B", new string[] { });
        }

        #region GetProjectsThatTransitivelyDependOnThisProject

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetProjectsThatTransitivelyDependOnThisProject()
        {
            VerifyReverseTransitiveReferences(CreateSolutionFromReferenceMap("A"), "A", new string[] { });
            VerifyReverseTransitiveReferences(CreateSolutionFromReferenceMap("B:A A"), "A", new string[] { "B" });
            VerifyReverseTransitiveReferences(CreateSolutionFromReferenceMap("C:B B:A A"), "A", new string[] { "B", "C" });
            VerifyReverseTransitiveReferences(CreateSolutionFromReferenceMap("C:B B:A A"), "C", new string[] { });
            VerifyReverseTransitiveReferences(CreateSolutionFromReferenceMap("D:C,B B:A C A"), "A", new string[] { "D", "B" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetProjectsThatTransitivelyDependOnThisProjectThrowsArgumentNull()
        {
            var solution = CreateSolutionFromReferenceMap("");

            Assert.Throws<ArgumentNullException>("projectId",
                () => solution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(null!));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesIncrementalUpdateInMiddle()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // but we will add the B -> C link last, to verify that when we add the B to C link we update the reverse references of D.

            var solution = CreateSolutionFromReferenceMap("A B C D");
            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { });

            solution = AddProjectReferences(solution, "A", new string[] { "B" });
            solution = AddProjectReferences(solution, "C", new string[] { "D" });

            VerifyDirectReverseReferences(solution, "B", new string[] { "A" });
            VerifyDirectReverseReferences(solution, "D", new string[] { "C" });

            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "C" });

            solution = AddProjectReferences(solution, "B", new string[] { "C" });

            VerifyDirectReverseReferences(solution, "C", new string[] { "B" });

            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { "A", "B" });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "A", "B", "C" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesForUnrelatedProjectAfterWithProjectReferences()
        {
            // We are going to create a solution with the references:
            //
            // A -> B       C -> D
            //
            // and will then remove the reference from C to D. This process will cause us to throw out
            // all our caches, and asking for the reverse references of A will compute it again.

            var solution = CreateSolutionFromReferenceMap("A:B B C:D D");
            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "C" });

            solution = solution.WithProjectReferences(solution.GetProjectsByName("C").Single().Id,
                SpecializedCollections.EmptyEnumerable<ProjectReference>());

            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestForwardReferencesAfterProjectRemoval()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // and will then remove project B.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C:D D");
            VerifyDirectReferences(solution, "A", new string[] { "B" });
            VerifyDirectReferences(solution, "B", new string[] { "C" });
            VerifyDirectReferences(solution, "C", new string[] { "D" });
            VerifyDirectReferences(solution, "D", new string[] { });

            solution = solution.RemoveProject(solution.GetProjectsByName("B").Single().Id);

            VerifyDirectReferences(solution, "A", new string[] { });
            VerifyDirectReferences(solution, "C", new string[] { "D" });
            VerifyDirectReferences(solution, "D", new string[] { });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestForwardTransitiveReferencesAfterProjectRemoval()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // and will then remove project B.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C:D D");
            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C", "D" });
            VerifyTransitiveReferences(solution, "B", new string[] { "C", "D" });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });

            solution = solution.RemoveProject(solution.GetProjectsByName("B").Single().Id);

            VerifyTransitiveReferences(solution, "A", new string[] { });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseReferencesAfterProjectRemoval()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // and will then remove project B.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C:D D");
            VerifyDirectReverseReferences(solution, "A", new string[] { });
            VerifyDirectReverseReferences(solution, "B", new string[] { "A" });
            VerifyDirectReverseReferences(solution, "C", new string[] { "B" });
            VerifyDirectReverseReferences(solution, "D", new string[] { "C" });

            solution = solution.RemoveProject(solution.GetProjectsByName("B").Single().Id);

            VerifyDirectReverseReferences(solution, "A", new string[] { });
            VerifyDirectReverseReferences(solution, "C", new string[] { });
            VerifyDirectReverseReferences(solution, "D", new string[] { "C" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesAfterProjectRemoval()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //
            // and will then remove project B.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C:D D");
            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { "A", "B" });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "A", "B", "C" });

            solution = solution.RemoveProject(solution.GetProjectsByName("B").Single().Id);

            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "C" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesAfterProjectReferenceRemoval_PreserveThroughUnrelatedSequence()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C
            //   \
            //    > D
            //
            // and will then remove the project reference A->B. This test verifies that the new project dependency graph
            // did not lose previously-computed information about the transitive reverse references for D.

            var solution = CreateSolutionFromReferenceMap("A:B,D B:C C D");
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "A" });

            var a = solution.GetProjectsByName("A").Single();
            var b = solution.GetProjectsByName("B").Single();
            var d = solution.GetProjectsByName("D").Single();
            var expected = solution.State.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(d.Id);

            var aToB = a.ProjectReferences.Single(reference => reference.ProjectId == b.Id);
            solution = solution.RemoveProjectReference(a.Id, aToB);

            // Before any other operations, verify that TryGetProjectsThatTransitivelyDependOnThisProject returns a
            // non-null set. Specifically, it returns the _same_ set that was computed prior to the project reference
            // removal.
            Assert.Same(expected, solution.State.GetProjectDependencyGraph().GetTestAccessor().TryGetProjectsThatTransitivelyDependOnThisProject(d.Id));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesAfterProjectReferenceRemoval_PreserveUnrelated()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C
            // D -> E
            //
            // and will then remove the project reference A->B. This test verifies that the new project dependency graph
            // did not lose previously-computed information about the transitive reverse references for E.

            var solution = CreateSolutionFromReferenceMap("A:B B:C C D:E E");
            VerifyReverseTransitiveReferences(solution, "E", new string[] { "D" });

            var a = solution.GetProjectsByName("A").Single();
            var b = solution.GetProjectsByName("B").Single();
            var e = solution.GetProjectsByName("E").Single();
            var expected = solution.State.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(e.Id);

            var aToB = a.ProjectReferences.Single(reference => reference.ProjectId == b.Id);
            solution = solution.RemoveProjectReference(a.Id, aToB);

            // Before any other operations, verify that TryGetProjectsThatTransitivelyDependOnThisProject returns a
            // non-null set. Specifically, it returns the _same_ set that was computed prior to the project reference
            // removal.
            Assert.Same(expected, solution.State.GetProjectDependencyGraph().GetTestAccessor().TryGetProjectsThatTransitivelyDependOnThisProject(e.Id));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestReverseTransitiveReferencesAfterProjectReferenceRemoval_DiscardImpacted()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C
            //   \
            //    > D
            //
            // and will then remove the project reference A->B. This test verifies that the new project dependency graph
            // discards previously-computed information about the transitive reverse references for C.

            var solution = CreateSolutionFromReferenceMap("A:B,D B:C C D");
            VerifyReverseTransitiveReferences(solution, "C", new string[] { "A", "B" });

            var a = solution.GetProjectsByName("A").Single();
            var b = solution.GetProjectsByName("B").Single();
            var c = solution.GetProjectsByName("C").Single();
            var notExpected = solution.State.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(c.Id);
            Assert.NotNull(notExpected);

            var aToB = a.ProjectReferences.Single(reference => reference.ProjectId == b.Id);
            solution = solution.RemoveProjectReference(a.Id, aToB);

            // Before any other operations, verify that TryGetProjectsThatTransitivelyDependOnThisProject returns a
            // null set.
            Assert.Null(solution.State.GetProjectDependencyGraph().GetTestAccessor().TryGetProjectsThatTransitivelyDependOnThisProject(c.Id));
            VerifyReverseTransitiveReferences(solution, "C", new string[] { "B" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestSameDependencyGraphAfterOneOfMultipleReferencesRemoved()
        {
            // We are going to create a solution with the references:
            //
            // A -> B -> C -> D
            //        \__^
            //
            // This solution has multiple references from B->C. We will remove one reference from B->C and verify that
            // the project dependency graph in the solution state did not change (reference equality).
            //
            // We then remove the second reference, and verify that the dependency graph does change.

            var solution = CreateSolutionFromReferenceMap("A:B B:C,C C:D D");

            VerifyDirectReferences(solution, "A", new string[] { "B" });
            VerifyDirectReferences(solution, "B", new string[] { "C" });
            VerifyDirectReferences(solution, "C", new string[] { "D" });
            VerifyDirectReferences(solution, "D", new string[] { });

            VerifyTransitiveReferences(solution, "A", new string[] { "B", "C", "D" });
            VerifyTransitiveReferences(solution, "B", new string[] { "C", "D" });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });

            VerifyDirectReverseReferences(solution, "A", new string[] { });
            VerifyDirectReverseReferences(solution, "B", new string[] { "A" });
            VerifyDirectReverseReferences(solution, "C", new string[] { "B" });
            VerifyDirectReverseReferences(solution, "D", new string[] { "C" });

            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { "A", "B" });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "A", "B", "C" });

            var dependencyGraph = solution.State.GetProjectDependencyGraph();
            Assert.NotNull(dependencyGraph);

            var b = solution.GetProjectsByName("B").Single();
            var c = solution.GetProjectsByName("C").Single();
            var firstBToC = b.ProjectReferences.First(reference => reference.ProjectId == c.Id);
            solution = solution.RemoveProjectReference(b.Id, firstBToC);
            Assert.Same(dependencyGraph, solution.State.GetProjectDependencyGraph());

            b = solution.GetProjectsByName("B").Single();
            var remainingBToC = b.ProjectReferences.Single(reference => reference.ProjectId == c.Id);
            solution = solution.RemoveProjectReference(b.Id, remainingBToC);
            Assert.NotSame(dependencyGraph, solution.State.GetProjectDependencyGraph());

            VerifyDirectReferences(solution, "A", new string[] { "B" });
            VerifyDirectReferences(solution, "B", new string[] { });
            VerifyDirectReferences(solution, "C", new string[] { "D" });
            VerifyDirectReferences(solution, "D", new string[] { });

            VerifyTransitiveReferences(solution, "A", new string[] { "B" });
            VerifyTransitiveReferences(solution, "B", new string[] { });
            VerifyTransitiveReferences(solution, "C", new string[] { "D" });
            VerifyTransitiveReferences(solution, "D", new string[] { });

            VerifyDirectReverseReferences(solution, "A", new string[] { });
            VerifyDirectReverseReferences(solution, "B", new string[] { "A" });
            VerifyDirectReverseReferences(solution, "C", new string[] { });
            VerifyDirectReverseReferences(solution, "D", new string[] { "C" });

            VerifyReverseTransitiveReferences(solution, "A", new string[] { });
            VerifyReverseTransitiveReferences(solution, "B", new string[] { "A" });
            VerifyReverseTransitiveReferences(solution, "C", new string[] { });
            VerifyReverseTransitiveReferences(solution, "D", new string[] { "C" });
        }

        private static void VerifyDirectReverseReferences(Solution solution, string project, string[] expectedResults)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatDirectlyDependOnThisProject(projectId);

            var actualResults = projectIds.Select(id => solution.GetRequiredProject(id).Name);
            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        private static void VerifyReverseTransitiveReferences(Solution solution, string project, string[] expectedResults)
        {
            var projectDependencyGraph = solution.GetProjectDependencyGraph();
            var projectId = solution.GetProjectsByName(project).Single().Id;
            var projectIds = projectDependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectId);

            var actualResults = projectIds.Select(id => solution.GetRequiredProject(id).Name);

            Assert.Equal<string>(
                expectedResults.OrderBy(n => n),
                actualResults.OrderBy(n => n));
        }

        #endregion

        #region Helpers

        private static Solution CreateSolutionFromReferenceMap(string projectReferences)
        {
            var solution = CreateSolution();

            var references = new Dictionary<string, IEnumerable<string>>();

            var projectDefinitions = projectReferences.Split(' ');
            foreach (var projectDefinition in projectDefinitions)
            {
                var projectDefinitionParts = projectDefinition.Split(':');
                string[]? referencedProjectNames = null;

                if (projectDefinitionParts.Length == 2)
                {
                    referencedProjectNames = projectDefinitionParts[1].Split(',');
                }
                else if (projectDefinitionParts.Length != 1)
                {
                    throw new ArgumentException("Invalid project definition: " + projectDefinition);
                }

                var projectName = projectDefinitionParts[0];
                if (referencedProjectNames != null)
                {
                    references.Add(projectName, referencedProjectNames);
                }

                solution = AddProject(solution, projectName);
            }

            foreach (var kvp in references)
            {
                solution = AddProjectReferences(solution, kvp.Key, kvp.Value);
            }

            return solution;
        }

        private static Solution AddProject(Solution solution, string projectName)
        {
            var projectId = ProjectId.CreateNewId(debugName: projectName);
            return solution.AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), projectName, projectName, LanguageNames.CSharp, projectName));
        }

        private static Solution AddProjectReferences(Solution solution, string projectName, IEnumerable<string> projectReferences)
        {
            var referencesByTargetProject = new Dictionary<string, List<ProjectReference>>();
            foreach (var targetProject in projectReferences)
            {
                var references = referencesByTargetProject.GetOrAdd(targetProject, _ => new List<ProjectReference>());
                if (references.Count == 0)
                {
                    references.Add(new ProjectReference(solution.GetProjectsByName(targetProject).Single().Id));
                }
                else
                {
                    references.Add(new ProjectReference(solution.GetProjectsByName(targetProject).Single().Id, ImmutableArray.Create($"alias{references.Count}")));
                }
            }

            return solution.AddProjectReferences(
                solution.GetProjectsByName(projectName).Single().Id,
                referencesByTargetProject.SelectMany(pair => pair.Value));
        }

        private static Solution CreateSolution()
            => new AdhocWorkspace().CurrentSolution;

        #endregion
    }
}
