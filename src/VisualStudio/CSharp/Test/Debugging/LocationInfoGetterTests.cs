// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging
{
    public class LocationInfoGetterTests
    {
        private void Test(string markup, string expectedName, int expectedLineOffset, CSharpParseOptions parseOptions = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(new[] { markup }, parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var position = testDocument.CursorPosition.Value;
                var locationInfo = LocationInfoGetter.GetInfoAsync(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    position,
                    CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                Assert.Equal(expectedName, locationInfo.Name);
                Assert.Equal(expectedLineOffset, locationInfo.LineOffset);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestClass()
        {
            Test("class F$$oo { }", "Foo", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668), WorkItem(538415)]
        public void TestMethod()
        {
            Test(
@"class Class
{
    public static void Meth$$od()
    {
    }
}
", "Class.Method()", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestNamespace()
        {
            Test(
@"namespace Namespace
{
    class Class
    {
        void Method()
        {
        }$$
    }
}", "Namespace.Class.Method()", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestDottedNamespace()
        {
            Test(
@"namespace Namespace.Another
{
    class Class
    {
        void Method()
        {
        }$$
    }
}", "Namespace.Another.Class.Method()", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestNestedNamespace()
        {
            Test(
@"namespace Namespace
{
    namespace Another
    {
        class Class
        {
            void Method()
            {
            }$$
        }
    }
}", "Namespace.Another.Class.Method()", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestNestedType()
        {
            Test(
@"class Outer
{
    class Inner
    {
        void Quux()
        {$$
        }
    }
}", "Outer.Inner.Quux()", 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestPropertyGetter()
        {
            Test(
@"class Class
{
    string Property
    {
        get
        {
            return null;$$
        }
    }
}", "Class.Property", 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestPropertySetter()
        {
            Test(
@"class Class
{
    string Property
    {
        get
        {
            return null;
        }

        set
        {
            string s = $$value;
        }
    }
}", "Class.Property", 9);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(538415)]
        public void TestField()
        {
            Test(
@"class Class
{
    int fi$$eld;
}", "Class.field", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(543494)]
        public void TestLambdaInFieldInitializer()
        {
            Test(
@"class Class
{
    Action<int> a = b => { in$$t c; };
}", "Class.a", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(543494)]
        public void TestMultipleFields()
        {
            Test(
@"class Class
{
    int a1, a$$2;
}", "Class.a2", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestConstructor()
        {
            Test(
@"class C1
{
    C1()
    {

    $$}
}
", "C1.C1()", 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestDestructor()
        {
            Test(
@"class C1
{
    ~C1()
    {
    $$}
}
", "C1.~C1()", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestOperator()
        {
            Test(
@"namespace N1
{
    class C1
    {
        public static int operator +(C1 x, C1 y)
        {
            $$return 42;
        }
    }
}
", "N1.C1.+(C1 x, C1 y)", 2); // Old implementation reports "operator +" (rather than "+")...
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestConversionOperator()
        {
            Test(
@"namespace N1
{
    class C1
    {
        public static explicit operator N1.C2(N1.C1 x)
        {
            $$return null;
        }
    }
    class C2
    {
    }
}
", "N1.C1.N1.C2(N1.C1 x)", 2); // Old implementation reports "explicit operator N1.C2" (rather than "N1.C2")...
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestEvent()
        {
            Test(
@"class C1
{
    delegate void D1();
    event D1 e1$$;
}
", "C1.e1", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TextExplicitInterfaceImplementation()
        {
            Test(
@"interface I1
{
    void M1();
}
class C1
{
    void I1.M1()
    {
    $$}
}
", "C1.M1()", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TextIndexer()
        {
            Test(
@"class C1
{
    C1 this[int x]
    {
        get
        {
            $$return null;
        }
    }
}
", "C1.this[int x]", 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestParamsParameter()
        {
            Test(
@"class C1
{
    void M1(params int[] x) { $$ }
}
", "C1.M1(params int[] x)", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestArglistParameter()
        {
            Test(
@"class C1
{
    void M1(__arglist) { $$ }
}
", "C1.M1(__arglist)", 0); // Old implementation does not show "__arglist"...
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestRefAndOutParameters()
        {
            Test(
@"class C1
{
    void M1( ref int x, out int y )
    {
        $$y = x;
    }
}
", "C1.M1( ref int x, out int y )", 2); // Old implementation did not show extra spaces around the parameters...
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestOptionalParameters()
        {
            Test(
@"class C1
{
    void M1(int x =1)
    {
        $$y = x;
    }
}
", "C1.M1(int x =1)", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestExtensionMethod()
        {
            Test(
@"static class C1
{
    static void M1(this int x)
    {
    }$$
}
", "C1.M1(this int x)", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestGenericType()
        {
            Test(
@"class C1<T, U>
{
    static void M1() { $$ }
}
", "C1.M1()", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestGenericMethod()
        {
            Test(
@"class C1<T, U>
{
    static void M1<V>() { $$ }
}
", "C1.M1()", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestGenericParameters()
        {
            Test(
@"class C1<T, U>
{
    static void M1<V>(C1<int, V> x, V y) { $$ }
}
", "C1.M1(C1<int, V> x, V y)", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestMissingNamespace()
        {
            Test(
@"{
    class Class
    {
        int a1, a$$2;
    }
}", "Class.a2", 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestMissingNamespaceName()
        {
            Test(
@"namespace
{
    class C1
    {
        int M1()
        $${
        }
    }
}", "?.C1.M1()", 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestMissingClassName()
        {
            Test(
@"namespace N1
    class 
    {
        int M1()
        $${
        }
    }
}", "N1.M1()", 1); // Old implementation displayed "N1.?.M1", but we don't see a class declaration in the syntax tree...
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestMissingMethodName()
        {
            Test(
@"namespace N1
{
    class C1
    {
        static void (int x)
        {
        $$}
    }
}", "N1.C1", 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestMissingParameterList()
        {
            Test(
@"namespace N1
{
    class C1
    {
        static void M1
        {
        $$}
    }
}", "N1.C1.M1", 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TopLevelField()
        {
            Test(
@"$$int f1;
", "f1", 0, new CSharpParseOptions(kind: SourceCodeKind.Script));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TopLevelMethod()
        {
            Test(
@"int M1(int x)
{
$$}
", "M1(int x)", 2, new CSharpParseOptions(kind: SourceCodeKind.Script));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TopLevelStatement()
        {
            Test(
@"

$$System.Console.WriteLine(""Hello"")
", null, 0, new CSharpParseOptions(kind: SourceCodeKind.Script));
        }
    }
}
