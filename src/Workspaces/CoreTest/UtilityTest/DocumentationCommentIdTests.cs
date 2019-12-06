// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class DocumentationCommentIdTests : TestBase
    {
        private CSharpCompilation CreateCSharpCompilation(string sourceText)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText);
            return CSharpCompilation.Create("goo.exe").AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib).AddSyntaxTrees(syntaxTree);
        }

        private void CheckDeclarationId(string expectedId, INamespaceOrTypeSymbol symbol, Compilation compilation)
        {
            var id = DocumentationCommentId.CreateDeclarationId(symbol);
            Assert.Equal(expectedId, id);

            var sym = DocumentationCommentId.GetFirstSymbolForDeclarationId(id, compilation);
            Assert.Equal(symbol, sym);
        }

        private TSymbol CheckDeclarationId<TSymbol>(string expectedId, Compilation compilation, Func<TSymbol, bool> test)
            where TSymbol : ISymbol
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(expectedId, compilation);
            Assert.True(symbol is TSymbol);
            Assert.True(test((TSymbol)symbol));

            return (TSymbol)symbol;
        }

        private void CheckDeclarationIdExact<TSymbol>(string expectedId, Compilation compilation, Func<TSymbol, bool> test)
            where TSymbol : ISymbol
        {
            var symbol = CheckDeclarationId(expectedId, compilation, test);

            var id = DocumentationCommentId.CreateDeclarationId(symbol);
            Assert.Equal(expectedId, id);
        }

        private void CheckReferenceId(string expectedId, INamespaceOrTypeSymbol symbol, Compilation compilation)
        {
            var id = DocumentationCommentId.CreateReferenceId(symbol);
            Assert.Equal(expectedId, id);

            var sym = DocumentationCommentId.GetSymbolsForReferenceId(id, compilation).FirstOrDefault();
            Assert.Equal(symbol, sym);
        }

        [Fact]
        public void TestCSharpTypeIds()
        {
            var compilation = CreateCSharpCompilation(@"
enum Color { Red, Blue, Green }
namespace Acme
{
    interface IProcess { }
    struct ValueType
    {
        private int total;
    }
    class Widget : IProcess
    {
        public class NestedClass { }
        public interface IMenuItem { }
        public delegate void Del(int i);
        public enum Direction { North, South, East, West }
    }
    class MyList<T>
    {
        class Helper<U,V> { }
    }
}
");
            CheckDeclarationId("T:Color", compilation.GetTypeByMetadataName("Color"), compilation);
            CheckDeclarationId("T:Acme.IProcess", compilation.GetTypeByMetadataName("Acme.IProcess"), compilation);
            CheckDeclarationId("T:Acme.ValueType", compilation.GetTypeByMetadataName("Acme.ValueType"), compilation);
            CheckDeclarationId("T:Acme.Widget", compilation.GetTypeByMetadataName("Acme.Widget"), compilation);
            CheckDeclarationId("T:Acme.Widget.NestedClass", compilation.GetTypeByMetadataName("Acme.Widget+NestedClass"), compilation);
            CheckDeclarationId("T:Acme.Widget.IMenuItem", compilation.GetTypeByMetadataName("Acme.Widget+IMenuItem"), compilation);
            CheckDeclarationId("T:Acme.Widget.Del", compilation.GetTypeByMetadataName("Acme.Widget+Del"), compilation);
            CheckDeclarationId("T:Acme.Widget.Direction", compilation.GetTypeByMetadataName("Acme.Widget+Direction"), compilation);
            CheckDeclarationId("T:Acme.MyList`1", compilation.GetTypeByMetadataName("Acme.MyList`1"), compilation);
            CheckDeclarationId("T:Acme.MyList`1.Helper`2", compilation.GetTypeByMetadataName("Acme.MyList`1+Helper`2"), compilation);
        }

        [Fact]
        public void TestCSharpFields()
        {
            var compilation = CreateCSharpCompilation(@"
enum Color { Red, Blue, Green }
namespace Acme
{
    interface IProcess { }
    struct ValueType
    {
        private int total;
    }
    class Widget : IProcess
    {
        public class NestedClass
        {
             private int value;
        }
        private string message;
        private static Color defaultColor;
        private const double PI = 3.14159;
        protected readonly double monthlyAverage;
        private long[] array1;
        private widget[,] array2;
        private unsafe int *pCount;
        private unsafe float **ppValues;
    }
}
");

            CheckDeclarationId<IFieldSymbol>("F:Acme.ValueType.total", compilation, s => s.Name == "total");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.NestedClass.value", compilation, s => s.Name == "value");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.message", compilation, s => s.Name == "message");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.defaultColor", compilation, s => s.Name == "defaultColor");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.PI", compilation, s => s.Name == "PI");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.monthlyAverage", compilation, s => s.Name == "monthlyAverage");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.array1", compilation, s => s.Name == "array1");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.array2", compilation, s => s.Name == "array2");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.pCount", compilation, s => s.Name == "pCount");
            CheckDeclarationId<IFieldSymbol>("F:Acme.Widget.ppValues", compilation, s => s.Name == "ppValues");
        }

        [Fact]
        public void TestCSharpConstructors()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    interface IProcess { }
    class Widget : IProcess
    {
        static Widget() { }
        public Widget() { }
        public Widget(string s) { }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.#cctor", compilation, s => s.MethodKind == MethodKind.StaticConstructor);
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.#ctor", compilation, s => s.MethodKind == MethodKind.Constructor && s.Parameters.Length == 0);
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.#ctor(System.String)", compilation, s => s.MethodKind == MethodKind.Constructor && s.Parameters.Length == 1);
        }

        [Fact]
        public void TestCSharpDestructors()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    interface IProcess { }
    class Widget : IProcess
    {
        ~Widget() { }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.Finalize", compilation, s => s.MethodKind == MethodKind.Destructor);
        }

        [Fact]
        public void TestCSharpMethods()
        {
            var compilation = CreateCSharpCompilation(@"
enum Color { Red, Blue, Green }
namespace Acme
{
    struct ValueType
    {
        public void M(int i) { }
    }
    interface IProcess { }
    class Widget : IProcess
    {
        public class NestedClass 
        {
            public void M(int i) { }
        }
        public static void M0() { }
        public void M1(char c, out float f, ref ValueType v) { }
        public void M2(short[] x1, int[,] x2, long[][] x3) { }
        public void M3(long[][] x3, Widget[][,,] x4) { }
        public void M4(char *pc, Color **pf) { }
        public void M5(void *pv, double *[][,] pd) { }
        public void M6(int i, params object[] args) { }
    }
    class MyList<T>
    {
        public void Test(T t) { }
    }
    class UseList
    {
        public void Process(MyList<int> list) { }
        public MyList<T> GetValues<T>(T inputValue) { return null; }
        public void Process2<T>(MyList<T> list) { }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.ValueType.M(System.Int32)", compilation, s => s.Name == "M" && s.Parameters.Length == 1 && s.Parameters[0].Type.Name == "Int32");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.NestedClass.M(System.Int32)", compilation, s => s.Name == "M" && s.Parameters.Length == 1 && s.Parameters[0].Type.Name == "Int32");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M0", compilation, s => s.Name == "M0" && s.Parameters.Length == 0);
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M1(System.Char,System.Single@,Acme.ValueType@)", compilation, s => s.Name == "M1");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M2(System.Int16[],System.Int32[0:,0:],System.Int64[][])", compilation, s => s.Name == "M2");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M3(System.Int64[][],Acme.Widget[0:,0:,0:][])", compilation, s => s.Name == "M3");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M4(System.Char*,Color**)", compilation, s => s.Name == "M4");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M5(System.Void*,System.Double*[0:,0:][])", compilation, s => s.Name == "M5");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.M6(System.Int32,System.Object[])", compilation, s => s.Name == "M6");
            CheckDeclarationId<IMethodSymbol>("M:Acme.MyList`1.Test(`0)", compilation, s => s.Name == "Test");
            CheckDeclarationId<IMethodSymbol>("M:Acme.UseList.Process(Acme.MyList{System.Int32})", compilation, s => s.Name == "Process");
            CheckDeclarationId<IMethodSymbol>("M:Acme.UseList.GetValues``1(``0)", compilation, s => s.Name == "GetValues");
            CheckDeclarationId<IMethodSymbol>("M:Acme.UseList.Process2``1(Acme.MyList{``0})", compilation, s => s.Name == "Process2");
        }

        [Fact]
        public void TestCSharpPropertiesAndIndexers()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    interface IProcess { }
    class Widget : IProcess
    {
        public int Width { get { return 0; } set { } }
        public int this[int i] { get { return 0; } set { } }
        public int this[string s, int i] { get { return 0; } set { } }
    }
}
");

            CheckDeclarationIdExact<IPropertySymbol>("P:Acme.Widget.Width", compilation, p => p.Name == "Width");
            CheckDeclarationIdExact<IPropertySymbol>("P:Acme.Widget.Item(System.Int32)", compilation, p => p.Parameters.Length == 1);
            CheckDeclarationIdExact<IPropertySymbol>("P:Acme.Widget.Item(System.String,System.Int32)", compilation, p => p.Parameters.Length == 2);
        }

        [Fact]
        public void TestCSharpEvents()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    class Widget : IProcess
    {
        public delegate void Del(int i);
        public event Del AnEvent;
    }
}
");
            CheckDeclarationId<IEventSymbol>("E:Acme.Widget.AnEvent", compilation, e => e.Name == "AnEvent");
        }

        [Fact]
        public void TestCSharpUnaryOperators()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    class Widget : IProcess
    {
        public static Widget operator+(Widget x) { return x; }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.op_UnaryPlus(Acme.Widget)", compilation, m => m.MethodKind == MethodKind.UserDefinedOperator && m.Parameters.Length == 1);
        }

        [Fact]
        public void TestCSharpBinaryOperators()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    class Widget : IProcess
    {
        public static Widget operator+(Widget x1, Widget x2) { return x1; }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.op_Addition(Acme.Widget,Acme.Widget)", compilation, m => m.MethodKind == MethodKind.UserDefinedOperator && m.Parameters.Length == 2);
        }

        [Fact]
        public void TestCSharpConversionOperators()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    class Widget : IProcess
    {
        public static explicit operator int(Widget x) { return 0; }
        public static implicit operator long(Widget x) { return 0; }
    }
}
");

            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.op_Explicit(Acme.Widget)~System.Int32", compilation, m => m.MethodKind == MethodKind.Conversion && m.Parameters.Length == 1 && m.ReturnType.Name == "Int32");
            CheckDeclarationId<IMethodSymbol>("M:Acme.Widget.op_Implicit(Acme.Widget)~System.Int64", compilation, m => m.MethodKind == MethodKind.Conversion && m.Parameters.Length == 1 && m.ReturnType.Name == "Int64");
        }

        [Fact]
        public void TestTypeReferences()
        {
            var compilation = CreateCSharpCompilation(@"
namespace Acme
{
    class OuterType<A>
    {
        class InnerType<B, C>
        {
        }

        public void M<D>(D d) { }
    }
}
");

            var outerType = compilation.GetTypeByMetadataName("Acme.OuterType`1");
            var innerType = compilation.GetTypeByMetadataName("Acme.OuterType`1+InnerType`2");
            var method = outerType.GetMembers("M").First() as IMethodSymbol;
            var ienum = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            var ienumTP = ienum.Construct(outerType.TypeArguments[0]);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // reference to type parameters
            CheckReferenceId("T:Acme.OuterType`1:`0", outerType.TypeParameters[0], compilation);
            CheckReferenceId("T:Acme.OuterType`1.InnerType`2:`1", innerType.TypeParameters[0], compilation);
            CheckReferenceId("T:Acme.OuterType`1.InnerType`2:`2", innerType.TypeParameters[1], compilation);
            CheckReferenceId("M:Acme.OuterType`1.M``1(``0):``0", method.TypeParameters[0], compilation);
            CheckReferenceId("System.Collections.Generic.IEnumerable{T:Acme.OuterType`1:`0}", ienumTP, compilation);

            // simple types
            CheckReferenceId("System.Int32", intType, compilation);
            CheckReferenceId("System.Int32[]", compilation.CreateArrayTypeSymbol(intType), compilation);
            CheckReferenceId("System.Int32*", compilation.CreatePointerTypeSymbol(intType), compilation);
        }
    }
}
