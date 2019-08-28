// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // NOTE: Skipped some expression tests.
    public class FlowTests : CSharpTestBase
    {
        private const string prefix = @"
using System;

// Need a base class with indexers.
public class DATestBase {
    public int this[int a] { get { return 0; } }
    public int this[int a, int b] { get { return 0; } }
}

// Need a struct with a couple fields.
public struct A {
    public int x;
    public int y;
}

// Need a struct with non-lifted short-circuiting operators.
public struct NLS
{
    public static NLS operator&(NLS a, NLS b) { return new NLS { value = a.value & b.value }; }
    public static NLS operator|(NLS a, NLS b) { return new NLS { value = a.value | b.value }; }
    public static bool operator true(NLS a) { return a.value; }
    public static bool operator false(NLS a) { return !a.value; }

    public bool value;
}

// Need a struct with lifted short-circuiting operators.
public struct LS
{
    public static LS operator&(LS a, LS b) { return new LS { value = a.value & b.value }; }
    public static LS operator|(LS a, LS b) { return new LS { value = a.value | b.value }; }
    public static bool operator true(LS? a) { return a.HasValue && a.Value.value; }
    public static bool operator false(LS? a) { return a.HasValue && !a.Value.value; }

    public bool value;
}

public delegate void D(); public delegate int DI();
public delegate void DefP(int a, ref int b, out int c);

public class DATest : DATestBase {
    public static volatile bool f;
    public static volatile int val;
    public static volatile byte b;
    public const bool fTrue = true;
    public const bool fFalse = false;
    public static int[] arr = { 1, 2, 3 };

    public static bool No() { return f; } // No-op
    public static bool F(int x) { return f; }
    public static bool G(out int x) { x = 0; return f; }
    public static bool Q(bool x) { return f; }
    public static bool S(A x) { return f; }
    public static int NNo() { return val; } // No-op
    public static int NF(int x) { return val; }
    public static int NG(out int x) { x = 0; return val; }
    public static int[] AF(int x) { return arr; }
    public static int[] AG(out int x) { x = 0; return arr; }
    public static int FA(int[] x) { return val; }
    public static int GA(out int[] x) { x = arr; return val; }
    public static IDisposable Res(bool x) { return null; }
    public static bool FP(params int[] x) { return f; }
    public static bool GP(out int x, params int[] y) { x = 0; return f; }
    public static NLS GetNLS() { return new NLS { value = f }; }
    public static NLS GetNLS(out int x) { x = 0; return new NLS { value = f }; }
    public static LS GetLS() { return new LS { value = f }; }
    public static LS? GetLS(out int x) { x = 0; return new LS { value = f }; }

    public class C {
        public C(params int[] x) { }
        public C(out int x, params int[] y) { x = 0; }
    }
";

        private const string suffix = @"
}";

        [Fact]
        [WorkItem(35011, "https://github.com/dotnet/roslyn/issues/35011")]
        public void SwitchConstantUnreachable()
        {
            var src = @"
class C
{
    const string S = ""abc"";

    public static string M1()
    {
        switch (S)
        {
            case ""abc"":
                return S;
        }
    }

    public static string M2()
    {
        const string S2 = S + """";
        switch (S)
        {
            case S2:
                return S;
        }
    }

    public static string M3()
    {
        const int I = 11;
        switch (I)
        {
            case 11:
                return S;
        }
    }

    public static string M4()
    {
        switch (S)
        {
            case ""def"":
                return S; // 1
            default:
                return S;
        }
    }

    public static string M5()
    {
        switch (S)
        {
            case ""def"":
                return S; // 2
        }
        // error
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (40,17): warning CS0162: Unreachable code detected
                //                 return S; // 1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(40, 17),
                // (46,26): error CS0161: 'C.M5()': not all code paths return a value
                //     public static string M5()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M5").WithArguments("C.M5()").WithLocation(46, 26),
                // (51,17): warning CS0162: Unreachable code detected
                //                 return S; // 2
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(51, 17));
            comp = CreateCompilation(src, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (40,17): warning CS0162: Unreachable code detected
                //                 return S; // 1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(40, 17),
                // (46,26): error CS0161: 'C.M5()': not all code paths return a value
                //     public static string M5()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M5").WithArguments("C.M5()").WithLocation(46, 26),
                // (51,17): warning CS0162: Unreachable code detected
                //                 return S; // 2
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(51, 17));
        }

        [Fact]
        public void General()
        {
            var source = prefix + @"
    // Value params and ref params are definitely assigned. Out params are not.
    public void T000(int a) { F(a); }
    public void T001(ref int a) { F(a); }
    public void T002(out int a) { F(a); G(out a); } // Error

    // Out params must be definitely assigned before leaving.
    public void T010(out int a) { } // Error
    public void T011(out int a) { G(out a); }
    public void T012(out int a) { if (f) G(out a); } // Error
    public void T013(out int a) { if (fTrue) G(out a); }
    public void T014(out int a) { if (f) G(out a); else throw new Exception(); }

    // General.
    public void T020() {
        { int a; F(a); } // Error
        { int a; if (fTrue) F(a); } // Error
        { int a; if (fFalse) F(a); } // Unreachable
        { int a; if (fFalse) F(a); else F(a); } // Error + Unreachable
        { int a; if (fFalse) F(a); else G(out a); F(a); } // Unreachable
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (52,37): error CS0269: Use of unassigned out parameter 'a'
                //     public void T002(out int a) { F(a); G(out a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "a").WithArguments("a"),
                // (55,17): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                //     public void T010(out int a) { } // Error
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "T010").WithArguments("a"),
                // (57,17): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                //     public void T012(out int a) { if (f) G(out a); } // Error
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "T012").WithArguments("a"),
                // (65,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (66,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) F(a); else F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (67,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) F(a); else G(out a); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (63,20): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (64,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (66,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse) F(a); else F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact]
        public void IfStatement()
        {
            var source = prefix + @"
    // If statement.
    public void T100() {
        { int a; if (F(a)) No(); } // Error
        { int a; if (F(a)) No(); else No(); } // Error

        { int a; if (f) F(a); } // Error
        { int a; if (f) F(a); else No(); } // Error
        { int a; if (f) No(); else F(a); } // Error

        { int a; if (fFalse) F(a); } // Unreachable
        { int a; if (fFalse) F(a); else No(); } // Unreachable
        { int a; if (fFalse) No(); else F(a); } // Error + Unreachable

        { int a; if (fTrue) F(a); } // Error
        { int a; if (fTrue) F(a); else No(); } // Error + Unreachable
        { int a; if (fTrue) No(); else F(a); } // Unreachable

        { int a; if (G(out a)) F(a); }
        { int a; if (G(out a)) F(a); else No(); }
        { int a; if (G(out a)) No(); else F(a); }
        { int a; if (G(out a)) No(); F(a); }
        { int a; if (G(out a)) No(); else No(); F(a); }

        // Assigned after true.
        { int a; if (f && G(out a)) F(a); }
        { int a; if (f && G(out a)) F(a); else No(); }
        { int a; if (f && G(out a)) No(); else F(a); } // Error
        { int a; if (f && G(out a)) No(); F(a); } // Error
        { int a; if (f && G(out a)) No(); else No(); F(a); } // Error
        { int a; if (f && G(out a)) No(); else G(out a); F(a); }

        // Unassigned due to user-defined operators.
        { int a; if (GetNLS() && GetNLS(out a)) F(a); } // error
        { int a; if (GetLS() && GetLS(out a)) F(a); } // error
        { int a; if (f && G(out a)) F(a); else No(); } // error
        { int a; if (f && G(out a)) F(a); else No(); } // error
        { int a; if (GetNLS() && GetNLS(out a)) No(); else F(a); } // Error
        { int a; if (GetLS() && GetLS(out a)) No(); else F(a); } // Error
        { int a; if (GetNLS() && GetNLS(out a)) No(); F(a); } // Error
        { int a; if (GetLS() && GetLS(out a)) No(); F(a); } // Error
        { int a; if (GetNLS() && GetNLS(out a)) No(); else No(); F(a); } // Error
        { int a; if (GetLS() && GetLS(out a)) No(); else No(); F(a); } // Error
        { int a; if (GetNLS() && GetNLS(out a)) No(); else G(out a); F(a); } // Error
        { int a; if (GetLS() && GetLS(out a)) No(); else G(out a); F(a); } // Error

        // Assigned.
        { int a; if (fTrue && G(out a)) F(a); }
        { int a; if (fTrue && G(out a)) F(a); else No(); }
        { int a; if (fTrue && G(out a)) No(); else F(a); }
        { int a; if (fTrue && G(out a)) No(); F(a); }
        { int a; if (fTrue && G(out a)) No(); else No(); F(a); }

        // Unassigned, consequence and alternative are reachable
        { int a; if (fFalse && G(out a)) F(a); } // Error
        { int a; if (fFalse && G(out a)) F(a); else No(); } // Error
        { int a; if (fFalse && G(out a)) No(); else F(a); } // Error
        { int a; if (fFalse && G(out a)) No(); F(a); } // Error 
        { int a; if (fFalse && G(out a)) No(); else No(); F(a); } // Error

        // Unassigned.  Unreachable expr considered assigned.
        { int a; if (fFalse && F(a)) F(a); } // Error
        { int a; if (fFalse && F(a)) F(a); else No(); }  // Error
        { int a; if (fFalse && F(a)) No(); else F(a); }  // Error
        { int a; if (fFalse && F(a)) No(); F(a); } // Error
        { int a; if (fFalse && F(a)) No(); else No(); F(a); } // Error

        // Assigned after false.
        { int a; if (f || G(out a)) F(a); } // Error
        { int a; if (f || G(out a)) F(a); else No(); } // Error
        { int a; if (f || G(out a)) No(); else F(a); }
        { int a; if (f || G(out a)) No(); F(a); } // Error
        { int a; if (f || G(out a)) No(); else No(); F(a); } // Error
        { int a; if (f || G(out a)) G(out a); else No(); F(a); }

        // Unassigned due to user-defined operators.
        { int a; if (GetNLS() || GetNLS(out a)) F(a); } // Error
        { int a; if (GetLS() || GetLS(out a)) F(a); } // Error
        { int a; if (GetNLS() || GetNLS(out a)) F(a); else No(); } // Error
        { int a; if (GetLS() || GetLS(out a)) F(a); else No(); } // Error
        { int a; if (GetNLS() || GetNLS(out a)) No(); else F(a); } // Error
        { int a; if (GetLS() || GetLS(out a)) No(); else F(a); } // Error
        { int a; if (GetNLS() || GetNLS(out a)) No(); F(a); } // Error
        { int a; if (GetLS() || GetLS(out a)) No(); F(a); } // Error
        { int a; if (GetNLS() || GetNLS(out a)) No(); else No(); F(a); } // Error
        { int a; if (GetLS() || GetLS(out a)) No(); else No(); F(a); } // Error
        { int a; if (GetNLS() || GetNLS(out a)) G(out a); else No(); F(a); } // Error
        { int a; if (GetLS() || GetLS(out a)) G(out a); else No(); F(a); } // Error

        // Unassigned. G(out a) is unreachable expr.
        { int a; if (fTrue || G(out a)) F(a); } // Error
        { int a; if (fTrue || G(out a)) F(a); else No(); } // Error 
        { int a; if (fTrue || G(out a)) No(); else F(a); } // Error
        { int a; if (fTrue || G(out a)) No(); F(a); } // Error
        { int a; if (fTrue || G(out a)) No(); else No(); F(a); } // Error 
        { int a; if (fTrue || G(out a)) G(out a); else No(); F(a); } // Error

        // Assigned.
        { int a; if (fFalse || G(out a)) F(a); }
        { int a; if (fFalse || G(out a)) F(a); else No(); }
        { int a; if (fFalse || G(out a)) No(); else F(a); }
        { int a; if (fFalse || G(out a)) No(); F(a); }
        { int a; if (fFalse || G(out a)) No(); else No(); F(a); }
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (83,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (84,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) F(a); else No(); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (85,30): warning CS0162: Unreachable code detected
                //         { int a; if (fFalse) No(); else F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "No"),
                // (88,40): warning CS0162: Unreachable code detected
                //         { int a; if (fTrue) F(a); else No(); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "No"),
                // (89,40): warning CS0162: Unreachable code detected
                //         { int a; if (fTrue) No(); else F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (76,24): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (F(a)) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (77,24): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (F(a)) No(); else No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (79,27): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (80,27): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f) F(a); else No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (81,38): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (85,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse) No(); else F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (87,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (88,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue) F(a); else No(); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (100,50): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && G(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (101,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && G(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (102,56): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && G(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // (106,51): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() && GetNLS(out a)) F(a); } // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(106, 51),
                // (107,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() && GetLS(out a)) F(a); } // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(107, 49),
                // (110,62): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() && GetNLS(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(110, 62),
                // (111,60): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() && GetLS(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(111, 60),
                // (112,57): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() && GetNLS(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(112, 57),
                // (113,55): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() && GetLS(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(113, 55),
                // (114,68): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() && GetNLS(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(114, 68),
                // (115,66): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() && GetLS(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(115, 66),
                // (116,72): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() && GetNLS(out a)) No(); else G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(116, 72),
                // (117,70): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() && GetLS(out a)) No(); else G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(117, 70),

                // Note: Dev10 spuriously reports (127,46,127,47): error CS0165: Use of unassigned local variable 'a'
                // Note: Dev10 spuriously reports (128,46,128,47): error CS0165: Use of unassigned local variable 'a'

                // (129,55): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && G(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(129, 55),
                // (130,50): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && G(out a)) No(); F(a); } // Error 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(130, 50),
                // (131,61): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && G(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(131, 61),

                // Note: Dev10 spuriously reports (134,42,134,43): error CS0165: Use of unassigned local variable 'a'
                // Note: Dev10 spuriously reports (135,42,135,43): error CS0165: Use of unassigned local variable 'a'

                // (136,51): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && F(a)) No(); else F(a); }  // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(136, 51),
                // (137,46): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && F(a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(137, 46),
                // (138,57): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fFalse && F(a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(138, 57),

                // (141,39): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || G(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(141, 39),
                // (142,39): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || G(out a)) F(a); else No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(142, 39),
                // (144,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || G(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(144, 45),
                // (145,56): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || G(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(145, 56),

                // (149,51): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(149, 51),
                // (150,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(150, 49),
                // (151,51): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) F(a); else No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(151, 51),
                // (152,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) F(a); else No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(152, 49),
                // (153,62): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(153, 62),
                // (154,60): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(154, 60),
                // (155,57): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(155, 57),
                // (156,55): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(156, 55),
                // (157,68): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(157, 68),
                // (158,66): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) No(); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(158, 66),
                // (159,72): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetNLS() || GetNLS(out a)) G(out a); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(159, 72),
                // (160,70): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (GetLS() || GetLS(out a)) G(out a); else No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(160, 70),

                // (163,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue || G(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(163, 43),
                // (164,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue || G(out a)) F(a); else No(); } // Error 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(164, 43),

                // Note: Dev10 spuriously reports (165,56,165,57): error CS0165: Use of unassigned local variable 'a'

                // (166,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue || G(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(166, 49),
                // (167,60): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (fTrue || G(out a)) No(); else No(); F(a); } // Error 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(167, 60)

                // Note: Dev10 spuriously reports (168,66,168,67): error CS0165: Use of unassigned local variable 'a'
                );
        }

        [Fact]
        public void SwitchStatement()
        {
            var source = prefix + @"
    // Switch statement.
    public void T110() {
        if (f) { int a; switch (a) { case 0: No(); break; } } // Error
        if (f) { int a; switch (val) { case 0: F(a); break; } } // Error
        if (f) { int a; switch (val) { case 0: G(out a); break; case 1: F(a); break; } } // Error
        if (f) { int a; switch (G(out a)) { case false: F(a); break; case true: No(); break; } }
        if (f) { int a; switch (G(out a)) { case false: No(); break; case true: F(a); break; } }

        // Assigned after false has no affect.
        if (f) { int a; switch (f || G(out a)) { case false: F(a); break; case true: No(); break; } } // Error

        // Assigned after true has no affect.
        if (f) { int a; switch (f && G(out a)) { case false: No(); break; case true: F(a); break; } } // Error

        // Exhaust cases
        if (f) { int a; switch (f) { case false: G(out a); break; case true: G(out a); break; } F(a); }
        if (f) { int a; switch (val) { case 0: goto default; default: G(out a); break; } F(a); }
        if (f) { int a; switch (val) { case 0: G(out a); break; default: goto case 0; } F(a); }
        if (f) { int a; switch (val) { case 0: G(out a); break; default: goto LSkip; } F(a); LSkip: No(); }
        if (f) { int a; switch (val) { case 0: G(out a); goto LHave; default: break; } No(); goto LSkip; LHave: F(a); LSkip: No(); }

        if (f) { int a;
            switch (b) {
            case 0x00: case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x06: case 0x07: case 0x08: case 0x09: case 0x0A: case 0x0B: case 0x0C: case 0x0D: case 0x0E: case 0x0F:
            case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x16: case 0x17: case 0x18: case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1E: case 0x1F:
            case 0x20: case 0x21: case 0x22: case 0x23: case 0x24: case 0x25: case 0x26: case 0x27: case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C: case 0x2D: case 0x2E: case 0x2F:
            case 0x30: case 0x31: case 0x32: case 0x33: case 0x34: case 0x35: case 0x36: case 0x37: case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F:
            case 0x40: case 0x41: case 0x42: case 0x43: case 0x44: case 0x45: case 0x46: case 0x47: case 0x48: case 0x49: case 0x4A: case 0x4B: case 0x4C: case 0x4D: case 0x4E: case 0x4F:
            case 0x50: case 0x51: case 0x52: case 0x53: case 0x54: case 0x55: case 0x56: case 0x57: case 0x58: case 0x59: case 0x5A: case 0x5B: case 0x5C: case 0x5D: case 0x5E: case 0x5F:
            case 0x60: case 0x61: case 0x62: case 0x63: case 0x64: case 0x65: case 0x66: case 0x67: case 0x68: case 0x69: case 0x6A: case 0x6B: case 0x6C: case 0x6D: case 0x6E: case 0x6F:
            case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75: case 0x76: case 0x77: case 0x78: case 0x79: case 0x7A: case 0x7B: case 0x7C: case 0x7D: case 0x7E: case 0x7F:
                G(out a);
                break;
            case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: case 0x86: case 0x87: case 0x88: case 0x89: case 0x8A: case 0x8B: case 0x8C: case 0x8D: case 0x8E: case 0x8F:
            case 0x90: case 0x91: case 0x92: case 0x93: case 0x94: case 0x95: case 0x96: case 0x97: case 0x98: case 0x99: case 0x9A: case 0x9B: case 0x9C: case 0x9D: case 0x9E: case 0x9F:
            case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: case 0xA6: case 0xA7: case 0xA8: case 0xA9: case 0xAA: case 0xAB: case 0xAC: case 0xAD: case 0xAE: case 0xAF:
            case 0xB0: case 0xB1: case 0xB2: case 0xB3: case 0xB4: case 0xB5: case 0xB6: case 0xB7: case 0xB8: case 0xB9: case 0xBA: case 0xBB: case 0xBC: case 0xBD: case 0xBE: case 0xBF:
            case 0xC0: case 0xC1: case 0xC2: case 0xC3: case 0xC4: case 0xC5: case 0xC6: case 0xC7: case 0xC8: case 0xC9: case 0xCA: case 0xCB: case 0xCC: case 0xCD: case 0xCE: case 0xCF:
            case 0xD0: case 0xD1: case 0xD2: case 0xD3: case 0xD4: case 0xD5: case 0xD6: case 0xD7: case 0xD8: case 0xD9: case 0xDA: case 0xDB: case 0xDC: case 0xDD: case 0xDE: case 0xDF:
            case 0xE0: case 0xE1: case 0xE2: case 0xE3: case 0xE4: case 0xE5: case 0xE6: case 0xE7: case 0xE8: case 0xE9: case 0xEA: case 0xEB: case 0xEC: case 0xED: case 0xEE: case 0xEF:
            case 0xF0: case 0xF1: case 0xF2: case 0xF3: case 0xF4: case 0xF5: case 0xF6: case 0xF7: case 0xF8: case 0xF9: case 0xFA: case 0xFB: case 0xFC: case 0xFD: case 0xFE: case 0xFF:
                G(out a);
                break;
            }
            F(a); // Error - we don't consider enumerating values for integral types to be exhaustive
        }

        if (f) { int a; switch (val) { default: goto case 0; case 0: goto default; } F(a); } // Unreachable
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (121,86): warning CS0162: Unreachable code detected
                //         if (f) { int a; switch (val) { default: goto case 0; case 0: goto default; } F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F").WithLocation(121, 86),
                // (76,33): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; switch (a) { case 0: No(); break; } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(76, 33),
                // (77,50): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; switch (val) { case 0: F(a); break; } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(77, 50),
                // (78,75): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; switch (val) { case 0: G(out a); break; case 1: F(a); break; } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(78, 75),
                // (83,64): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; switch (f || G(out a)) { case false: F(a); break; case true: No(); break; } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(83, 64),
                // (86,88): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; switch (f && G(out a)) { case false: No(); break; case true: F(a); break; } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(86, 88),
                // (118,15): error CS0165: Use of unassigned local variable 'a'
                //             F(a); // Error - we don't consider enumerating values for integral types to be exhaustive
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(118, 15)
                );
        }

        [Fact]
        public void WhileStatement()
        {
            var source = prefix + @"
    // While statement.
    public void T120() {
        // Unassigned.
        if (f) { int a; while (F(a)) No(); } // Error
        if (f) { int a; while (f) F(a); } // Error
        if (f) { int a; while (f) No(); F(a); } // Error
        if (f) { int a; while (f) G(out a); F(a); } // Error
        if (f) { int a; while (fFalse) F(a); } // Unreachable
        if (f) { int a; while (fTrue) No(); F(a); } // Unreachable

        // Assigned.
        if (f) { int a; while (G(out a)) F(a); }
        if (f) { int a; while (G(out a)) No(); F(a); }

        // Assigned after true.
        if (f) { int a; while (f && G(out a)) F(a); }
        if (f) { int a; while (f && G(out a)) No(); F(a); } // Error

        // Assigned
        if (f) { int a; while (fTrue && G(out a)) F(a); }
        if (f) { int a; while (fTrue && G(out a)) No(); F(a); }

        // Unassigned.
        if (f) { int a; while (fFalse && G(out a)) F(a); } // Error. Unreachable expression, not unreachable statement
        if (f) { int a; while (fFalse && G(out a)) No(); F(a); } // Error. Unreachable expression, not statement

        // Assigned after false.
        if (f) { int a; while (f || G(out a)) F(a); } // Error
        if (f) { int a; while (f || G(out a)) No(); F(a); }

        // Unassigned.
        if (f) { int a; while (fTrue || G(out a)) F(a); } // Error, unreachable expression
        if (f) { int a; while (fTrue || G(out a)) No(); F(a); } // Error, unreachable expression, not statement

        // Assigned 
        if (f) { int a; while (fFalse || G(out a)) F(a); }
        if (f) { int a; while (fFalse || G(out a)) No(); F(a); }
    }

    // While statement with break and continue.
    public void T121() {
        if (f) { int a; while (f) { break; F(a); } } // Unreachable
        if (f) { int a; while (fTrue) { } F(a); } // Unreachable
        if (f) { int a; while (fTrue) { break; } F(a); } // Error
        if (f) { int a; while (f) { G(out a); break; } F(a); } // Error
        if (f) { int a; while (fTrue) { G(out a); break; } F(a); }
        if (f) { int a; while (f) { break; G(out a); } F(a); } // Error + Unreachable
        if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
        if (f) { int a; while (fTrue || G(out a)) break; F(a); } // Error
        if (f) { int a; while (f) { if (f) G(out a); else break; F(a); } }
        if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
        if (f) { int a; while (fTrue) { if (f) break; G(out a); } F(a); } // Error
        if (f) { int a; while (fTrue) { if (fFalse) break; G(out a); break; } F(a); } // Unreachable (break)

        if (f) { int a; while (f) { continue; F(a); } } // Unreachable
        if (f) { int a; while (fTrue) { continue; } F(a); } // Unreachable
        if (f) { int a; while (fTrue) { if (f) continue; G(out a); break; } F(a); }
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (56,40): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fFalse) F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (57,45): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) No(); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (52,34): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (F(a)) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (53,37): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (54,43): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (55,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f) G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (65,55): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f && G(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Note: Dev10 spuriously reports (72,56,72,57): error CS0165: Use of unassigned local variable 'a'

                // (73,60): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fFalse && G(out a)) No(); F(a); } // Error. Unreachable expression, not statement
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (76,49): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f || G(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (80,53): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue || G(out a)) F(a); } // Error, unreachable expression
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Note: Dev10 spuriously reports (81,61,81,62): error CS0165: Use of unassigned local variable 'a'

                // (90,44): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (f) { break; F(a); } } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (91,43): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) { } F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (95,44): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (f) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "G"),
                // (96,48): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "G"),
                // (99,48): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "G"),
                // (101,53): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) { if (fFalse) break; G(out a); break; } F(a); } // Unreachable (break)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break"),
                // (103,47): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (f) { continue; F(a); } } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (104,53): warning CS0162: Unreachable code detected
                //         if (f) { int a; while (fTrue) { continue; } F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (92,52): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue) { break; } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (93,58): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f) { G(out a); break; } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (95,58): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (f) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (96,62): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (97,60): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue || G(out a)) break; F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (99,62): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue) { break; G(out a); } F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (100,69): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; while (fTrue) { if (f) break; G(out a); } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [WorkItem(529602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529602")]
        [Fact]
        public void DoWhileStatement()
        {
            var source = prefix + @"
    // Do statement.
    public void T130() {
        if (f) { int a; do F(a); while (f); } // Error
        if (f) { int a; do No(); while (F(a)); } // Error
        if (f) { int a; do No(); while (f); F(a); } // Error
        if (f) { int a; do G(out a); while (F(a)); }
        if (f) { int a; do G(out a); while (f); F(a); }
        if (f) { int a; do No(); while (G(out a)); F(a); }
        if (f) { int a; do No(); while (fTrue); F(a); } // Unreachable

        // Assigned after true - nothing special
        if (f) { int a; do No(); while (f && G(out a)); F(a); } // Error

        // Assigned
        if (f) { int a; do No(); while (fTrue && G(out a)); F(a); }

        // Unassigned.
        if (f) { int a; do No(); while (fFalse && G(out a)); F(a); } // Error

        // 
        if (f) { int a; do No(); while (f || G(out a)); F(a); } // Assigned after false
        if (f) { int a; do No(); while (fTrue || G(out a)); F(a); } // unreachable expr, unassigned
        if (f) { int a; do No(); while (fFalse || G(out a)); F(a); } // Assigned
    }

    // Do statement with break and continue.
    public void T131() {
        if (f) { int a; do { break; F(a); } while (f); } // Unreachable
        if (f) { int a; do break; while (F(a)); } // Unreachable
        if (f) { int a; do { if (f) break; G(out a); } while (F(a)); }
        if (f) { int a; do { if (f) break; G(out a); } while (f); F(a); } // Error
        if (f) { int a; do { if (f) break; No(); } while (G(out a)); F(a); } // Error
        if (f) { int a; do { if (f) break; } while (fTrue); F(a); } // Error

        if (f) { int a; do { if (f) continue; G(out a); } while (F(a)); } // Error
        if (f) { int a; do { if (f) continue; G(out a); } while (f); F(a); } // Error
        if (f) { int a; do { if (f) continue; No(); } while (G(out a)); F(a); }
        if (f) { int a; do { if (f) continue; } while (fTrue); F(a); } // Unreachable
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (57,49): warning CS0162: Unreachable code detected
                //         if (f) { int a; do No(); while (fTrue); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (51,30): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do F(a); while (f); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (52,43): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do No(); while (F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (53,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do No(); while (f); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (60,59): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do No(); while (f && G(out a)); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (66,64): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do No(); while (fFalse && G(out a)); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Note: Dev10 spuriously reports (70,65,70,66): error CS0165: Use of unassigned local variable 'a'

                // (76,37): warning CS0162: Unreachable code detected
                //         if (f) { int a; do { break; F(a); } while (f); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),

                // NOTE: By design, we will not match dev10's report of 
                // (77,44,77,48): warning CS0162: Unreachable code detected
                // See DevDiv #13696.

                // (86,64): warning CS0162: Unreachable code detected
                //         if (f) { int a; do { if (f) continue; } while (fTrue); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (79,69): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do { if (f) break; G(out a); } while (f); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (80,72): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do { if (f) break; No(); } while (G(out a)); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (81,63): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do { if (f) break; } while (fTrue); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (83,68): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do { if (f) continue; G(out a); } while (F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (84,72): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; do { if (f) continue; G(out a); } while (f); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [WorkItem(529602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529602")]
        [Fact]
        public void UnreachableDoWhileCondition()
        {
            var source = @"
class C
{
    bool F()
    {
        do { break; } while (F());
        return true;
    }
}
";

            // NOTE: By design, we will not match dev10's report of 
            // warning CS0162: Unreachable code detected
            // See DevDiv #13696.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ForStatement()
        {
            var source = prefix + @"
    // For statement.
    public void T140() {
        if (f) { int a; for (F(a);;) No(); } // Error
        if (f) { int a; for (;F(a);) No(); } // Error
        if (f) { int a; for (;;F(a)) No(); } // Error
        if (f) { int a; for (;;) F(a); } // Error
        if (f) { int a; for (;;) No(); F(a); } // Unreachable
        if (f) { int a; for (;f;) No(); F(a); } // Error
        if (f) { int a; for (;f;) G(out a); F(a); } // Error
        if (f) { int a; for (;fFalse;) G(out a); F(a); } // Error + Unreachable
        if (f) { int a; for (;fFalse;) F(a); } // Unreachable
        if (f) { int a; for (;;) No(); F(a); } // Unreachable

        // Assigned.
        if (f) { int a; for (G(out a);f;) F(a); }
        if (f) { int a; for (G(out a);f;) No(); F(a); }
        if (f) { int a; for (;G(out a);) F(a); }
        if (f) { int a; for (;G(out a);) No(); F(a); }
        if (f) { int a; for (;f;G(out a)) F(a); } // Error
        if (f) { int a; for (;f;G(out a)) No(); F(a); } // Error
        if (f) { int a; for (;f;F(a)) G(out a); }
        if (f) { int a; for (;f;) G(out a); F(a); } // Error

        // Assigned after true.
        if (f) { int a; for (;f && G(out a);) F(a); }
        if (f) { int a; for (;f && G(out a);) No(); F(a); } // Error

        // Assigned.
        if (f) { int a; for (;fTrue && G(out a);) F(a); }
        if (f) { int a; for (;fTrue && G(out a);) No(); F(a); }

        // Unassigned.
        if (f) { int a; for (;fFalse && G(out a);) F(a); } // Error, unreachable expr, no unreachable stmt
        if (f) { int a; for (;fFalse && G(out a);) No(); F(a); } // Error, unreachable expr

        // Assigned after false.
        if (f) { int a; for (;f || G(out a);) F(a); } // Error
        if (f) { int a; for (;f || G(out a);) No(); F(a); }

        // Unassigned.
        if (f) { int a; for (;fTrue || G(out a);) F(a); } // Error, unreachable expr
        if (f) { int a; for (;fTrue || G(out a);) No(); F(a); } // Unreachable expr, not unreachable code

        // Assigned.
        if (f) { int a; for (;fFalse || G(out a);) F(a); }
        if (f) { int a; for (;fFalse || G(out a);) No(); F(a); }
    }

    // For statement with break and continue.
    public void T141() {
        if (f) { int a; for (;;F(a)) G(out a); }
        if (f) { int a; for (;;F(a)) break; } // Unreachable
        if (f) { int a; for (;;F(a)) { G(out a); if (f) continue; } }
        if (f) { int a; for (;;F(a)) { G(out a); if (f) break; } }
        if (f) { int a; for (;;F(a)) { if (f) continue; G(out a); } } // Error
        if (f) { int a; for (;;) break; F(a); } // Error
        if (f) { int a; for (;;) { if (f) break; No(); } F(a); } // Error
        if (f) { int a; for (;;) { G(out a); if (f) break; } F(a); }
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (55,40): warning CS0162: Unreachable code detected
                //         if (f) { int a; for (;;) No(); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (58,40): warning CS0162: Unreachable code detected
                //         if (f) { int a; for (;fFalse;) G(out a); F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "G"),
                // (59,40): warning CS0162: Unreachable code detected
                //         if (f) { int a; for (;fFalse;) F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (60,40): warning CS0162: Unreachable code detected
                //         if (f) { int a; for (;;) No(); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (51,32): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (F(a);;) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (52,33): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;F(a);) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (53,34): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;;F(a)) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (54,36): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;;) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (56,43): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f;) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (57,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f;) G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (58,52): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;fFalse;) G(out a); F(a); } // Error + Unreachable
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (67,45): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f;G(out a)) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (68,51): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f;G(out a)) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (70,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f;) G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (74,55): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f && G(out a);) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Spurious Dev10: (81,56,81,57): error CS0165: Use of unassigned local variable 'a'

                // (82,60): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;fFalse && G(out a);) No(); F(a); } // Error, unreachable expr
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (85,49): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;f || G(out a);) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (89,53): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;fTrue || G(out a);) F(a); } // Error, unreachable expr
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Spurious Dev10: (90,61,90,62): error CS0165: Use of unassigned local variable 'a'

                // (100,32): warning CS0162: Unreachable code detected
                //         if (f) { int a; for (;;F(a)) break; } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (103,34): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;;F(a)) { if (f) continue; G(out a); } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (104,43): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;;) break; F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (105,60): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; for (;;) { if (f) break; No(); } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact]
        public void ThrowStatement()
        {
            var source = prefix + @"
    // Throw statement.
    public void T150() {
        if (f) { int a; throw new Exception(F(a).ToString()); }
        if (f) { int a; throw new Exception(""x""); F(a); } // Unreachable
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (52,51): warning CS0162: Unreachable code detected
                //         if (f) { int a; throw new Exception("x"); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (51,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; throw new Exception(F(a).ToString()); }
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact]
        public void ReturnStatement()
        {
            var source = prefix + @"
    // Return statement.
    public bool T160() { int a; return F(a); } // Error
    public bool T161() { int a; return No(); F(a); } // Unreachable
    public bool T162(out int a) { return F(a); } // Error
    public bool T163(out int a) { return No(); } // Error
    public bool T164(out int a) { try { return No(); } finally { G(out a); } }
    public bool T165(out int a) { return G(out a); }
    public bool T166(out int a) { try { return G(out a); } finally { F(a); } } // Error
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (50,42): error CS0165: Use of unassigned local variable 'a'
                //     public bool T160() { int a; return F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (51,46): warning CS0162: Unreachable code detected
                //     public bool T161() { int a; return No(); F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (52,44): error CS0269: Use of unassigned out parameter 'a'
                //     public bool T162(out int a) { return F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "a").WithArguments("a"),
                // (52,35): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                //     public bool T162(out int a) { return F(a); } // Error
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return F(a);").WithArguments("a"),
                // (53,35): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                //     public bool T163(out int a) { return No(); } // Error
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return No();").WithArguments("a"),
                // (56,72): error CS0269: Use of unassigned out parameter 'a'
                //     public bool T166(out int a) { try { return G(out a); } finally { F(a); } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "a").WithArguments("a"));
        }

        [Fact]
        public void TryCatchStatement()
        {
            var source = prefix + @"
    // Try-catch statement.
    public void T170() {
        if (f) { int a; try { F(a); } catch (Exception e) { } finally { } } // Error
        if (f) { int a; try { } catch (Exception e) { F(a); } finally { } } // Error
        if (f) { int a; try { } catch (Exception e) { } finally { F(a); } } // Error
        if (f) { int a; try { } catch (Exception e) { } finally { } F(a); } // Error

        if (f) { int a; try { G(out a); } catch (Exception e) { } F(a); } // Error
        if (f) { int a; try { G(out a); } finally { } F(a); }
        if (f) { int a; try { G(out a); } catch (Exception e) { } finally { } F(a); } // Error

        if (f) { int a; try { G(out a); } catch (Exception e) { G(out a); } F(a); }
        if (f) { int a; try { G(out a); } catch (Exception e) { G(out a); } finally { } F(a); }

        if (f) { int a; try { } finally { G(out a); } F(a); }
        if (f) { int a; try { } catch (Exception e) { } finally { G(out a); } F(a); }

        if (f) { int a; try { G(out a); goto L; } catch (Exception e) { } return; L: F(a); }
        if (f) { int a; try { G(out a); goto L; } finally { } return; L: F(a); }
        if (f) { int a; try { G(out a); goto L; } catch (Exception e) { } finally { } return; L: F(a); }

        if (f) { int a; try { goto L; } finally { G(out a); } return; L: F(a); }
        if (f) { int a; try { goto L; } catch (Exception e) { } finally { G(out a); } return; L: F(a); }

        // Unreachable end of finally.
        if (f) { int a; try { G(out a); } catch (Exception e) { } finally { for (;;) No(); } F(a); } // Unreachable
        if (f) { int a; try { goto L; } catch (Exception e) { } finally { for(;;) No(); } return; L: F(a); } // Unreachable
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (67,63): warning CS0162: Unreachable code detected
                //         if (f) { int a; try { G(out a); goto L; } finally { } return; L: F(a); }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                // (70,63): warning CS0162: Unreachable code detected
                //         if (f) { int a; try { goto L; } finally { G(out a); } return; L: F(a); }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                // (74,94): warning CS0162: Unreachable code detected
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } finally { for (;;) No(); } F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                // (75,91): warning CS0162: Unreachable code detected
                //         if (f) { int a; try { goto L; } catch (Exception e) { } finally { for(;;) No(); } return; L: F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                // (75,99): warning CS0162: Unreachable code detected
                //         if (f) { int a; try { goto L; } catch (Exception e) { } finally { for(;;) No(); } return; L: F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "L"),
                // (51,33): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { F(a); } catch (Exception e) { } finally { } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (51,56): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { F(a); } catch (Exception e) { } finally { } } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (52,57): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { } catch (Exception e) { F(a); } finally { } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (52,50): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { } catch (Exception e) { F(a); } finally { } } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (53,50): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { } catch (Exception e) { } finally { F(a); } } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (53,69): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { } catch (Exception e) { } finally { F(a); } } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (54,50): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { } catch (Exception e) { } finally { } F(a); } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (54,71): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { } catch (Exception e) { } finally { } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (56,60): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } F(a); } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (56,69): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (58,60): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } finally { } F(a); } // Error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (58,81): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } finally { } F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (60,60): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); } catch (Exception e) { G(out a); } F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (61,60): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); } catch (Exception e) { G(out a); } finally { } F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (64,50): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { } catch (Exception e) { } finally { G(out a); } F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (66,68): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); goto L; } catch (Exception e) { } return; L: F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (68,68): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); goto L; } catch (Exception e) { } finally { } return; L: F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (71,58): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { goto L; } catch (Exception e) { } finally { G(out a); } return; L: F(a); }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (74,60): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { G(out a); } catch (Exception e) { } finally { for (;;) No(); } F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"),
                // (75,58): warning CS0168: The variable 'e' is declared but never used
                //         if (f) { int a; try { goto L; } catch (Exception e) { } finally { for(;;) No(); } return; L: F(a); } // Unreachable
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e"));
        }

        [Fact]
        public void ForEachStatement()
        {
            var source = prefix + @"
    // Foreach statement.
    public void T180() {
        if (f) { int a; foreach (char ch in F(a).ToString()) No(); } // Error
        if (f) { int a; foreach (char ch in ""abc"") F(a); } // Error // BUG?: Error in wrong order.
        if (f) { int a; foreach (char ch in G(out a).ToString()) F(a); }
        if (f) { int a; foreach (char ch in ""abc"") No(); F(a); } // Error
        if (f) { int a; foreach (char ch in ""abc"") G(out a); F(a); } // Error
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (51,47): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; foreach (char ch in F(a).ToString()) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (52,54): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; foreach (char ch in "abc") F(a); } // Error // BUG?: Error in wrong order.
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (54,60): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; foreach (char ch in "abc") No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (55,64): error CS0165: Use of unassigned local variable 'a'
                //         if (f) { int a; foreach (char ch in "abc") G(out a); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact]
        public void UsingAndLockStatements()
        {
            var source = prefix + @"
    // Using and Lock statements.
    public void T190() {
        { int a; using (Res(F(a))) No(); } // Error
        { int a; using (Res(No())) F(a); } // Error
        { int a; using (Res(No())) No(); F(a); } // Error
        { int a; using (Res(G(out a))) F(a); }
        { int a; using (Res(G(out a))) No(); F(a); }
        { int a; using (Res(No())) G(out a); F(a); }

        { int a; lock (Res(F(a))) No(); } // Error
        { int a; lock (Res(No())) F(a); } // Error
        { int a; lock (Res(No())) No(); F(a); } // Error
        { int a; lock (Res(G(out a))) F(a); }
        { int a; lock (Res(G(out a))) No(); F(a); }
        { int a; lock (Res(No())) G(out a); F(a); }
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (51,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; using (Res(F(a))) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (52,38): error CS0165: Use of unassigned local variable 'a'
                //         { int a; using (Res(No())) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (53,44): error CS0165: Use of unassigned local variable 'a'
                //         { int a; using (Res(No())) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (58,30): error CS0165: Use of unassigned local variable 'a'
                //         { int a; lock (Res(F(a))) No(); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (59,37): error CS0165: Use of unassigned local variable 'a'
                //         { int a; lock (Res(No())) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (60,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; lock (Res(No())) No(); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact]
        public void LogicalExpression()
        {
            var source = prefix + @"
    // Logical and: E -> S && T
    public void T340() {
        // S -> DA then DA -> T and E -> DA
        { int a; Q(G(out a) && F(a)); } // DA -> T
        { int a; Q(G(out a) && No()); F(a); } // E -> DA

        // S -> DAT then DA -> T and E -> DAT
        { int a; Q((f && G(out a)) && F(a)); } // DA -> T
        { int a; Q((f && G(out a)) && No()); F(a); } // Error
        { int a; if ((f && G(out a)) && No()) F(a); } // E -> DAT

        // S -> DAF then DU -> T
        { int a; Q((f || G(out a)) && F(a)); } // Error
        // if T -> DU then E -> DU
        { int a; Q((f || G(out a)) && No()); F(a); } // Error
        { int a; if ((f || G(out a)) && No()) F(a); } // Error
        { int a; if ((f || G(out a)) && No()) No(); else F(a); } // Error
        // if T -> DA then E -> DA
        { int a; Q((f || G(out a)) && G(out a)); F(a); }
        // if T -> DAT then E -> DAT
        { int a; Q((f || G(out a)) && (f && G(out a))); F(a); } // Error
        { int a; if ((f || G(out a)) && (f && G(out a))) F(a); }
        // if T -> DAF then E -> DAF
        { int a; Q((f || G(out a)) && (f || G(out a))); F(a); } // Error
        { int a; if ((f || G(out a)) && (f || G(out a))) No(); else F(a); }

        // S -> DU then DU -> T
        { int a; Q(f && F(a)); } // Error
        // if T -> DA then E -> DAT
        { int a; Q(f && G(out a)); F(a); } // Error
        { int a; if (f && G(out a)) F(a); }
        // if T -> DAT then E -> DAT
        { int a; Q(f && (f && G(out a))); F(a); } // Error
        { int a; if (f && (f && G(out a))) F(a); }
        // if T -> DAF then E -> DU
        { int a; Q(f && (f || G(out a))); F(a); } // Error
        { int a; if (f && (f || G(out a))) F(a); } // Error
        { int a; if (f && (f || G(out a))) No(); else F(a); } // Error
        // if T -> DU then E -> DU
        { int a; Q(f && f); F(a); } // Error
        { int a; if (f && f) F(a); } // Error
        { int a; if (f && f) No(); else F(a); } // Error
    }

    // Logical or: E -> S || T
    public void T350() {
        // S -> DA then DA -> T and E -> DA
        { int a; Q(G(out a) || F(a)); } // DA -> T
        { int a; Q(G(out a) || No()); F(a); } // E -> DA

        // S -> DAF then DA -> T and E -> DAF
        { int a; Q((f || G(out a)) || F(a)); } // DA -> T
        { int a; Q((f || G(out a)) || No()); F(a); } // Error
        { int a; if ((f || G(out a)) || No()) No(); else F(a); } // E -> DAF

        // S -> DAT then DU -> T
        { int a; Q((f && G(out a)) || F(a)); } // Error
        // if T -> DU then E -> DU
        { int a; Q((f && G(out a)) || No()); F(a); } // Error
        { int a; if ((f && G(out a)) || No()) F(a); } // Error
        { int a; if ((f && G(out a)) || No()) No(); else F(a); } // Error
        // if T -> DA then E -> DA
        { int a; Q((f && G(out a)) || G(out a)); F(a); }
        // if T -> DAF then E -> DAF
        { int a; Q((f && G(out a)) || (f || G(out a))); F(a); } // Error
        { int a; if ((f && G(out a)) || (f || G(out a))) No(); else F(a); }
        // if T -> DAT then E -> DAT
        { int a; Q((f && G(out a)) || (f && G(out a))); F(a); } // Error
        { int a; if ((f && G(out a)) || (f && G(out a))) F(a); }

        // S -> DU then DU -> T
        { int a; Q(f || F(a)); } // Error
        // if T -> DA then E -> DAF
        { int a; Q(f || G(out a)); F(a); } // Error
        { int a; if (f || G(out a)) No(); else F(a); }
        // if T -> DAF then E -> DAF
        { int a; Q(f || (f || G(out a))); F(a); } // Error
        { int a; if (f || (f || G(out a))) No(); else F(a); }
        // if T -> DAT then E -> DU
        { int a; Q(f || (f && G(out a))); F(a); } // Error
        { int a; if (f || (f && G(out a))) F(a); } // Error
        { int a; if (f || (f && G(out a))) No(); else F(a); } // Error
        // if T -> DU then E -> DU
        { int a; Q(f || f); F(a); } // Error
        { int a; if (f || f) F(a); } // Error
        { int a; if (f || f) No(); else F(a); } // Error
    }

    // Logical not: E -> !S
    public void T360() {
        { bool a; Q(!a); } // Error

        { int a; Q(!G(out a)); F(a); }
        { int a; Q(!No()); F(a); } // Error

        // S -> DAF : E -> DAT
        { int a; Q(!(f || G(out a))); F(a); } // Error
        { int a; if (!(f || G(out a))) F(a); }

        // S -> DAT : E -> DAF
        { int a; Q(!(f && G(out a))); F(a); } // Error
        { int a; if (!(f && G(out a))) No(); else F(a); }
    }

    // Ternary operator: E -> S ? T : U;
    // For definite assignment purposes, this behaves the same as ""if (S) T; else U;""
    public void T370() {
        { bool a; F(a ? 1 : 2); } // Error

        // S -> DU
        { int a; F(f ? a : 2); } // Error
        { int a; F(f ? 1 : a); } // Error
        { int a; F(f ? 1 : 2); F(a); } // Error

        { int a; F(fFalse ? a : 2); } // no DA error; expr is not reachable
        { int a; F(fFalse ? 1 : a); } // 

        { int a; F(fTrue ? a : 2); } // Error - should it also be unreachable?
        { int a; F(fTrue ? 1 : a); } // BUG: Spec says error. Should this be unreachable?

        // S -> DA then DA -> T, DA -> U, E -> DA
        { int a; F(G(out a) ? a : 2); }
        { int a; F(G(out a) ? 1 : a); }
        { int a; F(G(out a) ? 1 : 2); F(a); }

        // S -> DAT then DA -> T, DU -> U

        // Assigned after true.
        { int a; F(f && G(out a) ? a : 2); }
        { int a; F(f && G(out a) ? 1 : a); } // Error
        { int a; F(f && G(out a) ? 1 : 2); F(a); } // Error
        { int a; F(f && G(out a) ? 1 : NG(out a)); F(a); }

        // Assigned.
        { int a; F(fTrue && G(out a) ? a : 2); }
        { int a; F(fTrue && G(out a) ? 1 : a); }
        { int a; F(fTrue && G(out a) ? 1 : 2); F(a); }

        // Unassigned.
        { int a; F(fFalse && G(out a) ? a : 2); } // Error
        { int a; F(fFalse && G(out a) ? 1 : a); } // Error
        { int a; F(fFalse && G(out a) ? 1 : 2); F(a); } // Error
        { int a; F(fFalse && G(out a) ? 1 : NG(out a)); F(a); } // Error

        // Unassigned.
        { int a; F(fFalse && F(a) ? a : 2); } // Error on a
        { int a; F(fFalse && F(a) ? 1 : a); } // Error on a
        { int a; F(fFalse && F(a) ? 1 : 2); F(a); } // Error on second F(a)
        { int a; F(fFalse && F(a) ? 1 : NG(out a)); F(a); } // Error on second F(a)

        // Assigned after false.
        { int a; F(f || G(out a) ? a : 2); } // Error
        { int a; F(f || G(out a) ? 1 : a); }
        { int a; F(f || G(out a) ? 1 : 2); F(a); } // Error
        { int a; F(f || G(out a) ? NG(out a) : 2); F(a); }

        // Unassigned, unreachable expression.
        { int a; F(fTrue || G(out a) ? a : 2); } // Error
        { int a; F(fTrue || G(out a) ? 1 : a); } // Error
        { int a; F(fTrue || G(out a) ? 1 : 2); F(a); } // Error
        { int a; F(fTrue || G(out a) ? NG(out a) : 2); F(a); } // Error

        // Assigned.
        { int a; F(fFalse || G(out a) ? a : 2); }
        { int a; F(fFalse || G(out a) ? 1 : a); }
        { int a; F(fFalse || G(out a) ? 1 : 2); F(a); }
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (57,48): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f && G(out a)) && No()); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (61,41): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f || G(out a)) && F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (63,48): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f || G(out a)) && No()); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (64,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if ((f || G(out a)) && No()) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (65,60): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if ((f || G(out a)) && No()) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (69,59): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f || G(out a)) && (f && G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (72,59): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f || G(out a)) && (f || G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (76,27): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f && F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (78,38): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f && G(out a)); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (81,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f && (f && G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (84,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f && (f || G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (85,46): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && (f || G(out a))) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (86,57): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && (f || G(out a))) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (88,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f && f); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (89,32): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && f) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (90,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f && f) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (101,48): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f || G(out a)) || No()); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (105,41): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f && G(out a)) || F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (107,48): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f && G(out a)) || No()); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (108,49): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if ((f && G(out a)) || No()) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (109,60): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if ((f && G(out a)) || No()) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (113,59): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f && G(out a)) || (f || G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (116,59): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q((f && G(out a)) || (f && G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (120,27): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f || F(a)); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (122,38): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f || G(out a)); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (125,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f || (f || G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (128,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f || (f && G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (129,46): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || (f && G(out a))) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (130,57): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || (f && G(out a))) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (132,31): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(f || f); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (133,32): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || f) F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (134,43): error CS0165: Use of unassigned local variable 'a'
                //         { int a; if (f || f) No(); else F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (139,22): error CS0165: Use of unassigned local variable 'a'
                //         { bool a; Q(!a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (142,30): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(!No()); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (145,41): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(!(f || G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (149,41): error CS0165: Use of unassigned local variable 'a'
                //         { int a; Q(!(f && G(out a))); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (156,21): error CS0165: Use of unassigned local variable 'a'
                //         { bool a; F(a ? 1 : 2); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (159,24): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f ? a : 2); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (160,28): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f ? 1 : a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (161,34): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f ? 1 : 2); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (164,33): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fFalse ? 1 : a); } // 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (166,28): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fTrue ? a : 2); } // Error - should it also be unreachable?
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (178,40): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f && G(out a) ? 1 : a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (179,46): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f && G(out a) ? 1 : 2); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Dev10 spurious: (188,43,188,44): error CS0165: Use of unassigned local variable 'a'

                // (189,45): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fFalse && G(out a) ? 1 : a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (190,51): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fFalse && G(out a) ? 1 : 2); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Dev10 spurious: (191,61,191,62): error CS0165: Use of unassigned local variable 'a'
                // Dev10 spurious: (194,39,194,40): error CS0165: Use of unassigned local variable 'a'

                // (195,41): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fFalse && F(a) ? 1 : a); } // Error on a
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (196,47): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fFalse && F(a) ? 1 : 2); F(a); } // Error on second F(a)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Dev10 spurious: error CS0165: Use of unassigned local variable 'a'

                // (200,36): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f || G(out a) ? a : 2); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (202,46): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(f || G(out a) ? 1 : 2); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (206,40): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fTrue || G(out a) ? a : 2); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),

                // Dev10 spurious: (207,46,207,47): error CS0165: Use of unassigned local variable 'a'

                // (208,50): error CS0165: Use of unassigned local variable 'a'
                //         { int a; F(fTrue || G(out a) ? 1 : 2); F(a); } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a")

                // Dev10 spurious: (209,60,209,61): error CS0165: Use of unassigned local variable 'a'
                );
        }

        [Fact]
        public void WhidbeyBug467493()
        {
            var source = prefix + @"
    // Whidbey bug #467493
    public static void M4() {
        int x;
        throw new Exception();
        ((DI)(delegate { if (x == 1) return 1; Console.WriteLine(""Bug""); }))();
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (78,15): error CS1643: Not all code paths return a value in anonymous method of type 'DI'
                //         ((DI)(delegate { if (x == 1) return 1; Console.WriteLine("Bug"); }))();
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "DI").WithLocation(78, 15),
                // (78,9): warning CS0162: Unreachable code detected
                //         ((DI)(delegate { if (x == 1) return 1; Console.WriteLine("Bug"); }))();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "(").WithLocation(78, 9)
                );
        }

        [WorkItem(648107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648107")]
        [Fact]
        public void WhidbeyBug479106()
        {
            var source = prefix + @"
    // Whidbey bug #479106
    public unsafe struct SF {
        public int x;
        public fixed int arr[5];
    }
    public unsafe static void M5() {
        SF s;
        s.arr[0]++; // OK

        SF[] rgs = new SF[2];
        fixed (SF * prgs = rgs) {
            int a;
            int b;
            prgs[a].arr[0] = 5; // Error: a
            prgs[b = 1].arr[0] = 5; // No error
            Console.WriteLine(b);
        }
    }
" + suffix;

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (62,18): error CS0165: Use of unassigned local variable 'a'
                //             prgs[a].arr[0] = 5; // Error: a
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a")
                );
        }

        [Fact, WorkItem(31370, "https://github.com/dotnet/roslyn/issues/31370")]
        public void WhidbeyBug467493_WithSuppression()
        {
            var source = prefix + @"
    // Whidbey bug #467493
    public static void M4() {
        int x;
        throw new Exception();
        ((DI)(delegate { if (x == 1) return 1; Console.WriteLine(""Bug""); } !))();
    }
" + suffix;

            // Covers GenerateExplicitConversionErrors
            CreateCompilation(source).VerifyDiagnostics(
                // (78,15): error CS1643: Not all code paths return a value in anonymous method of type 'DI'
                //         ((DI)(delegate { if (x == 1) return 1; Console.WriteLine("Bug"); }))();
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "DI").WithLocation(78, 15),
                // (78,9): warning CS0162: Unreachable code detected
                //         ((DI)(delegate { if (x == 1) return 1; Console.WriteLine("Bug"); }))();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "(").WithLocation(78, 9)
                );
        }

        [Fact]
        public void AccessingFixedFieldUsesTheReceiver()
        {
            var source = prefix + @"
    // Whidbey bug #479106
    public unsafe struct SF {
        public int x;
        public fixed int arr[5];
    }
    public unsafe struct SF2 {
        public SF z;
    }
    public unsafe static void M5() {
        SF2 s;
        s.z.arr[0]++; // OK
    }
" + suffix;

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(529603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529603")]
        [Fact]
        public void TernaryOperator()
        {
            var source = prefix + @"
    public static void M8()
    {
        int b = 1;
        int c = 1;
        int d = 1;
        bool x = true;
        bool y = false;
        bool z = false;

// ?: operator

// Check for state before exprtrue:

/* NDA --> NDA */       { int a; if (x               ? F(a) : F(b)) b = c; else d = c; } // Error
/* DAT --> DA  */       { int a; if ((x && G(out a)) ? F(a) : F(b)) b = c; else d = c; } // OK
/* DAF --> NDA */       { int a; if ((x || G(out a)) ? F(a) : F(b)) b = c; else d = c; } // Error
/* DA  --> DA  */       { int a; if (G(out a)        ? F(a) : F(b)) b = c; else d = c; } // OK

// Check for state before exprfalse:

/* NDA --> NDA */       { int a; if (x               ? F(b) : F(a)) b = c; else d = c; } // Error
/* DAT --> NDA */       { int a; if ((x && G(out a)) ? F(b) : F(a)) b = c; else d = c; } // Error
/* DAF --> DA  */       { int a; if ((x || G(out a)) ? F(b) : F(a)) b = c; else d = c; } // OK
/* DA  --> DA  */       { int a; if (G(out a)        ? F(b) : F(a)) b = c; else d = c; } // OK

// Check for state after expr:

/* NDA?NDA:NDA-->NDA */ { int a; if (x               ? y               : z)               b = a; else d = c; } // Error
/* NDA?NDA:NDA-->NDA */ { int a; if (x               ? y               : z)               b = c; else d = a; } // Error
/* NDA?NDA:DAT-->NDA */ { int a; if (x               ? y               : (z && G(out a))) b = a; else d = c; } // Error
/* NDA?NDA:DAT-->NDA */ { int a; if (x               ? y               : (z && G(out a))) b = c; else d = a; } // Error
/* NDA?NDA:DAF-->NDA */ { int a; if (x               ? y               : (z || G(out a))) b = a; else d = c; } // Error
/* NDA?NDA:DAF-->NDA */ { int a; if (x               ? y               : (z || G(out a))) b = c; else d = a; } // Error
/* NDA?NDA:DA -->NDA */ { int a; if (x               ? y               : G(out a))        b = a; else d = c; } // Error
/* NDA?NDA:DA -->NDA */ { int a; if (x               ? y               : G(out a))        b = c; else d = a; } // Error
/* NDA?DAT:NDA-->NDA */ { int a; if (x               ? (y && G(out a)) : z)               b = a; else d = c; } // Error
/* NDA?DAT:NDA-->NDA */ { int a; if (x               ? (y && G(out a)) : z)               b = c; else d = a; } // Error
/* NDA?DAT:DAT-->DAT */ { int a; if (x               ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
/* NDA?DAT:DAT-->DAT */ { int a; if (x               ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
/* NDA?DAT:DAF-->NDA */ { int a; if (x               ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
/* NDA?DAT:DAF-->NDA */ { int a; if (x               ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // Error
/* NDA?DAT:DA -->DAT */ { int a; if (x               ? (y && G(out a)) : G(out a))        b = a; else d = c; } // OK
/* NDA?DAT:DA -->DAT */ { int a; if (x               ? (y && G(out a)) : G(out a))        b = c; else d = a; } // Error
/* NDA?DAF:NDA-->NDA */ { int a; if (x               ? (y || G(out a)) : z)               b = a; else d = c; } // Error
/* NDA?DAF:NDA-->NDA */ { int a; if (x               ? (y || G(out a)) : z)               b = c; else d = a; } // Error
/* NDA?DAF:DAT-->NDA */ { int a; if (x               ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // Error
/* NDA?DAF:DAT-->NDA */ { int a; if (x               ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
/* NDA?DAF:DAF-->DAF */ { int a; if (x               ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
/* NDA?DAF:DAF-->DAF */ { int a; if (x               ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
/* NDA?DAF:DA -->DAF */ { int a; if (x               ? (y || G(out a)) : G(out a))        b = a; else d = c; } // Error
/* NDA?DAF:DA -->DAF */ { int a; if (x               ? (y || G(out a)) : G(out a))        b = c; else d = a; } // OK
/* NDA?DA :NDA-->NDA */ { int a; if (x               ? G(out a)        : z)               b = a; else d = c; } // Error
/* NDA?DA :NDA-->NDA */ { int a; if (x               ? G(out a)        : z)               b = c; else d = a; } // Error
/* NDA?DA :DAT-->DAT */ { int a; if (x               ? G(out a)        : (z && G(out a))) b = a; else d = c; } // OK
/* NDA?DA :DAT-->DAT */ { int a; if (x               ? G(out a)        : (z && G(out a))) b = c; else d = a; } // Error
/* NDA?DA :DAF-->DAF */ { int a; if (x               ? G(out a)        : (z || G(out a))) b = a; else d = c; } // Error
/* NDA?DA :DAF-->DAF */ { int a; if (x               ? G(out a)        : (z || G(out a))) b = c; else d = a; } // OK
/* NDA?DA :DA -->DA  */ { int a; if (x               ? G(out a)        : G(out a))        b = a; else d = a; } // OK
/* DAT?NDA:NDA-->NDA */ { int a; if ((x && G(out a)) ? y               : z)               b = a; else d = c; } // Error
/* DAT?NDA:NDA-->NDA */ { int a; if ((x && G(out a)) ? y               : z)               b = c; else d = a; } // Error
/* DAT?NDA:DAT-->DAT */ { int a; if ((x && G(out a)) ? y               : (z && G(out a))) b = a; else d = c; } // OK
/* DAT?NDA:DAT-->DAT */ { int a; if ((x && G(out a)) ? y               : (z && G(out a))) b = c; else d = a; } // Error
/* DAT?NDA:DAF-->DAF */ { int a; if ((x && G(out a)) ? y               : (z || G(out a))) b = a; else d = c; } // Error
/* DAT?NDA:DAF-->DAF */ { int a; if ((x && G(out a)) ? y               : (z || G(out a))) b = c; else d = a; } // OK
/* DAT?NDA:DA -->DA  */ { int a; if ((x && G(out a)) ? y               : G(out a))        b = a; else d = a; } // OK
/* DAT?DAT:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y && G(out a)) : z)               b = a; else d = c; } // Error
/* DAT?DAT:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y && G(out a)) : z)               b = c; else d = a; } // Error
/* DAT?DAT:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
/* DAT?DAT:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
/* DAT?DAT:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
/* DAT?DAT:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
/* DAT?DAT:DA -->DA  */ { int a; if ((x && G(out a)) ? (y && G(out a)) : G(out a))        b = a; else d = a; } // OK
/* DAT?DAF:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y || G(out a)) : z)               b = a; else d = c; } // Error
/* DAT?DAF:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y || G(out a)) : z)               b = c; else d = a; } // Error
/* DAT?DAF:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
/* DAT?DAF:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
/* DAT?DAF:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
/* DAT?DAF:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
/* DAT?DAF:DA -->DA  */ { int a; if ((x && G(out a)) ? (y || G(out a)) : G(out a))        b = a; else d = a; } // OK
/* DAT?DA :NDA-->NDA */ { int a; if ((x && G(out a)) ? G(out a)        : z)               b = a; else d = c; } // Error
/* DAT?DA :NDA-->NDA */ { int a; if ((x && G(out a)) ? G(out a)        : z)               b = c; else d = a; } // Error
/* DAT?DA :DAT-->DAT */ { int a; if ((x && G(out a)) ? G(out a)        : (z && G(out a))) b = a; else d = c; } // OK
/* DAT?DA :DAT-->DAT */ { int a; if ((x && G(out a)) ? G(out a)        : (z && G(out a))) b = c; else d = a; } // Error
/* DAT?DA :DAF-->DAF */ { int a; if ((x && G(out a)) ? G(out a)        : (z || G(out a))) b = a; else d = c; } // Error
/* DAT?DA :DAF-->DAF */ { int a; if ((x && G(out a)) ? G(out a)        : (z || G(out a))) b = c; else d = a; } // OK
/* DAT?DA :DA -->DA  */ { int a; if ((x && G(out a)) ? G(out a)        : G(out a))        b = a; else d = a; } // OK
/* DAF?NDA:NDA-->NDA */ { int a; if ((x || G(out a)) ? y               : z)               b = a; else d = c; } // Error
/* DAF?NDA:NDA-->NDA */ { int a; if ((x || G(out a)) ? y               : z)               b = c; else d = a; } // Error
/* DAF?NDA:DAT-->NDA */ { int a; if ((x || G(out a)) ? y               : (z && G(out a))) b = a; else d = c; } // Error
/* DAF?NDA:DAT-->NDA */ { int a; if ((x || G(out a)) ? y               : (z && G(out a))) b = c; else d = a; } // Error
/* DAF?NDA:DAF-->NDA */ { int a; if ((x || G(out a)) ? y               : (z || G(out a))) b = a; else d = c; } // Error
/* DAF?NDA:DAF-->NDA */ { int a; if ((x || G(out a)) ? y               : (z || G(out a))) b = c; else d = a; } // Error
/* DAF?NDA:DA -->NDA */ { int a; if ((x || G(out a)) ? y               : G(out a))        b = c; else d = a; } // Error
/* DAF?NDA:DA -->NDA */ { int a; if ((x || G(out a)) ? y               : G(out a))        b = c; else d = a; } // Error
/* DAF?DAT:NDA-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : z)               b = a; else d = c; } // OK
/* DAF?DAT:NDA-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : z)               b = c; else d = a; } // Error
/* DAF?DAT:DAT-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
/* DAF?DAT:DAT-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
/* DAF?DAT:DAF-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // OK
/* DAF?DAT:DAF-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // Error
/* DAF?DAT:DA -->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : G(out a))        b = a; else d = c; } // OK
/* DAF?DAT:DA -->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : G(out a))        b = c; else d = a; } // Error
/* DAF?DAF:NDA-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : z)               b = a; else d = c; } // Error
/* DAF?DAF:NDA-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : z)               b = c; else d = a; } // OK
/* DAF?DAF:DAT-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // Error
/* DAF?DAF:DAT-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // OK
/* DAF?DAF:DAF-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
/* DAF?DAF:DAF-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
/* DAF?DAF:DA -->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : G(out a))        b = a; else d = c; } // Error
/* DAF?DAF:DA -->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : G(out a))        b = c; else d = a; } // OK
/* DAF?DA :NDA-->DA  */ { int a; if ((x || G(out a)) ? G(out a)        : z)               b = a; else d = a; } // OK
/* DAF?DA :DAT-->DA  */ { int a; if ((x || G(out a)) ? G(out a)        : (z && G(out a))) b = a; else d = a; } // OK
/* DAF?DA :DAF-->DA  */ { int a; if ((x || G(out a)) ? G(out a)        : (z || G(out a))) b = a; else d = a; } // OK
/* DAF?DA :DA -->DA  */ { int a; if ((x || G(out a)) ? G(out a)        : G(out a))        b = a; else d = a; } // OK
/* DA ?NDA:NDA-->DA  */ { int a; if (G(out a)        ? y               : z)               b = a; else d = a; } // OK
/* DA ?NDA:DAT-->DA  */ { int a; if (G(out a)        ? y               : (z && G(out a))) b = a; else d = a; } // OK
/* DA ?NDA:DAF-->DA  */ { int a; if (G(out a)        ? y               : (z || G(out a))) b = a; else d = a; } // OK
/* DA ?NDA:DA -->DA  */ { int a; if (G(out a)        ? y               : G(out a))        b = a; else d = a; } // OK
/* DA ?DAT:NDA-->DA  */ { int a; if (G(out a)        ? (y && G(out a)) : z)               b = a; else d = a; } // OK
/* DA ?DAT:DAT-->DA  */ { int a; if (G(out a)        ? (y && G(out a)) : (z && G(out a))) b = a; else d = a; } // OK
/* DA ?DAT:DAF-->DA  */ { int a; if (G(out a)        ? (y && G(out a)) : (z || G(out a))) b = a; else d = a; } // OK
/* DA ?DAT:DA -->DA  */ { int a; if (G(out a)        ? (y && G(out a)) : G(out a))        b = a; else d = a; } // OK
/* DA ?DAF:NDA-->DA  */ { int a; if (G(out a)        ? (y || G(out a)) : z)               b = a; else d = a; } // OK
/* DA ?DAF:DAT-->DA  */ { int a; if (G(out a)        ? (y || G(out a)) : (z && G(out a))) b = a; else d = a; } // OK
/* DA ?DAF:DAF-->DA  */ { int a; if (G(out a)        ? (y || G(out a)) : (z || G(out a))) b = a; else d = a; } // OK
/* DA ?DAF:DA -->DA  */ { int a; if (G(out a)        ? (y || G(out a)) : G(out a))        b = a; else d = a; } // OK
/* DA ?DA :NDA-->DA  */ { int a; if (G(out a)        ? G(out a)        : z)               b = a; else d = a; } // OK
/* DA ?DA :DAT-->DA  */ { int a; if (G(out a)        ? G(out a)        : (z && G(out a))) b = a; else d = a; } // OK
/* DA ?DA :DAF-->DA  */ { int a; if (G(out a)        ? G(out a)        : (z || G(out a))) b = a; else d = a; } // OK
    }
" + suffix;

            CreateCompilation(source).VerifyDiagnostics(
                // (61,58): error CS0165: Use of unassigned local variable 'a'
                // /* NDA --> NDA */       { int a; if (x               ? F(a) : F(b)) b = c; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (63,58): error CS0165: Use of unassigned local variable 'a'
                // /* DAF --> NDA */       { int a; if ((x || G(out a)) ? F(a) : F(b)) b = c; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (68,65): error CS0165: Use of unassigned local variable 'a'
                // /* NDA --> NDA */       { int a; if (x               ? F(b) : F(a)) b = c; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (69,65): error CS0165: Use of unassigned local variable 'a'
                // /* DAT --> NDA */       { int a; if ((x && G(out a)) ? F(b) : F(a)) b = c; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (75,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:NDA-->NDA */ { int a; if (x               ? y               : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (76,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:NDA-->NDA */ { int a; if (x               ? y               : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (77,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DAT-->NDA */ { int a; if (x               ? y               : (z && G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (78,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DAT-->NDA */ { int a; if (x               ? y               : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (79,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DAF-->NDA */ { int a; if (x               ? y               : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (80,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DAF-->NDA */ { int a; if (x               ? y               : (z || G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (81,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DA -->NDA */ { int a; if (x               ? y               : G(out a))        b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (82,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?NDA:DA -->NDA */ { int a; if (x               ? y               : G(out a))        b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (83,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:NDA-->NDA */ { int a; if (x               ? (y && G(out a)) : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (84,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:NDA-->NDA */ { int a; if (x               ? (y && G(out a)) : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (85,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DAT-->DAT */ { int a; if (x               ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (86,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DAT-->DAT */ { int a; if (x               ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (87,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DAF-->NDA */ { int a; if (x               ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (88,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DAF-->NDA */ { int a; if (x               ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (89,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DA -->DAT */ { int a; if (x               ? (y && G(out a)) : G(out a))        b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (90,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAT:DA -->DAT */ { int a; if (x               ? (y && G(out a)) : G(out a))        b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (91,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:NDA-->NDA */ { int a; if (x               ? (y || G(out a)) : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (92,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:NDA-->NDA */ { int a; if (x               ? (y || G(out a)) : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (93,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DAT-->NDA */ { int a; if (x               ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (94,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DAT-->NDA */ { int a; if (x               ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (95,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DAF-->DAF */ { int a; if (x               ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (96,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DAF-->DAF */ { int a; if (x               ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (97,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DA -->DAF */ { int a; if (x               ? (y || G(out a)) : G(out a))        b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (98,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DAF:DA -->DAF */ { int a; if (x               ? (y || G(out a)) : G(out a))        b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (99,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :NDA-->NDA */ { int a; if (x               ? G(out a)        : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (100,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :NDA-->NDA */ { int a; if (x               ? G(out a)        : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (101,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :DAT-->DAT */ { int a; if (x               ? G(out a)        : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (102,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :DAT-->DAT */ { int a; if (x               ? G(out a)        : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (103,95): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :DAF-->DAF */ { int a; if (x               ? G(out a)        : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (104,107): error CS0165: Use of unassigned local variable 'a'
                // /* NDA?DA :DAF-->DAF */ { int a; if (x               ? G(out a)        : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (106,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:NDA-->NDA */ { int a; if ((x && G(out a)) ? y               : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (107,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:NDA-->NDA */ { int a; if ((x && G(out a)) ? y               : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (108,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:DAT-->DAT */ { int a; if ((x && G(out a)) ? y               : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (109,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:DAT-->DAT */ { int a; if ((x && G(out a)) ? y               : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (110,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:DAF-->DAF */ { int a; if ((x && G(out a)) ? y               : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (111,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?NDA:DAF-->DAF */ { int a; if ((x && G(out a)) ? y               : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (113,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y && G(out a)) : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (114,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y && G(out a)) : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (115,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (116,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (117,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (118,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAT:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (120,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y || G(out a)) : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (121,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:NDA-->NDA */ { int a; if ((x && G(out a)) ? (y || G(out a)) : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (122,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (123,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:DAT-->DAT */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (124,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (125,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DAF:DAF-->DAF */ { int a; if ((x && G(out a)) ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (127,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :NDA-->NDA */ { int a; if ((x && G(out a)) ? G(out a)        : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (128,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :NDA-->NDA */ { int a; if ((x && G(out a)) ? G(out a)        : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (129,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :DAT-->DAT */ { int a; if ((x && G(out a)) ? G(out a)        : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (130,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :DAT-->DAT */ { int a; if ((x && G(out a)) ? G(out a)        : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (131,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :DAF-->DAF */ { int a; if ((x && G(out a)) ? G(out a)        : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (132,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAT?DA :DAF-->DAF */ { int a; if ((x && G(out a)) ? G(out a)        : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (134,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:NDA-->NDA */ { int a; if ((x || G(out a)) ? y               : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (135,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:NDA-->NDA */ { int a; if ((x || G(out a)) ? y               : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (136,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DAT-->NDA */ { int a; if ((x || G(out a)) ? y               : (z && G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (137,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DAT-->NDA */ { int a; if ((x || G(out a)) ? y               : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (138,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DAF-->NDA */ { int a; if ((x || G(out a)) ? y               : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (139,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DAF-->NDA */ { int a; if ((x || G(out a)) ? y               : (z || G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (140,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DA -->NDA */ { int a; if ((x || G(out a)) ? y               : G(out a))        b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (141,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?NDA:DA -->NDA */ { int a; if ((x || G(out a)) ? y               : G(out a))        b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (142,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:NDA-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : z)               b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (143,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:NDA-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : z)               b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (144,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DAT-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z && G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (145,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DAT-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z && G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (146,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DAF-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z || G(out a))) b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (147,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DAF-->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : (z || G(out a))) b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (148,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DA -->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : G(out a))        b = a; else d = c; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (149,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAT:DA -->DAT */ { int a; if ((x || G(out a)) ? (y && G(out a)) : G(out a))        b = c; else d = a; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (150,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:NDA-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : z)               b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (151,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:NDA-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : z)               b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (152,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DAT-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z && G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (153,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DAT-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z && G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (154,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DAF-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z || G(out a))) b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (155,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DAF-->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : (z || G(out a))) b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (156,95): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DA -->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : G(out a))        b = a; else d = c; } // Error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"),
                // (157,107): error CS0165: Use of unassigned local variable 'a'
                // /* DAF?DAF:DA -->DAF */ { int a; if ((x || G(out a)) ? (y || G(out a)) : G(out a))        b = c; else d = a; } // OK
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a"));
        }

        [Fact, WorkItem(529603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529603")]
        public void IfConditionalAnd()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        bool a = true, b = true, c = true; // values don't matter

        int x;
        if (a ? (b && Set(out x)) : (c && Set(out x)))
        {
            int y = x; // x is definitely assigned if we reach this point
        }
    }

    static bool Set(out int x)
    {
        x = 1;
        return true;
    }
}
";
            // Bug#529603: Won't Fix (Native no error)
            CreateCompilation(source).VerifyDiagnostics(
                // (11,21): error CS0165: Use of unassigned local variable 'x'
                //             int y = x; // x is definitely assigned if we reach this point
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x"));
        }

        [WorkItem(545352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545352")]
        [Fact]
        public void UseDefViolationInDelegateInSwitchWithGoto()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        switch (5)
        {
            case 1:
                System.Action a = delegate { int b; int c = b; }; // Error on b.
            Lab:
                break;
            case 5:
                goto Lab;
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,31): warning CS0162: Unreachable code detected
                //                 System.Action a = delegate { int b; int c = b; }; // Error on b.
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System"),
                // (9,61): error CS0165: Use of unassigned local variable 'b'
                //                 System.Action a = delegate { int b; int c = b; }; // Error on b.
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b").WithArguments("b")
                );
        }

        [Fact]
        public void UseDefViolationInUnreachableDelegate()
        {
            var source = @"
class C
{
    static void Main()
    {
        if (false)
        {
            System.Action a = () => { int x; int y = x; };
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,27): warning CS0162: Unreachable code detected
                //             System.Action a = () => { int x; int y = x; };
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System"),
                // (8,54): error CS0165: Use of unassigned local variable 'x'
                //             System.Action a = () => { int x; int y = x; };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x"));
        }

        [Fact]
        public void UseDef_ExceptionFilters1()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (System.Exception e) when (e.Message == null)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UseDef_ExceptionFilters2()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (System.Exception e) when (F())
        {
        }
    }

    static bool F() { return true; }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,33): warning CS0168: The variable 'e' is declared but never used
                //         catch (System.Exception e) when (true)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 33));
        }

        [Fact]
        public void UseDef_ExceptionFilters3()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Exception f;

        try
        {
        }
        catch (Exception e) when ((f = e) != null)
        {
            Console.WriteLine(f);
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UseDef_ExceptionFilters4()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Exception f;

        try
        {
        }
        catch (Exception e) when (f == e)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,33): error CS0165: Use of unassigned local variable 'f'
                //         catch (Exception e) when (f == e)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f"));
        }

        [Fact]
        public void UseDef_ExceptionFilters5()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Exception f;

        try
        {
        }
        catch (Exception e) when ((f = e) != null)
        {
        }
        catch (Exception e) when (f == e)
        {
        }
    }
}
";
            // TODO (tomat): f is always gonna be assigned in subsequent filter expressions.
            CreateCompilation(source).VerifyDiagnostics(
                // (15,33): error CS0165: Use of unassigned local variable 'f'
                //         catch (Exception e) when (f == e)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f"));
        }

        [Fact]
        public void UseDef_ExceptionFilters6()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Exception f, g;

        try
        {
        }
        catch (Exception e) when ((f = e) != null)
        {
            Console.WriteLine(f);
            Console.WriteLine(g);
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,31): error CS0165: Use of unassigned local variable 'g'
                //             Console.WriteLine(g);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "g").WithArguments("g"));
        }

        [Fact]
        public void UseDef_CondAccess()
        {
            var source = @"
class C
{
    C M1(out C arg)
    {
        arg = this;
        return arg;
    }

    static void Main()
    {
        C o;

        var d = new C();
        var v = d ?. M1(out o);

        System.Console.WriteLine(o);
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    // (17,34): error CS0165: Use of unassigned local variable 'o'
    //         System.Console.WriteLine(o);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "o").WithArguments("o").WithLocation(17, 34)
    );
        }

        [Fact]
        public void UseDef_CondAccess01()
        {
            var source = @"
class C
{
    C M1(out C arg)
    {
        arg = this;
        return arg;
    }

    static void Main()
    {
        C o;

        var d = new C();
        var v = d ?. M1(out o) ?? (o = null);

        System.Console.WriteLine(o);
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    // (17,34): error CS0165: Use of unassigned local variable 'o'
    //         System.Console.WriteLine(o);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "o").WithArguments("o").WithLocation(17, 34)
    );
        }

        [Fact]
        public void UseDef_CondAccess02()
        {
            var source = @"
class C
{
    C M1(out C arg)
    {
        arg = this;
        return arg;
    }

    C M2( C arg)
    {
        return arg;
    }

    static void Main()
    {
        C o;

        var d = new C();
        var v = d ?. M1(out o) ?. M2(o);
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UseDef_CondAccess03()
        {
            var source = @"
class C
{
    C M1(out C arg)
    {
        arg = this;
        return arg;
    }

    C M2( C arg)
    {
        return arg;
    }

    static void Main()
    {
        C o;

        var d = new C();
        var v = d.M1(out o) ?. M1(out o) ?. M2(o);
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    );
        }

        [Fact, WorkItem(14651, "https://github.com/dotnet/roslyn/issues/14651")]
        public void IrrefutablePattern_1()
        {
            var source =
@"using System;
class C
{
    void TestFunc(int i)
    {
        int x;
        if (i is int j)
        {
            Console.WriteLine(""matched"");
        }
        else
        {
            x = x + 1; // reachable, and x is definitely assigned here
        }

        Console.WriteLine(j);
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        // DataFlowPass.VisitConversion with IsConditionalState.
        [Fact]
        public void OutVarConversion()
        {
            var source =
@"class C
{
    static object F(bool b)
    {
        return ((bool)(b && G(out var o))) ? o : null;
    }
    static bool G(out object o)
    {
        o = null;
        return true;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        // DataFlowPass.VisitConversion with IsConditionalState.
        [Fact]
        public void IsPatternConversion()
        {
            var source =
@"class C
{
    static object F(object o)
    {
        return ((bool)(o is C c)) ? c: null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        // DataFlowPass.VisitConversion with IsConditionalState.
        [Fact]
        public void IsPatternBadValueConversion()
        {
            // C#7.0 does not support this particular pattern so the pattern
            // expression is bound as a BadExpression with a conversion.
            var source =
@"class C
{
    static T F<T>(System.ValueType o)
    {
        return o is T t ? t : default(T);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (5,21): error CS8314: An expression of type 'ValueType' cannot be handled by a pattern of type 'T' in C# 7. Please use language version 7.1 or greater.
                //         return o is T t ? t : default(T);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("System.ValueType", "T", "7.0", "7.1").WithLocation(5, 21));
        }

        [Fact, WorkItem(19831, "https://github.com/dotnet/roslyn/issues/19831")]
        public void AssignedInFinallyUsedInTry()
        {
            var source =
@"  
    public class Program
    {
        static void Main(string[] args)
        {
            Test();
        }

        public static void Test()
        {
            object obj; 

            try
            {
                goto l3;

                l1:
                goto l2;

                l3:
                goto l1;


                l2:

                // Should be compile error
                // 'obj' is uninitialized
                obj.ToString();
            }
            finally
            {
                obj = 1;
            }
        }
    }
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (28,17): error CS0165: Use of unassigned local variable 'obj'
                //                 obj.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "obj").WithArguments("obj").WithLocation(28, 17)
                );
        }
    }
}
