∩╗┐// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            return CreateCompilation(new[] { source }, options: WithNullableEnable());
        }

        [Fact]
        public void VarPatternInfersNullableType()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
public class C
{
    public string Field = null!;
    void M1()
    {
        if (this is { Field: var s })
        {
            s.ToString();
            s = null;
        }
    }

    void M2()
    {
        if (this is (var s) _)
        {
            s.ToString();
            s = null;
        }
    }
    void Deconstruct(out string s) => throw null!;
}
");

            c.VerifyDiagnostics();
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
            x.ToString();
        }
    }
}
");
            c.VerifyDiagnostics(
                // (7,18): error CS0150: A constant value is expected
                //         if (x is nonConstant)
                Diagnostic(ErrorCode.ERR_ConstantExpected, "nonConstant").WithLocation(7, 18)
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
            c = null;
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
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13)
                );
        }

        [Fact]
        [WorkItem(40477, "https://github.com/dotnet/roslyn/issues/40477")]
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
                c = null;
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
                c = null;
            }
        }
    }
}
");
            c.VerifyTypes();
            c.VerifyDiagnostics();
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable());
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
            t1 = default;
        }

        t1.ToString(); // 1

        if (!(o is T t2))
        {
            t2 = tin;
        }
        else
        {
            t2.ToString();
        }
        t2.ToString(); // 2

        if (!(o is T t3)) return;
        t3.ToString();
    }
}
";
            var comp = CreateCompilation(source, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         t1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t1").WithLocation(15, 9),
                // (25,9): warning CS8602: Dereference of a possibly null reference.
                //         t2.ToString(); // 2
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
    void M0(object o)
    {
        var t = (o, o);
        _ = t switch
        {
            (null, null) => 1,
            (null, {}) => 2, // 1
            ({}, null) => 3, // 2
            ({}, {}) => 4, // 3, 4
        };
    }
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
        _ = t switch // 2 not exhaustive
        {
            (1, 2) => 1,
            ({}, {}) => 2,
        };
    }
    void M3(object o)
    {
        var t = (o, o);
        _ = t switch // 3 not exhaustive
        {
            (null, 2) => 1,
            ({}, {}) => 2,
            (null, {}) => 3,
        };
    }
    void M4(object o)
    {
        var t = (o, o);
        _ = t switch // 4 not exhaustive
        {
            { Item1: null, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
        };
    }
    void M5(object o)
    {
        var t = (o, o);
        _ = t switch // 5 not exhaustive
        {
            { Item1: 1, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
        };
    }
    void M6(object o)
    {
        var t = (o, o);
        _ = t switch // 6 not exhaustive
        {
            { Item1: null, Item2: 2 } => 1,
            { Item1: {}, Item2: {} } => 2,
            { Item1: null, Item2: {} } => 3,
        };
    }
    void M7(object o, bool b)
    {
        _ = o switch // 7 not exhaustive
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
                // 0.cs(10,20): hidden CS9271: The pattern is redundant.
                //             (null, {}) => 2, // 1
                Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(10, 20),
                // 0.cs(11,14): hidden CS9271: The pattern is redundant.
                //             ({}, null) => 3, // 2
                Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(11, 14),
                // 0.cs(12,14): hidden CS9271: The pattern is redundant.
                //             ({}, {}) => 4, // 3, 4
                Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(12, 14),
                // 0.cs(12,18): hidden CS9271: The pattern is redundant.
                //             ({}, {}) => 4, // 3, 4
                Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(12, 18),
                // 0.cs(18,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         _ = t switch // 1 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(18, 15),
                // 0.cs(27,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         _ = t switch // 2 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(27, 15),
                // 0.cs(36,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(not null, null)' is not covered.
                //         _ = t switch // 3 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(not null, null)").WithLocation(36, 15),
                // 0.cs(46,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         _ = t switch // 4 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(46, 15),
                // 0.cs(55,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         _ = t switch // 5 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(55, 15),
                // 0.cs(64,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(not null, null)' is not covered.
                //         _ = t switch // 6 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(not null, null)").WithLocation(64, 15),
                // 0.cs(73,15): warning CS8847: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered. However, a pattern with a 'when' clause might successfully match this value.
                //         _ = o switch // 7 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen, "switch").WithArguments("null").WithLocation(73, 15));
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
                // (12,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         return (s1, s2) switch { // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(12, 25),
                // (18,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         return (s1, s2) switch { // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(18, 25),
                // (24,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, _)' is not covered.
                //         return (s1, s2) switch { // 3
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, _)").WithLocation(24, 25),
                // (30,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, "")' is not covered.
                //         return (s1, s2) switch { // 4
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, \"\")").WithLocation(30, 25));
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
        [WorkItem(40477, "https://github.com/dotnet/roslyn/issues/40477")]
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
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 13)
                );
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
        [WorkItem(39888, "https://github.com/dotnet/roslyn/issues/39888")]
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
        _ = i switch { 2 => x, _ => y }/*T:D?*/;
        _ = i switch { 2 => y, _ => x }/*T:D?*/;
    }

    void Test3(int i, IIn<string> x, IIn<object>? y)
    {
        _ = i switch { 3 => x, _ => y }/*T:IIn<string!>?*/;
        _ = i switch { 3 => y, _ => x }/*T:IIn<string!>?*/;
    }

    void Test4(int i, IOut<string> x, IOut<object>? y)
    {
        _ = i switch { 4 => x, _ => y }/*T:IOut<object!>?*/;
        _ = i switch { 4 => y, _ => x }/*T:IOut<object!>?*/;
    }

    void Test5(int i, I<string> x, I<object>? y)
    {
        _ = i switch { 5 => x, _ => y }/*T:!*//*CT:!*/; // 1
        _ = i switch { 5 => y, _ => x }/*T:!*//*CT:!*/; // 2
    }

    void Test6(int i, I<string> x, I<string?> y)
    {
        _ = i switch { 6 => x, _ => y }/*T:I<string!>!*/; // 3
        _ = i switch { 6 => y, _ => x }/*T:I<string!>!*/; // 4
    }

    void Test7<T>(int i, T x)
    {
        _ = i switch { 7 => x, _ => default }/*T:T*/;
        _ = i switch { 7 => default, _ => x }/*T:T*/;
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
                //         _ = i switch { 5 => x, _ => y }/*T:<null>!*//*CT:!*/; // 1
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(33, 15),
                // (34,15): error CS8506: No best type was found for the switch expression.
                //         _ = i switch { 5 => y, _ => x }/*T:<null>!*//*CT:!*/; // 2
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(34, 15),
                // (39,37): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //         _ = i switch { 6 => x, _ => y }/*T:I<string!>!*/; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<string?>", "I<string>").WithLocation(39, 37),
                // (40,29): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //         _ = i switch { 6 => y, _ => x }/*T:I<string!>!*/; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("I<string?>", "I<string>").WithLocation(40, 29)
            );
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
    string? otherField = string.Empty;

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

    void M5(C c)
    {
        if (c is { field: null }) return;
        
        c.otherField.ToString(); // W
    }
}
");
            c.VerifyDiagnostics(
                // (47,9): warning CS8602: Dereference of a possibly null reference.
                //         c.otherField.ToString(); // W
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.otherField").WithLocation(47, 9)
                );
        }

        [Fact]
        [WorkItem(39264, "https://github.com/dotnet/roslyn/issues/39264")]
        public void IsPatternSplitState_02()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
#nullable enable
class C
{
    C? c = null;

    void M1(C c)
    {
        if (c is { c: null })
        {
            if (c.c != null)
            {
                c.c.c.c.ToString();
            }
        }
    }
}
");
            c.VerifyDiagnostics(
                // (13,17): warning CS8602: Dereference of a possibly null reference.
                //                 c.c.c.c.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.c.c").WithLocation(13, 17),
                // (13,17): warning CS8602: Dereference of a possibly null reference.
                //                 c.c.c.c.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.c.c.c").WithLocation(13, 17));
        }

        [Fact]
        [WorkItem(39264, "https://github.com/dotnet/roslyn/issues/39264")]
        public void IsPatternSplitState_03()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
#nullable enable
public class C {
    C? c = null;

    public static void Main()
    {
        C c = new C();
        M1(c, new C());
    }

    static void M1(C c, C c2)
    {
        if (c is { c : null } && c2 is { c: null })
        {
            c.c = c2;
            if (c.c != null)
            {
                c.c.c.ToString(); // warning
            }
        }
    }
}
");
            c.VerifyDiagnostics(
                // (19,17): warning CS8602: Dereference of a possibly null reference.
                //                 c.c.c.ToString(); // warning
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.c.c").WithLocation(19, 17)
                );
        }

        [Fact]
        [WorkItem(40629, "https://github.com/dotnet/roslyn/issues/40629")]
        public void NullTestInSwitch_01()
        {
            CSharpCompilation c = CreateNullableCompilation(@"
#nullable enable
class C
{
    void M(object? p)
    {
        switch (p)
        {
            case null:
                return;
        }

        p.ToString();
    }
}
");
            c.VerifyDiagnostics();
        }

        [Fact]
        public void NotNullIsAPureNullTest()
        {
            var source =
@"#nullable enable
class C
{
    void M1(C? x)
    {
        if (x is not null)
            x.ToString();
        x.ToString(); // 1
    }
    void M2(C x)
    {
        if (x is not null)
            x.ToString();
        x.ToString(); // 2
    }
    void M3(C x)
    {
        if (x is not null or _)
            x.ToString(); // 3
        x.ToString();
    }
    void M4(C x)
    {
        if (x is null or _)
            x.ToString(); // 4
        x.ToString();
    }
    void M5(C x)
    {
        if (x is _ or not null)
            x.ToString(); // 5
        x.ToString();
    }
    void M6(C x)
    {
        if (x is _ or null)
            x.ToString(); // 6
        x.ToString();
    }
    void M7(C x)
    {
        if (x is _ and null)
            x.ToString(); // 7
        x.ToString();
    }
    void M8(C x)
    {
        if (x is _ and not null)
            x.ToString();
        x.ToString(); // 8
    }
    void M9(C x)
    {
        if (x is not null and _)
            x.ToString();
        x.ToString(); // 9
    }
    void M10(int? x)
    {
        if (x is < 0)
            x.Value.ToString();
        x.Value.ToString(); // 10
    }
    void M11(C x)
    {
        if (x is not object)
            x.ToString(); // 11
        else
            x.ToString();
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 9),
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(14, 9),
                // (18,13): warning CS8794: An expression of type 'C' always matches the provided pattern.
                //         if (x is not null or _)
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "x is not null or _").WithArguments("C").WithLocation(18, 13),
                // (19,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(19, 13),
                // (24,13): warning CS8794: An expression of type 'C' always matches the provided pattern.
                //         if (x is null or _)
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "x is null or _").WithArguments("C").WithLocation(24, 13),
                // (25,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(25, 13),
                // (30,13): warning CS8794: An expression of type 'C' always matches the provided pattern.
                //         if (x is _ or not null)
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "x is _ or not null").WithArguments("C").WithLocation(30, 13),
                // (31,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(31, 13),
                // (36,13): warning CS8794: An expression of type 'C' always matches the provided pattern.
                //         if (x is _ or null)
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "x is _ or null").WithArguments("C").WithLocation(36, 13),
                // (37,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(37, 13),
                // (43,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 7
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(43, 13),
                // (50,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString(); // 8
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(50, 9),
                // (56,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString(); // 9
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(56, 9),
                // (62,9): warning CS8629: Nullable value type may be null.
                //         x.Value.ToString(); // 10
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "x").WithLocation(62, 9),
                // (67,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // 11
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(67, 13)
                );
        }

        [Fact]
        [WorkItem(50161, "https://github.com/dotnet/roslyn/issues/50161")]
        public void NestedPattern_Field_01()
        {
            var source =
@"#nullable enable
class E
{
    public E F = new E();
}
class Test
{
    static void M(E e)
    {
        switch (e)
        {
            case { F: { F: { F: { F: null } } } }: break;
        }
        e.F.F.F.F.ToString();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         e.F.F.F.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "e.F.F.F.F").WithLocation(14, 9));
        }

        [Fact]
        [WorkItem(50161, "https://github.com/dotnet/roslyn/issues/50161")]
        public void NestedPattern_Field_02()
        {
            var source =
@"#nullable enable
class E
{
    public E F = new E();
}
class Test
{
    static void M(E e)
    {
        switch (e)
        {
            case { F: { F: { F: { F: { F: null } } } } }: break;
        }
        e.F.F.F.F.F.ToString();
    }
}";
            // No warning because MaxSlotDepth exceeded.
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(50161, "https://github.com/dotnet/roslyn/issues/50161")]
        public void NestedPattern_Property_01()
        {
            var source =
@"#nullable enable
class E
{
    public E P => new E();
}
class Test
{
    static void M(E e)
    {
        switch (e)
        {
            case { P: { P: { P: { P: null } } } }: break;
        }
        e.P.P.P.P.ToString();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         e.P.P.P.P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "e.P.P.P.P").WithLocation(14, 9));
        }

        [Fact]
        [WorkItem(50161, "https://github.com/dotnet/roslyn/issues/50161")]
        public void NestedPattern_Property_02()
        {
            var source =
@"#nullable enable
class E
{
    public E P => new E();
}
class Test
{
    static void M(E e)
    {
        switch (e)
        {
            case { P: { P: { P: { P: { P: null } } } } }: break;
        }
        e.P.P.P.P.P.ToString();
    }
}";
            // No warning because MaxSlotDepth exceeded.
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void NestedLambdaArm_DoesNotObserveStateFromOtherArms()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public void M(object? o, Action? action) {
        _ = o switch
        {
            null => () => { action(); },
            _ => action = new Action(() => {}),
        };
    }
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics(
                // (8,29): warning CS8602: Dereference of a possibly null reference.
                //             null => () => { action(); },
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "action").WithLocation(8, 29)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_01()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    static void M(bool b)
    {
        string? s = null;
        Func<string> a = b switch { true => () => s.ToString(), false => () => s?.ToString() };
    }
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics(
                // (8,51): warning CS8602: Dereference of a possibly null reference.
                //         Func<string> a = b switch { true => () => s.ToString(), false => () => s?.ToString() };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(8, 51),
                // (8,80): warning CS8603: Possible null reference return.
                //         Func<string> a = b switch { true => () => s.ToString(), false => () => s?.ToString() };
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "s?.ToString()").WithLocation(8, 80)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_02()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    static void M(bool b)
    {
        string? s = null;
        var a = (Func<string>)(b switch { true => () => s.ToString(), false => () => s?.ToString() });
    }
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics(
                // (8,57): warning CS8602: Dereference of a possibly null reference.
                //         var a = (Func<string>)(b switch { true => () => s.ToString(), false => () => s?.ToString() });
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(8, 57),
                // (8,86): warning CS8603: Possible null reference return.
                //         var a = (Func<string>)(b switch { true => () => s.ToString(), false => () => s?.ToString() });
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "s?.ToString()").WithLocation(8, 86)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_03()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    static void M(bool b)
    {
        Func<string>? s = () => """";
        Func<object>? a = (b switch { true => s = null, false => () => s() });
    }
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_04()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    static void M(bool b)
    {
        Func<string>? s = () => """";
        Func<object>? a = (b switch { true => () => s(), false => s = null });
    }
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_05()
        {
            var source =
@"#nullable enable
class C
{
    static void M(bool b)
    {
        string? s = null;
        object a = b switch
        { 
            true => () => s.ToString(),
            false => () => s?.ToString()
        };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,24): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //             true => () => s.ToString(),
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(9, 24),
                // (9,27): warning CS8602: Dereference of a possibly null reference.
                //             true => () => s.ToString(),
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(9, 27),
                // (10,25): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //             false => () => s?.ToString()
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(10, 25));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,27): warning CS8602: Dereference of a possibly null reference.
                //             true => () => s.ToString(),
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(9, 27));
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_06()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    static void M(bool b)
    {
        string? s1 = null;
        string? s2 = """";
        M1(s2, b switch
               {
                   true => () => s1.ToString(),
                   false => () => s1?.ToString()
               }).ToString();
    }

    static T M1<T>(T t1, Func<T> t2) => t1;
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics(
                // (11,34): warning CS8602: Dereference of a possibly null reference.
                //                    true => () => s1.ToString(),
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(11, 34),
                // (12,35): warning CS8603: Possible null reference return.
                //                    false => () => s1?.ToString()
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "s1?.ToString()").WithLocation(12, 35)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461")]
        public void TargetTypedSwitch_07()
        {
            var comp = CreateCompilation(@"
interface I {}
class A : I {}
class B : I {}
class C
{
    static void M(I i, A a, B? b, bool @bool)
    {
        M1(i, @bool switch { true => a, false => b }).ToString();
    }

    static T M1<T>(T t1, T t2) => t1;
}
", options: WithNullableEnable());

            comp.VerifyDiagnostics(
                // (9,15): warning CS8604: Possible null reference argument for parameter 't2' in 'I C.M1<I>(I t1, I t2)'.
                //         M1(i, @bool switch { true => a, false => b }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "@bool switch { true => a, false => b }").WithArguments("t2", "I C.M1<I>(I t1, I t2)").WithLocation(9, 15)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461"), WorkItem(49735, "https://github.com/dotnet/roslyn/issues/49735")]
        public void TargetTypedSwitch_08()
        {
            var comp = CreateCompilation(@"
C? c = """".Length switch { > 0 => new A(), _ => new B() };
c.ToString();

class C { }
class A { public static implicit operator C?(A a) => null; }
class B { public static implicit operator C?(B b) => null; }
", options: WithNullableEnable(TestOptions.ReleaseExe));

            comp.VerifyDiagnostics(
                // (3,1): warning CS8602: Dereference of a possibly null reference.
                // c.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(3, 1)
            );
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461"), WorkItem(49735, "https://github.com/dotnet/roslyn/issues/49735")]
        public void TargetTypedSwitch_09()
        {
            var comp = CreateCompilation(@"
C? c = true switch { true => new A(), false => new B() };
c.ToString();

class C { }
class A { public static implicit operator C(A a) => new C(); }
class B { public static implicit operator C?(B b) => null; }
", options: WithNullableEnable(TestOptions.ReleaseExe));

            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(51461, "https://github.com/dotnet/roslyn/issues/51461"), WorkItem(49735, "https://github.com/dotnet/roslyn/issues/49735")]
        public void TargetTypedSwitch_10()
        {
            var comp = CreateCompilation(@"
C? c = false switch { true => new A(), false => new B() };
c.ToString();

class C { }
class A { public static implicit operator C?(A a) => null; }
class B { public static implicit operator C(B b) => new C(); }
", options: WithNullableEnable(TestOptions.ReleaseExe));

            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(51904, "https://github.com/dotnet/roslyn/issues/51904")]
        public void TupleSwitchWithSuppression()
        {
            // When an input value is suppressed, it will get a dedicated
            // slot during DAG analysis, instead of re-using the slot we might
            // get from the expression

            var comp = CreateCompilation(@"
#nullable enable

public class C
{
    public string M1(C? a)
        => a! switch
        {
            C => a.ToString() // 1
        };

    public string M11(C? a)
        => a! switch
        {
            null => string.Empty,
            C => a.ToString() // 2
        };

    public string M111(C? a)
        => a! switch
        {
            null => string.Empty,
            _ => a.ToString() // 3
        };

    public string M2(C? a)
        => (1, a!) switch
        {
            (_, C) => a.ToString() // 4
        };

    public string M22(C? a)
        => (1, a!) switch
        {
            (_, null) => string.Empty,
            (_, C) => a.ToString() // 5, 6
        };

    public string M222(C? a)
        => (1, a!) switch
        {
            (_, null) => string.Empty,
            (_, _) => a.ToString() // 7
        };

    public int M2222(C? a)
        => (1, a!) switch
        {
            (_, null) => 0,
            (_, _) => 1
        };

    public string M3(C? a)
        => (1, a)! switch // 8
        {
            (_, C) => a.ToString()
        };

    public string M4(C? a)
        => (1, (1, a!)) switch
        {
            (_, (_, C)) => a.ToString() // 9
        };

    public string M5(C? a)
        => (1, (1, a)!) switch  // 10
        {
            (_, (_, C)) => a.ToString() // 11
        };

    public string M6(C? a)
        => (1, (1, a))! switch  // 12
        {
            (_, (_, C)) => a.ToString() // 13
        };
}
");

            comp.VerifyDiagnostics(
                // (9,18): warning CS8602: Dereference of a possibly null reference.
                //             C => a.ToString() // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(9, 18),
                // (16,18): warning CS8602: Dereference of a possibly null reference.
                //             C => a.ToString() // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(16, 18),
                // (23,18): warning CS8602: Dereference of a possibly null reference.
                //             _ => a.ToString() // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(23, 18),
                // (29,23): warning CS8602: Dereference of a possibly null reference.
                //             (_, C) => a.ToString() // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(29, 23),
                // (36,17): hidden CS9271: The pattern is redundant.
                //             (_, C) => a.ToString() // 5, 6
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C").WithLocation(36, 17),
                // (36,23): warning CS8602: Dereference of a possibly null reference.
                //             (_, C) => a.ToString() // 5, 6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(36, 23),
                // (43,23): warning CS8602: Dereference of a possibly null reference.
                //             (_, _) => a.ToString() // 7
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(43, 23),
                // (54,20): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(_, null)' is not covered.
                //         => (1, a)! switch // 8
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(_, null)").WithLocation(54, 20),
                // (62,28): warning CS8602: Dereference of a possibly null reference.
                //             (_, (_, C)) => a.ToString() // 9
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(62, 28),
                // (66,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(_, (_, null))' is not covered.
                //         => (1, (1, a)!) switch  // 10
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(_, (_, null))").WithLocation(66, 25),
                // (68,28): warning CS8602: Dereference of a possibly null reference.
                //             (_, (_, C)) => a.ToString() // 11
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(68, 28),
                // (72,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(_, (_, null))' is not covered.
                //         => (1, (1, a))! switch  // 12
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(_, (_, null))").WithLocation(72, 25),
                // (74,28): warning CS8602: Dereference of a possibly null reference.
                //             (_, (_, C)) => a.ToString() // 13
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a").WithLocation(74, 28)
                );
        }

        [Fact, WorkItem(59804, "https://github.com/dotnet/roslyn/issues/59804")]
        public void NestedTypeUsedInPropertyPattern()
        {
            var source = @"
public class Class1
{
    public class Inner1
    {
    }
}

public class Class2
{
    public bool Test()
    {
        Class1 test = null;
        test switch { { Inner1: """" } => """" };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,17): error CS0161: 'Class2.Test()': not all code paths return a value
                //     public bool Test()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Test").WithArguments("Class2.Test()").WithLocation(11, 17),
                // (14,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         test switch { { Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_IllegalStatement, @"test switch { { Inner1: """" } => """" }").WithLocation(14, 9),
                // (14,25): error CS0572: 'Inner1': cannot reference a type through an expression; try 'Class1.Inner1' instead
                //         test switch { { Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_BadTypeReference, "Inner1").WithArguments("Inner1", "Class1.Inner1").WithLocation(14, 25),
                // (14,25): error CS0154: The property or indexer 'Inner1' cannot be used in this context because it lacks the get accessor
                //         test switch { { Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Inner1").WithArguments("Inner1").WithLocation(14, 25)
                );
        }

        [Fact, WorkItem(59804, "https://github.com/dotnet/roslyn/issues/59804")]
        public void NestedTypeUsedInPropertyPattern_ExtendedProperty()
        {
            var source = @"
public class Class1
{
    public Class1 Next { get; set; }

    public class Inner1
    {
    }
}

public class Class2
{
    public bool Test()
    {
        Class1 test = null;
        test switch { { Next.Inner1: """" } => """" };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS0161: 'Class2.Test()': not all code paths return a value
                //     public bool Test()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Test").WithArguments("Class2.Test()").WithLocation(13, 17),
                // (16,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         test switch { { Next.Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_IllegalStatement, @"test switch { { Next.Inner1: """" } => """" }").WithLocation(16, 9),
                // (16,30): error CS0572: 'Inner1': cannot reference a type through an expression; try 'Class1.Inner1' instead
                //         test switch { { Next.Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_BadTypeReference, "Inner1").WithArguments("Inner1", "Class1.Inner1").WithLocation(16, 30),
                // (16,30): error CS0154: The property or indexer 'Inner1' cannot be used in this context because it lacks the get accessor
                //         test switch { { Next.Inner1: "" } => "" };
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Inner1").WithArguments("Inner1").WithLocation(16, 30)
                );
        }

        [Fact, WorkItem(59804, "https://github.com/dotnet/roslyn/issues/59804")]
        public void MethodUsedInPropertyPattern()
        {
            var source = @"
public class Class1
{
    public void Method()
    {
    }
}

public class Class2
{
    public bool Test()
    {
        Class1 test = null;
        test switch { { Method: """" } => """" };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,17): error CS0161: 'Class2.Test()': not all code paths return a value
                //     public bool Test()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Test").WithArguments("Class2.Test()").WithLocation(11, 17),
                // (14,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         test switch { { Method: "" } => "" };
                Diagnostic(ErrorCode.ERR_IllegalStatement, @"test switch { { Method: """" } => """" }").WithLocation(14, 9),
                // (14,25): error CS0154: The property or indexer 'Method' cannot be used in this context because it lacks the get accessor
                //         test switch { { Method: "" } => "" };
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Method").WithArguments("Method").WithLocation(14, 25)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

class C
{
    void M()
    {
        string? s = "";

        for (var x = 0; x < 10; x++)
        {
            var a = Infer(s);
            if (a[0] is var z)
            {
                z.ToString();
            }

            s = null;
        }
    }

    List<T> Infer<T>(T t) => new() { t };
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,17): warning CS8602: Dereference of a possibly null reference.
                //                 z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(15, 17)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration_Tuple()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

class C
{
    void M()
    {
        string? s = "";

        for (var x = 0; x < 10; x++)
        {
            var a = Infer(s);
            if ((a[0], 1) is (var z, var z2))
            {
                z.ToString();
            }

            s = null;
        }
    }

    List<T> Infer<T>(T t) => new() { t };
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,17): warning CS8602: Dereference of a possibly null reference.
                //                 z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(15, 17)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration_ListPattern()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

string? s = "";

for (var x = 0; x < 10; x++)
{
    var a = Infer(s);
    if (a is [var z])
    {
        z.ToString();
    }

    s = null;
}

List<T> Infer<T>(T t) => new() { t };
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(11, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration_ListPattern_Inline()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

string? s = "";

for (var x = 0; x < 10; x++)
{
    if (Infer(s) is [var z])
    {
        z.ToString();
    }

    s = null;
}

List<T> Infer<T>(T t) => new() { t };
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(10, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration_SlicePattern()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    var a = Infer(s);
    if (a is [_, .. var z, _])
    {
        z.ToString();
    }

    s = null;
}

Collection<T> Infer<T>(T t) => throw null!;

class Collection<T>
{
    public int Length => throw null!;
    public T this[System.Index i] => throw null!;
    public T this[System.Range r] => throw null!;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            // Slice is assumed to be never null
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPatternDeclaration_SlicePattern_NestedNullability()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    var a = Infer(s);
    if (a is [_, .. var z, _])
    {
        z.Element.ToString();
    }

    s = null;
}

Collection<T> Infer<T>(T t) => throw null!;

class Collection<T>
{
    public T Element => throw null!;
    public int Length => throw null!;
    public T this[System.Index i] => throw null!;
    public Collection<T> this[System.Range r] => throw null!;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         z.Element.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z.Element").WithLocation(10, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithDeconstructionPattern()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    if (Infer(s) is (var z, var z2))
    {
        z.ToString(); // 1
    }

    s = null;
}

s = "";
if (Infer(s) is (var y, var y2))
{
    y.ToString();
}

s = null;
if (Infer(s) is (var w, var w2))
{
    w.ToString(); // 2
}

Container<T> Infer<T>(T t) => throw null!;

class Container<T>
{
    public void Deconstruct(out T t1, out T t2) => throw null!;
}
""";
            // Need to re-infer Deconstruct method https://github.com/dotnet/roslyn/issues/34232
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithFieldPattern()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    var a = Infer(s);
    if (a is { field: var z })
    {
        z.ToString(); // 1
    }

    s = null;
}


Container<T> Infer<T>(T t) => throw null!;

class Container<T>
{
    public T field = default!;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(10, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithFieldPattern_Inline()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    if (Infer(s) is { field: var z })
    {
        z.ToString(); // 1
    }

    s = null;
}


Container<T> Infer<T>(T t) => throw null!;

class Container<T>
{
    public T field = default!;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(9, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithPropertyPattern()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    var a = Infer(s);
    if (a is { field: var z })
    {
        z.ToString(); // 1
    }

    s = null;
}


Container<T> Infer<T>(T t) => throw null!;

class Container<T>
{
    public T field => default!;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(10, 9)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithLocalDeclaration()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

string? s = "";

for (var x = 0; x < 10; x++)
{
    var z = Infer(s)[0];
    z.ToString();

    s = null;
}

List<T> Infer<T>(T t) => new() { t };
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,5): warning CS8602: Dereference of a possibly null reference.
                //     z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(9, 5)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithDeconstructionDeclaration()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

string? s = "";

for (var x = 0; x < 10; x++)
{
    (var z, var z2) = (Infer(s)[0], 1);
    z.ToString();

    s = null;
}

List<T> Infer<T>(T t) => new() { t };
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,5): warning CS8602: Dereference of a possibly null reference.
                //     z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(9, 5)
                );
        }

        [Fact, WorkItem(65976, "https://github.com/dotnet/roslyn/issues/65976")]
        public void LoopWithDeconstructionDeclaration_CustomType()
        {
            var source = """
#nullable enable

string? s = "";

for (var x = 0; x < 10; x++)
{
    (var z, var z2) = Infer(s);
    z.ToString();

    s = null;
}

Container<T> Infer<T>(T t) => throw null!;

class Container<T>
{
    public void Deconstruct(out T t1, out T t2) => throw null!;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,5): warning CS8602: Dereference of a possibly null reference.
                //     z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(8, 5)
                );
        }
    }
}
