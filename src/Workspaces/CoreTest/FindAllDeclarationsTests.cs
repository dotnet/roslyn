// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindAllDeclarationsTests : TestBase
    {
        #region FindDeclarationsAsync

        [Theory,

        InlineData("", true, WorkspaceKind.SingleClass, new string[0]),
        InlineData(" ", true, WorkspaceKind.SingleClass, new string[0]),
        InlineData("\u2619", true, WorkspaceKind.SingleClass, new string[0]),

        InlineData("testcase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
        InlineData("testcase", false, WorkspaceKind.SingleClass, new string[0]),
        InlineData("testcases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
        InlineData("testcases", false, WorkspaceKind.SingleClass, new string[0]),
        InlineData("TestCase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
        InlineData("TestCase", false, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
        InlineData("TestCases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
        InlineData("TestCases", false, WorkspaceKind.SingleClass, new[] { "TestCases" }),

        InlineData("test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
        InlineData("test", false, WorkspaceKind.SingleClassWithSingleMethod, new string[0]),
        InlineData("Test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
        InlineData("Test", false, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

        InlineData("testproperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
        InlineData("testproperty", false, WorkspaceKind.SingleClassWithSingleProperty, new string[0]),
        InlineData("TestProperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
        InlineData("TestProperty", false, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

        InlineData("testfield", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
        InlineData("testfield", false, WorkspaceKind.SingleClassWithSingleField, new string[0]),
        InlineData("TestField", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
        InlineData("TestField", false, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),


        InlineData("testcase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
        InlineData("testcase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
        InlineData("testcases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
        InlineData("testcases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
        InlineData("TestCase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
        InlineData("TestCase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
        InlineData("TestCases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
        InlineData("TestCases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),

        InlineData("test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
        InlineData("test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
        InlineData("Test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
        InlineData("Test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

        InlineData("testproperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
        InlineData("testproperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
        InlineData("TestProperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
        InlineData("TestProperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

        InlineData("testfield", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
        InlineData("testfield", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
        InlineData("TestField", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
        InlineData("TestField", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

        InlineData("innertestcase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
        InlineData("innertestcase", false, WorkspaceKind.NestedClass, new string[0]),
        InlineData("InnerTestCase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
        InlineData("InnerTestCase", false, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

        InlineData("testcase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
        InlineData("testcase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),
        InlineData("TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
        InlineData("TestCase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
        InlineData("TestCase1.TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),]
        public static async Task FindDeclarationsAsync_Test(string searchTerm, bool ignoreCase, WorkspaceKind workspaceKind, string[] expectedResults)
        {
            var project = GetProject(workspaceKind);
            var declarations = await SymbolFinder.FindDeclarationsAsync(project, searchTerm, ignoreCase).ConfigureAwait(false);
            Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
        }

        [Fact]
        public static async Task FindDeclarationsAsync_Test_NullProject()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var declarations = await SymbolFinder.FindDeclarationsAsync(null, "Test", true);
            });
        }

        [Fact]
        public static async Task FindDeclarationsAsync_Test_NullString()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var project = GetProject(WorkspaceKind.SingleClass);
                var declarations = await SymbolFinder.FindDeclarationsAsync(project, null, true);
            });
        }

        [Fact]
        public static async Task FindDeclarationsAsync_Test_Cancellation()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var project = GetProject(WorkspaceKind.SingleClass);
                var declarations = await SymbolFinder.FindDeclarationsAsync(project, "Test", true, SymbolFilter.All, cts.Token);
            });
        }

        [Fact, WorkItem(1094411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")]
        public static async Task FindDeclarationsAsync_Metadata()
        {
            var solution = CreateSolution();
            var csharpId = ProjectId.CreateNewId();
            solution = solution
                .AddProject(csharpId, "CSharp", "CSharp", LanguageNames.CSharp)
                .AddMetadataReference(csharpId, MscorlibRef);

            var vbId = ProjectId.CreateNewId();
            solution = solution
                .AddProject(vbId, "VB", "VB", LanguageNames.VisualBasic)
                .AddMetadataReference(vbId, MscorlibRef);

            var csharpResult = await SymbolFinder.FindDeclarationsAsync(solution.GetProject(csharpId), "BackgroundColor", ignoreCase: false);
            Assert.True(csharpResult.Count() > 0);

            var vbResult = await SymbolFinder.FindDeclarationsAsync(solution.GetProject(vbId), "BackgroundColor", ignoreCase: true);
            Assert.True(vbResult.Count() > 0);
        }

        [Fact, WorkItem(6616, "https://github.com/dotnet/roslyn/issues/6616")]
        public static async Task FindDeclarationsAsync_PreviousSubmission()
        {
            var solution = CreateSolution();

            var submission0Id = ProjectId.CreateNewId();
            var submission0DocId = DocumentId.CreateNewId(submission0Id);
            const string submission0Name = "Submission#0";
            solution = solution
                .AddProject(submission0Id, submission0Name, submission0Name, LanguageNames.CSharp)
                .AddMetadataReference(submission0Id, MscorlibRef)
                .AddDocument(submission0DocId, submission0Name, @"
public class Outer
{
    public class Inner
    {
    }
}
");

            var submission1Id = ProjectId.CreateNewId();
            var submission1DocId = DocumentId.CreateNewId(submission1Id);
            const string submission1Name = "Submission#1";
            solution = solution
                .AddProject(submission1Id, submission1Name, submission1Name, LanguageNames.CSharp)
                .AddMetadataReference(submission1Id, MscorlibRef)
                .AddProjectReference(submission1Id, new ProjectReference(submission0Id))
                .AddDocument(submission1DocId, submission1Name, @"
Inner i;
");

            var actualSymbol = (await SymbolFinder.FindDeclarationsAsync(solution.GetProject(submission1Id), "Inner", ignoreCase: false)).SingleOrDefault();
            var expectedSymbol = (await solution.GetProject(submission0Id).GetCompilationAsync()).GlobalNamespace.GetMembers("Outer").SingleOrDefault().GetMembers("Inner").SingleOrDefault();
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        #endregion

        #region FindSourceDeclarationsAsync_Project

        [Theory,

         InlineData("", true, WorkspaceKind.SingleClass, new string[0]),
         InlineData(" ", true, WorkspaceKind.SingleClass, new string[0]),
         InlineData("\u2619", true, WorkspaceKind.SingleClass, new string[0]),

         InlineData("testcase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.SingleClass, new string[0]),
         InlineData("testcases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
         InlineData("testcases", false, WorkspaceKind.SingleClass, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("TestCases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
         InlineData("TestCases", false, WorkspaceKind.SingleClass, new[] { "TestCases" }),

         InlineData("test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("test", false, WorkspaceKind.SingleClassWithSingleMethod, new string[0]),
         InlineData("Test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("Test", false, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

         InlineData("testproperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("testproperty", false, WorkspaceKind.SingleClassWithSingleProperty, new string[0]),
         InlineData("TestProperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("TestProperty", false, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

         InlineData("testfield", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("testfield", false, WorkspaceKind.SingleClassWithSingleField, new string[0]),
         InlineData("TestField", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("TestField", false, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),


         InlineData("testcase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("testcases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
         InlineData("testcases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase" }),
         InlineData("TestCases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),
         InlineData("TestCases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases" }),

         InlineData("test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("Test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("Test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

         InlineData("testproperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("testproperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
         InlineData("TestProperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("TestProperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

         InlineData("testfield", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("testfield", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
         InlineData("TestField", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("TestField", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),

         InlineData("innertestcase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
         InlineData("innertestcase", false, WorkspaceKind.NestedClass, new string[0]),
         InlineData("InnerTestCase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
         InlineData("InnerTestCase", false, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

         InlineData("testcase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("TestCase1.TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),]
        public static async Task FindSourceDeclarationsAsync_Project_Test(string searchTerm, bool ignoreCase, WorkspaceKind workspaceKind, string[] expectedResults)
        {
            var project = GetProject(workspaceKind);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, searchTerm, ignoreCase).ConfigureAwait(false);
            Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Project_Test_NullProject()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var declarations = await SymbolFinder.FindSourceDeclarationsAsync((Project)null, "Test", true);
            });
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Project_Test_NullString()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var project = GetProject(WorkspaceKind.SingleClass);
                var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, null, true);
            });
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Project_Test_Cancellation()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                var project = GetProject(WorkspaceKind.SingleClass);
                cts.Cancel();
                var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, "Test", true, SymbolFilter.All, cts.Token);
            });
        }

        #endregion

        #region FindSourceDeclarationsAsync_Solution

        [Theory,

         InlineData("", true, WorkspaceKind.SingleClass, new string[0]),
         InlineData(" ", true, WorkspaceKind.SingleClass, new string[0]),
         InlineData("\u2619", true, WorkspaceKind.SingleClass, new string[0]),

         InlineData("testcase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.SingleClass, new string[0]),
         InlineData("testcases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
         InlineData("testcases", false, WorkspaceKind.SingleClass, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.SingleClass, new[] { "TestCases.TestCase" }),
         InlineData("TestCases", true, WorkspaceKind.SingleClass, new[] { "TestCases" }),
         InlineData("TestCases", false, WorkspaceKind.SingleClass, new[] { "TestCases" }),

         InlineData("test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("test", false, WorkspaceKind.SingleClassWithSingleMethod, new string[0]),
         InlineData("Test", true, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),
         InlineData("Test", false, WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])" }),

         InlineData("testproperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("testproperty", false, WorkspaceKind.SingleClassWithSingleProperty, new string[0]),
         InlineData("TestProperty", true, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),
         InlineData("TestProperty", false, WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty" }),

         InlineData("testfield", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("testfield", false, WorkspaceKind.SingleClassWithSingleField, new string[0]),
         InlineData("TestField", true, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),
         InlineData("TestField", false, WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases.TestCase.TestField" }),


         InlineData("testcase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("testcases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),
         InlineData("testcases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase", "TestCases.TestCase" }),
         InlineData("TestCases", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),
         InlineData("TestCases", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases" }),

         InlineData("test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),
         InlineData("test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new string[0]),
         InlineData("Test", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),
         InlineData("Test", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases.TestCase.Test(string[])", "TestCases.TestCase.Test(string[])" }),

         InlineData("testproperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),
         InlineData("testproperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new string[0]),
         InlineData("TestProperty", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),
         InlineData("TestProperty", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases.TestCase.TestProperty", "TestCases.TestCase.TestProperty" }),

         InlineData("testfield", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),
         InlineData("testfield", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new string[0]),
         InlineData("TestField", true, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),
         InlineData("TestField", false, WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases.TestCase.TestField", "TestCases.TestCase.TestField" }),

         InlineData("innertestcase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
         InlineData("innertestcase", false, WorkspaceKind.NestedClass, new string[0]),
         InlineData("InnerTestCase", true, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),
         InlineData("InnerTestCase", false, WorkspaceKind.NestedClass, new[] { "TestCases.TestCase.InnerTestCase" }),

         InlineData("testcase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("testcase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),
         InlineData("TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("TestCase", false, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1.TestCase", "TestCase2.TestCase" }),
         InlineData("TestCase1.TestCase", true, WorkspaceKind.TwoNamespacesWithIdenticalClasses, new string[0]),]
        public static async Task FindSourceDeclarationsAsync_Solution_Test(string searchTerm, bool ignoreCase, WorkspaceKind workspaceKind, string[] expectedResults)
        {
            var solution = GetSolution(workspaceKind);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, searchTerm, ignoreCase).ConfigureAwait(false);
            Verify(searchTerm, ignoreCase, workspaceKind, declarations, expectedResults);
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Solution_Test_NullProject()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var declarations = await SymbolFinder.FindSourceDeclarationsAsync((Solution)null, "Test", true);
            });
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Solution_Test_NullString()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var solution = GetSolution(WorkspaceKind.SingleClass);
                var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, null, true);
            });
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Solution_Test_Cancellation()
        {
            Assert.Throws<AggregateException>(() =>
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    var solution = GetSolution(WorkspaceKind.SingleClass);
                    cts.Cancel();
                    var declarations = SymbolFinder.FindSourceDeclarationsAsync(solution, "Test", true, SymbolFilter.All, cts.Token).Result;
                }
                catch (AggregateException ex)
                {
                    VerifyInnerExceptionIsType<OperationCanceledException>(ex);
                    throw;
                }
            });
        }

        #endregion

        #region FindSourceDeclarationsAsync_Project_Func

        [Theory,
        InlineData(WorkspaceKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
        InlineData(WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
        InlineData(WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
        InlineData(WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
        InlineData(WorkspaceKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
        InlineData(WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]
        public static async Task FindSourceDeclarationsAsync_Project_Func_Test(WorkspaceKind workspaceKind, string[] expectedResults)
        {
            var project = GetProject(workspaceKind);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => str.Contains("Test")).ConfigureAwait(false);
            Verify(workspaceKind, declarations, expectedResults);
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Project_Func_Test_AlwaysTruePredicate()
        {
            var project = GetProject(WorkspaceKind.SingleClass);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => true).ConfigureAwait(false);
            Verify(WorkspaceKind.SingleClass, declarations, "TestCases", "TestCases.TestCase");
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Project_Func_Test_AlwaysFalsePredicate()
        {
            var project = GetProject(WorkspaceKind.SingleClass);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(project, str => false).ConfigureAwait(false);
            Verify(WorkspaceKind.SingleClass, declarations);
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Project_Func_Test_NullProject()
        {
            Assert.Throws<AggregateException>(() =>
            {
                try
                {
                    var declarations = SymbolFinder.FindSourceDeclarationsAsync((Project)null, str => str.Contains("Test")).Result;
                }
                catch (AggregateException ex)
                {
                    VerifyInnerExceptionArgumentNull(ex, "project");
                    throw;
                }
            });
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Project_Func_Test_NullPredicate()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var project = GetProject(WorkspaceKind.SingleClass);
                var declarations = SymbolFinder.FindSourceDeclarationsAsync(project, null).Result;
            });
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Project_Func_Test_Cancellation()
        {
            Assert.Throws<AggregateException>(() =>
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    var project = GetProject(WorkspaceKind.SingleClass);
                    cts.Cancel();
                    var declarations = SymbolFinder.FindSourceDeclarationsAsync(project, str => str.Contains("Test"), SymbolFilter.All, cts.Token).Result;
                }
                catch (AggregateException ex)
                {
                    VerifyInnerExceptionIsType<OperationCanceledException>(ex);
                    throw;
                }
            });
        }

        #endregion

        #region FindSourceDeclarationsAsync_Solution_Func

        [Theory,
        InlineData(WorkspaceKind.SingleClass, new[] { "TestCases", "TestCases.TestCase" }),
        InlineData(WorkspaceKind.SingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
        InlineData(WorkspaceKind.SingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
        InlineData(WorkspaceKind.SingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])", "TestCases", "TestCases.TestCase", "TestCases.TestCase.Test(string[])" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestProperty" }),
        InlineData(WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField", "TestCases", "TestCases.TestCase", "TestCases.TestCase.TestField" }),
        InlineData(WorkspaceKind.NestedClass, new[] { "TestCases", "TestCases.TestCase", "TestCases.TestCase.InnerTestCase" }),
        InlineData(WorkspaceKind.TwoNamespacesWithIdenticalClasses, new[] { "TestCase1", "TestCase1.TestCase", "TestCase2.TestCase", "TestCase2" }),]
        public static async Task FindSourceDeclarationsAsync_Solution_Func_Test(WorkspaceKind workspaceKind, string[] expectedResult)
        {
            var solution = GetSolution(workspaceKind);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => str.Contains("Test")).ConfigureAwait(false);
            Verify(workspaceKind, declarations, expectedResult);
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Solution_Func_Test_AlwaysTruePredicate()
        {
            var solution = GetSolution(WorkspaceKind.SingleClass);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => true).ConfigureAwait(false);
            Verify(WorkspaceKind.SingleClass, declarations, "TestCases", "TestCases.TestCase");
        }

        [Fact]
        public static async Task FindSourceDeclarationsAsync_Solution_Func_Test_AlwaysFalsePredicate()
        {
            var solution = GetSolution(WorkspaceKind.SingleClass);
            var declarations = await SymbolFinder.FindSourceDeclarationsAsync(solution, str => false).ConfigureAwait(false);
            Verify(WorkspaceKind.SingleClass, declarations);
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Solution_Func_Test_NullSolution()
        {
            Assert.Throws<AggregateException>(() =>
            {
                try
                {
                    var declarations = SymbolFinder.FindSourceDeclarationsAsync((Solution)null, str => str.Contains("Test")).Result;
                }
                catch (AggregateException ex)
                {
                    VerifyInnerExceptionArgumentNull(ex, "solution");
                    throw;
                }
            });
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Solution_Func_Test_NullPredicate()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var solution = GetSolution(WorkspaceKind.SingleClass);
                var declarations = SymbolFinder.FindSourceDeclarationsAsync(solution, null).Result;
            });
        }

        [Fact]
        public static void FindSourceDeclarationsAsync_Solution_Func_Test_Cancellation()
        {
            Assert.Throws<AggregateException>(() =>
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    var solution = GetSolution(WorkspaceKind.SingleClass);
                    cts.Cancel();
                    var declarations = SymbolFinder.FindSourceDeclarationsAsync(solution, str => str.Contains("Test"), SymbolFilter.All, cts.Token).Result;
                }
                catch (AggregateException ex)
                {
                    VerifyInnerExceptionIsType<OperationCanceledException>(ex);
                    throw;
                }
            });
        }

        [Fact]
        public static async Task TestSymbolTreeInfoSerialization()
        {
            var solution = GetSolution(WorkspaceKind.SingleClass);
            var compilation = await solution.Projects.First().GetCompilationAsync();
            var assembly = compilation.GetSpecialType(SpecialType.System_Byte).ContainingAssembly;
            ////var assembly = compilation.Assembly;

            // create symbol tree info from assembly
            var version = VersionStamp.Create();
            var info = SymbolTreeInfo.Create(version, assembly, CancellationToken.None);

            using (var writerStream = new MemoryStream())
            {
                using (var writer = new ObjectWriter(writerStream))
                {
                    info.WriteTo(writer);
                }

                using (var readerStream = new MemoryStream(writerStream.ToArray()))
                using (var reader = new ObjectReader(readerStream))
                {
                    var readInfo = SymbolTreeInfo.ReadFrom(reader);

                    Assert.True(info.IsEquivalent(readInfo));
                }
            }
        }

        #endregion

        [Fact, WorkItem(7941, "https://github.com/dotnet/roslyn/pull/7941")]
        public async Task FindDeclarationsInErrorSymbolsDoesntCrash()
        {
            var source = @"
' missing `Class` keyword
Public Class1
    Public Event MyEvent(ByVal a As String)
End Class
";

            // create solution
            var pid = ProjectId.CreateNewId();
            var solution = CreateSolution()
                .AddProject(pid, "VBProject", "VBProject", LanguageNames.VisualBasic)
                .AddMetadataReference(pid, MscorlibRef);
            var did = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did, "VBDocument.vb", SourceText.From(source));
            var project = solution.Projects.Single();

            // perform the search
            var foundDeclarations = await SymbolFinder.FindDeclarationsAsync(project, name: "MyEvent", ignoreCase: true);
            Assert.Equal(1, foundDeclarations.Count());
            Assert.False(foundDeclarations.Any(decl => decl == null));
        }
    }
}
