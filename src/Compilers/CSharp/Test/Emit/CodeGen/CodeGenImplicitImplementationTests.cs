// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenImplicitImplementationTests : CSharpTestBase
    {
        [Fact]
        public void TestImplicitImplementation()
        {
            var source = @"
interface Interface
{
    void Method(int i);
    int Property { set; }
}

class Class : Interface
{
    public void Method(int i)
    {
        System.Console.WriteLine(""Class.Method({0})"", i);
    }

    public int Property
    {
        set
        {
            System.Console.WriteLine(""Class.Property.set({0})"", value);
        }
    }
}

class E
{
    public static void Main()
    {
        Class c = new Class();
        Interface ic = c;

        c.Method(1);
        ic.Method(2);

        c.Property = 3;
        ic.Property = 4;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Class.Method(1)
Class.Method(2)
Class.Property.set(3)
Class.Property.set(4)
");
        }

        [Fact]
        public void TestImplicitImplementationGenericType()
        {
            var source = @"
interface Interface<T>
{
    void Method(T i);
    T Property { set; }
}

class Class<T> : Interface<T>
{
    public void Method(T i)
    {
        System.Console.WriteLine(""Class.Method({0})"", i);
    }

    public T Property
    {
        set
        {
            System.Console.WriteLine(""Class.Property.set({0})"", value);
        }
    }
}

class E
{
    public static void Main()
    {
        Class<int> c = new Class<int>();
        Interface<int> ic = c;

        c.Method(1);
        ic.Method(2);

        c.Property = 3;
        ic.Property = 4;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Class.Method(1)
Class.Method(2)
Class.Property.set(3)
Class.Property.set(4)
");
        }

        [Fact]
        public void TestImplicitImplementationGenericMethod()
        {
            var source = @"
interface Interface
{
    void Method<U>(U u);
}

class Class : Interface
{
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Class.Method({0})"", v);
    }
}

class E
{
    public static void Main()
    {
        Class c = new Class();
        Interface ic = c;

        c.Method<ulong>(1);
        ic.Method<ushort>(2);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Class.Method(1)
Class.Method(2)
");
        }

        [Fact]
        public void TestImplicitImplementationGenericMethodAndType()
        {
            var source = @"
interface Interface<T>
{
    void Method<U>(T i, U u);
    void Method<U>(U u);
}

class Class<T> : Interface<T>
{
    public void Method<V>(T i, V v)
    {
        System.Console.WriteLine(""Class.Method({0}, {1})"", i, v);
    }
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Class.Method({0})"", v);
    }
}

class E
{
    public static void Main()
    {
        Class<int> c = new Class<int>();
        Interface<int> ic = c;

        c.Method<long>(1, 2);
        c.Method<ulong>(3);
        ic.Method<short>(4, 5);
        ic.Method<ushort>(6);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Class.Method(1, 2)
Class.Method(3)
Class.Method(4, 5)
Class.Method(6)
");
        }

        [Fact]
        public void TestImplicitImplementationInBase()
        {
            var source = @"
interface Interface
{
    void Method(int i);
    int Property { set; }
}

class Base
{
    public void Method(int i)
    {
        System.Console.WriteLine(""Base.Method({0})"", i);
    }

    public int Property
    {
        set
        {
            System.Console.WriteLine(""Base.Property.set({0})"", value);
        }
    }
}

class Derived : Base, Interface
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base bd = d;
        Interface id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);

        d.Property = 4;
        bd.Property = 5;
        id.Property = 6;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
Base.Property.set(4)
Base.Property.set(5)
Base.Property.set(6)
");
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericType()
        {
            var source = @"
interface Interface<T>
{
    void Method(T i);
    T Property { set; }
}

class Base<S>
{
    public void Method(S i)
    {
        System.Console.WriteLine(""Base.Method({0})"", i);
    }

    public S Property
    {
        set
        {
            System.Console.WriteLine(""Base.Property.set({0})"", value);
        }
    }
}

class Derived : Base<uint>, Interface<uint>
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base<uint> bd = d;
        Interface<uint> id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);

        d.Property = 4;
        bd.Property = 5;
        id.Property = 6;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
Base.Property.set(4)
Base.Property.set(5)
Base.Property.set(6)
");
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericMethod()
        {
            var source = @"
interface Interface
{
    void Method<U>(U u);
}

class Base : Interface
{
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Base.Method({0})"", v);
    }
}

class Derived : Base, Interface
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base bd = d;
        Interface id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
");
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericMethodAndType()
        {
            var source = @"
interface Interface<T>
{
    void Method<U>(T i, U u);
    void Method<U>(U u);
}

class Base<T> : Interface<T>
{
    public void Method<V>(T i, V v)
    {
        System.Console.WriteLine(""Base.Method({0}, {1})"", i, v);
    }
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Base.Method({0})"", v);
    }
}

class Derived : Base<int>, Interface<int>
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base<int> bd = d;
        Interface<int> id = d;

        d.Method<long>(1, 2);
        d.Method<ulong>(3);
        bd.Method<short>(4, 5);
        bd.Method<ushort>(6);
        id.Method<short>(7, 8);
        id.Method<ushort>(9);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Base.Method(1, 2)
Base.Method(3)
Base.Method(4, 5)
Base.Method(6)
Base.Method(7, 8)
Base.Method(9)
");
        }

        [Fact]
        public void TestImplicitImplementationInBaseOutsideAssembly()
        {
            var libSource = @"
public interface Interface
{
    void Method(int i);
    int Property { set; }
}

public class Base
{
    public void Method(int i)
    {
        System.Console.WriteLine(""Base.Method({0})"", i);
    }

    public int Property
    {
        set
        {
            System.Console.WriteLine(""Base.Property.set({0})"", value);
        }
    }
}
";
            var exeSource = @"
public class Derived : Base, Interface
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base bd = d;
        Interface id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);

        d.Property = 4;
        bd.Property = 5;
        id.Property = 6;
    }
}
";

            string expectedOutput = @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
Base.Property.set(4)
Base.Property.set(5)
Base.Property.set(6)
".TrimStart();

            CompileAndVerify(
                CreateCompilationWithMscorlibAndReference(libSource, exeSource),
                expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestImplicitImplementationInBaseOutsideAssemblyGenericType()
        {
            var libSource = @"
public interface Interface<T>
{
    void Method(T i);
    T Property { set; }
}

public class Base<S>
{
    public void Method(S i)
    {
        System.Console.WriteLine(""Base.Method({0})"", i);
    }

    public S Property
    {
        set
        {
            System.Console.WriteLine(""Base.Property.set({0})"", value);
        }
    }
}";

            var exeSource = @"
class Derived : Base<uint>, Interface<uint>
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base<uint> bd = d;
        Interface<uint> id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);

        d.Property = 4;
        bd.Property = 5;
        id.Property = 6;
    }
}
";
            string expectedOutput = @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
Base.Property.set(4)
Base.Property.set(5)
Base.Property.set(6)
".TrimStart();

            CompileAndVerify(
                CreateCompilationWithMscorlibAndReference(libSource, exeSource),
                expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestImplicitImplementationInBaseOutsideAssemblyGenericMethod()
        {
            var libSource = @"
public interface Interface
{
    void Method<U>(U u);
}

public class Base : Interface
{
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Base.Method({0})"", v);
    }
}";

            var exeSource = @"
class Derived : Base, Interface
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base bd = d;
        Interface id = d;

        d.Method(1);
        bd.Method(2);
        id.Method(3);
    }
}
";
            string expectedOutput = @"
Base.Method(1)
Base.Method(2)
Base.Method(3)
".TrimStart();

            CompileAndVerify(
                 CreateCompilationWithMscorlibAndReference(libSource, exeSource),
                 expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestImplicitImplementationInBaseOutsideAssemblyGenericMethodAndType()
        {
            var libSource = @"
public interface Interface<T>
{
    void Method<U>(T i, U u);
    void Method<U>(U u);
}

public class Base<T> : Interface<T>
{
    public void Method<V>(T i, V v)
    {
        System.Console.WriteLine(""Base.Method({0}, {1})"", i, v);
    }
    public void Method<V>(V v)
    {
        System.Console.WriteLine(""Base.Method({0})"", v);
    }
}";

            var exeSource = @"
class Derived : Base<int>, Interface<int>
{
}

class E
{
    public static void Main()
    {
        Derived d = new Derived();
        Base<int> bd = d;
        Interface<int> id = d;

        d.Method<long>(1, 2);
        d.Method<ulong>(3);
        bd.Method<short>(4, 5);
        bd.Method<ushort>(6);
        id.Method<short>(7, 8);
        id.Method<ushort>(9);
    }
}
";
            string expectedOutput = @"
Base.Method(1, 2)
Base.Method(3)
Base.Method(4, 5)
Base.Method(6)
Base.Method(7, 8)
Base.Method(9)
".TrimStart();

            CompileAndVerify(
                CreateCompilationWithMscorlibAndReference(libSource, exeSource),
                expectedOutput: expectedOutput);
        }

        /// <summary>
        /// In IL, list all declared interfaces *and their base interfaces*.  If they don't, the
        /// runtime doesn't find implicit implementation of base interface methods.
        /// </summary>
        [Fact]
        public void TestBaseInterfaceMetadata()
        {
            var source = @"
interface I1
{
    void foo();
}

interface I2 : I1
{
    void bar();
}

class X : I1
{
    void I1.foo()
    {
        System.Console.WriteLine(""X::I1.foo"");
    }
}

class Y : X, I2
{
    public virtual void foo()
    {
        System.Console.WriteLine(""Y.foo"");
    }

    void I2.bar()
    {
    }
}

class Program
{
    static void Main()
    {
        I2 b = new Y();
        b.foo();
    }
}
";
            CompileAndVerify(source, expectedOutput: "Y.foo");
        }

        /// <summary>
        /// Override different accessors of a virtual property in different subtypes.
        /// </summary>
        [Fact]
        public void TestPartialPropertyOverriding()
        {
            var source = @"
interface I
{
    int P { get; set; }
}

class Base
{
    public virtual int P
    {
        get { System.Console.WriteLine(""Base.P.get""); return 1; }
        set { System.Console.WriteLine(""Base.P.set""); }
    }
}

class Derived1 : Base
{
    public override int P
    {
        get { System.Console.WriteLine(""Derived1.P.get""); return 1; }
    }
}

class Derived2 : Derived1
{
    public override int P
    {
        set { System.Console.WriteLine(""Derived2.P.set""); }
    }
}

class Derived3 : Derived2, I
{
}

class Program
{

    static void Main()
    {
        I id3 = new Derived3();
        id3.P += 1;
    }
}
";

            string expectedOutput = @"
Derived1.P.get
Derived2.P.set";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(540410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540410")]
        [Fact]
        public void ImplementMultipleInterfaceWithCommonBase()
        {
            var source = @"
interface IBase
{
    void PBase();
}
interface IBase1 : IBase
{
    void PBase1();
}
interface IBase2 : IBase
{
    void PBase2();
}
class C1 : IBase1, IBase2
{
    void IBase1.PBase1() { }
    void IBase.PBase() {}
    public void PBase2() { }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var typeSymbol = module.GlobalNamespace.GetTypeMembers("C1").Single();
                Assert.True(typeSymbol.Interfaces.All(iface => iface.Name == "IBase" || iface.Name == "IBase1" || iface.Name == "IBase2"));
            };

            CompileAndVerify(source, sourceSymbolValidator: validator, symbolValidator: validator, expectedSignatures: new[]
            {
                Signature("C1", "IBase1.PBase1", ".method private hidebysig newslot virtual final instance System.Void IBase1.PBase1() cil managed"),
                Signature("C1", "IBase.PBase", ".method private hidebysig newslot virtual final instance System.Void IBase.PBase() cil managed"),
                Signature("C1", "PBase2", ".method public hidebysig newslot virtual final instance System.Void PBase2() cil managed")
            });
        }

        [Fact]
        public void ImplementInterfaceWithMultipleBasesWithSameMethod()
        {
            var source = @"
interface IBase1
{
    void BaseFoo();
}
interface IBase2
{
    void BaseFoo();
}
interface IInterface : IBase1, IBase2
{
    void InterfaceFoo();
}
class C1 : IInterface
{
    public void BaseFoo() { System.Console.Write(""BaseFoo "");}
    public void InterfaceFoo() { System.Console.Write(""InterfaceFoo ""); }
    public void Test()
    {
        C1 c = new C1();
        c.BaseFoo();
        c.InterfaceFoo();
        ((IBase1)c).BaseFoo();
        ((IBase2)c).BaseFoo();
        ((IInterface)c).InterfaceFoo();
        ((IInterface)c).BaseFoo();
    }
}
";
            CreateCompilationWithMscorlib(source)
                .VerifyDiagnostics(
                    // (26,9): error CS0121: The call is ambiguous between the following methods or properties: 'IBase1.BaseFoo()' and 'IBase2.BaseFoo()'
                    //         ((IInterface)c).BaseFoo();
                    Diagnostic(ErrorCode.ERR_AmbigCall, "BaseFoo").WithArguments("IBase1.BaseFoo()", "IBase2.BaseFoo()"));
        }

        [WorkItem(540410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540410")]
        [Fact]
        public void InterfaceDiamondInheritenceWithNewMember()
        {
            var source = @"
interface IBase
{
    void Foo();
}
interface ILeft : IBase
{
    new void Foo();
}
interface IRight : IBase
{
    void Bar();
}
interface IDerived : ILeft, IRight { }
class C1 : IDerived
{
    public void Bar() { }

    void IBase.Foo() { System.Console.Write(""IBase "");}

    void ILeft.Foo() { System.Console.Write(""ILeft ""); }
}
public static class MainClass
{
    static void Test(IDerived d)
    {
        d.Foo();           // Invokes ileft.foo()
        ((IBase)d).Foo();  // Invokes ibase.foo()
        ((ILeft)d).Foo();  // Invokes ileft.foo()
        ((IRight)d).Foo(); // Invokes ibase.foo()
    }
    public static void Main()
    {
        C1 c = new C1();
        Test(c);
    }
}
";
            CompileAndVerify(source, expectedOutput: "ILeft IBase ILeft IBase")
                .VerifyIL("MainClass.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  callvirt   ""void ILeft.Foo()""
  IL_0006:  ldarg.0   
  IL_0007:  callvirt   ""void IBase.Foo()""
  IL_000c:  ldarg.0   
  IL_000d:  callvirt   ""void ILeft.Foo()""
  IL_0012:  ldarg.0   
  IL_0013:  callvirt   ""void IBase.Foo()""
  IL_0018:  ret       
}");
        }

        [Fact]
        public void TestImplicitImplSignatureMismatches_ParamsAndOptionals()
        {
            // Tests:
            // Replace params with non-params in signature of implemented member (and vice-versa)
            // Replace optional parameter with non-optional parameter

            var source = @"
using System;
using System.Collections.Generic;
interface I1<T>
{
    void Method(int a, long b = 2, string c = null, params List<T>[] d);
}
interface I2 : I1<string>
{
    void Method<T>(out int a, ref T[] b, List<T>[] c);
}
class Base
{
    // Change default value of optional parameter
    public void Method(int a, long b = 3, string c = """", params List<string>[] d)
    {
        Console.WriteLine(""Base.Method({0}, {1}, {2})"", a, b, c);
    }
}
class Derived : Base, I1<string> // Implicit implementation in base
{
}
class Class : I1<string> // Implicit implementation
{
    // Replace optional with non-optional - OK
    public void Method(int a, long b, string c = """", params List<string>[] d)
    { 
        Console.WriteLine(""Class.Method({0}, {1}, {2})"", a, b, c); 
    }
}
class Class2 : I2 // Implicit implementation
{
    // Replace non-optional with optional - OK
    // Omit params and replace with optional - OK
    public void Method(int a = 4, long b = 3, string c = """", List<string>[] d = null)
    { 
        Console.WriteLine(""Class2.Method({0}, {1}, {2})"", a, b, c);
    }

    // Additional params - OK
    public void Method<U>(out int a, ref U[] b, params List<U>[] c)
    {
        a = 0; Console.WriteLine(""Class2.Method<U>({0}, {1})"", a, b[0]); 
    }
}
class Test
{
    public static void Main()
    {
        string[] s1 = new string[]{""a""};
        List<string>[] l1 = new List<string>[]{};

        I1<string> i1 = new Derived();
        i1.Method(1, 2, ""b"", l1);
        
        i1 = new Class();
        i1.Method(2, 3, ""c"", l1);

        I2 i2 = new Class2();
        i2.Method(1, 2, ""b"", l1);
        int i = 1;
        i2.Method<string>(out i, ref s1, l1);
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Method(1, 2, b)
Class.Method(2, 3, c)
Class2.Method(1, 2, b)
Class2.Method<U>(0, a)",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot virtual final instance System.Void Method(System.Int32 a, [opt] System.Int64 b = 3, [opt] System.String c = \"\", [System.ParamArrayAttribute()] System.Collections.Generic.List`1[System.String][] d) cil managed"),
                    Signature("Class", "Method", ".method public hidebysig newslot virtual final instance System.Void Method(System.Int32 a, System.Int64 b, [opt] System.String c = \"\", [System.ParamArrayAttribute()] System.Collections.Generic.List`1[System.String][] d) cil managed"),
                    Signature("Class2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method([opt] System.Int32 a = 4, [opt] System.Int64 b = 3, [opt] System.String c = \"\", [opt] System.Collections.Generic.List`1[System.String][] d) cil managed"),
                    Signature("Class2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<U>([out] System.Int32& a, U[]& b, [System.ParamArrayAttribute()] System.Collections.Generic.List`1[U][] c) cil managed")
                });
        }


        [Fact]
        public void ReImplementInterfaceInGrandChild()
        {
            var source = @"
interface I1
{
    void bar();
}
class C1 : I1
{
    void I1.bar() { System.Console.Write(""C1"");}
}
class C2 : C1 { }
class C3 : C2, I1
{
    public void bar() { System.Console.Write(""C3""); }
}
static class Program
{
    static void Main()
    {
        C3 c3 = new C3();
        C2 c2 = c3;
        C1 c1 = c3;
        c3.bar();
        ((I1)c3).bar();
        ((I1)c2).bar();
        ((I1)c1).bar();
    }
}
";
            CompileAndVerify(source, expectedOutput: "C3C3C3C3");
        }

        [Fact]
        public void OverrideBaseClassImplementation()
        {
            var source = @"
interface I1
{
    void Bar();
}
abstract class C1 : I1
{
    abstract public void Bar();
}
class C2 : C1
{
    public override void Bar() { System.Console.Write(""C2""); }
}
class C3 : C2
{
    public override void Bar() { System.Console.Write(""C3""); }
}
static class Program
{
    static void Main()
    {
        I1 i = new C3();
        i.Bar();
    }
}
";
            CompileAndVerify(source, expectedOutput: "C3");
        }

        [Fact]
        public void TestImplementingWithVirtualMembers()
        {
            // Tests:
            // Implement interface member with virtual / abstract / override member
            // Test that appropriate (derived) member is called when invoking through interface

            var source = @"
using System;
using System.Collections.Generic;
interface I1
{
    int Property { get; set; }
    void Method<T>(int a, T[] b, List<T> c);
    void Method(int a, System.Exception[] b);
    void Method();
}
class Base1
{
    public virtual void Method()
    {
        Console.WriteLine(""Base1.Method()"");
    }
}
abstract class Class1 : Base1, I1 // Implementing with abstract / virtual / override members from current type
{
    public abstract void Method(int b, System.Exception[] c);
    public abstract int Property { get; set; }
    public virtual void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Class1.Method<U>({0}, {1})"", i, j[0]);
    }
    public override void Method()
    {
        Console.WriteLine(""Class1.Method()"");
    }
}
class Class2 : Class1
{
    public override void Method(int i, System.Exception[] c)
    {
        System.Console.WriteLine(""Class2.Method({0})"", i);
    }
    public override int Property
    {
        get
        {
            System.Console.WriteLine(""Class2.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Class2.Property.set({0})"", value);
        }
    }
}
abstract class Base2 : Base1
{
    public abstract void Method(int b, System.Exception[] c);
    public abstract int Property { get; set; }
    public virtual void Method<U>(int i, U[] j, List<U> k)
    {
        Console.WriteLine(""Base2.Method<U>({0}, {1})"", i, j[0]);
    }
    public override void Method()
    {
        Console.WriteLine(""Base2.Method()"");
    }
}
abstract class Derived : Base2, I1 // Implementing with abstract / virtual / override members from base type
{
    public override int Property
    {
        set
        {
            System.Console.WriteLine(""Derived.Property.set()"");
        }
    }
}
class Derived2 : Derived, I1
{
    public override void Method(int i, System.Exception[] c)
    {
        System.Console.WriteLine(""Derived2.Method({0})"", i);
    }
    public override void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Derived2.Method<U>({0}, {1})"", i, j[0]);
    }
    public override int Property
    {
        get
        {
            System.Console.WriteLine(""Derived2.Property.get()"");
            return 1;
        }
    }
}
class Derived3 : Derived2, I1
{
    public override int Property
    {
        set
        {
            System.Console.WriteLine(""Derived3.Property.set({0})"", value);
        }
    }
    int I1.Property
    {
        get
        {
            System.Console.WriteLine(""Derived3.I1.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Derived3.I1.Property.set({0})"", value);
        }
    }
}
class Derived4 : Derived3
{
    public override int Property
    {
        get
        {
            System.Console.WriteLine(""Derived4.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Derived4.Property.set({0})"", value);
        }
    }
    public override void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Derived4.Method<U>({0}, {1})"", i, j[0]);
    }
}
class Test
{
    public static void Main()
    {
        I1 i = new Derived4();
        int x = 0;

        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 0 }, new List<int>());
        x = i.Property;
        i.Property = x;

        i = new Derived3();
        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 1 }, new List<int>());
        x = i.Property;
        i.Property = x;

        i = new Derived2();
        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 2 }, new List<int>());
        x = i.Property;
        i.Property = x;

        i = new Class2();
        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 3 }, new List<int>());
        x = i.Property;
        i.Property = x;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Base2.Method()
Derived2.Method(1)
Derived4.Method<U>(1, 0)
Derived3.I1.Property.get()
Derived3.I1.Property.set(1)
Base2.Method()
Derived2.Method(1)
Derived2.Method<U>(1, 1)
Derived3.I1.Property.get()
Derived3.I1.Property.set(1)
Base2.Method()
Derived2.Method(1)
Derived2.Method<U>(1, 2)
Derived2.Property.get()
Derived.Property.set()
Class1.Method()
Class2.Method(1)
Class1.Method<U>(1, 3)
Class2.Property.get()
Class2.Property.set(1)");
        }

        [Fact]
        public void TestImplementingWithSpecialVirtualMembers()
        {
            // Tests:
            // Implement interface member with abstract override / sealed / new member
            // Test that appropriate (derived) member is called when invoking through interface

            var source = @"
using System;
using System.Collections.Generic;
interface I1
{
    int Property { set; }
    void Method<T>(int a, T[] b, List<T> c);
    void Method(int a, System.Exception[] b);
    void Method();
}
abstract class Base1
{
    public virtual void Method()
    {
        Console.WriteLine(""Base1.Method()"");
    }
    public abstract void Method(int b, System.Exception[] c);
    public virtual void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Base1.Method<U>({0}, {1})"", i, j[0]);
    }
    public virtual int Property
    {
        get
        {
            System.Console.WriteLine(""Base1.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Base1.Property.set({0})"", value);
        }
    }
}
abstract class Class1 : Base1, I1 // Implementing with abstract override / sealed override / new members from current type
{
    public abstract override void Method();
    public sealed override void Method(int i, System.Exception[] c)
    {
        System.Console.WriteLine(""Class1.Method({0})"", i);
    }
    public new int Property
    {
        get
        {
            System.Console.WriteLine(""Class1.Property.get()"");
            return 1;
        }
        set
        {
            System.Console.WriteLine(""Class1.Property.set({0})"", value);
        }
    }
}
class Class2 : Class1
{
    public override void Method()
    {
        Console.WriteLine(""Class2.Method()"");
    }
}
abstract class Base2 : Base1
{
    public abstract override void Method();
    public new virtual void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Base2.Method<U>({0}, {1})"", i, j[0]);
    }
    public sealed override int Property
    {
        get
        {
            System.Console.WriteLine(""Base2.Property.get()"");
            return 1;
        } // Synthesized setter implements interface
    }
}
abstract class Derived : Base2, I1 // Implementing with abstract override / sealed override / new members from base type
{
}
class Derived2 : Derived 
{
    public sealed override void Method(int i, System.Exception[] c)
    {
        System.Console.WriteLine(""Derived2.Method({0})"", i);
    }
    public new void Method<U>(int i, U[] j, List<U> k)
    {
        System.Console.WriteLine(""Derived2.Method<U>({0}, {1})"", i, j[0]);
    }
    public override void Method()
    {
        Console.WriteLine(""Derived2.Method()"");
    }
}
class Test
{
    public static void Main()
    {
        I1 i = new Derived2();
        int x = 0;

        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 0 }, new List<int>());
        i.Property = x;
                
        i = new Class2();
        i.Method();
        i.Method(1, new Exception[] { });
        i.Method<int>(1, new int[] { 3 }, new List<int>());
        i.Property = x;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Derived2.Method()
Derived2.Method(1)
Base2.Method<U>(1, 0)
Base1.Property.set(0)
Class2.Method()
Class1.Method(1)
Base1.Method<U>(1, 3)
Class1.Property.set(0)");
        }

        [Fact]
        public void TestImplementingGenericNestedInterfaces_Implicit()
        {
            // Tests:
            // Sanity check – use open (T) and closed (C<String>) generic types in the signature of implemented methods
            // Implement members of generic interface nested inside other generic classes

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            T Property { set; }
            void Method<K>(T a, U[] b, List<V> c, Dictionary<W, K> d);
        }
            
        internal class Base<X, Y>
        {
            public X Property
            {
                set { Console.WriteLine(""Derived1.set_Property""); }
            }

            public void Method<V>(X A, int[] b, List<long> C, Dictionary<Y, V> d)
            {
                Console.WriteLine(""Derived1.Method"");
            }
        }
    }
}
internal class Derived1<U, T> : Outer<U>.Inner<T>.Base<U, T>, Outer<U>.Inner<int>.Interface<long, T>
{
    public class Derived2 : Outer<string>.Inner<int>.Interface<long, string>
    {
        public string Property
        {
            get { return null; } set { Console.WriteLine(""Derived2.set_Property""); }
        }

        public void Method<Z>(string A, int[] B, List<long> C, Dictionary<string, Z> D)
        {
            Console.WriteLine(""Derived2.Method"");
        }
    }
}
public class Test
{
    public static void Main()
    {
        Outer<string>.Inner<int>.Interface<long, string> i = new Derived1<string, string>();
        i.Property = """";
        i.Method<string>("""", new int[]{}, new List<long>(), new Dictionary<string,string>());
        i = new Derived1<string, string>.Derived2();
        i.Property = """";
        i.Method<string>("""", new int[] { }, new List<long>(), new Dictionary<string, string>());
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived1.set_Property
Derived1.Method
Derived2.set_Property
Derived2.Method",
                expectedSignatures: new[]
                {
                    Signature("Outer`1+Inner`1+Base`2", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(X value) cil managed"),
                    Signature("Outer`1+Inner`1+Base`2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<V>(X A, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[Y,V] d) cil managed"),
                    Signature("Derived1`2+Derived2", "get_Property", ".method public hidebysig specialname instance System.String get_Property() cil managed"),
                    Signature("Derived1`2+Derived2", "set_Property", ".method public hidebysig newslot specialname virtual final instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived1`2+Derived2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<Z>(System.String A, System.Int32[] B, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[System.String,Z] D) cil managed"),
                });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestImplementingGenericNestedInterfaces_Implicit_HideTypeParameter()
        {
            // Tests:
            // Implicitly implement generic methods on generic interfaces – test case where type parameter 
            // on method hides the type parameter on class (both in interface and in implementing type)

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d);
            void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<long>.Inner<int>.Interface<long, Y>
        {
            public void Method<X>(long A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
                Console.WriteLine(""Derived1.Method`1"");
            }
            public void Method<X, Y>(long A, int[] b, List<X> C, Dictionary<Y, Y> d)
            {
                Console.WriteLine(""Derived1.Method`2"");
            }
        }
    }
}
class Test
{
    public static void Main()
    {
        Outer<long>.Inner<int>.Interface<long, string> i = new Outer<long>.Inner<int>.Derived1<string, string>();
        i.Method<string>(1, new int[]{}, new List<long>(), new Dictionary<string, string>());
        i.Method<string, int>(1, new int[]{}, new List<string>(), new Dictionary<int, int>());
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived1.Method`1
Derived1.Method`2",
                expectedSignatures: new[]
                {
                    Signature("Outer`1+Inner`1+Derived1`2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<X>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[Y,X] d) cil managed"),
                    Signature("Outer`1+Inner`1+Derived1`2", "Method", ".method public hidebysig newslot virtual final instance System.Void Method<X, Y>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[X] C, System.Collections.Generic.Dictionary`2[Y,Y] d) cil managed")
                });

            comp.VerifyDiagnostics(
                // (11,25): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (11,28): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (15,32): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,32): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,35): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"));
        }


        [Fact]
        public void TestImplementationInBaseGenericType()
        {
            // Tests:
            // Implicitly implement interface member in base generic type – the method that implements interface member
            // should depend on type parameter of base type to satisfy signature (return type / parameter type) equality
            // Also test variation of above case where implementing member in base generic type does not depend 
            // on any type parameters

            var source = @"
using System;
interface Interface
{
    void Method(int x);
    void Method();
}
class Base<T>
{
    public void Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Base2<T> : Base<T>
{
    public void Method() { Console.WriteLine(""Base.Method()""); }
}
class Derived : Base2<int>, Interface
{
}
class Test
{
    public static void Main()
    {
        Interface i = new Derived();
        i.Method(1);
        i.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Base.Method(T)
Base.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericType2()
        {
            // Tests:
            // Implement I<string> implicitly in base class and I<int> implicitly in derived class –
            // assuming I<string> and I<int> have members with same signature (i.e. members 
            // that don't depend on generic-ness of the interface) test which (base / derived class) 
            // members are invoked when calling through each interface

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    public void Method() { Console.WriteLine(""Base.Method()""); }
    public void Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    new public void Method() { Console.WriteLine(""Derived`2.Method()""); }
    public new void Method(int x) { Console.WriteLine(""Derived`2.Method(int)""); }
    public void Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>
{
    new public void Method() { Console.WriteLine(""Derived.Method()""); }
    public new void Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Base.Method(T)
Base.Method()
Derived.Method(string)
Derived.Method()
Derived`2.Method(U)
Derived`2.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericType3()
        {
            // Tests:
            // Variation of TestImplicitImplementationInBaseGenericType2 with overriding

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    public virtual void Method() { Console.WriteLine(""Base.Method()""); }
    public virtual void Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    public override void Method() { Console.WriteLine(""Derived`2.Method()""); }
    public override void Method(int x) { Console.WriteLine(""Derived`2.Method(int)""); }
    public virtual void Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public virtual void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>
{
    public override void Method() { Console.WriteLine(""Derived.Method()""); }
    public override void Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Derived`2.Method(int)
Derived`2.Method()
Derived.Method(string)
Derived.Method()
Derived`2.Method(U)
Derived.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericType4()
        {
            // Tests:
            // Variation of TestImplicitImplementationInBaseGenericType2 with re-implementation

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    public void Method() { Console.WriteLine(""Base.Method()""); }
    public void Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    new public void Method() { Console.WriteLine(""Derived`2.Method()""); }
    public new void Method(int x) { Console.WriteLine(""Derived`2.Method(int)""); }
    public void Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>, Interface<int>
{
    new public void Method() { Console.WriteLine(""Derived.Method()""); }
    public new void Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Base.Method(T)
Base.Method()
Derived.Method(string)
Derived.Method()
Derived`2.Method(U)
Derived.Method()");

            comp.VerifyDiagnostics(
                // (17,17): warning CS1956: Member 'Derived<int, string>.Method(int)' implements interface member 'Interface<int>.Method(int)' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Method").WithArguments("Derived<int, string>.Method(int)", "Interface<int>.Method(int)", "Derived")); // No errors
        }

        [Fact]
        public void TestImplicitImplementationInBaseGenericType5()
        {
            // Tests:
            // Variation of TestImplicitImplementationInBaseGenericType3 with re-implementation

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    public virtual void Method() { Console.WriteLine(""Base.Method()""); }
    public virtual void Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    public override void Method() { Console.WriteLine(""Derived`2.Method()""); }
    public virtual void Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public virtual void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>, Interface<int>
{
    public override void Method() { Console.WriteLine(""Derived.Method()""); }
    public override void Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
    public override void Method(int x) { Console.WriteLine(""Derived.Method(int)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Base.Method(T)
Derived`2.Method()
Derived.Method(string)
Derived.Method()
Derived.Method(int)
Derived.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void ImplementInterfaceUsingSynthesizedSealedProperty()
        {
            // Tests: 
            // Implicitly implement a property / indexer with a sealed property / indexer in base class – 
            // test case where only one accessor is implemented by this sealed property / indexer in base class

            var text = @"
using System;
interface I1
{
    int Bar { get; }
}
class C1
{
    public virtual int Bar { get { return 23123;} set { }}
}

class C2 : C1, I1
{
    sealed public override int Bar { set { } } // Getter will be synthesized by compiler
}

class Test
{
    public static void Main()
    {
        I1 i = new C2();
        Console.WriteLine(i.Bar);
    }
}
";
            // TODO: Will need to update once CompilerGeneratedAttribute is emitted on synthesized accessor
            var comp = CompileAndVerify(text,
                expectedOutput: "23123",
                expectedSignatures: new[]
                {
                    Signature("C2", "get_Bar", ".method public hidebysig specialname virtual final instance System.Int32 get_Bar() cil managed"),
                    Signature("C2", "set_Bar", ".method public hidebysig specialname virtual final instance System.Void set_Bar(System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesImplicitly()
        {
            var source = @"
using System;
interface I1
{
    int P { set; }
    void M1(long x);
    void M2(long x);
    void M3(long x);
    void M4(ref long x);
    void M5(out long x);
    void M6(ref long x);
    void M7(out long x);
    void M8(params long[] x);
    void M9(long[] x);
}
interface I2 : I1
{
}
interface I3 : I2
{
    new long P { set; }
    new int M1(long x); // Return type
    void M2(ref long x); // Add ref
    void M3(out long x); // Add out
    void M4(long x); // Omit ref
    void M5(long x); // Omit out
    void M6(out long x); // Toggle ref to out
    void M7(ref long x); // Toggle out to ref
    new void M8(long[] x); // Omit params
    new void M9(params long[] x); // Add params
}
class Test : I3
{
    int I1.P { set { Console.WriteLine(""I1.P""); } } // Not possible to implement both I3.P and I1.P implicitly
    public long P { get { return 0; } set { Console.WriteLine(""I3.P""); } }
    public void M1(long x) { Console.WriteLine(""I1.M1""); } // Not possible to implement both I3.M1 and I1.M1 implicitly
    int I3.M1(long x) { Console.WriteLine(""I3.M1""); return 0; }
    public void M2(ref long x) { Console.WriteLine(""I3.M2""); }
    public void M2(long x) { Console.WriteLine(""I1.M2""); }
    public void M3(long x) { Console.WriteLine(""I1.M3""); }
    public void M3(out long x) { x = 0; Console.WriteLine(""I3.M3""); }
    public void M4(long x) { Console.WriteLine(""I3.M4""); }
    public void M4(ref long x) { Console.WriteLine(""I1.M4""); }
    public void M5(out long x) { x = 0; Console.WriteLine(""I3.M5""); }
    public void M5(long x) { Console.WriteLine(""I1.M5""); }
    void I1.M6(ref long x) { x = 0; Console.WriteLine(""I1.M6""); } // Not possible to implement both I3.M6 and I1.M6 implicitly
    public void M6(out long x) { x = 0; Console.WriteLine(""I3.M6""); }
    void I3.M7(ref long x) { Console.WriteLine(""I3.M7""); }
    public void M7(out long x) { x = 0; Console.WriteLine(""I1.M7""); } // Not possible to implement both I3.M7 and I1.M7 implicitly
    public void M8(long[] x) { Console.WriteLine(""I3.M8+I1.M8""); } // Implements both I3.M8 and I1.M8
    public void M9(params long[] x) { Console.WriteLine(""I3.M9+I1.M9""); } // Implements both I3.M9 and I1.M9
    public static void Main()
    {
        I3 i = new Test();
        long x = 1;
        i.M1(x); i.M2(x); i.M2(ref x); i.M3(x); i.M3(out x);
        i.M4(x); i.M4(ref x); i.M5(x); i.M5(out x);
        i.M6(out x); i.M6(ref x); i.M7(ref x); i.M7(out x);
        i.M8(new long[] { x, x, x }); i.M8(x, x, x);
        i.M9(new long[] { x, x, x }); i.M9(x, x, x);
        i.P = 1;

        I1 j = i;
        j.M1(x); j.M2(x); j.M3(x);
        j.M4(ref x); j.M5(out x);
        j.M6(ref x); j.M7(out x);
        j.M8(new long[] { x, x, x }); j.M8(x, x, x);
        j.M9(new long[] { x, x, x });
        j.P = 1;
    }
}";

            CompileAndVerify(source, expectedOutput: @"
I3.M1
I1.M2
I3.M2
I1.M3
I3.M3
I3.M4
I1.M4
I1.M5
I3.M5
I3.M6
I1.M6
I3.M7
I1.M7
I3.M8+I1.M8
I3.M8+I1.M8
I3.M9+I1.M9
I3.M9+I1.M9
I3.P
I1.M1
I1.M2
I1.M3
I1.M4
I3.M5
I1.M6
I1.M7
I3.M8+I1.M8
I3.M8+I1.M8
I3.M9+I1.M9
I1.P").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesImplicitlyInBase()
        {
            var source = @"
using System;
interface I1
{
    int P { set; }
    void M1(long x);
    void M2(long x);
    void M3(long x);
    void M4(ref long x);
    void M5(out long x);
    void M6(ref long x);
    void M7(out long x);
    void M8(params long[] x);
    void M9(long[] x);
}
interface I2 : I1
{
}
interface I3 : I2
{
    new long P { set; }
    new int M1(long x); // Return type
    void M2(ref long x); // Add ref
    void M3(out long x); // Add out
    void M4(long x); // Omit ref
    void M5(long x); // Omit out
    void M6(out long x); // Toggle ref to out
    void M7(ref long x); // Toggle out to ref
    new void M8(long[] x); // Omit params
    new void M9(params long[] x); // Add params
}
class Base
{
    public long P { get { return 0; } set { Console.WriteLine(""I3.P""); } }
    public void M1(long x) { Console.WriteLine(""I1.M1""); } 
    public void M2(ref long x) { Console.WriteLine(""I3.M2""); }
    public void M2(long x) { Console.WriteLine(""I1.M2""); }
    public void M3(long x) { Console.WriteLine(""I1.M3""); }
    public void M3(out long x) { x = 0; Console.WriteLine(""I3.M3""); }
    public void M4(long x) { Console.WriteLine(""I3.M4""); }
    public void M4(ref long x) { Console.WriteLine(""I1.M4""); }
    public void M5(out long x) { x = 0; Console.WriteLine(""I3.M5""); }
    public void M5(long x) { Console.WriteLine(""I1.M5""); }
    public void M6(out long x) { x = 0; Console.WriteLine(""I3.M6""); }
    public void M7(out long x) { x = 0; Console.WriteLine(""I1.M7""); }
    public void M8(long[] x) { Console.WriteLine(""Base:I3.M8+I1.M8""); }
    public void M9(params long[] x) { Console.WriteLine(""Base:I3.M9+I1.M9""); }
}
class Test : Base, I3
{
    int I1.P { set { Console.WriteLine(""I1.P""); } } // Not possible to implement both I3.P and I1.P implicitly
    int I3.M1(long x) { Console.WriteLine(""I3.M1""); return 0; } // Not possible to implement both I3.M1 and I1.M1 implicitly
    public void M6(ref long x) { x = 0; Console.WriteLine(""I1.M6""); } // Not possible to implement both I3.M6 and I1.M6 implicitly in same class
    public void M7(ref long x) { Console.WriteLine(""I3.M7""); } // Not possible to implement both I3.M7 and I1.M7 implicitly in same class
    public void M8(params long[] x) { Console.WriteLine(""Derived:I3.M8+I1.M8""); } // Implements both I3.M8 and I1.M8
    public void M9(long[] x) { Console.WriteLine(""Derived:I3.M9+I1.M9""); } // Implements both I3.M9 and I1.M9

    public static void Main()
    {
        I3 i = new Test();
        long x = 1;
        i.M1(x); i.M2(x); i.M2(ref x); i.M3(x); i.M3(out x);
        i.M4(x); i.M4(ref x); i.M5(x); i.M5(out x);
        i.M6(out x); i.M6(ref x); i.M7(ref x); i.M7(out x);
        i.M8(new long[] { x, x, x }); i.M8(x, x, x);
        i.M9(new long[] { x, x, x }); i.M9(x, x, x);
        i.P = 1;

        I1 j = i;
        j.M1(x); j.M2(x); j.M3(x);
        j.M4(ref x); j.M5(out x);
        j.M6(ref x); j.M7(out x);
        j.M8(new long[] { x, x, x }); j.M8(x, x, x);
        j.M9(new long[] { x, x, x });
        j.P = 1;
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
I3.M1
I1.M2
I3.M2
I1.M3
I3.M3
I3.M4
I1.M4
I1.M5
I3.M5
I1.M6
I1.M6
I3.M7
I3.M7
Derived:I3.M8+I1.M8
Derived:I3.M8+I1.M8
Derived:I3.M9+I1.M9
Derived:I3.M9+I1.M9
I3.P
I1.M1
I1.M2
I1.M3
I1.M4
I3.M5
I1.M6
I3.M7
Derived:I3.M8+I1.M8
Derived:I3.M8+I1.M8
Derived:I3.M9+I1.M9
I1.P");

            comp.VerifyDiagnostics(
                // (55,17): warning CS0108: 'Test.M8(params long[])' hides inherited member 'Base.M8(long[])'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M8").WithArguments("Test.M8(params long[])", "Base.M8(long[])"),
                // (56,17): warning CS0108: 'Test.M9(long[])' hides inherited member 'Base.M9(params long[])'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M9").WithArguments("Test.M9(long[])", "Base.M9(params long[])"));
        }

        [Fact]
        public void TestImplementingWithObjectMembers()
        {
            var source = @"
using System;
interface I
{
    string ToString();
    int GetHashCode();
}
class C : I { }
class Base { public override string ToString() { return ""Base.ToString""; } }
class Derived : Base, I { }
class Test
{
    public static void Main()
    {
        I i = new C(); i = new Derived();
        Console.WriteLine(i.ToString());
    }
}";

            CompileAndVerify(source, expectedOutput: "Base.ToString").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplementHiddenMemberImplicitly()
        {
            var source = @"
using System;
interface I1
{
    void Method(int i);
    int P { set; }
}
interface I2 : I1
{
    new void Method(int i);
    new int P { set; }
}
class B
{
    public int P { set { Console.WriteLine(""B.set_Property""); } }
}
class C : B, I2
{
    public void Method(int j) { Console.WriteLine(""C.Method""); }
}
class Test
{
    public static void Main()
    {
        I2 i = new C();
        I1 j = i;
        i.Method(1); i.P = 0;
        j.Method(1); j.P = 0;
    }
}";

            CompileAndVerify(source, expectedOutput: @"
C.Method
B.set_Property
C.Method
B.set_Property").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplementHiddenMemberImplicitly2()
        {
            var source = @"
using System;
interface I1
{
    void Method(int i);
    long P { set; }
}
interface I2 : I1
{
    new int Method(int i);
    new int P { set; }
}
class B
{
    public int P { set { Console.WriteLine(""B.set_Property""); } }
    public int Method(int j) { Console.WriteLine(""B.Method""); return 0; }
}
class C : B, I2
{
    public new long P { set { Console.WriteLine(""C.set_Property""); } }
    public new void Method(int j) { Console.WriteLine(""C.Method""); }
}
class Test
{
    public static void Main()
    {
        I2 i = new C();
        I1 j = i;
        i.Method(1); i.P = 0;
        j.Method(1); j.P = 0;
    }
}";

            CompileAndVerify(source, expectedOutput: @"
B.Method
B.set_Property
C.Method
C.set_Property").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplicitImplementationWithHidingAcrossBaseTypes()
        {
            var source = @"using System;
interface I
{
    void Method(int i);
    long P { set; }
}
class B1
{
    public void Method(int j) { Console.WriteLine(""B1.Method""); }
    public int P { get { return 0; } set { Console.WriteLine(""B1.set_Property""); } }
}
class B2 : B1 { }
class B3 : B2
{
    public new void Method(int j) { Console.WriteLine(""B3.Method""); }
    public new long P { set { Console.WriteLine(""B3.set_Property""); } }
}
class B4 : B3
{
}
class D : B4, I
{
    public new int Method(int j) { Console.WriteLine(""D.Method""); return 0; }
    public new long P { get { return 0; } set { Console.WriteLine(""D.set_Property""); } }
}
class Test
{
    public static void Main()
    {
        I i = new D();
        i.Method(1); i.P = 0;
    }
}";

            CompileAndVerify(source, expectedOutput: @"
B3.Method
D.set_Property").VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplicitImplementationAcrossBaseTypes()
        {
            var source = @"using System;
interface I
{
    void Method(int i);
    long P { set; }
    void M();
}
class B1
{
    public void Method(int j) { Console.WriteLine(""B1.Method""); }
    public long P { get { return 0; } set { Console.WriteLine(""B1.set_Property""); } }
}
class B2 : B1 
{
    protected new void Method(int j) { Console.WriteLine(""B2.Method""); } // non-public members should be ignored
    public void M() { Console.WriteLine(""B2.M""); }
}
class B3 : B2
{
    internal new void Method(int j) { Console.WriteLine(""B3.Method""); } // non-public members should be ignored
}
class D : B3, I
{
    public static new long P { set { Console.WriteLine(""D.set_Property""); } } // static members should be ignored
    public new void M() { Console.WriteLine(""D.M""); } // should pick the member in most derived class
}
class Test
{
    public static void Main()
    {
        I i = new D();
        i.Method(1); i.P = 0; i.M();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
B1.Method
B1.set_Property
D.M").VerifyDiagnostics(); // No errors
        }

        /// <summary>
        /// Compile libSource into a dll and then compile exeSource with that DLL as a reference.
        /// Assert that neither compilation has errors.  Return the exe compilation.
        /// </summary>
        private static CSharpCompilation CreateCompilationWithMscorlibAndReference(string libSource, string exeSource)
        {
            var libComp = CreateCompilationWithMscorlib(libSource, options: TestOptions.ReleaseDll, assemblyName: "OtherAssembly");
            libComp.VerifyDiagnostics();

            var exeComp = CreateCompilationWithMscorlib(exeSource, options: TestOptions.ReleaseExe, references: new[] { new CSharpCompilationReference(libComp) });
            exeComp.VerifyDiagnostics();

            return exeComp;
        }
    }
}
