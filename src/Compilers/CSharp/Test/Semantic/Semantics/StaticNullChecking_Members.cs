// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            // PROTOTYPE(NullableReferenceTypes): As above with properties.
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
        public void Interface()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of interface instance.
        }

        [Fact]
        public void GenericType()
        {
            // PROTOTYPE(NullableReferenceTypes): Test members of generic and constructed
            // classes, structs, interfaces.
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
    }
}
