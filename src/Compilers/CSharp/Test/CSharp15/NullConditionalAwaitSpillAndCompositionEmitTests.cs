// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitSpillAndCompositionEmitTests : CSharpTestBase
{
    // Covers interactions between `await?` and constructs that force spilling of an await across
    // a complex expression (argument lists, binary ops, ternaries, null-coalesce, object
    // initializers, switch expressions), plus composition with `?.`, nested `await?`, and
    // ConfigureAwait. Each test runs in both state-machine and runtime-async modes with meaningful
    // console output so execution can be cross-checked.

    private static readonly CSharpParseOptions s_preview = TestOptions.RegularPreview;

    private CompilationVerifier VerifyStateMachine(string source, string expectedOutput)
    {
        return CompileAndVerify(
            source,
            parseOptions: s_preview,
            targetFramework: TargetFramework.NetCoreApp,
            options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput);
    }

    private CompilationVerifier VerifyRuntimeAsync(string source, string expectedOutput)
    {
        var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(
            comp,
            expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
            verify: Verification.Skipped);
        verifier.VerifyDiagnostics();
        return verifier;
    }

    // Tests in this file end each test's Main with `Console.Write("done")` so that a missing
    // "done" reveals any mid-method abort. Every test additionally runs the null-receiver case
    // AFTER the non-null case, so the lines printed by the null case double as proof that the
    // non-null case returned control to the enclosing method.

    #region Spilling: await? inside non-trivial expressions

    [Fact]
    public void Spill_AwaitQuestion_AsArgument()
    {
        // F(a, await? t, b) — argument a must be spilled into a temp so it survives the await
        // suspension. With `await?` the entire short-circuit-or-await dance happens between the
        // evaluation of `a` and `b`.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static void F(int a, int? b, int c) => Console.WriteLine($"F({a},{b?.ToString() ?? "null"},{c})");

                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(20);
                    F(G("a"), await? t1, G("c"));

                    Task<int> t2 = null;
                    F(G("a2"), await? t2, G("c2"));
                    Console.Write("done");
                }

                static int G(string label) { Console.Write($"{label};"); return label.Length; }
            }
            """;
        var expected = """
            a;c;F(1,20,1)
            a2;c2;F(2,null,2)
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_AwaitQuestion_InBinaryOp()
    {
        // (await? taskOfInt).GetValueOrDefault() + side-effect — binary op with `await?` on the left,
        // side-effecting call on the right. Left operand (Nullable<int>) must be spilled before
        // evaluating the right. Null-receiver case: the left operand's GetValueOrDefault() must
        // still produce 0 and the right side-effect must still run.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(10);
                    int sum1 = (await? t1).GetValueOrDefault() + G(5);
                    Console.WriteLine(sum1);

                    Task<int> t2 = null;
                    int sum2 = (await? t2).GetValueOrDefault() + G(7);
                    Console.WriteLine(sum2);
                    Console.Write("done");
                }

                static int G(int x) { Console.Write($"G({x});"); return x; }
            }
            """;
        var expected = """
            G(5);15
            G(7);7
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_TwoAwaitQuestions_InBinaryOp()
    {
        // Two `await?` operands in a binary op. Each produces its own null-check-and-await, and
        // the left one must be spilled past the second. Null-left case: left contributes 0, the
        // right await still runs and contributes its value.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(3);
                    Task<int> t2 = Task.FromResult(4);
                    int sum = (await? t1).GetValueOrDefault() + (await? t2).GetValueOrDefault();
                    Console.WriteLine(sum);

                    Task<int> n = null;
                    int sumN = (await? n).GetValueOrDefault() + (await? t2).GetValueOrDefault();
                    Console.WriteLine(sumN);
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            7
            4
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_AwaitQuestion_InTernaryBranches()
    {
        // Both ternary branches contain `await?`; the condition is evaluated first, then the
        // selected branch's null-check-and-await runs. Also cover the null-operand case in each
        // branch to prove short-circuit inside a ternary continues correctly.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> a = Task.FromResult(1);
                    Task<int> b = Task.FromResult(2);
                    Task<int> an = null;
                    Task<int> bn = null;

                    Console.WriteLine((true  ? await? a  : await? b )?.ToString() ?? "null");
                    Console.WriteLine((false ? await? a  : await? b )?.ToString() ?? "null");
                    Console.WriteLine((true  ? await? an : await? b )?.ToString() ?? "null");
                    Console.WriteLine((false ? await? a  : await? bn)?.ToString() ?? "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            1
            2
            null
            null
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_AwaitQuestion_InNullCoalesce()
    {
        // left ?? await? right. If left is non-null, right is not evaluated at all. If left is null,
        // the `await?` runs and produces the fallback (including the null-right case, where the
        // `await?` itself short-circuits).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    string left = "present";
                    Task<string> right = Task.FromResult("fallback");
                    string v1 = left ?? await? right;
                    Console.WriteLine(v1);

                    left = null;
                    string v2 = left ?? await? right;
                    Console.WriteLine(v2);

                    left = null;
                    Task<string> nullRight = null;
                    string v3 = left ?? await? nullRight;
                    Console.WriteLine(v3 ?? "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            present
            fallback
            null
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_AwaitQuestion_InObjectInitializer()
    {
        // Object initializer with `await?` in a property assignment. Null-receiver case assigns
        // default(int?) to the property and the object-under-construction must still be returned
        // (demonstrates construction completes and `b2` is a well-formed reference).
        var source = """
            using System;
            using System.Threading.Tasks;

            class Box { public int? P; public override string ToString() => P?.ToString() ?? "null"; }

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(99);
                    var b1 = new Box { P = await? t1 };
                    Console.WriteLine(b1);

                    Task<int> t2 = null;
                    var b2 = new Box { P = await? t2 };
                    Console.WriteLine(b2);
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            99
            null
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Spill_AwaitQuestion_InSwitchExpression()
    {
        // `await? task` value as the governing expression of a switch expression. The null pattern
        // must match for the null-receiver case and execution continue afterwards.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(1);
                    string s1 = (await? t1) switch { 1 => "one", 2 => "two", null => "null", _ => "other" };
                    Console.WriteLine(s1);

                    Task<int> t2 = null;
                    string s2 = (await? t2) switch { 1 => "one", 2 => "two", null => "null", _ => "other" };
                    Console.WriteLine(s2);
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            one
            null
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Nested and composition

    [Fact]
    public void Nested_AwaitQuestion_AwaitQuestion()
    {
        // await? await? Task<Task<int>>.
        //   - outer operand null  -> int? = null (inner short-circuit doesn't run).
        //   - outer operand non-null, inner operand null -> int? = null.
        //   - outer non-null, inner non-null -> int? with the awaited value.
        // All three paths must return control and continue to the next statement.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<Task<int>> outerNull = null;
                    int? v1 = await? await? outerNull;
                    Console.WriteLine(v1?.ToString() ?? "outer-null");

                    Task<Task<int>> innerNull = Task.FromResult<Task<int>>(null);
                    int? v2 = await? await? innerNull;
                    Console.WriteLine(v2?.ToString() ?? "inner-null");

                    Task<Task<int>> both = Task.FromResult(Task.FromResult(42));
                    int? v3 = await? await? both;
                    Console.WriteLine(v3?.ToString() ?? "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            outer-null
            inner-null
            42
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Composition_NullConditionalMemberAccessPlusAwaitQuestion()
    {
        // `await?` composed with `?.`: `await? task?.ConfigureAwait(false)` — ConfigureAwait is
        // only invoked if task is non-null, and its result (Nullable<ConfiguredTaskAwaitable>) is
        // then the operand to `await?`.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task t1 = Task.CompletedTask;
                    Console.Write("nn;");
                    await? t1?.ConfigureAwait(false);
                    Console.Write("after-nn;");

                    Task t2 = null;
                    Console.Write("null;");
                    await? t2?.ConfigureAwait(false);
                    Console.Write("after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "nn;after-nn;null;after-null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Composition_AwaitQuestion_Inside_NullConditionalCall()
    {
        // `x?.M(await? y)` — null-conditional call composed with null-conditional await. If x is
        // null the enclosing `?.` short-circuits and the `await?` must NOT be evaluated at all.
        // To prove that, the third case uses a throwing awaitable as `y`: if the argument were
        // evaluated while `rn == null`, GetAwaiter would throw and the test would fail.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class Receiver
            {
                public string M(int? value) => $"M({value?.ToString() ?? "null"})";
            }

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter()
                    => throw new InvalidOperationException("GetAwaiter must not be called when the outer `?.` short-circuits.");
            }

            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    Receiver r = new();

                    Task<int> t1 = Task.FromResult(7);
                    Console.WriteLine(r?.M(await? t1) ?? "r-null");

                    Task<int> t2 = null;
                    Console.WriteLine(r?.M(await? t2) ?? "r-null");

                    Receiver rn = null;
                    ThrowingAwaitable thrower = new();
                    Console.WriteLine(rn?.M(await? thrower) ?? "r-null");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            M(7)
            M(null)
            r-null
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Evaluation-once and order-of-evaluation guarantees

    [Fact]
    public void EvaluateOnce_Receiver_HasSideEffects()
    {
        // The receiver expression in `await? receiver` must be evaluated exactly once (both for
        // the null check and for the underlying await) even when it has side effects. Exercises
        // both a non-null-returning factory and a null-returning factory to prove the single-
        // evaluation guarantee holds on both paths.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static int s_evalCount;

                static Task<int> GetTask(bool returnNull)
                {
                    s_evalCount++;
                    Console.Write($"eval{s_evalCount};");
                    return returnNull ? null : Task.FromResult(11);
                }

                public static async Task Main()
                {
                    int? v = await? GetTask(returnNull: false);
                    Console.WriteLine($"v={v};count={s_evalCount}");

                    int? vn = await? GetTask(returnNull: true);
                    Console.WriteLine($"vn={vn?.ToString() ?? "null"};count={s_evalCount}");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            eval1;v=11;count=1
            eval2;vn=null;count=2
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void EvaluateOnce_Receiver_HasSideEffects_NullableOperand()
    {
        // Same evaluate-once guarantee as the reference-operand test above, but for a
        // Nullable<V> receiver. The side-effecting factory must be called exactly once per
        // `await?` even though the lowering both null-checks the source and reads its value.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static int s_evalCount;

                static ValueTask<int>? GetTask(bool returnNull)
                {
                    s_evalCount++;
                    Console.Write($"eval{s_evalCount};");
                    return returnNull ? null : new ValueTask<int>(21);
                }

                public static async Task Main()
                {
                    int? v = await? GetTask(returnNull: false);
                    Console.WriteLine($"v={v};count={s_evalCount}");

                    int? vn = await? GetTask(returnNull: true);
                    Console.WriteLine($"vn={vn?.ToString() ?? "null"};count={s_evalCount}");
                    Console.Write("done");
                }
            }
            """;
        var expected = """
            eval1;v=21;count=1
            eval2;vn=null;count=2
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void EvaluationOrder_AwaitQuestion_InArgList()
    {
        // Arguments of a multi-arg call containing `await?` are evaluated strictly left-to-right.
        // Including a null-receiver `await?` proves that the short-circuit doesn't disturb the
        // ordering of surrounding argument evaluations.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static void F(int a, int? b, int c, int? d)
                    => Console.WriteLine($"F({a},{b?.ToString() ?? "null"},{c},{d?.ToString() ?? "null"})");

                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(2);
                    Task<int> t2 = null;
                    F(G(1), await? t1, G(3), await? t2);
                    Console.Write("done");
                }

                static int G(int x) { Console.Write($"G({x});"); return x; }
            }
            """;
        var expected = """
            G(1);G(3);F(1,2,3,null)
            done
            """;
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion
}
