// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SymbolSearchTests : CSharpTestBase
    {
        [Fact]
        public void TestSymbolFilterNone()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.ContainsSymbolsWithName(n => true, SymbolFilter.None);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.GetSymbolsWithName(n => true, SymbolFilter.None);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.ContainsSymbolsWithName("", SymbolFilter.None);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.GetSymbolsWithName("", SymbolFilter.None);
            });
        }

        [Fact]
        public void TestPredicateNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.ContainsSymbolsWithName(predicate: null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.GetSymbolsWithName(predicate: null);
            });
        }

        [Fact]
        public void TestNameNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.ContainsSymbolsWithName(name: null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.GetSymbolsWithName(name: null);
            });
        }

        [Fact]
        public void TestMergedNamespace()
        {
            var compilation = GetTestCompilation();

            TestNameAndPredicate(compilation, "System", includeNamespace: true, includeType: false, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "System", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "System", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "System", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            TestNameAndPredicate(compilation, "System", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            TestNameAndPredicate(compilation, "System", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "System", includeNamespace: false, includeType: true, includeMember: true, count: 0);
        }

        [Fact]
        public void TestSourceNamespace()
        {
            var compilation = GetTestCompilation();

            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: true, includeType: false, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace: false, includeType: true, includeMember: true, count: 0);
        }

        [Fact]
        public void TestClassInMergedNamespace()
        {
            var compilation = GetTestCompilation();

            TestNameAndPredicate(compilation, "Test", includeNamespace: false, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "Test", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "Test", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "Test", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            TestNameAndPredicate(compilation, "Test", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            TestNameAndPredicate(compilation, "Test", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "Test", includeNamespace: true, includeType: false, includeMember: true, count: 0);
        }

        [Fact]
        public void TestClassInSourceNamespace()
        {
            var compilation = GetTestCompilation();

            TestNameAndPredicate(compilation, "Test1", includeNamespace: false, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "Test1", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "Test1", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            TestNameAndPredicate(compilation, "Test1", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            TestNameAndPredicate(compilation, "Test1", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            TestNameAndPredicate(compilation, "Test1", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "Test1", includeNamespace: true, includeType: false, includeMember: true, count: 0);
        }

        [Fact]
        public void TestMembers()
        {
            var compilation = GetTestCompilation();

            TestNameAndPredicate(compilation, "myField", includeNamespace: false, includeType: false, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "myField", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "myField", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            TestNameAndPredicate(compilation, "myField", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            TestNameAndPredicate(compilation, "myField", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "myField", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            TestNameAndPredicate(compilation, "myField", includeNamespace: true, includeType: true, includeMember: false, count: 0);
        }

        [Fact]
        public void TestPartialSearch()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: false, includeType: false, includeMember: true, count: 4);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: false, includeType: true, includeMember: false, count: 4);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: false, includeType: true, includeMember: true, count: 8);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: true, includeType: false, includeMember: false, count: 1);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: true, includeType: false, includeMember: true, count: 5);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: true, includeType: true, includeMember: false, count: 5);
            Test(compilation, n => n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: true, includeType: true, includeMember: true, count: 9);

            Test(compilation, n => n.IndexOf("enum", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: true, includeType: true, includeMember: true, count: 2);
        }

        [WorkItem(876191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876191")]
        [Fact]
        public void TestExplicitInterfaceSearch()
        {
            const string source = @"
interface I
{
    void M();
}

class Explicit : I
{
    void I.M() { }
}

class Implicit : I
{
    public void M() { }
}
";
            var compilation = CreateCompilation(new[] { source });

            Test(compilation, n => n.IndexOf("M", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace: false, includeType: false, includeMember: true, count: 3);
        }

        private static CSharpCompilation GetTestCompilation()
        {
            const string source = @"
namespace System
{
    public class Test { }
}

namespace MyNamespace
{ 
    public class Test1 { }
}

public class MyClass
{
    private int myField;
    internal int MyProperty { get; set; }
    void MyMethod() { }

    public event EventHandler MyEvent;
    delegate string MyDelegate(int i);
}

struct MyStruct
{
}

interface MyInterface
{
}

enum Enum
{
    EnumValue
}
";
            return CreateCompilation(source: new string[] { source });
        }

        private static void TestNameAndPredicate(CSharpCompilation compilation, string name, bool includeNamespace, bool includeType, bool includeMember, int count)
        {
            Test(compilation, name, includeNamespace, includeType, includeMember, count);
            Test(compilation, n => n == name, includeNamespace, includeType, includeMember, count);
        }

        private static void Test(CSharpCompilation compilation, string name, bool includeNamespace, bool includeType, bool includeMember, int count)
        {
            SymbolFilter filter = ComputeFilter(includeNamespace, includeType, includeMember);

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(name, filter));
            Assert.Equal(count, compilation.GetSymbolsWithName(name, filter).Count());
        }

        private static void Test(CSharpCompilation compilation, Func<string, bool> predicate, bool includeNamespace, bool includeType, bool includeMember, int count)
        {
            SymbolFilter filter = ComputeFilter(includeNamespace, includeType, includeMember);

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(predicate, filter));
            Assert.Equal(count, compilation.GetSymbolsWithName(predicate, filter).Count());
        }

        private static SymbolFilter ComputeFilter(bool includeNamespace, bool includeType, bool includeMember)
        {
            var filter = SymbolFilter.None;
            filter = includeNamespace ? (filter | SymbolFilter.Namespace) : filter;
            filter = includeType ? (filter | SymbolFilter.Type) : filter;
            filter = includeMember ? (filter | SymbolFilter.Member) : filter;
            return filter;
        }
    }
}
