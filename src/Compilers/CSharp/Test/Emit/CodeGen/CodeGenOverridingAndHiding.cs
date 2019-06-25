// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenOverridingAndHidingTests : CSharpTestBase
    {
        [Fact]
        public void TestOverridingSimple()
        {
            // Tests:
            // Override virtual member
            // Override abstract member
            // Change parameter names in overridden member

            var source = @"
abstract class Base
{
    public abstract void Method(int a, ref string[] b);
    public virtual void Method(int a, System.Exception b)
    {
        System.Console.WriteLine(""Base.Method({0})"", a);
    }
    public virtual int Property
    {
        get
        {
            System.Console.WriteLine(""Base.Property.get()"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""Base.Property.set({0})"", value);
        }
    }
}

class Derived : Base
{
    public override void Method(int i, ref string[] j)
    {
        System.Console.WriteLine(""Derived.Method({0}, {1})"", i, j[0]);
    }
    public override int Property
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

class Derived2 : Derived
{
    public override void Method(int b, System.Exception c)
    { 
        System.Console.WriteLine(""Derived2.Method({0})"", b);
    }
}

class Test
{
    public static void Main()
    {
        Derived d = new Derived();
        Base b = d;
        Derived2 d2 = new Derived2();
        Base b2 = d2;
        Derived db = d2;
        
        string[] s1 = new string[] {""a""};
        string[] s2 = new string[] {""b""};
        string[] s3 = new string[] {""c""};
        d2.Method(1, ref s1);
        b2.Method(2, ref s1);
        d.Method(3, ref s2);
        b.Method(4, ref s3);

        d2.Method(3, new System.Exception());
        b2.Method(4, new System.ArgumentException());
        d.Method(5, new System.Exception());
        b.Method(6, new System.ArgumentException());
        db.Method(7, new System.Exception());

        int x = d2.Property;
        x = b2.Property;
        d.Property = 8;
        b.Property = 9;
        db.Property = 10;
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method(1, a)
Derived.Method(2, a)
Derived.Method(3, b)
Derived.Method(4, c)
Derived2.Method(3)
Derived2.Method(4)
Base.Method(5)
Base.Method(6)
Derived2.Method(7)
Derived.Property.get()
Derived.Property.get()
Derived.Property.set(8)
Derived.Property.set(9)
Derived.Property.set(10)",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method public hidebysig newslot abstract virtual instance System.Void Method(System.Int32 a, System.String[]& b) cil managed"),
                    Signature("Base", "Method", ".method public hidebysig newslot virtual instance System.Void Method(System.Int32 a, System.Exception b) cil managed"),
                    Signature("Base", "Property", ".property readwrite instance System.Int32 Property"),
                    Signature("Base", "get_Property", ".method public hidebysig newslot specialname virtual instance System.Int32 get_Property() cil managed"),
                    Signature("Base", "set_Property", ".method public hidebysig newslot specialname virtual instance System.Void set_Property(System.Int32 value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int32 i, System.String[]& j) cil managed"),
                    Signature("Derived", "Property", ".property readwrite instance System.Int32 Property"),
                    Signature("Derived", "get_Property", ".method public hidebysig specialname virtual instance System.Int32 get_Property() cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname virtual instance System.Void set_Property(System.Int32 value) cil managed"),
                    Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int32 b, System.Exception c) cil managed")
                });
        }

        [WorkItem(6470, "DevDiv_Projects/Roslyn")]
        [Fact()]
        public void TestHidingObjectMembers()
        {
            // Tests:
            // Hide / overload virtual methods declared on object (ToString, GetHashcode etc.)

            var source = @"
using System;
class BaseClass<TInt, TLong>
{
    public override string ToString() { Console.WriteLine(""BaseClass.ToString()""); return base.ToString(); }
    public virtual string ToString<T>() { Console.WriteLine(""BaseClass.ToString<T>()"");  return ToString(); }
    public new virtual int GetHashCode() { Console.WriteLine(""BaseClass.GetHashCode()""); return base.GetHashCode(); }
    public virtual long GetHashCode(TInt x) { Console.WriteLine(""BaseClass.GetHashCode({0})"", x.ToString()); return GetHashCode(); }
    public static new void Equals(object x, object y) { Console.WriteLine(""BaseClass.Equals({0}, {1})"", x.ToString(), y.ToString()); }
}

class DerivedClass : BaseClass<int, long>
{
    public new string ToString() { Console.WriteLine(""DerivedClass.ToString()""); return base.ToString(); }
    public new string ToString<T>() { Console.WriteLine(""DerivedClass.ToString<T>()""); return base.ToString<T>(); }
    public new virtual int GetHashCode() { Console.WriteLine(""DerivedClass.GetHashCode()""); return base.GetHashCode(); }
    public override long GetHashCode(int y) { Console.WriteLine(""DerivedClass.GetHashCode({0})"", y.ToString()); return base.GetHashCode(); }
    public override bool Equals(object obj) { Console.WriteLine(""DerivedClass.GetHashCode({0})"", obj.ToString()); return base.Equals(obj); }
    public static new void Equals(object x, object y) { Console.WriteLine(""DerivedClass.Equals({0}, {1})"", x.ToString(), y.ToString()); }
}

class Test
{
    public static void Main()
    {
        BaseClass<int, long> b = new DerivedClass();
        DerivedClass d = new DerivedClass();

        b.ToString();
        b.ToString<int>();
        b.GetHashCode();
        b.GetHashCode(0);
        b.Equals(1);
        BaseClass<int, long>.Equals(1, 2);

        d.ToString();
        d.ToString<int>();
        d.GetHashCode();
        d.GetHashCode(3);
        d.Equals(4);
        DerivedClass.Equals(5, 6);
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
BaseClass.ToString()
BaseClass.ToString<T>()
BaseClass.ToString()
BaseClass.GetHashCode()
DerivedClass.GetHashCode(0)
BaseClass.GetHashCode()
DerivedClass.GetHashCode(1)
BaseClass.Equals(1, 2)
DerivedClass.ToString()
BaseClass.ToString()
DerivedClass.ToString<T>()
BaseClass.ToString<T>()
BaseClass.ToString()
DerivedClass.GetHashCode()
BaseClass.GetHashCode()
DerivedClass.GetHashCode(3)
BaseClass.GetHashCode()
DerivedClass.GetHashCode(4)
DerivedClass.Equals(5, 6)",
                expectedSignatures: new[]
                {
                    Signature("BaseClass`2", "ToString", ".method public hidebysig virtual instance System.String ToString() cil managed"),
                    Signature("BaseClass`2", "ToString", ".method public hidebysig newslot virtual instance System.String ToString<T>() cil managed"),
                    Signature("DerivedClass", "ToString", ".method public hidebysig instance System.String ToString() cil managed"),
                    Signature("DerivedClass", "ToString", ".method public hidebysig instance System.String ToString<T>() cil managed"),
                    Signature("BaseClass`2", "GetHashCode", ".method public hidebysig newslot virtual instance System.Int32 GetHashCode() cil managed"),
                    Signature("BaseClass`2", "GetHashCode", ".method public hidebysig newslot virtual instance System.Int64 GetHashCode(TInt x) cil managed"),
                    Signature("DerivedClass", "GetHashCode", ".method public hidebysig newslot virtual instance System.Int32 GetHashCode() cil managed"),
                    Signature("DerivedClass", "GetHashCode", ".method public hidebysig virtual instance System.Int64 GetHashCode(System.Int32 y) cil managed")
                });

            comp.VerifyDiagnostics(
                // (12,7): warning CS0659: 'DerivedClass' overrides Object.Equals(object o) but does not override Object.GetHashCode()
                // class DerivedClass : BaseClass<int, long>
                Diagnostic(ErrorCode.WRN_EqualsWithoutGetHashCode, "DerivedClass").WithArguments("DerivedClass"));
        }

        [Fact]
        public void TestHidingDifferentMemberKinds()
        {
            // Tests:
            // Sanity check - hiding / overloading of one type of construct with another
            // (e.g. method with field / field with property etc.) should work

            var source = @"using System;
public class Class1
{
    public virtual float Member1 { set { Console.WriteLine(""Class1.Member1""); } } // virtual property
    public virtual void Member2() { Console.WriteLine(""Class1.Member2""); } // virtual method
}
public class Class2 : Class1
{
    new protected enum Member1 { First } // enum
    new interface Member2 { void Member1(); } // interface
    public static void Test()
    {
        Member1 x = Member1.First;
        Member2 y = null;
        Member1 z = x; x = z;
        Member2 w = y; y = w;
    }
    public class Class3 : Class2
    {
        new protected virtual void Member1() { Console.WriteLine(""Class3.Member1""); } // virtual method
        new public static int Member2 = 10; // static field
        public new static void Test()
        {
            Class3 a = new Class3();
            a.Member1();
            Class4.Member1();
            Class4 x = new Class4(); x.Member2(); // Does not bind to delegate?
            Class5 y = new Class5();
            Member2 = Class5.Member2 + y.Member1;
        }
        class Class4 : Class3
        {
            public new static void Member1() { Console.WriteLine(""Class4.Member1""); } // static method
            public new delegate void Member2(); // delegate
        }
        class Class5 : Class3
        {
            public new readonly int Member1 = 2; // readonly
            new public const int Member2 = 2; // const
        }
    }
}
public class Class5 : Class1
{
    protected new virtual float Member1 { get { Console.WriteLine(""Class5.get_Member1""); return 0; } set { Console.WriteLine(""Class5.set_Member1""); } } // virtual property
    public new virtual void Member2() { Console.WriteLine(""Class5.Member2""); } // virtual method
    public static void Test()
    {
        Class5 a = new Class5();
        a.Member2();
        Class6 y = new Class6(); y.Member2(); 
        a.Member1 = y.Member1;
        y.Member1 = a.Member1;
        Class7 z = new Class7();
        z.Member1 = z.Member1; z.Member2();
    }
    class Class6 : Class5
    {
        protected sealed override float Member1 { get { Console.WriteLine(""Class6.get_Member1""); return base.Member1; } } // overriding sealed property
        new private double[] Member2 = new double[] { }; // private field
    }
    class Class7 : Class6
    {
        protected new virtual double Member1 { get { Console.WriteLine(""Class7.get_Member1""); return base.Member1; } } // new virtual property
        public override void Member2() { Console.WriteLine(""Class7.Member2""); base.Member2(); } //overriding method
    }
}

class Test
{
    public static void Main()
    {
        Class2.Test();
        Class2.Class3.Test();
        Class5.Test();
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Class3.Member1
Class4.Member1
Class1.Member2
Class5.Member2
Class5.Member2
Class6.get_Member1
Class5.get_Member1
Class5.set_Member1
Class5.get_Member1
Class5.set_Member1
Class6.get_Member1
Class5.get_Member1
Class5.set_Member1
Class7.Member2
Class5.Member2",
               expectedSignatures: new[]
               {
                    Signature("Class2+Class3", "Member1", ".method family hidebysig newslot virtual instance System.Void Member1() cil managed"),
                    Signature("Class2+Class3", "Member2", ".field public static System.Int32 Member2"),
                    Signature("Class2+Class3+Class4", "Member1", ".method public hidebysig static System.Void Member1() cil managed"),
                    Signature("Class2+Class3+Class5", "Member1", ".field public initonly instance System.Int32 Member1"),
                    Signature("Class2+Class3+Class5", "Member2", ".field public literal static System.Int32 Member2 = 2"),
                    Signature("Class5", "Member2", ".method public hidebysig newslot virtual instance System.Void Member2() cil managed"),
                    Signature("Class5+Class6", "Member2", ".field private instance System.Double[] Member2"),
                    Signature("Class5+Class7", "Member2", ".method public hidebysig virtual instance System.Void Member2() cil managed")
               });
        }

        [Fact]
        public void TestOverridingChangeGenericParameterNames()
        {
            // Tests:
            // Change names of method-level type parameters in overridden method – test that we emit the type parameters correctly 

            var source = @"
using System;
using System.Collections.Generic;

abstract class Base<T, U, V>
{
    public virtual List<T> @virtual<T, U>(ref T x, U y, V z) { Console.WriteLine(""Base.virtual1""); return null; }
    public virtual List<T> @virtual<A, B>(T x, U y, V z, A a, B b) { Console.WriteLine(""Base.virtual2""); return null; }
}

class Derived<T, U> : Base<T, U, int>
{
    public override List<T> @virtual<T, U>(ref T x, U y, int z) { Console.WriteLine(""Derived.virtual1""); return null; }
    public override List<T> @virtual<A, B>(T x, U y, int z, A a, B b) { Console.WriteLine(""Derived.virtual2""); return null; }
}

class Derived2<X, Y> : Base<X, Y, int>
{
    public override List<A> @virtual<A, B>(ref A a, B b, int c) { Console.WriteLine(""Derived2.virtual1""); return null; }
    public override List<X> @virtual<T, U>(X a, Y b, int c, T d, U e) { Console.WriteLine(""Derived2.virtual2""); return null; }
}

class Test
{
    public static void Main()
    {
        Derived<int, long> d = new Derived<int, long>();
        Derived2<int, long> d2 = new Derived2<int, long>();
        Base<int, long, int> b = d;
        string s = """"; int i = 1; long l = 1;
        b.@virtual(ref s, i, i);
        b.@virtual(i, l, i, s, s);
        b = d2;
        b.@virtual(ref i, s, i);
        b.@virtual(i, l, i, i, l);
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.virtual1
Derived.virtual2
Derived2.virtual1
Derived2.virtual2",
                expectedSignatures: new[]
                {
                    Signature("Base`3", "virtual", ".method public hidebysig newslot virtual instance System.Collections.Generic.List`1[T] virtual<A, B>(T x, U y, V z, A a, B b) cil managed"),
                    Signature("Base`3", "virtual", ".method public hidebysig newslot virtual instance System.Collections.Generic.List`1[T] virtual<T, U>(T& x, U y, V z) cil managed"),
                    Signature("Derived`2", "virtual", ".method public hidebysig virtual instance System.Collections.Generic.List`1[T] virtual<A, B>(T x, U y, System.Int32 z, A a, B b) cil managed"),
                    Signature("Derived`2", "virtual", ".method public hidebysig virtual instance System.Collections.Generic.List`1[T] virtual<T, U>(T& x, U y, System.Int32 z) cil managed"),
                    Signature("Derived2`2", "virtual", ".method public hidebysig virtual instance System.Collections.Generic.List`1[A] virtual<A, B>(A& a, B b, System.Int32 c) cil managed"),
                    Signature("Derived2`2", "virtual", ".method public hidebysig virtual instance System.Collections.Generic.List`1[X] virtual<T, U>(X a, Y b, System.Int32 c, T d, U e) cil managed")
                });
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestOverridingWithParamsAndAliasedNames()
        {
            // Tests: 
            // Replace params with non-params in signature of overridden member (and vice-versa)
            // Use aliased name for type of parameter / return type in overridden member

            var source = @"
using System;
using TypeB = System.Int32;
using TypeC = NS.Derived;
using NSAlias2 = NS;
using NSAlias = NS;

abstract class Base
{
    internal abstract TypeB Method(System.Exception a, TypeB b, params NS.Derived[] c);
    public virtual void Method2(TypeC c1, NSAlias2.Derived c2, NS.Derived[] c3)
    {
        Console.WriteLine(""Base.Method2( , , [{0}])"", c3.Length);
    }
    public abstract void Method3(int[] b1, params int[] b2);
}

namespace NS
{
    using TypeA = System.Exception;
    abstract class Base2 : Base
    {
        // Adding additional 'params'
        public override void Method2(Derived c1, NS.Derived c2, params NSAlias.Derived[] C3)
        {
            Console.WriteLine(""Base2.Method2( , , [{0}])"", C3.Length);
        }
    }

    class Derived : Base2
    {
        // Omitting 'params'
        internal override TypeB Method(TypeA A, int B, TypeC[] C)
        {
            Console.WriteLine(""Derived.Method( , {0}, [{1}])"", B, C.Length);
            return 0;
        }
        // Preserving 'params'
        public override void Method3(int[] B1, params int[] b2)
        {
            Console.WriteLine(""Derived.Method3([{0}], [{1}])"", B1.Length, b2.Length);
        }
    }
}

class Test
{
    public static void Main()
    {
        TypeC d = new TypeC();
        Base b = d;
        d.Method(new System.Exception(), 1, new TypeC[]{d});
        b.Method(new System.Exception(), 2, d, d);
        // d.Method2(d, d, d, d, d); Should report error - No overload for Method2 takes 5 arguments
        b.Method2(d, d, new TypeC[]{d, d, d});
        d.Method3(new int[4]{1, 2, 3, 4}, new int[5] {6, 7, 8, 9, 10});
        b.Method3(new int[6]{1, 2, 3, 4, 5, 6}, 8, 9, 10, 11, 12, 13, 14);
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Method( , 1, [1])
Derived.Method( , 2, [2])
Base2.Method2( , , [3])
Derived.Method3([4], [5])
Derived.Method3([6], [7])",
                expectedSignatures: new[]
                {
                    Signature("Base", "Method", ".method assembly hidebysig newslot strict abstract virtual instance System.Int32 Method(System.Exception a, System.Int32 b, [System.ParamArrayAttribute()] NS.Derived[] c) cil managed"),
                    Signature("NS.Derived", "Method", ".method assembly hidebysig strict virtual instance System.Int32 Method(System.Exception A, System.Int32 B, [System.ParamArrayAttribute()] NS.Derived[] C) cil managed"),
                    Signature("Base", "Method2", ".method public hidebysig newslot virtual instance System.Void Method2(NS.Derived c1, NS.Derived c2, NS.Derived[] c3) cil managed"),
                    Signature("NS.Base2", "Method2", ".method public hidebysig virtual instance System.Void Method2(NS.Derived c1, NS.Derived c2, NS.Derived[] C3) cil managed"),
                    Signature("Base", "Method3", ".method public hidebysig newslot abstract virtual instance System.Void Method3(System.Int32[] b1, [System.ParamArrayAttribute()] System.Int32[] b2) cil managed"),
                    Signature("NS.Derived", "Method3", ".method public hidebysig virtual instance System.Void Method3(System.Int32[] B1, [System.ParamArrayAttribute()] System.Int32[] b2) cil managed")
                });
        }

        [Fact]
        public void TestOverridingVirtualWithAbstract()
        {
            // Tests: 
            // Override virtual member with abstract member – override this abstract member in further derived class 

            var source = @"
using System;

abstract class Base<T, U>
{
    T f;
    public abstract void Method(T i, U j);
    public virtual T Property
    {
        get { Console.WriteLine(""Base.get_Property""); return f; }
        set { Console.WriteLine(""Base.set_Property = {0}"", value.ToString()); }
    }
}

class Base2<A, B> : Base<A, B>
{
    public override void Method(A a, B b)
    {
        // base.Method(a, b); Error - Cannot call abstract base member
        Console.WriteLine(""Base2.Method({0}, {1})"", a.ToString(), b.ToString());
    }
    public override A Property
    {
        set { Console.WriteLine(""Base2.set_Property = {0}""); value.ToString(); }
    }
}

abstract class Base3<T, U> : Base2<T, U>
{
    public override abstract void Method(T x, U y);
    public override abstract T Property { set; }
}

class Base4<U, V> : Base3<U, V>
{
    U f;
    public override void Method(U x, V y)
    {
        // base.Method(x, y); Error - Cannot call abstract base member
        Console.WriteLine(""Base4.Method({0}, {1})"", x.ToString(), y.ToString());
    }
    public override U Property
    {
        set
        {
            // base.Property = f; Error - Cannot call abstract base member
            Console.WriteLine(""Base2.set_Property"");
        }
    }
}

class Derived : Base4<string, string>
{
}

class Test
{
    public static void Main()
    {
        Derived d = new Derived();
        Base<string, string> b = d;
        Base2<string, string> b2 = d;
        Base3<string, string> b3 = d;

        d.Method(""a"", ""b"");
        b.Method(""a"", ""b"");
        b2.Method(""a"", ""b"");
        b3.Method(""a"", ""b"");

        d.Property = ""a"";
        string x = d.Property;
        b.Property = ""b"";
        x = b.Property;
        b2.Property = ""c"";
        x = b2.Property;
        b3.Property = ""d"";
        x = b3.Property;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base4.Method(a, b)
Base4.Method(a, b)
Base4.Method(a, b)
Base4.Method(a, b)
Base2.set_Property
Base.get_Property
Base2.set_Property
Base.get_Property
Base2.set_Property
Base.get_Property
Base2.set_Property
Base.get_Property",
                expectedSignatures: new[]
                {
                    Signature("Base`2", "Method", ".method public hidebysig newslot abstract virtual instance System.Void Method(T i, U j) cil managed"),
                    Signature("Base`2", "Property", ".property readwrite instance T Property"),
                    Signature("Base`2", "get_Property", ".method public hidebysig newslot specialname virtual instance T get_Property() cil managed"),
                    Signature("Base`2", "set_Property", ".method public hidebysig newslot specialname virtual instance System.Void set_Property(T value) cil managed"),
                    Signature("Base2`2", "Method", ".method public hidebysig virtual instance System.Void Method(A a, B b) cil managed"),
                    Signature("Base2`2", "Property", ".property writeonly instance A Property"),
                    Signature("Base2`2", "set_Property", ".method public hidebysig specialname virtual instance System.Void set_Property(A value) cil managed"),
                    Signature("Base3`2", "Method", ".method public hidebysig abstract virtual instance System.Void Method(T x, U y) cil managed"),
                    Signature("Base3`2", "Property", ".property writeonly instance T Property"),
                    Signature("Base3`2", "set_Property", ".method public hidebysig specialname abstract virtual instance System.Void set_Property(T value) cil managed"),
                    Signature("Base4`2", "Method", ".method public hidebysig virtual instance System.Void Method(U x, V y) cil managed"),
                    Signature("Base4`2", "Property", ".property writeonly instance U Property"),
                    Signature("Base4`2", "set_Property", ".method public hidebysig specialname virtual instance System.Void set_Property(U value) cil managed")
                });
        }
        [Fact]

        private void TestBaseAccessForMembersHiddenInImmediateBaseClass()
        {
            // Tests: 
            // Invoke base virtual member from within overridden member using base.VirtualMember 
            // in case where an implementation for the member is hidden by accessibility 
            // in immediate base type but available in a further base type

            var source = @"
using System;

abstract class Base<T, U>
{
    T f;
    public virtual void Method(T i, U j)
    {
        Console.WriteLine(""Base.Method({0}, {1})"", i, j);
    }
    public virtual T Property
    {
        get { Console.WriteLine(""Base.get_Property()""); return f; }
        set { Console.WriteLine(""Base.set_Property({0})"", value); }
    }
}

class Base2<A, B> : Base<A, B>
{
    private new void Method(A a, B b)
    {
        Console.WriteLine(""Base2.Method({0}, {1})"", a, b);
    }
    private new A Property 
    {
        set { Console.WriteLine(""Base2.set_Property({0})"", value); } 
    }
}

class Derived<U, V> : Base2<U, V>
{
    U f;
    public override void Method(U x, V y)
    {
        base.Method(x, y);
    }
    public override U Property
    {
        set
        {
            f = base.Property;
            base.Property = f;
        }
    }
}

class Test
{
    public static void Main()
    {
        Derived<string, string> d = new Derived<string, string>();
        d.Method(""a"", ""b"");
        d.Property = ""c"";
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Base.Method(a, b)
Base.get_Property()
Base.set_Property()");

            comp.VerifyIL("Derived<U, V>.Method", @"
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldarg.2   
  IL_0003:  call       ""void Base<U, V>.Method(U, V)""
  IL_0008:  ret       
}");
            comp.VerifyIL("Derived<U, V>.Property.set", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.0   
  IL_0002:  call       ""U Base<U, V>.Property.get""
  IL_0007:  stfld      ""U Derived<U, V>.f""
  IL_000c:  ldarg.0   
  IL_000d:  ldarg.0   
  IL_000e:  ldfld      ""U Derived<U, V>.f""
  IL_0013:  call       ""void Base<U, V>.Property.set""
  IL_0018:  ret       
}");
        }
        [Fact]

        private void TestBaseAccessForMembersMissingInImmediateBaseClass()
        {
            // Tests: 
            // Invoke base virtual member from within overridden member using base.VirtualMember 
            // in case where an implementation for the member is missing 
            // in immediate base type but available in a further base type

            var source = @"
using System;

abstract class Base<T, U>
{
    T f;
    public virtual void Method(T i, U j)
    {
        Console.WriteLine(""Base.Method({0}, {1})"", i, j);
    }
    public virtual T Property
    {
        get { Console.WriteLine(""Base.get_Property()""); return f; }
        set { Console.WriteLine(""Base.set_Property({0})"", value); }
    }
}

class Base2<A, B> : Base<A, B>
{
}

class Derived<U, V> : Base2<U, V>
{
    U f;
    public override void Method(U x, V y)
    {
        base.Method(x, y);
    }
    public override U Property
    {
        get
        {
            f = base.Property;
            base.Property = f;
            return f;
        }
    }
}

class Test
{
    public static void Main()
    {
        Derived<string, string> d = new Derived<string, string>();
        d.Method(""a"", ""b"");
        string x = d.Property;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Base.Method(a, b)
Base.get_Property()
Base.set_Property()");

            comp.VerifyIL("Test.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  4
  IL_0000:  newobj     ""Derived<string, string>..ctor()""
  IL_0005:  dup
  IL_0006:  ldstr      ""a""
  IL_000b:  ldstr      ""b""
  IL_0010:  callvirt   ""void Base<string, string>.Method(string, string)""
  IL_0015:  callvirt   ""string Base<string, string>.Property.get""
  IL_001a:  pop
  IL_001b:  ret
}");
            comp.VerifyIL("Derived<U, V>.Method", @"
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldarg.2   
  IL_0003:  call       ""void Base<U, V>.Method(U, V)""
  IL_0008:  ret       
}");
            comp.VerifyIL("Derived<U, V>.Property.get", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.0   
  IL_0002:  call       ""U Base<U, V>.Property.get""
  IL_0007:  stfld      ""U Derived<U, V>.f""
  IL_000c:  ldarg.0   
  IL_000d:  ldarg.0   
  IL_000e:  ldfld      ""U Derived<U, V>.f""
  IL_0013:  call       ""void Base<U, V>.Property.set""
  IL_0018:  ldarg.0   
  IL_0019:  ldfld      ""U Derived<U, V>.f""
  IL_001e:  ret       
}");
        }
        [Fact]

        private void TestBaseAccessForObjectMembers()
        {
            // Tests: 
            // Override virtual methods declared on object (ToString, GetHashCode etc.) 
            // Sanity check – it should be possible to invoke virtual methods declared on object
            // from within derived type using base.ToString() etc.

            var source = @"
class BaseClass<TInt, TLong>
{
    public override string ToString() { return base.ToString(); }
    public override int GetHashCode() { return base.GetHashCode(); }
}

abstract class DerivedClass : BaseClass<int, long>
{
    public override int GetHashCode() { return base.GetHashCode(); }
    public override bool Equals(object obj) { return base.Equals(obj); }
}
";
            var comp = CompileAndVerify(source);

            comp.VerifyIL("BaseClass<TInt, TLong>.ToString", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""string object.ToString()""
  IL_0006:  ret       
}");
            comp.VerifyIL("BaseClass<TInt, TLong>.GetHashCode", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""int object.GetHashCode()""
  IL_0006:  ret       
}");
            comp.VerifyIL("DerivedClass.GetHashCode", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""int BaseClass<int, long>.GetHashCode()""
  IL_0006:  ret       
}");
            comp.VerifyIL("DerivedClass.Equals", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  call       ""bool object.Equals(object)""
  IL_0007:  ret       
}");
        }
        [Fact]

        private void TestOverridingFinalizeImpersonator()
        {
            // Tests:
            // Override overloaded member from base type named Finalize having same / different signature 
            // than object.Finalize()

            var source = @"using System;

abstract class Base<TInt, TLong>
{
    protected virtual void Finalize()
    {
        Console.WriteLine(""Base.Finalize()"");
    }
    protected abstract void Finalize(TInt x);
    protected abstract void Finalize(TLong y);
}

class Derived : Base<int, long>
{
    protected override void Finalize()
    {
        Console.WriteLine(""Derived.Finalize()"");
        base.Finalize();
    }
    protected override void Finalize(int x)
    {
        Console.WriteLine(""Derived.Finalize({0})"", x);
    }
    protected override void Finalize(long y)
    {
        Console.WriteLine(""Derived.Finalize({0}L)"", y);
    }

    public void Test()
    {
        this.Finalize();
        this.Finalize(1);
        this.Finalize(2L);
        base.Finalize();
    }
}

abstract class Base2
{
    protected abstract void Finalize();

    public void Test()
    {
        Base2 b = new Derived2();
        b.Finalize();
    }
}

class Derived2 : Base2
{
    protected override void Finalize()
    {
        Console.WriteLine(""Derived2.Finalize()"");
    }
}

class Test
{
    static void Main()
    {
        Derived d = new Derived();
        d.Test();
        Derived2 d2 = new Derived2();
        d2.Test();
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.Finalize()
Base.Finalize()
Derived.Finalize(1)
Derived.Finalize(2L)
Base.Finalize()
Derived2.Finalize()
",
                expectedSignatures: new[]
                {
                    Signature("Base`2", "Finalize", ".method family hidebysig newslot virtual instance System.Void Finalize() cil managed"),
                    Signature("Base`2", "Finalize", ".method family hidebysig newslot abstract virtual instance System.Void Finalize(TInt x) cil managed"),
                    Signature("Base`2", "Finalize", ".method family hidebysig newslot abstract virtual instance System.Void Finalize(TLong y) cil managed"),
                    Signature("Base2", "Finalize", ".method family hidebysig newslot abstract virtual instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize(System.Int32 x) cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize(System.Int64 y) cil managed"),
                    Signature("Derived2", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            comp.VerifyIL("Derived.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  callvirt   ""void Base<int, long>.Finalize()""
  IL_0006:  ldarg.0   
  IL_0007:  ldc.i4.1  
  IL_0008:  callvirt   ""void Base<int, long>.Finalize(int)""
  IL_000d:  ldarg.0   
  IL_000e:  ldc.i4.2  
  IL_000f:  conv.i8   
  IL_0010:  callvirt   ""void Base<int, long>.Finalize(long)""
  IL_0015:  ldarg.0   
  IL_0016:  call       ""void Base<int, long>.Finalize()""
  IL_001b:  ret       
}");

            comp.VerifyIL("Base2.Test", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Derived2..ctor()""
  IL_0005:  callvirt   ""void Base2.Finalize()""
  IL_000a:  ret
}");
        }
        [Fact]

        private void TestOverrideResolution2()
        {
            // Tests:
            // Override overloaded base virtual / abstract member – overloads differ by generic type parameter count
            // Override overloaded base virtual / abstract member – overloads spread across multiple base types

            var source = @"
using System;
abstract class Base<T, U>
{
    public abstract T Method();
    public abstract long Method<T>();
    public abstract long Method<T, U>();
}
abstract class Base2<A, B> : Base<A, B>
{
    A f;
    public override A Method()
    { Console.WriteLine(""Base2.Method()""); return f; }
    public override abstract long Method<A>();
    public override abstract long Method<A, B>();
}
class Derived : Base2<long, double>
{
    public override long Method()
    { base.Method(); Console.WriteLine(""Derived.Method()""); return 0; }
    public override long Method<X>()
    { Console.WriteLine(""Derived.Method<>""); return 0; }
    public override long Method<X, Y>()
    { Console.WriteLine(""Derived.Method<,>)""); return 0; }
}
class Test
{
    public static void Main()
    {
        Base<long, double> b = new Derived();
        b.Method();
        b.Method<int>();
        b.Method<int, int>();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Base2.Method()
Derived.Method()
Derived.Method<>
Derived.Method<,>)");
        }
        [Fact]

        private void TestOverrideResolution1()
        {
            // Tests:
            // Override overloaded base virtual / abstract member – overloads differ by parameter types and count
            // Override overloaded base virtual / abstract member – overloads spread across multiple base types

            var source = @"
using System;
using System.Collections.Generic;
abstract class Base<T, U>
{
    public abstract T Method(int x);
    public abstract T Method(List<T> x);
    public abstract long Method<T>(List<int> x);
    public abstract T Method(int x, string y);
    public abstract T Method(List<T> x, List<U> y);
    public abstract T Method<V>(T x, U y);
    public abstract T Method<V>(int x, string y);
    public abstract T Method<V>(List<T> x, List<V> y);
    public abstract T Method<V>(List<int> x, List<string> y);
}
abstract class Base2<A, B> : Base<A, B>
{
    A f;
    public override A Method(int x) { return f; }
    public abstract A Method(A x);
    public abstract A Method(List<int> x);
    public abstract long Method<A>(List<A> x);
    public abstract A Method(A x, B y);
    public abstract A Method(List<int> x, List<string> y);
    public abstract A Method<V>(A x, V y);
    public abstract A Method<V>(List<A> x, List<B> y);
    public abstract A Method<V>(List<int> x, List<long> y);
}
class Derived : Base2<long, double>
{
    public override long Method(int x) { return 1; }
    public override long Method(long x) { return 2; }
    public override long Method(List<long> x) { return 3; }
    public override long Method(List<int> x) { return 4; }
    public override long Method<X>(List<X> x) { return 5; }
    public override long Method<X>(List<int> x) { return 6; }
    public override long Method(long x, double y) { return 7; }
    public override long Method(int x, string y) { return 8; }
    public override long Method(List<long> x, List<double> y) { return 9; }
    public override long Method(List<int> x, List<string> y) { return 10; }
    public override long Method<V>(long x, V y) { return 11; }
    public override long Method<V>(long x, double y) { return 12; }
    public override long Method<V>(int x, string y) { return 13; }
    public override long Method<V>(List<long> x, List<V> y) { return 14; }
    public override long Method<V>(List<long> x, List<double> y) { return 15; }
    public override long Method<V>(List<int> x, List<string> y) { return 16; }
    public override long Method<V>(List<int> x, List<long> y) { return 17; }
}
class Test
{
    public static void Main()
    {
        Base2<long, double> b = new Derived();
        int i = 1; long l = 1L; double d = 1D;
        Console.Write(b.Method(i));
        Console.Write(b.Method(new List<long>()));
        Console.Write(b.Method(new List<int>()));
        Console.Write(b.Method(new List<string>()));
        Console.Write(b.Method<int>(new List<int>()));
        Console.Write(b.Method(l, d));
        Console.Write(b.Method(i, """"));
        Console.Write(b.Method(new List<long>(), new List<double>()));
        Console.Write(b.Method(new List<int>(), new List<string>()));
        Console.Write(b.Method(l, """"));
        Console.Write(b.Method<double>(1L, d));
        Console.Write(b.Method<string>(1, """"));
        Console.Write(b.Method(new List<long>(), new List<int>()));
        Console.Write(b.Method<int>(new List<long>(), new List<double>()));
        Console.Write(b.Method<int>(new List<int>(), new List<string>()));
        Console.Write(b.Method<int>(new List<int>(), new List<long>()));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"2545571191011111114151617");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "Test of execution of explicitly ambiguous IL")]
        private void TestAmbiguousOverridesWarningCase()
        {
            // Tests:
            // Test that we continue to report errors / warnings even when ambiguous base methods that we are trying to
            // override only differ by ref / out - test that only a warning (for runtime ambiguity) is reported 
            // in the case where ambiguous signatures differ by just ref / out

            var source = @"
using System;
using System.Collections.Generic;
abstract class Base<T, U>
{
    public virtual void Method(ref List<T> x, out List<U> y) { Console.WriteLine(""Base.Method(ref, out)""); y = null; }
    public virtual void Method(out List<U> y, ref List<T> x) { Console.WriteLine(""Base.Method(out, ref)""); y = null; }
    public virtual void Method(ref List<U> x) { Console.WriteLine(""Base.Method(ref)""); }
}
class Base2<A, B> : Base<A, B>
{
    public virtual void Method(out List<A> x) { Console.WriteLine(""Base2.Method(out)""); x = null; }
}
class Derived : Base2<int, int>
{
    public override void Method(ref List<int> a, out List<int> b) { Console.WriteLine(""Derived.Method(ref, out)""); b = null; } // Reports warning about runtime ambiguity
    public override void Method(ref List<int> a) { Console.WriteLine(""Derived.Method(ref)""); } // No warning when ambiguous signatures are spread across multiple base types
}
class Test
{
    public static void Main()
    {
        Base<int, int> b = new Derived();
        List<int> arg = new List<int>();
        b.Method(out arg, ref arg);
        b.Method(ref arg, out arg);
        b.Method(ref arg);
    }
}
";
            // Note: This test is exercising a case that is 'Runtime Ambiguous'. In the generated IL, it is ambiguous which
            // method is being overridden. As far as I can tell, the output won't change from build to build / machine to machine
            // although it may change from one version of the CLR to another (not sure). If it turns out that this makes
            // the test flaky, we can delete this test.

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived.Method(ref, out)
Base.Method(ref, out)
Base.Method(ref)");
        }
        [WorkItem(540214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540214")]
        [Fact]

        private void TestEmitSynthesizedSealedSetter()
        {
            var source = @"
class Base
{
    public virtual int P
    {
        get
        {
            System.Console.WriteLine(""Base.P.Get=1"");
            return 1;
        }

        set
        {
            System.Console.WriteLine(""Base.P.Set({0})"", value);
        }
    }
}

class Derived : Base
{
    public sealed override int P
    {
        get
        {
            System.Console.WriteLine(""Derived.P.Get=1"");
            return 1;
        }
    }
}

class Program
{
    static void Main()
    {
        Derived d = new Derived();
        Base bd = d;

        d.P++;
        bd.P++;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.P.Get=1
Base.P.Set(2)
Derived.P.Get=1
Base.P.Set(2)",
                expectedSignatures: new[]
                {
                    Signature("Derived", "set_P", ".method public hidebysig specialname virtual final instance System.Void set_P(System.Int32 value) cil managed")
                });
        }
        [WorkItem(540214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540214")]
        [Fact]

        private void TestEmitSynthesizedSealedGetter()
        {
            var source = @"
class Base
{
    public virtual int P
    {
        get
        {
            System.Console.WriteLine(""Base.P.Get=1"");
            return 1;
        }

        set
        {
            System.Console.WriteLine(""Base.P.Set({0})"", value);
        }
    }
}

class Derived : Base
{
    public sealed override int P
    {
        set
        {
            System.Console.WriteLine(""Derived.P.Set({0})"", value);
        }
    }
}

class Program
{
    static void Main()
    {
        Derived d = new Derived();
        Base bd = d;

        d.P++;
        bd.P++;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.P.Get=1
Derived.P.Set(2)
Base.P.Get=1
Derived.P.Set(2)",
                expectedSignatures: new[]
                {
                    Signature("Derived", "get_P", ".method public hidebysig specialname virtual final instance System.Int32 get_P() cil managed")
                });
        }
        [WorkItem(540327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540327")]
        [Fact]

        private void TestOverrideWithSealedProperty()
        {
            var source = @"using System;
abstract public class Base
{
    public virtual float Property1 { set { Console.WriteLine(""Base.set_Property1""); } }
    public virtual float Property2 { get { Console.WriteLine(""Base.get_Property2""); return 0; } }
    public abstract float Property3 { get; set; }

    public class Derived : Base
    {
        public sealed override float Property1 { set { Console.WriteLine(""Derived.set_Property1""); } }
        public sealed override float Property2 { get { Console.WriteLine(""Derived.get_Property2""); return 1; } }
        public sealed override float Property3
        { 
            get { Console.WriteLine(""Derived.get_Property3""); return 2; } 
            set { Console.WriteLine(""Derived.set_Property3"");}
        }
    }
}
class Test
{
    public static void Main()
    {
        Base b = new Base.Derived();
        b.Property1 = b.Property2;
        b.Property3 = b.Property3;
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.get_Property2
Derived.set_Property1
Derived.get_Property3
Derived.set_Property3",
                expectedSignatures: new[]
                {
                    Signature("Base+Derived", "Property1", ".property writeonly instance System.Single Property1"),
                    Signature("Base+Derived", "set_Property1", ".method public hidebysig specialname virtual final instance System.Void set_Property1(System.Single value) cil managed"),
                    Signature("Base+Derived", "Property2", ".property readonly instance System.Single Property2"),
                    Signature("Base+Derived", "get_Property2", ".method public hidebysig specialname virtual final instance System.Single get_Property2() cil managed"),
                    Signature("Base+Derived", "Property3", ".property readwrite instance System.Single Property3"),
                    Signature("Base+Derived", "get_Property3", ".method public hidebysig specialname virtual final instance System.Single get_Property3() cil managed"),
                    Signature("Base+Derived", "set_Property3", ".method public hidebysig specialname virtual final instance System.Void set_Property3(System.Single value) cil managed")
                });
        }
        [Fact]

        private void TestOverrideWithAbstractProperty()
        {
            var source = @"using System;
public class Base1
{
    public virtual long Property1 
    { 
        get { Console.WriteLine(""Base1.get_Property1""); return 0; } 
        set { Console.WriteLine(""Base1.set_Property1""); } 
    }

    public virtual long Property2 
    { 
        get { Console.WriteLine(""Base1.get_Property2""); return 0; } 
        set { Console.WriteLine(""Base1.set_Property2""); } 
    }
}
abstract public class Base2 : Base1
{
    public abstract override long Property1 { get; }

    public abstract override long Property2 { set; }
}
public class Derived : Base2
{
    public override long Property1
    { 
        get { Console.WriteLine(""Derived.get_Property1""); return 1; }
    }
    public override long Property2
    { 
        get { Console.WriteLine(""Derived.get_Property2""); return 1; }
        set { Console.WriteLine(""Derived.set_Property2""); }
    }
}
class Test
{
    public static void Main()
    {
        Base1 b = new Derived();
        Base2 b2 = new Derived();
        b.Property1++;
        ++b.Property2;
        b2.Property1 -= 1;
        b2.Property2 *= 1;
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.get_Property1
Base1.set_Property1
Derived.get_Property2
Derived.set_Property2
Derived.get_Property1
Base1.set_Property1
Derived.get_Property2
Derived.set_Property2",
                expectedSignatures: new[]
                {
                    Signature("Base2", "Property1", ".property readonly instance System.Int64 Property1"),
                    Signature("Base2", "get_Property1", ".method public hidebysig specialname abstract virtual instance System.Int64 get_Property1() cil managed"),
                    Signature("Base2", "Property2", ".property writeonly instance System.Int64 Property2"),
                    Signature("Base2", "set_Property2", ".method public hidebysig specialname abstract virtual instance System.Void set_Property2(System.Int64 value) cil managed"),
                    Signature("Derived", "Property1", ".property readonly instance System.Int64 Property1"),
                    Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual instance System.Int64 get_Property1() cil managed"),
                    Signature("Derived", "Property2", ".property readwrite instance System.Int64 Property2"),
                    Signature("Derived", "get_Property2", ".method public hidebysig specialname virtual instance System.Int64 get_Property2() cil managed"),
                    Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual instance System.Void set_Property2(System.Int64 value) cil managed")
                });
        }
        [Fact]

        private void TestOverrideWithAbstractProperty2()
        {
            var source = @"using System;
public class Base1
{
    public virtual long Property1 
    { 
        get { Console.WriteLine(""Base1.get_Property1""); return 0; } 
    }
    public virtual long Property2 
    { 
        set { Console.WriteLine(""Base1.set_Property2""); } 
    }
    public virtual long Property3 
    { 
        get { Console.WriteLine(""Base1.get_Property3""); return 0; } 
        set { Console.WriteLine(""Base1.set_Property3""); } 
    }
}
abstract public class Base2 : Base1
{
    public abstract override long Property1 { get; }
    public abstract override long Property2 { set; }
    public abstract override long Property3 { get; set; }
}
public class Derived : Base2
{
    public override long Property1
    { 
        get { Console.WriteLine(""Derived.get_Property1""); return 1; }
    }
    public override long Property2
    { 
        set { Console.WriteLine(""Derived.set_Property2""); }
    }
    public override long Property3 
    { 
        get { Console.WriteLine(""Derived.get_Property3""); return 1; } 
        set { Console.WriteLine(""Derived.set_Property3""); } 
    }
}
class Test
{
    public static void Main()
    {
        Base1 b = new Derived();
        Base2 b2 = new Derived();
        long x = b.Property1;
        b.Property2 = x;
        --b.Property3;
        x = b2.Property1;
        b2.Property2 = x;
        b2.Property3--;
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived.get_Property1
Derived.set_Property2
Derived.get_Property3
Derived.set_Property3
Derived.get_Property1
Derived.set_Property2
Derived.get_Property3
Derived.set_Property3",
                expectedSignatures: new[]
                {
                    Signature("Base2", "Property1", ".property readonly instance System.Int64 Property1"),
                    Signature("Base2", "get_Property1", ".method public hidebysig specialname abstract virtual instance System.Int64 get_Property1() cil managed"),
                    Signature("Base2", "Property2", ".property writeonly instance System.Int64 Property2"),
                    Signature("Base2", "set_Property2", ".method public hidebysig specialname abstract virtual instance System.Void set_Property2(System.Int64 value) cil managed"),
                    Signature("Base2", "Property3", ".property readwrite instance System.Int64 Property3"),
                    Signature("Base2", "get_Property3", ".method public hidebysig specialname abstract virtual instance System.Int64 get_Property3() cil managed"),
                    Signature("Base2", "set_Property3", ".method public hidebysig specialname abstract virtual instance System.Void set_Property3(System.Int64 value) cil managed"),
                    Signature("Derived", "Property1", ".property readonly instance System.Int64 Property1"),
                    Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual instance System.Int64 get_Property1() cil managed"),
                    Signature("Derived", "Property2", ".property writeonly instance System.Int64 Property2"),
                    Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual instance System.Void set_Property2(System.Int64 value) cil managed"),
                    Signature("Derived", "Property3", ".property readwrite instance System.Int64 Property3"),
                    Signature("Derived", "get_Property3", ".method public hidebysig specialname virtual instance System.Int64 get_Property3() cil managed"),
                    Signature("Derived", "set_Property3", ".method public hidebysig specialname virtual instance System.Void set_Property3(System.Int64 value) cil managed")
                });
        }

        [Fact]
        public void TestHidingStaticMembers()
        {
            // Tests:
            // Hide static base members using new
            // Overload static base member with member that has different signature
            // Hide private base members

            var source = @"
using System;
class Base
{
    static void Method() { Console.WriteLine(""Base.Method()""); }
    static void Method<T>() { Console.WriteLine(""Base.Method<T>()""); }
    static void Method<T>(T x, int y) { Console.WriteLine(""Base.Method<T>(T, int)""); }
    static long Property { get { Console.WriteLine(""Base.get_Property()""); return 0; } set { Console.WriteLine(""Base.set_Property()""); } }
    static public class Type { public static void Method() { Console.WriteLine(""Base.Type.Method()""); } }
    public class Derived : Base
    {
        static void Method() { Console.WriteLine(""Derived.Method()""); }
        new long Property { set { Console.WriteLine(""Derived.set_Property()""); } }
        static void Method<T>(T x) { Console.WriteLine(""Derived.Method<T>(T)""); }
        public static void Method<T, U>(T x, int y) { Console.WriteLine(""Derived.Method<T, U>(T, int)""); }
        new public class Type { public static void Method() { Console.WriteLine(""Derived.Type.Method()""); } }
        public new static void Test()
        {
            Derived.Method();
            Derived x = new Derived(); x.Property = 2;
            Method<int>(1);
            Type.Method();
        }
    }
    public static void Test()
    {
        Derived.Method();
        Derived.Method<int>();
        Derived.Method<int>(1, 2);
        Derived.Method<int, int>(1, 2);
        Derived.Property = 2;
        Derived.Type.Method();
    }
}
class Base2
{
    int Field = 2;
    protected const int Field2 = 2;
    public readonly long Field3 = 3;
    private long Field4 = 3;
    public class Type<T>
    {
        public static void Method() { Console.WriteLine(""Base2.Type<T>.Method()""); }
    }
    public class Type2<T>
    {
        public static void Method() { Console.WriteLine(""Base2.Type2<T>.Method()""); }
    }
    public void Test()
    {
        int x = Field;
        long y = Field4;
        Type<int>.Method();
        Type2<int>.Method();
        Derived2.Type<int>.Method();
        Derived2.Type<int, long>.Method();
        Derived2.Type<int, long>.Type2<int>.Method();
        Derived2.Type2<int>.Method();
    }
    public class Derived2 : Base2
    {
        int Field = 2;
        public int Field2 = 3;
        new public long Field4 = 4;
        new public static void Field3() { Console.WriteLine(""Derived2.Field3""); }
        public abstract class Type<T, U>
        {
            public static void Method() { Console.WriteLine(""Derived2.Type<T, U>.Method()""); }
            public class Type2<X>
            {
                public static void Method() { Console.WriteLine(""Derived2.Type2<T>.Method()""); }
            }
        }
        public class Type2<T>
        {
            public static void Method() { Console.WriteLine(""Derived2.Type<T, U>.Method()""); }
        }
        public new void Test()
        {
            Field4 = base.Field4;
            int x = Field;
            x = Field2;
            Field3();
            x = Base2.Field2;
            long y = base.Field3;
            y = Field4 = base.Field4;
        }
    }
}
class Base3
{
    int Field = 1;
    class Type { }
    public static int Field2 = 1;
    void Method() { }
}
abstract class Derived3 : Base3
{
    new int Field = 2;
    public class Type { }
    protected static int Field2 = 1;
    public abstract void Method();
}
class Test
{
    public static void Main()
    {
        Base.Test();
        Base.Derived.Test();
        Base2 b = new Base2();
        b.Test();
        Base2.Derived2 d = new Base2.Derived2();
        d.Test();
        Base2.Derived2.Field3();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Base.Method()
Base.Method<T>()
Base.Method<T>(T, int)
Derived.Method<T, U>(T, int)
Base.set_Property()
Derived.Type.Method()
Derived.Method()
Derived.set_Property()
Derived.Method<T>(T)
Derived.Type.Method()
Base2.Type<T>.Method()
Base2.Type2<T>.Method()
Base2.Type<T>.Method()
Derived2.Type<T, U>.Method()
Derived2.Type2<T>.Method()
Derived2.Type<T, U>.Method()
Derived2.Field3
Derived2.Field3");

            comp.VerifyDiagnostics(
                // (12,21): warning CS0108: 'Base.Derived.Method()' hides inherited member 'Base.Method()'. Use the new keyword if hiding was intended.
                //         static void Method() { Console.WriteLine("Derived.Method()"); }
                Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("Base.Derived.Method()", "Base.Method()").WithLocation(12, 21),
                // (62,13): warning CS0108: 'Base2.Derived2.Field' hides inherited member 'Base2.Field'. Use the new keyword if hiding was intended.
                //         int Field = 2;
                Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Base2.Derived2.Field", "Base2.Field").WithLocation(62, 13),
                // (63,20): warning CS0108: 'Base2.Derived2.Field2' hides inherited member 'Base2.Field2'. Use the new keyword if hiding was intended.
                //         public int Field2 = 3;
                Diagnostic(ErrorCode.WRN_NewRequired, "Field2").WithArguments("Base2.Derived2.Field2", "Base2.Field2").WithLocation(63, 20),
                // (74,22): warning CS0108: 'Base2.Derived2.Type2<T>' hides inherited member 'Base2.Type2<T>'. Use the new keyword if hiding was intended.
                //         public class Type2<T>
                Diagnostic(ErrorCode.WRN_NewRequired, "Type2").WithArguments("Base2.Derived2.Type2<T>", "Base2.Type2<T>").WithLocation(74, 22),
                // (99,13): warning CS0109: The member 'Derived3.Field' does not hide an accessible member. The new keyword is not required.
                //     new int Field = 2;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Field").WithArguments("Derived3.Field").WithLocation(99, 13),
                // (101,26): warning CS0108: 'Derived3.Field2' hides inherited member 'Base3.Field2'. Use the new keyword if hiding was intended.
                //     protected static int Field2 = 1;
                Diagnostic(ErrorCode.WRN_NewRequired, "Field2").WithArguments("Derived3.Field2", "Base3.Field2").WithLocation(101, 26),
                // (92,9): warning CS0414: The field 'Base3.Field' is assigned but its value is never used
                //     int Field = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Field").WithArguments("Base3.Field").WithLocation(92, 9),
                // (99,13): warning CS0414: The field 'Derived3.Field' is assigned but its value is never used
                //     new int Field = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Field").WithArguments("Derived3.Field").WithLocation(99, 13));
        }

        [Fact]
        public void TestHidingWarnings()
        {
            // Tests:
            // Hide base virtual member using new
            // By default members should be hidden by signature if new is not specified
            // new should hide by signature

            var source = @"
using System;
using System.Collections.Generic;
class Base<T>
{
    public virtual void Method()
    {
        Console.WriteLine(""Base<T>.Method()"");
    }
    public virtual void Method(T x)
    {
        Console.WriteLine(""Base<T>.Method(T)"");
    }
    public virtual void Method(T x, T y, List<T> a, Dictionary<T, T> b)
    {
        Console.WriteLine(""Base<T>.Method(T, T, List<T>, Dictionary<T, T>)"");
    }
    public virtual void Method<U>(T x, T y)
    {
        Console.WriteLine(""Base<T>.Method<U>(T, T)"");
    }
    public virtual void Method<U>(U x, T y, List<U> a, Dictionary<T, U> b)
    {
        Console.WriteLine(""Base<T>.Method<U>(U, T, List<U>, Dictionary<T, U>)"");
    }
    public virtual int Property1
    {
        get { Console.WriteLine(""Base<T>.Property""); return 0; }
    }
    public virtual int Property2
    {
        get { Console.WriteLine(""Base<T>.Property""); return 0; }
        set { }
    }
    public virtual void Method2()
    {
        Console.WriteLine(""Base<T>.Method2()"");
    }
    public virtual void Method3() 
    {
        Console.WriteLine(""Base<T>.Method3()"");
    }
}
class Derived<U> : Base<U>
{
    public new void Method(U x, U y)
    {
        Console.WriteLine(""Derived<U>.Method(U, U)"");
    }
    public void Method(U x, U y, List<U> a, Dictionary<U, U> b)
    {
        Console.WriteLine(""Derived<U>.Method(U, U, List<U>, Dictionary<U, U>)"");
    }
    public void Method<V>(V x, U y, List<V> a, Dictionary<U, V> b)
    {
        Console.WriteLine(""Derived<U>.Method(U, U)"");
    }
    public new void Method<V>(V x, U y, List<V> a, Dictionary<V, U> b)
    {
        Console.WriteLine(""Derived<U>.Method(U, U)"");
    }
    public virtual int Property1
    {
        set { Console.WriteLine(""Derived<U>.Property1""); }
    }
    public static int Property2 { get; set; }
    public static void Method(U i)
    {
        Console.WriteLine(""Derived<U>.Method(U)"");
    }
    public int Method2 = 0;
    public void Method<A, B>(U x, U y)
    {
        Console.WriteLine(""Derived<U>.Method<A, B>(U, U)"");
    }
    public new int Method3 { get; set; }
}
class Derived2 : Derived<int>
{
    public override void Method()
    {
        Console.WriteLine(""Derived2.Method()"");
    }
    public override void Method<U>(int x, int y)
    {
        Console.WriteLine(""Derived2.Method<U>(int x, int y)"");
    }
    public override int Property1
    {
        set { Console.WriteLine(""Derived2.Property1""); }
    }
}
class Test
{
    public static void Main()
    {
        Derived2 d2 = new Derived2();
        Derived<int> d = d2;
        Base<int> b = d2;

        b.Method();
        b.Method(1);
        b.Method<int>(1, 1);
        b.Method<int>(1, 1, new List<int>(), new Dictionary<int, int>());
        b.Method(1, 1, new List<int>(), new Dictionary<int, int>());
        b.Method2();
        int x = b.Property1;
        b.Property2 -= 1;
        b.Method3();

        d.Method();
        Derived<int>.Method(1);
        d.Method<int>(1, 1);
        d.Method<long>(1, 1, new List<long>(), new Dictionary<int, long>());
        d.Method<long>(1, 1, new List<long>(), new Dictionary<long, int>());
        d.Method(1, 1, new List<int>(), new Dictionary<int, int>());
        d.Method2();
        d.Method2 = 2; // Both Method2's are visible?
        d.Method<int, int>(1, 1);
        d.Property1 = 1;
        Derived<int>.Property2 = Derived<int>.Property2;
        d.Method3();
        x = d.Method3;
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"Derived2.Method()
Base<T>.Method(T)
Derived2.Method<U>(int x, int y)
Base<T>.Method<U>(U, T, List<U>, Dictionary<T, U>)
Base<T>.Method(T, T, List<T>, Dictionary<T, T>)
Base<T>.Method2()
Base<T>.Property
Base<T>.Property
Base<T>.Method3()
Derived2.Method()
Derived<U>.Method(U)
Derived2.Method<U>(int x, int y)
Derived<U>.Method(U, U)
Derived<U>.Method(U, U)
Derived<U>.Method(U, U, List<U>, Dictionary<U, U>)
Base<T>.Method2()
Derived<U>.Method<A, B>(U, U)
Derived2.Property1
Base<T>.Method3()");

            comp.VerifyDiagnostics(
                // (43,21): warning CS0109: The member 'Derived<U>.Method(U, U)' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("Derived<U>.Method(U, U)"),
                // (47,17): warning CS0114: 'Derived<U>.Method(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)' hides inherited member 'Base<U>.Method(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Derived<U>.Method(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)", "Base<U>.Method(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)"),
                // (51,17): warning CS0114: 'Derived<U>.Method<V>(V, U, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<U, V>)' hides inherited member 'Base<U>.Method<U>(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Derived<U>.Method<V>(V, U, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<U, V>)", "Base<U>.Method<U>(U, U, System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, U>)"),
                // (55,21): warning CS0109: The member 'Derived<U>.Method<V>(V, U, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<V, U>)' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("Derived<U>.Method<V>(V, U, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<V, U>)"),
                // (64,24): warning CS0114: 'Derived<U>.Method(U)' hides inherited member 'Base<U>.Method(U)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Derived<U>.Method(U)", "Base<U>.Method(U)"),
                // (59,24): warning CS0114: 'Derived<U>.Property1' hides inherited member 'Base<U>.Property1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Property1").WithArguments("Derived<U>.Property1", "Base<U>.Property1"),
                // (63,23): warning CS0114: 'Derived<U>.Property2' hides inherited member 'Base<U>.Property2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Property2").WithArguments("Derived<U>.Property2", "Base<U>.Property2"),
                // (68,16): warning CS0108: 'Derived<U>.Method2' hides inherited member 'Base<U>.Method2()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Method2").WithArguments("Derived<U>.Method2", "Base<U>.Method2()"));
        }

        [Fact]
        public void TestOverloadingByRefOutDifferences()
        {
            var text = @"
using System;
abstract class Base
{
    public abstract void Method(ref int x);
    public abstract void Method(Exception x);
    public abstract void Method(out long x);
    public abstract void Method(ArgumentException x);
}
abstract class Base2 : Base
{
    public abstract void Method(int x);
    public abstract void Method(out Exception x);
    public abstract void Method(long x);
    public abstract void Method(ref ArgumentException x);
}
class Derived2 : Base2
{
    public override void Method(ref int x) { }
    public override void Method(int x) { }
    public override void Method(Exception x) { }
    public override void Method(out Exception x) { x = null; }
    public override void Method(out long x) { x = 0;  }
    public override void Method(ArgumentException x) { }
    public override void Method(long x) { }
    public override void Method(ref ArgumentException x) { }
}";

            var comp = CompileAndVerify(text, expectedSignatures: new[]
            {
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.ArgumentException x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.ArgumentException& x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.Exception x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method([out] System.Exception& x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int32 x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int32& x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int64 x) cil managed"),
                Signature("Derived2", "Method", ".method public hidebysig virtual instance System.Void Method([out] System.Int64& x) cil managed")
            });

            comp.VerifyDiagnostics();
        }
        [WorkItem(540341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540341")]
        [Fact]

        private void TestInternalMethods()
        {
            // Tests:
            // internal virtual / abstract methods should be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    internal virtual List<T> Method1() { return null; }
}

abstract class Base2<T> : Base<T>
{
    internal abstract List<T> Method2();
}

class Derived : Base2<int>
{
    internal sealed override List<int> Method1(){ return null; }
    internal sealed override List<int> Method2(){ return null; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "Method1", ".method assembly hidebysig newslot strict virtual instance System.Collections.Generic.List`1[T] Method1() cil managed"),
                Signature("Base2`1", "Method2", ".method assembly hidebysig newslot strict abstract virtual instance System.Collections.Generic.List`1[T] Method2() cil managed"),
                Signature("Derived", "Method1", ".method assembly hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method1() cil managed"),
                Signature("Derived", "Method2", ".method assembly hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method2() cil managed"),
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestProtectedInternalMethods()
        {
            // Tests:
            // protected internal virtual / abstract methods should not be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    protected internal virtual List<T> Method1() { return null; }
}

abstract class Base2<T> : Base<T>
{
    protected internal abstract List<T> Method2();
}

class Derived : Base2<int>
{
    protected internal sealed override List<int> Method1(){ return null; }
    protected internal sealed override List<int> Method2(){ return null; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "Method1", ".method famorassem hidebysig newslot virtual instance System.Collections.Generic.List`1[T] Method1() cil managed"),
                Signature("Base2`1", "Method2", ".method famorassem hidebysig newslot abstract virtual instance System.Collections.Generic.List`1[T] Method2() cil managed"),
                Signature("Derived", "Method1", ".method famorassem hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method1() cil managed"),
                Signature("Derived", "Method2", ".method famorassem hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method2() cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestProtectedMethods()
        {
            // Tests:
            // protected virtual / abstract methods should not be marked with strict modifier

            var source = @"
using System;
using System.Collections.Generic;

abstract class Base<T>
{
    protected virtual List<T> Method1() { return null; }
}

abstract class Base2<T> : Base<T>
{
    protected abstract List<T> Method2();
}

class Derived : Base2<int>
{
    protected sealed override List<int> Method1(){ return null; }
    protected sealed override List<int> Method2(){ return null; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "Method1", ".method family hidebysig newslot virtual instance System.Collections.Generic.List`1[T] Method1() cil managed"),
                Signature("Base2`1", "Method2", ".method family hidebysig newslot abstract virtual instance System.Collections.Generic.List`1[T] Method2() cil managed"),
                Signature("Derived", "Method1", ".method family hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method1() cil managed"),
                Signature("Derived", "Method2", ".method family hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method2() cil managed")
            });
        }
        [Fact]

        private void TestPublicMethods()
        {
            // Tests:
            // public virtual / abstract methods should not be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public virtual List<T> Method1() { return null; }
}

abstract class Base2<T> : Base<T>
{
    public abstract List<T> Method2();
}

class Derived : Base2<int>
{
    public sealed override List<int> Method1(){ return null; }
    public sealed override List<int> Method2(){ return null; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "Method1", ".method public hidebysig newslot virtual instance System.Collections.Generic.List`1[T] Method1() cil managed"),
                Signature("Base2`1", "Method2", ".method public hidebysig newslot abstract virtual instance System.Collections.Generic.List`1[T] Method2() cil managed"),
                Signature("Derived", "Method1", ".method public hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method1() cil managed"),
                Signature("Derived", "Method2", ".method public hidebysig virtual final instance System.Collections.Generic.List`1[System.Int32] Method2() cil managed"),
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [WorkItem(540341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540341")]
        [Fact]

        private void TestInternalAccessors()
        {
            // Tests:
            // internal virtual / abstract accessors should be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public virtual List<T> Property1 { get { return null; } internal set { } }
    public virtual List<T> Property2 { set { } internal get { return null; } }
    internal abstract List<T> Property5 { get; set; }
}

abstract class Base2<T> : Base<T>
{
    public abstract List<T> Property3 { get; internal set; }
    public abstract List<T> Property4 { set; internal get; }
    internal virtual List<T> Property6 { get; set; }
}

class Derived : Base2<int>
{
    public sealed override List<int> Property1 { get { return null; } internal set { } }
    public sealed override List<int> Property2 { internal get { return null; } set { } }
    public sealed override List<int> Property3 { get { return null; } internal set { } }
    public sealed override List<int> Property4 { internal get { return null; } set { } }
    internal sealed override List<int> Property5 { get; set; }
    internal sealed override List<int> Property6 { get; set; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "get_Property1", ".method public hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property1() cil managed"),
                Signature("Base`1", "set_Property1", ".method assembly hidebysig newslot strict specialname virtual instance System.Void set_Property1(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property2", ".method assembly hidebysig newslot strict specialname virtual instance System.Collections.Generic.List`1[T] get_Property2() cil managed"),
                Signature("Base`1", "set_Property2", ".method public hidebysig newslot specialname virtual instance System.Void set_Property2(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property5", ".method assembly hidebysig newslot strict specialname abstract virtual instance System.Collections.Generic.List`1[T] get_Property5() cil managed"),
                Signature("Base`1", "set_Property5", ".method assembly hidebysig newslot strict specialname abstract virtual instance System.Void set_Property5(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base2`1", "get_Property3", ".method public hidebysig newslot specialname abstract virtual instance System.Collections.Generic.List`1[T] get_Property3() cil managed"),
                Signature("Base2`1", "set_Property3", ".method assembly hidebysig newslot strict specialname abstract virtual instance System.Void set_Property3(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base2`1", "get_Property4", ".method assembly hidebysig newslot strict specialname abstract virtual instance System.Collections.Generic.List`1[T] get_Property4() cil managed"),
                Signature("Base2`1", "set_Property4", ".method public hidebysig newslot specialname abstract virtual instance System.Void set_Property4(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base2`1", "get_Property6", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig newslot strict specialname virtual instance System.Collections.Generic.List`1[T] get_Property6() cil managed"),
                Signature("Base2`1", "set_Property6", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig newslot strict specialname virtual instance System.Void set_Property6(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived", "set_Property1", ".method assembly hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property2", ".method assembly hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property2() cil managed"),
                Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual final instance System.Void set_Property2(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property3", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property3() cil managed"),
                Signature("Derived", "set_Property3", ".method assembly hidebysig specialname virtual final instance System.Void set_Property3(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property4", ".method assembly hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property4() cil managed"),
                Signature("Derived", "set_Property4", ".method public hidebysig specialname virtual final instance System.Void set_Property4(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property5() cil managed"),
                Signature("Derived", "set_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig specialname virtual final instance System.Void set_Property5(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property6", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property6() cil managed"),
                Signature("Derived", "set_Property6", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] assembly hidebysig specialname virtual final instance System.Void set_Property6(System.Collections.Generic.List`1[System.Int32] value) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestProtectedInternalAccessors()
        {
            // Tests:
            // protected internal virtual / abstract accessors should not be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public virtual List<T> Property1 { get { return null; } protected internal set { } }
    public virtual List<T> Property2 { set { } protected internal get { return null; } }
    protected internal virtual List<T> Property5 { get; set; }
}

class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } protected internal set { } }
    public sealed override List<int> Property2 { protected internal get { return null; } set { } }
    protected internal sealed override List<int> Property5 { get; set; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "get_Property1", ".method public hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property1() cil managed"),
                Signature("Base`1", "set_Property1", ".method famorassem hidebysig newslot specialname virtual instance System.Void set_Property1(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property2", ".method famorassem hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property2() cil managed"),
                Signature("Base`1", "set_Property2", ".method public hidebysig newslot specialname virtual instance System.Void set_Property2(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] famorassem hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property5() cil managed"),
                Signature("Base`1", "set_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] famorassem hidebysig newslot specialname virtual instance System.Void set_Property5(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived", "set_Property1", ".method famorassem hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property2", ".method famorassem hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property2() cil managed"),
                Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual final instance System.Void set_Property2(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] famorassem hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property5() cil managed"),
                Signature("Derived", "set_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] famorassem hidebysig specialname virtual final instance System.Void set_Property5(System.Collections.Generic.List`1[System.Int32] value) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestProtectedInternalAccessorsInDifferentAssembly()
        {
            var source1 = @"
using System.Collections.Generic;
 
public class Base<T>
{
    public virtual List<T> Property1 { get { return null; } protected internal set { } }
    public virtual List<T> Property2 { protected internal get { return null; } set { } }
}";
            var compilation1 = CreateCompilation(source1);

            var source2 = @"
using System.Collections.Generic;
    
public class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } protected set { } }
    public sealed override List<int> Property2 { protected get { return null; } set { } }
}

// Omitted Accessors
public class Derived2 : Base<int>
{
    public sealed override List<int> Property1 { protected set { } }
    public sealed override List<int> Property2 { protected get { return null; } }
}";
            var comp = CompileAndVerify(source2, new[] { new CSharpCompilationReference(compilation1) }, expectedSignatures: new[]
            {
                Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived", "set_Property1", ".method family hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property2", ".method family hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property2() cil managed"),
                Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual final instance System.Void set_Property2(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived2", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived2", "set_Property1", ".method family hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived2", "get_Property2", ".method family hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property2() cil managed"),
                Signature("Derived2", "set_Property2", ".method public hidebysig specialname virtual final instance System.Void set_Property2(System.Collections.Generic.List`1[System.Int32] value) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestProtectedAccessors()
        {
            // Tests:
            // protected virtual / abstract accessors should not be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public virtual List<T> Property1 { get { return null; } protected set { } }
    public virtual List<T> Property2 { set { } protected get { return null; } }
    protected abstract List<T> Property5 { get; set; }
}

class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } protected set { } }
    public sealed override List<int> Property2 { protected get { return null; } set { } }
    protected sealed override List<int> Property5 { get; set; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "get_Property1", ".method public hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property1() cil managed"),
                Signature("Base`1", "set_Property1", ".method family hidebysig newslot specialname virtual instance System.Void set_Property1(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property2", ".method family hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property2() cil managed"),
                Signature("Base`1", "set_Property2", ".method public hidebysig newslot specialname virtual instance System.Void set_Property2(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property5", ".method family hidebysig newslot specialname abstract virtual instance System.Collections.Generic.List`1[T] get_Property5() cil managed"),
                Signature("Base`1", "set_Property5", ".method family hidebysig newslot specialname abstract virtual instance System.Void set_Property5(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived", "set_Property1", ".method family hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property2", ".method family hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property2() cil managed"),
                Signature("Derived", "set_Property2", ".method public hidebysig specialname virtual final instance System.Void set_Property2(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] family hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property5() cil managed"),
                Signature("Derived", "set_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] family hidebysig specialname virtual final instance System.Void set_Property5(System.Collections.Generic.List`1[System.Int32] value) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestPublicAccessors()
        {
            // Tests:
            // public virtual / abstract accessors should not be marked with strict modifier

            var source = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public virtual List<T> Property1 { get { return null; } set { } }
    public abstract List<T> Property5 { get; set; }
}

class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } set { } }
    public sealed override List<int> Property5 { get; set; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Base`1", "get_Property1", ".method public hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property1() cil managed"),
                Signature("Base`1", "set_Property1", ".method public hidebysig newslot specialname virtual instance System.Void set_Property1(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Base`1", "get_Property5", ".method public hidebysig newslot specialname abstract virtual instance System.Collections.Generic.List`1[T] get_Property5() cil managed"),
                Signature("Base`1", "set_Property5", ".method public hidebysig newslot specialname abstract virtual instance System.Void set_Property5(System.Collections.Generic.List`1[T] value) cil managed"),
                Signature("Derived", "get_Property1", ".method public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property1() cil managed"),
                Signature("Derived", "set_Property1", ".method public hidebysig specialname virtual final instance System.Void set_Property1(System.Collections.Generic.List`1[System.Int32] value) cil managed"),
                Signature("Derived", "get_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname virtual final instance System.Collections.Generic.List`1[System.Int32] get_Property5() cil managed"),
                Signature("Derived", "set_Property5", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname virtual final instance System.Void set_Property5(System.Collections.Generic.List`1[System.Int32] value) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestOverrideOverloadedMethod()
        {
            var source = @"
using System.Collections.Generic;
public abstract class Base<T>
{
    public abstract void Method(T x);
    public virtual void Method(List<T> x, int y) { }
}

abstract class Base2<T> : Base<T>
{
    public abstract void Method<U>(T x);
    public abstract int Method(List<T> x, long y);
}

class Derived : Base2<int>
{
    public override void Method(int x) { }
    public override void Method<T>(int x) { }
    public override void Method(List<int> x, int y) { }
    public override int Method(List<int> x, long y) { return 0; }
}";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Derived", "Method", ".method public hidebysig virtual instance System.Int32 Method(System.Collections.Generic.List`1[System.Int32] x, System.Int64 y) cil managed"),
                Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method(System.Collections.Generic.List`1[System.Int32] x, System.Int32 y) cil managed"),
                Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method(System.Int32 x) cil managed"),
                Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method<T>(System.Int32 x) cil managed")
            });

            comp.VerifyDiagnostics(); // No errors
        }
        [Fact]

        private void TestOverrideHidingMember()
        {
            // Tests:
            // Hide base virtual member with a virtual new / abstract new member 
            // Test that we don't override the hidden base member on further derived classes

            var source = @"
using System;
using System.Collections.Generic;
public abstract class Base<T>
{
    public virtual void Method<K>(T x) { Console.WriteLine(""Base<T>.Method<K>(T x)""); }
    public virtual void Method(List<T> x, int y) { Console.WriteLine(""Base<T>.Method(List<T> x, int y)""); }
    public virtual List<T> Property { set { Console.WriteLine(""Base<T>.set_Property""); } }
}

abstract class Base2<T> : Base<T>
{
    public new virtual void Method<U>(T x) { Console.WriteLine(""Base2<T>.Method<U>(T x)""); }
    public new virtual void Method(List<T> x, int y) { Console.WriteLine(""Base2<T>.Method(List<T> x, int y)""); }
    public new virtual List<T> Property
    {
        get { Console.WriteLine(""Base2<T>.get_Property""); return null; }
        set { Console.WriteLine(""Base2<T>.set_Property""); }
    }
}

class Derived : Base2<int>
{
    public override void Method<T>(int x) { Console.WriteLine(""Derived.Method<T>(int x)""); }
    public override void Method(List<int> x, int y) { Console.WriteLine(""Derived.Method(List<int> x, int y)""); }
    public override List<int> Property { set { Console.WriteLine(""Derived.set_Property""); } }
}

class Test
{
    public static void Main()
    {
        Base<int> b = new Derived();
        Base2<int> b2 = new Derived();

        b.Method<long>(1);
        b.Method(new List<int>(), 1);
        List<int> x = null;
        b.Property = x;
        
        b2.Method<long>(1);
        b2.Method(new List<int>(), 1);
        x = b2.Property;
        b2.Property = x;
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base<T>.Method<K>(T x)
Base<T>.Method(List<T> x, int y)
Base<T>.set_Property
Derived.Method<T>(int x)
Derived.Method(List<int> x, int y)
Base2<T>.get_Property
Derived.set_Property",
                expectedSignatures: new[]
                {
                    Signature("Base2`1", "Method", ".method public hidebysig newslot virtual instance System.Void Method(System.Collections.Generic.List`1[T] x, System.Int32 y) cil managed"),
                    Signature("Base2`1", "Method", ".method public hidebysig newslot virtual instance System.Void Method<U>(T x) cil managed"),
                    Signature("Base2`1", "get_Property", ".method public hidebysig newslot specialname virtual instance System.Collections.Generic.List`1[T] get_Property() cil managed"),
                    Signature("Base2`1", "set_Property", ".method public hidebysig newslot specialname virtual instance System.Void set_Property(System.Collections.Generic.List`1[T] value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method(System.Collections.Generic.List`1[System.Int32] x, System.Int32 y) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method<T>(System.Int32 x) cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname virtual instance System.Void set_Property(System.Collections.Generic.List`1[System.Int32] value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [WorkItem(528172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528172")]
        [Fact]
        public void TestHideWithInaccessibleVirtualMember()
        {
            // Tests:
            // Hide public base member with inaccessible internal virtual derived member
            // On further derived try to override public base member – this should result in PEVerify failure

            var source = @"
using System;
using System.Collections.Generic;
public abstract class Base<T>
{
    public virtual void Method<K>(T x) { Console.WriteLine(""Base<T>.Method<K>(T x)""); }
    public virtual void Method(List<T> x, int y) { Console.WriteLine(""Base<T>.Method(List<T> x, int y)""); }
    public virtual List<T> Property { set { Console.WriteLine(""Base<T>.set_Property""); } }
}

public abstract class Base2<T> : Base<T>
{
    internal new virtual void Method<U>(T x) { Console.WriteLine(""Base2<T>.Method<U>(T x)""); }
    internal new virtual void Method(List<T> x, int y) { Console.WriteLine(""Base2<T>.Method(List<T> x, int y)""); }
    internal new virtual List<T> Property
    {
        get { Console.WriteLine(""Base2<T>.get_Property""); return null; }
        set { Console.WriteLine(""Base2<T>.set_Property""); }
    }
}

class DerivedTest : Base2<int>
{
    internal override void Method<U>(int x) { Console.WriteLine(""DerivedTest.Method<U>(T x)""); }
    internal override void Method(List<int> x, int y) { Console.WriteLine(""DerivedTest.Method(List<int> x, int y)""); }
    internal override List<int> Property
    {
        get { Console.WriteLine(""DerivedTest.get_Property""); return null; }
        set { Console.WriteLine(""DerivedTest.set_Property""); }
    }
}";

            var source2 = @"
using System;
using System.Collections.Generic;
public class Derived : Base2<int>
{
    public override void Method<T>(int x) { Console.WriteLine(""Derived.Method<T>(int x)""); }
    public override void Method(List<int> x, int y) { Console.WriteLine(""Derived.Method(List<int> x, int y)""); }
    public override List<int> Property { set { Console.WriteLine(""Derived.set_Property""); } }
}

public class Test
{
    public static void Main()
    {
        Base<int> b = new Derived();
        Base2<int> b2 = new Derived();

        b.Method<long>(1);
        b.Method(new List<int>(), 1);
        List<int> x = null;
        b.Property = x;
        
        b2.Method<long>(1);
        b2.Method(new List<int>(), 1);
        b2.Property = x;
    }
}";

            var referencedCompilation =
                CreateCompilation(source,
                    options: TestOptions.ReleaseDll,
                    assemblyName: "OHI_CodeGen_TestHideWithInaccessibleVirtualMember1");

            var outerCompilation =
                CreateCompilation(source2,
                    new[] { new CSharpCompilationReference(referencedCompilation) },
                    options: TestOptions.ReleaseExe,
                    assemblyName: "OHI_CodeGen_TestHideWithInaccessibleVirtualMember2");

            outerCompilation.VerifyDiagnostics(); // No errors

            // Verify that PEVerify will fail despite the fact that compiler produces no errors
            // This is consistent with Dev10 behavior
            //
            // Dev10 PEVerify failure:
            // [token  0x02000002] Type load failed.
            // [IL]: Error: [Dev10.exe : Test::Main][offset 0x00000001] Unable to resolve token.
            //
            // Dev10 Runtime Exception:
            // Unhandled Exception: System.TypeLoadException: Method 'Method' on type 'Derived'
            // from assembly 'Dev10, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
            // is overriding a method that is not visible from that assembly.

            CompileAndVerify(outerCompilation, verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (Base2<int> V_0, //b2
                System.Collections.Generic.List<int> V_1) //x
  IL_0000:  newobj     ""Derived..ctor()""
  IL_0005:  newobj     ""Derived..ctor()""
  IL_000a:  stloc.0
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   ""void Base<int>.Method<long>(int)""
  IL_0012:  dup
  IL_0013:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0018:  ldc.i4.1
  IL_0019:  callvirt   ""void Base<int>.Method(System.Collections.Generic.List<int>, int)""
  IL_001e:  ldnull
  IL_001f:  stloc.1
  IL_0020:  ldloc.1
  IL_0021:  callvirt   ""void Base<int>.Property.set""
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.1
  IL_0028:  callvirt   ""void Base<int>.Method<long>(int)""
  IL_002d:  ldloc.0
  IL_002e:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0033:  ldc.i4.1
  IL_0034:  callvirt   ""void Base<int>.Method(System.Collections.Generic.List<int>, int)""
  IL_0039:  ldloc.0
  IL_003a:  ldloc.1
  IL_003b:  callvirt   ""void Base<int>.Property.set""
  IL_0040:  ret
}
");
        }

        [Fact]
        public void TestHideWithInaccessibleMember()
        {
            // Tests:
            // Hide public base member with inaccessible (internal) derived member
            // In further derived class override / invoke public base member – try hiding with static and instance members

            var source = @"
using System;
using System.Collections.Generic;
public abstract class Base<T>
{
    public virtual void Method<K>(T x) { Console.WriteLine(""Base<T>.Method<K>(T x)""); }
    public virtual void Method(List<T> x, int y) { Console.WriteLine(""Base<T>.Method(List<T> x, int y)""); }
    public virtual List<T> Property { set { Console.WriteLine(""Base<T>.set_Property""); } }
}

public class Base1<T> : Base<T>
{
}

public abstract class Base2<T> : Base1<T>
{
    private new void Method<U>(T x) { base.Method<U>(x); Console.WriteLine(""Base2<T>.Method<U>(T x)""); }
    internal new static void Method(List<T> x, int y) { Console.WriteLine(""Base2<T>.Method(List<T> x, int y)""); }
    internal new List<T> Property
    {
        get { Console.WriteLine(""Base2<T>.get_Property""); return null; }
        set { base.Property = value; Console.WriteLine(""Base2<T>.set_Property""); }
    }
}";

            var source2 = @"
using System;
using System.Collections.Generic;
public class Derived : Base2<int>
{
    public override void Method<T>(int x) { base.Method<T>(x); Console.WriteLine(""Derived.Method<T>(int x)""); }
    public override void Method(List<int> x, int y) { base.Method(x, y); Console.WriteLine(""Derived.Method(List<int> x, int y)""); }
    public override List<int> Property { set { base.Property = value; Console.WriteLine(""Derived.set_Property""); } }
}

public class Test
{
    public static void Main()
    {
        Base<int> b = new Derived();
        Base2<int> b2 = new Derived();

        b.Method<long>(1);
        b.Method(new List<int>(), 1);
        List<int> x = null;
        b.Property = x;
        
        b2.Method<long>(1);
        b2.Method(new List<int>(), 1);
        b2.Property = x;
    }
}";

            var referencedCompilation = CreateCompilation(source, assemblyName: "OHI_CodeGen_TestHideWithInaccessibleMember");

            var comp = CompileAndVerify(
                source2,
                new[] { referencedCompilation.EmitToImageReference() },
                expectedOutput: @"
Base<T>.Method<K>(T x)
Derived.Method<T>(int x)
Base<T>.Method(List<T> x, int y)
Derived.Method(List<int> x, int y)
Base<T>.set_Property
Derived.set_Property
Base<T>.Method<K>(T x)
Derived.Method<T>(int x)
Base<T>.Method(List<T> x, int y)
Derived.Method(List<int> x, int y)
Base<T>.set_Property
Derived.set_Property",
                expectedSignatures: new[]
                {
                    Signature("Base2`1", "Method", ".method assembly hidebysig static System.Void Method(System.Collections.Generic.List`1[T] x, System.Int32 y) cil managed"),
                    Signature("Base2`1", "Method", ".method private hidebysig instance System.Void Method<U>(T x) cil managed"),
                    Signature("Base2`1", "set_Property", ".method assembly hidebysig specialname instance System.Void set_Property(System.Collections.Generic.List`1[T] value) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method(System.Collections.Generic.List`1[System.Int32] x, System.Int32 y) cil managed"),
                    Signature("Derived", "Method", ".method public hidebysig virtual instance System.Void Method<T>(System.Int32 x) cil managed"),
                    Signature("Derived", "set_Property", ".method public hidebysig specialname virtual instance System.Void set_Property(System.Collections.Generic.List`1[System.Int32] value) cil managed")
                });

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestHideSealedMember()
        {
            // Tests:
            // Hide sealed member with virtual / abstract member – override this member in further derived class

            var source = @"
using System;
using System.Collections.Generic;
public abstract class Base<T>
{
    protected virtual void Method<K>(T x) { Console.WriteLine(""Base<T>.Method<K>(T x)""); }
    protected virtual void Method(List<T> x, int y) { Console.WriteLine(""Base<T>.Method(List<T> x, int y)""); }
    protected virtual List<T> Property
    {
        get { Console.WriteLine(""Base<T>.get_Property""); return null;} 
        set { Console.WriteLine(""Base<T>.set_Property""); } 
    }

    public void Test(Base<int> d)
    {
        Base<int> b = d;
        Base2<int> b2 = (Base2<int>)d;

        b.Method<long>(1);
        b.Method(new List<int>(), 1);
        List<int> x = null;
        b.Property = x;
        x = b.Property;
        
        b2.Method<long>(1);
        b2.Method(new List<int>(), 1);
        b2.Property = x;
        x = b2.Property;
    }
}

public class Base1<T> : Base<T>
{
    protected sealed override void Method<U>(T x) { Console.WriteLine(""Base1<T>.Method<U>(T x)""); }
    protected sealed override void Method(List<T> x, int y) { Console.WriteLine(""Base1<T>.Method(List<T> x, int y)""); }
    protected sealed override List<T> Property { set { Console.WriteLine(""Base1<T>.set_Property""); } }
}

public abstract class Base2<T> : Base1<T>
{
    protected internal new virtual void Method<U>(T x) { Console.WriteLine(""Base2<T>.Method<U>(T x)""); }
    protected internal new abstract void Method(List<T> x, int y);
    protected internal new virtual List<T> Property
    {
        set { Console.WriteLine(""Base2<T>.set_Property""); }
        get { Console.WriteLine(""Base2<T>.get_Property""); return null; }
    }
}";

            var source2 = @"
using System;
using System.Collections.Generic;
public class Derived : Base2<int>
{
    protected override void Method<T>(int x) { Console.WriteLine(""Derived.Method<T>(int x)""); }
    protected override void Method(List<int> x, int y) { Console.WriteLine(""Derived.Method(List<int> x, int y)""); }
    protected override List<int> Property { get { Console.WriteLine(""Derived.get_Property""); return null; } }
}

public class Test
{
    public static void Main()
    {
        Base<int> b = new Derived();
        b.Test(b);
    }
}";

            var referencedCompilation = CreateCompilation(source, assemblyName: "OHI_CodeGen_TestHideSealedMember");

            var comp = CompileAndVerify(
                source2,
                new[] { referencedCompilation.EmitToImageReference() },
                expectedOutput: @"
Base1<T>.Method<U>(T x)
Base1<T>.Method(List<T> x, int y)
Base1<T>.set_Property
Base<T>.get_Property
Derived.Method<T>(int x)
Derived.Method(List<int> x, int y)
Base2<T>.set_Property
Derived.get_Property");

            comp.VerifyDiagnostics(); // No errors
        }

        [WorkItem(540431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540431")]
        [Fact]
        public void TestOverrideNewToVBVirtualOverloadsMetadata()
        {
            #region "Impl"
            var text1 = @"using System;
public class CSIMeth02Derived : VBIMeth02Impl // VB Impl
{
    public override void Sub01(params byte[] ary) // base:virtual
    {
        Console.Write(""CSS1_OV "");
    }
    public new void Sub011(sbyte p1, params byte[] ary) // base:overloads
    {
        Console.Write(""CSS11_New "");
    }
    public new string Func01(params string[] ary) // base:virtual
    {
        Console.Write(""CSF1_New "");
        return ary[0];
    }
    public string Func011(object p1, params string[] ary) // base:overloads- warning CS108
    {
        Console.Write(""CSF11_Warn "");
        return p1.ToString();
    }
}
";
            #endregion

            var text2 = @"
class Test
{
    static void Main()
    {
        CSIMeth02Derived obj = new CSIMeth02Derived();
        obj.Sub01(1, 2, 3);
        ((VBIMeth02Impl)obj).Sub01(1, 2, 3);
        ((IMeth02)obj).Sub01(1, 2, 3);
        ((IMeth01)obj).Sub01(1, 2, 3);

        obj.Func01(""1"", ""2"", ""3"");
        ((VBIMeth02Impl)obj).Func01(""1"", ""2"", ""3"");
        ((IMeth02)obj).Func01(""1"", ""2"", ""3"");
        ((IMeth01)obj).Func01(""1"", ""2"", ""3"");
    }
}
";
            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses01;
            var refs = new System.Collections.Generic.List<MetadataReference>() { asm01, asm02 };

            var comp1 = CreateCompilation(text1, references: refs, assemblyName: "OHI_DeriveOverrideNewVirtualOverload001",
                            options: TestOptions.ReleaseDll);
            refs.Add(new CSharpCompilationReference(comp1));

            var comp = CreateCompilation(text2, references: refs, assemblyName: "OHI_DeriveOverrideNewVirtualOverload002",
                        options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"CSS1_OV CSS1_OV VBS11_OL CSS1_OV CSF1_New VBF1_V VBF11 VBF1_V");
        }

        [WorkItem(540431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540431")]
        [Fact]
        public void TestOverrideNewToVBVirtualPropMetadata()
        {
            #region "Impl"
            var text1 = @"using System;
public class CSIPropImplDerived : VBIPropImpl
{
    private string _str;
    private uint _uint;

    //public string ReadOnlyProp
    //{
    //    get { return _str; }
    //}

    public new string WriteOnlyProp
    {
        set { _str = value; }
    }

    public override string NormalProp // base:virtual
    {
        // get { return _str; }
        set { _str = value; }
    }

    public override uint get_ReadOnlyPropWithParams(ushort x) // base:virtual
    {
        return _uint;
    }

    public override void set_WriteOnlyPropWithParams(uint x, ulong y) // base:virtual
    {
        _uint = x;
    }

    public new long get_NormalPropWithParams(long x, short y) // base:virtual
    {
        return (long)_uint;
    }

    public new void set_NormalPropWithParams(long x, short y, long z) // base:virtual
    {
        _uint = (uint)x;
    }
}
";
            #endregion

            var text2 = @"using System;
class Test
{
    static void Main()
    {
        CSIPropImplDerived obj = new CSIPropImplDerived();

        obj.NormalProp = ""CSNormProp "";
        ((VBIPropImpl)obj).NormalProp = ""VBNormProp "";
        Console.Write(obj.NormalProp);
        Console.Write(((VBIPropImpl)obj).NormalProp);

        obj.WriteOnlyProp = ""CSWriteReadOnly "";
        ((VBIPropImpl)obj).WriteOnlyProp = ""VBWriteReadOnly "";
        Console.Write(obj.ReadOnlyProp);
        Console.Write(((VBIPropImpl)obj).ReadOnlyProp);

        obj.set_NormalPropWithParams(100, 2, 3);
        ((VBIPropImpl)obj).set_NormalPropWithParams(200, 200, 5);
        Console.Write(obj.get_NormalPropWithParams(300, 6));
        Console.Write(((VBIPropImpl)obj).get_NormalPropWithParams(400, 7));

        ((VBIPropImpl)obj).set_WriteOnlyPropWithParams(800, 0);
        obj.set_WriteOnlyPropWithParams(900, 0);
        Console.Write(obj.get_ReadOnlyPropWithParams(0));
        Console.Write(((VBIPropImpl)obj).get_ReadOnlyPropWithParams(1));
    }
}
";
            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses01;
            var refs = new System.Collections.Generic.List<MetadataReference>() { asm01, asm02 };

            var comp1 = CreateCompilation(text1, references: refs, assemblyName: "OHI_DeriveOverrideVirtualProp001",
                            options: TestOptions.ReleaseDll);
            refs.Add(new CSharpCompilationReference(comp1));

            var comp = CreateCompilation(text2, references: refs, assemblyName: "OHI_DeriveOverrideVirtualProp002",
                        options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"VBDefault VBDefault VBWriteReadOnly VBWriteReadOnly 100200900900");
        }

        [Fact]
        public void TestDerivedImplPropWithBaseInMetadata()
        {
            var text1 = @"using System;
using Metadata;

    public class ICSPropDerived : ICSPropImpl, ICSProp
    {
        // no impl of ReadOnlyProp
        public new EFoo WriteOnlyProp
        {
            set { efoo = (EFoo)(value + 1); }
        }
        // override get only
        public override EFoo ReadWriteProp
        {
            get { return (EFoo)(efoo + 1); }
            // set { efoo = value; }
        }
    }
";

            var text2 = @"using System;
using Metadata;

class Test
{
    static void Main()
    {
        ICSPropDerived pobj = new ICSPropDerived();
        ICSPropImpl pbaseobj = pobj;

        pobj.WriteOnlyProp = EFoo.One; // new in derived
        Console.Write(pobj.ReadOnlyProp); // call base

        pbaseobj.WriteOnlyProp = EFoo.Two; // call base
        Console.Write(pbaseobj.ReadWriteProp); // call derived

        pobj.ReadWriteProp = EFoo.Zero; // call base
        Console.Write(pobj.ReadWriteProp); // call derived

        pbaseobj.ReadWriteProp = EFoo.Zero; // call base
        Console.Write(pobj.ReadOnlyProp); // call base
    }
}
";

            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.CSInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.CSClasses01;

            var comp1 = CreateCompilation(
                text1,
                references: new[] { asm01, asm02 },
                assemblyName: "OHI_DeriveBaseInMetadataProp001");

            var comp2 = CreateCompilation(
                text2,
                references: new MetadataReference[] { asm01, asm02, new CSharpCompilationReference(comp1) },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_DeriveBaseInMetadataProp002");

            CompileAndVerify(comp2, expectedOutput: @"TwoThreeOneZero");
        }

        [WorkItem(540452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540452")]
        [WorkItem(540453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540453")]
        [Fact]
        public void TestGenericDDerivedImplWithBaseInMetadata()
        {
            #region "Text1"

            var text1 = @"using System;
namespace Metadata
{
    // base class ICSGenImpl<T, string> does NOT impl interface directly
    abstract public class ICSGenDerived<T> : ICSGenImpl<T, string>, ICSGen<T, string>
    {
        // base: virtual
        public override sealed void M01(T p1, T p2)
        {
            Console.Write(""DOvSe_TT "");
        }
        // base: abstract
        public override void M01(T p1, ref T p2, out DFoo<T> p3)
        {
            p3 = null;
            Console.Write(""DOv_RefOutT "");
        }
        // base: no virtual
        public new virtual string M01(string p1, string p2)
        {
            Console.Write(""DNewv_VV "");
            return p1.ToString();
        }
        // base: virtual
        public override string M01(string p1, object p2)
        {
            Console.Write(""DOv_VObj "");
            return p1;
        }
        // base: virtual
        public abstract override string M01(string p1, params object[] p2);
    }
}
";

            #endregion

            #region "Text2"
            var text2 = @"using System;
namespace Metadata
{
     public class ICSGenDerivedDerived<T> : ICSGenDerived<T>, ICSGen<T, string>
    {
        public new void M01(T p1, params T[] ary)
        {
            Console.Write(""DDNew_TParams "");
        }

        public override void M01(params T[] ary)
        {
            Console.Write(""DDOv_ParamsT "");
        }

        public new void M01(T p1, ref T p2, out DFoo<T> p3)
        {
            p3 = null;
            Console.Write(""DDNew_RefOutT "");
        }

        public override string M01(string p1, string p2)
        {
            Console.Write(""DDOv_VV "");
            return p1.ToString();
        }

        public override sealed string M01(string p1, object p2)
        {
            Console.Write(""DDOvSe_VObj "");
            return p1;
        }

        public override sealed string M01(string p1, params object[] p2)
        {
            Console.Write(""DDOvSe_VParams "");
            return p1;
        }
    }
}
";

            #endregion

            #region "Text3"
            var text3 = @"using System;
using Metadata;

class Test
{
    static void Main()
    {
            ICSGenDerivedDerived<short> ddobj = new ICSGenDerivedDerived<short>();
            ICSGenDerived<short> dobj = ddobj;
            ICSGenImpl<short, string> obj = dobj;
            ICSGen<short, string> iobj = dobj;

            sbyte sb = -128;
            short sh = 12345;
            DFoo<short> dfoo = null;
            // (base)virtual (D)override seal (DD) -
            obj.M01(sb, sh);
            iobj.M01(sb, sh);
            Console.WriteLine();

            // (base)virtual (D)_ (DD) new
            iobj.M01(3, sb, sh); // DD
            ddobj.M01(sh, sb, 1); // DD
            dobj.M01(sh, sb, 1); // b
            obj.M01(sb, 2, sh); // b
            Console.WriteLine();

            // (base)virtual (D)_ (DD) override
            short[] ary = new short[] { 1, 2, 3 };
            // all DD
            iobj.M01(new short[] { 1, 2 });
            ddobj.M01(ary);
            dobj.M01(new short[] { 1 });
            obj.M01(ary);
            Console.WriteLine();

            // (base)abstract (D) override (DD) new
            iobj.M01(-128, ref sh, out dfoo);
            ddobj.M01(sb, ref sh, out dfoo);
            dobj.M01(sh, ref sh, out dfoo); // Roslyn (40,38): error CS1620: Argument 3 must be passed with the 'ref' keyword
            obj.M01(127, ref sh, out dfoo);
            Console.WriteLine();

            // (base)None (D) new virtual (DD) override 
            string str = ""Hi"";
            iobj.M01(str, ""Hey"");
            ddobj.M01(""Hey"", str);
            dobj.M01(str, str);
            obj.M01(str, ""Hey"");
            Console.WriteLine();

            // (base)virtual (D)override (DD) override_seal
            object oo = null;
            iobj.M01(str, oo);
            ddobj.M01(str, oo);
            dobj.M01(str, new object());
            obj.M01(str, oo);
            Console.WriteLine();

            // (base)virtual (D) abstract override (DD) override seal
            iobj.M01(str, str, null);
            ddobj.M01(null, str, null); // Roslyn (62,23): error CS1503: Argument 1: cannot convert from '<null>' to 'short etc.
            dobj.M01(str, null, str, null);  // Roslyn (63,13): error CS1501: No overload for method 'M01' takes 4 arguments
            obj.M01(null, null, str, str); 
    }
}
";

            #endregion

            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.CSInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.CSClasses01;
            var refs = new System.Collections.Generic.List<MetadataReference>() { asm01, asm02 };

            var comp1 = CreateCompilation(text1, references: refs, assemblyName: "OHI_GenericDDeriveBaseInMetadata001",
                            options: TestOptions.ReleaseDll);
            // better output with error info if any
            comp1.VerifyDiagnostics(); // No Errors

            refs.Add(new CSharpCompilationReference(comp1));

            var comp2 = CreateCompilation(text2, references: refs, assemblyName: "OHI_GenericDDeriveBaseInMetadata002",
                            options: TestOptions.ReleaseDll);
            Assert.Equal(0, comp2.GetDiagnostics().Count());
            refs.Add(new CSharpCompilationReference(comp2));

            var comp = CreateCompilation(text3, references: refs, assemblyName: "OHI_GenericDDeriveBaseInMetadata003",
                            options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(); // No Errors

            CompileAndVerify(comp, expectedOutput:
@"DOvSe_TT DOvSe_TT 
DDNew_TParams DDNew_TParams Base_TParamsT Base_TParamsT 
DDOv_ParamsT DDOv_ParamsT DDOv_ParamsT DDOv_ParamsT 
DDNew_RefOutT DDNew_RefOutT DOv_RefOutT DOv_RefOutT 
DDOv_VV DDOv_VV DDOv_VV BaseNV_VV 
DDOvSe_VObj DDOvSe_VObj DDOvSe_VObj DDOvSe_VObj 
DDOvSe_VParams DDOvSe_VParams DDOvSe_VParams DDOvSe_VParams ");
        }

        [Fact]
        public void TestBridgeMethodFromBaseVBMetadata()
        {
            var text1 = @"using System;
using Metadata;

//partial class Test
//{
    public class D : VBBase, IMeth03.INested
    {
        public override sealed void NestedSub(ushort p)
        {
            Console.Write(""Derived (OVSealed) "");
        }
    }
//}
";

            var text2 = @"
using System;
partial class Test
{
    static void Main()
    {
        new D().NestedSub(1);
        object o = new Test();
        Console.Write(new D().NestedFunc(ref o));
    }
}
";
            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses02;

            var comp = CreateCompilation(
                new string[] { text1, text2 },
                references: new[] { asm01, asm02 },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_BridgeMethodFromBaseVB007");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"Derived (OVSealed) VBaseFunc (Non-Virtual)",
                expectedSignatures: new[]
                {
                    Signature("D", "NestedSub", ".method public hidebysig virtual final instance System.Void NestedSub(System.UInt16 p) cil managed"),
                    Signature("D", "IMeth03.INested.NestedFunc", ".method private hidebysig newslot virtual final instance System.String IMeth03.INested.NestedFunc(System.Object& p) cil managed")
                });
        }

        [Fact]
        public void TestOverridingGenericNestedClasses()
        {
            // Tests:
            // Sanity check – use open (T) and closed (C<String>) generic types in the signature of overriding methods
            // Override members of generic base class nested inside other generic classes

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal abstract class Base<V, W>
        {
            protected internal abstract T Property { set; }
            internal abstract void Method<Z>(T A, U[] B, List<V> C, Dictionary<W, Z> D);
        }
    }
}
internal class Derived1<T, U> : Outer<T>.Inner<int>.Base<long, U>
{
    protected internal override T Property
    {
        set { Console.WriteLine(""Derived1.set_Property""); }
    }
    internal override void Method<K>(T a, int[] b, List<long> c, Dictionary<U, K> d)
    {
        Console.WriteLine(""Derived1.Method"");
    }
    internal class Derived2 : Outer<string>.Inner<int>.Base<long, string>
    {
        protected internal override string Property
        {
            set { Console.WriteLine(""Derived2.set_Property""); }
        }

        internal override void Method<K>(string a, int[] b, List<long> c, Dictionary<string, K> d)
        {
            Console.WriteLine(""Derived2.Method"");
        }
    }
}
public class Test
{
    public static void Main()
    {
        Outer<string>.Inner<int>.Base<long, string> b = new Derived1<string, string>();
        b.Property = """";
        b.Method<string>("""", new int[] { }, new List<long>(), new Dictionary<string, string>());

        b = new Derived1<string, string>.Derived2();
        b.Property = """";
        b.Method<string>("""", new int[] { }, new List<long>(), new Dictionary<string, string>());
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
                    Signature("Derived1`2", "set_Property", ".method famorassem hidebysig specialname virtual instance System.Void set_Property(T value) cil managed"),
                    Signature("Derived1`2", "Method", ".method assembly hidebysig strict virtual instance System.Void Method<K>(T a, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] c, System.Collections.Generic.Dictionary`2[U,K] d) cil managed"),
                    Signature("Derived1`2+Derived2", "set_Property", ".method famorassem hidebysig specialname virtual instance System.Void set_Property(System.String value) cil managed"),
                    Signature("Derived1`2+Derived2", "Method", ".method assembly hidebysig strict virtual instance System.Void Method<K>(System.String a, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] c, System.Collections.Generic.Dictionary`2[System.String,K] d) cil managed")
                });

            comp.VerifyDiagnostics(); // No Errors
        }

        [Fact]
        public void TestOverloadGetSetMethodWithPropMetadata()
        {
            #region "src"
            var text1 = @"using System;
public class CSPropBase : VBIPropImpl
{
    protected string _str;

    public virtual string get_ReadOnlyProp()
    {
        return _str;
    }

    public void set_WriteOnlyProp(string val)
    {
        _str = val + ""M |""; 
    }
}
";
            var text2 = @"using System;
public class CSPropDerived : CSPropBase
{
    public override string get_ReadOnlyProp()
    {
        return _str;
    }

    public new void set_WriteOnlyProp(string val)
    {
        _str = val + ""D |""; 
    }
}
";
            var text3 = @"using System;

class Test
{
    static void Main()
    {
        CSPropDerived obj = new CSPropDerived();
        CSPropBase bobj = obj;

        // call derived methods
        obj.set_WriteOnlyProp(""Derived "");
        Console.Write(obj.get_ReadOnlyProp());

        bobj.set_WriteOnlyProp(""Base "");
        Console.Write(bobj.get_ReadOnlyProp());

        // call interface prop impl
        obj.WriteOnlyProp = ""PropImpl"";
        Console.Write(obj.ReadOnlyProp);
    }
}
";
            #endregion

            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses01;

            var comp1 = CreateCompilation(
                text1,
                references: new MetadataReference[] { asm01, asm02 },
                assemblyName: "OHI_OverloadGetSetMethodWithProp001");

            var comp2 = CreateCompilation(
                text2,
                references: new MetadataReference[] { asm01, asm02, new CSharpCompilationReference(comp1) },
                assemblyName: "OHI_OverloadGetSetMethodWithProp002");

            var comp = CreateCompilation(
                text3,
                references: new MetadataReference[] { asm01, asm02, new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_OverloadGetSetMethodWithProp003");

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "Derived D |Base M |PropImpl");
        }

        [Fact]
        public void TestVBNestedClassesOverrideNewMetadata()
        {
            #region "Text1"
            var text1 = @"
public class CNested : IMeth03.Nested
{
    sbyte _sbyte = 1;
    public override sbyte ReadOnlySByte
    {
        get { return _sbyte; }
    }

    public new virtual sbyte WriteOnlySByte
    {
        set { _sbyte = value; }
    }

    public override sbyte PropSByte
    {
        get { return _sbyte; }
        set { _sbyte = value; }
    }
}
";
            #endregion

            #region "Text2"
            var text2 = @"using System;
public class CNestedDerived : CNested
{
    sbyte _sbyte = 2;
    public new sbyte ReadOnlySByte
    {
        get { return _sbyte; }
    }

    public override sbyte WriteOnlySByte
    {
        set { _sbyte = value; }
    }

    public override sealed sbyte PropSByte
    {
        get { return _sbyte; }
        set { _sbyte = value; }
    }
}
";
            #endregion

            #region "Text"
            var text = @"using System;

class Test
{
    static void Main()
    {
        CNestedDerived obj = new CNestedDerived();
        CNested bobj = obj;
        IMeth03.Nested vbobj = obj;

        obj.PropSByte = 123;
        Console.WriteLine(obj.ReadOnlySByte);
        obj.WriteOnlySByte = 124;
        Console.WriteLine(obj.PropSByte);

        bobj.PropSByte = 125;
        Console.WriteLine(bobj.ReadOnlySByte);
        bobj.WriteOnlySByte = 126;
        Console.WriteLine(bobj.PropSByte);

        vbobj.PropSByte = 127;
        Console.WriteLine(vbobj.ReadOnlySByte);
        vbobj.WriteOnlySByte = -128;
        Console.WriteLine(vbobj.PropSByte);
    }
}
";
            #endregion

            var asmfile = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;

            var comp1 = CreateCompilation(
                text1,
                references: new[] { asmfile },
                assemblyName: "OHI_ClassOverrideNewVBNested001");

            var comp2 = CreateCompilation(
                text2,
                references: new[] { asmfile, comp1.EmitToImageReference() },
                assemblyName: "OHI_ClassOverrideNewVBNested002");

            var comp = CreateCompilation(
                text,
                references: new MetadataReference[] { asmfile, new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_ClassOverrideNewVBNested003");

            CompileAndVerify(comp, expectedOutput:
@"123
124
1
126
1
127
");
        }

        /// <summary>
        /// Override generic method with different type parameter letter
        ///  - public virtual void Method&lt;TMethod&gt;(TOuter modopt(IsConst)[] modopt(IsConst) x, 
        ///                                        TInner modopt(IsConst)[] modopt(IsConst) y, 
        ///                                        TMethod modopt(IsConst)[] modopt(IsConst) z);
        /// </summary>
        [Fact]
        public void TestOverrideGenericMethodWithTypeParamDiffNameWithCustomModifiers()
        {
            var text = @"
namespace Metadata
{
    using System;
    public class GD : Outer<string>.Inner<ulong>
    {
        public override void Method<X>(string[] x, ulong[] y, X[] z) { Console.Write(""Hello {0}"", z.Length); }

        static void Main()
        {
            new GD().Method<byte>(null, null, new byte[] { 0, 127, 255 });
        }
    }
}
";
            var verifier = CompileAndVerify(
                text,
                new[] { TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll },
                expectedOutput: @"Hello 3",
                expectedSignatures: new[]
                {
                    // The ILDASM output is following, and Roslyn handles it correctly. 
                    // Verifier tool gives different output due to the limitation of Reflection
                    // @".method public hidebysig virtual instance System.Void Method<X>(" +
                    // @"System.String modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x," +
                    // @"UInt64 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) y," +
                    // @"!!X modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) z) cil managed")
                    Signature("Metadata.GD", "Method",
                              @".method public hidebysig virtual instance System.Void Method<X>(" +
                              @"modopt(System.Runtime.CompilerServices.IsConst) System.String[] x, " +
                              @"modopt(System.Runtime.CompilerServices.IsConst) System.UInt64[] y, modopt(System.Runtime.CompilerServices.IsConst) X[] z) cil managed"),
                });
        }

        [WorkItem(540516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540516")]
        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/12422")]
        public void TestCallMethodsWithLeastCustomModifiers()
        {
            var text = @"using Metadata;
public class Program
{
    public static void Main()
    {
        LeastModoptsWin obj = new LeastModoptsWin();
        // ok - 51
        System.Console.Write(obj.M(obj.GetByte(), obj.GetByte())); 
    }
}
";
            var verifier = CompileAndVerify(text,
                references: new[] { TestReferences.SymbolsTests.CustomModifiers.ModoptTests },
                expectedOutput: "51");
        }

        [WorkItem(540517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540517")]
        [Fact]
        public void TestOverrideMethodsWithCustomModifiers()
        {
            var text = @"using System;
using Metadata;

public class Derived : LeastModoptsWin
{
    public override sealed byte M(byte t, byte v) { return 88; } // W CS1957
}

public class Test
{
    static void Main()
    {
         LeastModoptsWin d = new Derived();
         Console.WriteLine(d.M(33, 44));
         Console.WriteLine(d.M(d.GetByte(), d.GetByte()));
    }
}
";
            var verifier = CompileAndVerify(text,
                references: new[] { TestReferences.SymbolsTests.CustomModifiers.ModoptTests },
                expectedOutput: @"88
88
",
                expectedSignatures: new[]
                {
                   Signature("Derived", "M", ".method public hidebysig virtual final instance modopt(System.Runtime.CompilerServices.IsConst) System.Byte M(System.Byte t, System.Byte v) cil managed")
                });

            var comp = (CSharpCompilation)verifier.Compilation;
            comp.VerifyDiagnostics();

            var baseType = comp.GlobalNamespace.GetMember<NamespaceSymbol>("Metadata").GetMember<NamedTypeSymbol>("LeastModoptsWin");
            var derivedType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");

            var overridingMethod = derivedType.GetMember<MethodSymbol>("M");
            var overriddenMethod = overridingMethod.OverriddenMethod;

            Assert.Equal("System.Byte modopt(System.Runtime.CompilerServices.IsConst) Metadata.LeastModoptsWin.M(System.Byte t, System.Byte v)",
                overriddenMethod.ToTestDisplayString());
        }

        [Fact]
        public void TestOverridingGenericClasses_HideTypeParameter()
        {
            // Tests:
            // Override generic methods on generic classes – test case where type parameter 
            // on method hides the type parameter on class (both in base type and in overriding type)

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal abstract class Base<V, W>
        {
            public virtual void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d) { Console.WriteLine(""Base.Method`1""); }
            internal abstract void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<long>.Inner<int>.Base<long, Y>
        {
            public sealed override void Method<X>(long A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
                Console.WriteLine(""Derived1.Method`1"");
            }
            internal sealed override void Method<X, Y>(long A, int[] b, List<X> C, Dictionary<Y, Y> d)
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
        Outer<long>.Inner<int>.Base<long, string> b = new Outer<long>.Inner<int>.Derived1<string, string>();
        b.Method<string>(1, new int[]{}, new List<long>(), new Dictionary<string, string>());
        b.Method<string, int>(1, new int[]{}, new List<string>(), new Dictionary<int, int>());
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived1.Method`1
Derived1.Method`2",
                expectedSignatures: new[]
                {
                    Signature("Outer`1+Inner`1+Derived1`2", "Method", ".method public hidebysig virtual final instance System.Void Method<X>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[Y,X] d) cil managed"),
                    Signature("Outer`1+Inner`1+Derived1`2", "Method", ".method assembly hidebysig virtual final instance System.Void Method<X, Y>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[X] C, System.Collections.Generic.Dictionary`2[Y,Y] d) cil managed")
                });

            comp.VerifyDiagnostics(
                // (11,43): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Base<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Base<V, W>"),
                // (11,46): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Base<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Base<V, W>"),
                // (15,41): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,43): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,46): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"));
        }
        [Fact]

        private void TestHideMethodWithModreqCustomModifiers()
        {
            var text = @"using System;
using Metadata;

internal class D1 : Modreq
{
    public void M(uint x) { Console.Write(x + 1); } // silently hide base class member
}

internal class D2 : Modreq
{
    public new void M(uint x) { Console.Write(x + 2); } // Dev10 Warning CS0109
}

class Test
{
    static void Main()
    {
        new D1().M(10);
        new D2().M(20);
    }
}
";
            //var errs = comp.GetDiagnostics();
            //Assert.Equal(1, errs.Count());
            //Assert.Equal(109, errs.First().Code);

            var verifier = CompileAndVerify(text,
                references: new[] { TestReferences.SymbolsTests.CustomModifiers.ModoptTests },
                expectedOutput: "1122",
                expectedSignatures: new[]
                {
                    Signature("D1", "M", @".method public hidebysig instance System.Void M(System.UInt32 x) cil managed"),
                    Signature("D2", "M", @".method public hidebysig instance System.Void M(System.UInt32 x) cil managed")
                });
        }

        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void AccessorMethodAccessorOverridingExecution()
        {
            var text = @"
public class A
{
    protected string _p;
    virtual public string P
    {
        get { return _p; }
        set { _p = ""A"" + value; }
    }
}
public class B : A
{
    virtual public void set_P(string value) { _p = ""B"" + value; }
}
public class C : B
{
    public override string P
    {
        get { return _p; }
        set { _p = ""C"" + value; }
    }
}
class Program
{
    static void Main(string[] args)
    {
        C c = new C(); c.P = ""1"";
        System.Console.WriteLine(c.P);
    }
}
";

            CompileAndVerify(text, expectedOutput: "C1");
        }

        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void AccessorMethodAccessorOverridingRoundTrip()
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
            Action<ModuleSymbol> validator = module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classA = globalNamespace.GetMember<NamedTypeSymbol>("A");
                var classB = globalNamespace.GetMember<NamedTypeSymbol>("B");
                var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");

                var methodA = classA.GetMember<PropertySymbol>("P").GetMethod;
                var methodB = classB.GetMember<MethodSymbol>("get_P");
                var methodC = classC.GetMember<PropertySymbol>("P").GetMethod;

                Assert.True(methodA.IsVirtual);
                Assert.True(methodB.IsVirtual);
                Assert.True(methodC.IsOverride);

                Assert.Null(methodA.OverriddenMethod);
                Assert.Null(methodB.OverriddenMethod);
                Assert.Equal(methodA, methodC.OverriddenMethod);
            };

            CompileAndVerify(text, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void MethodAccessorMethodOverridingRoundTrip()
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
            Action<ModuleSymbol> validator = module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classA = globalNamespace.GetMember<NamedTypeSymbol>("A");
                var classB = globalNamespace.GetMember<NamedTypeSymbol>("B");
                var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");

                var methodA = classA.GetMember<MethodSymbol>("get_P");
                var methodB = classB.GetMember<PropertySymbol>("P").GetMethod;
                var methodC = classC.GetMember<MethodSymbol>("get_P");

                Assert.True(methodA.IsVirtual);
                Assert.True(methodB.IsVirtual);
                Assert.True(methodC.IsOverride);

                Assert.Null(methodA.OverriddenMethod);
                Assert.Null(methodB.OverriddenMethod);
                Assert.Equal(methodA, methodC.OverriddenMethod);
            };

            CompileAndVerify(text, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        /// <summary>
        /// If a PEMethodSymbol is metadata virtual and explicitly
        /// overrides another method, then it will return true for
        /// IsOverride, even if the method is not considered to be
        /// overridden by C# semantics.  In such cases, IsOverride
        /// will be true, but OverriddenMethod will return null.
        /// This test just checks that nothing blows up in such cases.
        /// </summary>
        [WorkItem(541834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541834")]
        [Fact]
        public void ExplicitOverrideWithoutCSharpOverride()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Foo() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class Base

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig newslot virtual 
          instance void  Bar() cil managed
  {
    .override Base::Foo //different name
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var cSharpSource = @"
public class Override : Derived
{
    public override void Bar() { }
}

public class Invoke
{
    public void Test(Derived d)
    {
        d.Foo();
        d.Bar();
    }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource, compilation =>
            {
                compilation.VerifyDiagnostics();

                var globalNamespace = compilation.GlobalNamespace;

                var baseClass = globalNamespace.GetMember<NamedTypeSymbol>("Base");
                var derivedClass = globalNamespace.GetMember<NamedTypeSymbol>("Derived");
                var overrideClass = globalNamespace.GetMember<NamedTypeSymbol>("Override");
                var invokeClass = globalNamespace.GetMember<NamedTypeSymbol>("Invoke");

                var baseMethod = baseClass.GetMember<MethodSymbol>("Foo");
                var derivedMethod = derivedClass.GetMember<MethodSymbol>("Bar");
                var overrideMethod = overrideClass.GetMember<MethodSymbol>("Bar");

                Assert.True(derivedMethod.IsOverride);
                Assert.Null(derivedMethod.OverriddenMethod);

                Assert.True(overrideMethod.IsOverride);
                Assert.Equal(derivedMethod, overrideMethod.OverriddenMethod);
            });
        }

        [WorkItem(542828, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542828")]
        [Fact]
        public void MetadataOverrideVirtualHiddenByNonVirtual()
        {
            var source = @"
using A = BaseVirtual;
using B = DerivedNonVirtual;
using C = Derived2Override;

class Program
{
    static void Main()
    {
        A a = new A();
        B b = new B();
        C c = new C();

        A ab = b;
        A ac = c;

        B bc = c;

        a.M();
        b.M();
        c.M();
        ab.M();
        ac.M();
        bc.M();
    }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var globalNamespace = module.GetReferencedAssemblySymbols().Last().GlobalNamespace;

                var classA = globalNamespace.GetMember<NamedTypeSymbol>("BaseVirtual");
                var classB = globalNamespace.GetMember<NamedTypeSymbol>("DerivedNonVirtual");
                var classC = globalNamespace.GetMember<NamedTypeSymbol>("Derived2Override");

                Assert.Equal(classA, classB.BaseType());
                Assert.Equal(classB, classC.BaseType());

                var methodA = classA.GetMember<MethodSymbol>("M");
                var methodB = classB.GetMember<MethodSymbol>("M");
                var methodC = classC.GetMember<MethodSymbol>("M");

                Assert.True(methodA.IsVirtual);
                Assert.False(methodB.IsVirtual);
                Assert.False(methodB.IsOverride);
                Assert.True(methodC.IsOverride);

                // Even though the runtime regards C.M as an override of A.M,
                // the language (i.e. C#) does not.
                Assert.Null(methodC.OverriddenMethod);
            };

            var references = new MetadataReference[] { TestReferences.SymbolsTests.Methods.ILMethods };
            var verifier = CompileAndVerify(
                source,
                references: references,
                sourceSymbolValidator: validator,
                expectedOutput: @"BaseVirtual
DerivedNonVirtual
Derived2Override
BaseVirtual
Derived2Override
DerivedNonVirtual
");

            // The emitted calls tell us about the overriding behavior that Dev10 expects 
            // (since it always emits a call to the least overridden method).  This is how
            // we can confirm that Roslyn ignores the runtime overriding behavior (i.e. C.M
            // overriding A.M) in the same way as Dev10.

            // From Dev10, calls should be A, B, C, A, A, B
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (BaseVirtual V_0, //a
  Derived2Override V_1, //c
  BaseVirtual V_2, //ab
  BaseVirtual V_3, //ac
  DerivedNonVirtual V_4) //bc
  IL_0000:  newobj     ""BaseVirtual..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""DerivedNonVirtual..ctor()""
  IL_000b:  newobj     ""Derived2Override..ctor()""
  IL_0010:  stloc.1
  IL_0011:  dup
  IL_0012:  stloc.2
  IL_0013:  ldloc.1
  IL_0014:  stloc.3
  IL_0015:  ldloc.1
  IL_0016:  stloc.s    V_4
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""void BaseVirtual.M()""
  IL_001e:  callvirt   ""void DerivedNonVirtual.M()""
  IL_0023:  ldloc.1
  IL_0024:  callvirt   ""void Derived2Override.M()""
  IL_0029:  ldloc.2
  IL_002a:  callvirt   ""void BaseVirtual.M()""
  IL_002f:  ldloc.3
  IL_0030:  callvirt   ""void BaseVirtual.M()""
  IL_0035:  ldloc.s    V_4
  IL_0037:  callvirt   ""void DerivedNonVirtual.M()""
  IL_003c:  ret
}
");
        }

        [WorkItem(543158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543158")]
        [Fact()]
        public void NoDefaultForParams_Dev10781558()
        {
            var source = @"
using System;

abstract class A
{
    public abstract void Foo(params int[] x);
}
class B : A
{
    public override void Foo(int[] x = null)
    {
        Console.Write(x);
    }

    static void Main()
    {
        new B().Foo();
    }
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromMetadata => module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classA = globalNamespace.GetMember<NamedTypeSymbol>("A");
                var classB = globalNamespace.GetMember<NamedTypeSymbol>("B");

                Assert.Equal(classA, classB.BaseType());

                var fooA = classA.GetMember<MethodSymbol>("Foo");
                var fooB = classB.GetMember<MethodSymbol>("Foo");

                Assert.Equal(fooA, fooB.GetConstructedLeastOverriddenMethod(classB));

                Assert.Equal(1, fooA.ParameterCount);
                var parameterA = fooA.Parameters[0];
                Assert.True(parameterA.IsParams, "Parameter is not ParameterArray");
                Assert.False(parameterA.HasExplicitDefaultValue, "ParameterArray param has default value");
                Assert.False(parameterA.IsOptional, "ParameterArray param cannot be optional");

                Assert.Equal(1, fooB.ParameterCount);
                var parameterB = fooB.Parameters[0];
                Assert.True(parameterB.IsParams, "Parameter is not ParameterArray");
                Assert.False(parameterB.HasExplicitDefaultValue, "ParameterArray param has default value");
                Assert.Equal(ConstantValue.Null, parameterB.ExplicitDefaultConstantValue);
                Assert.False(parameterB.IsOptional, "ParameterArray param cannot be optional");

                if (isFromMetadata)
                {
                    WellKnownAttributesTestBase.VerifyParamArrayAttribute(parameterB);
                };
            };

            var verifier = CompileAndVerify(source, symbolValidator: validator(true), sourceSymbolValidator: validator(false), expectedOutput: @"System.Int32[]");
        }

        [WorkItem(543158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543158")]
        [Fact]
        public void XNoDefaultForParams_Dev10781558()
        {
            var source = @"
public class Base
{
    public virtual void M() {}
}";
            var source2 = @"
using System;

public class Derived : Base
{
    public override void M() { Console.WriteLine(""M""); }
}

public class Test
{
    public static void Main()
    {
        var obj = new Derived();
        obj.M();
    }
}";
            var compref = CreateCompilation(source, assemblyName: "XNoDefaultForParams_Dev10781558_Library");
            var comp = CompileAndVerify(source2, references: new[] { new CSharpCompilationReference(compref) }, expectedOutput: "M");
        }

        [Fact]
        public void CrossLanguageCase1()
        {
            var vb1Compilation = CreateVisualBasicCompilation("VB1",
@"Public MustInherit Class C1
    MustOverride Sub foo()
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            vb1Compilation.VerifyDiagnostics();

            var cs1Compilation = CreateCSharpCompilation("CS1",
@"using System;
public abstract class C2 : C1
{
    new internal virtual void foo()
    {
        Console.WriteLine(""C2"");
    }
}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { vb1Compilation });
            var cs1Verifier = CompileAndVerify(cs1Compilation);
            cs1Verifier.VerifyDiagnostics();

            var vb2Compilation = CreateVisualBasicCompilation("VB2",
@"Imports System
Public Class C3 : Inherits C2
    Public Overrides Sub foo
        Console.WriteLine(""C3"")
    End Sub
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation });
            vb2Compilation.VerifyDiagnostics();

            var cs2Compilation = CreateCSharpCompilation("CS2",
@"
public class C4 : C3
{
}

// Below commented code results in PEVerify failures
// for both Roslyn and Dev11.
//public class C5 : C2
//{
//    public override void foo()
//    {
//        Console.WriteLine(""C5"");
//    }
//}

public class Program
{
    public static void Main()
    {
        C1 x = new C4();
        x.foo();
        //C2 y = new C5();
        //y.foo();
    }
}",
                compilationOptions: TestOptions.ReleaseExe,
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation, vb2Compilation });
            var cs2Verifier = CompileAndVerify(cs2Compilation,
                expectedOutput: @"C3");
            cs2Verifier.VerifyDiagnostics();
        }

        [Fact]
        public void CrossLanguageCase2()
        {
            var vb1Compilation = CreateVisualBasicCompilation("VB1",
@"Public MustInherit Class C1
    MustOverride Sub foo()
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            vb1Compilation.VerifyDiagnostics();

            var cs1Compilation = CreateCSharpCompilation("CS1",
@"using System;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS2"")]
public abstract class C2 : C1
{
    new internal virtual void foo()
    {
        Console.WriteLine(""C2"");
    }
}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { vb1Compilation });
            var cs1Verifier = CompileAndVerify(cs1Compilation);
            cs1Verifier.VerifyDiagnostics();

            var vb2Compilation = CreateVisualBasicCompilation("VB2",
@"Imports System
Public Class C3 : Inherits C2
    Public Overrides Sub foo
        Console.WriteLine(""C3"")
    End Sub
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation });
            vb2Compilation.VerifyDiagnostics();

            var cs2Compilation = CreateCSharpCompilation("CS2",
@"using System;

public class C4 : C3
{
    public override void foo()
    {
        Console.WriteLine(""C4"");
    }
}

abstract public class C5 : C2
{
    internal override void foo()
    {
        Console.WriteLine(""C5"");
    }
}

public class Program
{
    public static void Main()
    {
        C1 x = new C4();
        x.foo();
        C2 y = new C4();
        y.foo();
    }
}",
                compilationOptions: TestOptions.ReleaseExe,
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation, vb2Compilation });
            var cs2Verifier = CompileAndVerify(cs2Compilation, expectedOutput: @"C4
C2");
            cs2Verifier.VerifyDiagnostics();
        }

        [Fact]
        public void HidingAndNamedParameters()
        {
            var source =
@"using System;

class Base
{
    public void M(int x) { Console.WriteLine(""Base.M(x:"" + x + "")""); }
}

class Derived : Base
{
    public new void M(int y) { Console.WriteLine(""Derived.M(y:"" + y + "")""); }
}

public class Test
{
    public static void Main()
    {
        Derived d = new Derived();
        d.M(x: 1);
        d.M(y: 2);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput:
@"Base.M(x:1)
Derived.M(y:2)");
        }

        [WorkItem(531095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531095")]
        [Fact]
        public void MissingAssemblyReference01()
        {
            var A = CreateCSharpCompilation("A", @"public class A {}",
                compilationOptions: TestOptions.ReleaseDll);
            CompileAndVerify(A).VerifyDiagnostics();

            var B = CreateCSharpCompilation("B", @"public interface B { void M(A a); }",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { A });
            CompileAndVerify(B).VerifyDiagnostics();

            var C = CreateCSharpCompilation("C", @"public class C { public void M(int a) { } }",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { A });
            CompileAndVerify(B).VerifyDiagnostics();

            var D = CreateCSharpCompilation("D", @"public class D : C, B { }",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { B, C }).VerifyDiagnostics(
    // (1,21): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // public class D : C, B { }
    Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("A", "A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 21)
                );
        }

        [WorkItem(531095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531095")]
        [Fact]
        public void MissingAssemblyReference02()
        {
            var A = CreateCompilation(@"public class A {}", assemblyName: "A");
            var B = CreateCompilation(@"public interface B { void M(A a); }", references: new[] { new CSharpCompilationReference(A) }, assemblyName: "B");
            var C = CreateCompilation(@"public class C { public void M(A a) { } }", references: new[] { new CSharpCompilationReference(A) }, assemblyName: "C");

            var D = CreateCompilation(@"public class D : C, B { }", references: new[] { new CSharpCompilationReference(B), new CSharpCompilationReference(C) }, assemblyName: "D");

            A.VerifyDiagnostics();
            B.VerifyDiagnostics();
            C.VerifyDiagnostics();
            D.VerifyDiagnostics(
                // (1,14): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // public class D : C, B { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("A", "A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        #region "Diagnostics"

        [Fact]
        public void CrossLanguageCase3()
        {
            var vb1Compilation = CreateVisualBasicCompilation("VB1",
@"Public MustInherit Class C1
    MustOverride Sub foo()
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            vb1Compilation.VerifyDiagnostics();

            var cs1Compilation = CreateCSharpCompilation("CS1",
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS2"")]
public abstract class C2 : C1
{
    new internal virtual void foo()
    {
    }
}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { vb1Compilation });
            var cs1Verifier = CompileAndVerify(cs1Compilation);
            cs1Verifier.VerifyDiagnostics();

            var vb2Compilation = CreateVisualBasicCompilation("VB2",
@"Public Class C3 : Inherits C2
    Public Overrides Sub foo
    End Sub
End Class",
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation });
            vb2Compilation.VerifyDiagnostics();

            var cs2Compilation = CreateCSharpCompilation("CS2",
@"abstract public class C4 : C3
{
    public override void foo()
    {
    }
}

public class C5 : C2
{
    public override void foo()
    {
    }
}

public class C6 : C2
{
    internal override void foo()
    {
    }
}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new Compilation[] { vb1Compilation, cs1Compilation, vb2Compilation });

            cs2Compilation.VerifyDiagnostics(
                // (10,26): error CS0507: 'C5.foo()': cannot change access modifiers when overriding 'internal' inherited member 'C2.foo()'
                //     public override void foo()
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "foo").WithArguments("C5.foo()", "internal", "C2.foo()"),
                // (8,14): error CS0534: 'C5' does not implement inherited abstract member 'C1.foo()'
                // public class C5 : C2
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C5").WithArguments("C5", "C1.foo()"),
                // (15,14): error CS0534: 'C6' does not implement inherited abstract member 'C1.foo()'
                // public class C6 : C2
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C6").WithArguments("C6", "C1.foo()"));
        }

        #endregion
    }
}
