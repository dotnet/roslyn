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
        }

        [Fact]
        public void TestPredicateNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.ContainsSymbolsWithName(null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var compilation = GetTestCompilation();
                compilation.GetSymbolsWithName(null);
            });
        }

        [Fact]
        public void TestMergedNamespace()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n == "System", includeNamespace: true, includeType: false, includeMember: false, count: 1);
            Test(compilation, n => n == "System", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "System", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            Test(compilation, n => n == "System", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            Test(compilation, n => n == "System", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            Test(compilation, n => n == "System", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            Test(compilation, n => n == "System", includeNamespace: false, includeType: true, includeMember: true, count: 0);
        }

        [Fact]
        public void TestSourceNamespace()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n == "MyNamespace", includeNamespace: true, includeType: false, includeMember: false, count: 1);
            Test(compilation, n => n == "MyNamespace", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "MyNamespace", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            Test(compilation, n => n == "MyNamespace", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            Test(compilation, n => n == "MyNamespace", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            Test(compilation, n => n == "MyNamespace", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            Test(compilation, n => n == "MyNamespace", includeNamespace: false, includeType: true, includeMember: true, count: 0);
        }

        [Fact]
        public void TestClassInMergedNamespace()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n == "Test", includeNamespace: false, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "Test", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            Test(compilation, n => n == "Test", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "Test", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            Test(compilation, n => n == "Test", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            Test(compilation, n => n == "Test", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            Test(compilation, n => n == "Test", includeNamespace: true, includeType: false, includeMember: true, count: 0);
        }

        [Fact]
        public void TestClassInSourceNamespace()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n == "Test1", includeNamespace: false, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "Test1", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            Test(compilation, n => n == "Test1", includeNamespace: true, includeType: true, includeMember: false, count: 1);
            Test(compilation, n => n == "Test1", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            Test(compilation, n => n == "Test1", includeNamespace: false, includeType: false, includeMember: true, count: 0);
            Test(compilation, n => n == "Test1", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            Test(compilation, n => n == "Test1", includeNamespace: true, includeType: false, includeMember: true, count: 0);
        }

        [Fact]
        public void TestMembers()
        {
            var compilation = GetTestCompilation();

            Test(compilation, n => n == "myField", includeNamespace: false, includeType: false, includeMember: true, count: 1);
            Test(compilation, n => n == "myField", includeNamespace: false, includeType: true, includeMember: true, count: 1);
            Test(compilation, n => n == "myField", includeNamespace: true, includeType: false, includeMember: true, count: 1);
            Test(compilation, n => n == "myField", includeNamespace: true, includeType: true, includeMember: true, count: 1);

            Test(compilation, n => n == "myField", includeNamespace: false, includeType: true, includeMember: false, count: 0);
            Test(compilation, n => n == "myField", includeNamespace: true, includeType: false, includeMember: false, count: 0);
            Test(compilation, n => n == "myField", includeNamespace: true, includeType: true, includeMember: false, count: 0);
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
            var compilation = CreateCompilationWithMscorlib(new[] { source });

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
            return CreateCompilationWithMscorlib(sources: new string[] { source });
        }

        private static void Test(CSharpCompilation compilation, Func<string, bool> predicate, bool includeNamespace, bool includeType, bool includeMember, int count)
        {
            var filter = SymbolFilter.None;
            filter = includeNamespace ? filter | SymbolFilter.Namespace : filter;
            filter = includeType ? filter | SymbolFilter.Type : filter;
            filter = includeMember ? filter | SymbolFilter.Member : filter;

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(predicate, filter));
            Assert.Equal(count, compilation.GetSymbolsWithName(predicate, filter).Count());
        }
    }
}
