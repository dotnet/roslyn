﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actualResult = declarations.ElementAt(i).ToString();
                var expectedResult = expectedResults[i];
                Assert.True(
                    string.Equals(actualResult, expectedResult, StringComparison.Ordinal),
                    string.Format("Expected result to be {0} was {1}", expectedResult, actualResult));
            }
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
            for (int i = 0; i < sourceTexts.Length; i++)
            {
                var did = DocumentId.CreateNewId(pid);
                solution = solution.AddDocument(did, "foo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return solution;
        }

        private static Solution GetMultipleProjectSolution(params string[] sourceTexts)
        {
            var solution = CreateSolution();
            for (int i = 0; i < sourceTexts.Length; i++)
            {
                var pid = ProjectId.CreateNewId();
                var did = DocumentId.CreateNewId(pid);
                solution = solution
                    .AddProject(pid, "TestCases" + i, "TestCases" + i, LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef);
                solution = solution.AddDocument(did, "foo" + i + ".cs", SourceText.From(sourceTexts[i]));
            }

            return solution;
        }

        private static Solution GetSolution(WorkspaceKind workspaceKind)
        {
            switch (workspaceKind)
            {
                case WorkspaceKind.SingleClass:
                    return GetSingleProjectSolution(SingleClass);
                case WorkspaceKind.SingleClassWithSingleMethod:
                    return GetSingleProjectSolution(SingleClassWithSingleMethod);
                case WorkspaceKind.SingleClassWithSingleProperty:
                    return GetSingleProjectSolution(SingleClassWithSingleProperty);
                case WorkspaceKind.SingleClassWithSingleField:
                    return GetSingleProjectSolution(SingleClassWithSingleField);
                case WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleMethod:
                    return GetMultipleProjectSolution(SingleClassWithSingleMethod, SingleClassWithSingleMethod);
                case WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleProperty:
                    return GetMultipleProjectSolution(SingleClassWithSingleProperty, SingleClassWithSingleProperty);
                case WorkspaceKind.TwoProjectsEachWithASingleClassWithSingleField:
                    return GetMultipleProjectSolution(SingleClassWithSingleField, SingleClassWithSingleField);
                case WorkspaceKind.NestedClass:
                    return GetSingleProjectSolution(NestedClass);
                case WorkspaceKind.TwoNamespacesWithIdenticalClasses:
                    return GetSingleProjectSolution(Namespace1, Namespace2);
                default:
                    return null;
            }
        }

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
