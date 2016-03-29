// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class TypeTests : CSharpTestBase
    {
        [Fact]
        public void Bug18280()
        {
            string brackets = "[][][][][][][][][][][][][][][][][][][][]";
            brackets += brackets; // 40
            brackets += brackets; // 80
            brackets += brackets; // 160
            brackets += brackets; // 320
            brackets += brackets; // 640
            brackets += brackets; // 1280
            brackets += brackets; // 2560
            brackets += brackets; // 5120
            brackets += brackets; // 10240

            string code = "class C {  int " + brackets + @" x; }";

            var compilation = CreateCompilationWithMscorlib(code);
            var c = compilation.GlobalNamespace.GetTypeMembers("C")[0];
            var x = c.GetMembers("x").Single() as FieldSymbol;
            var arr = x.Type.TypeSymbol;

            arr.GetHashCode();
        }

        [Fact]
        public void AlphaRenaming()
        {
            var code = @"
class A1 : A<int> {}
class A2 : A<int> {}
class A<T> {
  class B<U> {
    A<A<U>> X;
  }
}
";
            var compilation = CreateCompilationWithMscorlib(code);
            var aint1 = compilation.GlobalNamespace.GetTypeMembers("A1")[0].BaseType;  // A<int>
            var aint2 = compilation.GlobalNamespace.GetTypeMembers("A2")[0].BaseType;  // A<int>
            var b1 = aint1.GetTypeMembers("B", 1).Single();                            // A<int>.B<U>
            var b2 = aint2.GetTypeMembers("B", 1).Single();                            // A<int>.B<U>
            Assert.NotSame(b1.TypeParameters[0], b2.TypeParameters[0]);                // they've been alpha renamed independently
            Assert.Equal(b1.TypeParameters[0], b2.TypeParameters[0]);                  // but happen to be the same type
            var xtype1 = (b1.GetMembers("X")[0] as FieldSymbol).Type.TypeSymbol;                  // Types using them are the same too
            var xtype2 = (b2.GetMembers("X")[0] as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(xtype1, xtype2);
        }

        [Fact]
        public void Access1()
        {
            var text =
@"
class A {
}
struct S {
}
interface B {
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var b = global.GetTypeMembers("B", 0).Single();
            var s = global.GetTypeMembers("S").Single();
            Assert.Equal(Accessibility.Internal, a.DeclaredAccessibility);
            Assert.Equal(Accessibility.Internal, b.DeclaredAccessibility);
            Assert.Equal(Accessibility.Internal, s.DeclaredAccessibility);
        }

        [Fact]
        public void InheritedTypesCrossTrees()
        {
            var text = @"namespace MT {
    public interface IFoo { void Foo(); }
    public interface IFoo<T, R> { R Foo(T t); }
}
";
            var text1 = @"namespace MT {
    public interface IBar<T> : IFoo { void Bar(T t); }
}
";
            var text2 = @"namespace NS {
    using System;
    using MT;
    public class A<T> : IFoo<T, string>, IBar<string> {
        void IFoo.Foo() { }
        void IBar<string>.Bar(string s) { }
        public string Foo(T t) { return null; }
    }

    public class B : A<int> {}
}
";
            var text3 = @"namespace NS {
    public class C : B {}
}
";

            var comp = CreateCompilationWithMscorlib(new[] { text, text1, text2, text3 });
            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;

            var type1 = ns.GetTypeMembers("C", 0).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(0, type1.Interfaces.Length);
            Assert.Equal(3, type1.AllInterfaces.Length);
            var sorted = (from i in type1.AllInterfaces
                          orderby i.Name
                          select i).ToArray();
            var i1 = sorted[0] as NamedTypeSymbol;
            var i2 = sorted[1] as NamedTypeSymbol;
            var i3 = sorted[2] as NamedTypeSymbol;
            Assert.Equal("MT.IBar<System.String>", i1.ToTestDisplayString());
            Assert.Equal(1, i1.Arity);
            Assert.Equal("MT.IFoo<System.Int32, System.String>", i2.ToTestDisplayString());
            Assert.Equal(2, i2.Arity);
            Assert.Equal("MT.IFoo", i3.ToTestDisplayString());
            Assert.Equal(0, i3.Arity);

            Assert.Equal("B", type1.BaseType.Name);
            // B
            var type2 = type1.BaseType as NamedTypeSymbol;
            Assert.Equal(3, type2.AllInterfaces.Length);
            Assert.NotNull(type2.BaseType);
            // A<int>
            var type3 = type2.BaseType as NamedTypeSymbol;
            Assert.Equal("NS.A<System.Int32>", type3.ToTestDisplayString());
            Assert.Equal(2, type3.Interfaces.Length);
            Assert.Equal(3, type3.AllInterfaces.Length);

            var type33 = ns.GetTypeMembers("A", 1).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal("NS.A<T>", type33.ToTestDisplayString());
            Assert.Equal(2, type33.Interfaces.Length);
            Assert.Equal(3, type33.AllInterfaces.Length);
        }

        [WorkItem(537752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537752")]
        [Fact]
        public void InheritedTypesCrossComps()
        {
            var text = @"namespace MT {
    public interface IFoo { void Foo(); }
    public interface IFoo<T, R> { R Foo(T t); }
    public interface IEmpty { }
}
";
            var text1 = @"namespace MT {
    public interface IBar<T> : IFoo, IEmpty { void Bar(T t); }
}
";
            var text2 = @"namespace NS {
    using MT;
    public class A<T> : IFoo<T, string>, IBar<T>, IFoo {
        void IFoo.Foo() { }
        public string Foo(T t) { return null; }
        void IBar<T>.Bar(T t) { }
    }

    public class B : A<ulong> {}
}
";
            var text3 = @"namespace NS {
    public class C : B {}
}
";
            var comp1 = CreateCompilationWithMscorlib(text);
            var compRef1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilationWithMscorlib(new string[] { text1, text2 }, assemblyName: "Test1",
                            references: new List<MetadataReference> { compRef1 });
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CreateCompilationWithMscorlib(text3, assemblyName: "Test2",
                            references: new List<MetadataReference> { compRef2, compRef1 });

            Assert.Equal(0, comp1.GetDiagnostics().Count());
            Assert.Equal(0, comp2.GetDiagnostics().Count());
            Assert.Equal(0, comp.GetDiagnostics().Count());

            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;

            var type1 = ns.GetTypeMembers("C", 0).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(0, type1.Interfaces.Length);
            //
            Assert.Equal(4, type1.AllInterfaces.Length);
            var sorted = (from i in type1.AllInterfaces
                          orderby i.Name
                          select i).ToArray();
            var i1 = sorted[0] as NamedTypeSymbol;
            var i2 = sorted[1] as NamedTypeSymbol;
            var i3 = sorted[2] as NamedTypeSymbol;
            var i4 = sorted[3] as NamedTypeSymbol;
            Assert.Equal("MT.IBar<System.UInt64>", i1.ToTestDisplayString());
            Assert.Equal(1, i1.Arity);
            Assert.Equal("MT.IEmpty", i2.ToTestDisplayString());
            Assert.Equal(0, i2.Arity);
            Assert.Equal("MT.IFoo<System.UInt64, System.String>", i3.ToTestDisplayString());
            Assert.Equal(2, i3.Arity);
            Assert.Equal("MT.IFoo", i4.ToTestDisplayString());
            Assert.Equal(0, i4.Arity);

            Assert.Equal("B", type1.BaseType.Name);
            // B
            var type2 = type1.BaseType as NamedTypeSymbol;
            //
            Assert.Equal(4, type2.AllInterfaces.Length);
            Assert.NotNull(type2.BaseType);
            // A<ulong>
            var type3 = type2.BaseType as NamedTypeSymbol;
            // T1?
            Assert.Equal("NS.A<System.UInt64>", type3.ToTestDisplayString());
            Assert.Equal(3, type3.Interfaces.Length);
            Assert.Equal(4, type3.AllInterfaces.Length);

            var type33 = ns.GetTypeMembers("A", 1).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal("NS.A<T>", type33.ToTestDisplayString());
            Assert.Equal(3, type33.Interfaces.Length);
            Assert.Equal(4, type33.AllInterfaces.Length);
        }

        [WorkItem(537746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537746")]
        [Fact]
        public void NestedTypes()
        {
            var text = @"namespace NS
    using System;
    public class Test
    {
        private void M() {}
        internal class NestedClass {
            internal protected interface INestedFoo {}
        }
        struct NestedStruct {}
    }

    public class Test<T>
    {
        T M() { return default(T); }
        public struct NestedS<V, V1> {
            class NestedC<R> {}
        }
        interface INestedFoo<T1, T2, T3> {}
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("Test", 0).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(2, type1.GetTypeMembers().Length);

            var type2 = type1.GetTypeMembers("NestedClass").Single() as NamedTypeSymbol;
            var type3 = type1.GetTypeMembers("NestedStruct").SingleOrDefault() as NamedTypeSymbol;

            Assert.Equal(type1, type2.ContainingSymbol);
            Assert.Equal(Accessibility.Internal, type2.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, type3.TypeKind);
            // Bug
            Assert.Equal(Accessibility.Private, type3.DeclaredAccessibility);

            var type4 = type2.GetTypeMembers().First() as NamedTypeSymbol;
            Assert.Equal(type2, type4.ContainingSymbol);
            Assert.Equal(Accessibility.ProtectedOrInternal, type4.DeclaredAccessibility);
            Assert.Equal(TypeKind.Interface, type4.TypeKind);

            // Generic
            type1 = ns.GetTypeMembers("Test", 1).SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(2, type1.GetTypeMembers().Length);

            type2 = type1.GetTypeMembers("NestedS", 2).Single() as NamedTypeSymbol;
            type3 = type1.GetTypeMembers("INestedFoo", 3).SingleOrDefault() as NamedTypeSymbol;

            Assert.Equal(type1, type2.ContainingSymbol);
            Assert.Equal(Accessibility.Public, type2.DeclaredAccessibility);
            Assert.Equal(TypeKind.Interface, type3.TypeKind);
            // Bug
            Assert.Equal(Accessibility.Private, type3.DeclaredAccessibility);

            type4 = type2.GetTypeMembers().First() as NamedTypeSymbol;
            Assert.Equal(type2, type4.ContainingSymbol);
            // Bug
            Assert.Equal(Accessibility.Private, type4.DeclaredAccessibility);
            Assert.Equal(TypeKind.Class, type4.TypeKind);
        }

        [Fact]
        public void PartialTypeCrossTrees()
        {
            var text = @"
namespace MT {
    using System.Collections.Generic;
    public partial interface IFoo<T> { void Foo(); }
}
";
            var text1 = @"
namespace MT {
    using System.Collections.Generic;
    public partial interface IFoo<T> { T Foo(T t); }
}

namespace NS {
    using System;
    using MT;

    public partial class A<T> : IFoo<T>
    {
        void IFoo<T>.Foo() { }
    }
}
";
            var text2 = @"
namespace NS {
    using MT;
    public partial class A<T> : IFoo<T>
    {
        public T Foo(T t) { return default(T); }
    }
}
";

            var comp = CreateCompilationWithMscorlib(new[] { text, text1, text2 });
            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;

            var type1 = ns.GetTypeMembers("A", 1).SingleOrDefault() as NamedTypeSymbol;
            // 2 Methods + Ctor
            Assert.Equal(3, type1.GetMembers().Length);
            Assert.Equal(1, type1.Interfaces.Length);
            Assert.Equal(2, type1.Locations.Length);

            var i1 = type1.Interfaces[0] as NamedTypeSymbol;
            Assert.Equal("MT.IFoo<T>", i1.ToTestDisplayString());
            Assert.Equal(2, i1.GetMembers().Length);
            Assert.Equal(2, i1.Locations.Length);
        }

        [WorkItem(537752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537752")]
        [Fact]
        public void TypeCrossComps()
        {
            #region "Interface Impl"
            var text = @"
    public interface IFoo  {
        void M0();
    }
";

            var text1 = @"
    public class Foo : IFoo  {
        public void M0() {}
    }
";
            var comp1 = CreateCompilationWithMscorlib(text);
            var compRef1 = new CSharpCompilationReference(comp1);
            var comp = CreateCompilationWithMscorlib(text1, references: new List<MetadataReference> { compRef1 }, assemblyName: "Comp2");

            Assert.Equal(0, comp.GetDiagnostics().Count());
            #endregion

            #region "Interface Inherit"
            text = @"
    public interface IFoo  {
        void M0();
    }
";

            text1 = @"
    public interface IBar : IFoo  {
        void M1();
    }
";
            comp1 = CreateCompilationWithMscorlib(text);
            compRef1 = new CSharpCompilationReference(comp1);
            comp = CreateCompilationWithMscorlib(text1, references: new List<MetadataReference> { compRef1 }, assemblyName: "Comp2");

            Assert.Equal(0, comp.GetDiagnostics().Count());
            #endregion

            #region "Class Inherit"
            text = @"
public class A { 
    void M0() {}
}
";

            text1 = @"
public class B : A { 
    void M1() {}
}
";
            comp1 = CreateCompilationWithMscorlib(text);
            compRef1 = new CSharpCompilationReference(comp1);
            comp = CreateCompilationWithMscorlib(text1, references: new List<MetadataReference> { compRef1 }, assemblyName: "Comp2");

            Assert.Equal(0, comp.GetDiagnostics().Count());
            #endregion

            #region "Partial"
            text = @"
public partial interface IBar {
    void M0();
}

public partial class A { }
";

            text1 = @"
public partial interface IBar {
    void M1();
}

public partial class A { }
";
            comp1 = CreateCompilationWithMscorlib(text);
            compRef1 = new CSharpCompilationReference(comp1);
            comp = CreateCompilationWithMscorlib(text1, references: new List<MetadataReference> { compRef1 }, assemblyName: "Comp2");

            Assert.Equal(0, comp.GetDiagnostics().Count());
            #endregion
        }

        [Fact, WorkItem(537233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537233"), WorkItem(537313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537313")]
        public void ArrayTypes()
        {
            var text =
@"public class Test
{
    static int[,] intAryField;
    internal ulong[][,] ulongAryField;

    public string[,][] MethodWithArray(
        ref Test[, ,] refArray, 
        out object[][][] outArray, 
        params byte[] varArray) 
    { 
        outArray = null;  return null; 
    }
}";

            var comp = CreateCompilationWithMscorlib(text);
            var classTest = comp.GlobalNamespace.GetTypeMembers("Test", 0).Single();

            var field1 = classTest.GetMembers("intAryField").Single();
            Assert.Equal(classTest, field1.ContainingSymbol);
            Assert.Equal(SymbolKind.Field, field1.Kind);
            Assert.True(field1.IsDefinition);
            Assert.True(field1.IsStatic);
            var elemType1 = (field1 as FieldSymbol).Type;
            Assert.Equal(TypeKind.Array, elemType1.TypeKind);
            Assert.Equal("System.Int32[,]", elemType1.TypeSymbol.ToTestDisplayString());

            // ArrayType public API
            Assert.False(elemType1.TypeSymbol.IsStatic);
            Assert.False(elemType1.TypeSymbol.IsAbstract);
            Assert.False(elemType1.TypeSymbol.IsSealed);
            Assert.Equal(Accessibility.NotApplicable, elemType1.TypeSymbol.DeclaredAccessibility);

            field1 = classTest.GetMembers("ulongAryField").Single();
            Assert.Equal(classTest, field1.ContainingSymbol);
            Assert.Equal(SymbolKind.Field, field1.Kind);
            Assert.True(field1.IsDefinition);
            var elemType2 = (field1 as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(TypeKind.Array, elemType2.TypeKind);
            // bug 2034
            Assert.Equal("System.UInt64[][,]", elemType2.ToTestDisplayString());
            Assert.Equal("Array", elemType2.BaseType.Name);

            var method = classTest.GetMembers("MethodWithArray").Single() as MethodSymbol;
            Assert.Equal(classTest, method.ContainingSymbol);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.True(method.IsDefinition);
            var retType = (method as MethodSymbol).ReturnType.TypeSymbol;
            Assert.Equal(TypeKind.Array, retType.TypeKind);

            // ArrayType public API
            Assert.Equal(0, retType.GetAttributes().Length); // Enumerable.Empty<SymbolAttribute>()
            Assert.Equal(0, retType.GetMembers().Length); // Enumerable.Empty<Symbol>()
            Assert.Equal(0, retType.GetMembers(string.Empty).Length);
            Assert.Equal(0, retType.GetTypeMembers().Length); // Enumerable.Empty<NamedTypeSymbol>()
            Assert.Equal(0, retType.GetTypeMembers(string.Empty).Length);
            Assert.Equal(0, retType.GetTypeMembers(string.Empty, 0).Length);
            // bug 2034
            Assert.Equal("System.String[,][]", retType.ToTestDisplayString());

            var paramList = (method as MethodSymbol).Parameters;
            var p1 = method.Parameters[0];
            var p2 = method.Parameters[1];
            var p3 = method.Parameters[2];
            Assert.Equal(RefKind.Ref, p1.RefKind);
            Assert.Equal("ref Test[,,] refArray", p1.ToTestDisplayString());
            Assert.Equal(RefKind.Out, p2.RefKind);
            Assert.Equal("out System.Object[][][] outArray", p2.ToTestDisplayString());
            Assert.Equal(RefKind.None, p3.RefKind);
            Assert.Equal(TypeKind.Array, p3.Type.TypeKind);
            Assert.Equal("params System.Byte[] varArray", p3.ToTestDisplayString());
        }

        // Interfaces impl-ed by System.Array
        // .Net 2/3.0 (7) IList&[T] -> ICollection&[T] ->IEnumerable&[T]; ICloneable;
        // .Net 4.0 (9) IList&[T] -> ICollection&[T] ->IEnumerable&[T]; ICloneable; IStructuralComparable; IStructuralEquatable
        // Array T[] impl IList[T] only
        [Fact, WorkItem(537300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537300"), WorkItem(527247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527247")]
        public void ArrayTypeInterfaces()
        {
            var text = @"
public class A {
    static byte[][] AryField;
    static byte[,] AryField2;
}
";

            var compilation = CreateCompilationWithMscorlib(text);
            int[] ary = new int[2];

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var classTest = globalNS.GetTypeMembers("A").Single() as NamedTypeSymbol;

            var sym1 = (classTest.GetMembers("AryField").First() as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, sym1.Kind);
            //
            Assert.Equal(1, sym1.Interfaces.Length);
            Assert.Equal("IList", sym1.Interfaces.First().Name);

            Assert.Equal(9, sym1.AllInterfaces.Length);
            // ? Don't seem sort right
            var sorted = sym1.AllInterfaces.OrderBy(i => i.Name).ToArray();

            var i1 = sorted[0] as NamedTypeSymbol;
            var i2 = sorted[1] as NamedTypeSymbol;
            var i3 = sorted[2] as NamedTypeSymbol;
            var i4 = sorted[3] as NamedTypeSymbol;
            var i5 = sorted[4] as NamedTypeSymbol;
            var i6 = sorted[5] as NamedTypeSymbol;
            var i7 = sorted[6] as NamedTypeSymbol;
            var i8 = sorted[7] as NamedTypeSymbol;
            var i9 = sorted[8] as NamedTypeSymbol;
            Assert.Equal("System.ICloneable", i1.ToTestDisplayString());
            Assert.Equal("System.Collections.ICollection", i2.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.ICollection<System.Byte[]>", i3.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Byte[]>", i4.ToTestDisplayString());
            Assert.Equal("System.Collections.IEnumerable", i5.ToTestDisplayString());
            Assert.Equal("System.Collections.IList", i6.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.IList<System.Byte[]>", i7.ToTestDisplayString());
            Assert.Equal("System.Collections.IStructuralComparable", i8.ToTestDisplayString());
            Assert.Equal("System.Collections.IStructuralEquatable", i9.ToTestDisplayString());

            var sym2 = (classTest.GetMembers("AryField2").First() as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, sym2.Kind);
            Assert.Equal(0, sym2.Interfaces.Length);
        }

        [Fact]
        public void ArrayTypeGetHashCode()
        {
            var text = @"public class A {
    public uint[] AryField1;
    static string[][] AryField2;
    private sbyte[,,] AryField3;
    A(){}
";
            var compilation = CreateCompilationWithMscorlib(text);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var classTest = globalNS.GetTypeMembers("A").Single() as NamedTypeSymbol;

            var sym1 = (classTest.GetMembers().First() as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, sym1.Kind);
            var v1 = sym1.GetHashCode();
            var v2 = sym1.GetHashCode();
            Assert.Equal(v1, v2);

            var sym2 = (classTest.GetMembers("AryField2").First() as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, sym2.Kind);
            v1 = sym2.GetHashCode();
            v2 = sym2.GetHashCode();
            Assert.Equal(v1, v2);

            var sym3 = (classTest.GetMembers("AryField3").First() as FieldSymbol).Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, sym3.Kind);
            v1 = sym3.GetHashCode();
            v2 = sym3.GetHashCode();
            Assert.Equal(v1, v2);
        }

        [Fact, WorkItem(527114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527114")]
        public void DynamicType()
        {
            var text =
@"class A 
{
    object field1;
    dynamic field2;
}";

            var global = CreateCompilationWithMscorlib(text).GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            foreach (var m in a.GetMembers())
            {
                if (m.Name == "field1")
                {
                    var f1 = (m as FieldSymbol).Type.TypeSymbol;
                    Assert.False(f1 is ErrorTypeSymbol, f1.GetType().ToString() + " : " + f1.ToTestDisplayString());
                }
                else if (m.Name == "field2")
                {
                    Assert.Equal(SymbolKind.Field, m.Kind);

                    // dynamic is NOT implemented
                    // var f2 = (m as FieldSymbol).Type;
                    // Assert.False(f2 is ErrorTypeSymbol); // failed
                }
            }

            var obj = a.GetMembers("field1").Single();
            Assert.Equal(a, obj.ContainingSymbol);
            Assert.Equal(SymbolKind.Field, obj.Kind);
            Assert.True(obj.IsDefinition);
            var objType = (obj as FieldSymbol).Type.TypeSymbol;
            Assert.False(objType is ErrorTypeSymbol, objType.GetType().ToString() + " : " + objType.ToTestDisplayString());
            Assert.NotEqual(SymbolKind.ErrorType, objType.Kind);

            var dyn = a.GetMembers("field2").Single();
            Assert.Equal(a, dyn.ContainingSymbol);
            Assert.Equal(SymbolKind.Field, dyn.Kind);
            Assert.True(dyn.IsDefinition);
            var dynType = (obj as FieldSymbol).Type.TypeSymbol;
            Assert.False(dynType is ErrorTypeSymbol, dynType.GetType().ToString() + " : " + dynType.ToTestDisplayString()); // this is ok
            Assert.NotEqual(SymbolKind.ErrorType, dynType.Kind);
        }

        [WorkItem(537187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537187")]
        [Fact]
        public void EnumFields()
        {
            var text =
@"public enum MyEnum 
{
    One,
    Two = 2,
    Three,
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var v = comp.GlobalNamespace.GetTypeMembers("MyEnum", 0).Single();
            Assert.NotEqual(null, v);
            Assert.Equal(Accessibility.Public, v.DeclaredAccessibility);

            var fields = v.GetMembers().OfType<FieldSymbol>().ToList();
            Assert.Equal(3, fields.Count);

            CheckField(fields[0], "One", isStatic: true);
            CheckField(fields[1], "Two", isStatic: true);
            CheckField(fields[2], "Three", isStatic: true);
        }

        private void CheckField(Symbol symbol, string name, bool isStatic)
        {
            Assert.Equal(SymbolKind.Field, symbol.Kind);
            Assert.Equal(name, symbol.Name);
            Assert.Equal(isStatic, symbol.IsStatic);
        }

        [WorkItem(542479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542479")]
        [WorkItem(538320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538320")]
        [Fact] // TODO: Dev10 does not report ERR_SameFullNameAggAgg here - source wins.
        public void SourceAndMetadata_SpecialType()
        {
            var text = @"
using System;
 
namespace System
{
    public struct Void
    {
        static void Main()
        {
            System.Void.Equals(1, 1);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            compilation.VerifyDiagnostics(
                // (10,13): warning CS0436: The type 'System.Void' in '' conflicts with the imported type 'void' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //             System.Void.Equals(1, 1);
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "System.Void").WithArguments("", "System.Void", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "void"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
        }

        [WorkItem(542479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542479")]
        [WorkItem(538320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538320")]
        [Fact] // TODO: Dev10 does not report ERR_SameFullNameAggAgg here - source wins.
        public void SourceAndMetadata_NonSpecialType()
        {
            var refSource = @"
namespace N
{
    public class C {}
}";

            var csharp = @"
using System;
 
namespace N
{
    public struct C
    {
        static void Main()
        {
            N.C.Equals(1, 1);
        }
    }
}
";

            var refAsm = CreateCompilationWithMscorlib(refSource, assemblyName: "RefAsm").ToMetadataReference();
            var compilation = CreateCompilationWithMscorlib(csharp, references: new[] { refAsm });
            compilation.VerifyDiagnostics(
                // (10,13): warning CS0436: The type 'N.C' in '' conflicts with the imported type 'N.C' in 'RefAsm, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //             N.C.Equals(1, 1);
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "N.C").WithArguments("", "N.C", "RefAsm, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "N.C"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
        }

        [WorkItem(542479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542479")]
        [Fact]
        public void DuplicateType()
        {
            string referenceText = @"
namespace N
{
    public class C { }
}
";
            var compilation1 = CreateCompilationWithMscorlib(referenceText, assemblyName: "A");
            compilation1.VerifyDiagnostics();

            var compilation2 = CreateCompilationWithMscorlib(referenceText, assemblyName: "B");
            compilation2.VerifyDiagnostics();

            var testText = @"
namespace M
{
    public struct Test
    {
        static void Main()
        {
            N.C.ToString();
        }
    }
}";

            var compilation3 = CreateCompilationWithMscorlib(testText, new[] { new CSharpCompilationReference(compilation1), new CSharpCompilationReference(compilation2) });
            compilation3.VerifyDiagnostics(
                // (8,13): error CS0433: The type 'N.C' exists in both 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                //             N.C.ToString();
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "N.C").WithArguments("A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "N.C", "B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [WorkItem(320, "https://github.com/dotnet/cli/issues/320")]
        [Fact]
        public void DuplicateCoreFxPublicTypes()
        {
            var sysConsoleSrc = @"
[assembly: System.Reflection.AssemblyVersion(""4.0.0.0"")]

namespace System
{
    public static class Console 
    {
        public static void Foo() {} 
    }
}
";
            var sysConsoleRef = CreateCompilation(
                sysConsoleSrc,
                new[] { SystemRuntimePP7Ref },
                TestOptions.ReleaseDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_b03f5f7f11d50a3a),
                assemblyName: "System.Console").EmitToImageReference();

            var mainSrc = @"
System.Console.Foo(); 
Foo();
";

            var main1 = CreateCompilation(
                new[] { Parse(mainSrc, options: TestOptions.Script) },
                new[] { MscorlibRef_v46, sysConsoleRef },
                TestOptions.ReleaseDll.WithUsings("System.Console"));

            main1.VerifyDiagnostics(
                // error CS0433: The type 'Console' exists in both 'System.Console, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' and 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg).WithArguments("System.Console, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Console", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                // (1,9): error CS0433: The type 'Console' exists in both 'System.Console, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' and 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "System.Console").WithArguments("System.Console, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Console", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                // (2,9): error CS0103: The name 'Foo' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Foo").WithArguments("Foo"));

            var main2 = CreateCompilation(
                new[] { Parse(mainSrc, options: TestOptions.Script) },
                new[] { MscorlibRef_v46, sysConsoleRef, SystemRuntimeFacadeRef },
                TestOptions.ReleaseDll.WithUsings("System.Console").WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes));

            main2.VerifyDiagnostics();
        }

        [Fact]
        public void SimpleGeneric()
        {
            var text =
@"namespace NS
{
    public interface IFoo<T> {}

    internal class A<V, U> {}

    public struct S<X, Y, Z> {}
}";

            var comp = CreateCompilationWithMscorlib(text);
            var namespaceNS = comp.GlobalNamespace.GetMembers("NS").First() as NamespaceOrTypeSymbol;
            Assert.Equal(3, namespaceNS.GetMembers().Length);

            var ifoo = namespaceNS.GetTypeMembers("IFoo").First();
            Assert.Equal(namespaceNS, ifoo.ContainingSymbol);
            Assert.Equal(SymbolKind.NamedType, ifoo.Kind);
            Assert.Equal(TypeKind.Interface, ifoo.TypeKind);
            Assert.Equal(Accessibility.Public, ifoo.DeclaredAccessibility);
            Assert.Equal(1, ifoo.TypeParameters.Length);
            Assert.Equal("T", ifoo.TypeParameters[0].Name);
            Assert.Equal(1, ifoo.TypeArguments.Length);

            // Bug#932083 - Not impl
            // Assert.False(ifoo.TypeParameters[0].IsReferenceType);
            // Assert.False(ifoo.TypeParameters[0].IsValueType);

            var classA = namespaceNS.GetTypeMembers("A").First();
            Assert.Equal(namespaceNS, classA.ContainingSymbol);
            Assert.Equal(SymbolKind.NamedType, classA.Kind);
            Assert.Equal(TypeKind.Class, classA.TypeKind);
            Assert.Equal(Accessibility.Internal, classA.DeclaredAccessibility);
            Assert.Equal(2, classA.TypeParameters.Length);
            Assert.Equal("V", classA.TypeParameters[0].Name);
            Assert.Equal("U", classA.TypeParameters[1].Name);

            // same as type parameter
            Assert.Equal(2, classA.TypeArguments.Length);

            var structS = namespaceNS.GetTypeMembers("S").First();
            Assert.Equal(namespaceNS, structS.ContainingSymbol);
            Assert.Equal(SymbolKind.NamedType, structS.Kind);
            Assert.Equal(TypeKind.Struct, structS.TypeKind);
            Assert.Equal(Accessibility.Public, structS.DeclaredAccessibility);
            Assert.Equal(3, structS.TypeParameters.Length);
            Assert.Equal("X", structS.TypeParameters[0].Name);
            Assert.Equal("Y", structS.TypeParameters[1].Name);
            Assert.Equal("Z", structS.TypeParameters[2].Name);
            Assert.Equal(3, structS.TypeArguments.Length);
        }

        [WorkItem(537199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537199")]
        [Fact]
        public void UseTypeInNetModule()
        {
            var module1Ref = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule");

            var text = @"class Test
{
    Class1 a = null;
}";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var comp = CreateCompilationWithMscorlib(text, references: new[] { module1Ref });

            var globalNS = comp.SourceModule.GlobalNamespace;
            var classTest = globalNS.GetTypeMembers("Test").First();
            var varA = classTest.GetMembers("a").First() as FieldSymbol;
            Assert.Equal(SymbolKind.Field, varA.Kind);
            Assert.Equal(TypeKind.Class, varA.Type.TypeKind);
            Assert.Equal(SymbolKind.NamedType, varA.Type.Kind);
        }

        [WorkItem(537344, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537344")]
        [Fact]
        public void ClassNameWithPrecedingAtChar()
        {
            var text =
@"using System;

static class @main
{
    public static void @Main() {}

}

";
            var comp = CreateCompilation(text);
            var typeSym = comp.Assembly.GlobalNamespace.GetTypeMembers().First();
            Assert.Equal("main", typeSym.ToTestDisplayString());
            var memSym = typeSym.GetMembers("Main").First();
            Assert.Equal("void main.Main()", memSym.ToTestDisplayString());
        }

        [Fact]
        public void ReturnsVoidWithoutCorlib()
        {
            // ensure a return type of "void" remains so even when corlib is unavailable.
            string code = @"
                class Test
                {
                    void Main()
                    {
                    }
                }";
            var comp = CreateCompilation(code);
            NamedTypeSymbol testTypeSymbol = comp.Assembly.GlobalNamespace.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            MethodSymbol methodSymbol = testTypeSymbol.GetMembers("Main").Single() as MethodSymbol;
            Assert.Equal("void Test.Main()", methodSymbol.ToTestDisplayString());
        }

        [WorkItem(537437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537437")]
        [Fact]
        public void ClassWithMultipleConstr()
        {
            var text =
@"public class MyClass 
{
    public MyClass() 
    {
    }

    public MyClass(int DummyInt)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var typeSym = comp.Assembly.GlobalNamespace.GetTypeMembers("MyClass").First();
            var actual = string.Join(", ", typeSym.GetMembers().Select(symbol => symbol.ToTestDisplayString()).OrderBy(name => name));
            Assert.Equal("MyClass..ctor(), MyClass..ctor(System.Int32 DummyInt)", actual);
        }

        [WorkItem(537446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537446")]
        [Fact]
        public void BaseTypeNotDefinedInSrc()
        {
            string code = @"
public class MyClass : T1
{
}";
            var comp = CreateCompilation(code);
            NamedTypeSymbol testTypeSymbol = comp.Assembly.GlobalNamespace.GetTypeMembers("MyClass").Single() as NamedTypeSymbol;
            Assert.Equal("T1", testTypeSymbol.BaseType.ToTestDisplayString());
        }

        [WorkItem(537447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537447")]
        [Fact]
        public void IllegalTypeArgumentInBaseType()
        {
            string code = @"
public class GC1<T> {}
public class X : GC1<BOGUS> {}
";
            var comp = CreateCompilation(code);
            NamedTypeSymbol testTypeSymbol = comp.Assembly.GlobalNamespace.GetTypeMembers("X").Single() as NamedTypeSymbol;
            Assert.Equal("GC1<BOGUS>", testTypeSymbol.BaseType.ToTestDisplayString());
        }

        [WorkItem(537449, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537449")]
        [Fact]
        public void MethodInDerivedGenericClassWithParamOfIllegalGenericType()
        {
            var text =
@"public class BaseT<T> : GenericClass {}

public class SubGenericClass<T> : BaseT<T>
{        
    public void Meth3(GC1<T> t) 
    { 
    }

    public void Meth4(System.NonexistentType t)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var typeSym = comp.Assembly.GlobalNamespace.GetTypeMembers("SubGenericClass").First();
            var actualSymbols = typeSym.GetMembers();
            var actual = string.Join(", ", actualSymbols.Select(symbol => symbol.ToTestDisplayString()).OrderBy(name => name));
            Assert.Equal("SubGenericClass<T>..ctor(), void SubGenericClass<T>.Meth3(GC1<T> t), void SubGenericClass<T>.Meth4(System.NonexistentType t)", actual);
        }

        [WorkItem(537449, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537449")]
        [Fact]
        public void TestAllInterfaces()
        {
            var text =
@"
interface I1 {}
interface I2 : I1 {}
interface I3 : I1, I2 {}
interface I4 : I2, I3 {}
interface I5 : I3, I4 {}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var interfaces = global.GetTypeMembers("I5", 0).Single().AllInterfaces;
            Assert.Equal(4, interfaces.Length);
            Assert.Equal(global.GetTypeMembers("I4", 0).Single(), interfaces[0]);
            Assert.Equal(global.GetTypeMembers("I3", 0).Single(), interfaces[1]);
            Assert.Equal(global.GetTypeMembers("I2", 0).Single(), interfaces[2]);
            Assert.Equal(global.GetTypeMembers("I1", 0).Single(), interfaces[3]);
        }

        [WorkItem(2750, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void NamespaceSameNameAsMetadataClass()
        {
            var text = @"
using System;

namespace Convert
{
    class Test
    {
        protected int M() { return 0; }
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("Convert").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            var mems = type1.GetMembers();
        }

        [WorkItem(537685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537685")]
        [Fact]
        public void NamespaceMemberArity()
        {
            var text = @"
namespace NS1.NS2
{
    internal class A<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, T11> {}
    internal proteced class B<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, T11, T12> {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var ns1 = global.GetMembers("NS1").Single() as NamespaceSymbol;
            var ns2 = ns1.GetMembers("NS2").Single() as NamespaceSymbol;
            var mems = ns2.GetMembers();
            var x = mems.Length;
        }

        [WorkItem(3178, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void NamespaceSameNameAsMetadataNamespace()
        {
            var text = @"
using System;
using System.Collections.Generic;

namespace Collections {
    class Test<T> 	{
        List<T> itemList = null;
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("Collections").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("Test", 1).Single() as NamedTypeSymbol;
            var mems = type1.GetMembers();
        }

        [WorkItem(537957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537957")]
        [Fact]
        public void EmptyNameErrorSymbolErr()
        {
            var text = @"
namespace NS
{
  class A { }
  class B : A[] {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var ns1 = global.GetMembers("NS").Single() as NamespaceSymbol;
            var syma = ns1.GetMembers("A").Single() as NamedTypeSymbol;
            var bt = (ns1.GetMembers("B").FirstOrDefault() as NamedTypeSymbol).BaseType;
            Assert.Equal("Object", bt.Name);
        }

        [WorkItem(538210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538210")]
        [Fact]
        public void NestedTypeAccessibility01()
        {
            var text = @"
using System;

class A
{
    public class B : A { }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(538242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538242")]
        [Fact]
        public void PartialClassWithBaseType()
        {
            var text = @"
class C1 { }
partial class C2 : C1 {}
partial class C2 : C1 {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(537873, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537873")]
        [Fact]
        public void InaccessibleTypesSkipped()
        {
            var text = @"
class B
{
    public class A
    {
        public class X { }
    }
}
class C : B
{
    class A { } /* private */
}
class D : C
{
    A.X x;
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count(diag => !ErrorFacts.IsWarning((ErrorCode)diag.Code)));
            var global = comp.GlobalNamespace;
            var d = global.GetMembers("D").Single() as NamedTypeSymbol;
            var x = d.GetMembers("x").Single() as FieldSymbol;
            Assert.Equal("B.A.X", x.Type.TypeSymbol.ToTestDisplayString());
        }

        [WorkItem(537970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537970")]
        [Fact]
        public void ImportedVersusSource()
        {
            var text = @"
namespace System
{
    public class String { }
    public class MyString : String { }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count(e => e.Severity >= DiagnosticSeverity.Error));
            var global = comp.GlobalNamespace;
            var system = global.GetMembers("System").Single() as NamespaceSymbol;
            var mystring = system.GetMembers("MyString").Single() as NamedTypeSymbol;
            var sourceString = mystring.BaseType;
            Assert.Equal(0,
                sourceString.GetMembers()
                .Count(m => !(m is MethodSymbol) || (m as MethodSymbol).MethodKind != MethodKind.Constructor));
        }

        [Fact, WorkItem(538012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538012"), WorkItem(538580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538580")]
        public void ErrorTypeSymbolWithArity()
        {
            var text = @"
namespace N
{
    public interface IFoo<T, V, U> {}
    public interface IBar<T> {}

    public class A : NotExist<int, int>
    {
        public class BB {}
        public class B : BB, IFoo<string, byte>
        {
        }
    }

    public class C : IBar<char, string>
    {
        // NotExist is binding error, Not error symbol
        public class D : IFoo<char, ulong, NotExist>
        {
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(4, comp.GetDiagnostics().Count());

            var global = comp.SourceModule.GlobalNamespace;
            var ns = global.GetMember<NamespaceSymbol>("N");

            var typeA = ns.GetMember<NamedTypeSymbol>("A");
            var typeAb = typeA.BaseType;
            Assert.Equal(SymbolKind.ErrorType, typeAb.Kind);
            Assert.Equal(2, typeAb.Arity);

            var typeB = typeA.GetMember<NamedTypeSymbol>("B");
            Assert.Equal("BB", typeB.BaseType.Name);
            var typeBi = typeB.Interfaces.Single();
            Assert.Equal("IFoo", typeBi.Name);
            Assert.Equal(SymbolKind.ErrorType, typeBi.Kind);
            Assert.Equal(2, typeBi.Arity); //matches arity in source, not arity of desired symbol

            var typeC = ns.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(SpecialType.System_Object, typeC.BaseType.SpecialType);
            var typeCi = typeC.Interfaces.Single();
            Assert.Equal("IBar", typeCi.Name);
            Assert.Equal(SymbolKind.ErrorType, typeCi.Kind);
            Assert.Equal(2, typeCi.Arity); //matches arity in source, not arity of desired symbol

            var typeD = typeC.GetMember<NamedTypeSymbol>("D");
            var typeDi = typeD.Interfaces.Single();
            Assert.Equal("IFoo", typeDi.Name);
            Assert.Equal(3, typeDi.TypeParameters.Length);
            Assert.Equal(SymbolKind.ErrorType, typeDi.TypeArguments[2].Kind);
        }

        [Fact]
        public void ErrorWithoutInterfaceGuess()
        {
            var text = @"
class Base<T> { }
interface Interface1<T> { }
interface Interface2<T> { }

//all one on part
partial class Derived0 : Base<int, int>, Interface1<int, int> { } 
partial class Derived0 { }

//all one on part, order reversed
partial class Derived1 : Interface1<int, int>, Base<int, int> { } 
partial class Derived1 { }

//interface on first part, base type on second
partial class Derived2 : Interface1<int, int> { } 
partial class Derived2 : Base<int, int> { }

//base type on first part, interface on second
partial class Derived3 : Base<int, int> { }
partial class Derived3 : Interface1<int, int> { } 

//interfaces on both parts
partial class Derived4 : Interface1<int, int> { }
partial class Derived4 : Interface2<int, int> { } 

//interfaces on both parts, base type on first
partial class Derived5 : Base<int, int>, Interface1<int, int> { }
partial class Derived5 : Interface2<int, int> { } 

//interfaces on both parts, base type on second
partial class Derived6 : Interface2<int, int> { } 
partial class Derived6 : Base<int, int>, Interface1<int, int> { }
";

            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.SourceModule.GlobalNamespace;

            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var interface1 = global.GetMember<NamedTypeSymbol>("Interface1");
            var interface2 = global.GetMember<NamedTypeSymbol>("Interface2");

            Assert.Equal(TypeKind.Class, baseType.TypeKind);
            Assert.Equal(TypeKind.Interface, interface1.TypeKind);
            Assert.Equal(TypeKind.Interface, interface2.TypeKind);

            //we could do this with a linq query, but then we couldn't exclude specific types
            var derivedTypes = new[]
            {
                global.GetMember<NamedTypeSymbol>("Derived0"),
                global.GetMember<NamedTypeSymbol>("Derived1"),
                global.GetMember<NamedTypeSymbol>("Derived2"),
                global.GetMember<NamedTypeSymbol>("Derived3"),
                global.GetMember<NamedTypeSymbol>("Derived4"),
                global.GetMember<NamedTypeSymbol>("Derived5"),
                global.GetMember<NamedTypeSymbol>("Derived6"),
            };

            foreach (var derived in derivedTypes)
            {
                if (derived.BaseType.SpecialType != SpecialType.System_Object)
                {
                    Assert.Equal(TypeKind.Error, derived.BaseType.TypeKind);
                }
                foreach (var i in derived.Interfaces)
                {
                    Assert.Equal(TypeKind.Error, i.TypeKind);
                }
            }

            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[0].BaseType));
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[0].Interfaces.Single()));

            //everything after the first interface is an interface
            Assert.Equal(SpecialType.System_Object, derivedTypes[1].BaseType.SpecialType);
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[1].Interfaces[0]));
            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[1].Interfaces[1]));

            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[2].BaseType));
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[2].Interfaces.Single()));

            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[3].BaseType));
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[3].Interfaces.Single()));

            Assert.Equal(SpecialType.System_Object, derivedTypes[4].BaseType.SpecialType);
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[4].Interfaces[0]));
            Assert.Same(interface2, ExtractErrorGuess(derivedTypes[4].Interfaces[1]));

            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[5].BaseType));
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[5].Interfaces[0]));
            Assert.Same(interface2, ExtractErrorGuess(derivedTypes[5].Interfaces[1]));

            Assert.Same(baseType, ExtractErrorGuess(derivedTypes[6].BaseType));
            Assert.Same(interface1, ExtractErrorGuess(derivedTypes[6].Interfaces[1]));
            Assert.Same(interface2, ExtractErrorGuess(derivedTypes[6].Interfaces[0]));
        }

        private static TypeSymbol ExtractErrorGuess(NamedTypeSymbol typeSymbol)
        {
            Assert.Equal(TypeKind.Error, typeSymbol.TypeKind);
            return typeSymbol.GetNonErrorGuess();
        }

        [WorkItem(2195, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CircularNestedInterfaceDeclaration()
        {
            var text = @"
class Bar : Bar.IFoo
{
    public interface IFoo { Foo GetFoo(); }

    public class Foo { }

    public Foo GetFoo() { return null; }
}";
            var comp = CreateCompilationWithMscorlib(text);
            Assert.Empty(comp.GetDiagnostics());
            var bar = comp.GetTypeByMetadataName("Bar");
            var iFooGetFoo = comp.GetTypeByMetadataName("Bar+IFoo").GetMembers("GetFoo").Single();
            MethodSymbol getFoo = (MethodSymbol)bar.FindImplementationForInterfaceMember(iFooGetFoo);
            Assert.Equal("Bar.GetFoo()", getFoo.ToString());
        }

        [WorkItem(3684, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ExplicitlyImplementGenericInterface()
        {
            var text = @"
public interface I<Q>
{
    void Foo();
}
public class Test1<Q> : I<Q>
{
    void I<Q>.Foo() {}
}";
            var comp = CreateCompilationWithMscorlib(text);
            Assert.Empty(comp.GetDiagnostics());
        }

        [Fact]
        public void MetadataNameOfGenericTypes()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class Gen1<T,U,V>
{}
class NonGen
{}
");

            var globalNS = compilation.GlobalNamespace;
            var gen1Class = ((NamedTypeSymbol)globalNS.GetMembers("Gen1").First());
            Assert.Equal("Gen1", gen1Class.Name);
            Assert.Equal("Gen1`3", gen1Class.MetadataName);
            var nonGenClass = ((NamedTypeSymbol)globalNS.GetMembers("NonGen").First());
            Assert.Equal("NonGen", nonGenClass.Name);
            Assert.Equal("NonGen", nonGenClass.MetadataName);
            var system = ((NamespaceSymbol)globalNS.GetMembers("System").First());
            var equatable = ((NamedTypeSymbol)system.GetMembers("IEquatable").First());
            Assert.Equal("IEquatable", equatable.Name);
            Assert.Equal("IEquatable`1", equatable.MetadataName);
        }

        [WorkItem(545154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545154")]
        [ClrOnlyFact]
        public void MultiDimArray()
        {
            var r = MetadataReference.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods.AsImmutableOrNull());
            var source = @"
class Program
{
    static void Main()
    {
        MultiDimArrays.Foo(null);
    }
}
";
            CompileAndVerify(source, new[] { r });
        }

        [Fact, WorkItem(530171, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530171")]
        public void ErrorTypeTest01()
        {
            var comp = CreateCompilationWithMscorlib(@"public void TopLevelMethod() {}");

            var errSymbol = comp.SourceModule.GlobalNamespace.GetMembers().FirstOrDefault() as NamedTypeSymbol;
            Assert.NotNull(errSymbol);
            Assert.Equal("<invalid-global-code>", errSymbol.Name);
            Assert.False(errSymbol.IsErrorType(), "ErrorType");
            Assert.True(errSymbol.IsImplicitClass, "ImplicitClass");
        }

        #region "Nullable"

        [Fact, WorkItem(537195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537195")]
        public void SimpleNullable()
        {
            var text =
@"namespace NS
{
    public class A 
    {
        int? x = null;
    }
}";

            var comp = CreateCompilationWithMscorlib(text);
            var namespaceNS = comp.GlobalNamespace.GetMembers("NS").First() as NamespaceOrTypeSymbol;
            var classA = namespaceNS.GetTypeMembers("A").First();
            var varX = classA.GetMembers("x").First() as FieldSymbol;
            Assert.Equal(SymbolKind.Field, varX.Kind);
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), varX.Type.TypeSymbol.OriginalDefinition);
        }

        [Fact]
        public void BuiltInTypeNullableTest01()
        {
            var text = @"
public class NullableTest
{
    // As Field Type
    static sbyte? field01 = null;
    protected byte? field02 = (byte)(field01 ?? 1);

    // As Property Type
    public char? Prop01 { get; private set; }
    internal short? this[ushort? p1, uint? p2 = null] { set { } }

    private static int? Method01(ref long? p1, out ulong? p2) { p2 = null; return null; }
    public decimal? Method02(double? p1 = null, params float?[] ary) { return null; }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            var topType = comp.SourceModule.GlobalNamespace.GetTypeMembers("NullableTest").FirstOrDefault();
            // ------------------------------
            var mem = topType.GetMembers("field01").Single();
            var memType = (mem as FieldSymbol).Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.True(memType.CanBeAssignedNull());

            var underType = memType.GetNullableUnderlyingType();
            Assert.True(underType.IsNonNullableValueType());
            Assert.Same(comp.GetSpecialType(SpecialType.System_SByte), underType);
            // ------------------------------
            mem = topType.GetMembers("field02").Single();
            memType = (mem as FieldSymbol).Type.TypeSymbol;
            Assert.True(memType.IsNullableType());
            Assert.False(memType.CanBeConst());

            underType = memType.StrippedType();
            Assert.Same(comp.GetSpecialType(SpecialType.System_Byte), underType);
            Assert.Same(underType, memType.GetNullableUnderlyingType());
            // ------------------------------
            mem = topType.GetMembers("Prop01").Single();
            memType = (mem as PropertySymbol).Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.True(memType.CanBeAssignedNull());

            underType = memType.StrippedType();
            Assert.Same(comp.GetSpecialType(SpecialType.System_Char), underType);
            Assert.Same(underType, memType.GetNullableUnderlyingType());
            // ------------------------------
            mem = topType.GetMembers(WellKnownMemberNames.Indexer).Single();
            memType = (mem as PropertySymbol).Type.TypeSymbol;
            Assert.True(memType.CanBeAssignedNull());
            Assert.False(memType.CanBeConst());

            underType = memType.GetNullableUnderlyingType();
            Assert.True(underType.IsNonNullableValueType());
            Assert.Same(comp.GetSpecialType(SpecialType.System_Int16), underType);

            var paras = mem.GetParameters();
            Assert.Equal(2, paras.Length);
            memType = paras[0].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_UInt16), memType.GetNullableUnderlyingType());
            memType = paras[1].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_UInt32), memType.GetNullableUnderlyingType());
            // ------------------------------
            mem = topType.GetMembers("Method01").Single();
            memType = (mem as MethodSymbol).ReturnType.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.True(memType.CanBeAssignedNull());
            underType = memType.StrippedType();
            Assert.Same(comp.GetSpecialType(SpecialType.System_Int32), underType);
            Assert.Same(underType, memType.GetNullableUnderlyingType());

            paras = mem.GetParameters();
            Assert.Equal(RefKind.Ref, paras[0].RefKind);
            Assert.Equal(RefKind.Out, paras[1].RefKind);
            memType = paras[0].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Int64), memType.GetNullableUnderlyingType());
            memType = paras[1].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_UInt64), memType.GetNullableUnderlyingType());
            // ------------------------------
            mem = topType.GetMembers("Method02").Single();
            memType = (mem as MethodSymbol).ReturnType.TypeSymbol;
            Assert.True(memType.IsNullableType());
            underType = memType.GetNullableUnderlyingType();
            Assert.True(underType.IsNonNullableValueType());
            Assert.Same(comp.GetSpecialType(SpecialType.System_Decimal), underType);
            Assert.Equal("decimal?", memType.ToDisplayString());

            paras = mem.GetParameters();
            Assert.True(paras[0].IsOptional);
            Assert.True(paras[1].IsParams);
            memType = paras[0].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Double), memType.GetNullableUnderlyingType());
            memType = paras[1].Type.TypeSymbol;
            Assert.True(memType.IsArray());
            Assert.Same(comp.GetSpecialType(SpecialType.System_Single), (memType as ArrayTypeSymbol).ElementType.TypeSymbol.GetNullableUnderlyingType());
        }

        [Fact]
        public void EnumStructNullableTest01()
        {
            var text = @"
public enum E {    Zero, One, Two    }

public struct S
{
    public struct Nested { }

    public delegate S? Dele(S? p1, E? p2 = E.Zero);
    event Dele efield;

    public static implicit operator Nested?(S? p)
    {
        return null;
    }

    public static E? operator +(S? p1, Nested? p)
    {
        return null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            var topType = comp.SourceModule.GlobalNamespace.GetTypeMembers("S").FirstOrDefault();
            var nestedType = topType.GetTypeMembers("Nested").Single();
            var enumType = comp.SourceModule.GlobalNamespace.GetTypeMembers("E").Single();
            // ------------------------------
            var mem = topType.GetMembers("efield").Single();
            var deleType = (mem as EventSymbol).Type.TypeSymbol;
            Assert.True(deleType.IsDelegateType());
            var memType = deleType.DelegateInvokeMethod().ReturnType.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);

            var paras = deleType.DelegateParameters();
            Assert.False(paras[0].IsOptional);
            Assert.True(paras[1].IsOptional);
            memType = paras[0].Type.TypeSymbol;
            Assert.Same(topType, memType.GetNullableUnderlyingType());
            memType = paras[1].Type.TypeSymbol;
            Assert.Same(enumType, memType.GetNullableUnderlyingType());
            Assert.Equal("E?", memType.ToDisplayString());
            // ------------------------------
            mem = topType.GetMembers(WellKnownMemberNames.ImplicitConversionName).Single();
            memType = (mem as MethodSymbol).ReturnType.TypeSymbol;
            Assert.True(memType.IsNullableType());
            Assert.False(memType.CanBeConst());

            var underType = memType.GetNullableUnderlyingType();
            Assert.True(underType.IsNonNullableValueType());
            Assert.Same(nestedType, underType);

            paras = (mem as MethodSymbol).GetParameters();
            Assert.Same(topType, paras[0].Type.TypeSymbol.GetNullableUnderlyingType());
            // ------------------------------
            mem = topType.GetMembers(WellKnownMemberNames.AdditionOperatorName).Single();
            memType = (mem as MethodSymbol).ReturnType.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.True(memType.CanBeAssignedNull());

            paras = mem.GetParameters();
            memType = paras[0].Type.TypeSymbol;
            Assert.Same(topType, memType.GetNullableUnderlyingType());
            memType = paras[1].Type.TypeSymbol;
            Assert.Same(nestedType, memType.GetNullableUnderlyingType());
        }

        [Fact]
        public void LocalNullableTest01()
        {
            var text = @"
using System;
using System.Collections;

class A
{
    static void M(DictionaryEntry? p = null)
    {
        System.IO.FileAccess? local01 = null;
        Action<char?, PlatformID?> local02 = delegate(char? p1, PlatformID? p2) { ; };
        Func<decimal?> local03 = () => 0.123m;
        var local04 = new { p0 = local01, p1 = new { p1 = local02, local03 }, p };

        // NYI - PlatformID?[] { PlatformID.MacOSX, null, 0 }
    }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var mnode = (MethodDeclarationSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.MethodDeclaration);
            var localvars = model.AnalyzeDataFlow(mnode.Body).VariablesDeclared;
            var locals = localvars.OrderBy(s => s.Name).Select(s => s).ToArray();
            // 4 locals + 2 lambda params
            Assert.Equal(6, locals.Length);
            // local04
            var anonymousType = (locals[3] as LocalSymbol).Type.TypeSymbol;
            Assert.True(anonymousType.IsAnonymousType);

            // --------------------
            // local01
            var memType = anonymousType.GetMember<PropertySymbol>("p0").Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.Same((locals[0] as LocalSymbol).Type.TypeSymbol, memType);

            // --------------------
            var nestedType = anonymousType.GetMember<PropertySymbol>("p1").Type.TypeSymbol;
            Assert.True(nestedType.IsAnonymousType);
            // local02
            memType = nestedType.GetMember<PropertySymbol>("p1").Type.TypeSymbol;
            Assert.True(memType.IsDelegateType());
            Assert.Same((locals[1] as LocalSymbol).Type.TypeSymbol, memType);
            // 
            var paras = memType.DelegateInvokeMethod().Parameters;
            memType = paras[0].Type.TypeSymbol;
            Assert.True(memType.IsNullableType());
            Assert.False(memType.CanBeConst());
            memType = paras[1].Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.True(memType.GetNullableUnderlyingType().IsEnumType());
            Assert.Equal("System.PlatformID?", memType.ToDisplayString());

            // local03
            memType = nestedType.GetMember<PropertySymbol>("local03").Type.TypeSymbol;
            Assert.Same((locals[2] as LocalSymbol).Type.TypeSymbol, memType);
            Assert.True(memType.IsDelegateType());
            // return type
            memType = memType.DelegateInvokeMethod().ReturnType.TypeSymbol;
            Assert.True(memType.IsNullableType());
            Assert.True(memType.CanBeAssignedNull());
            // --------------------
            // method parameter symbol
            var compType = (model.GetDeclaredSymbol(mnode) as MethodSymbol).Parameters[0].Type.TypeSymbol;
            memType = anonymousType.GetMember<PropertySymbol>("p").Type.TypeSymbol;
            Assert.Same(compType, memType);
            Assert.True(memType.IsNullableType());
            Assert.Equal("System.Collections.DictionaryEntry?", memType.ToDisplayString());
        }

        [Fact]
        public void TypeParameterNullableTest01()
        {
            var text = @"
using System;
using System.Collections.Generic;

namespace NS
{
    interface IFoo<T, R> where T : struct where R: struct
    {
        R? M<V>(ref T? p1, V? p2) where V: struct;
    }

    struct SFoo<T> : IFoo<T, PlatformID> where T : struct
    {
        PlatformID? IFoo<T, PlatformID>.M<V>(ref T? p1, V? p2) { return null; }
    }

    class CFoo
    {
        static void Main() 
        {
            IFoo<float, PlatformID> obj = new SFoo<float>();
            float? f = null;
            var ret = /*<bind0>*/obj/*</bind0>*/.M<decimal>(ref /*<bind1>*/f/*</bind1>*/, /*<bind2>*/null/*</bind2>*/);
        }
    }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var node1 = (LocalDeclarationStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.LocalDeclarationStatement, 3);
            var loc = node1.Declaration.Variables.First();
            var sym = model.GetDeclaredSymbol(node1.Declaration.Variables.First()) as LocalSymbol;
            // --------------------
            // R?
            var memType = sym.Type.TypeSymbol;
            Assert.Same(comp.GetSpecialType(SpecialType.System_Nullable_T), memType.OriginalDefinition);
            Assert.Equal("System.PlatformID?", memType.ToDisplayString());

            var nodes = GetBindingNodes<SyntaxNode>(comp).ToList();
            var tinfo = model.GetTypeInfo(nodes[0] as IdentifierNameSyntax);
            // obj: IFoo<float, PlatformID>
            Assert.NotNull(tinfo.Type);
            Assert.True(((TypeSymbol)tinfo.Type).IsInterfaceType());
            Assert.Equal("NS.IFoo<float, System.PlatformID>", tinfo.Type.ToDisplayString());
            // f: T? -> float?
            tinfo = model.GetTypeInfo(nodes[1] as IdentifierNameSyntax);
            Assert.True(((TypeSymbol)tinfo.Type).IsNullableType());
            Assert.Equal("float?", tinfo.Type.ToDisplayString());
            // decimal?
            tinfo = model.GetTypeInfo(nodes[2] as LiteralExpressionSyntax);
            Assert.True(((TypeSymbol)tinfo.ConvertedType).IsNullableType());
            Assert.Same(comp.GetSpecialType(SpecialType.System_Decimal), ((TypeSymbol)tinfo.ConvertedType).GetNullableUnderlyingType());
            Assert.Equal("decimal?", tinfo.ConvertedType.ToDisplayString());
        }

        #endregion

        [Fact]
        public void DynamicVersusObject()
        {
            var code = @"
using System;
class Foo {
    dynamic X;
    object Y;
    Func<dynamic> Z;
    Func<object> W;
}
";
            var compilation = CreateCompilationWithMscorlib(code);
            var Foo = compilation.GlobalNamespace.GetTypeMembers("Foo")[0];
            var Dynamic = (Foo.GetMembers("X")[0] as FieldSymbol).Type.TypeSymbol;
            var Object = (Foo.GetMembers("Y")[0] as FieldSymbol).Type.TypeSymbol;
            var Func_Dynamic = (Foo.GetMembers("Z")[0] as FieldSymbol).Type.TypeSymbol;
            var Func_Object = (Foo.GetMembers("W")[0] as FieldSymbol).Type.TypeSymbol;

            var comparator = TypeSymbol.EqualsIgnoringDynamicComparer;
            Assert.NotEqual(Object, Dynamic);
            Assert.Equal(comparator.GetHashCode(Dynamic), comparator.GetHashCode(Object));
            Assert.True(comparator.Equals(Dynamic, Object));
            Assert.True(comparator.Equals(Object, Dynamic));

            Assert.NotEqual(Func_Object, Func_Dynamic);
            Assert.Equal(comparator.GetHashCode(Func_Dynamic), comparator.GetHashCode(Func_Object));
            Assert.True(comparator.Equals(Func_Dynamic, Func_Object));
            Assert.True(comparator.Equals(Func_Object, Func_Dynamic));
        }

        [Fact]
        public void UnboundGenericTypeEquality()
        {
            var code = @"
class C<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib(code);
            var originalDefinition = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var unboundGeneric1 = originalDefinition.AsUnboundGenericType();
            var unboundGeneric2 = originalDefinition.AsUnboundGenericType();
            Assert.Equal(unboundGeneric1, unboundGeneric2);
        }

        [Fact]
        public void SymbolInfoForUnboundGenericTypeObjectCreation()
        {
            var code = @"
class C<T>
{
    static void Main()
    {
        var c = new C<>();
    }
}
";
            var compilation = CreateCompilationWithMscorlib(code);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(syntax);
            var symbol = info.Symbol;
            var originalDefinition = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.Equal(originalDefinition.InstanceConstructors.Single(), symbol.OriginalDefinition);
            Assert.False(symbol.ContainingType.IsUnboundGenericType);
            Assert.IsType<UnboundArgumentErrorTypeSymbol>(symbol.ContainingType.TypeArguments.Single());
        }
    }
}
