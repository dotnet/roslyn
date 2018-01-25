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
    static void F()
    {
        var a = new[] { new object(), (string)null };
        a[0].ToString();
        var b = new[] { (object)null, string.Empty };
        b[0].ToString();
        var c = new[] { string.Empty, (object)null };
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
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             c[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c[0]").WithLocation(9, 13),
                // (10,32): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //             var d = new[] { b, a };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "a").WithArguments("C<object>", "C<object?>").WithLocation(10, 32),
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             d[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "d[0]").WithLocation(11, 13),
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
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(3, 24));
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
    static void F(bool b, A<object> x, A<object?> y, B1 z)
    {
        (b ? x : z).F.ToString();
        (b ? z : x).P.ToString();
        (b ? y : z).F.ToString();
        (b ? z : y).P.ToString();
    }
    static void G(bool b, A<object> x, A<object?> y, B2 z)
    {
        (b ? x : z).F.ToString();
        (b ? z : x).P.ToString();
        (b ? y : z).F.ToString();
        (b ? z : y).P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,10): warning CS8626: No best nullability for operands of conditional expression 'A<object>' and 'B1'.
                //         (b ? x : z).F.ToString();
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "b ? x : z").WithArguments("A<object>", "B1").WithLocation(13, 10),
                // (14,10): warning CS8626: No best nullability for operands of conditional expression 'B1' and 'A<object>'.
                //         (b ? z : x).P.ToString();
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "b ? z : x").WithArguments("B1", "A<object>").WithLocation(14, 10),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : z).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? y : z).F").WithLocation(15, 9),
                // (16,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? z : y).P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? z : y).P").WithLocation(16, 9),
                // (22,10): warning CS8626: No best nullability for operands of conditional expression 'A<object?>' and 'B2'.
                //         (b ? y : z).F.ToString();
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "b ? y : z").WithArguments("A<object?>", "B2").WithLocation(22, 10),
                // (22,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : z).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? y : z).F").WithLocation(22, 9),
                // (23,10): warning CS8626: No best nullability for operands of conditional expression 'B2' and 'A<object?>'.
                //         (b ? z : y).P.ToString();
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "b ? z : y").WithArguments("B2", "A<object?>").WithLocation(23, 10),
                // (23,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? z : y).P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? z : y).P").WithLocation(23, 9));
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
                // (15,13): warning CS8602: Possible dereference of a null reference.
                //             (b ?? c)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ?? c)[0]").WithLocation(15, 13));
        }

        // PROTOTYPE(NullableReferenceType): NullableWalker.VisitAnonymousObjectCreationExpression
        // should support initializers with inferred nullability.
        [Fact(Skip = "TODO")]
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
                // (10,43): warning CS8603: Possible null reference return.
                //         F(() => { if (b) return x; return y; }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y").WithLocation(10, 43),
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
        public void IdentityConversion_LocalDeclaration()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object> x, IIn<object> y, IOut<object> z)
    {
        I<object?> a = x;
        IIn<object?> b = y;
        IOut<object?> c = z;
    }
    static void G(I<object?> x, IIn<object?> y, IOut<object?> z)
    {
        I<object> a = x;
        IIn<object> b = y;
        IOut<object> c = z;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,24): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         I<object?> a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(8, 24),
                // (9,26): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         IIn<object?> b = y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(9, 26),
                // (14,23): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         I<object> a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object?>", "I<object>").WithLocation(14, 23),
                // (16,26): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         IOut<object> c = z;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "z").WithArguments("IOut<object?>", "IOut<object>").WithLocation(16, 26));
        }

        [Fact]
        public void IdentityConversion_Assignment()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object> x, IIn<object> y, IOut<object> z)
    {
        I<object?> a;
        IIn<object?> b;
        IOut<object?> c;
        a = x;
        b = y;
        c = z;
    }
    static void G(I<object?> x, IIn<object?> y, IOut<object?> z)
    {
        I<object> a;
        IIn<object> b;
        IOut<object> c;
        a = x;
        b = y;
        c = z;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(11, 13),
                // (12,13): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         b = y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(12, 13),
                // (20,13): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object?>", "I<object>").WithLocation(20, 13),
                // (22,13): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         c = z;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "z").WithArguments("IOut<object?>", "IOut<object>").WithLocation(22, 13));
        }

        [Fact]
        public void IdentityConversion_Return()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static I<object?> F(I<object> x) => x;
    static IIn<object?> F(IIn<object> x) => x;
    static IOut<object?> F(IOut<object> x) => x;
    static I<object> G(I<object?> x) => x;
    static IIn<object> G(IIn<object?> x) => x;
    static IOut<object> G(IOut<object?> x) => x;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,41): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //     static I<object?> F(I<object> x) => x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(6, 41),
                // (7,45): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //     static IIn<object?> F(IIn<object> x) => x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IIn<object>", "IIn<object?>").WithLocation(7, 45),
                // (9,41): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //     static I<object> G(I<object?> x) => x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object?>", "I<object>").WithLocation(9, 41),
                // (11,47): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //     static IOut<object> G(IOut<object?> x) => x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IOut<object?>", "IOut<object>").WithLocation(11, 47));
        }

        [Fact]
        public void IdentityConversion_Argument()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object> x, IIn<object> y, IOut<object> z)
    {
        G(x, y, z);
    }
    static void G(I<object?> x, IIn<object?> y, IOut<object?> z)
    {
        F(x, y, z);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,11): warning CS8620: Nullability of reference types in argument of type 'I<object>' doesn't match target type 'I<object?>' for parameter 'x' in 'void C.G(I<object?> x, IIn<object?> y, IOut<object?> z)'.
                //         G(x, y, z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object>", "I<object?>", "x", "void C.G(I<object?> x, IIn<object?> y, IOut<object?> z)").WithLocation(8, 11),
                // (8,14): warning CS8620: Nullability of reference types in argument of type 'IIn<object>' doesn't match target type 'IIn<object?>' for parameter 'y' in 'void C.G(I<object?> x, IIn<object?> y, IOut<object?> z)'.
                //         G(x, y, z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<object>", "IIn<object?>", "y", "void C.G(I<object?> x, IIn<object?> y, IOut<object?> z)").WithLocation(8, 14),
                // (12,11): warning CS8620: Nullability of reference types in argument of type 'I<object?>' doesn't match target type 'I<object>' for parameter 'x' in 'void C.F(I<object> x, IIn<object> y, IOut<object> z)'.
                //         F(x, y, z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object?>", "I<object>", "x", "void C.F(I<object> x, IIn<object> y, IOut<object> z)").WithLocation(12, 11),
                // (12,17): warning CS8620: Nullability of reference types in argument of type 'IOut<object?>' doesn't match target type 'IOut<object>' for parameter 'z' in 'void C.F(I<object> x, IIn<object> y, IOut<object> z)'.
                //         F(x, y, z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<object?>", "IOut<object>", "z", "void C.F(I<object> x, IIn<object> y, IOut<object> z)").WithLocation(12, 17));
        }

        [Fact]
        public void IdentityConversion_OutArgument()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(out I<object> x, out IIn<object> y, out IOut<object> z)
    {
        G(out x, out y, out z);
    }
    static void G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)
    {
        F(out x, out y, out z);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,15): warning CS8620: Nullability of reference types in argument of type 'I<object>' doesn't match target type 'I<object?>' for parameter 'x' in 'void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)'.
                //         G(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object>", "I<object?>", "x", "void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)").WithLocation(8, 15),
                // (8,22): warning CS8620: Nullability of reference types in argument of type 'IIn<object>' doesn't match target type 'IIn<object?>' for parameter 'y' in 'void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)'.
                //         G(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<object>", "IIn<object?>", "y", "void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)").WithLocation(8, 22),
                // (8,29): warning CS8620: Nullability of reference types in argument of type 'IOut<object>' doesn't match target type 'IOut<object?>' for parameter 'z' in 'void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)'.
                //         G(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<object>", "IOut<object?>", "z", "void C.G(out I<object?> x, out IIn<object?> y, out IOut<object?> z)").WithLocation(8, 29),
                // (12,15): warning CS8620: Nullability of reference types in argument of type 'I<object?>' doesn't match target type 'I<object>' for parameter 'x' in 'void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)'.
                //         F(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object?>", "I<object>", "x", "void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)").WithLocation(12, 15),
                // (12,22): warning CS8620: Nullability of reference types in argument of type 'IIn<object?>' doesn't match target type 'IIn<object>' for parameter 'y' in 'void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)'.
                //         F(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<object?>", "IIn<object>", "y", "void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)").WithLocation(12, 22),
                // (12,29): warning CS8620: Nullability of reference types in argument of type 'IOut<object?>' doesn't match target type 'IOut<object>' for parameter 'z' in 'void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)'.
                //         F(out x, out y, out z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<object?>", "IOut<object>", "z", "void C.F(out I<object> x, out IIn<object> y, out IOut<object> z)").WithLocation(12, 29));
        }

        [Fact]
        public void IdentityConversion_RefArgument()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)
    {
        G(ref x, ref y, ref z);
    }
    static void G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)
    {
        F(ref x, ref y, ref z);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,15): warning CS8620: Nullability of reference types in argument of type 'I<object>' doesn't match target type 'I<object?>' for parameter 'x' in 'void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)'.
                //         G(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object>", "I<object?>", "x", "void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)").WithLocation(8, 15),
                // (8,22): warning CS8620: Nullability of reference types in argument of type 'IIn<object>' doesn't match target type 'IIn<object?>' for parameter 'y' in 'void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)'.
                //         G(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<object>", "IIn<object?>", "y", "void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)").WithLocation(8, 22),
                // (8,29): warning CS8620: Nullability of reference types in argument of type 'IOut<object>' doesn't match target type 'IOut<object?>' for parameter 'z' in 'void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)'.
                //         G(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<object>", "IOut<object?>", "z", "void C.G(ref I<object?> x, ref IIn<object?> y, ref IOut<object?> z)").WithLocation(8, 29),
                // (12,15): warning CS8620: Nullability of reference types in argument of type 'I<object?>' doesn't match target type 'I<object>' for parameter 'x' in 'void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)'.
                //         F(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<object?>", "I<object>", "x", "void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)").WithLocation(12, 15),
                // (12,22): warning CS8620: Nullability of reference types in argument of type 'IIn<object?>' doesn't match target type 'IIn<object>' for parameter 'y' in 'void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)'.
                //         F(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<object?>", "IIn<object>", "y", "void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)").WithLocation(12, 22),
                // (12,29): warning CS8620: Nullability of reference types in argument of type 'IOut<object?>' doesn't match target type 'IOut<object>' for parameter 'z' in 'void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)'.
                //         F(ref x, ref y, ref z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<object?>", "IOut<object>", "z", "void C.F(ref I<object> x, ref IIn<object> y, ref IOut<object> z)").WithLocation(12, 29));
        }

        [Fact]
        public void IdentityConversion_NullCoalescingOperator()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F(I<object>? x, I<object?> y)
    {
        I<object> z = x ?? y;
        I<object?> w = x ?? y;
    }
    static void F(IIn<object>? x, IIn<object?> y)
    {
        IIn<object> z = x ?? y;
        IIn<object?> w = x ?? y;
    }
    static void F(IOut<object>? x, IOut<object?> y)
    {
        IOut<object> z = x ?? y;
        IOut<object?> w = x ?? y;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,28): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         I<object> z = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(8, 28),
                // (9,29): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         I<object?> w = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(9, 29),
                // (9,24): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         I<object?> w = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x ?? y").WithArguments("I<object>", "I<object?>").WithLocation(9, 24),
                // (14,26): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         IIn<object?> w = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x ?? y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(14, 26),
                // (18,31): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         IOut<object> z = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(18, 31),
                // (19,32): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         IOut<object?> w = x ?? y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(19, 32));
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
    static void F(I<object>? x, I<object?>? y)
    {
        var a = new[] { x, y };
        var b = new[] { y, x };
    }
    static void F(IIn<object>? x, IIn<object?>? y)
    {
        var a = new[] { x, y };
        var b = new[] { y, x };
    }
    static void F(IOut<object>? x, IOut<object?>? y)
    {
        var a = new[] { x, y };
        var b = new[] { y, x };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,28): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         var a = new[] { x, y };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(8, 28),
                // (9,28): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         var b = new[] { y, x };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(9, 28));
        }

        [Fact]
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
        public void ImplicitConversion_ArrayInitializer_ExplicitType()
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
            comp.VerifyDiagnostics(
                // (9,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         a = c ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "c ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(9, 13),
                // (10,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         a = false ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "false ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(10, 13),
                // (11,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         a = true ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "true ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(11, 13),
                // (13,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "c ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(13, 13),
                // (13,13): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(13, 13),
                // (14,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         b = false ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "false ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(14, 13),
                // (14,13): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         b = false ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "false ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(14, 13),
                // (15,13): warning CS8626: No best nullability for operands of conditional expression 'I<object>' and 'I<object?>'.
                //         b = true ? x : y;
                Diagnostic(ErrorCode.WRN_NoBestNullabilityConditionalExpression, "true ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(15, 13),
                // (15,13): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         b = true ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "true ? x : y").WithArguments("I<object>", "I<object?>").WithLocation(15, 13),
                // (24,13): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? x : y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(24, 13),
                // (25,13): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         b = false ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "false ? x : y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(25, 13),
                // (26,13): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         b = true ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "true ? x : y").WithArguments("IIn<object>", "IIn<object?>").WithLocation(26, 13),
                // (31,13): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         a = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? x : y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(31, 13),
                // (32,13): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         a = false ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "false ? x : y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(32, 13),
                // (33,13): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         a = true ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "true ? x : y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(33, 13));
        }

        [Fact]
        public void IdentityConversion_CompoundAssignment()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    public static I<object> operator+(I<object> x, C y) => x;
    public static IIn<object> operator+(IIn<object> x, C y) => x;
    public static IOut<object> operator+(IOut<object> x, C y) => x;
    static void F(C c, I<object> x, I<object?> y)
    {
        x += c;
        y += c;
    }
    static void F(C c, IIn<object> x, IIn<object?> y)
    {
        x += c;
        y += c;
    }
    static void F(C c, IOut<object> x, IOut<object?> y)
    {
        x += c;
        y += c;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,9): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         y += c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(12, 9),
                // (12,9): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         y += c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y += c").WithArguments("I<object>", "I<object?>").WithLocation(12, 9),
                // (17,9): warning CS8619: Nullability of reference types in value of type 'IIn<object>' doesn't match target type 'IIn<object?>'.
                //         y += c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y += c").WithArguments("IIn<object>", "IIn<object?>").WithLocation(17, 9),
                // (22,9): warning CS8619: Nullability of reference types in value of type 'IOut<object?>' doesn't match target type 'IOut<object>'.
                //         y += c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("IOut<object?>", "IOut<object>").WithLocation(22, 9));
        }

        [Fact]
        public void IdentityConversion_DeconstructionAssignment()
        {
            var source =
@"interface IIn<in T> { }
interface IOut<out T> { }
class C<T>
{
    void Deconstruct(out IIn<T> x, out IOut<T> y)
    {
        throw new System.NotImplementedException();
    }
    static void F(C<object> c)
    {
        IIn<object?> x;
        IOut<object?> y;
        (x, y) = c;
    }
    static void G(C<object?> c)
    {
        IIn<object> x;
        IOut<object> y;
        (x, y) = c;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,18): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type '(IIn<object?> x, IOut<object?> y)'.
                //         (x, y) = c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c").WithArguments("C<object>", "(IIn<object?> x, IOut<object?> y)").WithLocation(13, 18),
                // (19,18): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type '(IIn<object> x, IOut<object> y)'.
                //         (x, y) = c;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c").WithArguments("C<object?>", "(IIn<object> x, IOut<object> y)").WithLocation(19, 18));
        }

        [Fact]
        public void IdentityConversion_DelegateReturnType()
        {
            var source =
@"delegate T D<T>();
interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static T F<T>() => throw new System.Exception();
    static void G()
    {
        D<object> a = F<object?>;
        D<object?> b = F<object>;
        D<I<object>> c = F<I<object?>>;
        D<I<object?>> d = F<I<object>>;
        D<IIn<object>> e = F<IIn<object?>>;
        D<IIn<object?>> f = F<IIn<object>>;
        D<IOut<object>> g = F<IOut<object?>>;
        D<IOut<object?>> h = F<IOut<object>>;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,23): warning CS8621: Nullability of reference types in return type of 'object? C.F<object?>()' doesn't match the target delegate 'D<object>'.
                //         D<object> a = F<object?>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F<object?>").WithArguments("object? C.F<object?>()", "D<object>").WithLocation(10, 23),
                // (12,26): warning CS8621: Nullability of reference types in return type of 'I<object?> C.F<I<object?>>()' doesn't match the target delegate 'D<I<object>>'.
                //         D<I<object>> c = F<I<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F<I<object?>>").WithArguments("I<object?> C.F<I<object?>>()", "D<I<object>>").WithLocation(12, 26),
                // (13,27): warning CS8621: Nullability of reference types in return type of 'I<object> C.F<I<object>>()' doesn't match the target delegate 'D<I<object?>>'.
                //         D<I<object?>> d = F<I<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F<I<object>>").WithArguments("I<object> C.F<I<object>>()", "D<I<object?>>").WithLocation(13, 27),
                // (15,29): warning CS8621: Nullability of reference types in return type of 'IIn<object> C.F<IIn<object>>()' doesn't match the target delegate 'D<IIn<object?>>'.
                //         D<IIn<object?>> f = F<IIn<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F<IIn<object>>").WithArguments("IIn<object> C.F<IIn<object>>()", "D<IIn<object?>>").WithLocation(15, 29),
                // (16,29): warning CS8621: Nullability of reference types in return type of 'IOut<object?> C.F<IOut<object?>>()' doesn't match the target delegate 'D<IOut<object>>'.
                //         D<IOut<object>> g = F<IOut<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F<IOut<object?>>").WithArguments("IOut<object?> C.F<IOut<object?>>()", "D<IOut<object>>").WithLocation(16, 29));
        }

        [Fact]
        public void IdentityConversion_DelegateParameter()
        {
            var source =
@"delegate void D<T>(T t);
interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F<T>(T t) { }
    static void G()
    {
        D<object> a = F<object?>;
        D<object?> b = F<object>;
        D<I<object>> c = F<I<object?>>;
        D<I<object?>> d = F<I<object>>;
        D<IIn<object>> e = F<IIn<object?>>;
        D<IIn<object?>> f = F<IIn<object>>;
        D<IOut<object>> g = F<IOut<object?>>;
        D<IOut<object?>> h = F<IOut<object>>;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,24): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<object>(object t)' doesn't match the target delegate 'D<object?>'.
                //         D<object?> b = F<object>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<object>").WithArguments("t", "void C.F<object>(object t)", "D<object?>").WithLocation(11, 24),
                // (12,26): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object?>>(I<object?> t)' doesn't match the target delegate 'D<I<object>>'.
                //         D<I<object>> c = F<I<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object?>>").WithArguments("t", "void C.F<I<object?>>(I<object?> t)", "D<I<object>>").WithLocation(12, 26),
                // (13,27): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object>>(I<object> t)' doesn't match the target delegate 'D<I<object?>>'.
                //         D<I<object?>> d = F<I<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object>>").WithArguments("t", "void C.F<I<object>>(I<object> t)", "D<I<object?>>").WithLocation(13, 27),
                // (14,28): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IIn<object?>>(IIn<object?> t)' doesn't match the target delegate 'D<IIn<object>>'.
                //         D<IIn<object>> e = F<IIn<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IIn<object?>>").WithArguments("t", "void C.F<IIn<object?>>(IIn<object?> t)", "D<IIn<object>>").WithLocation(14, 28),
                // (17,30): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IOut<object>>(IOut<object> t)' doesn't match the target delegate 'D<IOut<object?>>'.
                //         D<IOut<object?>> h = F<IOut<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IOut<object>>").WithArguments("t", "void C.F<IOut<object>>(IOut<object> t)", "D<IOut<object?>>").WithLocation(17, 30));
        }

        [Fact]
        public void IdentityConversion_LambdaReturnType()
        {
            var source =
@"delegate T D<T>();
interface I<T> { }
class C
{
    static void F(object x, object? y)
    {
        D<object?> a = () => x;
        D<object> b = () => y;
        if (y == null) return;
        D<object> c = () => y;
    }
    static void F(I<object> x, I<object?> y)
    {
        D<I<object?>> a = () => x;
        D<I<object>> b = () => y;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,29): warning CS8603: Possible null reference return.
                //         D<object> b = () => y;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y").WithLocation(8, 29),
                // (14,33): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         D<I<object?>> a = () => x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(14, 33),
                // (15,32): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         D<I<object>> b = () => y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<object?>", "I<object>").WithLocation(15, 32));
        }

        // PROTOTYPE(NullableReferenceTypes): Calculate lamba conversion.
        [Fact(Skip = "TODO")]
        public void IdentityConversion_LambdaParameter()
        {
            var source =
@"delegate void D<T>(T t);
interface I<T> { }
class C
{
    static void F()
    {
        D<object?> a = (object o) => { };
        D<object> b = (object? o) => { };
        D<I<object?>> c = (I<object> o) => { };
        D<I<object>> d = (I<object?> o) => { };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,24): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D<object?>'.
                //         D<object?> a = (object o) => { };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(object o) => { }").WithArguments("o", "lambda expression", "D<object?>").WithLocation(7, 24),
                // (8,23): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D<object>'.
                //         D<object> b = (object? o) => { };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(object? o) => { }").WithArguments("o", "lambda expression", "D<object>").WithLocation(8, 23),
                // (9,27): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D<I<object?>>'.
                //         D<I<object?>> c = (I<object> o) => { };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(I<object> o) => { }").WithArguments("o", "lambda expression", "D<I<object?>>").WithLocation(9, 27),
                // (10,26): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D<I<object>>'.
                //         D<I<object>> d = (I<object?> o) => { };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(I<object?> o) => { }").WithArguments("o", "lambda expression", "D<I<object>>").WithLocation(10, 26));
        }

        // PROTOTYPE(NullableReferenceTypes): Handle ref and out parameters.
        [Fact(Skip = "TODO")]
        public void IdentityConversion_DelegateOutParameter()
        {
            var source =
@"delegate void D<T>(out T t);
interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F<T>(out T t) { t = default; }
    static void G()
    {
        D<object> a = F<object?>;
        D<object?> b = F<object>;
        D<I<object>> c = F<I<object?>>;
        D<I<object?>> d = F<I<object>>;
        D<IIn<object>> e = F<IIn<object?>>;
        D<IIn<object?>> f = F<IIn<object>>;
        D<IOut<object>> g = F<IOut<object?>>;
        D<IOut<object?>> h = F<IOut<object>>;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,23): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<object?>(object? t)' doesn't match the target delegate 'D<object>'.
                //         D<object> a = F<object?>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<object?>").WithArguments("t", "void C.F<object?>(object? t)", "D<object>").WithLocation(10, 23),
                // (12,26): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object?>>(I<object?> t)' doesn't match the target delegate 'D<I<object>>'.
                //         D<I<object>> c = F<I<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object?>>").WithArguments("t", "void C.F<I<object?>>(I<object?> t)", "D<I<object>>").WithLocation(12, 26),
                // (13,27): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object>>(I<object> t)' doesn't match the target delegate 'D<I<object?>>'.
                //         D<I<object?>> d = F<I<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object>>").WithArguments("t", "void C.F<I<object>>(I<object> t)", "D<I<object?>>").WithLocation(13, 27),
                // (15,29): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IIn<object>>(IIn<object> t)' doesn't match the target delegate 'D<IIn<object?>>'.
                //         D<IIn<object?>> f = F<IIn<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IIn<object>>").WithArguments("t", "void C.F<IIn<object>>(IIn<object> t)", "D<IIn<object?>>").WithLocation(15, 29),
                // (16,29): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IOut<object?>>(IOut<object?> t)' doesn't match the target delegate 'D<IOut<object>>'.
                //         D<IOut<object>> g = F<IOut<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IOut<object?>>").WithArguments("t", "void C.F<IOut<object?>>(IOut<object?> t)", "D<IOut<object>>").WithLocation(16, 29));
        }

        // PROTOTYPE(NullableReferenceTypes): Handle ref and out parameters.
        [Fact(Skip = "TODO")]
        public void IdentityConversion_DelegateRefParameter()
        {
            var source =
@"delegate void D<T>(ref T t);
interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static void F<T>(ref T t) { }
    static void G()
    {
        D<object> a = F<object?>;
        D<object?> b = F<object>;
        D<I<object>> c = F<I<object?>>;
        D<I<object?>> d = F<I<object>>;
        D<IIn<object>> e = F<IIn<object?>>;
        D<IIn<object?>> f = F<IIn<object>>;
        D<IOut<object>> g = F<IOut<object?>>;
        D<IOut<object?>> h = F<IOut<object>>;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,23): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<object?>(object? t)' doesn't match the target delegate 'D<object>'.
                //         D<object> a = F<object?>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<object?>").WithArguments("t", "void C.F<object?>(object? t)", "D<object>").WithLocation(10, 23),
                // (12,26): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object?>>(I<object?> t)' doesn't match the target delegate 'D<I<object>>'.
                //         D<I<object>> c = F<I<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object?>>").WithArguments("t", "void C.F<I<object?>>(I<object?> t)", "D<I<object>>").WithLocation(12, 26),
                // (13,27): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<I<object>>(I<object> t)' doesn't match the target delegate 'D<I<object?>>'.
                //         D<I<object?>> d = F<I<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<I<object>>").WithArguments("t", "void C.F<I<object>>(I<object> t)", "D<I<object?>>").WithLocation(13, 27),
                // (15,29): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IIn<object>>(IIn<object> t)' doesn't match the target delegate 'D<IIn<object?>>'.
                //         D<IIn<object?>> f = F<IIn<object>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IIn<object>>").WithArguments("t", "void C.F<IIn<object>>(IIn<object> t)", "D<IIn<object?>>").WithLocation(15, 29),
                // (16,29): warning CS8622: Nullability of reference types in type of parameter 't' of 'void C.F<IOut<object?>>(IOut<object?> t)' doesn't match the target delegate 'D<IOut<object>>'.
                //         D<IOut<object>> g = F<IOut<object?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F<IOut<object?>>").WithArguments("t", "void C.F<IOut<object?>>(IOut<object?> t)", "D<IOut<object>>").WithLocation(16, 29));
        }

        [Fact]
        public void IdentityConversion_IndexerArgumentsOrder()
        {
            var source =
@"interface I<T> { }
class C
{
    static object F(C c, I<string> x, I<object> y)
    {
        return c[y: y, x: x];
    }
    static object G(C c, I<string?> x, I<object?> y)
    {
        return c[y: y, x: x];
    }
    object this[I<string> x, I<object?> y] => new object();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,21): warning CS8620: Nullability of reference types in argument of type 'I<object>' doesn't match target type 'I<object?>' for parameter 'y' in 'object C.this[I<string> x, I<object?> y].get'.
                //         return c[y: y, x: x];
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("I<object>", "I<object?>", "y", "object C.this[I<string> x, I<object?> y].get").WithLocation(6, 21),
                // (10,27): warning CS8620: Nullability of reference types in argument of type 'I<string?>' doesn't match target type 'I<string>' for parameter 'x' in 'object C.this[I<string> x, I<object?> y].get'.
                //         return c[y: y, x: x];
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string?>", "I<string>", "x", "object C.this[I<string> x, I<object?> y].get").WithLocation(10, 27));
        }

        [Fact]
        public void IdentityConversion_ObjectElementInitializerArgumentsOrder()
        {
            var source =
@"interface I<T> { }
class C
{
    static C F(I<string> x, I<object> y)
    {
        return new C() { [y: y, x: x] = 1 };
    }
    static object G(C c, I<string?> x, I<object?> y)
    {
        return new C() { [y: y, x: x] = 2 };
    }
    int this[I<string> x, I<object?> y]
    {
        get { return 0; }
        set { }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,30): warning CS8620: Nullability of reference types in argument of type 'I<object>' doesn't match target type 'I<object?>' for parameter 'y' in 'int C.this[I<string> x, I<object?> y]'.
                //         return new C() { [y: y, x: x] = 1 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("I<object>", "I<object?>", "y", "int C.this[I<string> x, I<object?> y]").WithLocation(6, 30),
                // (10,36): warning CS8620: Nullability of reference types in argument of type 'I<string?>' doesn't match target type 'I<string>' for parameter 'x' in 'int C.this[I<string> x, I<object?> y]'.
                //         return new C() { [y: y, x: x] = 2 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string?>", "I<string>", "x", "int C.this[I<string> x, I<object?> y]").WithLocation(10, 36));
        }

        [Fact]
        public void Conversions_01()
        {
            var source =
@"class A<T> { }
class B<T> : A<T> { }
class C
{
    static void F(B<object> x)
    {
        A<object?> y = x;
    }
    static void G(B<object?> x)
    {
        A<object> y = x;
    }
    static void H(B<object>? x)
    {
        A<object?> y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,24): warning CS8619: Nullability of reference types in value of type 'B<object>' doesn't match target type 'A<object?>'.
                //         A<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("B<object>", "A<object?>").WithLocation(7, 24),
                // (11,23): warning CS8619: Nullability of reference types in value of type 'B<object?>' doesn't match target type 'A<object>'.
                //         A<object> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("B<object?>", "A<object>").WithLocation(11, 23),
                // (15,24): warning CS8619: Nullability of reference types in value of type 'B<object>' doesn't match target type 'A<object?>'.
                //         A<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("B<object>", "A<object?>").WithLocation(15, 24),
                // (15,24): warning CS8601: Possible null reference assignment.
                //         A<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x").WithLocation(15, 24));
        }

        [Fact]
        public void Conversions_02()
        {
            var source =
@"interface IA<T> { }
interface IB<T> : IA<T> { }
class C
{
    static void F(IB<object> x)
    {
        IA<object?> y = x;
        y = x;
    }
    static void G(IB<object?> x)
    {
        IA<object> y = x;
        y = x;
    }
    static void H(IB<object>? x)
    {
        IA<object?> y = x;
        y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,25): warning CS8619: Nullability of reference types in value of type 'IB<object>' doesn't match target type 'IA<object?>'.
                //         IA<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object>", "IA<object?>").WithLocation(7, 25),
                // (8,13): warning CS8619: Nullability of reference types in value of type 'IB<object>' doesn't match target type 'IA<object?>'.
                //         y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object>", "IA<object?>").WithLocation(8, 13),
                // (12,24): warning CS8619: Nullability of reference types in value of type 'IB<object?>' doesn't match target type 'IA<object>'.
                //         IA<object> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object?>", "IA<object>").WithLocation(12, 24),
                // (13,13): warning CS8619: Nullability of reference types in value of type 'IB<object?>' doesn't match target type 'IA<object>'.
                //         y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object?>", "IA<object>").WithLocation(13, 13),
                // (17,25): warning CS8619: Nullability of reference types in value of type 'IB<object>' doesn't match target type 'IA<object?>'.
                //         IA<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object>", "IA<object?>").WithLocation(17, 25),
                // (17,25): warning CS8601: Possible null reference assignment.
                //         IA<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x").WithLocation(17, 25),
                // (18,13): warning CS8619: Nullability of reference types in value of type 'IB<object>' doesn't match target type 'IA<object?>'.
                //         y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("IB<object>", "IA<object?>").WithLocation(18, 13),
                // (18,13): warning CS8601: Possible null reference assignment.
                //         y = x;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x").WithLocation(18, 13));
        }

        [Fact]
        public void Conversions_03()
        {
            var source =
@"interface I<out T> { }
class C
{
    static void F(I<object> x)
    {
        I<object?> y = x;
    }
    static void G(I<object?> x)
    {
        I<object> y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,23): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         I<object> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object?>", "I<object>").WithLocation(10, 23));
        }

        [Fact]
        public void Conversions_04()
        {
            var source =
@"interface I<in T> { }
class C
{
    static void F(I<object> x)
    {
        I<object?> y = x;
    }
    static void G(I<object?> x)
    {
        I<object> y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,24): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         I<object?> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("I<object>", "I<object?>").WithLocation(6, 24));
        }

        [Fact]
        public void Conversions_05()
        {
            var source =
@"interface I<out T> { }
class A<T> : I<T> { }
class C
{
    static void F(A<string> x)
    {
        I<object?> y = x;
    }
    static void G(A<string?> x)
    {
        I<object> y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,23): warning CS8619: Nullability of reference types in value of type 'A<string?>' doesn't match target type 'I<object>'.
                //         I<object> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("A<string?>", "I<object>").WithLocation(11, 23));
        }

        [Fact]
        public void Conversions_06()
        {
            var source =
@"interface I<out T> { }
class A<T> : I<object?> { }
class C
{
    static void F(A<string> x)
    {
        I<object?> y = x;
    }
    static void G(A<string> x)
    {
        I<object> y = x;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,23): warning CS8619: Nullability of reference types in value of type 'A<string>' doesn't match target type 'I<object>'.
                //         I<object> y = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("A<string>", "I<object>").WithLocation(11, 23));
        }

        [Fact]
        public void MultipleConversions_01()
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
    static void F(B? b)
    {
        C c = b; // (ImplicitUserDefined)(ImplicitReference)b
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8604: Possible null reference argument for parameter 'a' in 'A.implicit operator C(A a)'.
                //         C c = b; // (ImplicitUserDefined)(ImplicitReference)b
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "b").WithArguments("a", "A.implicit operator C(A a)").WithLocation(12, 15));
        }

        [Fact]
        public void MultipleConversions_02()
        {
            var source =
@"class A
{
}
class B : A
{
}
class C
{
    public static implicit operator B?(C c) => null;
    static void F(C c)
    {
        A a = c; // (ImplicitReference)(ImplicitUserDefined)c
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8601: Possible null reference assignment.
                //         A a = c; // (ImplicitReference)(ImplicitUserDefined)c
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c").WithLocation(12, 15));
        }
    }
}
