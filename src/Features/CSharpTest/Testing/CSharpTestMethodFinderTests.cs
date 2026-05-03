// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests;

[UseExportProvider]
public sealed class CSharpTestMethodFinderTests
{
    #region Xunit

    [Fact]
    public Task TestFindsXUnitFactMethod()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [Fact]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestMatchesXUnitFactInInnerClassMethod()
        => TestXunitMatchAsync("""
            using Xunit;
            public class OuterClass
            {
                [Fact]
                public void TestMethod1() { }
            
                public class InnerClass
                {
                    [Fact]
                    public void Test$$Method2() { }
                }
            }
            """, "OuterClass+InnerClass.TestMethod2");

    [Fact]
    public Task TestMatchesXUnitFactInOuterClassMethod()
        => TestXunitMatchAsync("""
            using Xunit;
            public class OuterClass
            {
                [Fact]
                public void Test$$Method1() { }
            
                public class InnerClass
                {
                    [Fact]
                    public void TestMethod2() { }
                }
            }
            """, "OuterClass.TestMethod1");

    [Fact]
    public Task TestFindsXUnitFactAttributeMethod()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [FactAttribute]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsXUnitTheoryMethod()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [Theory]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsXUnitTheoryAttributeMethod()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [TheoryAttribute]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsXUnitAliasedFactMethod()
        => TestXunitAsync("""
            using Xunit;
            using test = Xunit.FactAttribute;
            public class TestClass
            {
                [test]
                public void Test$$Method1() { }
            }
            """);

    [Fact]
    public Task TestFindsXUnitFactOnlySelectedMethod()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [Fact]
                public void Test$$Method1() { }

                [Fact]
                public void TestMethod2() { }

                public void NotTestMethod() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsXunitMethodsInClassMethod()
        => TestXunitAsync("""
            using Xunit;
            public class Test$$Class
            {
                [Fact]
                public void TestMethod1() { }

                [Fact]
                public void TestMethod2() { }

                [Theory]
                public void TestMethod3() { }

                public void NotTestMethod() { }
            }
            """, "TestMethod1", "TestMethod2", "TestMethod3");

    [Fact]
    public Task TestXunitNoMethodsInClass()
        => TestXunitAsync("""
            using Xunit;
            public class Test$$Class
            {
                public void NotTestMethod() { }
            }
            """);

    [Fact]
    public Task TestFindsSelectedXunitMethods()
        => TestXunitAsync("""
            using Xunit;
            public class TestClass
            {
                [Fact]
                [|public void TestMethod1() { }

                [Fact]
                public void TestMethod2()|] { }

                [Theory]
                public void TestMethod3() { }

                public void NotTestMethod() { }
            }
            """, "TestMethod1", "TestMethod2");

    #endregion

    #region NUnit

    [Fact]
    public Task TestFindsNUnitTestMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [Test]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTestAttributeMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [TestAttribute]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTheoryMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [Theory]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTheoryAttributeMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [TheoryAttribute]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTestCaseMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [TestCase]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTestCaseAttributeMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                [TestCaseAttribute]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTestCaseSourceMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                public static string s_s = "";
                [TestCaseSource(nameof(s_s))]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitTestCaseSourceAttributeMethod()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class TestClass
            {
                public static string s_s = "";
                [TestCaseSourceAttribute(nameof(s_s))]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsNUnitMethodsInClass()
        => TestNUnitAsync("""
            using NUnit.Framework;
            public class Test$$Class
            {
                [Test]
                public void TestMethod1() { }

                [Theory]
                public void TestMethod2() { }

                [TestCase]
                public void TestMethod3() { }

                public static string s_s = "";
                [TestCaseSource(nameof(s_s))]
                public void TestMethod4() { }

                public void NotTestMethod() { }
            }
            """, "TestMethod1", "TestMethod2", "TestMethod3", "TestMethod4");

    #endregion

    #region MSTest

    [Fact]
    public Task TestFindsMSTestTestMethod()
        => TestMSTestAsync("""
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            public class TestClass
            {
                [TestMethod]
                public void Test$$Method1() { }
            }

            """, "TestMethod1");

    [Fact]
    public Task TestFindsMSTestTestMethodAttribute()
        => TestMSTestAsync("""
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            public class TestClass
            {
                [TestMethodAttribute]
                public void Test$$Method1() { }
            }

            """, "TestMethod1");

    [Fact]
    public Task TestFindsMSTestMethodsInClass()
        => TestMSTestAsync("""
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            public class Test$$Class
            {
                [TestMethod]
                public void TestMethod1() { }

                [TestMethod]
                public void TestMethod2() { }

                public void NotTestMethod() { }
            }
            """, "TestMethod1", "TestMethod2");

    #endregion

    [Fact]
    public Task TestFindsTestMethodInBlockScopedNamespace()
        => TestXunitAsync("""
            namespace BlockScoped
            {
                using Xunit;
                public class TestClass
                {
                    [Fact]
                    public void Test$$Method1() { }
                }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsTestMethodInFileScopedNamespace()
        => TestXunitAsync("""
            namespace FileScoped;
            using Xunit;
            public class TestClass
            {
                [Fact]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsTestMethodInStruct()
        => TestXunitAsync("""
            using Xunit;
            public struct TestClass
            {
                [Fact]
                public void Test$$Method1() { }
            }
            """, "TestMethod1");

    [Fact]
    public Task TestFindsTestMethodInPartialClass()
        => TestXunitAsync("""
            using Xunit;
            public partial class PartialClass
            {
                [Fact]
                public void Test$$Method1() { }
            }

            public partial class PartialClass
            {
                [Fact]
                public void TestMethod2() { }
            }
            """, "TestMethod1");

    private static Task TestXunitAsync(string code, params string[] expectedTestNames)
    {
        return TestAsync(code, """
            using System;
            namespace Xunit
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public class FactAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public class TheoryAttribute : FactAttribute { }
            }
            """, expectedTestNames);
    }

    private static Task TestXunitMatchAsync(string code, params string[] expectedQualifiedTestNames)
    {
        return TestMatchAsync(code, """
            using System;
            namespace Xunit
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public class FactAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public class TheoryAttribute : FactAttribute { }
            }
            """, expectedQualifiedTestNames);
    }

    private static Task TestNUnitAsync(string code, params string[] expectedTestNames)
    {
        return TestAsync(code, """
            using System;
            namespace NUnit.Framework
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
                public class TestAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited=true)]
                public class TheoryAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited=false)]
                public class TestCaseAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
                public class TestCaseSourceAttribute : Attribute
                {
                    public TestCaseSourceAttribute(string sourceName) { }
                }
            }
            """, expectedTestNames);
    }

    private static Task TestMSTestAsync(string code, params string[] expectedTestNames)
    {
        return TestAsync(code, """
            using System;
            namespace Microsoft.VisualStudio.TestTools.UnitTesting
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public class TestMethodAttribute : Attribute { }
            }
            """, expectedTestNames);
    }

    private static async Task TestAsync(string code, string testAttributeDefinitionsCode, params string[] expectedTestNames)
    {
        var workspace = TestWorkspace.CreateCSharp([code, testAttributeDefinitionsCode]);

        var testDocument = workspace.Documents.First();
        var span = testDocument.CursorPosition != null ? new TextSpan(testDocument.CursorPosition.Value, 0) : testDocument.SelectedSpans.Single();

        var testMethodFinder = workspace.CurrentSolution.Projects.Single().GetRequiredLanguageService<ITestMethodFinder>();
        var testMethods = await testMethodFinder.GetPotentialTestMethodsAsync(workspace.CurrentSolution.GetRequiredDocument(testDocument.Id), span, CancellationToken.None);
        var testMethodNames = testMethods.Cast<MethodDeclarationSyntax>().Select(m => m.Identifier.Text).ToArray();

        AssertEx.Equal(expectedTestNames, testMethodNames);
    }

    private static async Task TestMatchAsync(string code, string testAttributeDefinitionsCode, params string[] expectedTestNames)
    {
        var workspace = TestWorkspace.CreateCSharp([code, testAttributeDefinitionsCode]);

        var testDocument = workspace.Documents.First();
        var span = testDocument.CursorPosition != null ? new TextSpan(testDocument.CursorPosition.Value, 0) : testDocument.SelectedSpans.Single();

        var testMethodFinder = workspace.CurrentSolution.Projects.Single().GetRequiredLanguageService<ITestMethodFinder>();
        var testMethods = await testMethodFinder.GetPotentialTestMethodsAsync(workspace.CurrentSolution.GetRequiredDocument(testDocument.Id), span, CancellationToken.None);
        var semanticModel = await workspace.CurrentSolution.GetRequiredDocument(testDocument.Id).GetRequiredSemanticModelAsync(CancellationToken.None);

        List<string> unmatchedTestNames = [];

        foreach (var expectedTestName in expectedTestNames)
        {
            var matchFound = testMethods.Any(m => testMethodFinder.IsMatch(semanticModel, m, expectedTestName, CancellationToken.None));
            if (!matchFound)
            {
                unmatchedTestNames.Add(expectedTestName);
            }
        }

        Assert.True(unmatchedTestNames.Count == 0, $"Unable to match the following test names: {string.Join(", ", unmatchedTestNames)}");
    }
}
