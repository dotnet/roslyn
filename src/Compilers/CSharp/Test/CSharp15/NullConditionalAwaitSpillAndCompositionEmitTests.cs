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

    #region Switch-expression arm unification

    [Fact]
    public void SwitchExpression_MixedArms_AwaitQuestionAndLiteral()
    {
        // Switch expression arms: one arm returns `await? t` (int?), another returns a
        // literal int. Common type inference lifts literal int → int?.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = Task.FromResult(42);
                    int? v1 = 'a' switch
                    {
                        'a' => await? t,
                        _ => 0,
                    };
                    Console.Write($"v1={v1};");

                    Task<int> nullT = null;
                    int? v2 = 'a' switch
                    {
                        'a' => await? nullT,
                        _ => 0,
                    };
                    Console.Write($"v2={v2?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "v1=42;v2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void SwitchExpression_MixedArms_AwaitQuestionAndNullConditional()
    {
        // One arm returns `await? t`, another returns `x?.ToString()`. Both are `string?`
        // so the switch unifies to `string?` and the spill across the arm must preserve
        // the await.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t = Task.FromResult("awaited");
                    string plain = "non-awaited";
                    var v = 'a' switch
                    {
                        'a' => await? t,
                        _ => plain?.ToString(),
                    };
                    Console.Write($"v={v};done");
                }
            }
            """;
        var expected = "v=awaited;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Compound assignment RHS spill

    [Fact]
    public void CompoundAssign_Plus_AwaitQuestionRhs()
    {
        // `x += await? t;` — compound add with await? on the RHS. For `int x` and int?
        // from await?, the binary '+ ' is lifted to Nullable<int>, so the assignment
        // target must accept int? (or we get a conversion error). Use `int?` for x.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    int? x = 10;
                    Task<int> t = Task.FromResult(5);
                    x += await? t;
                    Console.Write($"x-nn={x};");

                    int? y = 10;
                    Task<int> nullTask = null;
                    y += await? nullTask;
                    Console.Write($"y-null={y?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        // Lifted `int? + int?` returns null when either side is null.
        var expected = "x-nn=15;y-null=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void CompoundAssign_StringConcat_AwaitQuestionRhs()
    {
        // `s += await? t;` — string concat with await? on RHS. For reference-result,
        // await? produces `string?`. String concat allows null operands (treated as "").
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    string s = "[";
                    Task<string> t1 = Task.FromResult("x");
                    s += await? t1;
                    s += "|";
                    Task<string> t2 = null;
                    s += await? t2;
                    s += "]";
                    Console.Write($"s={s};done");
                }
            }
            """;
        var expected = "s=[x|];done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Null-coalescing assignment (??=)

    [Fact]
    public void CoalesceAssign_ReferenceTarget_LeftNonNull_RightNotEvaluated()
    {
        // `x ??= await? t;` — if x is already non-null, the RHS (including `await?`) must
        // not be evaluated. Use a throwing awaitable as RHS to prove it.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter()
                    => throw new InvalidOperationException("RHS must not be evaluated when LHS is non-null");
            }
            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public string GetResult() => null!;
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    string x = "present";
                    ThrowingAwaitable thrower = new();
                    x ??= await? thrower;
                    Console.Write($"x={x};done");
                }
            }
            """;
        var expected = "x=present;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void CoalesceAssign_ReferenceTarget_LeftNull_RightEvaluated()
    {
        // `x ??= await? t;` — if x is null, the RHS is evaluated. Covers both the
        // non-null-task case (x takes GetResult's value) and the null-task case (x stays
        // null because the short-circuit produced null).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    string x1 = null;
                    x1 ??= await? Task.FromResult("fallback");
                    Console.Write($"x1={x1};");

                    string x2 = null;
                    Task<string> nullTask = null;
                    x2 ??= await? nullTask;
                    Console.Write($"x2={x2 ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "x1=fallback;x2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void CoalesceAssign_NullableIntTarget_AwaitQuestionRhs()
    {
        // `int? x ??= await? t;` — target is Nullable<int>, `await?` produces int?.
        // Covers both branches and the null-task case.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    int? x1 = 5;
                    x1 ??= await? Task.FromResult(9);
                    Console.Write($"x1={x1};");

                    int? x2 = null;
                    x2 ??= await? Task.FromResult(9);
                    Console.Write($"x2={x2};");

                    int? x3 = null;
                    Task<int> nullTask = null;
                    x3 ??= await? nullTask;
                    Console.Write($"x3={x3?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "x1=5;x2=9;x3=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Double-await? in null-coalesce / ternary

    [Fact]
    public void NullCoalesce_BothSidesAreAwaitQuestion_LeftNonNull_RightNotEvaluated()
    {
        // `(await? t1) ?? (await? t2)` — if t1 is non-null and GetResult returns non-null,
        // the RHS (including t2's `await?`) must not be evaluated. Throwing awaitable on
        // the right proves it.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter()
                    => throw new InvalidOperationException("right operand must not evaluate");
            }
            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public string GetResult() => null!;
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    Task<string> t1 = Task.FromResult("left");
                    ThrowingAwaitable right = new();
                    string v = (await? t1) ?? (await? right);
                    Console.Write($"v={v};done");
                }
            }
            """;
        var expected = "v=left;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void NullCoalesce_BothSidesAreAwaitQuestion_LeftNull_RightEvaluated()
    {
        // Same shape; both sides run when the left short-circuits to null.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t1 = null;
                    Task<string> t2 = Task.FromResult("right");
                    string v = (await? t1) ?? (await? t2);
                    Console.Write($"v={v};done");
                }
            }
            """;
        var expected = "v=right;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void NullCoalesce_BothSidesAreAwaitQuestion_BothNull_ResultNull()
    {
        // Both short-circuit → result is null.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t1 = null;
                    Task<string> t2 = null;
                    string v = (await? t1) ?? (await? t2);
                    Console.Write($"v={v ?? "null"};done");
                }
            }
            """;
        var expected = "v=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void DeepChain_AwaitQuestion_OnMultiLevelNullConditional()
    {
        // `await? a?.b?.c?.GetTask()` — the operand of `await?` is a 3-link null-conditional
        // chain. When any link in the chain short-circuits OR when `await?` itself sees a
        // null result from the chain, the whole expression yields null.
        var source = """
            using System;
            using System.Threading.Tasks;

            class Node { public Node Next; public Task<int> GetTask() => Task.FromResult(42); }

            class C
            {
                public static async Task Main()
                {
                    Node a = new() { Next = new Node { Next = new Node() } };
                    int? v1 = await? a?.Next?.Next?.GetTask();
                    Console.Write($"full-chain:v1={v1};");

                    // Inner link null: a.Next.Next.Next.GetTask() — but a.Next.Next is the
                    // third node whose Next is null, so the chain short-circuits at that link.
                    int? v2 = await? a?.Next?.Next?.Next?.GetTask();
                    Console.Write($"inner-null:v2={v2?.ToString() ?? "null"};");

                    // Top-level null: whole chain short-circuits.
                    Node aNull = null;
                    int? v3 = await? aNull?.Next?.Next?.GetTask();
                    Console.Write($"top-null:v3={v3?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "full-chain:v1=42;inner-null:v2=null;top-null:v3=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Interpolated strings

    [Fact]
    public void InterpolatedString_AwaitQuestionAsHole_ReferenceResult()
    {
        // `$"..{await? t}.."` where the hole yields a lifted string (reference result).
        // DefaultInterpolatedStringHandler receives the possibly-null string.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t1 = Task.FromResult("hello");
                    string s1 = $"[{await? t1}]";
                    Console.Write($"s1={s1};");

                    Task<string> t2 = null;
                    string s2 = $"[{await? t2}]";
                    Console.Write($"s2={s2};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "s1=[hello];s2=[];done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void InterpolatedString_AwaitQuestionAsHole_LiftedIntResult()
    {
        // Hole is int? from `await? Task<int>`. Interpolation formats Nullable<int>
        // directly (prints nothing when HasValue is false, the int value otherwise).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(42);
                    string s1 = $"[{await? t1}]";
                    Console.Write($"s1={s1};");

                    Task<int> t2 = null;
                    string s2 = $"[{await? t2}]";
                    Console.Write($"s2={s2};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "s1=[42];s2=[];done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void InterpolatedString_MultipleAwaitQuestions()
    {
        // Multiple holes each `await? t` — pin spill ordering across the interpolation
        // handler's AppendFormatted calls. Left hole evaluates first.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(1);
                    Task<int> t2 = null;
                    Task<int> t3 = Task.FromResult(3);
                    string s = $"[{await? t1},{await? t2},{await? t3}]";
                    Console.Write($"s={s};done");
                }
            }
            """;
        var expected = "s=[1,,3];done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Ref, stackalloc, collection-expression interactions

    [Fact]
    public void RefReturnReceiver_WithAwaitQuestionArgument()
    {
        // `GetReceiver(ref x)?.M(await? t)` — the receiver is obtained via a ref-return
        // that mutates `x`. The await? spills across the invocation; `x` must be
        // captured at the right point so that `x`'s post-await observed value is the
        // value at the call site, not something lost to a hoist bug.
        var source = """
            using System;
            using System.Threading.Tasks;

            class Holder
            {
                public int V;
                public string M(int? awaited, int x) => $"M(v={V},awaited={awaited},x={x})";
            }

            class C
            {
                static Holder s_instance = new() { V = 100 };

                static ref Holder GetReceiver(ref int x)
                {
                    x = 42;
                    return ref s_instance;
                }

                public static async Task Main()
                {
                    int x = 0;
                    Task<int> t = Task.FromResult(7);
                    string r = GetReceiver(ref x)?.M(await? t, x) ?? "null-receiver";
                    Console.Write($"{r};x-after={x};done");
                }
            }
            """;
        var expected = "M(v=100,awaited=7,x=42);x-after=42;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void StackAlloc_LengthFromAwaitQuestion()
    {
        // `stackalloc int[...]` length computed via `await? t`. The stackalloc itself
        // happens AFTER the await (Span<int> is a ref struct and can't survive an await
        // boundary), but the length feeds from the lifted await? result. Delegates the
        // post-await work to a sync helper to keep Main straightforward.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(3);
                    int? len1 = await? t1;
                    Console.Write($"arr1-sum={SumRange(len1.GetValueOrDefault())};");

                    Task<int> t2 = null;
                    int? len2 = await? t2;
                    Console.Write($"arr2-sum={SumRange(len2.GetValueOrDefault())};");

                    Console.Write("done");
                }

                static int SumRange(int n)
                {
                    Span<int> sp = stackalloc int[n];
                    for (int i = 0; i < n; i++) sp[i] = i;
                    int sum = 0;
                    foreach (int x in sp) sum += x;
                    return sum;
                }
            }
            """;
        var expected = "arr1-sum=3;arr2-sum=0;done";
        // Stackalloc doesn't round-trip through ILVerify, so skip that step. Execution
        // is still asserted in both modes.
        CompileAndVerify(
            source,
            parseOptions: s_preview,
            targetFramework: TargetFramework.NetCoreApp,
            options: TestOptions.ReleaseExe,
            expectedOutput: expected,
            verify: Verification.Skipped);
        var raComp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
        var raVerifier = CompileAndVerify(
            raComp,
            expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
            verify: Verification.Skipped);
        raVerifier.VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_WithAwaitQuestionElement()
    {
        // Collection expression with `await? t` as an element. Target type is `int?[]`
        // so each element is int?. Tests that spilling works inside the collection
        // literal's element list.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(10);
                    int?[] arr1 = [await? t1, 20, 30];
                    Console.Write($"arr1=[{arr1[0]},{arr1[1]},{arr1[2]}];");

                    Task<int> t2 = null;
                    int?[] arr2 = [await? t2, 20, 30];
                    Console.Write($"arr2=[{arr2[0]?.ToString() ?? "null"},{arr2[1]},{arr2[2]}];");

                    Console.Write("done");
                }
            }
            """;
        var expected = "arr1=[10,20,30];arr2=[null,20,30];done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion
}
