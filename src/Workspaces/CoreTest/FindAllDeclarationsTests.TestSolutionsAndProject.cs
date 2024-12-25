// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindAllDeclarationsTests
    {
        private readonly ITestOutputHelper _logger;

        public FindAllDeclarationsTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        private static void Verify(string searchTerm, bool respectCase, SolutionKind workspaceKind, IEnumerable<ISymbol> declarations, params string[] expectedResults)
        {
            var actualResultCount = declarations.Count();
            var expectedResultCount = expectedResults.Length;
            Assert.True(expectedResultCount == actualResultCount,
                string.Format("Search term '{0}' expected '{1}' results, found '{2}.  Ignore case was set to '{3}', Workspace {4} was used",
                    searchTerm,
                    expectedResultCount,
                    actualResultCount,
                    respectCase,
                    Enum.GetName(typeof(SolutionKind), workspaceKind)));
            if (actualResultCount > 0)
            {
                VerifyResults(declarations, expectedResults);
            }
        }

        private static void Verify(SolutionKind workspaceKind, IEnumerable<ISymbol> declarations, params string[] expectedResults)
        {
            var actualResultCount = declarations.Count();
            var expectedResultCount = expectedResults.Length;
            Assert.True(expectedResultCount == actualResultCount,
                string.Format("Expected '{0}' results, found '{1}. Workspace {2} was used",
                    expectedResultCount,
                    actualResultCount,
                    Enum.GetName(typeof(SolutionKind), workspaceKind)));
            if (actualResultCount > 0)
            {
                VerifyResults(declarations, expectedResults);
            }
        }

        private static void VerifyResults(IEnumerable<ISymbol> declarations, string[] expectedResults)
        {
            declarations = declarations.OrderBy(d => d.ToString());
            expectedResults = [.. expectedResults.OrderBy(r => r)];

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actualResult = declarations.ElementAt(i).ToString();
                var expectedResult = expectedResults[i];
                Assert.True(
                    string.Equals(actualResult, expectedResult, StringComparison.Ordinal),
                    string.Format("Expected result to be {0} was {1}", expectedResult, actualResult));
            }
        }

        private Workspace CreateWorkspace(TestHost testHost = TestHost.OutOfProcess)
        {
            var composition = FeaturesTestCompositions.Features.WithTestHostParts(testHost);
            var workspace = new AdhocWorkspace(composition.GetHostServices());

            if (testHost == TestHost.OutOfProcess)
            {
                var remoteHostProvider = (InProcRemoteHostClientProvider)workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
                remoteHostProvider.TraceListener = new XunitTraceListener(_logger);
            }

            return workspace;
        }

        private Workspace CreateWorkspaceWithSingleProjectSolution(TestHost testHost, string[] sourceTexts, out Solution solution)
        {
            var pid = ProjectId.CreateNewId();
            var workspace = CreateWorkspace(testHost);

            solution = workspace.CurrentSolution
                    .AddProject(pid, "TestCases", "TestCases", LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
            for (var i = 0; i < sourceTexts.Length; i++)
            {
                var did = DocumentId.CreateNewId(pid);
                solution = solution.AddDocument(did, "goo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return workspace;
        }

        private Workspace CreateWorkspaceWithMultipleProjectSolution(TestHost testHost, string[] sourceTexts, out Solution solution)
        {
            var workspace = CreateWorkspace(testHost);
            solution = workspace.CurrentSolution;
            for (var i = 0; i < sourceTexts.Length; i++)
            {
                var pid = ProjectId.CreateNewId();
                var did = DocumentId.CreateNewId(pid);
                solution = solution
                    .AddProject(pid, "TestCases" + i, "TestCases" + i, LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
                solution = solution.AddDocument(did, "goo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return workspace;
        }

        private Workspace CreateWorkspaceWithSolution(SolutionKind solutionKind, out Solution solution, TestHost testHost = TestHost.OutOfProcess)
            => solutionKind switch
            {
                SolutionKind.SingleClass => CreateWorkspaceWithSingleProjectSolution(testHost, [SingleClass], out solution),
                SolutionKind.SingleClassWithSingleMethod => CreateWorkspaceWithSingleProjectSolution(testHost, [SingleClassWithSingleMethod], out solution),
                SolutionKind.SingleClassWithSingleProperty => CreateWorkspaceWithSingleProjectSolution(testHost, [SingleClassWithSingleProperty], out solution),
                SolutionKind.SingleClassWithSingleField => CreateWorkspaceWithSingleProjectSolution(testHost, [SingleClassWithSingleField], out solution),
                SolutionKind.TwoProjectsEachWithASingleClassWithSingleMethod => CreateWorkspaceWithMultipleProjectSolution(testHost, [SingleClassWithSingleMethod, SingleClassWithSingleMethod], out solution),
                SolutionKind.TwoProjectsEachWithASingleClassWithSingleProperty => CreateWorkspaceWithMultipleProjectSolution(testHost, [SingleClassWithSingleProperty, SingleClassWithSingleProperty], out solution),
                SolutionKind.TwoProjectsEachWithASingleClassWithSingleField => CreateWorkspaceWithMultipleProjectSolution(testHost, [SingleClassWithSingleField, SingleClassWithSingleField], out solution),
                SolutionKind.NestedClass => CreateWorkspaceWithSingleProjectSolution(testHost, [NestedClass], out solution),
                SolutionKind.TwoNamespacesWithIdenticalClasses => CreateWorkspaceWithSingleProjectSolution(testHost, [Namespace1, Namespace2], out solution),
                _ => throw ExceptionUtilities.UnexpectedValue(solutionKind),
            };

        private Workspace CreateWorkspaceWithProject(SolutionKind solutionKind, out Project project, TestHost testHost = TestHost.OutOfProcess)
        {
            var workspace = CreateWorkspaceWithSolution(solutionKind, out var solution, testHost);
            project = solution.Projects.First();
            return workspace;
        }

        public enum SolutionKind
        {
            SingleClass,
            SingleClassWithSingleMethod,
            SingleClassWithSingleProperty,
            SingleClassWithSingleField,
            TwoProjectsEachWithASingleClassWithSingleMethod,
            TwoProjectsEachWithASingleClassWithSingleProperty,
            TwoProjectsEachWithASingleClassWithSingleField,
            NestedClass,
            TwoNamespacesWithIdenticalClasses
        }

        private const string SingleClass =
    @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCases
{
    class TestCase
    {

    }
}
            ";
        private const string SingleClassWithSingleMethod =
    @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCases
{
    class TestCase
    {
        static void Test(string[] args)
        {

        }
    }
}
            ";
        private const string SingleClassWithSingleProperty =
    @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCases
{
    class TestCase
    {
        public int TestProperty{ get; set; }
    }
}
            ";
        private const string SingleClassWithSingleField =
    @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCases
{
    class TestCase
    {
        private int TestField = 0;
    }
}
            ";
        private const string NestedClass =
    @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCases
{
    class TestCase
    {
        class InnerTestCase
        {

        }
    }
}
            ";

        private const string Namespace1 =
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCase1
{
    class TestCase
    {
    }
}
            ";

        private const string Namespace2 =
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCase2
{
    class TestCase
    {
    }
}
            ";
    }
}
