// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: do
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: do
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: foreach (var a in args)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: foreach (var a in args)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: foreach (var (x, y) in items)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: switch (x)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: switch (x)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
            // (5,12): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(5, 12),
            // (5,15): warning CS0164: This label has not been referenced
            //         a: b: c: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(5, 15),
            // (7,19): error CS9378: No enclosing loop or switch statement with the label 'b' out of which to break or continue
            //             break b;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "b").WithArguments("b").WithLocation(7, 19));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L0: while (a-- > 0)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L0").WithLocation(5, 9),
            // (7,13): warning CS0164: This label has not been referenced
            //             L1: switch (b)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L1").WithLocation(7, 13),
            // (10,21): warning CS0164: This label has not been referenced
            //                     L2: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L2").WithLocation(10, 21),
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
            // (6,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 1; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 9),
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
        CreateCompilation(source).VerifyDiagnostics(
            // (6,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         L: for (;;)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         \u006Futer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, @"\u006Futer").WithLocation(5, 9));
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
            // (6,19): error CS0159: No such label 'missing' within the scope of the goto statement
            //             break missing;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "missing").WithArguments("missing").WithLocation(6, 19));
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
            // (6,17): error CS0159: No such label 'L' within the scope of the goto statement
            //         { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(6, 17));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: { break L; }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (5,20): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //         L: { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(5, 20));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: if (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (6,19): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //             break L;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(6, 19));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: ;
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (6,28): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //         while (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(6, 28));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: System.Console.WriteLine();
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (6,25): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //         if (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(6, 25));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: var y = x switch { _ => 1 };
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (6,28): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //         while (true) break L;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(6, 28));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         L: lock (o) { break L; }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9),
            // (5,29): error CS9378: The label 'L' does not refer to a containing loop or switch statement
            //         L: lock (o) { break L; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(5, 29));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: switch (x)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
            // (7,13): error CS8070: Control cannot fall out of switch from final case label ('default:')
            //             default: continue outer;
            Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(7, 13),
            // (7,31): error CS9379: The label 'outer' does not refer to a containing loop statement
            //             default: continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 31));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (false) { }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
            // (6,30): error CS9380: The label 'outer' does not refer to a statement that contains this break or continue statement
            //         while (true) { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(6, 30));
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
            // (5,33): error CS9380: The label 'outer' does not refer to a statement that contains this break or continue statement
            //         while (true) { continue outer; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(5, 33),
            // (6,9): warning CS0162: Unreachable code detected
            //         outer: while (false) { }
            Diagnostic(ErrorCode.WRN_UnreachableCode, "outer").WithLocation(6, 9),
            // (6,9): warning CS0164: This label has not been referenced
            //         outer: while (false) { }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(6, 9));
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
            // (7,19): error CS9380: The label 'outer' does not refer to a statement that contains this break or continue statement
            //             break outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 19),
            // (8,9): warning CS0164: This label has not been referenced
            //         outer: foreach (var y in new int[0]) { }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(8, 9));
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
            // (7,22): error CS9380: The label 'outer' does not refer to a statement that contains this break or continue statement
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 22),
            // (8,9): warning CS0164: This label has not been referenced
            //         outer: foreach (var y in new int[0]) { }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(8, 9));
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
            // (7,22): error CS0159: No such label 'outer' within the scope of the goto statement
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 22),
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
            // (7,22): error CS9379: The label 'outer' does not refer to a containing loop statement
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 22),
            // (9,9): warning CS0164: This label has not been referenced
            //         outer: ;
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(9, 9));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: ;
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
            // (8,22): error CS9379: The label 'outer' does not refer to a containing loop statement
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(8, 22));
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
            // (7,22): error CS0159: No such label 'outer' within the scope of the goto statement
            //             continue outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 22),
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
            // (7,30): error CS9378: No enclosing loop or switch statement with the label 'outer' out of which to break or continue
            //             void F() { break outer; }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 30));
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
            // (7,15): error CS0159: No such label 'outer' within the scope of the goto statement
            //         break outer;
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "outer").WithArguments("outer").WithLocation(7, 15));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
            // (8,23): error CS0157: Control cannot leave the body of a finally clause
            //             finally { continue outer; }
            Diagnostic(ErrorCode.ERR_BadFinallyLeave, "continue").WithLocation(8, 23));
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
        CreateCompilation(source).VerifyDiagnostics(
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9));
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: while (true)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
            // (5,9): warning CS0164: This label has not been referenced
            //         outer: for (int i = 0; i < 10; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(5, 9),
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
            // (1,1): warning CS0164: This label has not been referenced
            // outer: for (int i = 0; i < 1; i++)
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outer").WithLocation(1, 1),
            // (1,31): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; i < 1; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 31));
    }

    #endregion
}
