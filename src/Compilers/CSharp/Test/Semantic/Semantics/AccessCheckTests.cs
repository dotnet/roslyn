// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
            CSharpCompilation c = CreateCompilation(@"
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
            CSharpCompilation c = CreateCompilation(@"
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
            CSharpCompilation c = CreateCompilation(@"
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
            CSharpCompilation c = CreateCompilation(@"
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

        [WorkItem(539561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539561")]
        [Fact]
        public void AccessCheckProtected02()
        {
            CSharpCompilation c = CreateCompilation(@"
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

        [WorkItem(539561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539561")]
        [Fact]
        public void AccessCheckProtected03()
        {
            CSharpCompilation c = CreateCompilation(@"
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
                // (8,11): error CS9338: Inconsistent accessibility: type 'B.C.D' is less accessible than class 'B.C'
                //     class C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisBaseType, "C").WithArguments("B.C", "B.C.D").WithLocation(8, 11));
        }

        [Fact]
        public void AccessCheckProtected04()
        {
            CSharpCompilation c = CreateCompilation(@"
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
       b.M(123); // because of the receiver type, only M(double x) is accessible.
    }
}
");

            c.VerifyDiagnostics();
        }

        [Fact]
        public void AccessCheckProtectedColorColor()
        {
            // Inaccessible methods are not a member of a method group.
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

            CSharpCompilation c = CreateCompilation(@"
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

        [WorkItem(539561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539561")]
        [Fact]
        public void AccessCheckPrivate()
        {
            CSharpCompilation c = CreateCompilation(@"
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

        [WorkItem(539561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539561")]
        [Fact]
        public void AccessCheckPrivate02()
        {
            CSharpCompilation c = CreateCompilation(@"
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
                // (8,11): error CS9338: Inconsistent accessibility: type 'B.C.D' is less accessible than class 'B.C'
                //     class C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisBaseType, "C").WithArguments("B.C", "B.C.D").WithLocation(8, 11));
        }

        [Fact]
        public void AccessCheckCrossAssembly()
        {
            CSharpCompilation other = CreateCompilation(@"
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

            CSharpCompilation c = CreateCompilation(@"
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
                Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("D").WithLocation(10, 17)
                );
        }

        [Fact]
        public void AccessCheckCrossAssemblyDerived()
        {
            CSharpCompilation other = CreateCompilation(@"
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

            CSharpCompilation c = CreateCompilation(@"
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
                Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("D").WithLocation(10, 17)
                );
        }

        [Fact]
        public void AccessCheckApi_01()
        {
            Compilation c = CreateCompilation(@"
using System.Collections.Generic;
using AliasForA = A;
class A
{
    static private int priv;
    static public int pub;
    protected int prot;
    static private Goo unknowntype;
    
    private class K {}

    private K[] karray;
    private A[] aarray;
    private K* kptr;
    private A* aptr;
    private delegate*<A, A, K> kinreturnfuncptr;
    private delegate*<K, A, A> kinparamfuncptr1;
    private delegate*<A, K, A> kinparamfuncptr2;
    private delegate*<A, A> afuncptr;
    private IEnumerable<K> kenum;
    private IEnumerable<A> aenum;
    void M()
    {
        _ = new A.K(); // K discard
        _ = new A(); // A discard
    }
}

class B
{}

class ADerived: A
{}

class ADerived2: A
{}
");
            Compilation compilation = c;
            INamespaceSymbol globalNS = c.GlobalNamespace;
            IAssemblySymbol sourceAssem = c.SourceModule.ContainingAssembly;
            IAssemblySymbol mscorlibAssem = ((CSharpCompilation)c).GetReferencedAssemblySymbol(c.ExternalReferences[0]).GetPublicSymbol();
            INamedTypeSymbol classA = globalNS.GetMembers("A").Single() as INamedTypeSymbol;
            var tree = c.SyntaxTrees.First();
            var model = c.GetSemanticModel(tree);
            IAliasSymbol aliasA = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Where(u => u.Alias != null).Single()) as IAliasSymbol;
            INamedTypeSymbol classADerived = globalNS.GetMembers("ADerived").Single() as INamedTypeSymbol;
            INamedTypeSymbol classADerived2 = globalNS.GetMembers("ADerived2").Single() as INamedTypeSymbol;
            INamedTypeSymbol classB = globalNS.GetMembers("B").Single() as INamedTypeSymbol;
            INamedTypeSymbol classK = classA.GetMembers("K").Single() as INamedTypeSymbol;
            IFieldSymbol privField = classA.GetMembers("priv").Single() as IFieldSymbol;
            IFieldSymbol pubField = classA.GetMembers("pub").Single() as IFieldSymbol;
            IFieldSymbol protField = classA.GetMembers("prot").Single() as IFieldSymbol;
            ITypeSymbol karrayType = (classA.GetMembers("karray").Single() as IFieldSymbol).Type;
            ITypeSymbol aarrayType = (classA.GetMembers("aarray").Single() as IFieldSymbol).Type;
            ITypeSymbol kptrType = (classA.GetMembers("kptr").Single() as IFieldSymbol).Type;
            ITypeSymbol aptrType = (classA.GetMembers("aptr").Single() as IFieldSymbol).Type;
            ITypeSymbol kinreturnfuncptrType = (classA.GetMembers("kinreturnfuncptr").Single() as IFieldSymbol).Type;
            ITypeSymbol kinparamfuncptr1Type = (classA.GetMembers("kinparamfuncptr1").Single() as IFieldSymbol).Type;
            ITypeSymbol kinparamfuncptr2Type = (classA.GetMembers("kinparamfuncptr2").Single() as IFieldSymbol).Type;
            ITypeSymbol afuncptrType = (classA.GetMembers("afuncptr").Single() as IFieldSymbol).Type;
            ITypeSymbol kenumType = (classA.GetMembers("kenum").Single() as IFieldSymbol).Type;
            ITypeSymbol aenumType = (classA.GetMembers("aenum").Single() as IFieldSymbol).Type;
            var discards = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ContextualKind() == SyntaxKind.UnderscoreToken).ToArray();
            IDiscardSymbol kdiscard = (IDiscardSymbol)model.GetSymbolInfo(discards[0]).Symbol;
            IDiscardSymbol adiscard = (IDiscardSymbol)model.GetSymbolInfo(discards[1]).Symbol;
            ITypeSymbol unknownType = (classA.GetMembers("unknowntype").Single() as IFieldSymbol).Type;

            ISymbol nullSymbol = null;
            Assert.Throws<ArgumentNullException>(() => { compilation.IsSymbolAccessibleWithin(classA, nullSymbol); });
            Assert.Throws<ArgumentNullException>(() => { compilation.IsSymbolAccessibleWithin(nullSymbol, classA); });
            Assert.Throws<ArgumentException>(() => { compilation.IsSymbolAccessibleWithin(classA, pubField); });

            Assert.True(Symbol.IsSymbolAccessible(classA.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(classA, classB));
            Assert.True(Symbol.IsSymbolAccessible(aliasA.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aliasA, classB));
            Assert.True(Symbol.IsSymbolAccessible(pubField.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(pubField, classB));
            Assert.False(Symbol.IsSymbolAccessible(privField.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(privField, classB));
            Assert.False(Symbol.IsSymbolAccessible(karrayType.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(karrayType, classB));
            Assert.True(Symbol.IsSymbolAccessible(aarrayType.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aarrayType, classB));
            Assert.False(Symbol.IsSymbolAccessible(kptrType.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kptrType, classB));
            Assert.True(Symbol.IsSymbolAccessible(aptrType.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aptrType, classB));
            Assert.False(Symbol.IsSymbolAccessible(kinreturnfuncptrType.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinreturnfuncptrType, classB));
            Assert.False(Symbol.IsSymbolAccessible(kinparamfuncptr1Type.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinparamfuncptr1Type, classB));
            Assert.False(Symbol.IsSymbolAccessible(kinparamfuncptr2Type.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinparamfuncptr2Type, classB));
            Assert.True(Symbol.IsSymbolAccessible(afuncptrType.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(afuncptrType, classB));
            Assert.False(Symbol.IsSymbolAccessible(kdiscard.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kdiscard, classB));
            Assert.True(Symbol.IsSymbolAccessible(adiscard.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(adiscard, classB));
            Assert.False(Symbol.IsSymbolAccessible(kenumType.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kenumType, classB));
            Assert.True(Symbol.IsSymbolAccessible(aenumType.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aenumType, classB));
            Assert.True(Symbol.IsSymbolAccessible(unknownType.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(unknownType, classB));
            Assert.True(Symbol.IsSymbolAccessible(globalNS.GetSymbol(), classB.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(globalNS, classB));
            Assert.True(Symbol.IsSymbolAccessible(protField.GetSymbol(), classA.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA));
            Assert.True(Symbol.IsSymbolAccessible(protField.GetSymbol(), classA.GetSymbol(), classADerived.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA, classADerived));
            Assert.False(Symbol.IsSymbolAccessible(protField.GetSymbol(), classB.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classB));
            Assert.False(Symbol.IsSymbolAccessible(protField.GetSymbol(), classB.GetSymbol(), classADerived.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classB, classADerived));
            Assert.True(Symbol.IsSymbolAccessible(protField.GetSymbol(), classA.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA));
            Assert.True(Symbol.IsSymbolAccessible(protField.GetSymbol(), classADerived.GetSymbol(), classADerived.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classADerived, classADerived));
            Assert.False(Symbol.IsSymbolAccessible(protField.GetSymbol(), classADerived.GetSymbol(), classADerived2.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classADerived, classADerived2));

            Assert.True(Symbol.IsSymbolAccessible(classA.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(classA, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(aliasA.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aliasA, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(aarrayType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aarrayType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(karrayType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(karrayType, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(aptrType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(aptrType, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(afuncptrType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(afuncptrType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(kptrType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kptrType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(kinreturnfuncptrType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinreturnfuncptrType, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(kinparamfuncptr1Type.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinparamfuncptr1Type, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(kinparamfuncptr2Type.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kinparamfuncptr2Type, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(adiscard.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(adiscard, sourceAssem));
            Assert.False(Symbol.IsSymbolAccessible(kdiscard.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.False(compilation.IsSymbolAccessibleWithin(kdiscard, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(unknownType.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(unknownType, sourceAssem));
            Assert.True(Symbol.IsSymbolAccessible(mscorlibAssem.GetSymbol(), sourceAssem.GetSymbol()));
            Assert.True(compilation.IsSymbolAccessibleWithin(mscorlibAssem, sourceAssem));

            Compilation otherC = CreateCompilation(@"
class Other
{
}");
            INamespaceSymbol otherGlobalNS = otherC.GlobalNamespace;
            INamedTypeSymbol classOther = otherGlobalNS.GetMembers("Other").Single() as INamedTypeSymbol;
            Assert.Throws<ArgumentException>(() => { compilation.IsSymbolAccessibleWithin(classA, classOther); });
        }

        [Fact]
        public void AccessCheckApi_02()
        {
            Compilation c1 = CreateCompilation(@"
using SomeAlias = System.Int32;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C3"")]
internal class Outer
{
    private class Inner
    {
        public int Field;
    }

    private Inner* Pointer;
    private int Integer = 1 + 2;

    protected int Protected;
    protected internal int ProtectedInternal;
    private protected int PrivateProtected;
}
internal class Other
{
}
private class Private
{
}
internal class Derived : Outer
{
}
");
            Compilation compilation1 = c1;
            var tree = c1.SyntaxTrees.First();
            var model = c1.GetSemanticModel(tree);
            IAliasSymbol SomeAlias = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Where(u => u.Alias != null).Single());
            INamespaceSymbol globalNS = c1.GlobalNamespace;
            IAssemblySymbol sourceAssem = c1.SourceModule.ContainingAssembly;
            IAssemblySymbol mscorlibAssem = ((CSharpCompilation)c1).GetReferencedAssemblySymbol(c1.ExternalReferences[0]).GetPublicSymbol();
            INamedTypeSymbol Outer = globalNS.GetMembers("Outer").Single() as INamedTypeSymbol;
            INamedTypeSymbol Outer_Inner = Outer.GetMembers("Inner").Single() as INamedTypeSymbol;
            IFieldSymbol Outer_Inner_Field = Outer_Inner.GetMembers("Field").Single() as IFieldSymbol;
            IFieldSymbol Outer_Pointer = Outer.GetMembers("Pointer").Single() as IFieldSymbol;
            IFieldSymbol Outer_Protected = Outer.GetMembers("Protected").Single() as IFieldSymbol;
            IFieldSymbol Outer_ProtectedInternal = Outer.GetMembers("ProtectedInternal").Single() as IFieldSymbol;
            IFieldSymbol Outer_PrivateProtected = Outer.GetMembers("PrivateProtected").Single() as IFieldSymbol;
            INamedTypeSymbol Other = globalNS.GetMembers("Other").Single() as INamedTypeSymbol;
            INamedTypeSymbol Private = globalNS.GetMembers("Private").Single() as INamedTypeSymbol;
            Assert.Equal(Accessibility.Private, Private.DeclaredAccessibility);
            IMethodSymbol IntegerPlus = model.GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single()).Symbol as IMethodSymbol;
            INamedTypeSymbol Derived = globalNS.GetMembers("Derived").Single() as INamedTypeSymbol;

            Assert.True(compilation1.IsSymbolAccessibleWithin(SomeAlias, Outer));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_Pointer.Type, Outer));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Outer_Pointer.Type, Other));
            Assert.True(compilation1.IsSymbolAccessibleWithin(IntegerPlus, Other));
            Assert.True(compilation1.IsSymbolAccessibleWithin(IntegerPlus, sourceAssem));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Private, Other));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Private, sourceAssem));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Outer_Inner_Field, Other));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Outer_Protected, Derived, Outer));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_ProtectedInternal, Derived, Outer));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Outer_PrivateProtected, Derived, Outer));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_Protected, Derived));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_ProtectedInternal, Derived));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_PrivateProtected, Derived));
            Assert.False(compilation1.IsSymbolAccessibleWithin(Outer_Protected, sourceAssem));
            Assert.True(compilation1.IsSymbolAccessibleWithin(Outer_Protected, Outer_Inner));

            Compilation c2 = CreateCompilation(@"
internal class InOtherCompilation
{
}
");
            INamedTypeSymbol InOtherCompilation = c2.GlobalNamespace.GetMember("InOtherCompilation") as INamedTypeSymbol;

            Compilation c3 = CreateCompilation(@"
internal class InFriendCompilation
{
}
", assemblyName: "C3");
            INamedTypeSymbol InFriendCompilation = c3.GlobalNamespace.GetMember("InFriendCompilation") as INamedTypeSymbol;
            Compilation compilation3 = c3;
            Assert.Throws<ArgumentException>(() => { compilation3.IsSymbolAccessibleWithin(Outer, InOtherCompilation); });
            Assert.Throws<ArgumentException>(() => { compilation3.IsSymbolAccessibleWithin(Outer, InFriendCompilation); });
        }

        [Fact]
        public void AccessCheckApi_03()
        {
            var r1 = AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(filePath: @"c:\temp\a.dll", display: "R1");
            var r2 = AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(filePath: @"c:\temp\a.dll", display: "R2");
            var source = @"class Q : C { }";
            var c = CreateCompilation(source, new[] { r1 });
            Compilation compilation = c;
            c.VerifyDiagnostics();
            Assert.NotNull(c.GetReferencedAssemblySymbol(r1));
            var classC = compilation.GlobalNamespace.GetMembers("C").OfType<INamedTypeSymbol>().Single();
            var classQ = compilation.GlobalNamespace.GetMembers("Q").OfType<INamedTypeSymbol>().Single();
            Assert.True(compilation.IsSymbolAccessibleWithin(classC, classQ));

            c = CreateEmptyCompilation(source, TargetFrameworkUtil.GetReferences(TargetFramework.Standard).AddRange(new[] { r1, r2 }));
            c.VerifyDiagnostics();

            // duplicate assembly results in no assembly symbol
            Assert.Null(c.GetReferencedAssemblySymbol(r1));
            // The variable classC represents a symbol from r1, which did not result in any symbols in c
            var c2 = ((Compilation)c).GlobalNamespace.GetMembers("C").OfType<INamedTypeSymbol>().Single();
            Assert.NotEqual(classC, c2);

            Assert.NotNull(c.GetReferencedAssemblySymbol(r2));
            classQ = ((Compilation)c).GlobalNamespace.GetMembers("Q").OfType<INamedTypeSymbol>().Single();
            // the below should not throw a null reference exception.
            Assert.Throws<ArgumentException>(() => compilation.IsSymbolAccessibleWithin(classC, classQ));
        }

        [Fact]
        public void AccessCheckCrossAssemblyParameterProtectedMethodP2P()
        {
            CSharpCompilation other = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1");
            Assert.Empty(other.GetDiagnostics());

            CSharpCompilation c = CreateCompilation(@"
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
            CSharpCompilation other = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2000000"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1");
            Assert.Empty(other.GetDiagnostics());

            CSharpCompilation c = CreateCompilation(@"
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
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("C").WithLocation(6, 17)
                );
        }

        [Fact]
        public void AccessCheckCrossAssemblyParameterProtectedMethodMD()
        {
            var other = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""AccessCheckCrossAssemblyParameterProtectedMethod2"")]
internal class C {}",
                    assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod1").EmitToArray();

            CSharpCompilation c = CreateCompilation(@"
public class A
{
  internal class B
  {
    protected B(C o) {}
  }
}", new MetadataReference[] { MetadataReference.CreateFromImage(other) }, assemblyName: "AccessCheckCrossAssemblyParameterProtectedMethod2");
            Assert.Empty(c.GetDiagnostics());
        }

        [WorkItem(543745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543745")]
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
                //         InstancePropertyContainer.PropIntProProSet = 12;
                Diagnostic(ErrorCode.ERR_BadAccess, "PropIntProProSet").WithArguments("InstancePropertyContainer.PropIntProProSet").WithLocation(7, 35)
                );
        }

        [WorkItem(546209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546209")]
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
            var compilation1 = CreateCompilation(source1, assemblyName: "A");
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
            var compilation2 = CreateCompilation(source2, assemblyName: "B", references: new[] { reference1 });
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
            var compilation3 = CreateCompilation(source3, assemblyName: "C", references: new[] { reference1, reference2 });
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

        [WorkItem(546209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546209")]
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
            var compilation1 = CreateCompilation(source1, assemblyName: "A");
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
            var compilation2 = CreateCompilation(source2, assemblyName: "B", references: new[] { reference1 });
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
            var compilation3 = CreateCompilation(source3, assemblyName: "C", references: new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics();
        }

        [WorkItem(530360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530360")]
        [Fact]
        public void InaccessibleReturnType()
        {
            var sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
internal class A
{
}
";
            var compilationA = CreateCompilation(sourceA, assemblyName: "A");
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
            var compilationB = CreateCompilation(sourceB, assemblyName: "B", references: new[] { referenceA });
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
            var compilationC = CreateCompilation(sourceC, assemblyName: "C", references: new[] { referenceA, referenceB });
            compilationC.VerifyDiagnostics(
                // (5,9): error CS0122: 'B.M()' is inaccessible due to its protection level
                //         b.M();
                Diagnostic(ErrorCode.ERR_BadAccess, "b.M").WithArguments("B.M()"));
        }

        [WorkItem(530360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530360")]
        [Fact]
        public void InaccessibleReturnType_Dynamic()
        {
            var sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
internal class A
{
}
";
            var compilationA = CreateCompilation(sourceA, assemblyName: "A");
            var compilationVerifier = CompileAndVerify(compilationA);
            var referenceA = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);

            var sourceB = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B
{
    internal A M(int a) { return null; }
}
";
            var compilationB = CreateCompilation(sourceB, assemblyName: "B", references: new[] { referenceA });
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
            var compilationC = CreateCompilation(sourceC, assemblyName: "C", references: new[] { referenceA, referenceB });
            compilationC.VerifyDiagnostics(
                // (6,9): error CS0122: 'B.M(int)' is inaccessible due to its protection level
                //         b.M(d);
                Diagnostic(ErrorCode.ERR_BadAccess, "b.M(d)").WithArguments("B.M(int)"));
        }

        [WorkItem(563573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/563573")]
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
            CreateCompilation(source).GetDiagnostics();
        }

        [WorkItem(563563, "DevDiv"), WorkItem(563573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/563573")]
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
            CreateCompilation(source).GetDiagnostics();
        }

        [Fact]
        [WorkItem(552452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552452")]
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
            CreateCompilation(source).GetDiagnostics();
        }

        [Fact, WorkItem(13652, "https://github.com/dotnet/roslyn/issues/13652")]
        public void UnusedFieldInAbstractClassShouldTriggerWarning()
        {
            var text = @"
abstract class Class1
{
    private int _UnusedField;
}";
            CompileAndVerify(text).VerifyDiagnostics(
                // (4,21): warning CS0169: The field 'Class1._UnusedField' is never used
                //         private int _UnusedField;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_UnusedField").WithArguments("Class1._UnusedField").WithLocation(4, 17));
        }

        [Fact, WorkItem(13652, "https://github.com/dotnet/roslyn/issues/13652")]
        public void AssignedButNotReadFieldInAbstractClassShouldTriggerWarning()
        {
            var text = @"
abstract class Class1
{
    private int _AssignedButNotReadField;

    public Class1()
    {
        _AssignedButNotReadField = 1;
    }
}";
            CompileAndVerify(text).VerifyDiagnostics(
                // (4,21): warning CS0414: The field 'Class1._AssignedButNotReadField' is assigned but its value is never used
                //         private int _AssignedButNotReadField;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_AssignedButNotReadField").WithArguments("Class1._AssignedButNotReadField").WithLocation(4, 17));
        }

        [Fact, WorkItem(13652, "https://github.com/dotnet/roslyn/issues/13652")]
        public void UsedButNotAssignedFieldInAbstractClassShouldTriggerWarning()
        {
            var text = @"
internal abstract class Class1
{
    protected int _UnAssignedField1;

    public Class1()
    {
        System.Console.WriteLine(_UnAssignedField1);
    }
}";
            CompileAndVerify(text).VerifyDiagnostics(
                // (4,18): warning CS0649: Field 'Class1._UnAssignedField1' is never assigned to, and will always have its default value 0
                //     internal int _UnAssignedField;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "_UnAssignedField1").WithArguments("Class1._UnAssignedField1", "0").WithLocation(4, 19));
        }

        [Fact, WorkItem(13652, "https://github.com/dotnet/roslyn/issues/13652")]
        public void UsedButNotAssignedFieldInAbstractInternalClassWithIVTsShouldNotTriggerWarning()
        {
            var text = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""Test2.dll"")]

internal abstract class Class1
{
    protected int _UnAssignedField1;

    public Class1()
    {
        System.Console.WriteLine(_UnAssignedField1);
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(13652, "https://github.com/dotnet/roslyn/issues/13652")]
        public void UsedButNotAssignedFieldInAbstractPublicClassShouldNotTriggerWarning()
        {
            var text = @"
public abstract class Class1
{
    protected int _UnAssignedField1;

    public Class1()
    {
        System.Console.WriteLine(_UnAssignedField1);
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(29253, "https://github.com/dotnet/roslyn/issues/29253")]
        [Fact]
        public void InaccessibleToUnnamedExe_01()
        {
            string sourceA =
@"class A { }";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB =
@"class B
{
    static void Main()
    {
        new A();
    }
}";
            // Unnamed assembly (the default from the command-line compiler).
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, assemblyName: null);
            comp.VerifyDiagnostics(
                // (5,13): error CS0122: 'A' is inaccessible due to its protection level
                //         new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "A").WithArguments("A").WithLocation(5, 13));

            // Named assembly.
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, assemblyName: "B");
            comp.VerifyDiagnostics(
                // (5,13): error CS0122: 'A' is inaccessible due to its protection level
                //         new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "A").WithArguments("A").WithLocation(5, 13));
        }

        [WorkItem(29253, "https://github.com/dotnet/roslyn/issues/29253")]
        [Fact]
        public void InaccessibleToUnnamedExe_02()
        {
            string sourceA =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
class A { }";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB =
@"class B
{
    static void Main()
    {
        new A();
    }
}";
            // Unnamed assembly (the default from the command-line compiler).
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, assemblyName: null);
            comp.VerifyDiagnostics(
                // (5,13): error CS0122: 'A' is inaccessible due to its protection level
                //         new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "A").WithArguments("A").WithLocation(5, 13));

            // Named assembly.
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, assemblyName: "B");
            comp.VerifyDiagnostics();

            // Named assembly (distinct).
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, assemblyName: "B2");
            comp.VerifyDiagnostics(
                // (5,13): error CS0122: 'A' is inaccessible due to its protection level
                //         new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "A").WithArguments("A").WithLocation(5, 13));
        }

        [Fact]
        public void FunctionPointerTypesFromOtherInaccessibleAssembly()
        {
            var comp = CreateCompilation(@"
unsafe class A
{
    internal delegate*<A> ptr1;
    internal delegate*<A, object> ptr2;
}", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9);

            var ptr1 = comp.GetMember<FieldSymbol>("A.ptr1").Type.GetPublicSymbol();
            var ptr2 = comp.GetMember<FieldSymbol>("A.ptr2").Type.GetPublicSymbol();

            var comp2 = CreateCompilation("class B {}");

            var b = comp2.GetMember("B").GetPublicSymbol();

            Assert.Throws<ArgumentException>(() => ((Compilation)comp2).IsSymbolAccessibleWithin(ptr1, b));
            Assert.Throws<ArgumentException>(() => ((Compilation)comp2).IsSymbolAccessibleWithin(ptr2, b));
        }
    }
}
