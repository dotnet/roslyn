// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class LabeledBreakContinueBindingTests : CSharpTestBase
{
    private static readonly CSharpParseOptions s_optionsCSharp14 =
        TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14);

    #region Valid: all loop types

    [Fact]
    public void Break_LabeledWhile()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        while (true)
                            break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_LabeledWhile()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        while (true)
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_LabeledDoWhile()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: do
                    {
                        do break outer; while (true);
                    } while (true);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_LabeledDoWhile()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: do
                    {
                        while (true)
                            continue outer;
                    } while (false);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_LabeledFor()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                            break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,37): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37));
    }

    [Fact]
    public void Continue_LabeledFor()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,37): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37));
    }

    [Fact]
    public void Break_LabeledForEach()
    {
        var source = """
            class C
            {
                void M(string[] args)
                {
                    outer: foreach (var a in args)
                    {
                        foreach (var b in args)
                            break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_LabeledForEach()
    {
        var source = """
            class C
            {
                void M(string[] args)
                {
                    outer: foreach (var a in args)
                    {
                        foreach (var b in args)
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_LabeledForEachDeconstruction()
    {
        var source = """
            class C
            {
                void M((int, int)[] items)
                {
                    outer: foreach (var (x, y) in items)
                    {
                        foreach (var z in items)
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Valid: switch

    [Fact]
    public void Break_LabeledSwitch()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    outer: switch (x)
                    {
                        default:
                            while (true)
                                break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_LabelOnLoop_FromInsideSwitch()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    outer: while (true)
                    {
                        switch (x)
                        {
                            default:
                                break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_NestedSwitches_BreakOuter()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    outer: switch (x)
                    {
                        case 0:
                            switch (x)
                            {
                                case 0: break outer;
                            }
                            break;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Valid: nested labels

    [Fact]
    public void Break_StackedLabels()
    {
        var source = """
            class C
            {
                void M()
                {
                    a: b: c: while (true)
                    {
                        break b;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(5, 9),
            // (5,15): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(5, 15),
            // (7,19): error CS9388: No enclosing loop or switch statement with the label 'b' out of which to break
            //             break b;
            Diagnostic(ErrorCode.ERR_NoBreakId, "b").WithArguments("b").WithLocation(7, 19));
    }

    [Fact]
    public void Break_DeeplyNested_LoopSwitchLoop()
    {
        var source = """
            class C
            {
                void M(int a, int b)
                {
                    L0: while (a-- > 0)
                    {
                        L1: switch (b)
                        {
                            default:
                                L2: while (true)
                                {
                                    if (a == 0) break L0;
                                    if (b == 0) break L1;
                                    continue L2;
                                }
                                break;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (16,21): warning CS0162: Unreachable code detected
            //                     break;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(16, 21));
    }

    #endregion

    #region Valid: interaction with other constructs

    [Fact]
    public void Break_InsideTryCatch()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        try
                        {
                            break outer;
                        }
                        catch { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_InsideChecked()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        checked
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,40): warning CS0162: Unreachable code detected
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(5, 40));
    }

    [Fact]
    public void Break_InsideLockUsing()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        lock (this)
                        {
                            using var d = default(System.IDisposable);
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_PatternSwitchWithWhen()
    {
        var source = """
            class C
            {
                void M(object o)
                {
                    outer: while (true)
                    {
                        switch (o)
                        {
                            case int x when x > 0:
                                break outer;
                            default:
                                break;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_AsyncMethod()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M()
                {
                    outer: for (int i = 0; i < 1; i++)
                    {
                        for (int j = 0; j < 1; j++)
                        {
                            await Task.Yield();
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,36): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 1; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(8, 36));
    }

    [Fact]
    public void Break_Iterator()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M()
                {
                    outer: while (true)
                    {
                        yield return 1;
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_GotoAndLabeledBreak_SameLabel()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: while (true)
                    {
                        goto L;
                        break L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             break L;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 13));
    }

    [Fact]
    public void Continue_SwitchInsideLabeledFor()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    L: for (;;)
                        switch (x)
                        {
                            default: continue L;
                        }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_UnicodeEscapedLabel()
    {
        var source = """
            class C
            {
                void M()
                {
                    \u006Futer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_UnreachableLabeledBreak_StillSuppressesWarning()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break;
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             break outer;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 13));
    }

    [Fact]
    public void Continue_UnreachableLabeledContinue_StillSuppressesWarning()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break;
                        continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             continue outer;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "continue").WithLocation(8, 13));
    }

    [Fact]
    public void Break_StackedLabels_OnlyValidLabelReferenced()
    {
        var source = """
            class C
            {
                void M()
                {
                    a: b: c: while (true)
                    {
                        break c;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(5, 9),
            // (5,12): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(5, 12));
    }

    [Fact]
    public void Break_MultipleLabeledBreaksToSameLabel()
    {
        var source = """
            class C
            {
                void M(bool x)
                {
                    outer: while (true)
                    {
                        if (x)
                            break outer;
                        else
                            break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_MultipleLabeledContinuesToSameLabel()
    {
        var source = """
            class C
            {
                void M(bool x)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        if (x)
                            continue outer;
                        else
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_MixedUnlabeledAndLabeledExits()
    {
        var source = """
            class C
            {
                void M(bool cond)
                {
                    outer: while (true)
                    {
                        while (cond)
                            break;
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_LabeledBreakFirst_GotoUnreachable()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: while (true)
                    {
                        break L;
                        goto L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             goto L;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(8, 13));
    }

    [Fact]
    public void Break_InCatch_WithFinally_TargetsOuterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        try
                        {
                            throw new System.Exception();
                        }
                        catch
                        {
                            break outer;
                        }
                        finally { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Invalid: label not found

    [Fact]
    public void Break_LabelNotFound()
    {
        var source = """
            class C
            {
                void M()
                {
                    while (true)
                        break missing;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,19): error CS9388: No enclosing loop or switch statement with the label 'missing' out of which to break
            //             break missing;
            Diagnostic(ErrorCode.ERR_NoBreakId, "missing").WithArguments("missing").WithLocation(6, 19));
    }

    [Fact]
    public void Break_LabelInSiblingBlock()
    {
        var source = """
            class C
            {
                void M()
                {
                    { L: while (true) { } }
                    { break L; }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,11): warning CS0164: This label has not been referenced
            //         { L: while (true) { } }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 11),
            // (6,11): warning CS0162: Unreachable code detected
            //         { break L; }
            Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(6, 11),
            // (6,17): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(6, 17));
    }

    #endregion

    #region Invalid: label not on loop/switch

    [Fact]
    public void Break_LabelOnBlock()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: { break L; }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         L: { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 20));
    }

    [Fact]
    public void Break_LabelOnIf()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: if (true)
                        break L;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,19): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //             break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(6, 19));
    }

    [Fact]
    public void Break_LabelOnEmptyStatement()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: ;
                    while (true) break L;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,28): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         while (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(6, 28));
    }

    [Fact]
    public void Break_LabelOnExpressionStatement()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: System.Console.WriteLine();
                    if (true) break L;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,25): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         if (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(6, 25));
    }

    [Fact]
    public void Break_LabelOnLocalDeclaration_SwitchExpression()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    L: var y = x switch { _ => 1 };
                    while (true) break L;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,28): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         while (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(6, 28));
    }

    [Fact]
    public void Break_LabelOnLock()
    {
        var source = """
            class C
            {
                void M(object o)
                {
                    L: lock (o) { break L; }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,29): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //         L: lock (o) { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 29));
    }

    #endregion

    #region Invalid: continue targeting switch

    [Fact]
    public void Continue_TargetIsSwitch()
    {
        var source = """
            class C
            {
                void M(int x)
                {
                    outer: switch (x)
                    {
                        default: continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,31): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //             default: continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 31));
    }

    #endregion

    #region Invalid: label not containing

    [Fact]
    public void Break_LabelOnSiblingLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (false) { }
                    while (true) { break outer; }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,30): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //         while (true) { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(6, 30));
    }

    [Fact]
    public void Continue_LabelOnFollowingLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    while (true) { continue outer; }
                    outer: while (false) { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,33): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //         while (true) { continue outer; }
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(5, 33));
    }

    [Fact]
    public void Break_LabelAfterBreak()
    {
        var source = """
            class C
            {
                void M()
                {
                    foreach (var x in new int[0])
                    {
                        break outer;
                    outer: foreach (var y in new int[0]) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,19): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //             break outer;
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(7, 19));
    }

    [Fact]
    public void Continue_LabelOnInnerLoop_NotContaining()
    {
        var source = """
            class C
            {
                void M()
                {
                    foreach (var x in new int[0])
                    {
                        continue outer;
                    outer: foreach (var y in new int[0]) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,22): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 22));
    }

    [Fact]
    public void Break_LabelOnDeeperNestedLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    foreach (var x in new int[0])
                    {
                        continue outer;
                        { outer: foreach (var y in new int[0]) { } }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,22): error CS9388: No enclosing loop with the label 'outer' out of which to continue
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 22),
            // (8,15): warning CS0164: This label has not been referenced
            //             { outer: foreach (var y in new int[0]) { } }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(8, 15));
    }

    #endregion

    #region Invalid: label various illegal positions

    [Fact]
    public void Continue_LabelExistsAfterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    foreach (var x in new int[0])
                    {
                        continue outer;
                    }
                    outer: ;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,22): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 22));
    }

    [Fact]
    public void Continue_LabelBeforeLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: ;
                    foreach (var x in new int[0])
                    {
                        continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,22): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(8, 22));
    }

    [Fact]
    public void Break_LabelOnContainingForEach_InsideNestedBlock()
    {
        var source = """
            class C
            {
                void M()
                {
                    foreach (var x in new int[0])
                    {
                        continue outer;
                        { outer: foreach (var y in new int[0]) { } }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,22): error CS9388: No enclosing loop with the label 'outer' out of which to continue
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 22),
            // (8,15): warning CS0164: This label has not been referenced
            //             { outer: foreach (var y in new int[0]) { } }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(8, 15));
    }

    #endregion

    #region Lambda and local function boundaries

    [Fact]
    public void Break_InsideLambda_LabelOutside()
    {
        var source = """
            using System;
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        Action a = () => { break outer; };
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 9),
            // (8,32): error CS1632: Control cannot leave the body of an anonymous method or lambda expression
            //             Action a = () => { break outer; };
            Diagnostic(ErrorCode.ERR_BadDelegateLeave, "break").WithLocation(8, 32));
    }

    [Fact]
    public void Break_InsideLocalFunction_LabelOutside()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        void F() { break outer; }
                        F();
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,30): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //             void F() { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(7, 30));
    }

    [Fact]
    public void Break_LabelInsideLambda_BreakOutside()
    {
        var source = """
            using System;
            class C
            {
                void M()
                {
                    Action a = () => { outer: while (true) { } };
                    break outer;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,28): warning CS0164: This label has not been referenced
            //         Action a = () => { outer: while (true) { } };
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 28),
            // (7,15): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //         break outer;
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(7, 15));
    }

    [Fact]
    public void Break_InsideLambda_TargetsLoopInsideLambda()
    {
        var source = """
            using System;
            class C
            {
                void M()
                {
                    Action a = () =>
                    {
                        outer: while (true)
                        {
                            while (true)
                                break outer;
                        }
                    };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_InsideLambda_TargetsLoopInsideLambda()
    {
        var source = """
            using System;
            class C
            {
                void M()
                {
                    Action a = () =>
                    {
                        outer: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                                continue outer;
                        }
                    };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,41): warning CS0162: Unreachable code detected
            //                     for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(10, 41));
    }

    [Fact]
    public void Break_InsideLocalFunction_TargetsLoopInsideLocalFunction()
    {
        var source = """
            class C
            {
                void M()
                {
                    void F()
                    {
                        outer: while (true)
                        {
                            while (true)
                                break outer;
                        }
                    }
                    F();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_InsideLocalFunction_TargetsLoopInsideLocalFunction()
    {
        var source = """
            class C
            {
                void M()
                {
                    void F()
                    {
                        outer: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                                continue outer;
                        }
                    }
                    F();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,41): warning CS0162: Unreachable code detected
            //                     for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(9, 41));
    }

    #endregion

    #region Finally blocks

    [Fact]
    public void Break_InFinally_TargetsOuterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        try { }
                        finally { break outer; }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,40): warning CS0162: Unreachable code detected
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(5, 40),
            // (8,23): error CS0157: Control cannot leave the body of a finally clause
            //             finally { break outer; }
            Diagnostic(ErrorCode.ERR_BadFinallyLeave, "break").WithLocation(8, 23));
    }

    [Fact]
    public void Continue_InFinally_TargetsOuterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        try { }
                        finally { continue outer; }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,23): error CS0157: Control cannot leave the body of a finally clause
            //             finally { continue outer; }
            Diagnostic(ErrorCode.ERR_BadFinallyLeave, "continue").WithLocation(8, 23));
    }

    [Fact]
    public void Break_InFinally_TargetsLoopInsideFinally()
    {
        var source = """
            class C
            {
                void M()
                {
                    try { }
                    finally
                    {
                        outer: while (true)
                        {
                            while (true)
                                break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_InFinally_TargetsLoopInsideFinally()
    {
        var source = """
            class C
            {
                void M()
                {
                    try { }
                    finally
                    {
                        outer: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                                continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,41): warning CS0162: Unreachable code detected
            //                     for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(10, 41));
    }

    [Fact]
    public void Break_InTryWithFinally_Valid()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        try { break outer; }
                        finally { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Using statements

    [Fact]
    public void LabeledBreakInUsingStatement()
    {
        var source = """
            outer: while (true)
            {
                using var x = GetResource();
                break outer; 
            }

            System.IDisposable GetResource() => null;
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void LabeledContinueInUsingStatement()
    {
        var source = """
            outer: while (true)
            {
                using var x = GetResource();
                continue outer; 
            }

            System.IDisposable GetResource() => null;
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Language version gating

    [Fact]
    public void Break_CSharp14_FeatureInPreview()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                        break outer;
                }
            }
            """;
        CreateCompilation(source, parseOptions: s_optionsCSharp14).VerifyDiagnostics(
            // (6,19): error CS8652: The feature 'labeled break and continue' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             break outer;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "outer").WithArguments("labeled break and continue").WithLocation(6, 19));
    }

    [Fact]
    public void Continue_CSharp14_FeatureInPreview()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                            continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source, parseOptions: s_optionsCSharp14).VerifyDiagnostics(
            // (7,37): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37),
            // (8,26): error CS8652: The feature 'labeled break and continue' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //                 continue outer;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "outer").WithArguments("labeled break and continue").WithLocation(8, 26));
    }

    #endregion

    #region Top-level statements

    [Fact]
    public void Break_TopLevelStatements()
    {
        var source = """
            outer: for (int i = 0; i < 1; i++)
                break outer;
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,31): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; i < 1; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 31));
    }

    [Fact]
    public void Continue_TopLevelStatements()
    {
        var source = """
            outer: for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                    continue outer;
            }
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (3,29): warning CS0162: Unreachable code detected
            //     for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(3, 29));
    }

    [Fact]
    public void Break_TopLevelStatements_Switch()
    {
        var source = """
            int x = 0;
            outer: switch (x)
            {
                case 0:
                    switch (x)
                    {
                        case 0: break outer;
                    }
                    break;
            }
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
    }

    #endregion

    #region Reachability and definite assignment

    [Fact]
    public void StatementAfterLabeledBreak_Unreachable()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                        System.Console.WriteLine("dead");
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             System.Console.WriteLine("dead");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 13));
    }

    [Fact]
    public void StatementAfterLabeledContinue_Unreachable()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        continue outer;
                        System.Console.WriteLine("dead");
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS0162: Unreachable code detected
            //             System.Console.WriteLine("dead");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 13));
    }

    [Fact]
    public void LabeledBreak_DefinitelyAssignedAfterLoop()
    {
        var source = """
            class C
            {
                int M()
                {
                    int x;
                    outer: while (true)
                    {
                        while (true)
                        {
                            x = 1;
                            break outer;
                        }
                    }
                    return x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void LabeledBreak_NotDefinitelyAssigned_ReportsCS0165()
    {
        var source = """
            class C
            {
                int M(bool b)
                {
                    int x;
                    outer: while (true)
                    {
                        if (b)
                        {
                            x = 1;
                            break outer;
                        }
                        break outer;
                    }
                    return x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (15,16): error CS0165: Use of unassigned local variable 'x'
            //         return x;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(15, 16));
    }

    [Fact]
    public void LabeledContinue_PreventsFallthrough_NoDefiniteAssignmentError()
    {
        var source = """
            class C
            {
                void M(int[] items, bool b)
                {
                    outer: foreach (var item in items)
                    {
                        int x;
                        if (b)
                        {
                            x = 1;
                        }
                        else
                        {
                            continue outer;
                        }
                        System.Console.WriteLine(x);
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void IntReturningMethod_WithLabeledBreak_NotAllPathsReturn_ReportsCS0161()
    {
        var source = """
            class C
            {
                int M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,9): error CS0161: 'C.M()': not all code paths return a value
            //     int M()
            Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(3, 9));
    }

    [Fact]
    public void IntReturningMethod_InfiniteLoopNoLabeledBreak_NoError()
    {
        var source = """
            class C
            {
                int M()
                {
                    outer: while (true)
                    {
                        continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void LabeledBreak_ExitsTryFinally_CodeAfterLoop_Reachable()
    {
        var source = """
            class C
            {
                int M()
                {
                    int x;
                    outer: while (true)
                    {
                        try
                        {
                            x = 1;
                            break outer;
                        }
                        finally
                        {
                            System.Console.WriteLine("cleanup");
                        }
                    }
                    return x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Scope transparency (unchecked / unsafe / fixed)

    [Fact]
    public void Break_Unchecked_TargetIsOuterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        unchecked
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_Unchecked_TargetIsOuterLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        unchecked
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Break_Unsafe_TargetIsOuterLoop()
    {
        var source = """
            class C
            {
                unsafe void M()
                {
                    outer: while (true)
                    {
                        unsafe
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_Unsafe_TargetIsOuterLoop()
    {
        var source = """
            class C
            {
                unsafe void M()
                {
                    outer: while (true)
                    {
                        unsafe
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
    }

    [Fact]
    public void Break_Fixed_TargetIsOuterLoop()
    {
        var source = """
            class C
            {
                unsafe void M(int[] a)
                {
                    outer: while (true)
                    {
                        fixed (int* p = a)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
    }

    #endregion

    #region Label shadowing

    [Fact]
    public void LabelShadowing_LocalFunctionInsideLabeledLoop_ReportsShadowing()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: while (true)
                    {
                        void Inner()
                        {
                            L: while (true)
                            {
                                break L;
                            }
                        }
                        break L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,18): warning CS8321: The local function 'Inner' is declared but never used
            //             void Inner()
            Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Inner").WithArguments("Inner").WithLocation(7, 18),
            // (9,17): error CS0158: The label 'L' shadows another label by the same name in a contained scope
            //                 L: while (true)
            Diagnostic(ErrorCode.ERR_LabelShadow, "L").WithArguments("L").WithLocation(9, 17));
    }

    [Fact]
    public void LabelShadowing_LambdaInsideLabeledLoop_ReportsShadowing()
    {
        var source = """
            using System;
            class C
            {
                void M()
                {
                    L: while (true)
                    {
                        Action a = () =>
                        {
                            L: while (true)
                            {
                                break L;
                            }
                        };
                        break L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,17): error CS0158: The label 'L' shadows another label by the same name in a contained scope
            //                 L: while (true)
            Diagnostic(ErrorCode.ERR_LabelShadow, "L").WithArguments("L").WithLocation(10, 17));
    }

    [Fact]
    public void LabelShadowing_DistinctLabelsInNestedLocalFunction_NoError()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        void Inner()
                        {
                            inner: while (true)
                            {
                                break inner;
                            }
                        }
                        Inner();
                        break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void LabelShadowing_MethodLevelDuplicate_ReportsError()
    {
        var source = """
            class C
            {
                void M()
                {
                    L: while (true) { break L; }
                    L: while (true) { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,9): error CS0140: The label 'L' is a duplicate
            //         L: while (true) { }
            Diagnostic(ErrorCode.ERR_DuplicateLabel, "L").WithArguments("L").WithLocation(6, 9));
    }

    // Labels live in their own name space, so a closer local with the same
    // name as an enclosing label must not prevent break/continue from
    // resolving the label.
    [Fact]
    public void LabelShadowing_LocalWithSameNameAsOuterLabel_Break_ResolvesToLabel()
    {
        var source = """
            class C
            {
                static void M()
                {
                    outer: while (true)
                    {
                        int outer = 1;
                        if (outer > 0)
                            break outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void LabelShadowing_LocalWithSameNameAsOuterLabel_Continue_ResolvesToLabel()
    {
        var source = """
            class C
            {
                static void M()
                {
                    outer: for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int outer = j;
                            if (outer == 1)
                                continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Top-level statements: error cases

    [Fact]
    public void Break_TopLevelStatements_UnknownLabel()
    {
        var source = """
            outer: while (true)
            {
                break missing;
            }
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): warning CS0164: This label has not been referenced
            // outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(1, 1),
            // (3,11): error CS9388: No enclosing loop or switch statement with the label 'missing' out of which to breake
            //     break missing;
            Diagnostic(ErrorCode.ERR_NoBreakId, "missing").WithArguments("missing").WithLocation(3, 11));
    }

    [Fact]
    public void Continue_TopLevelStatements_UnknownLabel()
    {
        var source = """
            outer: while (true)
            {
                continue missing;
            }
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): warning CS0164: This label has not been referenced
            // outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(1, 1),
            // (3,14): error CS9388: No enclosing loop with the label 'missing' out of which to continue
            //     continue missing;
            Diagnostic(ErrorCode.ERR_NoContinueId, "missing").WithArguments("missing").WithLocation(3, 14));
    }

    [Fact]
    public void Break_TopLevelStatements_LabeledJumpOutsideAnyLoop()
    {
        var source = """
            L: ;
            break L;
            """;
        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,7): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            // break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(2, 7));
    }

    #endregion

    #region Async iterator and await foreach

    [Fact]
    public void Break_AwaitForeach_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async Task M(IAsyncEnumerable<int> items)
                {
                    outer: await foreach (var x in items)
                    {
                        await foreach (var y in items)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_AwaitForeach_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async Task M(IAsyncEnumerable<int> items)
                {
                    outer: await foreach (var x in items)
                    {
                        await foreach (var y in items)
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    [Fact]
    public void Break_AsyncIterator_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async IAsyncEnumerable<int> M(int[] items)
                {
                    outer: foreach (var x in items)
                    {
                        foreach (var y in items)
                        {
                            await Task.Yield();
                            yield return y;
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_AsyncIterator_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async IAsyncEnumerable<int> M(int[] items)
                {
                    outer: foreach (var x in items)
                    {
                        foreach (var y in items)
                        {
                            await Task.Yield();
                            yield return y;
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    #endregion

    #region Ref foreach

    [Fact]
    public void Break_RefForeach_Labeled()
    {
        var source = """
            using System;
            class C
            {
                void M(Span<int> items)
                {
                    outer: foreach (ref int x in items)
                    {
                        foreach (ref int y in items)
                        {
                            if (y == 0)
                                break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_RefForeach_Labeled()
    {
        var source = """
            using System;
            class C
            {
                void M(Span<int> items)
                {
                    outer: foreach (ref int x in items)
                    {
                        foreach (ref int y in items)
                        {
                            if (y == 0)
                                continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    #endregion

    #region Out-var in loop condition

    [Fact]
    public void Break_OutVarInCondition_Labeled()
    {
        var source = """
            class C
            {
                bool Filter(out int i) { i = 0; return true; }
                void M()
                {
                    outer: while (Filter(out var i))
                    {
                        while (Filter(out var j))
                        {
                            if (i + j > 10)
                                break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_OutVarInCondition_Labeled()
    {
        var source = """
            class C
            {
                bool Filter(out int i) { i = 0; return true; }
                void M()
                {
                    outer: while (Filter(out var i))
                    {
                        while (Filter(out var j))
                        {
                            if (i + j > 10)
                                continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Labeled break/continue in if without enclosing loop

    [Fact]
    public void Break_IfWithoutEnclosingLoop_Labeled()
    {
        var source = """
            class C
            {
                void M(bool b)
                {
                    if (b) break outer;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,22): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //         if (b) break outer;
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(5, 22));
    }

    [Fact]
    public void Continue_IfWithoutEnclosingLoop_Labeled()
    {
        var source = """
            class C
            {
                void M(bool b)
                {
                    if (b) continue outer;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,25): error CS9388: No enclosing loop with the label 'outer' out of which to continue
            //         if (b) continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(5, 25));
    }

    #endregion

    #region Primary constructor parameter capture

    [Fact]
    public void LabeledBreakContinue_DoesNotCapturePrimaryConstructorParameter()
    {
        // The label identifiers in `break p1;` / `continue p2;` are label references,
        // not value references to the same-named primary-constructor parameters, so
        // the parameters should be reported as unread (no capture).
        var source = """
            class C(int p1, int p2)
            {
                void M()
                {
                    p1: while (true) { break p1; }
                    p2: foreach (var x in new[] { 1 }) { continue p2; }
                }
            }
            """;
        CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyEmitDiagnostics(
            // (1,13): warning CS9113: Parameter 'p1' is unread.
            // class C(int p1, int p2)
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(1, 13),
            // (1,21): warning CS9113: Parameter 'p2' is unread.
            // class C(int p1, int p2)
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p2").WithArguments("p2").WithLocation(1, 21));
    }

    #endregion

    #region Resolved-label-as-target side effects (probing for leaks)

    // When a labeled break/continue can't find an enclosing loop/switch, but the identifier does
    // resolve to *some* label symbol, the binder uses that label as the target so that downstream
    // passes treat the label as referenced.  These tests probe whether using a reachable-but-illegal
    // label as the target leaks any unexpected diagnostics in interesting scopes (lambdas, async
    // lambdas, iterator methods, finally blocks, separate switch sections, etc.).

    [Fact]
    public void Break_InsideLambda_LabelOutside_LeaksBadDelegateLeave()
    {
        // Pre-existing behavior, NOT introduced by the resolved-label-as-target change: the lambda
        // binder lets GetBreakLabel walk past the lambda boundary at bind time and find the outer
        // loop's break label.  As a result BindBreakOrContinue produces a non-errored
        // BoundBreakStatement and ControlFlowPass later reports CS1632 ("Control cannot leave...")
        // when the break can't actually be matched to a loop in the lambda's local scope.
        // Documented here so we notice if the resolved-label-as-target change ever starts
        // surfacing a *different* leak in this scenario.
        var source = """
            using System;
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        Action a = () => { break outer; };
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 9),
            // (8,32): error CS1632: Control cannot leave the body of an anonymous method or lambda expression
            //             Action a = () => { break outer; };
            Diagnostic(ErrorCode.ERR_BadDelegateLeave, "break").WithLocation(8, 32));
    }

    [Fact]
    public void Continue_InsideAsyncLambda_LabelOutside_LeaksBadDelegateLeave()
    {
        // Same lambda-boundary behavior as above, this time for an async lambda + continue.
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        Func<Task> a = async () => { await Task.Yield(); continue outer; };
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(7, 9),
            // (9,62): error CS1632: Control cannot leave the body of an anonymous method or lambda expression
            //             Func<Task> a = async () => { await Task.Yield(); continue outer; };
            Diagnostic(ErrorCode.ERR_BadDelegateLeave, "continue").WithLocation(9, 62));
    }

    [Fact]
    public void Break_InsideIterator_LabelOutside_StillReportsUnreachableCode()
    {
        // Inside an iterator method with a labeled break that has no valid target.  The break
        // becomes errored -> WRN_UnreferencedLabel is suppressed for `outer:`.  CS0162 is still
        // reported on the trailing `while (true)` because the previous `while (true)` body is
        // unreachable for a different reason (the break above has nothing to do with it).
        // Probing test: confirms the resolved-label-as-target change does not introduce
        // a *new* unreachable-code surprise here.
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M()
                {
                    outer: while (true)
                    {
                        yield return 1;
                    }
                    while (true) break outer;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,9): warning CS0162: Unreachable code detected
            //         while (true) break outer;
            Diagnostic(ErrorCode.WRN_UnreachableCode, "while").WithLocation(10, 9),
            // (10,28): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //         while (true) break outer;
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(10, 28));
    }

    [Fact]
    public void Break_InsideFinally_LabelOnOuterBlock_LeaksControlCannotLeaveFinally()
    {
        // The user-declared label `L:` *is* visible from inside the finally block, so BindLabel
        // resolves it.  GetBreakLabel returns null (try/finally provide no break target) so
        // BindBreakOrContinue falls through with target := L's LabelSymbol and hasErrors=true.
        // ControlFlowPass then reports CS0157 ("Control cannot leave the body of a finally
        // clause"), because the synthesized goto edge would in fact leave the finally.
        // This is a real *new* leak introduced by the resolved-label-as-target change.
        var source = """
            class C
            {
                void M()
                {
                    L: try { }
                    finally
                    {
                        break L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): error CS0157: Control cannot leave the body of a finally clause
            //             break L;
            Diagnostic(ErrorCode.ERR_BadFinallyLeave, "break").WithLocation(8, 13),
            // (8,19): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //             break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(8, 19));
    }

    [Fact]
    public void Break_InSwitchSection_LabelInDifferentSection_LabelTreatedAsReferenced()
    {
        // The label `L:` is in a different switch section from `break L;`.  Labels are visible
        // across switch sections at bind time, so BindLabel resolves to L's LabelSymbol and the
        // synthesized target makes flow analysis treat L as referenced (no CS0164).
        var source = """
            class C
            {
                void M(int x)
                {
                    switch (x)
                    {
                        case 0:
                            L: break;
                        case 1:
                            break L;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,23): error CS9388: No enclosing loop or switch statement with the label 'L' out of which to break
            //             break L;
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(10, 23));
    }

    [Fact]
    public void Continue_TargetOnSwitch_SuppressesSwitchFallOutAndUnreferencedLabel()
    {
        // Documents that ERR_SwitchFallOut (CS8070) is also suppressed for the case where the
        // synthesized target makes the continue look like a real exit edge.  This is a
        // *consequence* of resolving the bad label as the target, not a separately reported error.
        var source = """
            class C
            {
                void M(int x)
                {
                    outer: switch (x)
                    {
                        default: continue outer;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,31): error CS9389: No enclosing loop with the label 'outer' out of which to continue
            //             default: continue outer;
            Diagnostic(ErrorCode.ERR_NoContinueId, "outer").WithArguments("outer").WithLocation(7, 31));
    }

    [Fact]
    public void Break_LabelOnFollowingLoop_SuppressesUnreachableCodeAndUnreferencedLabel()
    {
        // Documents that WRN_UnreachableCode (CS0162) is also suppressed.  The break is errored,
        // but flow analysis still treats it as a real exit, so the trailing labeled loop
        // becomes "reachable" via the synthesized target -- no unreachable warning is produced.
        var source = """
            class C
            {
                void M()
                {
                    while (true) { break outer; }
                    outer: while (false) { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,30): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //         while (true) { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(5, 30));
    }

    [Fact]
    public void GetSymbolInfo_BreakWithBadTarget_SemanticModelStillResolvesIdentifier()
    {
        // The IDE side-benefit of this approach: even though the break is errored, the SemanticModel
        // can still resolve the identifier to its label symbol, so find-references / rename / etc.
        // continue to work on the broken code.
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        void F() { break outer; }
                        F();
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,30): error CS9388: No enclosing loop or switch statement with the label 'outer' out of which to break
            //             void F() { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakId, "outer").WithArguments("outer").WithLocation(7, 30));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var declaredSymbol = model.GetDeclaredSymbol(labelDecl);

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single(b => b.Name is not null);
        Assert.Same(declaredSymbol, model.GetSymbolInfo(breakSyntax.Name!).Symbol);
    }

    #endregion
}
