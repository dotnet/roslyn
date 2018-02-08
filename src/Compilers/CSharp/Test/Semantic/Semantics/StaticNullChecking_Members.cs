// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking_Members : CSharpTestBase
    {
        [Fact]
        public void Fields()
        {
            var source =
@"#pragma warning disable 0649
class C
{
    internal string? F;
}
class Program
{
    static void F(C a)
    {
        G(a.F);
        if (a.F != null) G(a.F);
        C b = new C();
        G(b.F);
        if (b.F != null) G(b.F);
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(a.F);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a.F").WithArguments("s", "void Program.G(string s)").WithLocation(10, 11),
                // (13,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(b.F);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "b.F").WithArguments("s", "void Program.G(string s)").WithLocation(13, 11));
        }

        [Fact]
        public void AutoProperties()
        {
            var source =
@"class C
{
    internal string? P { get; set; }
}
class Program
{
    static void F(C a)
    {
        G(a.P);
        if (a.P != null) G(a.P);
        C b = new C();
        G(b.P);
        if (b.P != null) G(b.P);
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(a.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a.P").WithArguments("s", "void Program.G(string s)").WithLocation(9, 11),
                // (12,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(b.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "b.P").WithArguments("s", "void Program.G(string s)").WithLocation(12, 11));
        }

        [Fact]
        public void Properties()
        {
            var source =
@"class C
{
    internal string? P { get { throw new System.Exception(); } set { } }
}
class Program
{
    static void F(C a)
    {
        G(a.P);
        if (a.P != null) G(a.P);
        C b = new C();
        G(b.P);
        if (b.P != null) G(b.P);
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(a.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a.P").WithArguments("s", "void Program.G(string s)").WithLocation(9, 11),
                // (12,11): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.G(string s)'.
                //         G(b.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "b.P").WithArguments("s", "void Program.G(string s)").WithLocation(12, 11));
        }

        [Fact]
        public void Events()
        {
            // PROTOTYPE(NullableReferenceTypes): Field-like and explicit events.
        }

        [Fact]
        public void AutoPropertyFromConstructor()
        {
            var source =
@"class A
{
    protected static void F(string s)
    {
    }
    protected string? P { get; set; }
    protected A()
    {
        F(P);
        if (P != null) F(P);
    }
}
class B : A
{
    B()
    {
        F(this.P);
        if (this.P != null) F(this.P);
        F(base.P);
        if (base.P != null) F(base.P);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (17,11): warning CS8604: Possible null reference argument for parameter 's' in 'void A.F(string s)'.
                //         F(this.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "this.P").WithArguments("s", "void A.F(string s)").WithLocation(17, 11),
                // (19,11): warning CS8604: Possible null reference argument for parameter 's' in 'void A.F(string s)'.
                //         F(base.P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "base.P").WithArguments("s", "void A.F(string s)").WithLocation(19, 11),
                // (9,11): warning CS8604: Possible null reference argument for parameter 's' in 'void A.F(string s)'.
                //         F(P);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "P").WithArguments("s", "void A.F(string s)").WithLocation(9, 11));
        }

        [Fact]
        public void ModifyMembers_01()
        {
            var source =
@"#pragma warning disable 0649
class C
{
    object? F;
    static void M(C c)
    {
        if (c.F == null) return;
        c = new C();
        c.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         c.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.F").WithLocation(9, 9));
        }

        [Fact]
        public void ModifyMembers_02()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal C? C;
}
class B
{
    internal A? A;
}
class C
{
    internal B? B;
}
class Program
{
    static void F()
    {
        object o;
        C? c = new C();
        c.B = new B();
        c.B.A = new A();
        o = c.B.A; // 1
        c.B.A = null;
        o = c.B.A; // 2
        c.B = new B();
        o = c.B.A; // 3
        c.B = null;
        o = c.B.A; // 4
        c = new C();
        o = c.B.A; // 5
        c = null;
        o = c.B.A; // 6
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (24,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.A; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.A").WithLocation(24, 13),
                // (26,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.A; // 3
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.A").WithLocation(26, 13),
                // (28,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.B.A; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.B").WithLocation(28, 13),
                // (28,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.A; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.A").WithLocation(28, 13),
                // (30,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.B.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.B").WithLocation(30, 13),
                // (30,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.A").WithLocation(30, 13),
                // (32,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.B.A; // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(32, 13),
                // (32,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.B.A; // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.B").WithLocation(32, 13),
                // (32,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.A; // 6
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.A").WithLocation(32, 13));
        }

        [Fact]
        public void ModifyMembers_03()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal object? F;
}
class C
{
    internal C? A;
    internal S B;
}
class Program
{
    static void M()
    {
        object o;
        C c = new C();
        o = c.A.A; // 1
        o = c.B.F; // 1
        c.A = new C();
        c.B = new S();
        o = c.A.A; // 2
        o = c.B.F; // 2
        c.A.A = new C();
        c.B.F = new C();
        o = c.A.A; // 3
        o = c.B.F; // 3
        c.A = new C();
        c.B = new S();
        o = c.A.A; // 4
        o = c.B.F; // 4
        c = new C();
        o = c.A.A; // 5
        o = c.B.F; // 5
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (17,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.A.A; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.A").WithLocation(17, 13),
                // (17,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(17, 13),
                // (18,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.F; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.F").WithLocation(18, 13),
                // (21,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(21, 13),
                // (22,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.F; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.F").WithLocation(22, 13),
                // (29,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(29, 13),
                // (30,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.F; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.F").WithLocation(30, 13),
                // (32,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.A.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.A").WithLocation(32, 13),
                // (32,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(32, 13),
                // (33,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.F; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.F").WithLocation(33, 13));
        }

        [Fact]
        public void ModifyMembers_Properties()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal object? P { get; set; }
}
class C
{
    internal C? A { get; set; }
    internal S B;
}
class Program
{
    static void M()
    {
        object o;
        C c = new C();
        o = c.A.A; // 1
        o = c.B.P; // 1
        c.A = new C();
        c.B = new S();
        o = c.A.A; // 2
        o = c.B.P; // 2
        c.A.A = new C();
        c.B.P = new C();
        o = c.A.A; // 3
        o = c.B.P; // 3
        c.A = new C();
        c.B = new S();
        o = c.A.A; // 4
        o = c.B.P; // 4
        c = new C();
        o = c.A.A; // 5
        o = c.B.P; // 5
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (17,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.A.A; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.A").WithLocation(17, 13),
                // (17,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(17, 13),
                // (18,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.P").WithLocation(18, 13),
                // (21,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(21, 13),
                // (22,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.P; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.P").WithLocation(22, 13),
                // (29,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(29, 13),
                // (30,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.P; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.P").WithLocation(30, 13),
                // (32,13): warning CS8602: Possible dereference of a null reference.
                //         o = c.A.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.A").WithLocation(32, 13),
                // (32,13): warning CS8601: Possible null reference assignment.
                //         o = c.A.A; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.A.A").WithLocation(32, 13),
                // (33,13): warning CS8601: Possible null reference assignment.
                //         o = c.B.P; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.B.P").WithLocation(33, 13));
        }

        [Fact]
        public void ModifyMembers_Struct()
        {
            var source =
@"#pragma warning disable 0649
struct A
{
    internal object? F;
}
struct B
{
    internal A A;
    internal object? G;
}
class Program
{
    static void F()
    {
        object o;
        B b = new B();
        b.G = new object();
        b.A.F = new object();
        o = b.G; // 1
        o = b.A.F; // 1
        b.A = default(A);
        o = b.G; // 2
        o = b.A.F; // 2
        b = default(B);
        o = b.G; // 3
        o = b.A.F; // 3
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (23,13): warning CS8601: Possible null reference assignment.
                //         o = b.A.F; // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "b.A.F").WithLocation(23, 13),
                // (25,13): warning CS8601: Possible null reference assignment.
                //         o = b.G; // 3
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "b.G").WithLocation(25, 13),
                // (26,13): warning CS8601: Possible null reference assignment.
                //         o = b.A.F; // 3
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "b.A.F").WithLocation(26, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Handle struct properties that are not auto-properties.
        [Fact(Skip = "Struct property not auto-property")]
        public void ModifyMembers_StructPropertyExplicitAccessors()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    private object? _p;
    internal object? P { get { return _p; } set { _p = value; } }
}
class C
{
    S F;
    void M()
    {
        object o;
        o = F.P; // 1
        F.P = new object();
        o = F.P; // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,13): warning CS8601: Possible null reference assignment.
                //         o = F.P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "F.P").WithLocation(13, 13));
        }

        [Fact]
        public void ModifyMembers_StructProperty()
        {
            var source =
@"#pragma warning disable 0649
public struct S
{
    public object? P { get; set; }
}
class C
{
    S F;
    void M()
    {
        object o;
        o = F.P; // 1
        F.P = new object();
        o = F.P; // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8601: Possible null reference assignment.
                //         o = F.P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "F.P").WithLocation(12, 13));
        }

        [Fact]
        public void ModifyMembers_StructPropertyFromMetadata()
        {
            var source0 =
@"public struct S
{
    public object? P { get; set; }
}";
            var comp0 = CreateStandardCompilation(source0, parseOptions: TestOptions.Regular8);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"#pragma warning disable 0649
class C
{
    S F;
    void M()
    {
        object o;
        o = F.P; // 1
        F.P = new object();
        o = F.P; // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, references: new[] { ref0 });
            comp.VerifyDiagnostics(
                // (8,13): warning CS8601: Possible null reference assignment.
                //         o = F.P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "F.P").WithLocation(8, 13));
        }

        [Fact]
        public void ModifyMembers_ClassPropertyNoBackingField()
        {
            var source =
@"#pragma warning disable 0649
class C
{
    object? P { get { return null; } set { } }
    void M()
    {
        object o;
        o = P; // 1
        P = new object();
        o = P; // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,13): warning CS8601: Possible null reference assignment.
                //         o = P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P").WithLocation(8, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Handle struct properties that are not auto-properties.
        [Fact(Skip = "Struct property not auto-property")]
        public void ModifyMembers_StructPropertyNoBackingField()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal object? P { get { return null; } set { } }
}
class C
{
    S F;
    void M()
    {
        object o;
        o = F.P; // 1
        F.P = new object();
        o = F.P; // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8601: Possible null reference assignment.
                //         o = F.P; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "F.P").WithLocation(12, 13));
        }

        // Calling a method should reset the state for members.
        [Fact]
        public void CallMethod()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal object? P { get; set; }
}
class B
{
    internal A? Q { get; set; }
}
class Program
{
    static void M()
    {
        object o;
        B b = new B() { Q = new A() { P = new object() } };
        o = b.Q.P; // 1
        b.Q.P.ToString();
        o = b.Q.P; // 2
        b.Q.ToString();
        o = b.Q.P; // 3
        b = new B() { Q = new A() { P = new object() } };
        o = b.Q.P; // 4
        b.ToString();
        o = b.Q.P; // 5
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report warnings.
            comp.VerifyDiagnostics(/*...*/);
        }

        [Fact]
        public void ModifyMembers_Conditional()
        {
            // PROTOTYPE(NullableReferenceTypes): Modify
            // member in one branch of conditional.
        }

        [Fact]
        public void ModifyMembers_ByRef()
        {
            // PROTOTYPE(NullableReferenceTypes): Invalidate
            // by passing one of the members by reference.
        }

        [Fact]
        public void ModifyMembers_In()
        {
            // PROTOTYPE(NullableReferenceTypes): Members should not
            // be invalidated by passing container to `in` parameter.
        }

        [Fact]
        public void Interface()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of interface instance.
        }

        [Fact]
        public void GenericType()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of generic and constructed
            // classes, structs, interfaces. In particular, fields and properties of type T.
        }

        [Fact]
        public void Array()
        {
            // PROTOTYPE(NullableReferenceTypes): Test if (a != null) F(a.Length);
        }

        // Note, there are no fields or properties on an unconstrained type
        // parameter so this tests constrained type parameters only.
        [Fact]
        public void TypeParameter()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of type parameter
            // instance constrained to specific interface, class, or struct.
        }

        [Fact]
        public void ObjectInitializer()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal object? F1;
    internal object? F2;
}
class B
{
    internal A? G;
}
class Program
{
    static void F()
    {
        (new B() { G = new A() { F1 = new object() } }).G.F1.ToString();
        B b;
        b = new B() { G = new A() { F1 = new object() } };
        b.G.F1.ToString(); // 1
        b.G.F2.ToString(); // 1
        b = new B() { G = new A() { F2 = new object() } };
        b.G.F1.ToString(); // 2
        b.G.F2.ToString(); // 2
        b = new B() { G = new A() };
        b.G.F1.ToString(); // 3
        b.G.F2.ToString(); // 3
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F2").WithLocation(19, 9),
                // (21,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F1").WithLocation(21, 9),
                // (24,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F1").WithLocation(24, 9),
                // (25,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F2.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F2").WithLocation(25, 9));
        }

        [Fact]
        public void ObjectInitializer_Struct()
        {
            var source =
@"#pragma warning disable 0649
struct A
{
    internal object? F1;
    internal object? F2;
}
struct B
{
    internal A G;
}
class Program
{
    static void F()
    {
        (new B() { G = new A() { F1 = new object() } }).G.F1.ToString();
        B b;
        b = new B() { G = new A() { F1 = new object() } };
        b.G.F1.ToString(); // 1
        b.G.F2.ToString(); // 1
        b = new B() { G = new A() { F2 = new object() } };
        b.G.F1.ToString(); // 2
        b.G.F2.ToString(); // 2
        b = new B() { G = new A() };
        b.G.F1.ToString(); // 3
        b.G.F2.ToString(); // 3
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F2").WithLocation(19, 9),
                // (21,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F1").WithLocation(21, 9),
                // (24,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F1").WithLocation(24, 9),
                // (25,9): warning CS8602: Possible dereference of a null reference.
                //         b.G.F2.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.G.F2").WithLocation(25, 9));
        }

        [Fact]
        public void ObjectInitializer_Properties()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal object? P1 { get; set; }
    internal object? P2 { get; set; }
}
class B
{
    internal A? Q { get; set; }
}
class Program
{
    static void F()
    {
        (new B() { Q = new A() { P1 = new object() } }).Q.P1.ToString();
        B b;
        b = new B() { Q = new A() { P1 = new object() } };
        b.Q.P1.ToString(); // 1
        b.Q.P2.ToString(); // 1
        b = new B() { Q = new A() { P2 = new object() } };
        b.Q.P1.ToString(); // 2
        b.Q.P2.ToString(); // 2
        b = new B() { Q = new A() };
        b.Q.P1.ToString(); // 3
        b.Q.P2.ToString(); // 3
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         b.Q.P2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Q.P2").WithLocation(19, 9),
                // (21,9): warning CS8602: Possible dereference of a null reference.
                //         b.Q.P1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Q.P1").WithLocation(21, 9),
                // (24,9): warning CS8602: Possible dereference of a null reference.
                //         b.Q.P1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Q.P1").WithLocation(24, 9),
                // (25,9): warning CS8602: Possible dereference of a null reference.
                //         b.Q.P2.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Q.P2").WithLocation(25, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Support assignment of derived type instances.
        [Fact(Skip = "TODO")]
        public void ObjectInitializer_DerivedType()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal A? F;
}
class B : A
{
    internal object? G;
}
class Program
{
    static void Main()
    {
        A a;
        a = new B() { F = new A(), G = new object() };
        a.F.ToString(); // 1
        a = new A();
        a.F.ToString(); // 2
        a = new B() { F = new B() { F = new A() } };
        a.F.ToString(); // 3
        a.F.F.ToString(); // 3
        a = new B() { G = new object() };
        a.F.ToString(); // 4
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         a.F.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a.F").WithLocation(18, 9),
                // (20,9): warning CS8602: Possible dereference of a null reference.
                //         a.F.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a.F").WithLocation(20, 9),
                // (23,9): warning CS8602: Possible dereference of a null reference.
                //         a.F.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a.F").WithLocation(23, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Support assignment of derived type instances.
        [Fact(Skip = "TODO")]
        public void Assignment()
        {
            var source =
@"#pragma warning disable 0649
class A
{
    internal A? F;
}
class B : A
{
    internal object? G;
}
class Program
{
    static void Main()
    {
        B b = new B();
        A a;
        a = b;
        a.F.ToString(); // 1
        b.F = new A();
        a = b;
        a.F.ToString(); // 2
        b = new B() { F = new B() { F = new A() } };
        a = b;
        a.F.ToString(); // 3
        a.F.F.ToString(); // 3
        b = new B() { G = new object() };
        a = b;
        a.F.ToString(); // 4
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (17,9): warning CS8602: Possible dereference of a null reference.
                //         a.F.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a.F").WithLocation(17, 9),
                // (27,9): warning CS8602: Possible dereference of a null reference.
                //         a.F.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a.F").WithLocation(27, 9));
        }

        [Fact]
        public void DynamicInstance()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of dynamic instance.
        }

        [Fact]
        public void DynamicMembers()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members that have dynamic type.
        }

        [Fact]
        public void Tuple()
        {
            // PROTOTYPE(NullableReferenceTypes): Test tuple fields, including Rest.
            // Virtual field should be tracked as actual field. Checking one or assigning
            // to one should affect both.
        }

        [Fact]
        public void AnonymousType()
        {
            // PROTOTYPE(NullableReferenceTypes): Test anonymous type fields: classes and structs.
        }

        [Fact]
        public void LocalFunction()
        {
            // PROTOTYPE(NullableReferenceTypes): Test combinations of
            // checks and dereferences inside and outside local functions.
        }

        [Fact]
        public void ExplicitBackingFields()
        {
            // PROTOTYPE(NullableReferenceTypes): Test references within the
            // class to property and associated backing field. The two should be
            // tracked independently. Test for classes and structs.
        }

        [Fact]
        public void FieldCycle_01()
        {
            var source =
@"class C
{
    C? F;
    void M()
    {
        F = this;
        F.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F.F").WithLocation(7, 9));
        }

        [Fact]
        public void FieldCycle_02()
        {
            var source =
@"class C
{
    C? F;
    void M()
    {
        F = new C() { F = this };
        F.F.ToString();
        F.F.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F.F.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F.F.F").WithLocation(8, 9));
        }

        [Fact]
        public void FieldCycle_03()
        {
            var source =
@"class C
{
    C? F;
    static void M()
    {
        var x = new C();
        x.F = x;
        var y = new C() { F = x };
        y.F.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         y.F.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y.F.F").WithLocation(9, 9));
        }

        [Fact]
        public void FieldCycle_Struct()
        {
            var source =
@"struct S
{
    internal C F;
    internal C? G;
}
class C
{
    internal S S;
    static void Main()
    {
        var s = new S() { F = new C(), G = new C() };
        s.F.S = s;
        s.G.S = s;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // Valid struct since the property is not backed by a field.
        // PROTOTYPE(NullableReferenceTypes): Handle struct properties that are not auto-properties.
        [Fact(Skip = "Struct property not auto-property")]
        public void PropertyCycle_Struct()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal S(object? f)
    {
        F = f;
    }
    internal object? F;
    internal S P
    {
        get { return new S(F); }
        set { F = value.F; }
    }
}
class C
{
    static void M(S s)
    {
        s.P.F.ToString(); // 1
        if (s.P.F == null) return;
        s.P.F.ToString(); // 2
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         s.P.F.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.P.F").WithLocation(19, 9));
        }
    }
}
