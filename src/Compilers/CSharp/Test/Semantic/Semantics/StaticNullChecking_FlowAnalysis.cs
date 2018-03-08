// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Nullable
{
    public class StaticNullChecking_FlowAnalysis : CSharpTestBase
    {
        [Fact]
        public void ImplicitlyTypedArrayCreation_01()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        var a = new[] { x, x };
        a.ToString();
        a[0].ToString();
        var b = new[] { x, y };
        b.ToString();
        b[0].ToString();
        var c = new[] { b };
        c[0].ToString();
        c[0][0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(10, 9),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         c[0][0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c[0][0]").WithLocation(13, 9));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_02()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        var a = new[] { x };
        a[0].ToString();
        var b = new[] { y };
        b[0].ToString();
        var c = new[] { a, b };
        c[0][0].ToString();
        var d = new[] { a, b! };
        d[0][0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(8, 9),
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         c[0][0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c[0][0]").WithLocation(10, 9));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_03()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        (new[] { x, x })[1].ToString();
        (new[] { y, x })[1].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, x })[1].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, x })[1]").WithLocation(6, 9));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_04()
        {
            var source =
@"class C
{
    static void F()
    {
        object? o = new object();
        var a = new[] { o };
        a[0].ToString();
        var b = new[] { a };
        b[0][0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_05()
        {
            var source =
@"class C
{
    static void F(int n)
    {
        object? o = new object();
        while (n-- > 0)
        {
            var a = new[] { o };
            a[0].ToString();
            var b = new[] { a };
            b[0][0].ToString();
            o = null;
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             a[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a[0]").WithLocation(9, 13),
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             b[0][0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0][0]").WithLocation(11, 13));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_06()
        {
            var source =
@"class C
{
    static void F(string s)
    {
        var a = new[] { new object(), (string)null };
        a[0].ToString();
        var b = new[] { (object)null, s };
        b[0].ToString();
        var c = new[] { s, (object)null };
        c[0].ToString();
        var d = new[] { (string)null, new object() };
        d[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         a[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a[0]").WithLocation(6, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(8, 9),
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         c[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c[0]").WithLocation(10, 9),
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         d[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "d[0]").WithLocation(12, 9));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_07()
        {
            var source =
@"#pragma warning disable 0649
class A<T>
{
    internal T F;
}
class B1 : A<object?> { }
class B2 : A<object> { }
class C
{
    static void F()
    {
        var a = new[] { new A<object>(), new B1() };
        a[0].F.ToString();
        var b = new[] { new A<object?>(), new B2() };
        b[0].F.ToString();
        var c = new[] { new B1(), new A<object>() };
        c[0].F.ToString();
        var d = new[] { new B2(), new A<object?>() };
        d[0].F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,42): warning CS8619: Nullability of reference types in value of type 'B1' doesn't match target type 'A<object>'.
                //         var a = new[] { new A<object>(), new B1() };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new B1()").WithArguments("B1", "A<object>").WithLocation(12, 42),
                // (14,43): warning CS8619: Nullability of reference types in value of type 'B2' doesn't match target type 'A<object?>'.
                //         var b = new[] { new A<object?>(), new B2() };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new B2()").WithArguments("B2", "A<object?>").WithLocation(14, 43),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0].F").WithLocation(15, 9),
                // (16,25): warning CS8619: Nullability of reference types in value of type 'B1' doesn't match target type 'A<object>'.
                //         var c = new[] { new B1(), new A<object>() };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new B1()").WithArguments("B1", "A<object>").WithLocation(16, 25),
                // (18,25): warning CS8619: Nullability of reference types in value of type 'B2' doesn't match target type 'A<object?>'.
                //         var d = new[] { new B2(), new A<object?>() };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new B2()").WithArguments("B2", "A<object?>").WithLocation(18, 25),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         d[0].F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "d[0].F").WithLocation(19, 9));
        }

        // PROTOTYPE(NullableReferenceType): The array element type should be nullable,
        // even though there is no best type when considering nullability for C<object>? and
        // C<object?>. In short, should report WRN_NullReferenceReceiver for `c[0].ToString()`
        [Fact]
        public void ImplicitlyTypedArrayCreation_08()
        {
            var source =
@"class C<T> { }
class C
{
    static void F(C<object>? a, C<object?> b)
    {
        if (a == null)
        {
            var c = new[] { a, b };
            c[0].ToString();
            var d = new[] { b, a };
            d[0].ToString();
        }
        else
        {
            var c = new[] { a, b };
            c[0].ToString();
            var d = new[] { b, a };
            d[0].ToString();
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,32): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //             var c = new[] { a, b };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("C<object?>", "C<object>").WithLocation(8, 32),
                // (10,32): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //             var d = new[] { b, a };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "a").WithArguments("C<object>", "C<object?>").WithLocation(10, 32),
                // (15,32): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //             var c = new[] { a, b };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("C<object?>", "C<object>").WithLocation(15, 32),
                // (17,32): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //             var d = new[] { b, a };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "a").WithArguments("C<object>", "C<object?>").WithLocation(17, 32));
        }

        [Fact]
        public void ImplicitlyTypedArrayCreation_09()
        {
            var source =
@"class C
{
    static void F(C x, Unknown? y)
    {
        var a = new[] { x, y };
        a[0].ToString();
        var b = new[] { y, x };
        b[0].ToString();
    }
    static void G(C? x, Unknown y)
    {
        var a = new[] { x, y };
        a[0].ToString();
        var b = new[] { y, x };
        b[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,25): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void G(C? x, Unknown y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(10, 25),
                // (3,24): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F(C x, Unknown? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(3, 24),
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         a[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a[0]").WithLocation(6, 9));
        }

        [Fact]
        public void ExplicitlyTypedArrayCreation()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        var a = new object[] { x, y };
        a[0].ToString();
        var b = new object?[] { x, y };
        b[0].ToString();
        var c = new object[] { x, y! };
        c[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,35): warning CS8601: Possible null reference assignment.
                //         var a = new object[] { x, y };
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y").WithLocation(5, 35),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(8, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_01()
        {
            var source =
@"class C
{
    static void F(bool b, object x, object? y)
    {
        var z = b ? x : y;
        z.ToString();
        var w = b ? y : x;
        w.ToString();
        var v = true ? y : x;
        v.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(6, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         w.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "w").WithLocation(8, 9),
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         v.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "v").WithLocation(10, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_02()
        {
            var source =
@"class C
{
    static void F(bool b, object x, object? y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
        if (y != null) (b ? x : y).ToString();
        if (y != null) (b ? y : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(5, 10),
                // (6,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(6, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_03()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        (false ? x : y).ToString();
        (false ? y : x).ToString();
        (true ? x : y).ToString();
        (true ? y : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (false ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "false ? x : y").WithLocation(5, 10),
                // (8,10): warning CS8602: Possible dereference of a null reference.
                //         (true ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "true ? y : x").WithLocation(8, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_04()
        {
            var source =
@"class C
{
    static void F(bool b, object x, string? y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
    static void G(bool b, object? x, string y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(5, 10),
                // (6,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(6, 10),
                // (10,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(10, 10),
                // (11,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(11, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Should report nullability mismatch warnings.
        [Fact]
        public void ConditionalOperator_05()
        {
            var source =
@"#pragma warning disable 0649
class A<T>
{
    internal T F;
    internal T P { get; }
}
class B1 : A<object?> { }
class B2 : A<object> { }
class C
{
    static void F(bool b, A<object> x, B1 y)
    {
        (b ? x : y).F.ToString();
        (b ? y : x).P.ToString();
    }
    static void G(bool b, A<object?> x, B2 y)
    {
        (b ? x : y).F.ToString();
        (b ? y : x).P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? x : y).F").WithLocation(18, 9),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? y : x).P").WithLocation(19, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_06()
        {
            var source =
@"class C
{
    static void F(bool b, object x, string? y)
    {
        (b ? null : x).ToString();
        (b ? null : y).ToString();
        (b ? x: null).ToString();
        (b ? y: null).ToString();
        (b ? null: null).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between '<null>' and '<null>'
                //         (b ? null: null).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? null: null").WithArguments("<null>", "<null>").WithLocation(9, 10),
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? null : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? null : x").WithLocation(5, 10),
                // (6,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? null : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? null : y").WithLocation(6, 10),
                // (7,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x: null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x: null").WithLocation(7, 10),
                // (8,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y: null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y: null").WithLocation(8, 10),
                // (9,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? null: null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? null: null").WithLocation(9, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_07()
        {
            var source =
@"class C
{
    static void F(bool b, Unknown x, Unknown? y)
    {
        (b ? null : x).ToString();
        (b ? null : y).ToString();
        (b ? x: null).ToString();
        (b ? y: null).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,27): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F(bool b, Unknown x, Unknown? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(3, 27),
                // (3,38): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F(bool b, Unknown x, Unknown? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(3, 38),
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? null : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? null : x").WithLocation(5, 10),
                // (6,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? null : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? null : y").WithLocation(6, 10),
                // (7,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x: null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x: null").WithLocation(7, 10),
                // (8,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y: null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y: null").WithLocation(8, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_08()
        {
            var source =
@"class C
{
    static void F1(bool b, UnknownA x, UnknownB y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
    static void F2(bool b, UnknownA? x, UnknownB y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
    static void F3(bool b, UnknownA? x, UnknownB? y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,28): error CS0246: The type or namespace name 'UnknownA' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F1(bool b, UnknownA x, UnknownB y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownA").WithArguments("UnknownA").WithLocation(3, 28),
                // (3,40): error CS0246: The type or namespace name 'UnknownB' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F1(bool b, UnknownA x, UnknownB y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownB").WithArguments("UnknownB").WithLocation(3, 40),
                // (8,28): error CS0246: The type or namespace name 'UnknownA' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F2(bool b, UnknownA? x, UnknownB y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownA").WithArguments("UnknownA").WithLocation(8, 28),
                // (8,41): error CS0246: The type or namespace name 'UnknownB' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F2(bool b, UnknownA? x, UnknownB y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownB").WithArguments("UnknownB").WithLocation(8, 41),
                // (13,28): error CS0246: The type or namespace name 'UnknownA' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F3(bool b, UnknownA? x, UnknownB? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownA").WithArguments("UnknownA").WithLocation(13, 28),
                // (13,41): error CS0246: The type or namespace name 'UnknownB' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F3(bool b, UnknownA? x, UnknownB? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownB").WithArguments("UnknownB").WithLocation(13, 41),
                // (10,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(10, 10),
                // (11,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(11, 10),
                // (15,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(15, 10),
                // (16,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(16, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_09()
        {
            var source =
@"struct A { }
struct B { }
class C
{
    static void F1(bool b, A x, B y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
    static void F2(bool b, A x, C y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
    static void F3(bool b, B x, C? y)
    {
        (b ? x : y).ToString();
        (b ? y : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'A' and 'B'
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? x : y").WithArguments("A", "B").WithLocation(7, 10),
                // (8,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'B' and 'A'
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? y : x").WithArguments("B", "A").WithLocation(8, 10),
                // (12,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'A' and 'C'
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? x : y").WithArguments("A", "C").WithLocation(12, 10),
                // (13,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'C' and 'A'
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? y : x").WithArguments("C", "A").WithLocation(13, 10),
                // (17,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'B' and 'C'
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? x : y").WithArguments("B", "C").WithLocation(17, 10),
                // (18,10): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'C' and 'B'
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? y : x").WithArguments("C", "B").WithLocation(18, 10),
                // (17,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : y").WithLocation(17, 10),
                // (18,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? y : x").WithLocation(18, 10));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void ConditionalOperator_10()
        {
            var source =
@"using System;
class C
{
    static void F(bool b, object? x, object y)
    {
        (b ? x : throw new Exception()).ToString();
        (b ? y : throw new Exception()).ToString();
        (b ? throw new Exception() : x).ToString();
        (b ? throw new Exception() : y).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : throw new Exception()).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? x : throw new Exception()").WithLocation(6, 10),
                // (8,10): warning CS8602: Possible dereference of a null reference.
                //         (b ? throw new Exception() : x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b ? throw new Exception() : x").WithLocation(8, 10));
        }

        [Fact]
        public void ConditionalOperator_11()
        {
            var source =
@"class C
{
    static void F(bool b, object x)
    {
        (b ? x : throw null).ToString();
        (b ? throw null : x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: NullCoalescingOperator
        [Fact(Skip = "TODO")]
        public void NullCoalescingOperator_01()
        {
            var source =
@"class C
{
    static void F(object? x, object? y)
    {
        var z = x ?? y;
        z.ToString();
        if (y == null) return;
        var w = x ?? y;
        w.ToString();
        var v = null ?? x;
        v.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(6, 9),
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         v.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "v").WithLocation(11, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: NullCoalescingOperator
        [Fact(Skip = "TODO")]
        public void NullCoalescingOperator_02()
        {
            var source =
@"class C
{
    static void F(object? x, object? y)
    {
        (x ?? y).ToString();
        if (y != null) (x ?? y).ToString();
        if (y != null) (y ?? x).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,10): warning CS8602: Possible dereference of a null reference.
                //         (x ?? y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x ?? y").WithLocation(5, 10),
                // (7,25): hidden CS8607: Expression is probably never null.
                //         if (y != null) (y ?? x).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y").WithLocation(7, 25));
        }

        [Fact]
        public void NullCoalescingOperator_03()
        {
            var source =
@"class C
{
    static void F(object x, object? y)
    {
        (null ?? null).ToString();
        (null ?? x).ToString();
        (null ?? y).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,10): error CS0019: Operator '??' cannot be applied to operands of type '<null>' and '<null>'
                //         (null ?? null).ToString();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null ?? null").WithArguments("??", "<null>", "<null>").WithLocation(5, 10),
                // (7,10): warning CS8602: Possible dereference of a null reference.
                //         (null ?? y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "null ?? y").WithLocation(7, 10));
        }

        [Fact]
        public void NullCoalescingOperator_04()
        {
            var source =
@"class C
{
    static void F(string x, string? y)
    {
        ("""" ?? x).ToString();
        ("""" ?? y).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCoalescingOperator_05()
        {
            var source =
@"class C
{
    static void F(C x, Unknown? y)
    {
        (x ?? y).ToString();
        (y ?? x).ToString();
        (null ?? y).ToString();
        (y ?? null).ToString();
    }
    static void G(C? x, Unknown y)
    {
        (x ?? y).ToString();
        (y ?? x).ToString();
        (null ?? y).ToString();
        (y ?? null).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,25): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void G(C? x, Unknown y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(10, 25),
                // (3,24): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F(C x, Unknown? y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(3, 24),
                // (5,10): hidden CS8607: Expression is probably never null.
                //         (x ?? y).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x").WithLocation(5, 10),
                // (7,10): warning CS8602: Possible dereference of a null reference.
                //         (null ?? y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "null ?? y").WithLocation(7, 10),
                // (13,10): hidden CS8607: Expression is probably never null.
                //         (y ?? x).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y").WithLocation(13, 10),
                // (15,10): hidden CS8607: Expression is probably never null.
                //         (y ?? null).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y").WithLocation(15, 10));
        }

        [Fact]
        public void NullCoalescingOperator_06()
        {
            var source =
@"class C
{
    static void F(object? o, object[]? a, object?[]? b)
    {
        if (o == null)
        {
            var c = new[] { o };
            (a ?? c)[0].ToString();
            (b ?? c)[0].ToString();
        }
        else
        {
            var c = new[] { o };
            (a ?? c)[0].ToString();
            (b ?? c)[0].ToString();
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,19): warning CS8619: Nullability of reference types in value of type 'object?[]' doesn't match target type 'object[]'.
                //             (a ?? c)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c").WithArguments("object?[]", "object[]").WithLocation(8, 19),
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             (b ?? c)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ?? c)[0]").WithLocation(9, 13),
                // (15,19): warning CS8619: Nullability of reference types in value of type 'object[]' doesn't match target type 'object?[]'.
                //             (b ?? c)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c").WithArguments("object[]", "object?[]").WithLocation(15, 19),
                // (15,13): warning CS8602: Possible dereference of a null reference.
                //             (b ?? c)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ?? c)[0]").WithLocation(15, 13));
        }

        [Fact]
        public void NullCoalescingOperator_07()
        {
            var source =
@"interface I<T> { }
class C
{
    static object? F((I<object>, I<object?>)? x, (I<object?>, I<object>)? y)
    {
        return x ?? y;
    }
    static object F((I<object>, I<object?>)? x, (I<object?>, I<object>) y)
    {
        return x ?? y;
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,21): warning CS8619: Nullability of reference types in value of type '(I<object?>, I<object>)?' doesn't match target type '(I<object>, I<object?>)?'.
                //         return x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("(I<object?>, I<object>)?", "(I<object>, I<object?>)?").WithLocation(6, 21),
                // (10,21): warning CS8619: Nullability of reference types in value of type '(I<object?>, I<object>)' doesn't match target type '(I<object>, I<object?>)'.
                //         return x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("(I<object?>, I<object>)", "(I<object>, I<object?>)").WithLocation(10, 21));
        }

        [Fact]
        public void AnonymousObjectCreation_01()
        {
            var source =
@"class C
{
    static void F(object? o)
    {
        (new { P = o }).P.ToString();
        if (o == null) return;
        (new { Q = o }).Q.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,9): warning CS8602: Possible dereference of a null reference.
                //         (new { P = o }).P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new { P = o }).P").WithLocation(5, 9));
        }

        // PROTOTYPE(NullableReferenceType): NullableWalker.VisitAnonymousObjectCreationExpression
        // should support initializers with inferred nullability.
        [Fact(Skip = "TODO")]
        public void AnonymousObjectCreation_02()
        {
            var source =
@"class C
{
    static void F(object? o)
    {
        (new { P = new[] { o }}).P[0].ToString();
        if (o == null) return;
        (new { Q = new[] { o }}).Q[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,9): warning CS8602: Possible dereference of a null reference.
                //         (new { P = new[] { o }}).P[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new { P = new[] { o }}).P[0]").WithLocation(5, 9));
        }

        [Fact]
        public void ObjectInitializer_01()
        {
            var source =
@"class C
{
    C(object o) { }
    static void F(object? x)
    {
        var y = new C(x);
        if (x != null) y = new C(x);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,23): warning CS8604: Possible null reference argument for parameter 'o' in 'C.C(object o)'.
                //         var y = new C(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("o", "C.C(object o)").WithLocation(6, 23));
        }

        [Fact]
        public void ObjectInitializer_02()
        {
            var source =
@"class A
{
    internal B F = new B();
}
class B
{
    internal object? G;
}
class C
{
    static void Main()
    {
        var o = new A() { F = { G = new object() } };
        o.F.G.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Track nullability through deconstruction assignment.
        [Fact(Skip = "TODO")]
        public void DeconstructionTypeInference()
        {
            var source =
@"class C
{
    static void F((object? a, object? b) t)
    {
        if (t.b == null) return;
        object? x;
        object? y;
        (x, y) = t;
        x.ToString();
        y.ToString();
    }
    static void F(object? a, object? b)
    {
        if (b == null) return;
        object? x;
        object? y;
        (x, y) = (a, b);
        x.ToString();
        y.ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 9),
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(18, 9));
        }

        [Fact]
        public void DynamicInvocation()
        {
            var source =
@"class C
{
    static void F(object x, object y)
    {
    }
    static void G(object? x, dynamic y)
    {
        F(x, y);
        if (x != null) F(y, x);
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): We should be able to report warnings
            // when all applicable methods agree on the nullability of particular parameters.
            // (For instance, x in F(x, y) above.)
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicObjectCreation_01()
        {
            var source =
@"class C
{
    C(object x, object y)
    {
    }
    static void G(object? x, dynamic y)
    {
        var o = new C(x, y);
        if (x != null) o = new C(y, x);
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): We should be able to report warnings
            // when all applicable methods agree on the nullability of particular parameters.
            // (For instance, x in F(x, y) above.)
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicObjectCreation_02()
        {
            var source =
@"class C
{
    C(object f)
    {
        F = f;
    }
    object? F;
    object? G;
    static void M(dynamic d)
    {
        var o = new C(d) { G = new object() };
        o.G.ToString();
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_01()
        {
            var source =
@"class C<T>
{
    internal T F;
}
class C
{
    static C<T> Create<T>(T t)
    {
        return new C<T>();
    }
    static void F(object? x)
    {
        if (x == null)
        {
            Create(x).F = null;
            var y = Create(x);
            y.F = null;
        }
        else
        {
            Create(x).F = null;
            var y = Create(x);
            y.F = null;
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (21,27): warning CS8600: Cannot convert null to non-nullable reference.
                //             Create(x).F = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(21, 27),
                // (23,19): warning CS8600: Cannot convert null to non-nullable reference.
                //             y.F = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(23, 19));
        }

        [Fact]
        public void TypeInference_ArgumentOrder()
        {
            var source =
@"interface I<T>
{
     T P { get; }
}
class C
{
    static T F<T, U>(I<T> x, I<U> y) => x.P;
    static void M(I<object?> x, I<string> y)
    {
        F(y: y, x: x).ToString();
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(y: y, x: x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y: y, x: x)").WithLocation(10, 9));
        }

        [Fact]
        public void LambdaReturnValue_01()
        {
            var source =
@"using System;
class C
{
    static void F(Func<object> f)
    {
    }
    static void G(string x, object? y)
    {
        F(() => { if ((object)x == y) return x; return y; });
        F(() => { if (y == null) return x; return y; });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,56): warning CS8603: Possible null reference return.
                //         F(() => { if ((object)x == y) return x; return y; });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y").WithLocation(9, 56));
        }

        [Fact]
        public void LambdaReturnValue_02()
        {
            var source =
@"using System;
class C
{
    static void F(Func<object> f)
    {
    }
    static void G(bool b, object x, string? y)
    {
        F(() => { if (b) return x; return y; });
        F(() => { if (b) return y; return x; });
    }
    static void H(bool b, object? x, string y)
    {
        F(() => { if (b) return x; return y; });
        F(() => { if (b) return y; return x; });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,43): warning CS8603: Possible null reference return.
                //         F(() => { if (b) return x; return y; });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y").WithLocation(9, 43),
                // (10,33): warning CS8603: Possible null reference return.
                //         F(() => { if (b) return y; return x; });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y").WithLocation(10, 33),
                // (14,33): warning CS8603: Possible null reference return.
                //         F(() => { if (b) return x; return y; });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x").WithLocation(14, 33),
                // (15,43): warning CS8603: Possible null reference return.
                //         F(() => { if (b) return y; return x; });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x").WithLocation(15, 43));
        }

        // PROTOTYPE(NullableReferenceTypes): Infer lambda return type nullability.
        [Fact(Skip = "TODO")]
        public void LambdaReturnValue_03()
        {
            var source =
@"using System;
class C
{
    static T F<T>(Func<T> f)
    {
        return default(T);
    }
    static void G(bool b, object x, string? y)
    {
        F(() => { if (b) return x; return y; }).ToString();
    }
    static void H(bool b, object? x, string y)
    {
        F(() => { if (b) return x; return y; }).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => { if (b) return x; return y; }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => { if (b) return x; return y; })").WithLocation(10, 9),
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => { if (b) return x; return y; }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => { if (b) return x; return y; })").WithLocation(14, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Infer lambda return type nullability.
        [Fact(Skip = "TODO")]
        public void LambdaReturnValue_04()
        {
            var source =
@"using System;
class C
{
    static T F<T>(Func<T> f)
    {
        return default(T);
    }
    static void G(object? o)
    {
        F(() => o).ToString();
        if (o != null) F(() => o).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => o).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => o)").WithLocation(10, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Infer lambda return type nullability.
        [Fact(Skip = "TODO")]
        public void LambdaReturnValue_05()
        {
            var source =
@"using System;
class C
{
    static T F<T>(Func<object?, T> f)
    {
        return default(T);
    }
    static void G()
    {
        F(o => { if (o == null) throw new ArgumentException(); return o; }).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IdentityConversion_ArrayInitializer()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object> x, I<object?> y, I<object>? z, I<object?>? w)
    {
        (new[] { x, y })[0].ToString(); // A1
        (new[] { x, z })[0].ToString(); // A2
        (new[] { x, w })[0].ToString(); // A3
        (new[] { y, z })[0].ToString(); // A4
        (new[] { y, w })[0].ToString(); // A5
        (new[] { w, z })[0].ToString(); // A6
    }
    static void F(IIn<object> x, IIn<object?> y, IIn<object>? z, IIn<object?>? w)
    {
        (new[] { x, y })[0].ToString(); // B1
        (new[] { x, z })[0].ToString(); // B2
        (new[] { x, w })[0].ToString(); // B3
        (new[] { y, z })[0].ToString(); // B4
        (new[] { y, w })[0].ToString(); // B5
        (new[] { w, z })[0].ToString(); // B6
    }
    static void F(IOut<object> x, IOut<object?> y, IOut<object>? z, IOut<object?>? w)
    {
        (new[] { x, y })[0].ToString(); // C1
        (new[] { x, z })[0].ToString(); // C2
        (new[] { x, w })[0].ToString(); // C3
        (new[] { y, z })[0].ToString(); // C4
        (new[] { y, w })[0].ToString(); // C5
        (new[] { w, z })[0].ToString(); // C6
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,21): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         (new[] { x, y })[0].ToString(); // A1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(8, 21),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, z })[0].ToString(); // A2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, z })[0]").WithLocation(9, 9),
                // (10,21): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         (new[] { x, w })[0].ToString(); // A3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "w").WithArguments("I<object?>", "I<object>").WithLocation(10, 21),
                // (11,21): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         (new[] { y, z })[0].ToString(); // A4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "z").WithArguments("I<object>", "I<object?>").WithLocation(11, 21),
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, w })[0].ToString(); // A5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, w })[0]").WithLocation(12, 9),
                // (13,21): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         (new[] { w, z })[0].ToString(); // A6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "z").WithArguments("I<object>", "I<object?>").WithLocation(13, 21),
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, z })[0].ToString(); // B2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, z })[0]").WithLocation(18, 9),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, w })[0].ToString(); // B3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, w })[0]").WithLocation(19, 9),
                // (20,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, z })[0].ToString(); // B4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, z })[0]").WithLocation(20, 9),
                // (21,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, w })[0].ToString(); // B5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, w })[0]").WithLocation(21, 9),
                // (22,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { w, z })[0].ToString(); // B6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { w, z })[0]").WithLocation(22, 9),
                // (27,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, z })[0].ToString(); // C2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, z })[0]").WithLocation(27, 9),
                // (28,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, w })[0].ToString(); // C3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, w })[0]").WithLocation(28, 9),
                // (29,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, z })[0].ToString(); // C4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, z })[0]").WithLocation(29, 9),
                // (30,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, w })[0].ToString(); // C5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, w })[0]").WithLocation(30, 9),
                // (31,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { w, z })[0].ToString(); // C6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { w, z })[0]").WithLocation(31, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Update this method to use types from unannotated assemblies
        // rather than `x!`, particularly because `x!` should result in IsNullable=false rather than IsNullable=null.
        // PROTOTYPE(NullableReferenceTypes): Should report the same warnings (or no warnings) for { x, x! } and { x!, x }.
        [Fact(Skip = "TODO")]
        public void IdentityConversion_ArrayInitializer_IsNullableNull()
        {
            var source =
@"#pragma warning disable 0649
class A<T>
{
    internal T F;
}
class B
{
    static void F(object? x, object y)
    {
        (new[] { x, x! })[0].ToString();
        (new[] { x!, x })[0].ToString();
        (new[] { y, y! })[0].ToString();
        (new[] { y!, y })[0].ToString();
    }
    static void F(A<object?> z, A<object> w)
    {
        (new[] { z, z! })[0].F.ToString();
        (new[] { z!, z })[0].F.ToString();
        (new[] { w, w! })[0].F.ToString();
        (new[] { w!, w })[0].F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Update this method to use types from unannotated assemblies
        // rather than `x!`, particularly because `x!` should result in IsNullable=false rather than IsNullable=null.
        // PROTOTYPE(NullableReferenceTypes): Should report the same warnings (or no warnings) for (x, x!) and (x!, x).
        [Fact(Skip = "TODO")]
        public void IdentityConversion_TypeInference_IsNullableNull()
        {
            var source =
@"class A<T>
{
}
class B
{
    static T F1<T>(T x, T y)
    {
        return x;
    }
    static void G1(object? x, object y)
    {
        F1(x, x!).ToString();
        F1(x!, x).ToString();
        F1(y, y!).ToString();
        F1(y!, y).ToString();
    }
    static T F2<T>(A<T> x, A<T> y)
    {
        throw new System.Exception();
    }
    static void G(A<object?> z, A<object> w)
    {
        F2(z, z!).ToString();
        F2(z!, z).ToString();
        F2(w, w!).ToString();
        F2(w!, w).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: Other
        [Fact(Skip = "TODO")]
        public void IdentityConversion_ArrayInitializer_ExplicitType()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object>? x, I<object?>? y)
    {
        I<object?>?[] a = new[] { x };
        I<object?>[] b = new[] { y };
        I<object>?[] c = new[] { y };
        I<object>[] d = new[] { x };
    }
    static void F(IIn<object>? x, IIn<object?>? y)
    {
        IIn<object?>?[] a = new[] { x };
        IIn<object?>[] b = new[] { y };
        IIn<object>?[] c = new[] { y };
        IIn<object>[] d = new[] { x };
    }
    static void F(IOut<object>? x, IOut<object?>? y)
    {
        IOut<object?>?[] a = new[] { x };
        IOut<object?>[] b = new[] { y };
        IOut<object>?[] c = new[] { y };
        IOut<object>[] d = new[] { x };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,27): warning CS8619: Nullability of reference types in value of type 'I<object>?[]' doesn't match target type 'I<object?>?[]'.
                //         I<object?>?[] a = new[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { x }").WithArguments("I<object>?[]", "I<object?>?[]").WithLocation(8, 27),
                // (9,26): warning CS8619: Nullability of reference types in value of type 'I<object?>?[]' doesn't match target type 'I<object?>[]'.
                //         I<object?>[] b = new[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { y }").WithArguments("I<object?>?[]", "I<object?>[]").WithLocation(9, 26),
                // (10,26): warning CS8619: Nullability of reference types in value of type 'I<object?>?[]' doesn't match target type 'I<object>?[]'.
                //         I<object>?[] c = new[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { y }").WithArguments("I<object?>?[]", "I<object>?[]").WithLocation(10, 26),
                // (11,25): warning CS8619: Nullability of reference types in value of type 'I<object>?[]' doesn't match target type 'I<object>[]'.
                //         I<object>[] d = new[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { x }").WithArguments("I<object>?[]", "I<object>[]").WithLocation(11, 25),
                // (15,29): warning CS8619: Nullability of reference types in value of type 'IIn<object>?[]' doesn't match target type 'IIn<object?>?[]'.
                //         IIn<object?>?[] a = new[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { x }").WithArguments("IIn<object>?[]", "IIn<object?>?[]").WithLocation(15, 29),
                // (16,28): warning CS8619: Nullability of reference types in value of type 'IIn<object?>?[]' doesn't match target type 'IIn<object?>[]'.
                //         IIn<object?>[] b = new[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { y }").WithArguments("IIn<object?>?[]", "IIn<object?>[]").WithLocation(16, 28),
                // (18,27): warning CS8619: Nullability of reference types in value of type 'IIn<object>?[]' doesn't match target type 'IIn<object>[]'.
                //         IIn<object>[] d = new[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { x }").WithArguments("IIn<object>?[]", "IIn<object>[]").WithLocation(18, 27),
                // (23,29): warning CS8619: Nullability of reference types in value of type 'IOut<object?>?[]' doesn't match target type 'IOut<object?>[]'.
                //         IOut<object?>[] b = new[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { y }").WithArguments("IOut<object?>?[]", "IOut<object?>[]").WithLocation(23, 29),
                // (24,29): warning CS8619: Nullability of reference types in value of type 'IOut<object?>?[]' doesn't match target type 'IOut<object>?[]'.
                //         IOut<object>?[] c = new[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { y }").WithArguments("IOut<object?>?[]", "IOut<object>?[]").WithLocation(24, 29),
                // (25,28): warning CS8619: Nullability of reference types in value of type 'IOut<object>?[]' doesn't match target type 'IOut<object>[]'.
                //         IOut<object>[] d = new[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new[] { x }").WithArguments("IOut<object>?[]", "IOut<object>[]").WithLocation(25, 28));
        }

        [Fact]
        public void ImplicitConversion_ArrayInitializer_ExplicitType_01()
        {
            var source =
@"class A<T> { }
class B<T> : A<T> { }
class C
{
    static void F(A<object> x, B<object?> y)
    {
        var z = new A<object>[] { x, y };
        var w = new A<object?>[] { x, y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,38): warning CS8619: Nullability of reference types in value of type 'B<object?>' doesn't match target type 'A<object>'.
                //         var z = new A<object>[] { x, y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("B<object?>", "A<object>").WithLocation(7, 38),
                // (8,36): warning CS8619: Nullability of reference types in value of type 'A<object>' doesn't match target type 'A<object?>'.
                //         var w = new A<object?>[] { x, y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("A<object>", "A<object?>").WithLocation(8, 36));
        }

        [Fact]
        public void ImplicitConversion_ArrayInitializer_ExplicitType_02()
        {
            var source =
@"interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(IIn<object> x, IIn<object?> y)
    {
        var a = new IIn<string?>[] { x };
        var b = new IIn<string>[] { y };
    }
    static void F(IOut<string> x, IOut<string?> y)
    {
        var a = new IOut<object?>[] { x };
        var b = new IOut<object>[] { y };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,38): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<string?>'.
                //         var a = new IIn<string?>[] { x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IIn<object>", "IIn<string?>").WithLocation(7, 38),
                // (13,38): warning CS8619: Nullability of reference types in value of type 'IOut<string?>' doesn't match target type 'IOut<object>'.
                //         var b = new IOut<object>[] { y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IOut<string?>", "IOut<object>").WithLocation(13, 38));
        }

        [Fact]
        public void MultipleConversions_ArrayInitializer()
        {
            var source =
@"class A
{
    public static implicit operator C(A a) => new C();
}
class B : A
{
}
class C
{
    static void F(B x, C? y)
    {
        (new[] { x, y })[0].ToString();
        (new[] { y, x })[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { x, y })[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { x, y })[0]").WithLocation(12, 9),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         (new[] { y, x })[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(new[] { y, x })[0]").WithLocation(13, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Conversions: ConditionalOperator
        [Fact(Skip = "TODO")]
        public void IdentityConversion_ConditionalOperator()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(bool c, I<object> x, I<object?> y)
    {
        I<object> a;
        a = c ? x : y;
        a = false ? x : y;
        a = true ? x : y;
        I<object?> b;
        b = c ? x : y;
        b = false ? x : y;
        b = true ? x : y;
    }
    static void F(bool c, IIn<object> x, IIn<object?> y)
    {
        IIn<object> a;
        a = c ? x : y;
        a = false ? x : y;
        a = true ? x : y;
        IIn<object?> b;
        b = c ? x : y;
        b = false ? x : y;
        b = true ? x : y;
    }
    static void F(bool c, IOut<object> x, IOut<object?> y)
    {
        IOut<object> a;
        a = c ? x : y;
        a = false ? x : y;
        a = true ? x : y;
        IOut<object?> b;
        b = c ? x : y;
        b = false ? x : y;
        b = true ? x : y;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(/*TODO*/);
        }
    }
}
