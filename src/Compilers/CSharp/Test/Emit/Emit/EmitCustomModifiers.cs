// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class EmitCustomModifiers : EmitMetadataTestBase
    {
        [Fact]
        public void Test1()
        {
            var mscorlibRef = Net40.References.mscorlib;
            string source = @"
public class A
{
    unsafe public static void Main()
    {
        Modifiers.F1(1);
        Modifiers.F2(1);
        Modifiers.F3(1);

        System.Console.WriteLine(Modifiers.F7());
        Modifiers.F8();
        Modifiers.F9();
        Modifiers.F10();

        C4.M4();
    }
}
";
            var c = CreateCompilation(source,
                new[] { TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll },
                options: TestOptions.UnsafeReleaseExe);

            CompileAndVerify(c, verify: Verification.Passes, expectedOutput:
@"F1
F2
F3
F7
F8
F9
F10
M4
");
        }

        /// <summary>
        /// Test implementing a single interface with custom modifiers.
        /// </summary>
        [Fact]
        public void TestSingleInterfaceImplementationWithCustomModifiers()
        {
            var text = @"
class Class : CppCli.CppInterface1
{
    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface1.Method1(int x)
    {
        System.Console.WriteLine(""Class.Method1({0})"", x);
    }

    //synthesize bridge method
    public void Method2(int x)
    {
        System.Console.WriteLine(""Class.Method2({0})"", x);
    }

    public static void Main()
    {
        Class c = new Class();
        CppCli.CppInterface1 ic = c;

        //c.Method1(1); //only available through iface
        c.Method2(2);
        ic.Method1(3);
        ic.Method2(4);
    }
}
";

            var expectedOutput = @"
Class.Method2(2)
Class.Method1(3)
Class.Method2(4)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Test implementing multiple (identical) interfaces with custom modifiers.
        /// </summary>
        [Fact]
        public void TestMultipleInterfaceImplementationWithCustomModifiers()
        {
            var text = @"
class Class : CppCli.CppInterface1, CppCli.CppInterface2
{
    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface1.Method1(int x)
    {
        System.Console.WriteLine(""Class.Method1a({0})"", x);
    }

    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface2.Method1(int x)
    {
        System.Console.WriteLine(""Class.Method1b({0})"", x);
    }

    //synthesize two bridge methods
    public void Method2(int x)
    {
        System.Console.WriteLine(""Class.Method2({0})"", x);
    }

    public static void Main()
    {
        Class c = new Class();
        CppCli.CppInterface1 i1c = c;
        CppCli.CppInterface2 i2c = c;

        //c.Method1(1); //only available through ifaces
        c.Method2(2);
        i1c.Method1(3);
        i1c.Method2(4);
        i2c.Method1(5);
        i2c.Method2(6);
    }
}
";

            var expectedOutput = @"
Class.Method2(2)
Class.Method1a(3)
Class.Method2(4)
Class.Method1b(5)
Class.Method2(6)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Test a direct override of a metadata method with custom modifiers.
        /// Also confirm that a source method without custom modifiers can hide
        /// a metadata method with custom modifiers (in the sense that "new" is
        /// required) but does not copy the custom modifiers.
        /// </summary>
        [Fact]
        public void TestSingleOverrideWithCustomModifiers()
        {
            var text = @"
class Class : CppCli.CppBase1
{
    //copies custom modifiers
    public override void VirtualMethod(int x)
    {
        System.Console.WriteLine(""Class.VirtualMethod({0})"", x);
    }

    //new required, does not copy custom modifiers
    public new void NonVirtualMethod(int x)
    {
        System.Console.WriteLine(""Class.NonVirtualMethod({0})"", x);
    }

    public static void Main()
    {
        Class c = new Class();
        CppCli.CppBase1 bc = c;

        c.VirtualMethod(1);
        c.NonVirtualMethod(2);
        bc.VirtualMethod(3);
        bc.NonVirtualMethod(4);
    }
}
";

            var expectedOutput = @"
Class.VirtualMethod(1)
Class.NonVirtualMethod(2)
Class.VirtualMethod(3)
CppBase1::NonVirtualMethod(4)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Test overriding a source method that overrides a metadata method with
        /// custom modifiers.  The custom modifiers should propagate to the second
        /// override as well.
        /// </summary>
        [Fact]
        public void TestRepeatedOverrideWithCustomModifiers()
        {
            var text = @"
class Base : CppCli.CppBase1
{
    //copies custom modifiers
    public override void VirtualMethod(int x)
    {
        System.Console.WriteLine(""Base.VirtualMethod({0})"", x);
    }

    //new required, does not copy custom modifiers
    public new virtual void NonVirtualMethod(int x)
    {
        System.Console.WriteLine(""Base.NonVirtualMethod({0})"", x);
    }
}

class Derived : Base
{
    //copies custom modifiers
    public override void VirtualMethod(int x)
    {
        System.Console.WriteLine(""Derived.VirtualMethod({0})"", x);
    }

    //would copy custom modifiers, but there are none
    public override void NonVirtualMethod(int x)
    {
        System.Console.WriteLine(""Derived.NonVirtualMethod({0})"", x);
    }

    public static void Main()
    {
        Derived d = new Derived();
        Base bd = d;
        CppCli.CppBase1 bbd = d;

        d.VirtualMethod(1);
        d.NonVirtualMethod(2);
        bd.VirtualMethod(3);
        bd.NonVirtualMethod(4);
        bbd.VirtualMethod(5);
        bbd.NonVirtualMethod(6);
    }
}
";

            var expectedOutput = @"
Derived.VirtualMethod(1)
Derived.NonVirtualMethod(2)
Derived.VirtualMethod(3)
Derived.NonVirtualMethod(4)
Derived.VirtualMethod(5)
CppBase1::NonVirtualMethod(6)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Test the case of a source type extending a metadata type that could implicitly
        /// implement a metadata interface with custom modifiers.  If the source type does
        /// not implement an interface method, the base method fills in and a bridge method
        /// is synthesized in the source type.  If the source type does implement an interface
        /// method, no bridge method is synthesized.
        /// </summary>
        [Fact]
        public void TestImplicitImplementationInBaseWithCustomModifiers()
        {
            var text = @"
class Class1 : CppCli.CppBase2, CppCli.CppInterface1
{
}

class Class2 : CppCli.CppBase2, CppCli.CppInterface1
{
    //copies custom modifiers
    public override void Method1(int x)
    {
        System.Console.WriteLine(""Class2.Method1({0})"", x);
    }
}

class Class3 : CppCli.CppBase2, CppCli.CppInterface1
{
    //needs a bridge, since custom modifiers are not copied
    public new void Method1(int x)
    {
        System.Console.WriteLine(""Class3.Method1({0})"", x);
    }
}

class E
{
    static void Main()
    {
        Class1 c1 = new Class1();
        CppCli.CppInterface1 ic1 = c1;

        c1.Method1(1);
        c1.Method2(2);
        ic1.Method1(3);
        ic1.Method2(4);

        System.Console.WriteLine();

        Class2 c2 = new Class2();
        CppCli.CppInterface1 ic2 = c2;

        c2.Method1(5);
        c2.Method2(6);
        ic2.Method1(7);
        ic2.Method2(8);

        System.Console.WriteLine();

        Class3 c3 = new Class3();
        CppCli.CppInterface1 ic3 = c3;

        c3.Method1(9);
        c3.Method2(10);
        ic3.Method1(11);
        ic3.Method2(12);
    }
}
";

            var expectedOutput = @"
CppBase2::Method1(1)
CppBase2::Method2(2)
CppBase2::Method1(3)
CppBase2::Method2(4)

Class2.Method1(5)
CppBase2::Method2(6)
Class2.Method1(7)
CppBase2::Method2(8)

Class3.Method1(9)
CppBase2::Method2(10)
Class3.Method1(11)
CppBase2::Method2(12)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Unlike override lookup, implicit implementation lookup ignores custom
        /// modifiers and should simply choose the most derived method that matches
        /// the interface method signature (modulo custom modifiers).
        /// </summary>
        [Fact]
        public void TestImplicitImplementationBestMatchWithCustomModifiers()
        {
            var text = @"
    class Class1 : CppCli.CppBestMatchBase2, CppCli.CppBestMatchInterface
    {
    }

    class Class2 : CppCli.CppBestMatchBase2, CppCli.CppBestMatchInterface
    {
        public new virtual void Method(int x, int y)
        {
            System.Console.WriteLine(""Class2.Method({0},{1})"", x, y);
        }
    }

class E
{
    static void Main()
    {
        new Class2().Method(1, 2);
        new Class1().Method(3, 4);
        new CppCli.CppBestMatchBase2().Method(5, 6);
        new CppCli.CppBestMatchBase1().Method(7, 8);

        System.Console.WriteLine();

        Class1 c1 = new Class1();
        CppCli.CppBestMatchBase2 b2c1 = c1;
        CppCli.CppBestMatchBase1 b1c1 = c1;
        CppCli.CppBestMatchInterface ic1 = c1;

        c1.Method(9, 10);
        b2c1.Method(11, 12);
        b1c1.Method(13, 14);
        ic1.Method(15, 16);

        System.Console.WriteLine();

        Class2 c2 = new Class2();
        CppCli.CppBestMatchBase2 b2c2 = c2;
        CppCli.CppBestMatchBase1 b1c2 = c2;
        CppCli.CppBestMatchInterface ic2 = c2;

        c2.Method(17, 18);
        b2c2.Method(19, 20);
        b1c2.Method(21, 22);
        ic2.Method(23, 24);
    }
}
";

            var expectedOutput = @"
Class2.Method(1,2)
CppBestMatchBase2::Method(3,4)
CppBestMatchBase2::Method(5,6)
CppBestMatchBase1::Method(7,8)

CppBestMatchBase2::Method(9,10)
CppBestMatchBase2::Method(11,12)
CppBestMatchBase1::Method(13,14)
CppBestMatchBase2::Method(15,16)

Class2.Method(17,18)
CppBestMatchBase2::Method(19,20)
CppBestMatchBase1::Method(21,22)
Class2.Method(23,24)
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Make sure custom modifiers can be applied to type parameters.
        /// </summary>
        [Fact]
        public void TestGenericsWithCustomModifiers()
        {
            var text = @"
    class Derived1<U, V> : Outer<U>.Inner<V>
    {
        public override void Method<W>(U[] x, V[] y, W[] z)
        {
            System.Console.WriteLine(""Derived1.Method({0}, {1}, {2})"", x.GetType().Name, y.GetType().Name, z.GetType().Name);
        }
    }

    class Derived2 : Derived1<long, short>
    {
        public override void Method<Z>(long[] x, short[] y, Z[] z)
        {
            System.Console.WriteLine(""Derived2.Method({0}, {1}, {2})"", x.GetType().Name, y.GetType().Name, z.GetType().Name);
        }
    }

class E
{
    static void Main()
    {
        Derived2 d2 = new Derived2();
        Derived1<long, short> d1d2 = d2;
        Outer<long>.Inner<short> oid2 = d2;

        d2.Method<string>(new long[0], new short[0], new string[0]);
        d1d2.Method<object>(new long[1], new short[1], new object[1]);
        oid2.Method<float>(new long[2], new short[2], new float[2]);
    }
}
";

            var expectedOutput = @"
Derived2.Method(Int64[], Int16[], String[])
Derived2.Method(Int64[], Int16[], Object[])
Derived2.Method(Int64[], Int16[], Single[])
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// Sanity check assignment conversions in the presence of custom modifiers.
        /// </summary>
        [Fact]
        public void TestAssignmentWithCustomModifiers()
        {
            var text = @"
class C : I3
{
    void I3.M1(int[] arrayWithCustomModifiers)
    {
        System.Console.WriteLine(arrayWithCustomModifiers);
        int[] a = arrayWithCustomModifiers; //RHS type is actually int const [] const
        System.Console.WriteLine(a);
        int i = arrayWithCustomModifiers[0]; //RHS type is actually int const
        System.Console.WriteLine(i);
    }
}

class E
{
    static void Main()
    {
        I3 ic = new C();
        ic.M1(new int[2]);
    }
}
";

            var expectedOutput = @"
System.Int32[]
System.Int32[]
0
".TrimStart();

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            CompileAndVerify(
                source: text,
                references: new MetadataReference[] { ilAssemblyReference },
                expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(737971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737971")]
        public void ByRefBeforeCustomModifiers()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  // Increments argument
  .method public hidebysig static void Incr(uint32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) & a) cil managed
  {
    ldarg.0
    dup
    ldind.u4
    ldc.i4.1
    add
    stind.i4
    ret
  } // end of method Test::Incr

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class C
";

            var source = @"
class Test
{
    static void Main()
    {
        uint u = 1;
        C.Incr(ref u);
        System.Console.WriteLine(u);
    }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(source, il, TargetFramework.Mscorlib40, options: TestOptions.ReleaseExe);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("Incr");
            var parameter = method.Parameters.Single();

            Assert.Equal(RefKind.Ref, parameter.RefKind);
            Assert.False(parameter.TypeWithAnnotations.CustomModifiers.IsEmpty);
            Assert.True(parameter.RefCustomModifiers.IsEmpty);

            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        [WorkItem(737971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737971")]
        public void ByRefBeforeCustomModifiersOnSourceParameter()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual instance void M(uint32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) & a) cil managed
  {
    ret
  } // end of method Test::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class D
";

            var source = @"
class D : C
{
    public override void M(ref uint u)
    {
        u++;
    }
}

class Test
{
    static void Main()
    {
        uint u = 1;
        D d = new D();
        d.M(ref u);
        System.Console.WriteLine(u);
    }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(source, il, TargetFramework.Mscorlib40, options: TestOptions.ReleaseExe);

            var baseType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var baseMethod = baseType.GetMember<MethodSymbol>("M");
            var baseParameter = baseMethod.Parameters.Single();

            Assert.Equal(RefKind.Ref, baseParameter.RefKind);
            Assert.False(baseParameter.TypeWithAnnotations.CustomModifiers.IsEmpty);
            Assert.True(baseParameter.RefCustomModifiers.IsEmpty);

            var derivedType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
            var derivedMethod = derivedType.GetMember<MethodSymbol>("M");
            var derivedParameter = derivedMethod.Parameters.Single();

            Assert.Equal(RefKind.Ref, derivedParameter.RefKind);
            Assert.False(derivedParameter.TypeWithAnnotations.CustomModifiers.IsEmpty);
            Assert.True(derivedParameter.RefCustomModifiers.IsEmpty);

            CompileAndVerify(comp, expectedOutput: "2");
        }

        [WorkItem(294553, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=294553")]
        [Fact]
        public void VoidPointerWithCustomModifiers()
        {
            var ilSource =
@".class public A
{
  // F1(void* p)
  .method public static void F1(void* p) { ret }
  // F2(const void* p)
  .method public static void F2(void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* p) { ret }
  // F3(void* const p)
  .method public static void F3(void* modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) { ret }
  // F4(const void* const p)
  .method public static void F4(void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) { ret }
}";
            var source =
@"class B
{
    static void Main()
    {
        unsafe
        {
            A.F1(null);
            A.F2(null);
            A.F3(null);
            A.F4(null);
        }
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.UnsafeReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void IntPointerWithCustomModifiers()
        {
            var ilSource =
@".class public A
{
  // F1(int* p)
  .method public static void F1(int32* p) { ret }
  // F2(const int* p)
  .method public static void F2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* p) { ret }
  // F3(int* const p)
  .method public static void F3(int32* modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) { ret }
  // F4(const int* const p)
  .method public static void F4(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) { ret }
}";
            var source =
@"class B
{
    static void Main()
    {
        unsafe
        {
            A.F1(null);
            A.F2(null);
            A.F3(null);
            A.F4(null);
        }
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.UnsafeReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, verify: Verification.FailsPEVerify);
        }
    }
}
