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
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindAllDeclarationsTests
    {
        private readonly ITestOutputHelper _logger;
        private AdhocWorkspace _lazyWorkspace;

        public FindAllDeclarationsTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        public override void Dispose()
        {
            base.Dispose();
            _lazyWorkspace?.Dispose();
        }

        private static void Verify(string searchTerm, bool respectCase, WorkspaceKind workspaceKind, IEnumerable<ISymbol> declarations, params string[] expectedResults)
        {
            var actualResultCount = declarations.Count();
            var expectedResultCount = expectedResults.Length;
            Assert.True(expectedResultCount == actualResultCount,
                string.Format("Search term '{0}' expected '{1}' results, found '{2}.  Ignore case was set to '{3}', Workspace {4} was used",
                    searchTerm,
                    expectedResultCount,
                    actualResultCount,
                    respectCase,
                    Enum.GetName(typeof(WorkspaceKind), workspaceKind)));
            if (actualResultCount > 0)
            {
                VerifyResults(declarations, expectedResults);
            }
        }

        private static void Verify(WorkspaceKind workspaceKind, IEnumerable<ISymbol> declarations, params string[] expectedResults)
        {
            var actualResultCount = declarations.Count();
            var expectedResultCount = expectedResults.Length;
            Assert.True(expectedResultCount == actualResultCount,
                string.Format("Expected '{0}' results, found '{1}. Workspace {2} was used",
                    expectedResultCount,
                    actualResultCount,
                    Enum.GetName(typeof(WorkspaceKind), workspaceKind)));
            if (actualResultCount > 0)
            {
                VerifyResults(declarations, expectedResults);
            }
        }

        private static void VerifyResults(IEnumerable<ISymbol> declarations, string[] expectedResults)
        {
            declarations = declarations.OrderBy(d => d.ToString());
            expectedResults = expectedResults.OrderBy(r => r).ToArray();

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actualResult = declarations.ElementAt(i).ToString();
                var expectedResult = expectedResults[i];
                Assert.True(
                    string.Equals(actualResult, expectedResult, StringComparison.Ordinal),
                    string.Format("Expected result to be {0} was {1}", expectedResult, actualResult));
            }
        }

        private Solution CreateSolution(TestHost testHost = TestHost.InProcess)
        {
            Assert.True(_lazyWorkspace == null, "Only one workspace can be created by a single test");

            var composition = FeaturesTestCompositions.Features.WithTestHostParts(testHost);
            _lazyWorkspace = new AdhocWorkspace(composition.GetHostServices());

            if (testHost == TestHost.OutOfProcess)
            {
                var remoteHostProvider = (InProcRemoteHostClientProvider)_lazyWorkspace.Services.GetRequiredService<IRemoteHostClientProvider>();
                remoteHostProvider.TraceListener = new XunitTraceListener(_logger);
            }

            return _lazyWorkspace.CurrentSolution;
        }

        private Solution GetSingleProjectSolution(TestHost testHost, string[] sourceTexts)
        {
            var pid = ProjectId.CreateNewId();
            var solution = CreateSolution(testHost)
                    .AddProject(pid, "TestCases", "TestCases", LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
            for (var i = 0; i < sourceTexts.Length; i++)
            {
                var did = DocumentId.CreateNewId(pid);
                solution = solution.AddDocument(did, "goo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return solution;
        }

        private Solution GetMultipleProjectSolution(TestHost testHost, string[] sourceTexts)
        {
            var solution = CreateSolution(testHost);
            for (var i = 0; i < sourceTexts.Length; i++)
            {
                var pid = ProjectId.CreateNewId();
                var did = DocumentId.CreateNewId(pid);
                solution = solution
                    .AddProject(pid, "TestCases" + i, "TestCases" + i, LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
                solution = solution.AddDocument(did, "goo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return solution;
        }

        private Solution GetSolution(WorkspaceKind workspaceKind, TestHost testHost = TestHost.InProcess)
            => workspaceKind switch
            {
                WorkspaceKind.SingleClass => GetSingleProjectSolution(testHost, new[] { SingleClass }),
                WorkspaceKind.SingleClassWithSingleMethod => GetSingleProjectSolution(testHost, new[] { SingleClassWithSingleMethod }),
                WorkspaceKind.SingleClassWithSingleProperty => GetSingleProjectSolution(testHost, new[] { SingleClassWithSingleProperty }),
                WorkspaceKind.SingleClassWithSingleField => GetSingleProjectSolution(testHost, new[] { SingleClassWithSingleField }),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod => GetMultipleProjectSolution(testHost, new[] { SingleClassWithSingleMethod, SingleClassWithSingleMethod }),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty => GetMultipleProjectSolution(testHost, new[] { SingleClassWithSingleProperty, SingleClassWithSingleProperty }),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField => GetMultipleProjectSolution(testHost, new[] { SingleClassWithSingleField, SingleClassWithSingleField }),
                WorkspaceKind.NestedClass => GetSingleProjectSolution(testHost, new[] { NestedClass }),
                WorkspaceKind.TwoNamespacesWithIdenticalClasses => GetSingleProjectSolution(testHost, new[] { Namespace1, Namespace2 }),
                _ => null,
            };

        private Project GetProject(WorkspaceKind workspaceKind, TestHost testHost = TestHost.InProcess)
            => GetSolution(workspaceKind, testHost).Projects.First();

        public enum WorkspaceKind
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
