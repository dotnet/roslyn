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
        a[0].ToString();
        var b = new[] { x, y };
        b[0].ToString();
        var c = new[] { b };
        c[0][0].ToString();
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
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(8, 9));
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

        // PROTOTYPE(NullableReferenceTypes): Should report nullability mismatch warnings.
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
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0].F").WithLocation(15, 9),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         d[0].F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "d[0].F").WithLocation(19, 9));
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

        // PROTOTYPE(NullableReferenceTypes): Should report nullability mismatch warnings.
        [Fact]
        public void ConditionalOperator_05()
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
    static void F(bool b, A<object> x, B1 y)
    {
        (b ? x : y).F.ToString();
        (b ? y : x).F.ToString();
    }
    static void G(bool b, A<object?> x, B2 y)
    {
        (b ? x : y).F.ToString();
        (b ? y : x).F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (17,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? x : y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? x : y).F").WithLocation(17, 9),
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         (b ? y : x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(b ? y : x).F").WithLocation(18, 9));
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

        [Fact]
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

        [Fact]
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
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => o)").WithLocation(10, 9),
                // (11,24): warning CS8602: Possible dereference of a null reference.
                //         if (o != null) F(() => o).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => o)").WithLocation(11, 24));
        }

        [Fact]
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
    }
}
