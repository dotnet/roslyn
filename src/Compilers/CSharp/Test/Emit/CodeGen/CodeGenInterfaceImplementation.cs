// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenInterfaceImplementationTests : CSharpTestBase
    {
        [Fact]
        public void TestInterfaceImplementationSimple()
        {
            // Tests:
            // Implement interface that has several base interfaces (several levels of inheritance)
            // Implement some members implicitly + some explicitly + some implicitly in base class
            // Change parameter names of implemented member 

            var source = @"
using System;
using System.Collections.Generic;
interface I1
{
    int Property { get; set; }
}
interface I2 : I1
{
    void Method<T>(int a, ref T[] b, out List<T> c);
}
interface I3 : I2
{
    void Method(int a = 3, params System.Exception[] b);
}
class Base
{
    public void Method(int b = 4, params System.Exception[] c)
    { 
        System.Console.WriteLine(""Base.Method({0})"", b);
    }
    public int Property
    {
        get
        {
            System.Console.WriteLine(""Derived.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Derived.Property.set({0})"", value);
        }
    }
}
class Derived : Base, I3
{
    public void Method<U>(int i, ref U[] j, out List<U> k)
    {
        k = null;
        System.Console.WriteLine(""Derived.Method<U>({0}, {1})"", i, j[0]);
    }
}
class Derived2 : Derived, I1
{
    int I1.Property
    {
        get
        {
            System.Console.WriteLine(""Derived2.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Derived2.Property.set({0})"", value);
        }
    }
}
class Class : I3
{
    void I3.Method(int b = 4, params System.Exception[] c)
    { 
        System.Console.WriteLine(""Class.Method({0})"", b);
    }

    void I2.Method<U>(int i, ref U[] j, out List<U> k)
    {
        k = null;
        System.Console.WriteLine(""Class.Method<U>({0}, {1})"", i, j[0]);
    }

    int I1.Property
    {
        get
        {
            System.Console.WriteLine(""Class.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Class.Property.set({0})"", value);
        }
    }
}
class Test
{
    public static void Main()
    {
        I3 i3 = new Derived2();
        string[] s1 = new string[] {""a""};
        string[] s2 = new string[] {""b""};

        List<string> s3 = null;
        i3.Method<string>(1, ref s1, out s3);
        i3.Method(2, new ArgumentException());
        int x = i3.Property;
        i3.Property = x;

        i3 = new Derived();
        i3.Method<string>(3, ref s2, out s3);
        i3.Method(4, new ArgumentException());
        x = i3.Property;
        i3.Property = x;

        i3 = new Class();
        i3.Method<string>(5, ref s2, out s3);
        i3.Method(6, new ArgumentException());
        x = i3.Property;
        i3.Property = x;
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method<U>(1, a)
Base.Method(2)
Derived2.Property.get()
Derived2.Property.set(1)
Derived.Method<U>(3, b)
Base.Method(4)
Derived.Property.get()
Derived.Property.set(1)
Class.Method<U>(5, b)
Class.Method(6)
Class.Property.get()
Class.Property.set(1)",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method([opt] System.Int32 b = 4, [System.ParamArrayAttribute()] System.Exception[] c) cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig newslot specialname virtual final instance System.Int32 get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.Int32 value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<U>(System.Int32 i, U[]& j, [out] System.Collections.Generic.List`1[U]& k) cil managed"),
                    Signature("Derived2", "I1.get_Property", ".method private hidebysig newslot specialname virtual final instance System.Int32 I1.get_Property() cil managed"),
                    Signature("Derived2", "I1.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void I1.set_Property(System.Int32 value) cil managed"),
                    Signature("Class", "I1.get_Property", ".method private hidebysig newslot specialname virtual final instance System.Int32 I1.get_Property() cil managed"),
                    Signature("Class", "I1.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void I1.set_Property(System.Int32 value) cil managed"),
                    Signature("Class", "I2.Method", ".method private hidebysig newslot virtual final instance System.Void I2.Method<U>(System.Int32 i, U[]& j, [out] System.Collections.Generic.List`1[U]& k) cil managed"),
                    Signature("Class", "I3.Method", ".method private hidebysig newslot virtual final instance System.Void I3.Method([opt] System.Int32 b = 4, [System.ParamArrayAttribute()] System.Exception[] c) cil managed")
                });
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestImplementingWithAliasedNames()
        {
            // Tests: 
            // Use aliased name for type of parameter / return type in implemented member

            var source = @"
using System;
using TypeA = Type<int>;
using TypeB = System.Int32;
using TypeC = NS.Derived;
using NSAlias2 = NS;
using NSAlias = NS;
class Type<T>
{
}
interface Interface
{
    TypeB Method(System.Exception a, TypeB b, params NS.Derived[] c);
    void Method2(TypeC c1, NSAlias2.Derived c2, NS.Derived[] c3);
    void Method3(int[] b1, TypeA b2, params int[] b3);
}
namespace NS
{
    using TypeA = System.Exception;
    using Type = Interface;
    using TypeD = Type<int>;
    abstract class Base
    {
        public void Method2(Derived c1, NS.Derived c2, params NSAlias.Derived[] C3)
        {
            Console.WriteLine(""Base.Method2( , , [{0}])"", C3.Length);
        }
    }
    class Derived : Base, Interface
    {
        public TypeB Method(TypeA A, int B, TypeC[] C)
        {
            Console.WriteLine(""Derived.Method( , {0}, [{1}])"", B, C.Length);
            return 0;
        }
        public void Method3(int[] B1, TypeD B2, params int[] b2)
        {
            Console.WriteLine(""Derived.Method3([{0}], , [{1}])"", B1.Length, b2.Length);
        }
    }
    class Class1 : Type
    {
        TypeB Type.Method(TypeA A, int B, TypeC[] C)
        {
            Console.WriteLine(""Class1.Method( , {0}, [{1}])"", B, C.Length);
            return 0;
        }
        void Interface.Method2(Derived c1, NS.Derived c2, NSAlias.Derived[] C3)
        {
            Console.WriteLine(""Class1.Method2( , , [{0}])"", C3.Length);
        }
        void Type.Method3(int[] B1, Type<TypeB> B2, params int[] B3)
        {
            Console.WriteLine(""Class1.Method3([{0}], , [{1}])"", B1.Length, B3.Length);
        }
    }
}
class Test
{
    public static void Main()
    {
        TypeC d = new TypeC();
        TypeA a = new TypeA();
        Interface b = d;
        d.Method(new System.Exception(), 1, new TypeC[]{d});
        b.Method(new System.Exception(), 2, d, d);
        b.Method2(d, d, new TypeC[]{d, d, d});
        d.Method3(new int[4]{1, 2, 3, 4}, a, new int[5] {6, 7, 8, 9, 10});
        b.Method3(new int[6]{1, 2, 3, 4, 5, 6}, a, 8, 9, 10, 11, 12, 13, 14);

        b = new NS.Class1();
        b.Method(new System.Exception(), 2, d, d);
        b.Method2(d, d, new TypeC[]{d, d, d});
        b.Method3(new int[6]{1, 2, 3, 4, 5, 6}, a, 8, 9, 10, 11, 12, 13, 14);
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method( , 1, [1])
Derived.Method( , 2, [2])
Base.Method2( , , [3])
Derived.Method3([4], , [5])
Derived.Method3([6], , [7])
Class1.Method( , 2, [2])
Class1.Method2( , , [3])
Class1.Method3([6], , [7])",
                expectedSignatures: new[]
                {
                    Signature("NS.Derived", "Method", ".method public hidebysig newslot virtual final instance System.Int32 Method(System.Exception A, System.Int32 B, NS.Derived[] C) cil managed"),
                    Signature("NS.Base", "Method2", ".method public hidebysig newslot virtual final instance System.Void Method2(NS.Derived c1, NS.Derived c2, [System.ParamArrayAttribute()] NS.Derived[] C3) cil managed"),
                    Signature("NS.Derived", "Method3", ".method public hidebysig newslot virtual final instance System.Void Method3(System.Int32[] B1, Type`1[System.Int32] B2, [System.ParamArrayAttribute()] System.Int32[] b2) cil managed"),
                    Signature("NS.Class1", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Int32 Interface.Method(System.Exception A, System.Int32 B, NS.Derived[] C) cil managed"),
                    Signature("NS.Class1", "Interface.Method2", ".method private hidebysig newslot virtual final instance System.Void Interface.Method2(NS.Derived c1, NS.Derived c2, NS.Derived[] C3) cil managed"),
                    Signature("NS.Class1", "Interface.Method3", ".method private hidebysig newslot virtual final instance System.Void Interface.Method3(System.Int32[] B1, Type`1[System.Int32] B2, [System.ParamArrayAttribute()] System.Int32[] B3) cil managed")
                });
        }

        [Fact]
        public void TestVBNestedInterfaceImplementationMetadata()
        {
            #region "Source"

            var text1 = @"using System;
public class CINestedImpl : IMeth03.INested
{
    public virtual void NestedSub(ushort p)
    {
        Console.Write(""ImpSub "");
    }

    public virtual string NestedFunc(ref object p)
    {
        Console.Write(""ImpFunc "");
        return p.ToString();
    }

    void IMeth03.INested.NestedSub(ushort p)
    {
        Console.Write(""ExpSub "");
    }

    string IMeth03.INested.NestedFunc(ref object p)
    {
        Console.Write(""ExpFunc "");
        return p.ToString();
    }
}
";

            var text2 = @"using System;
public class CINestedDerived : CINestedImpl, IMeth03.INested
{
    public override void NestedSub(ushort p)
    {
        Console.Write(""ImpSubDerived "");
    }

    public new string NestedFunc(ref object p)
    {
        Console.Write(""ImpFuncDerived "");
        return p.ToString();
    }

    void IMeth03.INested.NestedSub(ushort p)
    {
        Console.Write(""ExpSubDerived "");
    }

    string IMeth03.INested.NestedFunc(ref object p)
    {
        Console.Write(""ExpFuncDerived "");
        return p.ToString();
    }
}
";
            var text3 = @"
class Test
{
    static void Main()
    {
        CINestedDerived obj = new CINestedDerived();
        CINestedImpl bobj = obj;
        IMeth03.INested iobj = obj;
        object o = obj;

        obj.NestedSub(123);
        obj.NestedFunc(ref o);

        bobj.NestedSub(456);
        bobj.NestedFunc(ref o);

        iobj.NestedSub(789);
        iobj.NestedFunc(ref o);
    }
}
";

            #endregion

            var asmRef = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;

            var comp1 = CreateCompilationWithMscorlib(
                text1,
                references: new[] { asmRef },
                assemblyName: "OHI_ExpImpImplVBNested001");

            var comp2 = CreateCompilationWithMscorlib(
                text2,
                references: new[] { asmRef, comp1.EmitToImageReference() },
                assemblyName: "OHI_ExpImpImplVBNested002");

            var comp3 = CreateCompilationWithMscorlib(
                text3,
                references: new MetadataReference[] { asmRef, new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_ExpImpImplVBNested003");

            CompileAndVerify(comp3, expectedOutput: @"ImpSubDerived ImpFuncDerived ImpSubDerived ImpFunc ExpSubDerived ExpFuncDerived");
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses1()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses1A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base
{
}
class Derived : Base1, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived1 : Derived
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses2()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses2A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived1 : Derived, Interface
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [WorkItem(540558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540558")]
        [WorkItem(540561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540561")]
        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses3()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Derived : Base
{
    public void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                // Implementing members in Derived should not be marked as virtual final
                Signature("Derived", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                // Stubs in Derived3 "call" corresponding members in Derived above
                Signature("Derived3", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived3", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                });

            comp.VerifyDiagnostics(); // No errors
            // Stub should "call" Derived::Method / Derived::set_Property (even though we have another base class Derived1 in between)
            comp.VerifyIL("Derived3.Interface.set_Property", @"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  call       ""void Derived.Property.set""
  IL_0007:  ret       
}");
        }

        [WorkItem(540558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540558")]
        [WorkItem(540561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540561")]
        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses3A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Base1 : Base
{
}
class Derived : Base1
{
    public void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived1 : Derived
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                // Implementing members in Derived should not be marked as virtual final
                Signature("Derived", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                // Stubs in Derived3 "call" corresponding members in Derived above
                Signature("Derived3", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived3", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors

            // Stub should "call" Derived::Method / Derived::set_Property (even though we have another base class Derived1 in between)
            comp.VerifyIL("Derived3.Interface.set_Property", @"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  call       ""void Derived.Property.set""
  IL_0007:  ret       
}");
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses4()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Derived : Base, Interface
{
    public void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses4A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
    public void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived1 : Derived, Interface
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses5()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base
{
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses5A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base
{
}
class Derived : Base1
{
}
class Derived1 : Derived
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses6()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses6A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
}
class Derived1 : Derived, Interface
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property
Base.Interface.Method
Base.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses7()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses7A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base
{
}
class Derived : Base1, Interface
{
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived1 : Derived
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses8()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses8A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived1 : Derived, Interface
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method
Derived.Property
Derived.Method
Derived.Property
Derived.Method
Derived.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig newslot virtual final instance System.Void Method() cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses9()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }

    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Derived : Base, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses9A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }

    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
}
class Derived1 : Derived
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses10()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Derived : Base, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived2 : Derived
{
}
class Derived3 : Derived, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMappingAcrossBaseClasses10A()
        {
            var source = @"
using System;
interface Interface
{
    void Method();
    string Property { set; }
}
class Base : Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Base.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Base.Interface.Property""); }
    }
    public void Method()
    {
        Console.WriteLine(""Base.Method"");
    }
    public string Property
    {
        get { return null; }
        set { Console.WriteLine(""Base.Property""); }
    }
}
class Base1 : Base, Interface
{
}
class Derived : Base1, Interface
{
    void Interface.Method()
    {
        Console.WriteLine(""Derived.Interface.Method"");
    }
    string Interface.Property
    {
        set { Console.WriteLine(""Derived.Interface.Property""); }
    }
    public new void Method()
    {
        Console.WriteLine(""Derived.Method"");
    }
    public new string Property
    {
        get { return null; }
        set { Console.WriteLine(""Derived.Property""); }
    }
}
class Derived1 : Derived, Interface
{
}
class Derived2 : Derived1
{
}
class Derived3 : Derived1, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method();
        i.Property = string.Empty;

        i = new Derived2();
        i.Method();
        i.Property = string.Empty;

        i = new Derived3();
        i.Method();
        i.Property = string.Empty;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property
Derived.Interface.Method
Derived.Interface.Property",
                expectedSignatures: new[]
                {
                    Signature("Base", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Base", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Base", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived", "Interface.Method", ".method private hidebysig newslot virtual final instance System.Void Interface.Method() cil managed"),
                    Signature("Derived", "Interface.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Interface.set_Property(System.String value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig instance System.Void Method() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterface()
        {
            // Tests:
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member from base class

            var source = @"
using System;
partial interface I1<T, U>
{
    void Method<V>(T x, Func<U, T, V> v, U z);
}
class Implicit : I1<int, Int32>
{
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
partial interface I1<T, U>
{
    void Method<Z>(U x, Func<T, U, Z> v, T z);
}
class Base
{
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
class Base2 : Base { }
class ImplicitInBase : Base2, I1<int, Int32> { }
class Explicit : I1<int, Int32>
{
    void I1<Int32, Int32>.Method<V>(int x, Func<int, int, V> v, int z) { }
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
class Test
{
    public static void Main()
    {
        I1<int, int> i = new Implicit();
        i = new ImplicitInBase();
        i = new Explicit();
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: "",
                expectedSignatures: new[]
                {
                    Signature("Implicit", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<V>(System.Int32 x, System.Func`3[System.Int32,System.Int32,V] v, System.Int32 z) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<V>(System.Int32 x, System.Func`3[System.Int32,System.Int32,V] v, System.Int32 z) cil managed"),
                    Signature("Explicit", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<V>(System.Int32 x, System.Func`3[System.Int32,System.Int32,V] v, System.Int32 z) cil managed"),
                    Signature("Explicit", "I1<System.Int32,System.Int32>.Method", ".method private hidebysig newslot virtual final instance System.Void I1<System.Int32,System.Int32>.Method<V>(System.Int32 x, System.Func`3[System.Int32,System.Int32,V] v, System.Int32 z) cil managed")
                });

            comp.VerifyDiagnostics(
                // (23,27): warning CS0473: Explicit interface implementation 'Explicit.I1<int, int>.Method<V>(int, System.Func<int, int, V>, int)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Explicit.I1<int, int>.Method<V>(int, System.Func<int, int, V>, int)"));
        }

        [WorkItem(540581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540581")]
        [Fact]
        public void TestImplementAmbiguousSignaturesFromDifferentInterfaces()
        {
            // Tests:
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member from base class

            var source = @"
using System;
interface I1<T>
{
    void Method<V>(T x, Func<T, V> v);
    Action<T> Property { get; set; }
}
interface I2<U>
{
    void Method<Z>(U x, Func<U, Z> v);
    Action<U> Property { get; set; }
}
interface I3<W> : I1<W>, I2<W>
{
    void Method<Z>(W x, Func<W, Z> v);
    Action<W> Property { get; set; }
}
class Implicit : I1<int>, I2<Int32>
{
    public void Method<V>(int x, Func<int, V> v) { Console.WriteLine(""Implicit.Method""); }
    public Action<int> Property
    {
        get { Console.WriteLine(""Implicit.get_Property""); return null; }
        set { Console.WriteLine(""Implicit.set_Property""); }
    }
}
class Implicit2 : I3<string>
{
    public void Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Implicit2.Method""); }
    public Action<string> Property
    {
        get { Console.WriteLine(""Implicit2.get_Property""); return null; }
        set { Console.WriteLine(""Implicit2.set_Property""); }
    }
}
class Base
{
    public void Method<V>(int x, Func<int, V> v) { Console.WriteLine(""Base.Method - int""); }
    public void Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Base.Method - string""); }
    public Action<int> Property
    {
        get { Console.WriteLine(""Base.get_Property - int""); return null; }
        set { Console.WriteLine(""Base.set_Property - int""); }
    }
}
class Base2 : Base
{
    public Action<string> Property
    {
        get { Console.WriteLine(""Base2.get_Property - string""); return null; }
        set { Console.WriteLine(""Base2.set_Property - string""); }
    }
}
class ImplicitInBase : Base2, I1<int>, I2<Int32> { }
class ImplicitInBase2 : Base2, I3<string> { }
class Explicit : I1<int>, I2<Int32>
{
    void I1<Int32>.Method<V>(int x, Func<int, V> v) { Console.WriteLine(""Explicit.I1<int>.Method""); }
    void I2<Int32>.Method<V>(int x, Func<int, V> v) { Console.WriteLine(""Explicit.I2<int>.Method""); }
    Action<int> I1<int>.Property
    {
        get { Console.WriteLine(""Explicit.I1<int>.get_Property""); return null; }
        set { Console.WriteLine(""Explicit.I1<int>.set_Property""); }
    }
    Action<int> I2<int>.Property
    {
        get { Console.WriteLine(""Explicit.I2<int>.get_Property""); return null; }
        set { Console.WriteLine(""Explicit.I2<int>.set_Property""); }
    }
}
class Explicit2 : I3<string>
{
    void I1<string>.Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Explicit2.I1<string>.Method""); }
    void I2<string>.Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Explicit2.I2<string>.Method""); }
    void I3<string>.Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Explicit2.I3<string>.Method""); }
    Action<string> I1<string>.Property
    {
        get { Console.WriteLine(""Explicit2.I1<string>.get_Property""); return null; }
        set { Console.WriteLine(""Explicit2.I1<string>.set_Property""); }
    }
    Action<string> I2<string>.Property
    {
        get { Console.WriteLine(""Explicit2.I2<string>.get_Property""); return null; }
        set { Console.WriteLine(""Explicit2.I2<string>.set_Property""); }
    }
    Action<string> I3<string>.Property
    {
        get { Console.WriteLine(""Explicit2.I3<string>.get_Property""); return null; }
        set { Console.WriteLine(""Explicit2.I3<string>.set_Property""); }
    }
}
class Test
{
    public static void Main()
    {
        I1<int> i = new Implicit();
        Action<int> x = null;
        i.Method<string>(1, null); i.Property = x; x = i.Property;
        i = new ImplicitInBase();
        i.Method<string>(1, null); i.Property = x; x = i.Property;
        i = new Explicit();
        i.Method<string>(1, null); i.Property = x; x = i.Property;

        I2<int> j = new Implicit();
        j.Method<string>(1, null); j.Property = x; x = j.Property;
        j = new ImplicitInBase();
        j.Method<string>(1, null); j.Property = x; x = j.Property;
        j = new Explicit();
        j.Method<string>(1, null); j.Property = x; x = j.Property;

        I3<string> k = new Implicit2();
        Action<string> y = null;
        k.Method<string>("""", null); k.Property = y; y = k.Property;
        k = new ImplicitInBase2();
        k.Method<string>("""", null); k.Property = y; y = k.Property;
        k = new Explicit2();
        k.Method<string>("""", null); k.Property = y; y = k.Property;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Implicit.Method
Implicit.set_Property
Implicit.get_Property
Base.Method - int
Base.set_Property - int
Base.get_Property - int
Explicit.I1<int>.Method
Explicit.I1<int>.set_Property
Explicit.I1<int>.get_Property
Implicit.Method
Implicit.set_Property
Implicit.get_Property
Base.Method - int
Base.set_Property - int
Base.get_Property - int
Explicit.I2<int>.Method
Explicit.I2<int>.set_Property
Explicit.I2<int>.get_Property
Implicit2.Method
Implicit2.set_Property
Implicit2.get_Property
Base.Method - string
Base2.set_Property - string
Base2.get_Property - string
Explicit2.I3<string>.Method
Explicit2.I3<string>.set_Property
Explicit2.I3<string>.get_Property");

            comp.VerifyDiagnostics(
                // (15,10): warning CS0108: 'I3<W>.Method<Z>(W, System.Func<W, Z>)' hides inherited member 'I1<W>.Method<V>(W, System.Func<W, V>)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("I3<W>.Method<Z>(W, System.Func<W, Z>)", "I1<W>.Method<V>(W, System.Func<W, V>)"),
                // (16,15): warning CS0108: 'I3<W>.Property' hides inherited member 'I1<W>.Property'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("I3<W>.Property", "I1<W>.Property"),
                // (48,27): warning CS0108: 'Base2.Property' hides inherited member 'Base.Property'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("Base2.Property", "Base.Property"));
        }

        [WorkItem(540581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540581")]
        [Fact]
        public void RegressionTestRefEmitBugRelatedToHidingInInterfaces()
        {
            var source = @"
using System;
interface I1<T>
{
    void Method<V>(T x, Func<T, V> v);
    Action<T> Property { get; set; }
}
interface I2<U>
{
    void Method<Z>(U x, Func<U, Z> v);
    Action<U> Property { get; set; }
}
interface I3<W> : I1<W>, I2<W>
{
    void Method<Z>(W x, Func<W, Z> v);
    Action<W> Property { get; set; }
}
class Implicit2 : I3<string>
{
    public void Method<V>(string x, Func<string, V> v) { Console.WriteLine(""Implicit2.Method""); }
    public Action<string> Property
    {
        get { Console.WriteLine(""Implicit2.get_Property""); return null; }
        set { Console.WriteLine(""Implicit2.set_Property""); }
    }
}
class Test
{
    public static void Main()
    {
        I3<string> i = new Implicit2();
    }
}";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void TestImplementUnambiguousSignaturesFromSameInterface()
        {
            var source = @"
using System;
interface Interface<S, T>
{
    void Method();
    void Method<T>(T x);
}
class Base
{
    public void Method() { Console.WriteLine(""Base - Method""); }
}
class Implicit : Base, Interface<int, int>, Interface<string, string>
{
    public void Method<T>(T x) { Console.WriteLine(""Implicit - Method<T>""); }
}
class Explicit : Interface<int, int>, Interface<string, string>
{
    void Interface<int, int>.Method() { Console.WriteLine(""Explicit - Method - int""); }
    void Interface<string, string>.Method() { Console.WriteLine(""Explicit - Method - string""); }
    void Interface<int, int>.Method<T>(T x) { Console.WriteLine(""Explicit - Method<T> - int""); }
    void Interface<string, string>.Method<V>(V x) { Console.WriteLine(""Explicit - Method<V> - string""); }
}
class Test
{
    public static void Main()
    {
        Interface<int, int> i = new Explicit();
        i.Method();
        i.Method(1);
        i.Method<string>("""");

        i = new Implicit();
        i.Method();
        i.Method(1);
        i.Method<string>("""");
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Explicit - Method - int
Explicit - Method<T> - int
Explicit - Method<T> - int
Base - Method
Implicit - Method<T>
Implicit - Method<T>");

            comp.VerifyDiagnostics(
                // (6,17): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Interface<S, T>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Interface<S, T>")); // No errors
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterfaceImplicitlyAndExplicitly()
        {
            var source = @"
using System;
interface I1<T, U>
{
    Action<T> Method(ref T x);
    Action<U> Method(U x); // Omit ref
    void Method(T x, U[] y);
    void Method(U x, params T[] y); // Add params
    long Method(T x, Func<T, U> v, U[] y);
    int Method(U x, Func<T, U> v, params U[] y); // Add params and change return type
}
class Implicit : I1<int, int>
{
    public Action<int> Method(ref int x) { Console.WriteLine(""Method(ref int x)""); return null; }
    public Action<int> Method(int x) { Console.WriteLine(""Method(int x)""); return null; }
    public void Method(int x, int[] y) { Console.WriteLine(""Method(int x, int[] y)""); }
    // Implements both params and non-params version
    public long Method(int x, Func<int, int> v, int[] y) { Console.WriteLine(""Method(int x, Func<int, int> v, int[] y)""); return 0; }
    // We have to implement this explicitly
    int I1<int, int>.Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine(""I1<int, int>.Method(int x, Func<int, int> v, params int[] y)""); return 0; }
}
class Explicit : I1<int, int>
{
    Action<int> I1<int, int>.Method(ref int x) { Console.WriteLine(""I1<int, int>.Method(ref int x)""); return null; }
    Action<int> I1<int, int>.Method(int x) { Console.WriteLine(""I1<int, int>.Method(int x)""); return null; }
    void I1<int, int>.Method(int x, int[] y) { Console.WriteLine(""I1<int, int>.Method(int x, int[] y)""); }
    // This has to be implicit so as not to clash with the above
    public void Method(int x, params int[] y) { Console.WriteLine(""Method(int x, params int[] y)""); }
    long I1<int, int>.Method(int x, Func<int, int> v, int[] y) { Console.WriteLine(""long I1<int, int>.Method(int x, Func<int, int> v, int[] y)""); return 0; }
    int I1<int, int>.Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine(""int I1<int, int>.Method(int x, Func<int, int> v, params int[] y)""); return 0; }
}
class Test
{
    public static void Main()
    {
        int x = 1; Func<int, int> y = null;
        I1<int, int> i = new Implicit();
        i.Method(ref x); i.Method(x);
        i.Method(x, x, x, x);
        i.Method(x, y, x, x, x);

        i = new Explicit();
        i.Method(ref x); i.Method(x);
        i.Method(x, x, x, x);
        i.Method(x, y, x, x, x);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Method(ref int x)
Method(int x)
Method(int x, int[] y)
I1<int, int>.Method(int x, Func<int, int> v, params int[] y)
I1<int, int>.Method(ref int x)
I1<int, int>.Method(int x)
Method(int x, params int[] y)
int I1<int, int>.Method(int x, Func<int, int> v, params int[] y)");

            comp.VerifyDiagnostics(
                // (26,23): warning CS0473: Explicit interface implementation 'Explicit.I1<int, int>.Method(int, int[])' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Explicit.I1<int, int>.Method(int, int[])"));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterfaceImplicitlyInBaseClass()
        {
            var source = @"
using System;
interface I1<T, U>
{
    Action<T> Method(ref T x);
    Action<U> Method(U x); // Omit ref
    void Method(ref Func<U, T> v);
    void Method(out Func<T, U> v); // Toggle ref to out
    void Method(T x, U[] y);
    void Method(U x, params T[] y); // Add params
    long Method(T x, Func<T, U> v, U[] y);
    int Method(U x, Func<T, U> v, params U[] y); // Add params and change return type
}
class Base
{
    public Action<int> Method(ref int x) { Console.WriteLine(""Method(ref int x)""); return null; }
    public Action<int> Method(int x) { Console.WriteLine(""Method(int x)""); return null; }
    public void Method(ref Func<int, int> v) { Console.WriteLine(""Method(ref Func<int, int> v)""); }
    public void Method(int x, int[] y) { Console.WriteLine(""Method(int x, int[] y)""); }
    public long Method(int x, Func<int, int> v, int[] y) { Console.WriteLine(""long Method(int x, Func<int, int> v, int[] y)""); return 0; }
}
class ImplicitInBase : Base, I1<int, int>
{
    public void Method(out Func<int, int> v) { v = null; Console.WriteLine(""Method(out Func<int, int> v)""); }
    public int Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine(""int Method(int x, Func<int, int> v, params int[] y)""); return 0; }
}
class Test
{
    public static void Main()
    {
        int x = 1; Func<int, int> y = null;
        I1<int, int> i = new ImplicitInBase();
        i.Method(ref x); i.Method(x); i.Method(ref y); i.Method(out y);
        i.Method(x, x, x, x);
        i.Method(x, y, x, x, x);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Method(ref int x)
Method(int x)
Method(out Func<int, int> v)
Method(out Func<int, int> v)
Method(int x, int[] y)
int Method(int x, Func<int, int> v, params int[] y)");

            comp.VerifyDiagnostics(
                // (25,16): warning CS0108: 'ImplicitInBase.Method(int, System.Func<int, int>, params int[])' hides inherited member 'Base.Method(int, System.Func<int, int>, int[])'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("ImplicitInBase.Method(int, System.Func<int, int>, params int[])", "Base.Method(int, System.Func<int, int>, int[])"));
        }

        [WorkItem(540582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540582")]
        [Fact]
        public void TestImplementNestedInterface()
        {
            var source = @"
using System;
public class Base
{
    public int Method(long g) { Console.WriteLine(""Base.Method""); return 0;} // implicit in base
}
class C : Base, C.IX 
{
    public interface IX : C.IY 
    {
        new void Method(int i);
    }
    public interface IY 
    {
        void Method(int i);
        int Method(long j);
        int Property { set; }
    }
    public void Method(int i) { Console.WriteLine(""C.Method""); } // implicit
    int IY.Property { set { Console.WriteLine(""C.IY.set_Property""); } } // explicit
    public int Property { get { return 0; } set { Console.WriteLine(""C.set_Property""); } }
}
class U : U.I
{
    public interface I 
    {
        void Method(int i);
        int Property { set; }
    };
    void I.Method(int i) { Console.WriteLine(""U.I.Method""); } // explicit
    public int Property { set { Console.WriteLine(""U.set_Property""); } get { return 0; } } // implicit
}
class Test
{
    public static void Main()
    {
        C.IX i = new C();
        i.Method(1);
        i.Method(1L);
        i.Property = 0;

        U.I j = new U();
        j.Method(1);
        j.Property = 0;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
C.Method
Base.Method
C.IY.set_Property
U.I.Method
U.set_Property").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestInterfaceMembersSignature()
        {
            var source = @"interface IFace
{
    void Method();
    int Prop { get; set; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("IFace", "Method", ".method public hidebysig newslot abstract virtual instance System.Void Method() cil managed"),
                Signature("IFace", "get_Prop", ".method public hidebysig newslot specialname abstract virtual instance System.Int32 get_Prop() cil managed"),
                Signature("IFace", "set_Prop", ".method public hidebysig newslot specialname abstract virtual instance System.Void set_Prop(System.Int32 value) cil managed"),
                Signature("IFace", "Prop", ".property readwrite instance System.Int32 Prop")
            });
        }

        [WorkItem(545625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545625")]
        [Fact]
        public void ReverseArrayRankSpecifiersInExplicitImplementationName()
        {
            var source = @"
using System;
 
interface I<T>
{
    void Foo();
}
 
class C : I<int[][,]>
{
    static void Main()
    {
        I<int[][,]> x = new C();
        Action a = x.Foo;
        Console.WriteLine(a.Method);
    }
 
    void I<int[][,]>.Foo() { }
}
";
            // NOTE: order reversed from C# notation.
            CompileAndVerify(source, expectedOutput: @"Void I<System.Int32[,][]>.Foo()");
        }

        [Fact]
        [WorkItem(530164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530164"), WorkItem(531642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531642"), WorkItem(531643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531643")]
        public void SynthesizedExplicitImplementationOfByRefReturn()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance int32&  M() cil managed
  {
  }

} // end of class I

.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig instance int32& 
          M() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B
";

            var source = @"
public class D : B, I
{
}
";

            var comp = CreateCompilationWithCustomILSource(source, il, options: TestOptions.DebugDll);

            var verifier = CompileAndVerify(comp, expectedSignatures: new[]
            {
                // NOTE: dev11 has the return type as void, which doesn't peverify.
                Signature("D", "I.M", ".method private hidebysig newslot virtual final instance System.Int32& I.M() cil managed")
            });

            // NOTE: local optimized away even with optimizations turned off (since returning a ref local doesn't peverify).
            verifier.VerifyIL("D.I.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "" B.M()""
  IL_0006:  ret
}
");
        }

        [Fact]
        [WorkItem(530164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530164")]
        public void SynthesizedExplicitImplementationOfGenericByRefReturn()
        {
            var il = @"
.class interface public abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance !T&  M1() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance class I`1<!T[]>&  M2() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance !!U&  M3<U>() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance class I`1<!!U[]>&  M4<U>() cil managed
  {
  }

} // end of class I`1

.class public auto ansi beforefieldinit B`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance !T&  M1() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig instance class I`1<!T[]>& 
          M2() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig instance !!U&  M3<U>() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig instance class I`1<!!U[]>& 
          M4<U>() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B`1
";

            var source = @"
public class D : B<char>, I<char>
{
}
";

            var comp = CreateCompilationWithCustomILSource(source, il, options: TestOptions.DebugDll);

            var global = comp.GlobalNamespace;
            var derivedType = global.GetMember<NamedTypeSymbol>("D");
            var interfaceType = derivedType.Interfaces.Single();
            Assert.Equal(global.GetMember<NamedTypeSymbol>("I"), interfaceType.OriginalDefinition);
            var baseType = derivedType.BaseType;
            Assert.Equal(global.GetMember<NamedTypeSymbol>("B"), baseType.OriginalDefinition);

            var baseMethods = Enumerable.Range(1, 4).Select(i => baseType.GetMember<MethodSymbol>("M" + i)).ToArray();
            var interfaceMethods = Enumerable.Range(1, 4).Select(i => interfaceType.GetMember<MethodSymbol>("M" + i)).ToArray();

            AssertEx.Equal(baseMethods, interfaceMethods.Select(interfaceMethod => derivedType.FindImplementationForInterfaceMember(interfaceMethod)));

            var verifier = CompileAndVerify(comp, expectedSignatures: new[]
            {
                // NOTE: dev11 has the return type as void, which doesn't peverify.
                Signature("D", "I<System.Char>.M1", ".method private hidebysig newslot virtual final instance System.Char& I<System.Char>.M1() cil managed"),
                Signature("D", "I<System.Char>.M2", ".method private hidebysig newslot virtual final instance I`1[System.Char[]]& I<System.Char>.M2() cil managed"),
                Signature("D", "I<System.Char>.M3", ".method private hidebysig newslot virtual final instance U& I<System.Char>.M3<U>() cil managed"),
                Signature("D", "I<System.Char>.M4", ".method private hidebysig newslot virtual final instance I`1[U[]]& I<System.Char>.M4<U>() cil managed"),
            });

            foreach (var suffix in new[] { "1", "2", "3<U>", "4<U>" })
            {
                // NOTE: local optimized away even with optimizations turned off (since returning a ref local doesn't peverify).
                verifier.VerifyIL("D.I<char>.M" + suffix, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "" B<char>.M" + suffix + @"()""
  IL_0006:  ret
}
");
            }
        }
    }
}
