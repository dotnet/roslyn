// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.Patterns)]
public class PatternMatchingTests_Variables : PatternMatchingTestBase
{
    [Fact]
    public void DefiniteAssignment_IsPatternExpression()
    {
        var program =
@"#line 21
object o = null;

{
    if (o is var c or var c) // always-true 1
        c.ToString();
    else
        c.ToString();
}
{
    if (o is var c or 1) // always-true 2
        c.ToString(); // 01
    else
        c.ToString();
}
{
    if (o is 1 or var c) // always-true 3
        c.ToString(); // 02
    else
        c.ToString();
}
{
    if (o is not C(1) c and not C(2) c)
        c.ToString(); // 03
    else
        c.ToString();
}
{
    if (!(o is not C(1) c and not C(2) c))
        c.ToString();
    else
        c.ToString(); // 04
}
{
    if (o is not C(1) c or not C(2) c) // duplicate 1
        c.ToString(); // 05
    else
        c.ToString();
}
{
    if (o is not C(1) c and C(2) c) // duplicate 2
        c.ToString(); // 06
    else
        c.ToString();
}
{
    if (o is C(1) c and not C(2) c) // duplicate 3
        c.ToString();
    else
        c.ToString(); // 07
}
{
    if (o is C(1) c or C(2) c)
        c.ToString();
    else
        c.ToString(); // 08
}
{
    if (o is C(1) c or 1)
        c.ToString(); // 09
    else
        c.ToString();
}
{
    if (!(o is C(1) c or 1))
        c.ToString(); // 10
    else
        c.ToString();
}
{
    if (o is 1 or C(1) c)
        c.ToString(); // 11
}
{
    if (!(o is 1 or C(1) c))
        c.ToString(); // 12
    else
        c.ToString();
}
{
    if (o is 1 or not C(1) c)
        c.ToString(); // 13
}
{
    if (!(o is 1 or not C(1) c))
        c.ToString(); // 14
}
{
    if (o is not (not C c, not C c))
        c.ToString(); // 15
}
{
    if (o is not ((not C c, _) and (_, not C c)))
        c.ToString(); // 16
}
{
    if (!(o is not ((not C c, _) and (_, not C c))))
        c.ToString(); // 17
}
{
    if (o is not ((not C c, _) or (_, not C c)))
        c.ToString(); // 18
}
{
    if (o is ((not C c, _) or (_, not C c))) // duplicate 4
        c.ToString(); // 19
}
{
    if (!(o is ((not C c, _) and (_, not C c))))
        c.ToString(); // 20
}
{
    if (!(o is not ((C c, _) or (_, C c))))
        c.ToString();
}
{
    if (o is (C(1) c or C(2) c) and I i)
        _ = (c, i);
}
{
    if (o is I i and (C(1) c or C(2) c))
        _ = (c, i);
}

class C
{
    public void Deconstruct(out object i) => throw null;
    public void Deconstruct(out object i, out object j) => throw null;
}
interface I
{
}
";
        CreateCompilation(new[] { program, TestSources.ITuple }).VerifyEmitDiagnostics(
                // (24,9): warning CS8794: An expression of type 'object' always matches the provided pattern.
                //     if (o is var c or var c) // always-true 1
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is var c or var c").WithArguments("object").WithLocation(24, 9),
                // (30,9): warning CS8794: An expression of type 'object' always matches the provided pattern.
                //     if (o is var c or 1) // always-true 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is var c or 1").WithArguments("object").WithLocation(30, 9),
                // (31,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 01
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(31, 9),
                // (36,9): warning CS8794: An expression of type 'object' always matches the provided pattern.
                //     if (o is 1 or var c) // always-true 3
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is 1 or var c").WithArguments("object").WithLocation(36, 9),
                // (37,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 02
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(37, 9),
                // (43,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 03
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(43, 9),
                // (51,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 04
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(51, 9),
                // (54,37): error CS0128: A local variable or function named 'c' is already defined in this scope
                //     if (o is not C(1) c or not C(2) c) // duplicate 1
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(54, 37),
                // (55,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 05
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(55, 9),
                // (60,34): error CS0128: A local variable or function named 'c' is already defined in this scope
                //     if (o is not C(1) c and C(2) c) // duplicate 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(60, 34),
                // (61,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 06
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(61, 9),
                // (66,34): error CS0128: A local variable or function named 'c' is already defined in this scope
                //     if (o is C(1) c and not C(2) c) // duplicate 3
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(66, 34),
                // (69,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 07
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(69, 9),
                // (75,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 08
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(75, 9),
                // (79,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 09
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(79, 9),
                // (85,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 10
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(85, 9),
                // (91,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 11
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(91, 9),
                // (95,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 12
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(95, 9),
                // (101,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 13
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(101, 9),
                // (105,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 14
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(105, 9),
                // (109,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 15
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(109, 9),
                // (113,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 16
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(113, 9),
                // (117,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 17
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(117, 9),
                // (121,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 18
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(121, 9),
                // (124,41): error CS0128: A local variable or function named 'c' is already defined in this scope
                //     if (o is ((not C c, _) or (_, not C c))) // duplicate 4
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(124, 41),
                // (125,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 19
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(125, 9),
                // (129,9): error CS0165: Use of unassigned local variable 'c'
                //         c.ToString(); // 20
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(129, 9)
                );
    }

    [Fact]
    public void DecisionDag_SwitchStatement()
    {
        var program = @"
using static System.Console;
using static C;

WriteLine(Simplify01('*', 1, 9));
WriteLine(Simplify01('*', 9, 1));
WriteLine(Simplify01('*', 9, 9));

WriteLine(Simplify01('+', 0, 9));
WriteLine(Simplify01('+', 9, 0));
WriteLine(Simplify01('+', 9, 9));

WriteLine(Simplify02('*', 1, 9));
WriteLine(Simplify02('*', 9, 1));
WriteLine(Simplify02('*', 9, 9));

WriteLine(Simplify02('+', 0, 9));
WriteLine(Simplify02('+', 9, 0));
WriteLine(Simplify02('+', 9, 9));

static class C
{
    public static object Simplify01(char op, int left, int right)
    {
        switch (op, left, right)
        {
            case ('*', 1, var x) or
                 ('*', var x, 1) or
                 ('+', 0, var x) or
                 ('+', var x, 0):
                return x;
            default:
                return -1;
        }
    }

    public static object Simplify02(char op, int left, int right)
    {
        switch (op, left, right)
        {
            case ('*', 1, var x):
            case ('*', var x, 1):
            case ('+', 0, var x):
            case ('+', var x, 0):
                return x;
            default:
                return -1;
        }
    }
}
";
        var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
9
9
-1
9
9
-1
9
9
-1
9
9
-1";
        var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        AssertEx.Multiple(
            () => VerifyDecisionDagDump<SwitchStatementSyntax>(compilation,
@"[0]: t1 = t0.op; [1]
[1]: t1 == * ? [2] : [6]
[2]: t2 = t0.left; [3]
[3]: t2 == 1 ? [9] : [4]
[4]: t3 = t0.right; [5]
[5]: t3 == 1 ? [13] : [15]
[6]: t1 == + ? [7] : [15]
[7]: t2 = t0.left; [8]
[8]: t2 == 0 ? [9] : [11]
[9]: t3 = t0.right; [10]
[10]: bind x = t3; [14]
[11]: t3 = t0.right; [12]
[12]: t3 == 0 ? [13] : [15]
[13]: bind x = t2; [14]
[14]: leaf `case ('*', 1, var x) or
                 ('*', var x, 1) or
                 ('+', 0, var x) or
                 ('+', var x, 0):`
[15]: leaf `default`
", index: 0),
            () => VerifyDecisionDagDump<SwitchStatementSyntax>(compilation,
@"[0]: t1 = t0.op; [1]
[1]: t1 == * ? [2] : [11]
[2]: t2 = t0.left; [3]
[3]: t2 == 1 ? [4] : [7]
[4]: t3 = t0.right; [5]
[5]: bind x = t3; [6]
[6]: leaf `case ('*', 1, var x):`
[7]: t3 = t0.right; [8]
[8]: t3 == 1 ? [9] : [21]
[9]: bind x = t2; [10]
[10]: leaf `case ('*', var x, 1):`
[11]: t1 == + ? [12] : [21]
[12]: t2 = t0.left; [13]
[13]: t2 == 0 ? [14] : [17]
[14]: t3 = t0.right; [15]
[15]: bind x = t3; [16]
[16]: leaf `case ('+', 0, var x):`
[17]: t3 = t0.right; [18]
[18]: t3 == 0 ? [19] : [21]
[19]: bind x = t2; [20]
[20]: leaf `case ('+', var x, 0):`
[21]: leaf `default`
", index: 1),
            () => verifier.VerifyIL("C.Simplify01",
@"{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  beq.s      IL_000c
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.s   43
  IL_0008:  beq.s      IL_0016
  IL_000a:  br.s       IL_0029
  IL_000c:  ldarg.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0019
  IL_0010:  ldarg.2
  IL_0011:  ldc.i4.1
  IL_0012:  beq.s      IL_0020
  IL_0014:  br.s       IL_0029
  IL_0016:  ldarg.1
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  ldarg.2
  IL_001a:  stloc.0
  IL_001b:  br.s       IL_0022
  IL_001d:  ldarg.2
  IL_001e:  brtrue.s   IL_0029
  IL_0020:  ldarg.1
  IL_0021:  stloc.0
  IL_0022:  ldloc.0
  IL_0023:  box        ""int""
  IL_0028:  ret
  IL_0029:  ldc.i4.m1
  IL_002a:  box        ""int""
  IL_002f:  ret
}"),
            () => verifier.VerifyIL("C.Simplify02",
@"{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  beq.s      IL_000c
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.s   43
  IL_0008:  beq.s      IL_001c
  IL_000a:  br.s       IL_002f
  IL_000c:  ldarg.1
  IL_000d:  ldc.i4.1
  IL_000e:  bne.un.s   IL_0014
  IL_0010:  ldarg.2
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_0028
  IL_0014:  ldarg.2
  IL_0015:  ldc.i4.1
  IL_0016:  bne.un.s   IL_002f
  IL_0018:  ldarg.1
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_0028
  IL_001c:  ldarg.1
  IL_001d:  brtrue.s   IL_0023
  IL_001f:  ldarg.2
  IL_0020:  stloc.0
  IL_0021:  br.s       IL_0028
  IL_0023:  ldarg.2
  IL_0024:  brtrue.s   IL_002f
  IL_0026:  ldarg.1
  IL_0027:  stloc.0
  IL_0028:  ldloc.0
  IL_0029:  box        ""int""
  IL_002e:  ret
  IL_002f:  ldc.i4.m1
  IL_0030:  box        ""int""
  IL_0035:  ret
}")
        );
    }

    [Fact]
    public void DecisionDag_IsPatternExpression()
    {
        var program = @"
using static System.Console;
using static C;

WriteLine(Simplify01('*', 1, 9));
WriteLine(Simplify01('*', 9, 1));
WriteLine(Simplify01('*', 9, 9));

WriteLine(Simplify01('+', 0, 9));
WriteLine(Simplify01('+', 9, 0));
WriteLine(Simplify01('+', 9, 9));

WriteLine(Simplify02('*', 1, 9));
WriteLine(Simplify02('*', 9, 1));
WriteLine(Simplify02('*', 9, 9));

WriteLine(Simplify02('+', 0, 9));
WriteLine(Simplify02('+', 9, 0));
WriteLine(Simplify02('+', 9, 9));

static class C
{
    public static object Simplify01(char op, int left, int right)
    {
        if ((op, left, right) is
                ('*', 1, var x) or
                ('*', var x, 1) or
                ('+', 0, var x) or
                ('+', var x, 0))
        {
            return x;
        }
        return -1;
    }

    public static object Simplify02(char op, int left, int right)
    {
        if ((op, left, right) is
                not ('*', 1, var x) and
                not ('*', var x, 1) and
                not ('+', 0, var x) and
                not ('+', var x, 0))
        {
            return -1;
        }
        return x;
    }
}
";
        var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
        string expectedOutput = @"
9
9
-1
9
9
-1
9
9
-1
9
9
-1";
        var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        AssertEx.Multiple(
            () => VerifyDecisionDagDump<IsPatternExpressionSyntax>(compilation,
@"[0]: t1 = t0.op; [1]
[1]: t1 == * ? [2] : [6]
[2]: t2 = t0.left; [3]
[3]: t2 == 1 ? [9] : [4]
[4]: t3 = t0.right; [5]
[5]: t3 == 1 ? [13] : [15]
[6]: t1 == + ? [7] : [15]
[7]: t2 = t0.left; [8]
[8]: t2 == 0 ? [9] : [11]
[9]: t3 = t0.right; [10]
[10]: bind x = t3; [14]
[11]: t3 = t0.right; [12]
[12]: t3 == 0 ? [13] : [15]
[13]: bind x = t2; [14]
[14]: leaf <isPatternSuccess> `('*', 1, var x) or
                ('*', var x, 1) or
                ('+', 0, var x) or
                ('+', var x, 0)`
[15]: leaf <isPatternFailure> `('*', 1, var x) or
                ('*', var x, 1) or
                ('+', 0, var x) or
                ('+', var x, 0)`
", index: 0),
            () => VerifyDecisionDagDump<IsPatternExpressionSyntax>(compilation,
@"[0]: t1 = t0.op; [1]
[1]: t1 == * ? [2] : [6]
[2]: t2 = t0.left; [3]
[3]: t2 == 1 ? [9] : [4]
[4]: t3 = t0.right; [5]
[5]: t3 == 1 ? [13] : [15]
[6]: t1 == + ? [7] : [15]
[7]: t2 = t0.left; [8]
[8]: t2 == 0 ? [9] : [11]
[9]: t3 = t0.right; [10]
[10]: bind x = t3; [14]
[11]: t3 = t0.right; [12]
[12]: t3 == 0 ? [13] : [15]
[13]: bind x = t2; [14]
[14]: leaf <isPatternFailure> `not ('*', 1, var x) and
                not ('*', var x, 1) and
                not ('+', 0, var x) and
                not ('+', var x, 0)`
[15]: leaf <isPatternSuccess> `not ('*', 1, var x) and
                not ('*', var x, 1) and
                not ('+', 0, var x) and
                not ('+', var x, 0)`
", index: 1),
            () => verifier.VerifyIL("C.Simplify01",
@"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (int V_0, //x
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  beq.s      IL_000c
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.s   43
  IL_0008:  beq.s      IL_0016
  IL_000a:  br.s       IL_0026
  IL_000c:  ldarg.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0019
  IL_0010:  ldarg.2
  IL_0011:  ldc.i4.1
  IL_0012:  beq.s      IL_0020
  IL_0014:  br.s       IL_0026
  IL_0016:  ldarg.1
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  ldarg.2
  IL_001a:  stloc.0
  IL_001b:  br.s       IL_0022
  IL_001d:  ldarg.2
  IL_001e:  brtrue.s   IL_0026
  IL_0020:  ldarg.1
  IL_0021:  stloc.0
  IL_0022:  ldc.i4.1
  IL_0023:  stloc.1
  IL_0024:  br.s       IL_0028
  IL_0026:  ldc.i4.0
  IL_0027:  stloc.1
  IL_0028:  ldloc.1
  IL_0029:  brfalse.s  IL_0032
  IL_002b:  ldloc.0
  IL_002c:  box        ""int""
  IL_0031:  ret
  IL_0032:  ldc.i4.m1
  IL_0033:  box        ""int""
  IL_0038:  ret
}"),
            () => verifier.VerifyIL("C.Simplify02",
@"{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (int V_0, //x
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  beq.s      IL_000c
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.s   43
  IL_0008:  beq.s      IL_0016
  IL_000a:  br.s       IL_0024
  IL_000c:  ldarg.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0019
  IL_0010:  ldarg.2
  IL_0011:  ldc.i4.1
  IL_0012:  beq.s      IL_0020
  IL_0014:  br.s       IL_0024
  IL_0016:  ldarg.1
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  ldarg.2
  IL_001a:  stloc.0
  IL_001b:  br.s       IL_0028
  IL_001d:  ldarg.2
  IL_001e:  brtrue.s   IL_0024
  IL_0020:  ldarg.1
  IL_0021:  stloc.0
  IL_0022:  br.s       IL_0028
  IL_0024:  ldc.i4.1
  IL_0025:  stloc.1
  IL_0026:  br.s       IL_002a
  IL_0028:  ldc.i4.0
  IL_0029:  stloc.1
  IL_002a:  ldloc.1
  IL_002b:  brfalse.s  IL_0034
  IL_002d:  ldc.i4.m1
  IL_002e:  box        ""int""
  IL_0033:  ret
  IL_0034:  ldloc.0
  IL_0035:  box        ""int""
  IL_003a:  ret
}")
        );
    }
}
