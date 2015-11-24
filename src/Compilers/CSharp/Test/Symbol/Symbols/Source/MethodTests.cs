// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MethodTests : CSharpTestBase
    {
        [Fact]
        public void Simple1()
        {
            var text =
@"
class A {
    void M(int x) {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            Assert.NotEqual(null, m);
            Assert.Equal(true, m.ReturnsVoid);
            var x = m.Parameters[0];
            Assert.Equal("x", x.Name);
            Assert.Equal(SymbolKind.NamedType, x.Type.Kind);
            Assert.Equal("Int32", x.Type.Name); // fully qualified to work around a metadata reader bug
            Assert.Equal(SymbolKind.Parameter, x.Kind);
            Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
        }

        [Fact]
        public void NoParameterlessCtorForStruct()
        {
            var text = "struct A { A() {} }";
            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(1, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(537194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537194")]
        [Fact]
        public void DefaultCtor1()
        {
            Action<string, string, int, Accessibility?> check =
                (source, className, ctorCount, accessibility) =>
                {
                    var comp = CreateCompilationWithMscorlib(source);
                    var global = comp.GlobalNamespace;
                    var a = global.GetTypeMembers(className, 0).Single();
                    var ctors = a.InstanceConstructors; // Note, this only returns *instance* constructors.
                    Assert.Equal(ctorCount, ctors.Length);
                    foreach (var ct in ctors)
                    {
                        Assert.Equal(
                            ct.IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName,
                            ct.Name
                        );

                        if (accessibility != null)
                            Assert.Equal(accessibility, ct.DeclaredAccessibility);
                    }
                };

            Accessibility? doNotCheckAccessibility = null;

            // A class without any defined constructors should generator a public constructor.
            check(@"internal class A { }", "A", 1, Accessibility.Public);

            // An abstract class without any defined constructors should generator a protected constructor.
            check(@"abstract internal class A { }", "A", 1, Accessibility.Protected);

            // A static class should not generate a default constructor
            check(@"static internal class A { }", "A", 0, doNotCheckAccessibility);

            // A class with a defined instance constructor should not generate a default constructor.
            check(@"internal class A { A(int x) {} }", "A", 1, doNotCheckAccessibility);

            // A class with only a static constructor defined should still generate a default instance constructor.
            check(@"internal class A { static A(int x) {} }", "A", 1, doNotCheckAccessibility);
        }

        [WorkItem(537345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537345")]
        [Fact]
        public void Ctor1()
        {
            var text =
@"
class A {
    A(int x) {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.InstanceConstructors.Single();
            Assert.NotEqual(null, m);
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m.Name);
            Assert.Equal(true, m.ReturnsVoid);
            Assert.Equal(MethodKind.Constructor, m.MethodKind);
            var x = m.Parameters[0];
            Assert.Equal("x", x.Name);
            Assert.Equal(SymbolKind.NamedType, x.Type.Kind);
            Assert.Equal("Int32", x.Type.Name); // fully qualified to work around a metadata reader bug
            Assert.Equal(SymbolKind.Parameter, x.Kind);
            Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
        }

        [Fact]
        public void Simple2()
        {
            var text =
@"
class A {
    void M<T>(int x) {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            Assert.NotEqual(null, m);
            Assert.Equal(true, m.ReturnsVoid);
            Assert.Equal(MethodKind.Ordinary, m.MethodKind);
            var x = m.Parameters[0];
            Assert.Equal("x", x.Name);
            Assert.Equal(SymbolKind.NamedType, x.Type.Kind);
            Assert.Equal("Int32", x.Type.Name); // fully qualified to work around a metadata reader bug
            Assert.Equal(SymbolKind.Parameter, x.Kind);
            Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
        }

        [Fact]
        public void Access1()
        {
            var text =
@"
class A {
    void M1() {}
}
interface B {
    void M2() {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m1 = a.GetMembers("M1").Single() as MethodSymbol;
            var b = global.GetTypeMembers("B", 0).Single();
            var m2 = b.GetMembers("M2").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
        }

        [Fact]
        public void GenericParameter()
        {
            var text =
@"
public class MyList<T>
{
    public void Add(T element)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var mylist = global.GetTypeMembers("MyList", 1).Single();
            var t1 = mylist.TypeParameters[0];
            var add = mylist.GetMembers("Add").Single() as MethodSymbol;
            var element = add.Parameters[0];
            var t2 = element.Type;
            Assert.Equal(t1, t2);
        }

        [Fact]
        public void PartialLocation()
        {
            var text =
@"
public partial class A {
  partial void M();
}
public partial class A {
  partial void M() {}
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M");
            Assert.Equal(1, m.Length);
            Assert.Equal(1, m.First().Locations.Length);
        }

        [Fact]
        public void FullName()
        {
            var text =
@"
public class A {
  public string M(int x);
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            Assert.Equal("System.String A.M(System.Int32 x)", m.ToTestDisplayString());
        }

        [Fact]
        public void TypeParameterScope()
        {
            var text =
@"
public interface A {
  T M<T>(T t);
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            var t = m.TypeParameters[0];
            Assert.Equal(t, m.Parameters[0].Type);
            Assert.Equal(t, m.ReturnType);
        }

        [WorkItem(931142, "DevDiv/Personal")]
        [Fact]
        public void RefOutParameterType()
        {
            var text = @"public class A {
  void M(ref A refp, out long outp) { }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            var p1 = m.Parameters[0];
            var p2 = m.Parameters[1];
            Assert.Equal(RefKind.Ref, p1.RefKind);
            Assert.Equal(RefKind.Out, p2.RefKind);

            var refP = p1.Type;
            Assert.Equal(TypeKind.Class, refP.TypeKind);
            Assert.True(refP.IsReferenceType);
            Assert.False(refP.IsValueType);
            Assert.Equal("Object", refP.BaseType.Name);
            Assert.Equal(2, refP.GetMembers().Length); // M + generated constructor.
            Assert.Equal(1, refP.GetMembers("M").Length);

            var outP = p2.Type;
            Assert.Equal(TypeKind.Struct, outP.TypeKind);
            Assert.False(outP.IsReferenceType);
            Assert.True(outP.IsValueType);
            Assert.False(outP.IsStatic);
            Assert.False(outP.IsAbstract);
            Assert.True(outP.IsSealed);
            Assert.Equal(Accessibility.Public, outP.DeclaredAccessibility);
            Assert.Equal(5, outP.Interfaces.Length);
            Assert.Equal(0, outP.GetTypeMembers().Length); // Enumerable.Empty<NamedTypeSymbol>()
            Assert.Equal(0, outP.GetTypeMembers(String.Empty).Length);
            Assert.Equal(0, outP.GetTypeMembers(String.Empty, 0).Length);
        }

        [Fact]
        public void RefReturn()
        {
            var text =
@"public class A
{
    ref int M(ref int i)
    {
        return ref i;
    }
}
";

            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m = a.GetMembers("M").Single() as MethodSymbol;
            Assert.Equal(RefKind.Ref, m.RefKind);
            Assert.Equal(TypeKind.Struct, m.ReturnType.TypeKind);
            Assert.False(m.ReturnType.IsReferenceType);
            Assert.True(m.ReturnType.IsValueType);
            var p1 = m.Parameters[0];
            Assert.Equal(RefKind.Ref, p1.RefKind);

            Assert.Equal("ref System.Int32 A.M(ref System.Int32 i)", m.ToTestDisplayString());
        }

        [Fact]
        public void BothKindsOfCtors()
        {
            var text =
@"public class Test
{
    public Test() {}
    public static Test() {}
}";

            var comp = CreateCompilationWithMscorlib(text);
            var classTest = comp.GlobalNamespace.GetTypeMembers("Test", 0).Single();
            var members = classTest.GetMembers();
            Assert.Equal(2, members.Length);
        }

        [WorkItem(931663, "DevDiv/Personal")]
        [Fact]
        public void RefOutArrayParameter()
        {
            var text =
@"public class Test
{
    public void MethodWithRefOutArray(ref int[] ary1, out string[] ary2)
    {
        ary2 = null;
    }
}";

            var comp = CreateCompilationWithMscorlib(text);
            var classTest = comp.GlobalNamespace.GetTypeMembers("Test", 0).Single();
            var method = classTest.GetMembers("MethodWithRefOutArray").Single() as MethodSymbol;
            Assert.Equal(classTest, method.ContainingSymbol);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.True(method.IsDefinition);

            // var paramList = (method as MethodSymbol).Parameters;
            var p1 = method.Parameters[0];
            var p2 = method.Parameters[1];
            Assert.Equal(RefKind.Ref, p1.RefKind);
            Assert.Equal(RefKind.Out, p2.RefKind);
        }

        [Fact]
        public void InterfaceImplementsCrossTrees()
        {
            var text1 =
@"using System;
using System.Collections.Generic;

namespace NS
{
  public class Abc {}

  public interface IFoo<T>
  {
    void M(ref T t);
  }

  public interface I1
  {
    void M(ref string p);
    int M1(short p1, params object[] ary);
  }
  
  public interface I2 : I1 
  {
    void M21(); 
    Abc M22(ref Abc p);
  }
}";

            var text2 =
@"using System;
using System.Collections.Generic;

namespace NS.NS1
{
  public class Impl : I2, IFoo<string>, I1
  {
    void IFoo<string>.M(ref string p) { }
    void I1.M(ref string p) { }
    public int M1(short p1, params object[] ary) { return p1; }
    public void M21() {}
    public Abc M22(ref Abc p) { return p; }
  }

  struct S<T>: IFoo<T>
  {
    void IFoo<T>.M(ref T t) {}
  }
}";

            var comp = CreateCompilationWithMscorlib(new[] { text1, text2 });
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var ns1 = ns.GetMembers("NS1").Single() as NamespaceSymbol;

            var classImpl = ns1.GetTypeMembers("Impl", 0).Single() as NamedTypeSymbol;
            Assert.Equal(3, classImpl.Interfaces.Length);
            // 
            var itfc = classImpl.Interfaces.First() as NamedTypeSymbol;
            Assert.Equal(1, itfc.Interfaces.Length);
            itfc = itfc.Interfaces.First() as NamedTypeSymbol;
            Assert.Equal("I1", itfc.Name);

            // explicit interface member names include the explicit interface
            var mems = classImpl.GetMembers("M");
            Assert.Equal(0, mems.Length);
            //var mem1 = mems.First() as MethodSymbol;
            // not impl
            // Assert.Equal(MethodKind.ExplicitInterfaceImplementation, mem1.MethodKind);
            // Assert.Equal(1, mem1.ExplicitInterfaceImplementation.Count());

            var mem1 = classImpl.GetMembers("M22").Single() as MethodSymbol;
            // not impl
            // Assert.Equal(0, mem1.ExplicitInterfaceImplementation.Count());
            var param = mem1.Parameters.First() as ParameterSymbol;
            Assert.Equal(RefKind.Ref, param.RefKind);
            Assert.Equal("ref NS.Abc p", param.ToTestDisplayString());

            var structImpl = ns1.GetTypeMembers("S").Single() as NamedTypeSymbol;
            Assert.Equal(1, structImpl.Interfaces.Length);
            itfc = structImpl.Interfaces.First() as NamedTypeSymbol;
            Assert.Equal("NS.IFoo<T>", itfc.ToTestDisplayString());
            //var mem2 = structImpl.GetMembers("M").Single() as MethodSymbol;
            // not impl
            // Assert.Equal(1, mem2.ExplicitInterfaceImplementation.Count());
        }

        [Fact]
        public void AbstractVirtualMethodsCrossTrees()
        {
            var text = @"
namespace MT  {
    public interface IFoo  {
        void M0();
    }
}
";

            var text1 = @"
namespace N1  {
    using MT;
    public abstract class Abc : IFoo  {
        public abstract void M0();
        public char M1;
        public abstract object M2(ref object p1);
        public virtual void M3(ulong p1, out ulong p2) { p2 = p1; }
        public virtual object M4(params object[] ary) { return null; }
        public static void M5<T>(T t) { }
        public abstract ref int M6(ref int i);
    }
}
";

            var text2 = @"
namespace N1.N2  {
    public class Bbc : Abc  {
        public override void M0() { }
        public override object M2(ref object p1) { M1 = 'a'; return p1; }
        public sealed override void M3(ulong p1, out ulong p2) { p2 = p1; }
        public override object M4(params object[] ary) { return null; }
        public static new void M5<T>(T t) { }
        public override ref int M6(ref int i) { return ref i; }
    }
}
";

            var comp = CreateExperimentalCompilationWithMscorlib45(new[] { text, text1, text2 });
            Assert.Equal(0, comp.GetDiagnostics().Count());
            var ns = comp.GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol;
            var ns1 = ns.GetMembers("N2").Single() as NamespaceSymbol;

            #region "Bbc"
            var type1 = ns1.GetTypeMembers("Bbc", 0).Single() as NamedTypeSymbol;
            var mems = type1.GetMembers();
            Assert.Equal(7, mems.Length);
            // var sorted = mems.Orderby(m => m.Name).ToArray();
            var sorted = (from m in mems
                          orderby m.Name
                          select m).ToArray();

            var m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.False(m0.IsAbstract);
            Assert.False(m0.IsOverride);
            Assert.False(m0.IsSealed);
            Assert.False(m0.IsVirtual);

            var m1 = sorted[1] as MethodSymbol;
            Assert.Equal("M0", m1.Name);
            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsOverride);
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsVirtual);

            var m2 = sorted[2] as MethodSymbol;
            Assert.Equal("M2", m2.Name);
            Assert.False(m2.IsAbstract);
            Assert.True(m2.IsOverride);
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsVirtual);

            var m3 = sorted[3] as MethodSymbol;
            Assert.Equal("M3", m3.Name);
            Assert.False(m3.IsAbstract);
            Assert.True(m3.IsOverride);
            Assert.True(m3.IsSealed);
            Assert.False(m3.IsVirtual);

            var m4 = sorted[4] as MethodSymbol;
            Assert.Equal("M4", m4.Name);
            Assert.False(m4.IsAbstract);
            Assert.True(m4.IsOverride);
            Assert.False(m4.IsSealed);
            Assert.False(m4.IsVirtual);

            var m5 = sorted[5] as MethodSymbol;
            Assert.Equal("M5", m5.Name);
            Assert.False(m5.IsAbstract);
            Assert.False(m5.IsOverride);
            Assert.False(m5.IsSealed);
            Assert.False(m5.IsVirtual);
            Assert.True(m5.IsStatic);

            var m6 = sorted[6] as MethodSymbol;
            Assert.Equal("M6", m6.Name);
            Assert.False(m6.IsAbstract);
            Assert.True(m6.IsOverride);
            Assert.False(m6.IsSealed);
            Assert.False(m6.IsVirtual);
            #endregion

            #region "Abc"
            var type2 = type1.BaseType;
            Assert.Equal("Abc", type2.Name);
            mems = type2.GetMembers();

            Assert.Equal(8, mems.Length);
            sorted = (from m in mems
                      orderby m.Name
                      select m).ToArray();

            var mm = sorted[2] as FieldSymbol;
            Assert.Equal("M1", mm.Name);
            Assert.False(mm.IsAbstract);
            Assert.False(mm.IsOverride);
            Assert.False(mm.IsSealed);
            Assert.False(mm.IsVirtual);

            m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.Equal(Accessibility.Protected, m0.DeclaredAccessibility);

            m1 = sorted[1] as MethodSymbol;
            Assert.Equal("M0", m1.Name);
            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsOverride);
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsVirtual);

            m2 = sorted[3] as MethodSymbol;
            Assert.Equal("M2", m2.Name);
            Assert.True(m2.IsAbstract);
            Assert.False(m2.IsOverride);
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsVirtual);

            m3 = sorted[4] as MethodSymbol;
            Assert.Equal("M3", m3.Name);
            Assert.False(m3.IsAbstract);
            Assert.False(m3.IsOverride);
            Assert.False(m3.IsSealed);
            Assert.True(m3.IsVirtual);

            m4 = sorted[5] as MethodSymbol;
            Assert.Equal("M4", m4.Name);
            Assert.False(m4.IsAbstract);
            Assert.False(m4.IsOverride);
            Assert.False(m4.IsSealed);
            Assert.True(m4.IsVirtual);

            m5 = sorted[6] as MethodSymbol;
            Assert.Equal("M5", m5.Name);
            Assert.False(m5.IsAbstract);
            Assert.False(m5.IsOverride);
            Assert.False(m5.IsSealed);
            Assert.False(m5.IsVirtual);
            Assert.True(m5.IsStatic);

            m6 = sorted[7] as MethodSymbol;
            Assert.Equal("M6", m6.Name);
            Assert.True(m6.IsAbstract);
            Assert.False(m6.IsOverride);
            Assert.False(m6.IsSealed);
            Assert.False(m6.IsVirtual);
            #endregion
        }

        [WorkItem(537752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537752")]
        [Fact]
        public void AbstractVirtualMethodsCrossComps()
        {
            var text = @"
namespace MT  {
    public interface IFoo  {
        void M0();
    }
}
";

            var text1 = @"
namespace N1  {
    using MT;
    public abstract class Abc : IFoo  {
        public abstract void M0();
        public char M1;
        public abstract object M2(ref object p1);
        public virtual void M3(ulong p1, out ulong p2) { p2 = p1; }
        public virtual object M4(params object[] ary) { return null; }
        public static void M5<T>(T t) { }
        public abstract ref int M6(ref int i);
    }
}
";

            var text2 = @"
namespace N1.N2  {
    public class Bbc : Abc  {
        public override void M0() { }
        public override object M2(ref object p1) { M1 = 'a'; return p1; }
        public sealed override void M3(ulong p1, out ulong p2) { p2 = p1; }
        public override object M4(params object[] ary) { return null; }
        public static new void M5<T>(T t) { }
        public override ref int M6(ref int i) { return ref i; }
    }
}
";

            var comp1 = CreateExperimentalCompilationWithMscorlib45(text);
            var compRef1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateExperimentalCompilationWithMscorlib45(new string[] { text1 }, new List<MetadataReference>() { compRef1 }, assemblyName: "Test2");
            //Compilation.Create(outputName: "Test2", options: CompilationOptions.Default,
            //                    syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text1) },
            //                    references: new MetadataReference[] { compRef1, GetCorlibReference() });
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CreateExperimentalCompilationWithMscorlib45(new string[] { text2 }, new List<MetadataReference>() { compRef1, compRef2 }, assemblyName: "Test3");
            //Compilation.Create(outputName: "Test3", options: CompilationOptions.Default,
            //                        syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text2) },
            //                        references: new MetadataReference[] { compRef1, compRef2, GetCorlibReference() });

            Assert.Equal(0, comp1.GetDiagnostics().Count());
            Assert.Equal(0, comp2.GetDiagnostics().Count());
            Assert.Equal(0, comp.GetDiagnostics().Count());
            //string errs = String.Empty;
            //foreach (var e in comp.GetDiagnostics())
            //{
            //    errs += e.Info.ToString() + "\r\n";
            //}
            //Assert.Equal("Errs", errs);

            var ns = comp.GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol;
            var ns1 = ns.GetMembers("N2").Single() as NamespaceSymbol;

            #region "Bbc"
            var type1 = ns1.GetTypeMembers("Bbc", 0).Single() as NamedTypeSymbol;
            var mems = type1.GetMembers();
            Assert.Equal(7, mems.Length);
            // var sorted = mems.Orderby(m => m.Name).ToArray();
            var sorted = (from m in mems
                          orderby m.Name
                          select m).ToArray();

            var m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.False(m0.IsAbstract);
            Assert.False(m0.IsOverride);
            Assert.False(m0.IsSealed);
            Assert.False(m0.IsVirtual);

            var m1 = sorted[1] as MethodSymbol;
            Assert.Equal("M0", m1.Name);
            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsOverride);
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsVirtual);

            var m2 = sorted[2] as MethodSymbol;
            Assert.Equal("M2", m2.Name);
            Assert.False(m2.IsAbstract);
            Assert.True(m2.IsOverride);
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsVirtual);

            var m3 = sorted[3] as MethodSymbol;
            Assert.Equal("M3", m3.Name);
            Assert.False(m3.IsAbstract);
            Assert.True(m3.IsOverride);
            Assert.True(m3.IsSealed);
            Assert.False(m3.IsVirtual);

            var m4 = sorted[4] as MethodSymbol;
            Assert.Equal("M4", m4.Name);
            Assert.False(m4.IsAbstract);
            Assert.True(m4.IsOverride);
            Assert.False(m4.IsSealed);
            Assert.False(m4.IsVirtual);

            var m5 = sorted[5] as MethodSymbol;
            Assert.Equal("M5", m5.Name);
            Assert.False(m5.IsAbstract);
            Assert.False(m5.IsOverride);
            Assert.False(m5.IsSealed);
            Assert.False(m5.IsVirtual);
            Assert.True(m5.IsStatic);

            var m6 = sorted[6] as MethodSymbol;
            Assert.Equal("M6", m6.Name);
            Assert.False(m6.IsAbstract);
            Assert.True(m6.IsOverride);
            Assert.False(m6.IsSealed);
            Assert.False(m6.IsVirtual);
            #endregion

            #region "Abc"
            var type2 = type1.BaseType;
            Assert.Equal("Abc", type2.Name);
            mems = type2.GetMembers();
            Assert.Equal(8, mems.Length);
            sorted = (from m in mems
                      orderby m.Name
                      select m).ToArray();

            var mm = sorted[2] as FieldSymbol;
            Assert.Equal("M1", mm.Name);
            Assert.False(mm.IsAbstract);
            Assert.False(mm.IsOverride);
            Assert.False(mm.IsSealed);
            Assert.False(mm.IsVirtual);

            m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.False(m0.IsAbstract);
            Assert.False(m0.IsOverride);
            Assert.False(m0.IsSealed);
            Assert.False(m0.IsVirtual);

            m1 = sorted[1] as MethodSymbol;
            Assert.Equal("M0", m1.Name);
            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsOverride);
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsVirtual);

            m2 = sorted[3] as MethodSymbol;
            Assert.Equal("M2", m2.Name);
            Assert.True(m2.IsAbstract);
            Assert.False(m2.IsOverride);
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsVirtual);

            m3 = sorted[4] as MethodSymbol;
            Assert.Equal("M3", m3.Name);
            Assert.False(m3.IsAbstract);
            Assert.False(m3.IsOverride);
            Assert.False(m3.IsSealed);
            Assert.True(m3.IsVirtual);

            m4 = sorted[5] as MethodSymbol;
            Assert.Equal("M4", m4.Name);
            Assert.False(m4.IsAbstract);
            Assert.False(m4.IsOverride);
            Assert.False(m4.IsSealed);
            Assert.True(m4.IsVirtual);

            m5 = sorted[6] as MethodSymbol;
            Assert.Equal("M5", m5.Name);
            Assert.False(m5.IsAbstract);
            Assert.False(m5.IsOverride);
            Assert.False(m5.IsSealed);
            Assert.False(m5.IsVirtual);
            Assert.True(m5.IsStatic);

            m6 = sorted[7] as MethodSymbol;
            Assert.Equal("M6", m6.Name);
            Assert.True(m6.IsAbstract);
            Assert.False(m6.IsOverride);
            Assert.False(m6.IsSealed);
            Assert.False(m6.IsVirtual);
            #endregion
        }

        [Fact]
        public void OverloadMethodsCrossTrees()
        {
            var text = @"
using System;
namespace NS
{
    public class A
    {
        public void Overloads(ushort p) { }
        public void Overloads(A p) { }
    }
}
";

            var text1 = @"
namespace NS
{
    using System;
    public class B : A
    {
        public void Overloads(ref A p) { }
        public string Overloads(B p) { return null; }
        protected long Overloads(A p, long p2) { return p2; }
    }
}
";

            var text2 = @"
namespace NS  {
    public class Test  {
        public class C : B  {
            protected long Overloads(A p, B p2) { return 1; }
        }
        public static void Main()  {
            var obj = new C();
            A a = obj;
            obj.Overloads(ref a);
            obj.Overloads(obj);
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(new[] { text, text1, text2 });
            // Not impl errors
            // Assert.Equal(0, comp.GetDiagnostics().Count());

            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;

            var type1 = (ns.GetTypeMembers("Test").Single() as NamedTypeSymbol).GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility);

            var mems = type1.GetMembers();
            Assert.Equal(2, mems.Length);

            var mems1 = type1.BaseType.GetMembers();
            Assert.Equal(4, mems1.Length);

            var mems2 = type1.BaseType.BaseType.GetMembers();
            Assert.Equal(3, mems2.Length);

            var list = new List<Symbol>();
            list.AddRange(mems);
            list.AddRange(mems1);
            list.AddRange(mems2);
            var sorted = (from m in list
                          orderby m.Name
                          select m).ToArray();

            var m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            m0 = sorted[1] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            m0 = sorted[2] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);

            var m1 = sorted[3] as MethodSymbol;
            Assert.Equal("System.Int64 NS.Test.C.Overloads(NS.A p, NS.B p2)", m1.ToTestDisplayString());
            m1 = sorted[4] as MethodSymbol;
            Assert.Equal("void NS.B.Overloads(ref NS.A p)", m1.ToTestDisplayString());
            m1 = sorted[5] as MethodSymbol;
            Assert.Equal("System.String NS.B.Overloads(NS.B p)", m1.ToTestDisplayString());
            m1 = sorted[6] as MethodSymbol;
            Assert.Equal("System.Int64 NS.B.Overloads(NS.A p, System.Int64 p2)", m1.ToTestDisplayString());
            m1 = sorted[7] as MethodSymbol;
            Assert.Equal("void NS.A.Overloads(System.UInt16 p)", m1.ToTestDisplayString());
            m1 = sorted[8] as MethodSymbol;
            Assert.Equal("void NS.A.Overloads(NS.A p)", m1.ToTestDisplayString());
        }

        [WorkItem(537752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537752")]
        [Fact]
        public void OverloadMethodsCrossComps()
        {
            var text = @"
namespace NS
{
    public class A
    {
        public void Overloads(ushort p) { }
        public void Overloads(A p) { }
    }
}
";

            var text1 = @"
namespace NS
{
    public class B : A
    {
        public void Overloads(ref A p) { }
        public string Overloads(B p) { return null; }
        protected long Overloads(A p, long p2) { return p2; }
    }
}
";

            var text2 = @"
namespace NS  {
    public class Test  {
        public class C : B  {
            protected long Overloads(A p, B p2) { return 1; }
        }
        public static void Main()  {
            C obj = new C(); // var NotImpl ???
            A a = obj;
            obj.Overloads(ref a);
            obj.Overloads(obj);
        }
    }
}
";

            var comp1 = CreateCompilationWithMscorlib(text);
            var compRef1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilationWithMscorlib(new string[] { text1 }, new List<MetadataReference>() { compRef1 }, assemblyName: "Test2");
            //Compilation.Create(outputName: "Test2", options: CompilationOptions.Default,
            //                    syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text1) },
            //                    references: new MetadataReference[] { compRef1, GetCorlibReference() });
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CreateCompilationWithMscorlib(new string[] { text2 }, new List<MetadataReference>() { compRef1, compRef2 }, assemblyName: "Test3");
            //Compilation.Create(outputName: "Test3", options: CompilationOptions.Default,
            //                        syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text2) },
            //                        references: new MetadataReference[] { compRef1, compRef2, GetCorlibReference() });

            Assert.Equal(0, comp1.GetDiagnostics().Count());
            Assert.Equal(0, comp2.GetDiagnostics().Count());
            Assert.Equal(0, comp.GetDiagnostics().Count());
            //string errs = String.Empty;
            //foreach (var e in comp.GetDiagnostics())
            //{
            //    errs += e.Info.ToString() + "\r\n";
            //}
            //Assert.Equal(String.Empty, errs);

            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = (ns.GetTypeMembers("Test").Single() as NamedTypeSymbol).GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility);

            var mems = type1.GetMembers();
            Assert.Equal(2, mems.Length);

            var mems1 = type1.BaseType.GetMembers();
            Assert.Equal(4, mems1.Length);

            var mems2 = type1.BaseType.BaseType.GetMembers();
            Assert.Equal(3, mems2.Length);

            var list = new List<Symbol>();
            list.AddRange(mems);
            list.AddRange(mems1);
            list.AddRange(mems2);
            var sorted = (from m in list
                          orderby m.Name
                          select m).ToArray();

            var m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            m0 = sorted[1] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            m0 = sorted[2] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);

            var m1 = sorted[3] as MethodSymbol;
            Assert.Equal("System.Int64 NS.Test.C.Overloads(NS.A p, NS.B p2)", m1.ToTestDisplayString());
            m1 = sorted[4] as MethodSymbol;
            Assert.Equal("void NS.B.Overloads(ref NS.A p)", m1.ToTestDisplayString());
            m1 = sorted[5] as MethodSymbol;
            Assert.Equal("System.String NS.B.Overloads(NS.B p)", m1.ToTestDisplayString());
            m1 = sorted[6] as MethodSymbol;
            Assert.Equal("System.Int64 NS.B.Overloads(NS.A p, System.Int64 p2)", m1.ToTestDisplayString());
            m1 = sorted[7] as MethodSymbol;
            Assert.Equal("void NS.A.Overloads(System.UInt16 p)", m1.ToTestDisplayString());
            m1 = sorted[8] as MethodSymbol;
            Assert.Equal("void NS.A.Overloads(NS.A p)", m1.ToTestDisplayString());
        }

        [WorkItem(537754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537754")]
        [Fact]
        public void PartialMethodsCrossTrees()
        {
            var text = @"
namespace NS
{
    public partial struct PS
    {
        partial void M0(string p);

        partial class GPC<T>
        {
            partial void GM0(T p1, short p2);
        }
    }
}
";

            var text1 = @"
namespace NS
{
    partial struct PS
    {
        partial void M0(string p) { }
        partial void M1(params ulong[] ary);

        public partial class GPC<T>
        {
            partial void GM0(T p1, short p2) { }
            partial void GM1<V>(T p1, V p2);
        }
    }
}
";

            var text2 = @"
namespace NS
{
    partial struct PS
    {
        partial void M1(params ulong[] ary) {}
        partial void M2(sbyte p);

        partial class GPC<T>
        {
            partial void GM1<V>(T p1, V p2) { }
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(new[] { text, text1, text2 });
            Assert.Equal(0, comp.GetDiagnostics().Count());

            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;

            var type1 = ns.GetTypeMembers("PS", 0).Single() as NamedTypeSymbol;
            // Bug
            // Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility);
            Assert.Equal(3, type1.Locations.Length);
            Assert.False(type1.IsReferenceType);
            Assert.True(type1.IsValueType);

            var mems = type1.GetMembers();
            Assert.Equal(5, mems.Length);
            var sorted = (from m in mems
                          orderby m.Name
                          select m).ToArray();

            var m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.Equal(Accessibility.Public, m0.DeclaredAccessibility);
            Assert.Equal(3, m0.Locations.Length);

            var m2 = sorted[2] as MethodSymbol;
            Assert.Equal("M0", m2.Name);
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility);
            Assert.Equal(1, m2.Locations.Length);
            Assert.True(m2.ReturnsVoid);

            var m3 = sorted[3] as MethodSymbol;
            Assert.Equal("M1", m3.Name);
            Assert.Equal(Accessibility.Private, m3.DeclaredAccessibility);
            Assert.Equal(1, m3.Locations.Length);

            var m4 = sorted[4] as MethodSymbol;
            Assert.Equal("M2", m4.Name);
            Assert.Equal(Accessibility.Private, m4.DeclaredAccessibility);
            Assert.Equal(1, m4.Locations.Length);

            #region "GPC"
            var type2 = sorted[1] as NamedTypeSymbol;
            Assert.Equal("NS.PS.GPC<T>", type2.ToTestDisplayString());
            Assert.True(type2.IsNestedType());
            // Bug
            Assert.Equal(Accessibility.Public, type2.DeclaredAccessibility);
            Assert.Equal(3, type2.Locations.Length);
            Assert.False(type2.IsValueType);
            Assert.True(type2.IsReferenceType);

            mems = type2.GetMembers();
            // Assert.Equal(3, mems.Count());
            sorted = (from m in mems
                      orderby m.Name
                      select m).ToArray();

            m0 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m0.Name);
            Assert.Equal(Accessibility.Public, m0.DeclaredAccessibility);
            Assert.Equal(3, m0.Locations.Length);

            var mm = sorted[1] as MethodSymbol;
            Assert.Equal("GM0", mm.Name);
            Assert.Equal(Accessibility.Private, mm.DeclaredAccessibility);
            Assert.Equal(1, mm.Locations.Length);

            m2 = sorted[2] as MethodSymbol;
            Assert.Equal("GM1", m2.Name);
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility);
            Assert.Equal(1, m2.Locations.Length);
            Assert.True(m2.ReturnsVoid);
            #endregion
        }

        [WorkItem(537755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537755")]
        [Fact]
        public void PartialMethodsWithRefParams()
        {
            var text = @"
namespace NS
{
    public partial class PC
    {
        partial void M0(ref long p);
        partial void M1(ref string p);
    }

    partial class PC
    {
        partial void M0(ref long p) {}
        partial void M1(ref string p) {}
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(0, comp.GetDiagnostics().Count());

            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("PC", 0).Single() as NamedTypeSymbol;

            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility);
            Assert.Equal(2, type1.Locations.Length);
            Assert.True(type1.IsReferenceType);
            Assert.False(type1.IsValueType);

            var mems = type1.GetMembers();
            // Bug: actual 5
            Assert.Equal(3, mems.Length);
            var sorted = (from m in mems
                          orderby m.Name
                          select m).ToArray();

            var m1 = sorted[0] as MethodSymbol;
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, m1.Name);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
            Assert.Equal(2, m1.Locations.Length);

            var m2 = sorted[1] as MethodSymbol;
            Assert.Equal("M0", m2.Name);
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility);
            Assert.Equal(1, m2.Locations.Length);
            Assert.True(m2.ReturnsVoid);

            var m3 = sorted[2] as MethodSymbol;
            Assert.Equal("M1", m3.Name);
            Assert.Equal(Accessibility.Private, m3.DeclaredAccessibility);
            Assert.Equal(1, m3.Locations.Length);
            Assert.True(m3.ReturnsVoid);
        }

        [WorkItem(2358, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var text = @"

interface ISubFuncProp
{
}

interface Interface3
{
   System.Collections.Generic.List<ISubFuncProp> Foo();
}

interface Interface3Derived : Interface3
{
}

public class DerivedClass : Interface3Derived
{
  System.Collections.Generic.List<ISubFuncProp> Interface3.Foo()
  {
    return null;
  }

  System.Collections.Generic.List<ISubFuncProp> Foo()
  {
    return null;
  }
}";

            var comp = CreateCompilationWithMscorlib(text);

            var derivedClass = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("DerivedClass")[0];
            var members = derivedClass.GetMembers();
            Assert.Equal(3, members.Length);
        }

        [Fact]
        public void SubstitutedExplicitInterfaceImplementation()
        {
            var text = @"
public class A<T>
{
    public interface I<U>
    {
        void M<V>(T t, U u, V v);
    }
}

public class B<Q, R> : A<Q>.I<R>
{
    void A<Q>.I<R>.M<S>(Q q, R r, S s) { }
}

public class C : B<int, long>
{
}";

            var comp = CreateCompilationWithMscorlib(text);

            var classB = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers("B").Single();

            var classBTypeArguments = classB.TypeArguments;
            Assert.Equal(2, classBTypeArguments.Length);
            Assert.Equal("Q", classBTypeArguments[0].Name);
            Assert.Equal("R", classBTypeArguments[1].Name);

            var classBMethodM = (MethodSymbol)classB.GetMembers().Single(sym => sym.Name.EndsWith("M", StringComparison.Ordinal));
            var classBMethodMTypeParameters = classBMethodM.TypeParameters;
            Assert.Equal(1, classBMethodMTypeParameters.Length);
            Assert.Equal("S", classBMethodMTypeParameters[0].Name);

            var classBMethodMParameters = classBMethodM.Parameters;
            Assert.Equal(3, classBMethodMParameters.Length);
            Assert.Equal(classBTypeArguments[0], classBMethodMParameters[0].Type);
            Assert.Equal(classBTypeArguments[1], classBMethodMParameters[1].Type);
            Assert.Equal(classBMethodMTypeParameters[0], classBMethodMParameters[2].Type);

            var classC = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers("C").Single();

            var classCBase = classC.BaseType;
            Assert.Equal(classB, classCBase.ConstructedFrom);

            var classCBaseTypeArguments = classCBase.TypeArguments;
            Assert.Equal(2, classCBaseTypeArguments.Length);
            Assert.Equal(SpecialType.System_Int32, classCBaseTypeArguments[0].SpecialType);
            Assert.Equal(SpecialType.System_Int64, classCBaseTypeArguments[1].SpecialType);

            var classCBaseMethodM = (MethodSymbol)classCBase.GetMembers().Single(sym => sym.Name.EndsWith("M", StringComparison.Ordinal));
            Assert.NotEqual(classBMethodM, classCBaseMethodM);

            var classCBaseMethodMTypeParameters = classCBaseMethodM.TypeParameters;
            Assert.Equal(1, classCBaseMethodMTypeParameters.Length);
            Assert.Equal("S", classCBaseMethodMTypeParameters[0].Name);

            var classCBaseMethodMParameters = classCBaseMethodM.Parameters;
            Assert.Equal(3, classCBaseMethodMParameters.Length);
            Assert.Equal(classCBaseTypeArguments[0], classCBaseMethodMParameters[0].Type);
            Assert.Equal(classCBaseTypeArguments[1], classCBaseMethodMParameters[1].Type);
            Assert.Equal(classCBaseMethodMTypeParameters[0], classCBaseMethodMParameters[2].Type);
        }

        #region Regressions

        [WorkItem(527149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527149")]
        [Fact]
        public void MethodWithParamsInParameters()
        {
            var text =
@"class C
{
    void F1(params int[] a) { }
}
";
            var comp = CreateCompilation(text);
            var c = comp.GlobalNamespace.GetTypeMembers("C").Single();
            var f1 = c.GetMembers("F1").Single() as MethodSymbol;
            Assert.Equal("void C.F1(params System.Int32[missing][] a)", f1.ToTestDisplayString());
        }

        [WorkItem(537352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537352")]
        [Fact]
        public void Arglist()
        {
            string code = @"
                class AA
                {
                   public static int Method1(__arglist)
                   {
                   }
                }";

            var comp = CreateCompilationWithMscorlib(code);
            NamedTypeSymbol nts = comp.Assembly.GlobalNamespace.GetTypeMembers()[0];
            Assert.Equal("AA", nts.ToTestDisplayString());
            Assert.Empty(comp.GetDeclarationDiagnostics());
            Assert.Equal("System.Int32 AA.Method1(__arglist)", nts.GetMembers("Method1").Single().ToTestDisplayString());
        }

        [WorkItem(537877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537877")]
        [Fact]
        public void ExpImpInterfaceWithGlobal()
        {
            var text = @"
using System;
namespace N1 
{
    interface I1
    {
        int Method();
    }
}

namespace N2
{
    class ExpImpl : N1.I1
    {
        int global::N1.I1.Method()
        {
            return 42;
        }

        ExpImpl(){}
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());

            var ns = comp.GlobalNamespace.GetMembers("N2").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("ExpImpl", 0).Single() as NamedTypeSymbol;
            var m1 = type1.GetMembers().FirstOrDefault() as MethodSymbol;
            Assert.Equal("System.Int32 N2.ExpImpl.N1.I1.Method()", m1.ToTestDisplayString());
            var em1 = m1.ExplicitInterfaceImplementations.First() as MethodSymbol;
            Assert.Equal("System.Int32 N1.I1.Method()", em1.ToTestDisplayString());
        }

        [WorkItem(537877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537877")]
        [Fact]
        public void BaseInterfaceNameWithAlias()
        {
            var text = @"
using N1Alias = N1;
namespace N1 
{
    interface I1 {}
}

namespace N2
{
    class N1Alias {}

    class Test : N1Alias::I1
    {
        static int Main() 
        {
            Test t = new Test();
            
            return 0;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());

            var n2 = comp.GlobalNamespace.GetMembers("N2").Single() as NamespaceSymbol;
            var test = n2.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            var bt = test.Interfaces.Single() as NamedTypeSymbol;
            Assert.Equal("N1.I1", bt.ToTestDisplayString());
        }

        [WorkItem(538209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538209")]
        [Fact]
        public void ParameterAccessibility01()
        {
            var text = @"
using System;
class MyClass
{
    private class MyInner
    {
        public int MyMeth(MyInner2 arg)
        {
            return arg.intI;
        }
    }
    protected class MyInner2
    {
        public int intI = 2;
    }

    public static int Main()
    {
        MyInner MI = new MyInner();
        if (MI.MyMeth(new MyInner2()) == 2)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(537877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537877")]
        [Fact]
        public void MethodsWithSameSigDiffReturnType()
        {
            var text = @"
class Test
{
    public int M1()
    {
    }

    float M1()
    {
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);

            var test = comp.GlobalNamespace.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            var members = test.GetMembers("M1");

            Assert.Equal(2, members.Length);
            Assert.Equal("System.Int32 Test.M1()", members[0].ToTestDisplayString());
            Assert.Equal("System.Single Test.M1()", members[1].ToTestDisplayString());
        }

        [Fact]
        public void OverriddenMethod01()
        {
            var text = @"
class A
{
    public virtual void F(object[] args) {}
}
class B : A
{
    public override void F(params object[] args) {}
    public static void Main(B b)
    {
        b.F(// yes, there is a parse error here
    }
}
";

            var comp = CreateCompilationWithMscorlib(text);

            var a = comp.GlobalNamespace.GetTypeMembers("A").Single() as NamedTypeSymbol;
            var b = comp.GlobalNamespace.GetTypeMembers("B").Single() as NamedTypeSymbol;
            var f = b.GetMembers("F").Single() as MethodSymbol;
            Assert.True(f.IsOverride);
            var f2 = f.OverriddenMethod;
            Assert.NotNull(f2);
            Assert.Equal("A", f2.ContainingSymbol.Name);
        }
        #endregion

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void MethodEscapedIdentifier()
        {
            var text = @"
interface @void { @void @return(@void @in); };
class @int { virtual @int @float(@int @in); };
class C1 : @int, @void
{
    @void @void.@return(@void @in) { return null; }
    override @int @float(@int @in) { return null; }
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            NamedTypeSymbol c1 = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("C1").Single();
            // Per explanation from NGafter:
            //
            // We intentionally escape keywords that appear in the type qualification of the interface name
            // on interface implementation members.  That is necessary to distinguish I<int>.F from I<@int>.F,
            // for example, which might both be members in a class.  An alternative would be to stop using the
            // abbreviated names for the built-in types, but since we may want to use these names in
            // diagnostics the @-escaped version is preferred.
            //
            MethodSymbol mreturn = (MethodSymbol)c1.GetMembers("@void.return").Single();
            Assert.Equal("@void.return", mreturn.Name);
            Assert.Equal("C1.@void.@return(@void)", mreturn.ToString());
            NamedTypeSymbol rvoid = (NamedTypeSymbol)mreturn.ReturnType;
            Assert.Equal("void", rvoid.Name);
            Assert.Equal("@void", rvoid.ToString());
            MethodSymbol mvoidreturn = (MethodSymbol)mreturn.ExplicitInterfaceImplementations.Single();
            Assert.Equal("return", mvoidreturn.Name);
            Assert.Equal("@void.@return(@void)", mvoidreturn.ToString());
            ParameterSymbol pin = mreturn.Parameters.Single();
            Assert.Equal("in", pin.Name);
            Assert.Equal("@in", pin.ToDisplayString(
                new SymbolDisplayFormat(
                    parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)));
            MethodSymbol mfloat = (MethodSymbol)c1.GetMembers("float").Single();
            Assert.Equal("float", mfloat.Name);
            Assert.Empty(c1.GetMembers("@float"));
        }

        [Fact]
        public void ExplicitInterfaceImplementationSimple()
        {
            string text = @"
interface I
{
    void Method();
}

class C : I
{
    void I.Method() { }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var globalNamespace = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("C").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classMethod = (MethodSymbol)@class.GetMembers("I.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceMethod, explicitImpl);

            var typeDef = (Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDef.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(@class, explicitOverride.ContainingType);
            Assert.Equal(classMethod, explicitOverride.ImplementingMethod);
            Assert.Equal(interfaceMethod, explicitOverride.ImplementedMethod);
            context.Diagnostics.Verify();
        }

        [Fact]
        public void ExplicitInterfaceImplementationCorLib()
        {
            string text = @"
class F : System.IFormattable
{
    string System.IFormattable.ToString(string format, System.IFormatProvider formatProvider)
    {
        return null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var globalNamespace = comp.GlobalNamespace;
            var systemNamespace = (NamespaceSymbol)globalNamespace.GetMembers("System").Single();

            var @interface = (NamedTypeSymbol)systemNamespace.GetTypeMembers("IFormattable").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("ToString").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("F").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classMethod = (MethodSymbol)@class.GetMembers("System.IFormattable.ToString").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceMethod, explicitImpl);

            var typeDef = (Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
               GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDef.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(@class, explicitOverride.ContainingType);
            Assert.Equal(classMethod, explicitOverride.ImplementingMethod);
            Assert.Equal(interfaceMethod, explicitOverride.ImplementedMethod);
            context.Diagnostics.Verify();
        }

        [Fact]
        public void ExplicitInterfaceImplementationRef()
        {
            string text = @"
interface I
{
    ref int Method(ref int i);
}

class C : I
{
    ref int I.Method(ref int i) { return ref i; }
}
";

            var comp = CreateExperimentalCompilationWithMscorlib45(text);

            var globalNamespace = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();
            Assert.Equal(RefKind.Ref, interfaceMethod.RefKind);

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("C").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classMethod = (MethodSymbol)@class.GetMembers("I.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);
            Assert.Equal(RefKind.Ref, classMethod.RefKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceMethod, explicitImpl);

            var typeDef = (Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDef.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(@class, explicitOverride.ContainingType);
            Assert.Equal(classMethod, explicitOverride.ImplementingMethod);
            Assert.Equal(interfaceMethod, explicitOverride.ImplementedMethod);
            context.Diagnostics.Verify();
        }

        [Fact]
        public void ExplicitInterfaceImplementationGeneric()
        {
            string text = @"
namespace Namespace
{
    interface I<T>
    {
        void Method(T t);
    }
}

class IC : Namespace.I<int>
{
    void Namespace.I<int>.Method(int i) { }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var globalNamespace = comp.GlobalNamespace;
            var systemNamespace = (NamespaceSymbol)globalNamespace.GetMembers("Namespace").Single();

            var @interface = (NamedTypeSymbol)systemNamespace.GetTypeMembers("I", 1).Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IC").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceMethod = (MethodSymbol)substitutedInterface.GetMembers("Method").Single();

            var classMethod = (MethodSymbol)@class.GetMembers("Namespace.I<System.Int32>.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterface, explicitImpl.ContainingType);
            Assert.Equal(substitutedInterfaceMethod.OriginalDefinition, explicitImpl.OriginalDefinition);

            var typeDef = (Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDef.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(@class, explicitOverride.ContainingType);
            Assert.Equal(classMethod, explicitOverride.ImplementingMethod);

            var explicitOverrideImplementedMethod = explicitOverride.ImplementedMethod;
            Assert.Equal(substitutedInterface, explicitOverrideImplementedMethod.GetContainingType(context));
            Assert.Equal(substitutedInterfaceMethod.Name, explicitOverrideImplementedMethod.Name);
            Assert.Equal(substitutedInterfaceMethod.Arity, explicitOverrideImplementedMethod.GenericParameterCount);
            context.Diagnostics.Verify();
        }

        [Fact()]
        public void TestMetadataVirtual()
        {
            string text = @"
class C
{
    virtual void Method1() { }
    virtual void Method2() { }
    void Method3() { }
    void Method4() { }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var @class = (NamedTypeSymbol)comp.GlobalNamespace.GetTypeMembers("C").Single();

            var method1 = (SourceMethodSymbol)@class.GetMembers("Method1").Single();
            var method2 = (SourceMethodSymbol)@class.GetMembers("Method2").Single();
            var method3 = (SourceMethodSymbol)@class.GetMembers("Method3").Single();
            var method4 = (SourceMethodSymbol)@class.GetMembers("Method4").Single();

            Assert.True(method1.IsVirtual);
            Assert.True(method2.IsVirtual);
            Assert.False(method3.IsVirtual);
            Assert.False(method4.IsVirtual);

            //1 and 3 - read before set
            Assert.True(((Cci.IMethodDefinition)method1).IsVirtual);
            Assert.False(((Cci.IMethodDefinition)method3).IsVirtual);

            //2 and 4 - set before read
            method2.EnsureMetadataVirtual();
            method4.EnsureMetadataVirtual();

            //can set twice (e.g. if the method implicitly implements more than one interface method)
            method2.EnsureMetadataVirtual();
            method4.EnsureMetadataVirtual();

            Assert.True(((Cci.IMethodDefinition)method2).IsVirtual);
            Assert.True(((Cci.IMethodDefinition)method4).IsVirtual);

            //API view unchanged
            Assert.True(method1.IsVirtual);
            Assert.True(method2.IsVirtual);
            Assert.False(method3.IsVirtual);
            Assert.False(method4.IsVirtual);
        }

        [Fact]
        public void ExplicitStaticConstructor()
        {
            string text = @"
class C
{
    static C() { }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var staticConstructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.StaticConstructorName);

            Assert.Equal(MethodKind.StaticConstructor, staticConstructor.MethodKind);
            Assert.Equal(Accessibility.Private, staticConstructor.DeclaredAccessibility);
            Assert.True(staticConstructor.IsStatic, "Static constructor should be static");
            Assert.Equal(SpecialType.System_Void, staticConstructor.ReturnType.SpecialType);
        }

        [Fact]
        public void ImplicitStaticConstructor()
        {
            string text = @"
class C
{
    static int f = 1; //initialized in implicit static constructor
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (4,16): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     static int f = 1; //initialized in implicit static constructor
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f")
            );

            var staticConstructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.StaticConstructorName);

            Assert.Equal(MethodKind.StaticConstructor, staticConstructor.MethodKind);
            Assert.Equal(Accessibility.Private, staticConstructor.DeclaredAccessibility);
            Assert.True(staticConstructor.IsStatic, "Static constructor should be static");
            Assert.Equal(SpecialType.System_Void, staticConstructor.ReturnType.SpecialType);
        }

        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void AccessorMethodAccessorOverriding()
        {
            var text = @"
public class A
{
    public virtual int P { get; set; }
}

public class B : A
{
    public virtual int get_P() { return 0; }
}

public class C : B
{
    public override int P { get; set; }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var globalNamespace = comp.GlobalNamespace;

            var classA = globalNamespace.GetMember<NamedTypeSymbol>("A");
            var classB = globalNamespace.GetMember<NamedTypeSymbol>("B");
            var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");

            var methodA = classA.GetMember<PropertySymbol>("P").GetMethod;
            var methodB = classB.GetMember<MethodSymbol>("get_P");
            var methodC = classC.GetMember<PropertySymbol>("P").GetMethod;

            var typeDefC = (Cci.ITypeDefinition)classC;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)classC.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDefC.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(classC, explicitOverride.ContainingType);
            Assert.Equal(methodC, explicitOverride.ImplementingMethod);
            Assert.Equal(methodA, explicitOverride.ImplementedMethod);
            context.Diagnostics.Verify();
        }

        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void MethodAccessorMethodOverriding()
        {
            var text = @"
public class A
{
    public virtual int get_P() { return 0; }
}

public class B : A
{
    public virtual int P { get; set; }
}

public class C : B
{
    public override int get_P() { return 0; }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var globalNamespace = comp.GlobalNamespace;

            var classA = globalNamespace.GetMember<NamedTypeSymbol>("A");
            var classB = globalNamespace.GetMember<NamedTypeSymbol>("B");
            var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");

            var methodA = classA.GetMember<MethodSymbol>("get_P");
            var methodB = classB.GetMember<PropertySymbol>("P").GetMethod;
            var methodC = classC.GetMember<MethodSymbol>("get_P");

            var typeDefC = (Cci.ITypeDefinition)classC;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)classC.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverride = typeDefC.GetExplicitImplementationOverrides(context).Single();
            Assert.Equal(classC, explicitOverride.ContainingType);
            Assert.Equal(methodC, explicitOverride.ImplementingMethod);
            Assert.Equal(methodA, explicitOverride.ImplementedMethod);
            context.Diagnostics.Verify();
        }

        [WorkItem(543444, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543444")]
        [Fact]
        public void BadArityInOperatorDeclaration()
        {
            var text =
@"class A
{
    public static bool operator true(A x, A y) { return false; }
}

class B
{
    public static B operator *(B x) { return null; }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (3,33): error CS1020: Overloadable binary operator expected
                // public static bool operator true(A x, A y) { return false; }
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "true"),
                // (8,30): error CS1019: Overloadable unary operator expected
                //     public static B operator *(B x) { return null; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "*"),
                // (3,33): error CS0216: The operator 'A.operator true(A, A)' requires a matching operator 'false' to also be defined
                //     public static bool operator true(A x, A y) { return false; }
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("A.operator true(A, A)", "false")
            );
        }

        [WorkItem(779441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/779441")]
        [Fact]
        public void UserDefinedOperatorLocation()
        {
            var source = @"
public class C
{
    public static C operator +(C c) { return null; }
}
";

            var keywordPos = source.IndexOf('+');
            var parenPos = source.IndexOf('(');

            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.UnaryPlusOperatorName).Single();
            var span = symbol.Locations.Single().SourceSpan;
            Assert.Equal(keywordPos, span.Start);
            Assert.Equal(parenPos, span.End);
        }

        [WorkItem(779441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/779441")]
        [Fact]
        public void UserDefinedConversionLocation()
        {
            var source = @"
public class C
{
    public static explicit operator string(C c) { return null; }
}
";

            var keywordPos = source.IndexOf("string", StringComparison.Ordinal);
            var parenPos = source.IndexOf('(');

            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.ExplicitConversionName).Single();
            var span = symbol.Locations.Single().SourceSpan;
            Assert.Equal(keywordPos, span.Start);
            Assert.Equal(parenPos, span.End);
        }
        [WorkItem(787708, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/787708")]
        [Fact]
        public void PartialAsyncMethodInTypeWithAttributes()
        {
            var source = @"
using System;

class Attr : Attribute
{
    public int P { get; set; }
}

[Attr(P = F)]
partial class C
{
    const int F = 1;

    partial void M();
    async partial void M() { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
              // (15,24): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
              //     async partial void M() { }
              Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M"));
        }

        [WorkItem(910100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910100")]
        [Fact]
        public void SubstitutedParameterEquality()
        {
            var source = @"
class C
{
    void M<T>(T t) { }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");

            var constructedMethod1 = method.Construct(type);
            var constructedMethod2 = method.Construct(type);
            Assert.Equal(constructedMethod1, constructedMethod2);
            Assert.NotSame(constructedMethod1, constructedMethod2);

            var substitutedParameter1 = constructedMethod1.Parameters.Single();
            var substitutedParameter2 = constructedMethod2.Parameters.Single();
            Assert.Equal(substitutedParameter1, substitutedParameter2);
            Assert.NotSame(substitutedParameter1, substitutedParameter2);
        }

        [WorkItem(910100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910100")]
        [Fact]
        public void ReducedExtensionMethodParameterEquality()
        {
            var source = @"
static class C
{
    static void M(this int i, string s) { }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");

            var reducedMethod1 = method.ReduceExtensionMethod();
            var reducedMethod2 = method.ReduceExtensionMethod();
            Assert.Equal(reducedMethod1, reducedMethod2);
            Assert.NotSame(reducedMethod1, reducedMethod2);

            var extensionParameter1 = reducedMethod1.Parameters.Single();
            var extensionParameter2 = reducedMethod2.Parameters.Single();
            Assert.Equal(extensionParameter1, extensionParameter2);
            Assert.NotSame(extensionParameter1, extensionParameter2);
        }

        [Fact]
        public void RefReturningVoidMethod()
        {
            var source = @"
static class C
{
    static ref void M() { }
}
";

            CreateExperimentalCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,12): error CS8088: Void-returning methods cannot return by reference
                //     static ref void M() { }
                Diagnostic(ErrorCode.ERR_VoidReturningMethodCannotReturnByRef, "ref").WithLocation(4, 12));
        }

        [Fact]
        public void RefReturningAsyncMethod()
        {
            var source = @"
static class C
{
    static async ref int M() { }
}
";

            CreateExperimentalCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,18): error CS1519: Invalid token 'ref' in class, struct, or interface member declaration
                //     static async ref int M() { }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "ref").WithArguments("ref").WithLocation(4, 18),
                // (4,26): error CS0708: 'M': cannot declare instance members in a static class
                //     static async ref int M() { }
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "M").WithArguments("M").WithLocation(4, 26),
                // (4,26): error CS0161: 'C.M()': not all code paths return a value
                //     static async ref int M() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 26));
        }
    }
}
