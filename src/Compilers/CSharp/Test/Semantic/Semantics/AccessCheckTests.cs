// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class AccessCheckTests : CompilingTestBase
    {
        [Fact]
        public void AccessCheckOutsideToInner()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static public int c_pub;
    static internal int c_int;
    static protected int c_pro;
    static internal protected int c_intpro;
    static private int c_priv;

    void m()
    {
        C.c_pub = 1;
        C.c_int = 1;
        C.c_pro = 1;
        C.c_intpro = 1;
        C.c_priv = 1;
        N1.n1_pub = 1;
        N1.n1_int = 1;
        N1.n1_pro = 1;
        N1.n1_intpro = 1;
        N1.n1_priv = 1;
        N1.N2.n2_pub = 1;
        N1.N2.n2_int = 1;
        N1.N2.n2_pro = 1;
        N1.N2.n2_intpro = 1;
        N1.N2.n2_priv = 1;
        N1.N3.n3_pub = 1;
        N1.N4.n4_pub = 1;
        N1.N5.n5_pub = 1;
        N1.N6.n6_pub = 1;
    }

    private class N1
    {
        static public int n1_pub;
        static internal int n1_int;
        static protected int n1_pro;
        static internal protected int n1_intpro;
        static private int n1_priv;

        public class N2
        {
            static public int n2_pub;
            static internal int n2_int;
            static protected int n2_pro;
            static internal protected int n2_intpro;
            static private int n2_priv;
        }

        private class N3
        {
            static public int n3_pub;
        }

        protected class N4
        {
            static public int n4_pub;
        }

        internal class N5
        {
            static public int n5_pub;
        }

        protected internal class N6
        {
            static public int n6_pub;
        }
    }
}
");
            c.VerifyDiagnostics(
                // (19,12): error CS0122: 'C.N1.n1_pro' is inaccessible due to its protection level
                //         N1.n1_pro = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n1_pro").WithArguments("C.N1.n1_pro"),
                // (21,12): error CS0122: 'C.N1.n1_priv' is inaccessible due to its protection level
                //         N1.n1_priv = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n1_priv").WithArguments("C.N1.n1_priv"),
                // (24,15): error CS0122: 'C.N1.N2.n2_pro' is inaccessible due to its protection level
                //         N1.N2.n2_pro = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n2_pro").WithArguments("C.N1.N2.n2_pro"),
                // (26,15): error CS0122: 'C.N1.N2.n2_priv' is inaccessible due to its protection level
                //         N1.N2.n2_priv = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n2_priv").WithArguments("C.N1.N2.n2_priv"),
                // (27,12): error CS0122: 'C.N1.N3' is inaccessible due to its protection level
                //         N1.N3.n3_pub = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "N3").WithArguments("C.N1.N3"),
                // (28,12): error CS0122: 'C.N1.N4' is inaccessible due to its protection level
                //         N1.N4.n4_pub = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "N4").WithArguments("C.N1.N4"),
                // (37,30): warning CS0649: Field 'C.N1.n1_pro' is never assigned to, and will always have its default value 0
                //         static protected int n1_pro;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n1_pro").WithArguments("C.N1.n1_pro", "0"),
                // (39,28): warning CS0169: The field 'C.N1.n1_priv' is never used
                //         static private int n1_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "n1_priv").WithArguments("C.N1.n1_priv"),
                // (45,34): warning CS0649: Field 'C.N1.N2.n2_pro' is never assigned to, and will always have its default value 0
                //             static protected int n2_pro;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n2_pro").WithArguments("C.N1.N2.n2_pro", "0"),
                // (47,32): warning CS0169: The field 'C.N1.N2.n2_priv' is never used
                //             static private int n2_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "n2_priv").WithArguments("C.N1.N2.n2_priv"),
                // (52,31): warning CS0649: Field 'C.N1.N3.n3_pub' is never assigned to, and will always have its default value 0
                //             static public int n3_pub;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n3_pub").WithArguments("C.N1.N3.n3_pub", "0"),
                // (57,31): warning CS0649: Field 'C.N1.N4.n4_pub' is never assigned to, and will always have its default value 0
                //             static public int n4_pub;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n4_pub").WithArguments("C.N1.N4.n4_pub", "0"),
                // (8,24): warning CS0414: The field 'C.c_priv' is assigned but its value is never used
                //     static private int c_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "c_priv").WithArguments("C.c_priv")
                );
        }

        [Fact]
        public void AccessCheckInnerToOuter()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static public int c_pub;
    static internal int c_int;
    static protected int c_pro;
    static internal protected int c_intpro;
    static private int c_priv;


    private class N1
    {
        static public int n1_pub;
        static internal int n1_int;
        static protected int n1_pro;
        static internal protected int n1_intpro;
        static private int n1_priv;

        public class N2
        {
            static public int n2_pub;
            static internal int n2_int;
            static protected int n2_pro;
            static internal protected int n2_intpro;
            static private int n2_priv;

            void m()
            {
                c_pub = 1;
                c_int = 1;
                c_pro = 1;
                c_intpro = 1;
                c_priv = 1;
                n1_pub = 1;
                n1_int = 1;
                n1_pro = 1;
                n1_intpro = 1;
                n1_priv = 1;
                n2_pub = 1;
                n2_int = 1;
                n2_pro = 1;
                n2_intpro = 1;
                n2_priv = 1;
                N3.n3_pub = 1;
                N4.n4_pub = 1;
                N5.n5_pub = 1;
                N6.n6_pub = 1;
            }

        }

        private class N3
        {
            static public int n3_pub;
        }

        protected class N4
        {
            static public int n4_pub;
        }

        internal class N5
        {
            static public int n5_pub;
        }

        protected internal class N6
        {
            static public int n6_pub;
        }
    }
}
");
            c.VerifyDiagnostics(
                // (8,24): warning CS0414: The field 'C.c_priv' is assigned but its value is never used
                //     static private int c_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "c_priv").WithArguments("C.c_priv"),
                // (17,28): warning CS0414: The field 'C.N1.n1_priv' is assigned but its value is never used
                //         static private int n1_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n1_priv").WithArguments("C.N1.n1_priv"),
                // (25,32): warning CS0414: The field 'C.N1.N2.n2_priv' is assigned but its value is never used
                //             static private int n2_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n2_priv").WithArguments("C.N1.N2.n2_priv")
                );
        }

        [Fact]
        public void AccessCheckDerived()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static public int c_pub;
    static internal int c_int;
    static protected int c_pro;
    static internal protected int c_intpro;
    static private int c_priv;

    protected class N3
    {
        static public int n3_pub;
        static internal int n3_int;
        static protected int n3_pro;
        static internal protected int n3_intpro;
        static private int n3_priv;
    }

    private class N4
    {
        static public int n4_pub;
    }

    internal class N5
    {
        static public int n5_pub;
    }

    protected internal class N6
    {
        static public int n6_pub;
    }
}

class D: C
{
    static public int n1_pub;
    static internal int n1_int;
    static protected int n1_pro;
    static internal protected int n1_intpro;
    static private int n1_priv;
}

class E: D
{
    static public int n2_pub;
    static internal int n2_int;
    static protected int n2_pro;
    static internal protected int n2_intpro;
    static private int n2_priv;

    void m()
    {
        c_pub = 1;
        c_int = 1;
        c_pro = 1;
        c_intpro = 1;
        c_priv = 1;
        n1_pub = 1;
        n1_int = 1;
        n1_pro = 1;
        n1_intpro = 1;
        n1_priv = 1;
        n2_pub = 1;
        n2_int = 1;
        n2_pro = 1;
        n2_intpro = 1;
        n2_priv = 1;
        N3.n3_pub = 1;
        N3.n3_int = 1;
        N3.n3_pro = 1;
        N3.n3_intpro = 1;
        N3.n3_priv = 1;
        N4.n4_pub = 1;
        N5.n5_pub = 1;
        N6.n6_pub = 1;
    }
}
");
            c.VerifyDiagnostics(
                // (58,9): error CS0122: 'C.c_priv' is inaccessible due to its protection level
                //         c_priv = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_priv").WithArguments("C.c_priv"),
                // (63,9): error CS0122: 'D.n1_priv' is inaccessible due to its protection level
                //         n1_priv = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n1_priv").WithArguments("D.n1_priv"),
                // (71,12): error CS0122: 'C.N3.n3_pro' is inaccessible due to its protection level
                //         N3.n3_pro = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n3_pro").WithArguments("C.N3.n3_pro"),
                // (73,12): error CS0122: 'C.N3.n3_priv' is inaccessible due to its protection level
                //         N3.n3_priv = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "n3_priv").WithArguments("C.N3.n3_priv"),
                // (74,9): error CS0122: 'C.N4' is inaccessible due to its protection level
                //         N4.n4_pub = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "N4").WithArguments("C.N4"),
                // (14,30): warning CS0649: Field 'C.N3.n3_pro' is never assigned to, and will always have its default value 0
                //         static protected int n3_pro;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n3_pro").WithArguments("C.N3.n3_pro", "0"),
                // (16,28): warning CS0169: The field 'C.N3.n3_priv' is never used
                //         static private int n3_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "n3_priv").WithArguments("C.N3.n3_priv"),
                // (21,27): warning CS0649: Field 'C.N4.n4_pub' is never assigned to, and will always have its default value 0
                //         static public int n4_pub;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n4_pub").WithArguments("C.N4.n4_pub", "0"),
                // (8,24): warning CS0414: The field 'C.c_priv' is assigned but its value is never used
                //     static private int c_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "c_priv").WithArguments("C.c_priv"),
                // (41,24): warning CS0414: The field 'D.n1_priv' is assigned but its value is never used
                //     static private int n1_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n1_priv").WithArguments("D.n1_priv"),
                // (50,24): warning CS0414: The field 'E.n2_priv' is assigned but its value is never used
                //     static private int n2_priv;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n2_priv").WithArguments("E.n2_priv")
            );
        }

        [Fact]
        public void AccessCheckProtected()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A
{
    protected int iField;
    static protected int sField;
}

public class B : A
{
    public class N : A
    {
        public class NN
        {
            public void m(B b, C c, D d, E e) {
                int x = b.iField;
                int y = B.sField;
                int z = c.iField;
                int w = C.sField;
                int q = d.iField;
                int r = D.sField;
                int s = e.iField;
                int t = E.sField;
            }
        }
    }
}

public class C : B
{}
public class D : A
{}
public class E : B.N
{}");

            c.VerifyDiagnostics(
                // (19,27): error CS1540: Cannot access protected member 'A.iField' via a qualifier of type 'D'; the qualifier must be of type 'B.N.NN' (or derived from it)
                //                 int q = d.iField;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "iField").WithArguments("A.iField", "D", "B.N.NN").WithLocation(19, 27));
        }

        [WorkItem(539561, "DevDiv")]
        [Fact]
        public void AccessCheckProtected02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
interface I<T> { }
 
class A { }
 
class C : I<C.D.E>
{
    protected class D : A
    {
        public class E { }
    }
}
");

            c.VerifyDiagnostics();
        }

        [WorkItem(539561, "DevDiv")]
        [Fact]
        public void AccessCheckProtected03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class X<T> { }

class A { }

class B
{
    class C : X<C.D.E>
    {
        protected class D : A
        {
            public class E { }
        }
    }
}
");

            c.VerifyDiagnostics(
                // (8,11): error CS0060: Inconsistent accessibility: base class 'X<B.C.D.E>' is less accessible than class 'B.C'
                //     class C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C").WithArguments("B.C", "X<B.C.D.E>").WithLocation(8, 11));
        }

        [Fact]
        public void AccessCheckProtected04()
        {
            // SPEC VIOLATION: The specification implies that first overload resolution chooses the
            // SPEC VIOLATION: best method, and then if the receiver is the wrong type and the
            // SPEC VIOLATION: best method is protected, then an error occurs. The native compiler
            // SPEC VIOLATION: does it in the other order: first protected methods with potentially
            // SPEC VIOLATION: the wrong receiver type are discarded from the candidate set, and
            // SPEC VIOLATION: then the best method is chosen.
            // SPEC VIOLATION: We should consider changing the specification to match the native
            // SPEC VIOLATION: compiler behavior; it is arguably sensible and would be a 
            // SPEC VIOLATION: bad breaking change to fix now.

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class B
{
    protected void M(int x) {}
    public void M(double x) {}
}

public class D : B
{
    public void X()
    {
       B b = new D();
       b.M(123);
       // According to the spec, this should choose the int version and then error;
       // the native compiler chooses the double version. We match the native compiler behavior.
    }
}
");

            c.VerifyDiagnostics();
        }

        [Fact]
        public void AccessCheckProtectedColorColor()
        {
            // SPEC VIOLATION: The specification implies that first overload resolution chooses the
            // SPEC VIOLATION: best method, and then if the receiver is the wrong type and the
            // SPEC VIOLATION: best method is protected, then an error occurs. The native compiler
            // SPEC VIOLATION: does it in the other order: first protected methods with potentially
            // SPEC VIOLATION: the wrong receiver type are discarded from the candidate set, and
            // SPEC VIOLATION: then the best method is chosen.
            // SPEC VIOLATION: We should consider changing the specification to match the native
            // SPEC VIOLATION: compiler behavior; it is arguably sensible and would be a 
            // SPEC VIOLATION: bad breaking change to fix now.
            //
            // This fact has interesting implications in "Color Color" scenarios; when one 
            // interpretation would produce an error, we fall back to the non-error producing
            // interpretation.
            //
            // Here we verify that every combination of overload resolution problems involving 
            // a potential "color color" scenario that *can* succeed *does* succeed, even if
            // doing so picks a "worse" method. Case 5, where both the double and int versions
            // are protected instance methods must fail, and is therefore omitted. All the rest
            // have a way to succeed, either by calling a protected static method or a non-protected
            // instance method.

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
namespace CS1540
{
    public class Base
    {

        public Base() {}
        protected static void M0(double x) {}
        protected void M1(double x) {}
        public static void M2(double x) {}
        public void M3(double x) {}
        protected static void M4(double x) {}
//      protected void M5(double x) {}
        public static void M6(double x) {}
        public void M7(double x) {}
        protected static void M8(double x) {}
        protected void M9(double x) {}
        public static void Ma(double x) {}
        public void Mb(double x) {}
        protected static void Mc(double x) {}
        protected void Md(double x) {}
        public static void Me(double x) {}
        public void Mf(double x) {}

        protected static void M0(int x) {}
        protected static void M1(int x) {}
        protected static void M2(int x) {}
        protected static void M3(int x) {}
        protected void M4(int x) {}
        // protected void M5(int x) {}
        protected void M6(int x) {}
        protected void M7(int x) {}
        public static void M8(int x) {}
        public static void M9(int x) {}
        public static void Ma(int x) {}
        public static void Mb(int x) {}
        public void Mc(int x) {}
        public void Md(int x) {}
        public void Me(int x) {}
        public void Mf(int x) {}


        
    }

    public class Derived : Base
    {

        public Base Base { get { return new Base(); } }

        private void X()
        {
            Base.M0(123);
            Base.M1(123);
            Base.M2(123);
            Base.M3(123);
            Base.M4(123);
            //Base.M5(123);
            Base.M6(123);
            Base.M7(123);
            Base.M8(123);
            Base.M9(123);
            Base.Ma(123);
            Base.Mb(123);
            Base.Mc(123);
            Base.Md(123);
            Base.Me(123);
            Base.Mf(123);
        }
        
        static void Main()
        {
        }
    }
}

");

            c.VerifyDiagnostics();
        }

        [WorkItem(539561, "DevDiv")]
        [Fact]
        public void AccessCheckPrivate()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
interface I<T> { }
 
class A { }
 
class C : I<C.D.E>
{
    private class D : A
    {
        public class E { }
    }
}
");

            c.VerifyDiagnostics();
        }

        [WorkItem(539561, "DevDiv")]
        [Fact]
        public void AccessCheckPrivate02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class X<T> { }

class A { }

class B
{
    class C : X<C.D.E>
    {
        private class D : A
        {
            public class E { }
        }
    }
}
");

            c.VerifyDiagnostics(
                // (8,11): error CS0060: Inconsistent accessibility: base class 'X<B.C.D.E>' is less accessible than class 'B.C'
                //     class C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C").WithArguments("B.C", "X<B.C.D.E>").WithLocation(8, 11));
        }

        [Fact]
        public void AccessCheckCrossAssembly()
        {
            CSharpCompilation other = CreateCompilationWithMscorlib(@"
public class C
{
    static public int c_pub;
    static internal int c_int;
    static protected int c_pro;
    static internal protected int c_intpro;
    static private int c_priv;
}

internal class D
{
    static public int d_pub;
}");

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A
{
    public void m() {
        int a = C.c_pub;
        int b = C.c_int;
        int c = C.c_pro;
        int d = C.c_intpro;
        int e = C.c_priv;
        int f = D.d_pub;
    }
}", new List<MetadataReference>() { new CSharpCompilationReference(other) });

            c.VerifyDiagnostics(
                // (6,19): error CS0122: 'C.c_int' is inaccessible due to its protection level
                //         int b = C.c_int;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_int").WithArguments("C.c_int").WithLocation(6, 19),
                // (7,19): error CS0122: 'C.c_pro' is inaccessible due to its protection level
                //         int c = C.c_pro;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_pro").WithArguments("C.c_pro").WithLocation(7, 19),
                // (8,19): error CS0122: 'C.c_intpro' is inaccessible due to its protection level
                //         int d = C.c_intpro;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_intpro").WithArguments("C.c_intpro").WithLocation(8, 19),
                // (9,19): error CS0122: 'C.c_priv' is inaccessible due to its protection level
                //         int e = C.c_priv;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_priv").WithArguments("C.c_priv").WithLocation(9, 19),
                // (10,17): error CS0122: 'D' is inaccessible due to its protection level
                //         int f = D.d_pub;
                Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("D").WithLocation(10, 17));
        }

        [Fact]
        public void AccessCheckCrossAssemblyDerived()
        {
            CSharpCompilation other = CreateCompilationWithMscorlib(@"
public class C
{
    static public int c_pub;
    static internal int c_int;
    static protected int c_pro;
    static internal protected int c_intpro;
    static private int c_priv;
}

internal class D
{
    static public int d_pub;
}");

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A: C
{
    public void m() {
        int a = C.c_pub;
        int b = C.c_int;
        int c = C.c_pro;
        int d = C.c_intpro;
        int e = C.c_priv;
        int f = D.d_pub;
    }
}", new[] { new CSharpCompilationReference(other) });

            c.VerifyDiagnostics(
                // (6,19): error CS0122: 'C.c_int' is inaccessible due to its protection level
                //         int b = C.c_int;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_int").WithArguments("C.c_int").WithLocation(6, 19),
                // (9,19): error CS0122: 'C.c_priv' is inaccessible due to its protection level
                //         int e = C.c_priv;
                Diagnostic(ErrorCode.ERR_BadAccess, "c_priv").WithArguments("C.c_priv").WithLocation(9, 19),
                // (10,17): error CS0122: 'D' is inaccessible due to its protection level
                //         int f = D.d_pub;
                Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("D").WithLocation(10, 17));
        }

        [Fact]
        public void AccessCheckApi1()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
using System.Collections.Generic;
class A
{
    static private int priv;
    static public int pub;
    protected int prot;
    static private Foo unknowntype;
    
    private class K {}

    private K[] karray;
    private A[] aarray;
    private IEnumerable<K> kenum;
    private IEnumerable<A> aenum;
}

class B
{}

class ADerived: A
{}

class ADerived2: A
{}
");
            NamespaceSymbol globalNS = c.GlobalNamespace;
            AssemblySymbol sourceAssem = c.SourceModule.ContainingAssembly;
            AssemblySymbol mscorlibAssem = c.GetReferencedAssemblySymbol(c.ExternalReferences[0]);
            NamedTypeSymbol classA = globalNS.GetMembers("A").Single() as NamedTypeSymbol;
            NamedTypeSymbol classADerived = globalNS.GetMembers("ADerived").Single() as NamedTypeSymbol;
            NamedTypeSymbol classADerived2 = globalNS.GetMembers("ADerived2").Single() as NamedTypeSymbol;
            NamedTypeSymbol classB = globalNS.GetMembers("B").Single() as NamedTypeSymbol;
            NamedTypeSymbol classK = classA.GetMembers("K").Single() as NamedTypeSymbol;
            FieldSymbol privField = classA.GetMembers("priv").Single() as FieldSymbol;
            FieldSymbol pubField = classA.GetMembers("pub").Single() as FieldSymbol;
            FieldSymbol protField = classA.GetMembers("prot").Single() as FieldSymbol;
            TypeSymbol karrayType = (classA.GetMembers("karray").Single() as FieldSymbol).Type.TypeSymbol;
            TypeSymbol aarrayType = (classA.GetMembers("aarray").Single() as FieldSymbol).Type.TypeSymbol;
            TypeSymbol kenumType = (classA.GetMembers("kenum").Single() as FieldSymbol).Type.TypeSymbol;
            TypeSymbol aenumType = (classA.GetMembers("aenum").Single() as FieldSymbol).Type.TypeSymbol;
            TypeSymbol unknownType = (classA.GetMembers("unknowntype").Single() as FieldSymbol).Type.TypeSymbol;
            var semanticModel = c.GetSemanticModel(c.SyntaxTrees[0]);

            Assert.True(Symbol.IsSymbolAccessible(classA, classB));
            Assert.True(Symbol.IsSymbolAccessible(pubField, classB));
            Assert.False(Symbol.IsSymbolAccessible(privField, classB));
            Assert.False(Symbol.IsSymbolAccessible(karrayType, classB));
            Assert.True(Symbol.IsSymbolAccessible(aarrayType, classB));
            Assert.False(Symbol.IsSymbolAccessible(kenumType, classB));
            Assert.True(Symbol.IsSymbolAccessible(aenumType, classB));
            Assert.True(Symbol.IsSymbolAccessible(unknownType, classB));
            Assert.True(Symbol.IsSymbolAccessible(globalNS, classB));
            Assert.True(Symbol.IsSymbolAccessible(protField, classA));
            Assert.True(Symbol.IsSymbolAccessible(protField, classA, classADerived));
            Assert.False(Symbol.IsSymbolAccessible(protField, classB));
            Assert.False(Symbol.IsSymbolAccessible(protField, classB, classADerived));
            Assert.True(Symbol.IsSymbolAccessible(protField, classA));
            Assert.True(Symbol.IsSymbolAccessible(protField, classADerived, classADerived));
            Assert.False(Symbol.IsSymbolAccessible(protField, classADerived, classADerived2));

            Assert.True(Symbol.IsSymbolAccessible(classA, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(aarrayType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(karrayType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(classA, mscorlibAssem));
            Assert.True(Symbol.IsSymbolAccessible(unknownType, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(mscorlibAssem, sourceAssem));
        }

        [Fact]
        public void AccessCheckCrossAssemblyParameterProtectedMethodP2P()
        {
            CSharpCompilation other = CreateCompilationWithMscorlib(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1");
            Assert.Empty(other.GetDiagnostics());

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A
{
  internal class B
  {
    protected B(C o) {}
  }
}", new MetadataReference[] { new CSharpCompilationReference(other) }, assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod2");
            Assert.Empty(c.GetDiagnostics());
        }

        [Fact]
        public void EnsureAccessCheckWithBadIVTDenies()
        {
            CSharpCompilation other = CreateCompilationWithMscorlib(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2000000"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1");
            Assert.Empty(other.GetDiagnostics());

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A
{
  internal class B
  {
    protected B(C o) {}
  }
}", new MetadataReference[] { new CSharpCompilationReference(other) }, assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod2");
            c.VerifyDiagnostics(
                // (6,17): error CS0122: 'C' is inaccessible due to its protection level
                //     protected B(C o) {}
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("C").WithLocation(6, 17));
        }

        [Fact]
        public void AccessCheckCrossAssemblyParameterProtectedMethodMD()
        {
            var other = CreateCompilationWithMscorlib(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1").EmitToArray();

            CSharpCompilation c = CreateCompilationWithMscorlib(@"
public class A
{
  internal class B
  {
    protected B(C o) {}
  }
}", new MetadataReference[] { MetadataReference.CreateFromImage(other) }, assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod2");
            Assert.Empty(c.GetDiagnostics());
        }

        [WorkItem(543745, "DevDiv")]
        [Fact]
        public void InternalInaccessibleProperty()
        {
            var assembly1Compilation = CreateCSharpCompilation("Assembly1",
@"public class InstancePropertyContainer
{
    internal protected int PropIntProProSet { get { return 5; } protected set { } }
}",
                compilationOptions: TestOptions.ReleaseDll);
            var assembly1Verifier = CompileAndVerify(assembly1Compilation);
            assembly1Verifier.VerifyDiagnostics();

            var assembly2Compilation = CreateCSharpCompilation("Assembly2",
@"public class SubInstancePropertyContainer
{
    public static void RunTest()
    {
        InstancePropertyContainer InstancePropertyContainer = new InstancePropertyContainer();

        InstancePropertyContainer.PropIntProProSet = 12;
    }
}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new[] { assembly1Compilation });

            assembly2Compilation.VerifyDiagnostics(
                // (7,35): error CS0122: 'InstancePropertyContainer.PropIntProProSet' is inaccessible due to its protection level
                //         PropIntProProSet
                Diagnostic(ErrorCode.ERR_BadAccess, "PropIntProProSet").WithArguments("InstancePropertyContainer.PropIntProProSet"));
        }

        [WorkItem(546209, "DevDiv")]
        [Fact]
        public void OverriddenMemberFromInternalType()
        {
            var source1 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
internal class A
{
    public virtual void M() { }
    public virtual object P { get { return null; } set { } }
}";
            var compilation1 = CreateCompilationWithMscorlib(source1, assemblyName: "A");
            compilation1.VerifyDiagnostics();
            var compilationVerifier = CompileAndVerify(compilation1);
            var reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source2 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
// Override none.
internal abstract class B0 : A
{
}
// Override both accessors.
internal abstract class B1 : A
{
    public override void M() { }
    public override object P { get { return null; } set { } }
}
// Override get only.
internal abstract class B2 : A
{
    public override object P { get { return null; } }
}
// Override get only.
internal abstract class B3 : A
{
    public override object P { set { } }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, assemblyName: "B", references: new[] { reference1 });
            compilation2.VerifyDiagnostics();
            compilationVerifier = CompileAndVerify(compilation2);
            var reference2 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source3 =
@"class C
{
    static void M(B0 b0, B1 b1, B2 b2, B3 b3)
    {
        object o;
        b0.M();
        b1.M();
        o = b0.P;
        b0.P = o;
        o = b1.P;
        b1.P = o;
        o = b2.P;
        b2.P = o;
        o = b3.P;
        b3.P = o;
    }
}";
            var compilation3 = CreateCompilationWithMscorlib(source3, assemblyName: "C", references: new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics(
                // (6,12): error CS0122: 'A.M()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("A.M()").WithLocation(6, 12),
                // (8,16): error CS0122: 'A.P' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "P").WithArguments("A.P").WithLocation(8, 16),
                // (9,12): error CS0122: 'A.P' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "P").WithArguments("A.P").WithLocation(9, 12),
                // (13,9): error CS0272: The property or indexer 'B2.P' cannot be used in this context because the set accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "b2.P").WithArguments("B2.P").WithLocation(13, 9),
                // (14,13): error CS0271: The property or indexer 'B3.P' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "b3.P").WithArguments("B3.P").WithLocation(14, 13));
        }

        [WorkItem(546209, "DevDiv")]
        [Fact]
        public void InternalOverriddenMember()
        {
            var source1 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
public abstract class A
{
    internal abstract void M();
    internal abstract object P { get; }
}";
            var compilation1 = CreateCompilationWithMscorlib(source1, assemblyName: "A");
            compilation1.VerifyDiagnostics();
            var compilationVerifier = CompileAndVerify(compilation1);
            var reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source2 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public abstract class B : A
{
    internal override abstract void M();
    internal override abstract object P { get; }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, assemblyName: "B", references: new[] { reference1 });
            compilation2.VerifyDiagnostics();
            compilationVerifier = CompileAndVerify(compilation2);
            var reference2 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source3 =
@"class C
{
    static object M(B b)
    {
        b.M();
        return b.P;
    }
}";
            var compilation3 = CreateCompilationWithMscorlib(source3, assemblyName: "C", references: new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics();
        }

        [WorkItem(530360, "DevDiv")]
        [Fact]
        public void InaccessibleReturnType()
        {
            var sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
internal class A
{
}
";
            var compilationA = CreateCompilationWithMscorlib(sourceA, assemblyName: "A");
            var compilationVerifier = CompileAndVerify(compilationA);
            var referenceA = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);

            // Dev11 compiler doesn't allow this code, Roslyn does.
            var sourceB = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B
{
    internal A M() { return null; }
}
";
            var compilationB = CreateCompilationWithMscorlib(sourceB, assemblyName: "B", references: new[] { referenceA });
            compilationVerifier = CompileAndVerify(compilationB);
            var referenceB = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);

            var sourceC = @"
class C
{
    static void M(B b)
    {
        b.M();
    }
}
";
            var compilationC = CreateCompilationWithMscorlib(sourceC, assemblyName: "C", references: new[] { referenceA, referenceB });
            compilationC.VerifyDiagnostics(
                // (5,9): error CS0122: 'B.M()' is inaccessible due to its protection level
                //         b.M();
                Diagnostic(ErrorCode.ERR_BadAccess, "b.M").WithArguments("B.M()"));
        }

        [WorkItem(530360, "DevDiv")]
        [Fact]
        public void InaccessibleReturnType_Dynamic()
        {
            var sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
internal class A
{
}
";
            var compilationA = CreateCompilationWithMscorlib(sourceA, assemblyName: "A");
            var compilationVerifier = CompileAndVerify(compilationA);
            var referenceA = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);

            var sourceB = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B
{
    internal A M(int a) { return null; }
}
";
            var compilationB = CreateCompilationWithMscorlib(sourceB, assemblyName: "B", references: new[] { referenceA });
            compilationVerifier = CompileAndVerify(compilationB);
            var referenceB = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);

            var sourceC = @"
class C
{
    static void M(B b, dynamic d)
    {
        b.M(d);
    }
}
";
            var compilationC = CreateCompilationWithMscorlib(sourceC, assemblyName: "C", references: new[] { referenceA, referenceB, SystemCoreRef });
            compilationC.VerifyDiagnostics(
                // (6,9): error CS0122: 'B.M(int)' is inaccessible due to its protection level
                //         b.M(d);
                Diagnostic(ErrorCode.ERR_BadAccess, "b.M(d)").WithArguments("B.M(int)"));
        }

        [WorkItem(563573, "DevDiv")]
        [Fact]
        public void MissingIdentifier01()
        {
            var source =
@"class /*MyClass*/
{
    internal protected class MyInner
    {
        public int MyMeth(MyInner2 arg)
        {
            return arg.intI;
        }
    }
    internal protected class MyInner2
    {
        public int intI = 2;
    }
    public static int Main()
    {
        MyInner MI = new MyInner();
        return MI.MyMeth(new MyInner2());
    }
}";
            CreateCompilationWithMscorlib(source).GetDiagnostics();
        }

        [WorkItem(563563, "DevDiv"), WorkItem(563573, "DevDiv")]
        [Fact]
        public void MissingIdentifier02()
        {
            var source =
@"class /*MyClass*/
{
    internal protected class MyInner
    {
        protected int MyMeth(MyInner2 arg)
        {
            return arg.intI;
        }
    }
    internal protected class MyInner2
    {
        public int intI = 2;
    }
    public static int Main()
    {
        MyInner MI = new MyInner();
        return MI.MyMeth(new MyInner2());
    }
}";
            CreateCompilationWithMscorlib(source).GetDiagnostics();
        }

        [Fact]
        [WorkItem(552452, "DevDiv")]
        public void AccessTestInBadCode01()
        {
            var source =
@"///////////////
using System;
using System.Collections;
public static partial class Extensions
{
    public static bool Test(this bool b) { return b; }
    public static int Test(this int i) { return i; }
    public static System.Object Test(this System.Object o) { return o; }
    public static System.String Test(this System.String s) { return s; }
    public static E Test(this E e) { return e; }
}
public struct S { }
public enum E { A, B, C }
public class Test { }
 
//
c class TestClass5 { }
[MyAttribue((object)((new S()).Test()))]
public clas TestClass2 { }
[MyAttribut(object) ((new E()).Test()))]
public class TestClass1 { }
///////////////";
            CreateCompilationWithMscorlib(source).GetDiagnostics();
        }
    }
}
