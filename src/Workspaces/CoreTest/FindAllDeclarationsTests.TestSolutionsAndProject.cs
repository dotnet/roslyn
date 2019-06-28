// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindAllDeclarationsTests
    {
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

        private static void VerifyInnerExceptionArgumentNull(AggregateException ex, string argName)
        {
            var exception = ex.InnerException as ArgumentNullException;
            Assert.True(exception != null, string.Format("Expected InnerException to be 'System.ArgumentNullException' was '{0}'", ex.InnerException.ToString()));
            Assert.True(exception.ParamName.Contains(argName), string.Format("Expected InnerException ParamName to contain '{0}', actual ParamName is: '{1}'", argName, exception.ParamName));
        }

        private static void VerifyInnerExceptionIsType<T>(Exception ex) where T : Exception
        {
            Assert.True(ex.InnerException is T, string.Format("Expected InnerException to be '{0}' was '{1}'", typeof(T).Name, ex.InnerException.ToString()));
        }

        private static Solution CreateSolution()
        {
            return new AdhocWorkspace().CurrentSolution;
        }

        private static Solution GetSingleProjectSolution(params string[] sourceTexts)
        {
            var pid = ProjectId.CreateNewId();
            var solution = CreateSolution()
                    .AddProject(pid, "TestCases", "TestCases", LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
            for (var i = 0; i < sourceTexts.Length; i++)
            {
                var did = DocumentId.CreateNewId(pid);
                solution = solution.AddDocument(did, "goo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return solution;
        }

        private static Solution GetMultipleProjectSolution(params string[] sourceTexts)
        {
            var solution = CreateSolution();
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

        private static Solution GetSolution(WorkspaceKind workspaceKind)
            => workspaceKind switch
            {
                WorkspaceKind.SingleClass => GetSingleProjectSolution(SingleClass),
                WorkspaceKind.SingleClassWithSingleMethod => GetSingleProjectSolution(SingleClassWithSingleMethod),
                WorkspaceKind.SingleClassWithSingleProperty => GetSingleProjectSolution(SingleClassWithSingleProperty),
                WorkspaceKind.SingleClassWithSingleField => GetSingleProjectSolution(SingleClassWithSingleField),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod => GetMultipleProjectSolution(SingleClassWithSingleMethod, SingleClassWithSingleMethod),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty => GetMultipleProjectSolution(SingleClassWithSingleProperty, SingleClassWithSingleProperty),
                WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField => GetMultipleProjectSolution(SingleClassWithSingleField, SingleClassWithSingleField),
                WorkspaceKind.NestedClass => GetSingleProjectSolution(NestedClass),
                WorkspaceKind.TwoNamespacesWithIdenticalClasses => GetSingleProjectSolution(Namespace1, Namespace2),
                _ => null,
            };

        private static Project GetProject(WorkspaceKind workspaceKind)
        {
            return GetSolution(workspaceKind).Projects.First();
        }

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
