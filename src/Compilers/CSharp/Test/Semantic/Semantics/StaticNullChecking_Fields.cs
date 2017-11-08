// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking_Fields : CSharpTestBase
    {
        [Fact]
        public void NoNonNullWarnings_CSharp7()
        {
            var source =
@"class C
{
    internal object F;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (3,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(3, 21));
        }

        [Fact]
        public void ReadWriteFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
#pragma warning disable 0649
    private object F1;
    internal object? F2;
    internal object?[] F3;
    private object[]? F4;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8618: Non-nullable field 'F3' is uninitialized.
                // class C
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(1, 7),
                // (1,7): warning CS8618: Non-nullable field 'F1' is uninitialized.
                // class C
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(1, 7));
        }

        [Fact]
        public void ReadWriteFields_ExplicitConstructors()
        {
            var source =
@"class C
{
    internal object F1;
    private object? F2;
    private object?[] F3;
    internal object[]? F4;
    internal C()
    {
    }
    internal C(object o)
    {
        F1 = o;
        F3 = new[] { o, null };
    }
    internal C(object x, object y)
    {
        F2 = x;
        F4 = new[] { x, y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(15, 14));
        }

        [Fact]
        public void ReadOnlyFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
#pragma warning disable 0649
    private readonly object F1;
    internal readonly object? F2;
    internal readonly object?[] F3;
    private readonly object[]? F4;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8618: Non-nullable field 'F3' is uninitialized.
                // class C
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(1, 7),
                // (1,7): warning CS8618: Non-nullable field 'F1' is uninitialized.
                // class C
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(1, 7));
        }

        [Fact]
        public void ReadOnlyFields_ExplicitConstructors()
        {
            var source =
@"class C
{
    internal readonly object F1;
    private readonly object? F2;
    private readonly object?[] F3;
    internal readonly object[]? F4;
    internal C()
    {
    }
    internal C(object o)
    {
        F1 = o;
        F3 = new[] { o, null };
    }
    internal C(object x, object y)
    {
        F2 = x;
        F4 = new[] { x, y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(15, 14));
        }

        [Fact]
        public void FieldInitializers_DefaultConstructor()
        {
            var source =
@"class C
{
    private object F1 = new object();
    internal object? F2 = new object();
    internal object?[] F3 = new [] { new object(), null };
    private object[]? F4 = new [] { new object() };
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Report warnings for static fields.
        [Fact(Skip = "Static fields")]
        public void StaticFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F1;
    private static object F2 = new object();
    private readonly static object F3;
    private readonly static object F4 = new object();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,12): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(8, 12),
                // (8,12): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(8, 12));
        }

        // PROTOTYPE(NullableReferenceTypes): Report warnings for static fields.
        [Fact(Skip = "Static fields")]
        public void StaticFields_ExplicitConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F1;
    private static object F2;
    private readonly static object F3;
    private readonly static object F4;
    static C()
    {
        F2 = new object();
        F4 = new object();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,12): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(8, 12),
                // (8,12): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(8, 12));
        }

        // Each constructor is handled in isolation.
        [Fact]
        public void ChainedConstructors()
        {
            var source =
@"class C
{
    private object F1;
    private object F2;
    private readonly object F3;
    private object F4 = new object();
    private C()
    {
        F1 = new object();
    }
    internal C(object x) : this()
    {
        F2 = x;
    }
    internal C(object x, object y) : this(x)
    {
        F3 = y;
    }
    internal C(object x, object y, object z) : this()
    {
        F3 = z;
    }
    internal C(object x, object y, string z) : base()
    {
        F3 = z;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(7, 13),
                // (7,13): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 13),
                // (23,14): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     internal C(object x, object y, string z) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(23, 14),
                // (23,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C(object x, object y, string z) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(23, 14));
        }

        [Fact]
        public void ReadWriteAutoProperties_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; set; }
    internal object? P2 { get; set; }
    internal object?[] P3 { get; set; }
    private object[]? P4 { get; set; }
    internal C()
    {
    }
    internal C(object o)
    {
        P1 = o;
        P3 = new[] { o,  null };
    }
    internal C(object x, object y)
    {
        P2 = x;
        P4 = new[] { x, y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(15, 14));
        }

        [Fact]
        public void ReadOnlyAutoProperties_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; }
    internal object? P2 { get; }
    internal object?[] P3 { get; }
    private object[]? P4 { get; }
    internal C()
    {
    }
    internal C(object o)
    {
        P1 = o;
        P3 = new[] { o,  null };
    }
    internal C(object x, object y)
    {
        P2 = x;
        P4 = new[] { x, y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(15, 14));
        }

        [Fact]
        public void AutoPropertyInitializers_DefaultConstructor()
        {
            var source =
@"class C
{
    private object P1 { get; } = new object();
    internal object P2 { get; set; } = new object();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AutoPropertyInitializers_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; } = new object();
    internal object?[] P2 { get; } = new object?[0];
    internal object P3 { get; set; } = new object();
    private object?[] P4 { get; set; } = new object?[0];
    internal C(object o)
    {
        P1 = o;
        P2 = new object?[] { o };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Properties_ExplicitImplementations()
        {
            var source =
@"using System;
class C
{
    private object P1 { get { throw new NotImplementedException(); } }
    private object P3 { set { } }
    private object[] P2 { get { throw new NotImplementedException(); } set { } }
    internal C()
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Events_ExplicitConstructors()
        {
            // PROTOTYPE(NullableReferenceTypes): Handle events.
        }

        [Fact]
        public void GenericType()
        {
            var source =
@"class C<T>
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T F2;
    private C(T t)
    {
        F1 = t;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GenericType_ClassConstraint()
        {
            var source =
@"class C<T> where T : class
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T? F2;
    private C()
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(6, 13));
        }

        [Fact]
        public void GenericType_StructConstraint()
        {
            var source =
@"class C<T> where T : struct
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T? F2;
    private C()
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Tuple()
        {
            var source =
@"class C
{
#pragma warning disable 0649
    internal readonly (object A, object B) F1;
    internal readonly (object? A, object) F2;
    internal readonly (object, object? B) F3;
    internal readonly (object?, object?) F4;
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotInitializedInAllPaths_Class()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; }
    internal C(string s)
    {
        if (s.Length > 0)
            F = s;
        else
            P = new [] { s };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,14): warning CS8618: Non-nullable property 'P' is uninitialized.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P").WithLocation(6, 14),
                // (6,14): warning CS8618: Non-nullable field 'F' is uninitialized.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F").WithLocation(6, 14));
        }

        [Fact]
        public void NotInitializedInAllPaths_Struct()
        {
            var source =
@"struct S
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; set; }
    internal S(string s)
    {
        if (s.Length > 0)
            F = s;
        else
            P = new [] { s };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,14): error CS0843: Auto-implemented property 'S.P' must be fully assigned before control is returned to the caller.
                //     internal S(string s)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S").WithArguments("S.P").WithLocation(6, 14),
                // (6,14): error CS0171: Field 'S.F' must be fully assigned before control is returned to the caller
                //     internal S(string s)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S").WithArguments("S.F").WithLocation(6, 14),
                // (6,14): warning CS8618: Non-nullable property 'P' is uninitialized.
                //     internal S(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "P").WithLocation(6, 14),
                // (6,14): warning CS8618: Non-nullable field 'F' is uninitialized.
                //     internal S(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "F").WithLocation(6, 14));
        }

        [Fact]
        public void EmptyStruct()
        {
            var source =
@"struct S
{
    S(object o)
    {
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Struct assign `this`.
        [Fact(Skip = "Assign this")]
        public void StructAssignThis()
        {
            var source =
@"struct S
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; set; }
    internal S(S s)
    {
        this = s;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StructObjectCreation()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal string F;
    internal string G;
}
class C
{
    static void F(S s) { }
    static void Main()
    {
        F(new S());
        F(new S() { F = string.Empty });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ValueTypeFields()
        {
            var source =
@"#pragma warning disable 0169
struct S { }
class C
{
    private readonly S s;
    private int i;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TryCatch()
        {
            // PROTOTYPE(NullableReferenceTypes):
        }

        [Fact]
        public void LocalFunction()
        {
            var source =
@"#pragma warning disable 0169
class C
{
    private object F;
    private object G;
    C()
    {
        void L(object o)
        {
            F = o;
        }
        L(new object());
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,5): warning CS8618: Non-nullable field 'G' is uninitialized.
                //     C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "G").WithLocation(6, 5));
        }
    }
}
