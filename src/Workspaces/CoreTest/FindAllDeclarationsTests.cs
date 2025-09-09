// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed partial class FindAllDeclarationsTests : TestBase
{
    #region FindDeclarationsAsync

    [Theory,

    InlineData("", true, SolutionKind.SingleClass, new string[0]),
    InlineData(" ", true, SolutionKind.SingleClass, new string[0]),
    InlineData("\u2619", true, SolutionKind.SingleClass, new string[0]),

    InlineData("testcase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
    InlineData("testcase", false, SolutionKind.SingleClass, new string[0]),
    InlineData("testcases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
    InlineData("testcases", false, SolutionKind.SingleClass, new string[0]),
    InlineData("TestCase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
    InlineData("TestCase", false, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
    InlineData("TestCases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
    InlineData("TestCases", false, SolutionKind.SingleClass, new[] { "TestCases" }),

    InlineData("test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
    InlineData("test", false, SolutionKind.SingleClassWithSingleMethod, new string[0]),
    InlineData("Test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
    InlineData("Test", false, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

    InlineData("testproperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
    InlineData("testproperty", false, SolutionKind.SingleClassWithSingleProperty, new string[0]),
    InlineData("TestProperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
    InlineData("TestProperty", false, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

    InlineData("testfield", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
    InlineData("testfield", false, SolutionKind.SingleClassWithSingleField, new string[0]),
    InlineData("TestField", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
    InlineData("TestField", false, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

    InlineData("testcase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
    InlineData("testcase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
    InlineData("testcases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
    InlineData("testcases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
    InlineData("TestCase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
    InlineData("TestCase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
    InlineData("TestCases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
    InlineData("TestCases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),

    InlineData("test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
    InlineData("test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
    InlineData("Test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
    InlineData("Test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

    InlineData("testproperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
    InlineData("testproperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
    InlineData("TestProperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
    InlineData("TestProperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

    InlineData("testfield", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
    InlineData("testfield", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
    InlineData("TestField", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
    InlineData("TestField", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

    InlineData("innertestcase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
    InlineData("innertestcase", false, SolutionKind.NestedClass, new string[0]),
    InlineData("InnerTestCase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
    InlineData("InnerTestCase", false, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

    InlineData("testcase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
    InlineData("testcase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),
    InlineData("TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
    InlineData("TestCase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
    InlineData("TestCase1.TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),]

    public async Task FindDeclarationsAsync_Test(string searchTerm, bool ignoreCase, SolutionKind workspaceKind, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithProject(workspaceKind, out var project);
        var declarations = await SymbolFinder.FindDeclarationsAsync(project, searchTerm, ignoreCase).ConfigureAwait(false);
        Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public Task FindDeclarationsAsync_Test_NullProject()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            var declarations = await SymbolFinder.FindDeclarationsAsync(null, "Test", true);
        });

    [Fact]
    public Task FindDeclarationsAsync_Test_NullString()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindDeclarationsAsync(project, null, true);
        });

    [Theory, CombinatorialData]
    public Task FindDeclarationsAsync_Test_Cancellation(TestHost testHost)
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project, testHost);
            var declarations = await SymbolFinder.FindDeclarationsAsync(project, "Test", true, SymbolFilter.All, new CancellationToken(true));
        });

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")]
    public async Task FindDeclarationsAsync_Metadata(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost);
        var solution = workspace.CurrentSolution;
        var csharpId = ProjectId.CreateNewId();
        solution = solution
            .AddProject(csharpId, "CSharp", "CSharp", LanguageNames.CSharp)
            .AddMetadataReference(csharpId, MscorlibRef);

        var vbId = ProjectId.CreateNewId();
        solution = solution
            .AddProject(vbId, "VB", "VB", LanguageNames.VisualBasic)
            .AddMetadataReference(vbId, MscorlibRef);

        var csharpResult = await SymbolFinder.FindDeclarationsAsync(solution.GetProject(csharpId), "Console", ignoreCase: false);
        Assert.True(csharpResult.Count() > 0);

        var vbResult = await SymbolFinder.FindDeclarationsAsync(solution.GetProject(vbId), "Console", ignoreCase: true);
        Assert.True(vbResult.Count() > 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6616")]
    public async Task FindDeclarationsAsync_PreviousSubmission(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost);
        var solution = workspace.CurrentSolution;

        var submission0Id = ProjectId.CreateNewId();
        var submission0DocId = DocumentId.CreateNewId(submission0Id);
        const string submission0Name = "Submission#0";
        solution = solution
            .AddProject(submission0Id, submission0Name, submission0Name, LanguageNames.CSharp)
            .AddMetadataReference(submission0Id, MscorlibRef)
            .AddDocument(submission0DocId, submission0Name, """
            public class Outer
            {
                public class Inner
                {
                }
            }
            """);

        var submission1Id = ProjectId.CreateNewId();
        var submission1DocId = DocumentId.CreateNewId(submission1Id);
        const string submission1Name = "Submission#1";
        solution = solution
            .AddProject(submission1Id, submission1Name, submission1Name, LanguageNames.CSharp)
            .AddMetadataReference(submission1Id, MscorlibRef)
            .AddProjectReference(submission1Id, new ProjectReference(submission0Id))
            .AddDocument(submission1DocId, submission1Name, """
            Inner i;
            """);

        var actualSymbol = (await SymbolFinder.FindDeclarationsAsync(solution.GetProject(submission1Id), "Inner", ignoreCase: false)).SingleOrDefault();
        var expectedSymbol = (await solution.GetProject(submission0Id).GetCompilationAsync()).GlobalNamespace.GetMembers("Outer").SingleOrDefault().GetMembers("Inner").SingleOrDefault();
        Assert.Equal(expectedSymbol, actualSymbol);
    }

    #endregion

    #region FindSourceDeclarationsAsync_Project

    [Theory,

     InlineData("", true, SolutionKind.SingleClass, new string[0]),
     InlineData(" ", true, SolutionKind.SingleClass, new string[0]),
     InlineData("\u2619", true, SolutionKind.SingleClass, new string[0]),

     InlineData("testcase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("testcase", false, SolutionKind.SingleClass, new string[0]),
     InlineData("testcases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
     InlineData("testcases", false, SolutionKind.SingleClass, new string[0]),
     InlineData("TestCase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("TestCase", false, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("TestCases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
     InlineData("TestCases", false, SolutionKind.SingleClass, new[] { "TestCases" }),

     InlineData("test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("test", false, SolutionKind.SingleClassWithSingleMethod, new string[0]),
     InlineData("Test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("Test", false, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

     InlineData("testproperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("testproperty", false, SolutionKind.SingleClassWithSingleProperty, new string[0]),
     InlineData("TestProperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("TestProperty", false, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

     InlineData("testfield", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("testfield", false, SolutionKind.SingleClassWithSingleField, new string[0]),
     InlineData("TestField", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("TestField", false, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

     InlineData("testcase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
     InlineData("testcase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("testcases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
     InlineData("testcases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("TestCase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
     InlineData("TestCase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
     InlineData("TestCases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
     InlineData("TestCases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),

     InlineData("test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("Test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("Test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

     InlineData("testproperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("testproperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
     InlineData("TestProperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("TestProperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

     InlineData("testfield", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("testfield", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
     InlineData("TestField", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("TestField", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

     InlineData("innertestcase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
     InlineData("innertestcase", false, SolutionKind.NestedClass, new string[0]),
     InlineData("InnerTestCase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
     InlineData("InnerTestCase", false, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

     InlineData("testcase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("testcase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),
     InlineData("TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("TestCase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("TestCase1.TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),]

    public async Task FindSourceDeclarationsAsync_Project_Test(string searchTerm, bool ignoreCase, SolutionKind workspaceKind, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithProject(workspaceKind, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, searchTerm, ignoreCase).ConfigureAwait(false);
        Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Test_NullProject()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync((Project)null, "Test", true);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Test_NullString()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, null, true);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, "Test", true, SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    #region FindSourceDeclarationsAsync_Solution

    [Theory,

     InlineData("", true, SolutionKind.SingleClass, new string[0]),
     InlineData(" ", true, SolutionKind.SingleClass, new string[0]),
     InlineData("\u2619", true, SolutionKind.SingleClass, new string[0]),

     InlineData("testcase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("testcase", false, SolutionKind.SingleClass, new string[0]),
     InlineData("testcases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
     InlineData("testcases", false, SolutionKind.SingleClass, new string[0]),
     InlineData("TestCase", true, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("TestCase", false, SolutionKind.SingleClass, new[] { "TestCases.TestCase" }),
     InlineData("TestCases", true, SolutionKind.SingleClass, new[] { "TestCases" }),
     InlineData("TestCases", false, SolutionKind.SingleClass, new[] { "TestCases" }),

     InlineData("test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("test", false, SolutionKind.SingleClassWithSingleMethod, new string[0]),
     InlineData("Test", true, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
     InlineData("Test", false, SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

     InlineData("testproperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("testproperty", false, SolutionKind.SingleClassWithSingleProperty, new string[0]),
     InlineData("TestProperty", true, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
     InlineData("TestProperty", false, SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

     InlineData("testfield", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("testfield", false, SolutionKind.SingleClassWithSingleField, new string[0]),
     InlineData("TestField", true, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
     InlineData("TestField", false, SolutionKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

     InlineData("testcase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
     InlineData("testcase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("testcases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),
     InlineData("testcases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("TestCase", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
     InlineData("TestCase", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
     InlineData("TestCases", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),
     InlineData("TestCases", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),

     InlineData("test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),
     InlineData("test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
     InlineData("Test", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),
     InlineData("Test", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),

     InlineData("testproperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),
     InlineData("testproperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
     InlineData("TestProperty", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),
     InlineData("TestProperty", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),

     InlineData("testfield", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),
     InlineData("testfield", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
     InlineData("TestField", true, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),
     InlineData("TestField", false, SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),

     InlineData("innertestcase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
     InlineData("innertestcase", false, SolutionKind.NestedClass, new string[0]),
     InlineData("InnerTestCase", true, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
     InlineData("InnerTestCase", false, SolutionKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

     InlineData("testcase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("testcase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),
     InlineData("TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("TestCase", false, SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
     InlineData("TestCase1.TestCase", true, SolutionKind.TwoNamespacesWithIdenticalClasses, new string[0]),]

    public async Task FindSourceDeclarationsAsync_Solution_Test(string searchTerm, bool ignoreCase, SolutionKind workspaceKind, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithSolution(workspaceKind, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, searchTerm, ignoreCase).ConfigureAwait(false);
        Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Test_NullProject()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync((Solution)null, "Test", true);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Test_NullString()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, null, true);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, "Test", true, SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    #region FindSourceDeclarationsAsync_Project_Func

    [Theory,
    InlineData(SolutionKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
    InlineData(SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]

    public async Task FindSourceDeclarationsAsync_Project_Func_Test(SolutionKind workspaceKind, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithProject(workspaceKind, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => str.Contains("Test")).ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public async Task FindSourceDeclarationsAsync_Project_Func_Test_AlwaysTruePredicate()
    {
        using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => true).ConfigureAwait(false);
        Verify(SolutionKind.SingleClass, declarations, "TestCases", "TestCases.TestCase");
    }

    [Fact]
    public async Task FindSourceDeclarationsAsync_Project_Func_Test_AlwaysFalsePredicate()
    {
        using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => false).ConfigureAwait(false);
        Verify(SolutionKind.SingleClass, declarations);
    }

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Func_Test_NullProject()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync((Project)null, str => str.Contains("Test"));
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Func_Test_NullPredicate()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, null);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Project_Func_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => str.Contains("Test"), SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    #region FindSourceDeclarationsAsync_Solution_Func

    [Theory,
    InlineData(SolutionKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])", "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
    InlineData(SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]

    public async Task FindSourceDeclarationsAsync_Solution_Func_Test(SolutionKind workspaceKind, string[] expectedResult)
    {
        using var workspace = CreateWorkspaceWithSolution(workspaceKind, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => str.Contains("Test")).ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResult);
    }

    [Fact]
    public async Task FindSourceDeclarationsAsync_Solution_Func_Test_AlwaysTruePredicate()
    {
        using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => true).ConfigureAwait(false);
        Verify(SolutionKind.SingleClass, declarations, "TestCases", "TestCases.TestCase");
    }

    [Fact]
    public async Task FindSourceDeclarationsAsync_Solution_Func_Test_AlwaysFalsePredicate()
    {
        using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => false).ConfigureAwait(false);
        Verify(SolutionKind.SingleClass, declarations);
    }

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Func_Test_NullSolution()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            await SymbolFinder.FindSourceDeclarationsAsync((Solution)null, str => str.Contains("Test"));
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Func_Test_NullPredicate()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            await SymbolFinder.FindSourceDeclarationsAsync(solution, null);
        });

    [Fact]
    public Task FindSourceDeclarationsAsync_Solution_Func_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            await SymbolFinder.FindSourceDeclarationsAsync(solution, str => str.Contains("Test"), SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    #region FindSourceDeclarationsWithPatternAsync_Project

    [Theory,
    InlineData(SolutionKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
    InlineData(SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]

    public async Task FindSourceDeclarationsWithPatternAsync_Project_Test(SolutionKind workspaceKind, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithProject(workspaceKind, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(project, "test").ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResults);
    }

    [Theory,
    InlineData(SolutionKind.SingleClass, "tc", new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, "tc", new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, "tp", new[] { "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, "tf", new[] { "TestCases.TestCase.TestField" }),]

    public async Task FindSourceDeclarationsWithPatternAsync_CamelCase_Project_Test(SolutionKind workspaceKind, string pattern, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithProject(workspaceKind, out var project);
        var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(project, pattern).ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Project_Test_NullProject()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync((Project)null, "test");
        });

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Project_Test_NullPattern()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(project, null);
        });

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Project_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithProject(SolutionKind.SingleClass, out var project);
            var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(project, "test", SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    #region FindSourceDeclarationsWithPatternAsync_Solution

    [Theory,
    InlineData(SolutionKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])", "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
    InlineData(SolutionKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
    InlineData(SolutionKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]

    public async Task FindSourceDeclarationsWithPatternAsync_Solution_Test(SolutionKind workspaceKind, string[] expectedResult)
    {
        using var workspace = CreateWorkspaceWithSolution(workspaceKind, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(solution, "test").ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResult);
    }

    [Theory,
    InlineData(SolutionKind.SingleClass, "tc", new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleMethod, "tc", new[] { "TestCases", "TestCases.TestCase" }),
    InlineData(SolutionKind.SingleClassWithSingleProperty, "tp", new[] { "TestCases.TestCase.TestProperty" }),
    InlineData(SolutionKind.SingleClassWithSingleField, "tf", new[] { "TestCases.TestCase.TestField" }),]

    public async Task FindSourceDeclarationsWithPatternAsync_CamelCase_Solution_Test(SolutionKind workspaceKind, string pattern, string[] expectedResults)
    {
        using var workspace = CreateWorkspaceWithSolution(workspaceKind, out var solution);
        var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(solution, pattern).ConfigureAwait(false);
        Verify(workspaceKind, declarations, expectedResults);
    }

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Solution_Test_NullSolution()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            await SymbolFinder.FindSourceDeclarationsWithPatternAsync((Solution)null, "test");
        });

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Solution_Test_NullPattern()
        => Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            await SymbolFinder.FindSourceDeclarationsWithPatternAsync(solution, null);
        });

    [Fact]
    public Task FindSourceDeclarationsWithPatternAsync_Solution_Test_Cancellation()
        => Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
            await SymbolFinder.FindSourceDeclarationsWithPatternAsync(solution, "test", SymbolFilter.All, new CancellationToken(true));
        });

    #endregion

    [Fact]
    public async Task TestSymbolTreeInfoSerialization()
    {
        using var workspace = CreateWorkspaceWithSolution(SolutionKind.SingleClass, out var solution);
        var project = solution.Projects.First();

        // create symbol tree info from assembly
        var info = await SymbolTreeInfo.CreateSourceSymbolTreeInfoAsync(
            project, Checksum.Null, cancellationToken: CancellationToken.None);

        using var writerStream = new MemoryStream();
        using (var writer = new ObjectWriter(writerStream, leaveOpen: true))
        {
            info.WriteTo(writer);
        }

        using var readerStream = new MemoryStream(writerStream.ToArray());
        using var reader = ObjectReader.TryGetReader(readerStream);
        var readInfo = SymbolTreeInfo.TestAccessor.ReadSymbolTreeInfo(reader, Checksum.Null);

        info.AssertEquivalentTo(readInfo);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/7941")]
    public async Task FindDeclarationsInErrorSymbolsDoesntCrash()
    {

        // create solution
        var pid = ProjectId.CreateNewId();
        using var workspace = CreateWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(pid, "VBProject", "VBProject", LanguageNames.VisualBasic)
            .AddMetadataReference(pid, MscorlibRef);
        var did = DocumentId.CreateNewId(pid);
        solution = solution.AddDocument(did, "VBDocument.vb", SourceText.From("""
            ' missing `Class` keyword
            Public Class1
                Public Event MyEvent(ByVal a As String)
            End Class
            """));
        var project = solution.Projects.Single();

        // perform the search
        var foundDeclarations = await SymbolFinder.FindDeclarationsAsync(project, name: "MyEvent", ignoreCase: true);
        Assert.Equal(1, foundDeclarations.Count());
        Assert.False(foundDeclarations.Any(decl => decl == null));
    }
}
