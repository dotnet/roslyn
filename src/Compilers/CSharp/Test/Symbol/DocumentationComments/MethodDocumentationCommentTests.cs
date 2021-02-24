// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MethodDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public MethodDocumentationCommentTests()
        {
            _compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"namespace Acme
{
    struct ValueType
    {
        public void M(int i) { }

        public static explicit operator ValueType (byte value)
        {
            return default(ValueType);
        }
    }
    class Widget: IProcess
    {
        public class NestedClass
        {
            public void M(int i) { }
        }

        /// <summary>M0 Summary.</summary>
        public static void M0() { }
        public void M1(char c, out float f, ref ValueType v) { }
        public void M2(short[] x1, int[,] x2, long[][] x3) { }
        public void M3(long[][] x3, Widget[][,,] x4) { }
        public unsafe void M4(char *pc, Color **pf) { }
        public unsafe void M5(void *pv, double *[][,] pd) { }
        public void M6(int i, params object[] args) { }
        public void M7((int x1, int x2, int x3, int x4, int x5, int x6, short x7) z) { }
        public void M8((int x1, int x2, int x3, int x4, int x5, int x6, short x7, int x8) z) { }
        public void M9((int x1, int x2, int x3, int x4, int x5, int x6, short x7, (string y1, string y2)) z) { }
        public void M10((int x1, short x2) y, System.Tuple<int, short> z) { }
    }
    class MyList<T>
    {
        public void Test(T t) { }
        public void Zip(MyList<T> other) { }
        public void ReallyZip(MyList<MyList<T>> other) { }
    }
    class UseList
    {
        public void Process(MyList<int> list) { }
        public MyList<T> GetValues<T>(T inputValue) { return null; }
    }
}
");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestMethodInStruct()
        {
            Assert.Equal("M:Acme.ValueType.M(System.Int32)", _acmeNamespace.GetTypeMembers("ValueType").Single().GetMembers("M").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestNestedClass()
        {
            Assert.Equal("M:Acme.Widget.NestedClass.M(System.Int32)", _widgetClass.GetTypeMembers("NestedClass").Single().GetMembers("M").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestStaticMethod()
        {
            var m0 = _widgetClass.GetMembers("M0").Single();
            Assert.Equal("M:Acme.Widget.M0", m0.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.M0"">
    <summary>M0 Summary.</summary>
</member>
", m0.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestMethodWithRefAndOut()
        {
            Assert.Equal("M:Acme.Widget.M1(System.Char,System.Single@,Acme.ValueType@)", _widgetClass.GetMembers("M1").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestMethodWithArrays1()
        {
            Assert.Equal("M:Acme.Widget.M2(System.Int16[],System.Int32[0:,0:],System.Int64[][])", _widgetClass.GetMembers("M2").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestMethodWithArrays2()
        {
            Assert.Equal("M:Acme.Widget.M3(System.Int64[][],Acme.Widget[0:,0:,0:][])", _widgetClass.GetMembers("M3").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafe1()
        {
            Assert.Equal("M:Acme.Widget.M4(System.Char*,Color**)", _widgetClass.GetMembers("M4").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafe2()
        {
            Assert.Equal("M:Acme.Widget.M5(System.Void*,System.Double*[0:,0:][])", _widgetClass.GetMembers("M5").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestParams()
        {
            Assert.Equal("M:Acme.Widget.M6(System.Int32,System.Object[])", _widgetClass.GetMembers("M6").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestTupleLength7()
        {
            Assert.Equal("M:Acme.Widget.M7(System.ValueTuple{System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int16})",
                _widgetClass.GetMembers("M7").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestTupleLength8()
        {
            Assert.Equal("M:Acme.Widget.M8(System.ValueTuple{System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int16,System.ValueTuple{System.Int32}})",
                _widgetClass.GetMembers("M8").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestTupleLength9()
        {
            Assert.Equal("M:Acme.Widget.M9(System.ValueTuple{System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int16,System.ValueTuple{System.ValueTuple{System.String,System.String}}})",
                _widgetClass.GetMembers("M9").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestTupleLength2()
        {
            Assert.Equal("M:Acme.Widget.M10(System.ValueTuple{System.Int32,System.Int16},System.Tuple{System.Int32,System.Int16})",
                _widgetClass.GetMembers("M10").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestMethodInGenericClass()
        {
            Assert.Equal("M:Acme.MyList`1.Test(`0)", _acmeNamespace.GetTypeMembers("MyList", 1).Single().GetMembers("Test").Single().GetDocumentationCommentId());
        }

        [WorkItem(766313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766313")]
        [Fact]
        public void TestMethodWithGenericDeclaringTypeAsParameter()
        {
            Assert.Equal("M:Acme.MyList`1.Zip(Acme.MyList{`0})", _acmeNamespace.GetTypeMembers("MyList", 1).Single().GetMembers("Zip").Single().GetDocumentationCommentId());
        }

        [WorkItem(766313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766313")]
        [Fact]
        public void TestMethodWithGenericDeclaringTypeAsTypeParameter()
        {
            Assert.Equal("M:Acme.MyList`1.ReallyZip(Acme.MyList{Acme.MyList{`0}})", _acmeNamespace.GetTypeMembers("MyList", 1).Single().GetMembers("ReallyZip").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestMethodWithClosedGenericParameter()
        {
            Assert.Equal("M:Acme.UseList.Process(Acme.MyList{System.Int32})", _acmeNamespace.GetTypeMembers("UseList").Single().GetMembers("Process").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestGenericMethod()
        {
            Assert.Equal("M:Acme.UseList.GetValues``1(``0)", _acmeNamespace.GetTypeMembers("UseList").Single().GetMembers("GetValues").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestMethodWithMissingType()
        {
            var csharpAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.CSharp;
            var ilAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.IL;
            var compilation = CreateCompilation(references: new[] { csharpAssemblyReference, ilAssemblyReference }, source:
@"class C
{
    internal static CSharpErrors.ClassMethods F = null;
}");
            var type = compilation.Assembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            type = (NamedTypeSymbol)type.GetMember<FieldSymbol>("F").Type;
            var members = type.GetMembers();
            Assert.InRange(members.Length, 1, int.MaxValue);
            foreach (var member in members)
            {
                var docComment = member.GetDocumentationCommentXml();
                Assert.NotNull(docComment);
            }
        }

        [Fact, WorkItem(530924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530924")]
        public void TestConversionOperator()
        {
            Assert.Equal("M:Acme.ValueType.op_Explicit(System.Byte)~Acme.ValueType", _acmeNamespace.GetTypeMembers("ValueType").Single().GetMembers("op_Explicit").Single().GetDocumentationCommentId());
        }

        [WorkItem(4699, "https://github.com/dotnet/roslyn/issues/4699")]
        [WorkItem(25781, "https://github.com/dotnet/roslyn/issues/25781")]
        [ConditionalFact(typeof(IsEnglishLocal))]
        public void GetMalformedDocumentationCommentXml()
        {
            var source = @"
class Test
{
    /// <summary>
    /// Info
    /// <!-- comment
    /// </summary
    static void Main() {}
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
            var main = compilation.GetTypeByMetadataName("Test").GetMember<MethodSymbol>("Main");

            Assert.Equal(@"<!-- Badly formed XML comment ignored for member ""M:Test.Main"" -->", main.GetDocumentationCommentXml(EnsureEnglishUICulture.PreferredOrNull).Trim());

            compilation = CreateEmptyCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
            main = compilation.GetTypeByMetadataName("Test").GetMember<MethodSymbol>("Main");

            Assert.Equal(@"<!-- Badly formed XML comment ignored for member ""M:Test.Main"" -->", main.GetDocumentationCommentXml(EnsureEnglishUICulture.PreferredOrNull).Trim());

            compilation = CreateEmptyCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
            main = compilation.GetTypeByMetadataName("Test").GetMember<MethodSymbol>("Main");

            Assert.Equal(@"", main.GetDocumentationCommentXml().Trim());
        }
    }
}
