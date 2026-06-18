// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class LabeledBreakContinueEmitTests : CSharpTestBase
{
    // Tests that target Net100 can only execute on a runtime that supports it.
    // On other hosts (e.g. net472 CI) the emitted assembly references Net 10
    // assemblies that cannot be loaded for execution, so skip the runtime check
    // by passing null for the expected output.
    private static string? IncludeExpectedOutput(string expectedOutput) =>
        ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

    #region while

    [Fact]
    public void Break_While_Immediate()
    {
        var source = """
            outer: while (true)
            {
                System.Console.Write("A ");
                break outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A done").VerifyDiagnostics();
    }

    [Fact]
    public void Break_While_Nested()
    {
        var source = """
            outer: while (true)
            {
                while (true)
                {
                    System.Console.Write("A ");
                    break outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A done").VerifyDiagnostics(
            // (8,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 5));
    }

    [Fact]
    public void Continue_While_Immediate()
    {
        var source = """
            int i = 0;
            outer: while (i < 3)
            {
                i++;
                System.Console.Write(i + " ");
                continue outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 2 3 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_While_Nested()
    {
        var source = """
            int i = 0;
            outer: while (i < 3)
            {
                i++;
                while (true)
                {
                    System.Console.Write(i + " ");
                    continue outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 2 3 done").VerifyDiagnostics(
            // (10,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(10, 5));
    }

    #endregion

    #region do/while

    [Fact]
    public void Break_DoWhile_Immediate()
    {
        var source = """
            outer: do
            {
                System.Console.Write("A ");
                break outer;
            } while (true);
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A done").VerifyDiagnostics();
    }

    [Fact]
    public void Break_DoWhile_Nested()
    {
        var source = """
            outer: do
            {
                do
                {
                    System.Console.Write("A ");
                    break outer;
                } while (true);
                System.Console.Write("SKIPPED ");
            } while (true);
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A done").VerifyDiagnostics(
            // (8,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 5));
    }

    [Fact]
    public void Continue_DoWhile_Immediate()
    {
        var source = """
            int i = 0;
            outer: do
            {
                i++;
                System.Console.Write(i + " ");
                continue outer;
            } while (i < 3);
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 2 3 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_DoWhile_Nested()
    {
        var source = """
            int i = 0;
            outer: do
            {
                i++;
                while (true)
                {
                    System.Console.Write(i + " ");
                    continue outer;
                }
                System.Console.Write("SKIPPED ");
            } while (i < 3);
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 2 3 done").VerifyDiagnostics(
            // (10,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(10, 5));
    }

    #endregion

    #region for

    [Fact]
    public void Break_For_Immediate()
    {
        var source = """
            outer: for (int i = 0; ; i++)
            {
                System.Console.Write(i + " ");
                break outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "0 done").VerifyDiagnostics(
            // (1,26): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; ; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 26));
    }

    [Fact]
    public void Break_For_Nested()
    {
        var source = """
            outer: for (int i = 0; i < 3; i++)
            {
                for (int j = 0; ; j++)
                {
                    System.Console.Write($"({i},{j}) ");
                    break outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "(0,0) done").VerifyDiagnostics(
            // (3,23): warning CS0162: Unreachable code detected
            //     for (int j = 0; ; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(3, 23),
            // (8,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 5));
    }

    [Fact]
    public void Continue_For_Immediate()
    {
        var source = """
            outer: for (int i = 0; i < 3; i++)
            {
                System.Console.Write(i + " ");
                continue outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "0 1 2 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_For_Nested()
    {
        var source = """
            outer: for (int i = 0; i < 3; i++)
            {
                for (int j = 0; ; j++)
                {
                    System.Console.Write($"({i},{j}) ");
                    continue outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "(0,0) (1,0) (2,0) done").VerifyDiagnostics(
            // (3,23): warning CS0162: Unreachable code detected
            //     for (int j = 0; ; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(3, 23),
            // (8,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 5));
    }

    #endregion

    #region foreach

    [Fact]
    public void Break_ForEach_Immediate()
    {
        var source = """
            outer: foreach (var x in new[] { 1, 2, 3 })
            {
                System.Console.Write(x + " ");
                break outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 done").VerifyDiagnostics();
    }

    [Fact]
    public void Break_ForEach_Nested()
    {
        var source = """
            outer: foreach (var x in new[] { 1, 2, 3 })
            {
                foreach (var y in new[] { 10, 20 })
                {
                    System.Console.Write($"{x}+{y} ");
                    break outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1+10 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_ForEach_Immediate()
    {
        var source = """
            outer: foreach (var x in new[] { 1, 2, 3 })
            {
                System.Console.Write(x + " ");
                continue outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1 2 3 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_ForEach_Nested()
    {
        var source = """
            outer: foreach (var x in new[] { 1, 2, 3 })
            {
                foreach (var y in new[] { 10, 20 })
                {
                    System.Console.Write($"{x}+{y} ");
                    continue outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "1+10 2+10 3+10 done").VerifyDiagnostics();
    }

    #endregion

    #region switch

    [Fact]
    public void Break_Switch_Immediate()
    {
        var source = """
            int x = 1;
            outer: switch (x)
            {
                case 1:
                    System.Console.Write("case1 ");
                    break outer;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "case1 done").VerifyDiagnostics();
    }

    [Fact]
    public void Break_Switch_Nested()
    {
        var source = """
            outer: while (true)
            {
                switch (1)
                {
                    case 1:
                        System.Console.Write("inner ");
                        break outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "inner done").VerifyDiagnostics(
            // (9,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(9, 5));
    }

    [Fact]
    public void Break_Switch_FromNestedSwitch()
    {
        var source = """
            outer: switch (1)
            {
                case 1:
                    switch (2)
                    {
                        case 2:
                            System.Console.Write("nested ");
                            break outer;
                    }
                    System.Console.Write("SKIPPED ");
                    break;
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "nested done").VerifyDiagnostics(
            // (10,9): warning CS0162: Unreachable code detected
            //         System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(10, 9));
    }

    [Fact]
    public void Continue_For_FromSwitch()
    {
        var source = """
            outer: for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        System.Console.Write("zero ");
                        continue outer;
                    case 1:
                        System.Console.Write("one ");
                        continue outer;
                    default:
                        System.Console.Write("other ");
                        continue outer;
                }
                System.Console.Write("SKIPPED ");
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "zero one other done").VerifyDiagnostics(
            // (15,5): warning CS0162: Unreachable code detected
            //     System.Console.Write("SKIPPED ");
            Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(15, 5));
    }

    #endregion

    #region try/finally

    [Fact]
    public void Break_Finally_Single()
    {
        var source = """
            outer: for (int i = 0; ; i++)
            {
                try
                {
                    System.Console.Write("A ");
                    break outer;
                }
                finally { System.Console.Write("F "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A F done").VerifyDiagnostics(
            // (1,26): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; ; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 26));
    }

    [Fact]
    public void Break_Finally_Multiple_InnermostFirst()
    {
        var source = """
            outer: for (int i = 0; ; i++)
            {
                try
                {
                    try
                    {
                        try
                        {
                            System.Console.Write("A ");
                            break outer;
                        }
                        finally { System.Console.Write("F3 "); }
                    }
                    finally { System.Console.Write("F2 "); }
                }
                finally { System.Console.Write("F1 "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A F3 F2 F1 done").VerifyDiagnostics(
            // (1,26): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; ; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 26));
    }

    [Fact]
    public void Break_Finally_AcrossNestedLoops()
    {
        var source = """
            outer: for (int i = 0; ; i++)
            {
                try
                {
                    for (int j = 0; ; j++)
                    {
                        try
                        {
                            System.Console.Write("A ");
                            break outer;
                        }
                        finally { System.Console.Write("Finner "); }
                    }
                }
                finally { System.Console.Write("Fouter "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A Finner Fouter done").VerifyDiagnostics(
            // (1,26): warning CS0162: Unreachable code detected
            // outer: for (int i = 0; ; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(1, 26),
            // (5,27): warning CS0162: Unreachable code detected
            //         for (int j = 0; ; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(5, 27));
    }

    [Fact]
    public void Break_Finally_OuterTryNotExited()
    {
        var source = """
            try
            {
                outer: for (int i = 0; ; i++)
                {
                    try
                    {
                        System.Console.Write("A ");
                        break outer;
                    }
                    finally { System.Console.Write("Finner "); }
                }
                System.Console.Write("after-loop ");
            }
            finally { System.Console.Write("Fouter "); }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A Finner after-loop Fouter done").VerifyDiagnostics(
            // (3,30): warning CS0162: Unreachable code detected
            //     outer: for (int i = 0; ; i++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(3, 30));
    }

    [Fact]
    public void Continue_Finally_Single()
    {
        var source = """
            int count = 0;
            outer: while (count < 3)
            {
                count++;
                try
                {
                    System.Console.Write($"A{count} ");
                    continue outer;
                }
                finally { System.Console.Write("F "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A1 F A2 F A3 F done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_Finally_Multiple_InnermostFirst()
    {
        var source = """
            int count = 0;
            outer: while (count < 2)
            {
                count++;
                try
                {
                    try
                    {
                        try
                        {
                            System.Console.Write($"A{count} ");
                            continue outer;
                        }
                        finally { System.Console.Write("F3 "); }
                    }
                    finally { System.Console.Write("F2 "); }
                }
                finally { System.Console.Write("F1 "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A1 F3 F2 F1 A2 F3 F2 F1 done").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_Finally_AcrossNestedLoops()
    {
        var source = """
            int count = 0;
            outer: while (count < 2)
            {
                count++;
                try
                {
                    for (int j = 0; ; j++)
                    {
                        try
                        {
                            System.Console.Write($"A{count} ");
                            continue outer;
                        }
                        finally { System.Console.Write("Finner "); }
                    }
                }
                finally { System.Console.Write("Fouter "); }
            }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A1 Finner Fouter A2 Finner Fouter done").VerifyDiagnostics(
            // (7,27): warning CS0162: Unreachable code detected
            //         for (int j = 0; ; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 27));
    }

    [Fact]
    public void Continue_Finally_OuterTryNotExited()
    {
        var source = """
            int count = 0;
            try
            {
                outer: while (count < 2)
                {
                    count++;
                    try
                    {
                        System.Console.Write($"A{count} ");
                        continue outer;
                    }
                    finally { System.Console.Write("Finner "); }
                }
                System.Console.Write("after-loop ");
            }
            finally { System.Console.Write("Fouter "); }
            System.Console.Write("done");
            """;
        CompileAndVerify(source, expectedOutput: "A1 Finner A2 Finner after-loop Fouter done").VerifyDiagnostics();
    }

    #endregion

    #region await foreach and async iterator

    [Fact]
    public void Break_AwaitForeach_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    outer: await foreach (var x in Items())
                    {
                        await foreach (var y in Items())
                        {
                            System.Console.Write($"{x}{y} ");
                            if (x == 2) break outer;
                        }
                    }
                    System.Console.Write("done");
                }

                static async IAsyncEnumerable<int> Items()
                {
                    await Task.Yield();
                    yield return 1;
                    yield return 2;
                    yield return 3;
                }
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.Net100, expectedOutput: IncludeExpectedOutput("11 12 13 21 done"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_AwaitForeach_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    outer: await foreach (var x in Items())
                    {
                        await foreach (var y in Items())
                        {
                            System.Console.Write($"{x}{y} ");
                            if (y == 1) continue outer;
                        }
                        System.Console.Write("SKIPPED ");
                    }
                    System.Console.Write("done");
                }

                static async IAsyncEnumerable<int> Items()
                {
                    await Task.Yield();
                    yield return 1;
                    yield return 2;
                }
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.Net100, expectedOutput: IncludeExpectedOutput("11 21 done"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void Break_AsyncIterator_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var x in Produce())
                    {
                        System.Console.Write($"{x} ");
                    }
                    System.Console.Write("done");
                }

                static async IAsyncEnumerable<int> Produce()
                {
                    outer: for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            await Task.Yield();
                            yield return i * 10 + j;
                            if (i == 1 && j == 1) break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.Net100, expectedOutput: IncludeExpectedOutput("0 1 2 10 11 done"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void Continue_AsyncIterator_Labeled()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var x in Produce())
                    {
                        System.Console.Write($"{x} ");
                    }
                    System.Console.Write("done");
                }

                static async IAsyncEnumerable<int> Produce()
                {
                    outer: for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            await Task.Yield();
                            if (j == 1) continue outer;
                            yield return i * 10 + j;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.Net100, expectedOutput: IncludeExpectedOutput("0 10 20 done"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    #endregion

    #region Label name vs local name

    [Fact]
    public void Break_LocalWithSameNameAsOuterLabel_ResolvesToLabel()
    {
        var source = """
            int sum = 0;
            outer: for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int outer = i * 3 + j;
                    sum += outer;
                    if (outer == 4)
                        break outer;
                }
            }
            System.Console.Write($"sum={sum}");
            """;
        CompileAndVerify(source, expectedOutput: "sum=10").VerifyDiagnostics();
    }

    [Fact]
    public void Continue_LocalWithSameNameAsOuterLabel_ResolvesToLabel()
    {
        var source = """
            var seen = "";
            outer: for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int outer = i;
                    if (outer == 1)
                        continue outer;
                    seen += $"{i}{j} ";
                }
            }
            System.Console.Write(seen);
            """;
        CompileAndVerify(source, expectedOutput: "00 01 20 21 ").VerifyDiagnostics();
    }

    #endregion

    #region IL verification

    [Fact]
    public void IL_Break_While_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: while (b1)
                    {
                        while (b2)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        9 (0x9)
              .maxstack  1
              IL_0000:  br.s       IL_0005
              IL_0002:  ldarg.1
              IL_0003:  brtrue.s   IL_0008
              IL_0005:  ldarg.0
              IL_0006:  brtrue.s   IL_0002
              IL_0008:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_While_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2, bool b3)
                {
                    outer: while (b1)
                    {
                        while (b2)
                        {
                            if (b3) break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  br.s       IL_0008
              IL_0002:  ldarg.2
              IL_0003:  brtrue.s   IL_000b
              IL_0005:  ldarg.1
              IL_0006:  brtrue.s   IL_0002
              IL_0008:  ldarg.0
              IL_0009:  brtrue.s   IL_0005
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_While_Immediate()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: while (b)
                    {
                        continue outer;
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        4 (0x4)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  brtrue.s   IL_0000
              IL_0003:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_While_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: while (b1)
                    {
                        while (b2)
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  br.s       IL_0004
              IL_0002:  ldarg.1
              IL_0003:  pop
              IL_0004:  ldarg.0
              IL_0005:  brtrue.s   IL_0002
              IL_0007:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_While_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2, bool b3)
                {
                    outer: while (b1)
                    {
                        while (b2)
                        {
                            if (b3) continue outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  br.s       IL_0008
              IL_0002:  ldarg.2
              IL_0003:  brtrue.s   IL_0008
              IL_0005:  ldarg.1
              IL_0006:  brtrue.s   IL_0002
              IL_0008:  ldarg.0
              IL_0009:  brtrue.s   IL_0005
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_DoWhile_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: do
                    {
                        do
                        {
                            break outer;
                        } while (b2);
                    } while (b1);
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        1 (0x1)
              .maxstack  1
              IL_0000:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_DoWhile_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2, bool b3)
                {
                    outer: do
                    {
                        do
                        {
                            if (b3) break outer;
                        } while (b2);
                    } while (b1);
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              IL_0000:  ldarg.2
              IL_0001:  brtrue.s   IL_0009
              IL_0003:  ldarg.1
              IL_0004:  brtrue.s   IL_0000
              IL_0006:  ldarg.0
              IL_0007:  brtrue.s   IL_0000
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_DoWhile_Immediate()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: do
                    {
                        continue outer;
                    } while (b);
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        4 (0x4)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  brtrue.s   IL_0000
              IL_0003:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_DoWhile_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: do
                    {
                        do
                        {
                            continue outer;
                        } while (b2);
                    } while (b1);
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        4 (0x4)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  brtrue.s   IL_0000
              IL_0003:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_DoWhile_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2, bool b3)
                {
                    outer: do
                    {
                        do
                        {
                            if (b3) continue outer;
                        } while (b2);
                    } while (b1);
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              IL_0000:  ldarg.2
              IL_0001:  brtrue.s   IL_0006
              IL_0003:  ldarg.1
              IL_0004:  brtrue.s   IL_0000
              IL_0006:  ldarg.0
              IL_0007:  brtrue.s   IL_0000
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_For_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; b; j++)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics(
            // (7,32): warning CS0162: Unreachable code detected
            //             for (int j = 0; b; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 32)).VerifyIL("C.M", """
            {
              // Code size       19 (0x13)
              .maxstack  2
              .locals init (int V_0, //i
                           int V_1) //j
              IL_0000:  ldc.i4.0
              IL_0001:  stloc.0
              IL_0002:  br.s       IL_000d
              IL_0004:  ldc.i4.0
              IL_0005:  stloc.1
              IL_0006:  ldarg.0
              IL_0007:  brtrue.s   IL_0012
              IL_0009:  ldloc.0
              IL_000a:  ldc.i4.1
              IL_000b:  add
              IL_000c:  stloc.0
              IL_000d:  ldloc.0
              IL_000e:  ldc.i4.s   10
              IL_0010:  blt.s      IL_0004
              IL_0012:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_For_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; b1; j++)
                        {
                            if (b2) break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       28 (0x1c)
              .maxstack  2
              .locals init (int V_0, //i
                           int V_1) //j
              IL_0000:  ldc.i4.0
              IL_0001:  stloc.0
              IL_0002:  br.s       IL_0016
              IL_0004:  ldc.i4.0
              IL_0005:  stloc.1
              IL_0006:  br.s       IL_000f
              IL_0008:  ldarg.1
              IL_0009:  brtrue.s   IL_001b
              IL_000b:  ldloc.1
              IL_000c:  ldc.i4.1
              IL_000d:  add
              IL_000e:  stloc.1
              IL_000f:  ldarg.0
              IL_0010:  brtrue.s   IL_0008
              IL_0012:  ldloc.0
              IL_0013:  ldc.i4.1
              IL_0014:  add
              IL_0015:  stloc.0
              IL_0016:  ldloc.0
              IL_0017:  ldc.i4.s   10
              IL_0019:  blt.s      IL_0004
              IL_001b:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_For_Immediate()
    {
        var source = """
            class C
            {
                static void M()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        continue outer;
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              .locals init (int V_0) //i
              IL_0000:  ldc.i4.0
              IL_0001:  stloc.0
              IL_0002:  br.s       IL_0008
              IL_0004:  ldloc.0
              IL_0005:  ldc.i4.1
              IL_0006:  add
              IL_0007:  stloc.0
              IL_0008:  ldloc.0
              IL_0009:  ldc.i4.s   10
              IL_000b:  blt.s      IL_0004
              IL_000d:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_For_Nested()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; b; j++)
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics(
            // (7,32): warning CS0162: Unreachable code detected
            //             for (int j = 0; b; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 32)).VerifyIL("C.M", """
            {
              // Code size       18 (0x12)
              .maxstack  2
              .locals init (int V_0, //i
                           int V_1) //j
              IL_0000:  ldc.i4.0
              IL_0001:  stloc.0
              IL_0002:  br.s       IL_000c
              IL_0004:  ldc.i4.0
              IL_0005:  stloc.1
              IL_0006:  ldarg.0
              IL_0007:  pop
              IL_0008:  ldloc.0
              IL_0009:  ldc.i4.1
              IL_000a:  add
              IL_000b:  stloc.0
              IL_000c:  ldloc.0
              IL_000d:  ldc.i4.s   10
              IL_000f:  blt.s      IL_0004
              IL_0011:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_For_Nested_Conditional()
    {
        var source = """
            class C
            {
                static void M(bool b1, bool b2)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; b1; j++)
                        {
                            if (b2) continue outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       28 (0x1c)
              .maxstack  2
              .locals init (int V_0, //i
                           int V_1) //j
              IL_0000:  ldc.i4.0
              IL_0001:  stloc.0
              IL_0002:  br.s       IL_0016
              IL_0004:  ldc.i4.0
              IL_0005:  stloc.1
              IL_0006:  br.s       IL_000f
              IL_0008:  ldarg.1
              IL_0009:  brtrue.s   IL_0012
              IL_000b:  ldloc.1
              IL_000c:  ldc.i4.1
              IL_000d:  add
              IL_000e:  stloc.1
              IL_000f:  ldarg.0
              IL_0010:  brtrue.s   IL_0008
              IL_0012:  ldloc.0
              IL_0013:  ldc.i4.1
              IL_0014:  add
              IL_0015:  stloc.0
              IL_0016:  ldloc.0
              IL_0017:  ldc.i4.s   10
              IL_0019:  blt.s      IL_0004
              IL_001b:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_ForEach_Nested()
    {
        var source = """
            class C
            {
                static void M(int[] a, int[] b)
                {
                    outer: foreach (var x in a)
                    {
                        foreach (var y in b)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       38 (0x26)
              .maxstack  2
              .locals init (int[] V_0,
                           int V_1,
                           int[] V_2,
                           int V_3)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldc.i4.0
              IL_0003:  stloc.1
              IL_0004:  br.s       IL_001f
              IL_0006:  ldloc.0
              IL_0007:  ldloc.1
              IL_0008:  ldelem.i4
              IL_0009:  pop
              IL_000a:  ldarg.1
              IL_000b:  stloc.2
              IL_000c:  ldc.i4.0
              IL_000d:  stloc.3
              IL_000e:  br.s       IL_0015
              IL_0010:  ldloc.2
              IL_0011:  ldloc.3
              IL_0012:  ldelem.i4
              IL_0013:  pop
              IL_0014:  ret
              IL_0015:  ldloc.3
              IL_0016:  ldloc.2
              IL_0017:  ldlen
              IL_0018:  conv.i4
              IL_0019:  blt.s      IL_0010
              IL_001b:  ldloc.1
              IL_001c:  ldc.i4.1
              IL_001d:  add
              IL_001e:  stloc.1
              IL_001f:  ldloc.1
              IL_0020:  ldloc.0
              IL_0021:  ldlen
              IL_0022:  conv.i4
              IL_0023:  blt.s      IL_0006
              IL_0025:  ret
            }
            """);
    }

    [Fact]
    public void IL_Continue_ForEach_Nested()
    {
        var source = """
            class C
            {
                static void M(int[] a, int[] b)
                {
                    outer: foreach (var x in a)
                    {
                        foreach (var y in b)
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size       39 (0x27)
              .maxstack  2
              .locals init (int[] V_0,
                           int V_1,
                           int[] V_2,
                           int V_3)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldc.i4.0
              IL_0003:  stloc.1
              IL_0004:  br.s       IL_0020
              IL_0006:  ldloc.0
              IL_0007:  ldloc.1
              IL_0008:  ldelem.i4
              IL_0009:  pop
              IL_000a:  ldarg.1
              IL_000b:  stloc.2
              IL_000c:  ldc.i4.0
              IL_000d:  stloc.3
              IL_000e:  br.s       IL_0016
              IL_0010:  ldloc.2
              IL_0011:  ldloc.3
              IL_0012:  ldelem.i4
              IL_0013:  pop
              IL_0014:  br.s       IL_001c
              IL_0016:  ldloc.3
              IL_0017:  ldloc.2
              IL_0018:  ldlen
              IL_0019:  conv.i4
              IL_001a:  blt.s      IL_0010
              IL_001c:  ldloc.1
              IL_001d:  ldc.i4.1
              IL_001e:  add
              IL_001f:  stloc.1
              IL_0020:  ldloc.1
              IL_0021:  ldloc.0
              IL_0022:  ldlen
              IL_0023:  conv.i4
              IL_0024:  blt.s      IL_0006
              IL_0026:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_Switch_Immediate()
    {
        var source = """
            class C
            {
                static void M(int x)
                {
                    outer: switch (x)
                    {
                        case 0: break outer;
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        3 (0x3)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  pop
              IL_0002:  ret
            }
            """);
    }

    [Fact]
    public void IL_Break_Switch_FromWhile()
    {
        var source = """
            class C
            {
                static void M(bool b, int x)
                {
                    outer: while (b)
                    {
                        switch (x)
                        {
                            case 0: break outer;
                        }
                    }
                }
            }
            """;
        CompileAndVerify(source).VerifyDiagnostics().VerifyIL("C.M", """
            {
              // Code size        9 (0x9)
              .maxstack  1
              IL_0000:  br.s       IL_0005
              IL_0002:  ldarg.1
              IL_0003:  brfalse.s  IL_0008
              IL_0005:  ldarg.0
              IL_0006:  brtrue.s   IL_0002
              IL_0008:  ret
            }
            """);
    }

    #endregion

    #region PDB sequence points

    [Fact]
    public void SequencePoints_LabeledBreak()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                break outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.DebugDll);
        comp.VerifyPdb("C.M", """
            <symbols>
              <files>
                <file id="1" name="" language="C#" />
              </files>
              <methods>
                <method containingType="C" name="M" parameterNames="b">
                  <sequencePoints>
                    <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                    <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="15" document="1" />
                    <entry offset="0x2" hidden="true" document="1" />
                    <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="10" document="1" />
                    <entry offset="0x5" hidden="true" document="1" />
                    <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="14" document="1" />
                    <entry offset="0x8" startLine="9" startColumn="17" endLine="9" endColumn="23" document="1" />
                    <entry offset="0xa" hidden="true" document="1" />
                    <entry offset="0xd" startLine="10" startColumn="21" endLine="10" endColumn="33" document="1" />
                    <entry offset="0xf" startLine="11" startColumn="13" endLine="11" endColumn="14" document="1" />
                    <entry offset="0x10" startLine="7" startColumn="13" endLine="7" endColumn="22" document="1" />
                    <entry offset="0x12" hidden="true" document="1" />
                    <entry offset="0x15" startLine="12" startColumn="9" endLine="12" endColumn="10" document="1" />
                    <entry offset="0x16" startLine="5" startColumn="16" endLine="5" endColumn="25" document="1" />
                    <entry offset="0x18" hidden="true" document="1" />
                    <entry offset="0x1b" startLine="13" startColumn="5" endLine="13" endColumn="6" document="1" />
                  </sequencePoints>
                </method>
              </methods>
            </symbols>
            """, format: DebugInformationFormat.PortablePdb, options: PdbValidationOptions.ExcludeCustomDebugInformation);
    }

    [Fact]
    public void SequencePoints_LabeledContinue()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                continue outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.DebugDll);
        comp.VerifyPdb("C.M", """
            <symbols>
              <files>
                <file id="1" name="" language="C#" />
              </files>
              <methods>
                <method containingType="C" name="M" parameterNames="b">
                  <sequencePoints>
                    <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                    <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="15" document="1" />
                    <entry offset="0x2" hidden="true" document="1" />
                    <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="10" document="1" />
                    <entry offset="0x5" hidden="true" document="1" />
                    <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="14" document="1" />
                    <entry offset="0x8" startLine="9" startColumn="17" endLine="9" endColumn="23" document="1" />
                    <entry offset="0xa" hidden="true" document="1" />
                    <entry offset="0xd" startLine="10" startColumn="21" endLine="10" endColumn="36" document="1" />
                    <entry offset="0xf" startLine="11" startColumn="13" endLine="11" endColumn="14" document="1" />
                    <entry offset="0x10" startLine="7" startColumn="13" endLine="7" endColumn="22" document="1" />
                    <entry offset="0x12" hidden="true" document="1" />
                    <entry offset="0x15" startLine="12" startColumn="9" endLine="12" endColumn="10" document="1" />
                    <entry offset="0x16" startLine="5" startColumn="16" endLine="5" endColumn="25" document="1" />
                    <entry offset="0x18" hidden="true" document="1" />
                    <entry offset="0x1b" startLine="13" startColumn="5" endLine="13" endColumn="6" document="1" />
                  </sequencePoints>
                </method>
              </methods>
            </symbols>
            """, format: DebugInformationFormat.PortablePdb, options: PdbValidationOptions.ExcludeCustomDebugInformation);
    }

    #endregion
}
