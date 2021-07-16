// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Declarations
{
    public class SourcePlusMetadataTests : CSharpTestBase
    {
        [Fact]
        public void DefaultBaseType1()
        {
            var text =
@"
class X : object {}
class Y {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            Assert.Equal(SymbolKind.NamedType, x.BaseType().Kind);
            var y = global.GetTypeMembers("Y", 0).Single();
            Assert.Equal(SymbolKind.NamedType, y.BaseType().Kind);
            Assert.Equal(x.BaseType(), y.BaseType());
            Assert.Equal("Object", y.BaseType().Name);
        }

        [Fact]
        public void DefaultBaseType2()
        {
            var text =
@"
struct X {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            Assert.Equal(SymbolKind.NamedType, x.BaseType().Kind);
            Assert.Equal("ValueType", x.BaseType().Name);
        }

        [Fact]
        public void DefaultBaseType3()
        {
            var text =
@"
interface X {}
class Y {}
class Z {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            Assert.Null(x.BaseType());
            var y = global.GetTypeMembers("Y", 0).Single();
            Assert.Equal(SymbolKind.NamedType, y.BaseType().Kind);
            var z = global.GetTypeMembers("Z", 0).Single();
            Assert.Equal(SymbolKind.NamedType, z.BaseType().Kind);
            Assert.Equal(z.BaseType(), y.BaseType());
            Assert.Equal("Object", y.BaseType().Name);
        }

        [Fact]
        public void MergedNamespaces()
        {
            var text =
@"
namespace System {
  class A : Object {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var system = global.GetMembers("System").Single() as NamespaceSymbol;
            var a = system.GetTypeMembers("A", 0).Single();
            Assert.Equal(SymbolKind.NamedType, a.BaseType().Kind);
            Assert.Equal("Object", a.BaseType().Name);
        }

        [Fact]
        public void BuiltinTypes()
        {
            TestBuiltinType("bool", "Boolean");
            TestBuiltinType("byte", "Byte");
            TestBuiltinType("sbyte", "SByte");
            TestBuiltinType("short", "Int16");
            TestBuiltinType("ushort", "UInt16");
            TestBuiltinType("int", "Int32");
            TestBuiltinType("long", "Int64");
            TestBuiltinType("ulong", "UInt64");
            TestBuiltinType("double", "Double");
            TestBuiltinType("float", "Single");
            TestBuiltinType("decimal", "Decimal");
            TestBuiltinType("string", "String");
            TestBuiltinType("char", "Char");
            TestBuiltinType("object", "Object");
        }

        [WorkItem(546566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546566")]
        [Fact]
        public void Bug16199()
        {
            var text1 = @"
namespace NS
{
    namespace Name1 {}
    class Name2 {}
    namespace Name3 {}
    class Name4 {}
    namespace Name5 {}
}
";
            var text2 = @"
namespace NS
{
    namespace Name3 {}
    class Name5 {}
    namespace Name5 {}
}
";
            var text3 = @"
namespace NS
{
    struct Name4 {}
    struct Name5 {}
}
";
            var compilation = CreateCompilation(new string[] { text1, text2, text3 });

            var ns = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("NS").Single();

            var members1 = ns.GetMembers("Name1");
            var types1 = ns.GetTypeMembers("Name1");
            Assert.Equal(1, members1.Length);
            Assert.True(types1.IsEmpty);

            var members2 = ns.GetMembers("Name2");
            var types2 = ns.GetTypeMembers("Name2");
            Assert.Equal(1, members2.Length);
            Assert.Equal(1, types2.Length);

            var members3 = ns.GetMembers("Name3");
            var types3 = ns.GetTypeMembers("Name3");
            Assert.Equal(1, members3.Length);
            Assert.True(types1.IsEmpty);

            var members4 = ns.GetMembers("Name4");
            var types4 = ns.GetTypeMembers("Name4");
            Assert.Equal(2, members4.Length);
            Assert.Equal(2, types4.Length);

            var members5 = ns.GetMembers("Name5");
            var types5 = ns.GetTypeMembers("Name5");
            Assert.Equal(3, members5.Length);
            Assert.Equal(2, types5.Length);

            compilation.VerifyDiagnostics(
                // (5,11): error CS0101: The namespace 'NS' already contains a definition for 'Name5'
                //     class Name5 {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Name5").WithArguments("Name5", "NS"),
                // (5,12): error CS0101: The namespace 'NS' already contains a definition for 'Name5'
                //     struct Name5 {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Name5").WithArguments("Name5", "NS"),
                // (4,12): error CS0101: The namespace 'NS' already contains a definition for 'Name4'
                //     struct Name4 {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Name4").WithArguments("Name4", "NS"));
        }

        [WorkItem(527531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527531")]
        [Fact]
        public void InterfaceName()
        {
            var text = @"
    class BaseTypeSpecifierClass : global::System.IComparable
    {
        public int CompareTo(object o) { return 0; }
    }
";
            var compilation = CreateEmptyCompilation(text, new[] { MscorlibRef });
            var srcSym = compilation.GlobalNamespace.GetTypeMembers("BaseTypeSpecifierClass").Single();

            var ref2 = TestReferences.SymbolsTests.InheritIComparable;
            var comp2 = CSharpCompilation.Create("Compilation2", references: new MetadataReference[] { ref2, MscorlibRef });
            var metaSym = comp2.GlobalNamespace.GetTypeMembers("BaseTypeSpecifierClass").First();
            Assert.Equal(srcSym.Interfaces()[0], metaSym.Interfaces()[0]);
        }

        [WorkItem(527532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527532")]
        [Fact]
        public void BaseTypeName()
        {
            var text = @"
    class FooAttribute : System.Attribute {}
";
            var compilation = CreateEmptyCompilation(text, new[] { MscorlibRef });
            var srcSym = compilation.GlobalNamespace.GetTypeMembers("FooAttribute").Single();

            var ref2 = TestReferences.SymbolsTests.InheritIComparable;
            var comp2 = CSharpCompilation.Create("Compilation2", references: new MetadataReference[] { ref2, MscorlibRef });
            var metaSym = comp2.GlobalNamespace.GetTypeMembers("FooAttribute").First();
            Assert.Equal(srcSym.BaseType(), metaSym.BaseType());
        }

        [WorkItem(4084, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void AccessibilityOfExplicitImpInterfaceMethod()
        {
            var text = @"
    interface I1
{
    int Method();
}
class Test : I1
{
    struct N1 { }
    int I1.Method()
    {
        return 5;
    }
}
";
            var compilation = CreateCompilation(text);
            var classC = compilation.GlobalNamespace.GetTypeMembers("Test").Single();
            var srcSym = classC.GetMembers("I1.Method").Single();

            var ref2 = TestReferences.SymbolsTests.InheritIComparable;
            var comp2 = CSharpCompilation.Create("Compilation2", references: new MetadataReference[] { ref2, MscorlibRef });
            var metaType = comp2.GlobalNamespace.GetTypeMembers("Test").Single();
            var metaSym = metaType.GetMembers("I1.Method").First();
            Assert.Equal(srcSym.DeclaredAccessibility, metaSym.DeclaredAccessibility);
        }

        private void TestBuiltinType(string keyword, string systemTypeName)
        {
            var text =
@"
class Box<T> {}
class A : Box<" + keyword + @"> {}
class B : Box<System." + systemTypeName + @"> {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var b = global.GetTypeMembers("B", 0).Single();
            var key = a.BaseType().TypeArguments()[0] as NamedTypeSymbol;
            var nam = b.BaseType().TypeArguments()[0] as NamedTypeSymbol;
            Assert.Equal(SymbolKind.NamedType, key.Kind);
            Assert.Equal(SymbolKind.NamedType, nam.Kind);
            Assert.Equal(nam, key);
        }

        /// <summary>
        /// C {}
        /// B { C GetC(); }
        /// A { void Main() { object o = B.GetC() }   - needs a references to C, but only has B
        /// </summary>
        [Fact]
        public void MissingReturnType()
        {
            var comp1 = CreateCompilation(@"public class C { }",
                assemblyName: "C");

            var C = MetadataReference.CreateFromImage(comp1.EmitToArray());

            var comp2 = CreateCompilation(@"public class B { public static C GetC() { return new C(); } }",
                assemblyName: "B",
                references: new[] { C });

            var B = MetadataReference.CreateFromImage(comp2.EmitToArray());

            var comp3 = CreateCompilation(@"public class A { public static void Main() { object o = B.GetC(); } }",
                assemblyName: "A",
                references: new[] { B });

            comp3.VerifyDiagnostics(
                // (1,57): error CS0012: The type 'C' is defined in an assembly that is not referenced.
                // You must add a reference to assembly 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B.GetC").WithArguments("C", "C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }
    }
}
