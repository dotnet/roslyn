// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.NullableReferenceTypes, CompilerFeature.Patterns)]
    public class NullableReferenceTypesVsPatterns : CSharpTestBase
    {
        private CSharpCompilation CreateNullableCompilation(string source)
        {
            return CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_Null()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        if (x is null)
        {
            x.ToString(); // warn
        }
        else
        {
            x.ToString();
        }
    }
}
");

            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NullInverted()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        if (!(x is null))
        {
            x.ToString();
        }
        else
        {
            x.ToString(); // warn
        }
    }
}
");
            c.VerifyDiagnostics(
                // (12,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NonNull()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        const string nonNullConstant = ""hello"";
        if (x is nonNullConstant)
        {
            x.ToString();
        }
        else
        {
            x.ToString(); // warn
        }
    }
}
");
            c.VerifyDiagnostics(
                // (13,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NullConstant()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        const string? nullConstant = null;
        if (x is nullConstant)
        {
            x.ToString(); // warn
        }
        else
        {
            x.ToString();
        }
    }
}
");
            c.VerifyDiagnostics(
                // (9,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NonConstant()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        string nonConstant = ""hello"";
        if (x is nonConstant)
        {
            x.ToString(); // warn
        }
    }
}
");
            c.VerifyDiagnostics(
                // (7,18): error CS0150: A constant value is expected
                //         if (x is nonConstant)
                Diagnostic(ErrorCode.ERR_ConstantExpected, "nonConstant").WithLocation(7, 18),
                // (9,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_Null_AlreadyTestedAsNonNull()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        if (x != null)
        {
            if (x is null)
            {
                x.ToString(); // warn
            }
            else
            {
                x.ToString();
            }
        }
    }
}
");
            c.VerifyDiagnostics(
                // (10,17): warning CS8602: Dereference of a possibly null reference.
                //                 x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(10, 17)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_Null_AlreadyTestedAsNull()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test(object? x)
    {
        if (x == null)
        {
            if (x is null)
            {
                x.ToString(); // warn
            }
            else
            {
                x.ToString();
            }
        }
    }
}
");
            c.VerifyDiagnostics(
                // (10,17): warning CS8602: Dereference of a possibly null reference.
                //                 x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(10, 17)
                );
        }

        [Fact]
        public void ConditionalBranching_IsDeclarationPattern_02()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test1(object? x)
    {
        if (x is C c)
        {
            x.ToString();
            c.ToString();
        }
        else
        {
            x.ToString(); // warn
        }
    }
    void Test2(object x)
    {
        if (x is C c)
        {
            x.ToString();
            c.ToString();
        }
        else
        {
            x.ToString();
        }
    }
}
");
            c.VerifyDiagnostics(
                // (13,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test1(object? x)
    {
        if (x is var c)
        {
            x.ToString(); // warn 1
            c /*T:object?*/ .ToString(); // warn 2
        }
        else
        {
            x.ToString();
        }
    }
    void Test2(object x)
    {
        if (x is var c)
        {
            x.ToString();
            c /*T:object!*/ .ToString();
        }
        else
        {
            x.ToString();
        }
    }
}
");
            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13),
                // (9,13): warning CS8602: Dereference of a possibly null reference.
                //             c /*T:object?*/ .ToString(); // warn 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern_Discard()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test1(object? x)
    {
        if (x is var _)
        {
            x.ToString(); // 1
        }
        else
        {
            x.ToString();
        }
    }
    void Test2(object x)
    {
        if (x is var _)
        {
            x.ToString();
        }
        else
        {
            x.ToString();
        }
    }
}
");
            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13));
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern_AlreadyTestedAsNonNull()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class C
{
    void Test1(object? x)
    {
        if (x != null)
        {
            if (x is var c)
            {
                c /*T:object!*/ .ToString();
                c = null; // 1
            }
        }
    }
    void Test2(object x)
    {
        if (x != null)
        {
            if (x is var c)
            {
                c /*T:object!*/ .ToString();
                c = null; // 2
            }
        }
    }
}
");
            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (11,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //                 c = null; // 1
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(11, 21),
                // (22,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //                 c = null; // 2
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(22, 21));
        }

        [Fact]
        public void IsPattern_01()
        {
            var source =
@"class C
{
    static void F(object x) { }
    static void G(string s)
    {
        F(s is var o);
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(29909, "https://github.com/dotnet/roslyn/issues/29909")]
        public void IsPattern_02()
        {
            var source =
@"class C
{
    static void F(string s) { }
    static void G(string? s)
    {
        if (s is string t)
        {
            F(t);
            F(s);
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_DeclarationPattern()
        {
            var source =
@"class C
{
    static void G1(string? s)
    {
        if (s?.ToString() is string t)
        {
            s.ToString();
        }
        else
        {
            s.ToString(); // 1
        }
    }
    static void G2(string s)
    {
        if (s?.ToString() is string t)
        {
            s.ToString();
        }
        else
        {
            s.ToString(); // 2
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             s.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(11, 13),
                // (22,13): warning CS8602: Dereference of a possibly null reference.
                //             s.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(22, 13));
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_NullableValueType()
        {
            var source =
@"class C
{
    static void G1(int? i)
    {
        if (i?.ToString() is string t)
        {
            i.Value.ToString();
        }
        else
        {
            i.Value.ToString(); // 1
        }
    }
    static void G2(int? i)
    {
        i = 1;
        if (i?.ToString() is string t)
        {
            i.Value.ToString();
        }
        else
        {
            i.Value.ToString(); // 2
        }
    }
    static void G3(int? i)
    {
        i = 1;
        if (i is int q)
        {
            i.Value.ToString();
        }
        else
        {
            i.Value.ToString();
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (11,13): warning CS8629: Nullable value type may be null.
                //             i.Value.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "i").WithLocation(11, 13),
                // (23,13): warning CS8629: Nullable value type may be null.
                //             i.Value.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "i").WithLocation(23, 13));
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_NullableValueType_Nested()
        {
            var source = @"
public struct S
{
    public int? field;
}
class C
{
    static void G(S? s)
    {
        if (s?.field?.ToString() is string t)
        {
            s.Value.ToString();
            s.Value.field.Value.ToString();
        }
        else
        {
            s.Value.ToString(); // warn
            s.Value.field.Value.ToString(); // warn
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (17,13): warning CS8629: Nullable value type may be null.
                //             s.Value.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "s").WithLocation(17, 13),
                // (18,13): warning CS8629: Nullable value type may be null.
                //             s.Value.field.Value.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "s.Value.field").WithLocation(18, 13)
                );
        }

        [Fact, WorkItem(28798, "https://github.com/dotnet/roslyn/issues/28798")]
        public void IsPattern_AffectsNullConditionalOperator_VarPattern()
        {
            var source =
@"class C
{
    static void G(string? s)
    {
        if (s?.ToString() is var t)
        {
            s.ToString(); // 1
        }
        else
        {
            s.ToString();
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8602: Dereference of a possibly null reference.
                //             s.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(7, 13)
                );
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_NullConstantPattern()
        {
            var source =
@"class C
{
    static void G(string? s)
    {
        if (s?.ToString() is null)
        {
            s.ToString(); // warn
        }
        else
        {
            s.ToString();
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8602: Dereference of a possibly null reference.
                //             s.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(7, 13)
                );
        }

        [Fact]
        [WorkItem(29909, "https://github.com/dotnet/roslyn/issues/29909")]
        [WorkItem(23944, "https://github.com/dotnet/roslyn/issues/23944")]
        public void PatternSwitch()
        {
            var source =
@"class C
{
    static void F(object o) { }
    static void G(object? x)
    {
        switch (x)
        {
            case string s:
                F(s);
                F(x);
                break;
            case object y when y is string t:
                F(y);
                F(t);
                F(x);
                break;
            case null:
                F(x); // 1
                break;
            default:
                F(x);
                break;
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (18,19): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F(object o)'.
                //                 F(x); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("o", "void C.F(object o)").WithLocation(18, 19));
        }

        [Fact]
        public void IsDeclarationPattern_01()
        {
            var source =
@"class Program
{
    static void F1(object x1)
    {
        if (x1 is string y1)
        {
            x1/*T:object!*/.ToString();
            y1/*T:string!*/.ToString();
        }
        x1/*T:object!*/.ToString();
    }
    static void F2(object? x2)
    {
        if (x2 is string y2)
        {
            x2/*T:object!*/.ToString();
            y2/*T:string!*/.ToString();
        }
        x2/*T:object?*/.ToString(); // 1
    }
    static void F3(object x3)
    {
        x3 = null; // 2
        if (x3 is string y3)
        {
            x3/*T:object!*/.ToString();
            y3/*T:string!*/.ToString();
        }
        x3/*T:object?*/.ToString(); // 3
    }
    static void F4(object? x4)
    {
        if (x4 == null) return;
        if (x4 is string y4)
        {
            x4/*T:object!*/.ToString();
            y4/*T:string!*/.ToString();
        }
        x4/*T:object!*/.ToString();
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Dereference of a possibly null reference.
                //         x2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(19, 9),
                // (23,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 2
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(23, 14),
                // (29,9): warning CS8602: Dereference of a possibly null reference.
                //         x3.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(29, 9));
            comp.VerifyTypes();
        }

        [Fact]
        [WorkItem(30952, "https://github.com/dotnet/roslyn/issues/30952")]
        public void IsDeclarationPattern_02()
        {
            var source =
@"class Program
{
    static void F1<T, U>(T t1)
        where T : class
        where U : class
    {
        if (t1 is U u1)
        {
            t1.ToString();
            u1.ToString();
        }
        t1.ToString();
    }
    static void F2<T, U>(T t2)
        where T : class?
        where U : class
    {
        if (t2 is U u2)
        {
            t2.ToString();
            u2.ToString();
        }
        t2.ToString(); // 1
    }
    static void F3<T, U>(T t3)
        where T : class
        where U : class
    {
        t3 = null; // 2
        if (t3 is U u3)
        {
            t3.ToString();
            u3.ToString();
        }
        t3.ToString(); // 3
    }
    static void F4<T, U>(T t4)
        where T : class?
        where U : class
    {
        if (t4 == null) return;
        if (t4 is U u4)
        {
            t4.ToString();
            u4.ToString();
        }
        t4.ToString();
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (23,9): warning CS8602: Dereference of a possibly null reference.
                //         t2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t2").WithLocation(23, 9),
                // (29,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         t3 = null; // 2
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(29, 14),
                // (35,9): warning CS8602: Dereference of a possibly null reference.
                //         t3.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t3").WithLocation(35, 9));
        }

        [Fact]
        public void IsDeclarationPattern_03()
        {
            var source =
@"class Program
{
    static void F1<T, U>(T t1)
    {
        if (t1 is U u1)
        {
            t1.ToString();
            u1.ToString();
        }
        t1.ToString(); // 1
    }
    static void F2<T, U>(T t2)
    {
        if (t2 == null) return;
        if (t2 is U u2)
        {
            t2.ToString();
            u2.ToString();
        }
        t2.ToString();
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         t1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t1").WithLocation(10, 9));
        }

        [Fact]
        public void IsDeclarationPattern_NeverNull_01()
        {
            var source =
@"class Program
{
    static void F1(object x1)
    {
        if (x1 is string y1)
        {
            x1.ToString();
            x1?.ToString();
            y1.ToString();
            y1?.ToString();
        }
        x1.ToString(); // 1 (because of x1?. above)
    }
    static void F2(object? x2)
    {
        if (x2 is string y2)
        {
            x2.ToString();
            x2?.ToString();
            y2.ToString();
            y2?.ToString();
        }
        x2.ToString(); // 2
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         x1.ToString(); // 1 (because of x1?. above)
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(12, 9),
                // (23,9): warning CS8602: Dereference of a possibly null reference.
                //         x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(23, 9));
        }

        [Fact]
        public void IsDeclarationPattern_Unassigned_01()
        {
            var source =
@"class Program
{
    static void F1(object x1)
    {
        if (x1 is string y1)
        {
        }
        else
        {
            x1.ToString();
            y1.ToString(); // 1
        }
    }
    static void F2(object? x2)
    {
        if (x2 is string y2)
        {
        }
        else
        {
            x2.ToString(); // 2
            y2.ToString(); // 3
        }
    }
    static void F3(object x3)
    {
        x3 = null; // 4
        if (x3 is string y3)
        {
        }
        else
        {
            x3.ToString(); // 5
            y3.ToString(); // 6
        }
    }
    static void F4(object? x4)
    {
        if (x4 == null) return;
        if (x4 is string y4)
        {
        }
        else
        {
            x4.ToString();
            y4.ToString(); // 7
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): error CS0165: Use of unassigned local variable 'y1'
                //             y1.ToString(); // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(11, 13),
                // (21,13): warning CS8602: Dereference of a possibly null reference.
                //             x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(21, 13),
                // (22,13): error CS0165: Use of unassigned local variable 'y2'
                //             y2.ToString(); // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(22, 13),
                // (27,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 4
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(27, 14),
                // (33,13): warning CS8602: Dereference of a possibly null reference.
                //             x3.ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(33, 13),
                // (34,13): error CS0165: Use of unassigned local variable 'y3'
                //             y3.ToString(); // 6
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y3").WithArguments("y3").WithLocation(34, 13),
                // (46,13): error CS0165: Use of unassigned local variable 'y4'
                //             y4.ToString(); // 7
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y4").WithArguments("y4").WithLocation(46, 13));
        }

        [Fact]
        public void IsDeclarationPattern_Unassigned_02()
        {
            var source =
@"class Program
{
    static void F1(object x1)
    {
        if (x1 is string y1) { }
        x1.ToString();
        y1.ToString(); // 1
    }
    static void F2(object? x2)
    {
        if (x2 is string y2) { }
        x2.ToString(); // 2
        y2.ToString(); // 3
    }
    static void F3(object x3)
    {
        x3 = null; // 4
        if (x3 is string y3) { }
        x3.ToString(); // 5
        y3.ToString(); // 6
    }
    static void F4(object? x4)
    {
        if (x4 == null) return;
        if (x4 is string y4) { }
        x4.ToString();
        y4.ToString(); // 7
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,9): error CS0165: Use of unassigned local variable 'y1'
                //         y1.ToString(); // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(7, 9),
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(12, 9),
                // (13,9): error CS0165: Use of unassigned local variable 'y2'
                //         y2.ToString(); // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(13, 9),
                // (17,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 4
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(17, 14),
                // (19,9): warning CS8602: Dereference of a possibly null reference.
                //         x3.ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(19, 9),
                // (20,9): error CS0165: Use of unassigned local variable 'y3'
                //         y3.ToString(); // 6
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y3").WithArguments("y3").WithLocation(20, 9),
                // (27,9): error CS0165: Use of unassigned local variable 'y4'
                //         y4.ToString(); // 7
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y4").WithArguments("y4").WithLocation(27, 9));
        }

        [Fact]
        public void UnconstrainedTypeParameter_PatternMatching()
        {
            var source =
@"
class C
{
    static void F1<T>(object o, T tin)
    {
        if (o is T t1)
        {
            t1.ToString();
        }
        else
        {
            t1 = default; // 1
        }

        t1.ToString(); // 2

        if (!(o is T t2))
        {
            t2 = tin;
        }
        else
        {
            t2.ToString();
        }
        t2.ToString(); // 3

        if (!(o is T t3)) return;
        t3.ToString();
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (12,18): warning CS8652: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //             t1 = default; // 1
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default").WithArguments("T").WithLocation(12, 18),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         t1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t1").WithLocation(15, 9),
                // (25,9): warning CS8602: Dereference of a possibly null reference.
                //         t2.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t2").WithLocation(25, 9)
                );
        }

        [WorkItem(32503, "https://github.com/dotnet/roslyn/issues/32503")]
        [Fact]
        public void PatternDeclarationBreaksNullableAnalysis()
        {
            var source = @"
class A { }
class B : A
{
    A M()
    {
        var s = new A();
        if (s is B b) {}
        return s; 
    }
} 
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void InferenceWithITuplePattern()
        {
            var source = @"
class A { }
class B : A
{
    A M()
    {
        var s = new A();
        if (s is B b) {}
        return s; 
    }
} 
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecursivePatternNullInferenceWithDowncast_01()
        {
            var source = @"
class Base
{
    public object Value = """";
}
class Derived : Base
{
    public new object Value = """";
}
class Program
{
    void M(Base? b)
    {
        if (b is Derived { Value: null })
            b.Value.ToString();
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecursivePatternNullInferenceWithDowncast_02()
        {
            var source = @"
class Base
{
    public object Value = """";
}
class Derived : Base
{
}
class Program
{
    void M(Base? b)
    {
        if (b is Derived { Value: null })
            b.Value.ToString(); // 1
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Value.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Value").WithLocation(14, 13));
        }

        [Fact]
        public void TuplePatternNullInference_01()
        {
            var source = @"
class Program
{
    void M((object, object) t)
    {
        if (t is (1, null))
        {
        }
        else
        {
            t.Item2.ToString(); // 1
            t.Item1.ToString();
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             t.Item2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item2").WithLocation(11, 13));
        }

        [Fact]
        public void MultiplePathsThroughDecisionDag_01()
        {
            var source = @"
class Program
{
    bool M1(object? o, bool cond = true)
    {
        o = 1;
        switch (o)
        {
            case null:
                throw null!;
            case """" when M1(o = null):
                break;
            default:
                if (cond) o.ToString(); // 1
                break;
        }

        return cond;
    }
    bool M2(object? o, bool cond = true)
    {
        o = 1;
        switch (o)
        {
            case """" when M2(o = null):
                break;
            default:
                if (cond) o.ToString(); // 2
                break;
        }

        return cond;
    }
    bool M3(object? o, bool cond = true)
    {
        o = 1;
        switch (o)
        {
            case null:
                throw null!;
            default:
                if (cond) o.ToString();
                break;
        }

        return cond;
    }
    bool M4(object? o, bool cond = true)
    {
        o = 1;
        switch (o)
        {
            case null:
                throw null!;
            case """" when M4(o = null):
                break;
            case var q:
                q.ToString(); // 3 (!?)
                break;
        }

        return cond;
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (14,27): warning CS8602: Dereference of a possibly null reference.
                //                 if (cond) o.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(14, 27),
                // (28,27): warning CS8602: Dereference of a possibly null reference.
                //                 if (cond) o.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(28, 27),
                // (58,17): warning CS8602: Dereference of a possibly null reference.
                //                 q.ToString(); // 3 (!?)
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "q").WithLocation(58, 17));
        }

        [Fact]
        [WorkItem(30597, "https://github.com/dotnet/roslyn/issues/30597")]
        [WorkItem(32414, "https://github.com/dotnet/roslyn/issues/32414")]
        public void NotExhaustiveForNull_01()
        {
            var source = @"
class Program
{
    void M1(object o)
    {
        var t = (o, o);
        _ = t switch // 1 not exhaustive
        {
            (null, 2) => 1,
            ({}, {}) => 2,
        };
    }
    void M2(object o)
    {
        var t = (o, o);
        _ = t switch
        {
            (1, 2) => 1,
            ({}, {}) => 2,
        };
    }
    void M3(object o)
    {
        var t = (o, o);
        _ = t switch
        {
            (null, 2) => 1,
            ({}, {}) => 2,
            (null, {}) => 3,
        };
    }
    void M4(object o)
    {
        var t = (o, o);
        _ = t switch // 2 not exhaustive
        {
            { Item1: null, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
        };
    }
    void M5(object o)
    {
        var t = (o, o);
        _ = t switch
        {
            { Item1: 1, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
        };
    }
    void M6(object o)
    {
        var t = (o, o);
        _ = t switch
        {
            { Item1: null, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
            { Item1: null, Item2: {} } => 3,
        };
    }
    void M7(object o, bool b)
    {
        _ = o switch // 3 not exhaustive
        {
            null when b => 1,
            {} => 2,
        };
    }
    void M8(object o, bool b)
    {
        _ = o switch
        {
            null when b => 1,
            {} => 2,
            null => 3,
        };
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = t switch // 1 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(7, 15),
                // (35,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = t switch // 2 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(35, 15),
                // (62,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = o switch // 3 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(62, 15));
        }

        [Fact]
        [WorkItem(30597, "https://github.com/dotnet/roslyn/issues/30597")]
        [WorkItem(32414, "https://github.com/dotnet/roslyn/issues/32414")]
        public void NotExhaustiveForNull_02()
        {
            var source = @"
class Test
{
    int M1(string s1, string s2)
    {
        return (s1, s2) switch {
            (string x, string y) => x.Length + y.Length
            };
    }
    int M2(string? s1, string s2)
    {
        return (s1, s2) switch { // 1
            (string x, string y) => x.Length + y.Length
            };
    }
    int M3(string s1, string? s2)
    {
        return (s1, s2) switch { // 2
            (string x, string y) => x.Length + y.Length
            };
    }
    int M4(string? s1, string? s2)
    {
        return (s1, s2) switch { // 3
            (string x, string y) => x.Length + y.Length
            };
    }
    int M5(string s1, string s2)
    {
        return (s1, s2) switch { // 4
            (null, ""x"") => 1,
            (string x, string y) => x.Length + y.Length
            };
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (12,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         return (s1, s2) switch { // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(12, 25),
                // (18,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         return (s1, s2) switch { // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(18, 25),
                // (24,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         return (s1, s2) switch { // 3
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(24, 25),
                // (30,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         return (s1, s2) switch { // 4
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(30, 25));
        }

        [Fact, WorkItem(31881, "https://github.com/dotnet/roslyn/issues/31881")]
        public void NullableVsPattern_31881()
        {
            var source = @"
using System;

public class C
{
    public object? AProperty { get; set; }
    public void M(C? input, int i)
    {
        if (input?.AProperty is string str)
        {
            Console.WriteLine(str.ToString());

            switch (i)
            {
                case 1:
                    Console.WriteLine(input?.AProperty.ToString());
                    break;
                case 2:
                    Console.WriteLine(input.AProperty.ToString());
                    break;
                case 3:
                    Console.WriteLine(input.ToString());
                    break;
            }
        }
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(33499, "https://github.com/dotnet/roslyn/issues/33499")]
        public void PatternVariablesAreNotOblivious_33499()
        {
            var source = @"
class Test
{
    static void M(object o)
    {
        if (o is string s) { }
        s = null;
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 13));
        }

        [Fact]
        public void IsPatternAlwaysFalse()
        {
            var source = @"
class Test
{
    void M1(ref object o)
    {
        if (2 is 3)
            o = null;
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): warning CS8519: The given expression never matches the provided pattern.
                //         if (2 is 3)
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "2 is 3").WithLocation(6, 13));
        }

        [Fact]
        [WorkItem(29619, "https://github.com/dotnet/roslyn/issues/29619")]
        public void StructWithNotBackedProperty()
        {
            var source = @"
struct Point
{
    public object X, Y;
    public Point Mirror => new Point { X = Y, Y = X };
    bool Test => this is { X: 1, Y: 2, Mirror: { X: 2, Y: 1 } };
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LearnFromNullPattern_01()
        {
            var source = @"
class Node
{
    public Node Next = null!;
}
class Program
{
    void M1(Node n)
    {
        if (n is null) {}
        n.Next.Next.Next.ToString(); // 1
    }
    void M2(Node n)
    {
        if (n is {Next: null}) {}
        n.Next.Next.Next.ToString(); // 2
    }
    void M3(Node n)
    {
        if (n is {Next: {Next: null}}) {}
        n.Next.Next.Next.ToString(); // 3
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n").WithLocation(11, 9),
                // (16,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(16, 9),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(21, 9));
        }

        [Fact]
        public void LearnFromNullPattern_02()
        {
            var source = @"
class Node
{
    public Node Next = null!;
}
class Program
{
    void M1(Node n)
    {
        switch (n) { case null: break; }
        n.Next.Next.Next.ToString(); // 1
    }
    void M2(Node n)
    {
        switch (n) { case {Next: null}: break; }
        n.Next.Next.Next.ToString(); // 2
    }
    void M3(Node n)
    {
        switch (n) { case {Next: {Next: null}}: break; }
        n.Next.Next.Next.ToString(); // 3
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n").WithLocation(11, 9),
                // (16,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(16, 9),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(21, 9));
        }

        [Fact]
        public void LearnFromNullPattern_03()
        {
            var source = @"
class Node
{
    public Node Next = null!;
}
class Program
{
    void M1(Node n)
    {
        _ = n switch { null => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 1
    }
    void M2(Node n)
    {
        _ = n switch { {Next: null} => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 2
    }
    void M3(Node n)
    {
        _ = n switch { {Next: {Next: null}} => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 3
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n").WithLocation(11, 9),
                // (16,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(16, 9),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(21, 9));
        }

        [Fact]
        public void LearnFromNullPattern_04()
        {
            var source = @"
#nullable disable
class Node
{
    public Node Next = null!;
}
#nullable enable
class Program
{
#nullable disable
    void M1(Node n)
#nullable enable
    {
        _ = n switch { null => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 1
    }
#nullable disable
    void M2(Node n)
#nullable enable
    {
        _ = n switch { {Next: null} => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 2
    }
#nullable disable
    void M3(Node n)
#nullable enable
    {
        _ = n switch { {Next: {Next: null}} => 1, _ => 2 };
        n.Next.Next.Next.ToString(); // 3
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n").WithLocation(15, 9),
                // (22,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(22, 9),
                // (29,9): warning CS8602: Dereference of a possibly null reference.
                //         n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(29, 9));
        }

        [Fact]
        public void LearnFromNullPattern_05()
        {
            var source = @"
class Program
{
    void M1((string s1, string s2) n)
    {
        _ = n switch {
            (_, null) => n.s1.ToString(),
            var q => n.s1.ToString(),
            };
        n.s1.ToString();
        n.s2.ToString(); // 1
    }
    void M2((string s1, string s2) n)
    {
        _ = n switch {
            (null, _) => n.s1.ToString(), // 2
            (_, _)  => n.s1.ToString(),
            };
        n.s1.ToString(); // 3
        n.s2.ToString();
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         n.s2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.s2").WithLocation(11, 9),
                // (16,26): warning CS8602: Dereference of a possibly null reference.
                //             (null, _) => n.s1.ToString(), // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.s1").WithLocation(16, 26));
        }

        [Fact]
        public void LearnFromNonNullPattern_01()
        {
            var source = @"
class Node
{
    public Node? Next = null;
}
class Program
{
    void M1(Node? n)
    {
        if (n is {} q)
            n.Next.ToString(); // 1
    }
    void M2(Node? n)
    {
        if (n is {Next: {}} q)
            n.Next.Next.ToString(); // 2
    }
    void M3(Node? n)
    {
        if (n is {Next: {Next: {}}} q)
            n.Next.Next.Next.ToString(); // 3
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(11, 13),
                // (16,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(16, 13),
                // (21,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next.Next").WithLocation(21, 13));
        }

        [Fact]
        public void LearnFromNonNullPattern_02()
        {
            var source = @"
class Node
{
    public Node? Next = null;
}
class Program
{
    void M1(Node? n)
    {
        switch (n) { case {} q:
            n.Next.ToString(); // 1
        break; }
    }
    void M2(Node? n)
    {
        switch (n) { case {Next: {}} q:
            n.Next.Next.ToString(); // 2
        break; }
    }
    void M3(Node? n)
    {
        switch (n) { case {Next: {Next: {}}} q:
            n.Next.Next.Next.ToString(); // 3
        break; }
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(11, 13),
                // (17,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(17, 13),
                // (23,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.Next.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next.Next").WithLocation(23, 13));
        }

        [Fact]
        public void LearnFromNonNullPattern_03()
        {
            var source = @"
class Node
{
    public Node? Next = null;
}
class Program
{
    void M1(Node? n)
    {
        _ = n switch { {} q =>
            n.Next.ToString(), // 1
            _ => string.Empty };
    }
    void M2(Node? n)
    {
        _ = n switch { {Next: {}} q =>
            n.Next.Next.ToString(), // 2
            _ => string.Empty };
    }
    void M3(Node? n)
    {
        _ = n switch { {Next: {Next: {}}} q =>
            n.Next.Next.Next.ToString(), // 3
            _ => string.Empty };
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.ToString(), // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next").WithLocation(11, 13),
                // (17,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.ToString(), // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next").WithLocation(17, 13),
                // (23,13): warning CS8602: Dereference of a possibly null reference.
                //             n.Next.Next.Next.ToString(), // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Next.Next.Next").WithLocation(23, 13));
        }

        [Fact]
        public void LearnFromNonNullPattern_04()
        {
            var source = @"
class Program
{
    void M1((string? s1, string? s2)? n)
    {
        _ = n switch {
            var q => n.Value.ToString(), // 1: n
            };
    }
    void M2((string? s1, string? s2)? n)
    {
        _ = n switch {
            (_, _) => n.Value.s1.ToString(), // 2: n.Value.s1
            _      => n.Value.ToString(), // 3: n
            };
    }
    void M3((string? s1, string? s2)? n)
    {
        _ = n switch {
            ({}, _) => n.Value.s1.ToString(),
            (_, _)  => n.Value.s1.ToString(), // 4: n.Value.s1
            _       => n.Value.ToString(), // 5: n
            };
    }
    void M4((string? s1, string? s2)? n)
    {
        _ = n switch {
            (null, _) => n.Value.s2.ToString(), // 6: n.Value.s2
            (_, null) => n.Value.s1.ToString(),
            (_, _)    => n.Value.s1.ToString() + n.Value.s2.ToString(),
            _         => n.Value.ToString(), // 7: n
            };
    }
}
";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics(
                // (7,22): warning CS8629: Nullable value type may be null.
                //             var q => n.Value.ToString(), // 1: n
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "n").WithLocation(7, 22),
                // (13,23): warning CS8602: Dereference of a possibly null reference.
                //             (_, _) => n.Value.s1.ToString(), // 2: n.Value.s1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Value.s1").WithLocation(13, 23),
                // (14,23): warning CS8629: Nullable value type may be null.
                //             _      => n.Value.ToString(), // 3: n
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "n").WithLocation(14, 23),
                // (21,24): warning CS8602: Dereference of a possibly null reference.
                //             (_, _)  => n.Value.s1.ToString(), // 4: n.Value.s1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Value.s1").WithLocation(21, 24),
                // (22,24): warning CS8629: Nullable value type may be null.
                //             _       => n.Value.ToString(), // 5: n
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "n").WithLocation(22, 24),
                // (28,26): warning CS8602: Dereference of a possibly null reference.
                //             (null, _) => n.Value.s2.ToString(), // 6: n.Value.s2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "n.Value.s2").WithLocation(28, 26),
                // (31,26): warning CS8629: Nullable value type may be null.
                //             _         => n.Value.ToString(), // 7: n
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "n").WithLocation(31, 26));
        }

        [Fact]
        [WorkItem(34246, "https://github.com/dotnet/roslyn/issues/34246")]
        public void LearnFromConstantPattern_01()
        {
            var source = @"
class Program
{
    static void M(string? s)
    {
        switch (s?.Length)
        {
            case 0:
                s.ToString();
                break;
        }
    }
}";
            var comp = CreateNullableCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(34233, "https://github.com/dotnet/roslyn/issues/34233")]
        public void SwitchExpressionResultType_01()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
class Test
{
    void Test1(int i, object? x, object y)
    {
        _ = i switch { 1 => x, _ => y }/*T:object?*/;
        _ = i switch { 1 when x != null => x, _ => y }/*T:object!*/;
        _ = i switch { 1 => y, _ => x }/*T:object?*/;
        _ = i switch { 1 => x!, _ => y }/*T:object!*/;
        _ = i switch { 1 => y, _ => x! }/*T:object!*/;
    }

    void Test2(int i, C x, D y)
    {
        _ = i switch { 1 => x, _ => y }/*T:D?*/;
        _ = i switch { 1 => y, _ => x }/*T:D?*/;
    }

    void Test3(int i, IIn<string> x, IIn<object>? y)
    {
        _ = i switch { 1 => x, _ => y }/*T:IIn<string!>?*/;
        _ = i switch { 1 => y, _ => x }/*T:IIn<string!>?*/;
    }

    void Test4(int i, IOut<string> x, IOut<object>? y)
    {
        _ = i switch { 1 => x, _ => y }/*T:IOut<object!>?*/;
        _ = i switch { 1 => y, _ => x }/*T:IOut<object!>?*/;
    }

    void Test5(int i, I<string> x, I<object>? y)
    {
        _ = i switch { 1 => x, _ => y }/*T:!*/; // 1
        _ = i switch { 1 => y, _ => x }/*T:!*/; // 2
    }

    void Test6(int i, I<string> x, I<string?> y)
    {
        _ = i switch { 1 => x, _ => y }/*T:I<string!>!*/; // 3
        _ = i switch { 1 => y, _ => x }/*T:I<string!>!*/; // 4
    }

    void Test7<T>(int i, T x)
    {
        _ = i switch { 1 => x, _ => default }/*T:T*/; // 5
        _ = i switch { 1 => default, _ => x }/*T:T*/; // 6
    }
}

class B {
    public static implicit operator D?(B? b) => throw null!;
}
class C : B {}
class D {}

public interface I<T> { }
public interface IIn<in T> { }
public interface IOut<out T> { }
");
            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (33,15): error CS8506: No best type was found for the switch expression.
                //         _ = i switch { 1 => x, _ => y }/*T:!*/; // 1
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(33, 15),
                // (34,15): error CS8506: No best type was found for the switch expression.
                //         _ = i switch { 1 => y, _ => x }/*T:!*/; // 2
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(34, 15),
                // (39,37): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //         _ = i switch { 1 => x, _ => y }/*T:I<string!>!*/; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<string?>", "I<string>").WithLocation(39, 37),
                // (40,29): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //         _ = i switch { 1 => y, _ => x }/*T:I<string!>!*/; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<string?>", "I<string>").WithLocation(40, 29),
                // (45,37): warning CS8653: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //         _ = i switch { 1 => x, _ => default }/*T:T*/; // 5
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default").WithArguments("T").WithLocation(45, 37),
                // (46,29): warning CS8653: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //         _ = i switch { 1 => default, _ => x }/*T:T*/; // 6
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default").WithArguments("T").WithLocation(46, 29));
        }

        [Fact]
        [WorkItem(39264, "https://github.com/dotnet/roslyn/issues/39264")]
        public void IsPatternSplitState_01()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
#nullable enable
class C
{
    string? field = string.Empty;

    void M1(C c)
    {
        if (c.field == null) return;
        
        c.field.ToString();
    }
    
    void M2(C c)
    {
        if (c is { field: null }) return;
        
        c.field.ToString();
    }

    void M3(C c)
    {
        switch (c)
        {
            case { field: null }:
                break;
            default:
                c.field.ToString();
                break;
        }
    }

    void M4(C c)
    {
        _ = c switch
        {
            { field: null } => string.Empty,
            _ => c.field.ToString(),
        };
    }
}
");
            c.VerifyDiagnostics(
                );
        }

    }
}
