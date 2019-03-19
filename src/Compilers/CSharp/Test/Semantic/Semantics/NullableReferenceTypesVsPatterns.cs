// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.NullableReferenceTypes, CompilerFeature.Patterns)]
    public class NullableReferenceTypesVsPatterns : CSharpTestBase
    {

        [Fact]
        public void ConditionalBranching_IsConstantPattern_Null()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NullInverted()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (12,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NonNull()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (13,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NullConstant()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_NonConstant()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (7,18): error CS0150: A constant value is expected
                //         if (x is nonConstant)
                Diagnostic(ErrorCode.ERR_ConstantExpected, "nonConstant").WithLocation(7, 18),
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 13)
                );
        }

        [Fact]
        [WorkItem(29868, "https://github.com/dotnet/roslyn/issues/29868")]
        public void ConditionalBranching_IsConstantPattern_Null_AlreadyTestedAsNonNull()
        {
            // https://github.com/dotnet/roslyn/issues/29868: confirm that we want such hidden warnings
            CSharpCompilation c = CreateCompilation(new[] { @"
class C
{
    void Test(object? x)
    {
        if (x != null)
        {
            if (x is null) // hidden
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (10,17): warning CS8602: Possible dereference of a null reference.
                //                 x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(10, 17)
                );
        }

        [Fact]
        public void ConditionalBranching_IsConstantPattern_Null_AlreadyTestedAsNull()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
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
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (10,17): warning CS8602: Possible dereference of a null reference.
                //                 x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(10, 17)
                );
        }

        [Fact]
        public void ConditionalBranching_IsDeclarationPattern()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
class C
{
    void Test(object? x)
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
}
" }, options: WithNonNullTypesTrue());

            c.VerifyDiagnostics(
                // (13,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
class C
{
    void Test(object? x)
    {
        if (x is var c)
        {
            x.ToString(); // warn 1
            c /*T:object?*/ .ToString();
        }
        else
        {
            x.ToString();
        }
    }
}
" }, options: WithNonNullTypesTrue());

            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // warn 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13),
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             c /*T:object?*/ .ToString(); // warn 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern_Discard()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
class C
{
    void Test(object? x)
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
}
" }, options: WithNonNullTypesTrue());

            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             x.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 13));
        }

        [Fact]
        public void ConditionalBranching_IsVarDeclarationPattern_AlreadyTestedAsNonNull()
        {
            CSharpCompilation c = CreateCompilation(new[] { @"
class C
{
    void Test(object? x)
    {
        if (x != null)
        {
            if (x is var c)
            {
                c /*T:object!*/ .ToString();
                c = null; // warn
            }
        }
    }
}
" }, options: WithNonNullTypesTrue());

            c.VerifyTypes();
            c.VerifyDiagnostics(
                // (11,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //                 c = null; // warn
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(11, 21)
                );
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_DeclarationPattern()
        {
            var source =
@"class C
{
    static void G(string? s)
    {
        if (s?.ToString() is string t)
        {
            s.ToString();
        }
        else
        {
            s.ToString(); // warn
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             s.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(11, 13)
                );
        }

        [Fact]
        public void IsPattern_AffectsNullConditionalOperator_NullableValueType()
        {
            var source =
@"class C
{
    static void G(int? i)
    {
        if (i?.ToString() is string t)
        {
            i.Value.ToString();
        }
        else
        {
            i.Value.ToString(); // warn
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (11,13): warning CS8629: Nullable value type may be null.
                //             i.Value.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "i").WithLocation(11, 13)
                );
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (7,13): warning CS8602: Possible dereference of a null reference.
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (7,13): warning CS8602: Possible dereference of a null reference.
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
                F(x); // string s
                break;
            case object y when y is string t:
                F(y);
                F(t);
                F(x); // object y
                break;
            case null:
                F(x); // null
                break;
            default:
                F(x); // default
                break;
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (18,19): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F(object o)'.
                //                 F(x); // null
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         x2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(19, 9),
                // (23,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 2
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(23, 14),
                // (29,9): warning CS8602: Possible dereference of a null reference.
                //         x3.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(29, 9));
            comp.VerifyTypes();
        }

        [Fact]
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (23,9): warning CS8602: Possible dereference of a null reference.
                //         t2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t2").WithLocation(23, 9),
                // (29,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         t3 = null; // 2
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(29, 14),
                // (35,9): warning CS8602: Possible dereference of a null reference.
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         x1.ToString(); // 1 (because of x1?. above)
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(12, 9),
                // (23,9): warning CS8602: Possible dereference of a null reference.
                //         x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(23, 9));
        }

        [Fact]
        [WorkItem(30952, "https://github.com/dotnet/roslyn/issues/30952")]
        public void IsDeclarationPattern_NeverNull_02()
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
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (23,9): warning CS8602: Possible dereference of a null reference.
                //         t2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t2").WithLocation(23, 9));
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (11,13): error CS0165: Use of unassigned local variable 'y1'
                //             y1.ToString(); // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(11, 13),
                // (21,13): warning CS8602: Possible dereference of a null reference.
                //             x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(21, 13),
                // (22,13): error CS0165: Use of unassigned local variable 'y2'
                //             y2.ToString(); // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(22, 13),
                // (27,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 4
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(27, 14),
                // (33,13): warning CS8602: Possible dereference of a null reference.
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
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (7,9): error CS0165: Use of unassigned local variable 'y1'
                //         y1.ToString(); // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(7, 9),
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         x2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(12, 9),
                // (13,9): error CS0165: Use of unassigned local variable 'y2'
                //         y2.ToString(); // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(13, 9),
                // (17,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x3 = null; // 4
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(17, 14),
                // (19,9): warning CS8602: Possible dereference of a null reference.
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

            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (12,18): warning CS8652: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //             t1 = default; // 1
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default").WithArguments("T").WithLocation(12, 18),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         t1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t1").WithLocation(15, 9),
                // (25,9): warning CS8602: Possible dereference of a null reference.
                //         t2.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t2").WithLocation(25, 9)
                );
        }

        [WorkItem(32503, "https://github.com/dotnet/roslyn/issues/32503")]
        [Fact]
        public void PatternDeclarationBreaksNullableAnalysis()
        {
            var source = @"
#nullable enable
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void InferenceWithITuplePattern()
        {
            var source = @"
#nullable enable
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecursivePatternNullInferenceWithDowncast_01()
        {
            var source = @"
#nullable enable
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecursivePatternNullInferenceWithDowncast_02()
        {
            var source = @"
#nullable enable
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,13): warning CS8602: Possible dereference of a null reference.
                //             b.Value.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Value").WithLocation(15, 13));
        }

        [Fact]
        public void TuplePatternNullInference_01()
        {
            var source = @"
#nullable enable
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
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item2").WithLocation(12, 13));
        }

        [Fact]
        public void MultiplePathsThroughDecisionDag_01()
        {
            var source = @"
#nullable enable
class Program
{
    bool M(object? o, bool cond = true)
    {
        o = 1;
        switch (o)
        {
            case null:
                throw null!;
            case """" when M(o = null):
                break;
            default:
                if (cond) o.ToString(); // warning
                break;
        }

        return cond;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,27): warning CS8602: Possible dereference of a null reference.
                //                 if (cond) o.ToString(); // warning
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 27));
        }

        [Fact]
        [WorkItem(30597, "https://github.com/dotnet/roslyn/issues/30597")]
        [WorkItem(32414, "https://github.com/dotnet/roslyn/issues/32414")]
        public void NotExhaustiveForNull_01()
        {
            var source = @"
#nullable enable
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = t switch // 1 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(8, 15),
                // (36,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = t switch // 2 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(36, 15),
                // (63,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         _ = o switch // 3 not exhaustive
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(63, 15));
        }

        [Fact, WorkItem(31881, "https://github.com/dotnet/roslyn/issues/31881")]
        public void NullableVsPattern_31881()
        {
            var source = @"
using System;
#nullable enable

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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(33499, "https://github.com/dotnet/roslyn/issues/33499")]
        public void PatternVariablesAreNotOblivious_33499()
        {
            var source = @"
#nullable enable
class Test
{
    static void M(object o)
    {
        if (o is string s) { }
        s = null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 13));
        }

        [Fact]
        [WorkItem(30597, "https://github.com/dotnet/roslyn/issues/30597")]
        [WorkItem(32414, "https://github.com/dotnet/roslyn/issues/32414")]
        public void NotExhaustiveForNull_02()
        {
            var source = @"
#nullable enable
class Test
{
    int M1(string s1, string s2)
    {
        return (s1, s2) switch {
            (string x, string y) => x.Length + y.Length
            };
    }
    int M2(string? s1, string? s2)
    {
        return (s1, s2) switch { // 1
            (string x, string y) => x.Length + y.Length
            };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive).
                //         return (s1, s2) switch { // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithLocation(13, 25));
        }

        [Fact]
        public void IsPatternAlwaysFalse()
        {
            var source = @"
#nullable enable
class Test
{
    void M1(ref object o)
    {
        if (2 is 3)
            o = null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8519: The given expression never matches the provided pattern.
                //         if (2 is 3)
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "2 is 3").WithLocation(7, 13));
        }

        [Fact]
        [WorkItem(29619, "https://github.com/dotnet/roslyn/issues/29619")]
        public void StructWithNotBackedProperty()
        {
            var source = @"
#nullable enable
struct Point
{
    public object X, Y;
    public Point Mirror => new Point { X = Y, Y = X };
    bool Test => this is { X: 1, Y: 2, Mirror: { X: 2, Y: 1 } };
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }
    }
}
