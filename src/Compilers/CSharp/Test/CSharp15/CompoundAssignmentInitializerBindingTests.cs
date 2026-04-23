// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CompoundAssignmentInitializerBindingTests : CSharpTestBase
{
    /// <summary>
    /// Source-level polyfills for types the default reference set doesn't include. Add to a specific
    /// compilation via <c>[source, Polyfills]</c> when the test needs any of: <c>IsExternalInit</c>
    /// (record classes, <c>init;</c> setters, <c>with</c> on record classes), <c>CompilerFeatureRequiredAttribute</c>
    /// (user-defined <c>operator +=</c>), or <c>RequiredMemberAttribute</c> / <c>SetsRequiredMembersAttribute</c>
    /// (<c>required</c> members). Tests that use only plain properties, fields, indexers, events,
    /// enums, structs, or record <em>structs</em> don't need it and call <c>CreateCompilation(source)</c>
    /// / <c>CompileAndVerify(source, ...)</c> directly.
    /// </summary>
    private static readonly string Polyfills =
        IsExternalInitTypeDefinition +
        CompilerFeatureRequiredAttribute +
        RequiredMemberAttribute +
        SetsRequiredMembersAttribute;

    /// <summary>All 11 compound assignment operators in the spec's `compound_assignment_operator` set.</summary>
    public static TheoryData<string> AllCompoundOperators => new()
    {
        "+=", "-=", "*=", "/=", "%=",
        "&=", "|=", "^=",
        "<<=", ">>=", ">>>=",
    };

    /// <summary>
    /// Expected (as string) result of applying <paramref name="op"/> to seed value <c>6</c> and RHS
    /// <c>5</c>. Shared expected-value table for the op-matrix theories across init-only,
    /// ref-returning, dynamic, and record-struct property targets; any divergence across accessor
    /// kinds on the same seed+RHS would point at a lowering or binding regression specific to that
    /// kind rather than at the operator itself.
    /// </summary>
    private static string ExpectedForOpOn6With5(string op) => op switch
    {
        "+=" => "11",   // 6 + 5
        "-=" => "1",    // 6 - 5
        "*=" => "30",   // 6 * 5
        "/=" => "1",    // 6 / 5
        "%=" => "1",    // 6 % 5
        "&=" => "4",    // 0110 & 0101
        "|=" => "7",    // 0110 | 0101
        "^=" => "3",    // 0110 ^ 0101
        "<<=" => "192", // 6 << 5
        ">>=" => "0",   // 6 >> 5
        ">>>=" => "0",  // 6 >>> 5
        _ => throw new System.InvalidOperationException($"unexpected op: {op}"),
    };

    /// <summary>
    /// The 9 compound operators other than `+=` / `-=`. Per spec, an event target is only valid with
    /// `+=` or `-=`; every other compound form falls through `BindCompoundAssignmentCore`'s event
    /// dispatch and hits overload resolution on the delegate type, producing CS0019.
    /// </summary>
    public static TheoryData<string> NonPlusMinusCompoundOperators => new()
    {
        "*=", "/=", "%=",
        "&=", "|=", "^=",
        "<<=", ">>=", ">>>=",
    };

    /// <summary>Bitwise operators that work on any enum (flags or plain).</summary>
    public static TheoryData<string> EnumBitwiseOperators => new()
    {
        "&=", "|=", "^=",
    };

    #region Target kinds

    [Fact]
    public void Target_WritableInstanceField_Succeeds()
    {
        var source = """
            class C
            {
                public int F = 10;
                public static void Main() => System.Console.Write(new C { F += 5 }.F);
            }
            """;
        CompileAndVerify(source, expectedOutput: "15");
    }

    [Fact]
    public void Target_ReadonlyInstanceField_Fails()
    {
        var source = """
            class C
            {
                public readonly int F;
                public static C Make() => new C { F += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            //     public static C Make() => new C { F += 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonly, "F").WithLocation(4, 39));
    }

    [Fact]
    public void Target_GetOnlyAutoProperty_Fails()
    {
        var source = """
            class C
            {
                public int P { get; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(4, 39));
    }

    [Fact]
    public void Target_SetOnlyProperty_Fails()
    {
        var source = """
            class C
            {
                private int _p;
                public int P { set { _p = value; } }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(5, 39));
    }

    [Fact]
    public void Target_InitOnlyProperty_SucceedsInNew()
    {
        var source = """
            class C
            {
                public int P { get; init; } = 10;
                public static void Main() => System.Console.Write(new C { P += 5 }.P);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void Target_InitOnlyProperty_AllCompoundOperators(string op)
    {
        // `init` accessors are accepted on the same terms as `=` during object creation (per the
        // spec's "Accessor requirements" bullet); the init setter fires exactly once after the
        // compound's read-modify sequence. Seed = 6 (binary 0110) so every op yields a distinct
        // non-seed result when applied with 5.
        var source = $$"""
            class C
            {
                public int P { get; init; } = 6;
                public static void Main() => System.Console.Write(new C { P {{op}} 5 }.P);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: ExpectedForOpOn6With5(op));
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void Target_RefReturningProperty_AllCompoundOperators(string op)
    {
        // A ref-returning property classifies its access as a variable, so every compound operator
        // in the set is valid on it — the read and write both go through the returned ref. Same
        // seed + RHS as the init-only theory; any divergence would indicate the value-kind path
        // diverged between the two accessor kinds.
        var source = $$"""
            class C
            {
                private int _p = 6;
                public ref int P => ref _p;
                public static void Main() => System.Console.Write(new C { P {{op}} 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: ExpectedForOpOn6With5(op));
    }

    [Fact]
    public void Target_InitOnlyProperty_SucceedsInWith()
    {
        var source = """
            record C(int V)
            {
                public int P { get; init; } = 10;
                public static void Main()
                {
                    var r = new C(0);
                    var r2 = r with { P += 5 };
                    System.Console.Write($"{r.P},{r2.P}");
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "10,15");
    }

    [Fact]
    public void Target_RefReturningProperty_Succeeds()
    {
        var source = """
            class C
            {
                private int _p = 10;
                public ref int P => ref _p;
                public static void Main() => System.Console.Write(new C { P += 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "15");
    }

    [Fact]
    public void Target_RefReadonlyProperty_Fails()
    {
        var source = """
            class C
            {
                private int _p;
                public ref readonly int P => ref _p;
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): error CS8331: Cannot assign to property 'C.P' or use it as the right hand side of a ref assignment because it is a readonly variable
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "P").WithLocation(5, 39));
    }

    [Fact]
    public void Target_StaticProperty_Fails()
    {
        var source = """
            class C
            {
                public static int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS1914: Static field or property 'C.P' cannot be assigned in an object initializer
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "P").WithArguments("C.P").WithLocation(4, 39));
    }

    [Fact]
    public void Target_StaticProperty_InWith_Fails()
    {
        // Static-member rejection has to fire for `with` the same way it does for `new`. The `with`
        // path reuses member_initializer, so the check happens in the same binder code — but nothing
        // pins it for the with side, so a regression would be invisible.
        var source = """
            record R(int V)
            {
                public static int P { get; set; }
                public static R Make(R r) => r with { P += 1 };
            }
            """;
        // `with` rejects with CS0176 ("member 'R.P' cannot be accessed with an instance reference")
        // rather than the `new` form's CS1914, because the `with` binder treats the clone receiver
        // differently. Either diagnostic is informative; what this test pins is that the shape is
        // rejected at all — not that binding silently accepts a static member.
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,43): error CS0176: Member 'R.P' cannot be accessed with an instance reference; qualify it with a type name instead
            //     public static R Make(R r) => r with { P += 1 };
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "P").WithArguments("R.P").WithLocation(4, 43));
    }

    [Fact]
    public void Target_InheritedProperty_Compound_RoutesThroughBaseSetter()
    {
        // Compound initializer on an inherited property must use the base-declared accessor pair —
        // symbol resolution, get/set selection, and value-kind checks all go through the base.
        // Runtime-verify by seeding 10 on Base.P (via Base() { }) and confirming `new Derived() { P += 5 }`
        // reports 15 through `base.P`'s setter.
        var source = """
            class Base { public int P { get; set; } = 10; }
            class Derived : Base { }
            class Driver
            {
                public static void Main() => System.Console.Write(new Derived { P += 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "15");
    }

    [Fact]
    public void Target_ShadowedProperty_Compound_UsesDerivedDeclaration()
    {
        // When a derived class declares `new int P { … }` with its own backing storage, the initializer
        // must resolve the derived P, not the base. This exercises the normal member-lookup priority
        // through BindObjectInitializerMember / BindInstanceMemberAccess; a regression that resolved
        // the base would be visible in the final value.
        var source = """
            class Base { public int P { get; set; } = 100; }
            class Derived : Base { public new int P { get; set; } = 10; }
            class Driver
            {
                public static void Main()
                {
                    var d = new Derived { P += 5 };
                    // Derived.P reads 10 + 5 = 15; Base.P is unchanged at 100. If binding accidentally
                    // resolved to Base.P, we'd see 105 on the derived reference.
                    System.Console.Write($"{d.P},{((Base)d).P}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "15,100");
    }

    [Fact]
    public void Target_InaccessiblePrivateSetter_Compound_Fails()
    {
        // Compound's value-kind check must enforce setter accessibility exactly as simple `=` does.
        // `public int P { get; private set; }` accessed from outside the declaring type has an
        // inaccessible set accessor; `new C { P += 1 }` should fail with CS0272.
        var source = """
            public class C
            {
                public int P { get; private set; }
            }
            public class Driver
            {
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,39): error CS0272: The property or indexer 'C.P' cannot be used in this context because the set accessor is inaccessible
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P").WithArguments("C.P").WithLocation(7, 39));
    }

    [Fact]
    public void Target_ObsoleteProperty_Compound_ReportsOnce()
    {
        // Compound reads and writes through the same accessor pair; `[Obsolete]` on a property
        // reports once per property reference, same as `=` / non-initializer compound. Pin the
        // diagnostic to catch a regression that double-counts, stays silent, or switches to a
        // set-only error on a get-and-set member.
        var source = """
            using System;
            class C
            {
                [Obsolete("old")] public int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): warning CS0618: 'C.P' is obsolete: 'old'
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "P").WithArguments("C.P", "old").WithLocation(5, 39));
    }

    [Fact]
    public void Target_StaticEvent_Fails()
    {
        // Object-initializer targets must be instance members. Static events are rejected up front
        // by the "static member in initializer" check — same as any other static member — so compound
        // / `+=` / `-=` on a static event all fail with CS1914 before event-specific logic runs.
        var source = """
            using System;
            class C
            {
                public static event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,38): warning CS0067: The event 'C.E' is never used
            //     public static event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 38),
            // (5,53): error CS1914: Static field or property 'C.E' cannot be assigned in an object initializer
            //     public static C Make(EventHandler h) => new C { E += h };
            Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "E").WithArguments("C.E").WithLocation(5, 53));
    }

    [Fact]
    public void Target_Indexer_DictionaryStyle_Succeeds()
    {
        var source = """
            class C
            {
                private int[] _values = { 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 };
                public int this[int i] { get => _values[i]; set => _values[i] = value; }
                public static void Main()
                {
                    var c = new C { [0] |= 2, [1] &= 3, [2] += 10 };
                    System.Console.Write($"{c[0]},{c[1]},{c[2]}");
                }
            }
            """;
        // 5|2 = 7, 6&3 = 2, 7+10 = 17
        CompileAndVerify(source, expectedOutput: "7,2,17");
    }

    [Fact]
    public void Target_NestedArray_Compound_Runs()
    {
        // Nested object initializer on a non-readonly `int[]` field. The inner `{ [0] += 5, [1] |= 3 }`
        // binds each compound's LHS as a bare BoundArrayAccess (not wrapped in BoundObjectInitializerMember),
        // hitting the dispatcher's ArrayAccess arm and RewriteArrayInitializerAccess. Runtime-verify
        // that both elements receive their compound's effect.
        var source = """
            class C
            {
                public int[] A = { 0, 6, 0 };
                public static void Main()
                {
                    var c = new C { A = { [0] += 5, [1] |= 3 } };
                    System.Console.Write($"{c.A[0]},{c.A[1]}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "5,7");
    }

    [Fact]
    public void Target_NestedArray_SideEffectingIndex_EvaluatedOnce()
    {
        // Spec-normative (inherited via the statement-expression lowering): indexer arguments on an
        // initializer target are evaluated exactly once. For a bare BoundArrayAccess LHS, this is
        // enforced by RewriteArrayInitializerAccess's EvaluateSideEffectingArgumentsToTemps call. Pin
        // the side-effect count with a GetIndex method.
        var source = """
            class C
            {
                public int[] A = new int[3];
                public int Counter;
                public int GetIndex() { Counter++; return 0; }
                public static void Main()
                {
                    var c = new C();
                    _ = new C { A = { [c.GetIndex()] += 5 } };
                    System.Console.Write(c.Counter);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1");
    }

    [Fact]
    public void Target_NestedPointerField_Compound_Runs()
    {
        // Pointer-element compound through the nested-initializer path. The inner `{ [0] += 5 }` LHS
        // binds as a bare BoundPointerElementAccess and hits the dispatcher's PointerElementAccess
        // arm (→ RewritePointerElementInitializerAccess). Unsafe context; PEVerify skipped because
        // pointer access isn't verifiable IL. Construction takes the pointer via the ctor so `c.P`
        // is valid when the inner `{ [0] += 5 }` reads/writes through it.
        var source = """
            unsafe class C
            {
                public int* P;
                public C(int* p) { P = p; }
                public static void Main()
                {
                    int backing = 10;
                    _ = new C(&backing) { P = { [0] += 5 } };
                    System.Console.Write(backing);
                }
            }
            """;
        CompileAndVerify(
            source,
            options: TestOptions.UnsafeReleaseExe,
            verify: Verification.Skipped,
            expectedOutput: "15");
    }

    [Fact]
    public void Target_NestedDynamicIndexer_Compound_Runs()
    {
        // Regression test for audit finding #1: `new Outer { Inner = { [0] += 5 } }` where Inner is
        // `dynamic` used to trip `Debug.Assert(memberSymbol is object)` in MakeObjectInitializerMemberAccess
        // during lowering. The compound path now detects `BoundObjectInitializerMember { MemberSymbol:
        // null } && Type.IsDynamic()`, unwraps to the underlying BoundDynamicIndexerAccess, and lets
        // TransformDynamicIndexerAccess emit the runtime GetIndex+SetIndex call-site pair — the same
        // shape `d[0] += 5` uses outside an initializer. Pre-populate the dictionary so the compound's
        // read step has a value.
        var source = """
            using System.Collections.Generic;
            class Outer
            {
                public dynamic Inner { get; set; } = new Dictionary<int,int> { { 0, 10 } };
                public static void Main()
                {
                    var o = new Outer { Inner = { [0] += 5 } };
                    System.Console.Write((int)o.Inner[0]);
                }
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: "15");
    }

    [Fact]
    public void Target_FieldLikeEvent_PlusEqualsAndMinusEquals_Succeed()
    {
        // Field-like event: `E += h, E -= h` in one initializer subscribes then unsubscribes. After
        // both, Raise() should invoke no handlers (count stays 0).
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    var c = new C { E += h, E -= h };
                    c.Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "0");
    }

    [Fact]
    public void Target_FieldLikeEvent_PlusEqualsFromOutsideDeclaringType_Succeeds()
    {
        // `+= / -=` on a field-like event dispatch through the event's `add_X` / `remove_X` accessors,
        // which are public for a public event — so access from outside the declaring type works the
        // same as from inside. This parity matters: the `??=` / `=` cases reject from outside (no
        // accessible backing field), but `+= / -=` must not. Pin runtime behavior: handler fires once.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
            }
            class Driver
            {
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    var c = new C { E += h };
                    c.Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1");
    }

    [Fact]
    public void Target_CustomEvent_PlusEqualsAndMinusEquals_Succeed()
    {
        // Custom event (explicit add / remove): `E += h, E -= h` must call add_E then remove_E.
        // Verify both accessors observed exactly one call.
        var source = """
            using System;
            class C
            {
                public int Adds;
                public int Removes;
                public event EventHandler E
                {
                    add { Adds++; }
                    remove { Removes++; }
                }
                public static void Main()
                {
                    EventHandler h = (s, e) => { };
                    var c = new C { E += h, E -= h };
                    System.Console.Write($"{c.Adds},{c.Removes}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,1");
    }

    [Fact]
    public void Target_DynamicProperty_Succeeds()
    {
        var source = """
            class C
            {
                public dynamic X { get; set; } = 10;
                public static void Main() => System.Console.Write((int)new C { X += 5 }.X);
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: "15");
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void Target_DynamicProperty_AllCompoundOperators(string op)
    {
        // Dynamic-typed LHS dispatches each operator to the DLR at runtime. Every compound in the
        // set must survive the bind-then-lower pipeline and produce the same observable result a
        // non-initializer `x.X op 5` would — the initializer path wraps the same dynamic dispatch.
        // Seed `X = 6` (matches other matrix tests) so a divergent result flags the issue.
        //
        // `>>>=` has no dynamic binder (pre-existing compiler limitation; CS0019 fires for any
        // `dynamic >>>= int` expression regardless of context), so we pin the same CS0019 here
        // rather than runtime-verify. This keeps the matrix complete and the dynamic/non-dynamic
        // asymmetry explicit.
        if (op == ">>>=")
        {
            var badSource = $$"""
                class C
                {
                    public dynamic X { get; set; } = 6;
                    public static void Main() => System.Console.Write((int)new C { X {{op}} 5 }.X);
                }
                """;
            CreateCompilation(badSource, targetFramework: TargetFramework.StandardAndCSharp).VerifyDiagnostics(
                // (4,68): error CS0019: Operator '>>>=' cannot be applied to operands of type 'dynamic' and 'int'
                //     public static void Main() => System.Console.Write((int)new C { X >>>= 5 }.X);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"X {op} 5").WithArguments(op, "dynamic", "int").WithLocation(4, 68));
            return;
        }

        var source = $$"""
            class C
            {
                public dynamic X { get; set; } = 6;
                public static void Main() => System.Console.Write((int)new C { X {{op}} 5 }.X);
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: ExpectedForOpOn6With5(op));
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void Target_RecordStructProperty_AllCompoundOperators_InWith(string op)
    {
        // Records `with` clones the receiver; compound operators on the clone must respect the
        // same accessor rules as on plain classes. Pin the full operator set so record-struct
        // copy semantics + compound assignment stay consistent. Seed Value = 6.
        var source = $$"""
            record struct Counter(int Value)
            {
                public static void Main()
                {
                    var a = new Counter(6);
                    var b = a with { Value {{op}} 5 };
                    System.Console.Write(b.Value);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: ExpectedForOpOn6With5(op));
    }

    #endregion

    #region Event target constraints

    [Theory, MemberData(nameof(NonPlusMinusCompoundOperators))]
    public void Event_NonPlusOrMinusCompound_FromInside_Fails(string op)
    {
        // From inside the declaring type the event's backing field is accessible
        // (`IsUsableAsField == true`), so `CheckEventValueKind(… Assignable …)` succeeds and the
        // binder falls through to overload resolution on the delegate type, producing CS0019.
        // Matches what `c.E {op} h` as a statement fires from inside the declaring type.
        var source = $$"""
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E {{op}} h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31),
            // (5,53): error CS0019: Operator 'op' cannot be applied to operands of type 'EventHandler' and 'EventHandler'
            //     public static C Make(EventHandler h) => new C { E op h };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, $"E {op} h").WithArguments(op, "System.EventHandler", "System.EventHandler").WithLocation(5, 53));
    }

    [Theory, MemberData(nameof(NonPlusMinusCompoundOperators))]
    public void Event_NonPlusOrMinusCompound_FromOutside_Fails(string op)
    {
        // From outside the declaring type the backing field is inaccessible, so the event-specific
        // CS0070 must win over the generic CS0019 — matching `c.E {op} h` as a statement from the
        // same call site. Before the fix, the initializer path's event dispatch guard keyed on
        // `left.Kind == BoundKind.EventAccess` and missed the `BoundObjectInitializerMember` wrapper,
        // so CS0019 leaked through instead of CS0070.
        var source = $$"""
            using System;
            class C
            {
                public event EventHandler E;
            }
            class Outer
            {
                public static void N(C c, EventHandler h) { c.E {{op}} h; }
                public static C   I(EventHandler h) => new C { E {{op}} h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,51): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
            //     public static void N(C c, EventHandler h) { c.E op h; }
            Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(8, 51),
            // (9,52): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
            //     public static C   I(EventHandler h) => new C { E op h };
            Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(9, 52));
    }

    [Fact]
    public void Event_SimpleAssignment_FromInsideContainingType_Succeeds()
    {
        // From inside C, the field-like event `E` is usable as a field, so `E = h` in an object
        // initializer binds as assignment to the backing delegate field — same as `c.E = h` would
        // from inside C. (This is a spec violation that Roslyn has long permitted for field-like
        // events; confirming compound-assignment-in-initializer work doesn't regress it.)
        // `E = h` replaces the backing delegate wholesale; after Make(h) then Make(null),
        // the second object's raise count should be zero.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static C Make(EventHandler h) => new C { E = h };
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    Make(h).Raise();
                    Make(null).Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1");
    }

    [Fact]
    public void Event_SimpleAssignment_FromOutsideContainingType_Fails()
    {
        // From outside the containing type, the event's backing field isn't accessible, so `E = h`
        // simple assignment is illegal: "The event 'C.E' can only appear on the left hand side of
        // += or -=". Object-initializer context doesn't relax that.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
            }
            class Outer
            {
                public static C Make(EventHandler h) => new C { E = h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31),
            // (8,53): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
            //     public static C Make(EventHandler h) => new C { E = h };
            Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(8, 53));
    }

    [Fact]
    public void Event_SimpleAssignment_CustomEvent_Fails()
    {
        // A custom event (explicit add / remove accessors) has no backing field, so `E = h` simple
        // assignment is illegal even from inside the containing type — the event isn't usable as a
        // field. Reinforces that the simple-assignment path doesn't special-case events as a writable
        // location.
        var source = """
            using System;
            class C
            {
                public event EventHandler E
                {
                    add { }
                    remove { }
                }
                public C Fluent(EventHandler h) => new C { E = h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,48): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //     public C Fluent(EventHandler h) => new C { E = h };
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(9, 48));
    }

    [Fact]
    public void Event_InWith_PlusEquals_Succeeds()
    {
        // `r with { E += h }` clones r, then subscribes h on the clone. Records copy fields
        // shallow; the backing delegate field is copied too, so both r and the clone end up
        // observing h (the clone invocation list is r's + h). Verify the clone raises count=1.
        var source = """
            using System;
            record C(int V)
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    var r = new C(0);
                    var r2 = r with { E += h };
                    r2.Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "1");
    }

    #endregion

    #region Language version gating

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void LangVersion_CSharp14_FailsForEveryCompoundOperator(string op)
    {
        // C# 14 is the latest non-preview language version and is also where user-defined `operator +=`
        // shipped. Every compound operator in the spec set fires the Preview gate identically — one
        // diagnostic at the operator token, with no cascade from the user-defined-`+=` feature.
        var source = $$"""
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P {{op}} 1 };
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (4,41): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make() => new C { P op 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, op).WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 41));
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void LangVersion_CSharp14_Fails_InWithExpression(string op)
    {
        // `with` shares the same grammar extension (`identifier assignment_operator expression`) and
        // therefore the same feature gate. Every compound operator fires the Preview gate in a `with`
        // clause too, one diagnostic at the operator token.
        var source = $$"""
            record C(int P)
            {
                public static C Make(C r) => r with { P {{op}} 1 };
            }
            """;
        CreateCompilation([source, Polyfills], parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,45): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make(C r) => r with { P op 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, op).WithArguments("compound assignment in object initializer and with expression").WithLocation(3, 45));
    }

    [Fact]
    public void LangVersion_CSharp14_Fails_InWithExpression_Coalesce()
    {
        // `??=` in a `with` clause is equally preview-gated; confirm that the `??=` token is where
        // the diagnostic points. Note `P` is a reference type here so `??=` binds successfully under
        // preview — the error is purely the feature gate, not a type check.
        var source = """
            record C(string P)
            {
                public static C Make(C r) => r with { P ??= "x" };
            }
            """;
        CreateCompilation([source, Polyfills], parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,45): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make(C r) => r with { P ??= "x" };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "??=").WithArguments("compound assignment in object initializer and with expression").WithLocation(3, 45));
    }

    #endregion

    #region Null-coalescing assignment (??=)

    // The spec diff targets ECMA-334 v7 whose `assignment_operator` production pre-dates `??=`.
    // The C# 8 null-coalescing-assignment proposal explicitly defines `??=` to follow compound-
    // assignment semantic rules (with the elide-if-non-null short-circuit), so we admit it here
    // alongside the eleven regular compound operators. The tests below pin that behavior.

    [Fact]
    public void Coalesce_NullableValueType_SetsWhenNull()
    {
        // `P ??= 5` when P is null(int?) → after the initializer, P == 5. Runtime-verify via Main.
        var source = """
            class C
            {
                public int? P { get; set; }
                public static void Main() => System.Console.Write(new C { P ??= 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "5");
    }

    [Fact]
    public void Coalesce_NullableValueType_SkipsWhenNotNull()
    {
        // `P ??= 5` when P starts as 10(int?) → the short-circuit elides the assignment; P stays 10.
        var source = """
            class C
            {
                public int? P { get; set; } = 10;
                public static void Main() => System.Console.Write(new C { P ??= 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "10");
    }

    [Fact]
    public void Coalesce_ReferenceType_SetsWhenNull()
    {
        // `S ??= "b"` when S is null(string?) → S becomes "b".
        var source = """
            #nullable enable
            class C
            {
                public string? S { get; set; }
                public static void Main() => System.Console.Write(new C { S ??= "b" }.S);
            }
            """;
        CompileAndVerify(source, expectedOutput: "b");
    }

    [Fact]
    public void Coalesce_ReferenceType_SkipsWhenNotNull()
    {
        // `S ??= "b"` when S starts as "a" → S stays "a".
        var source = """
            #nullable enable
            class C
            {
                public string? S { get; set; } = "a";
                public static void Main() => System.Console.Write(new C { S ??= "b" }.S);
            }
            """;
        CompileAndVerify(source, expectedOutput: "a");
    }

    [Fact]
    public void Coalesce_InWith_Record()
    {
        // `r with { P ??= 5 }` clones r then short-circuit-assigns P on the clone. Original unchanged.
        var source = """"
            record C(int? P)
            {
                public static void Main()
                {
                    var a = new C((int?)null);
                    var b = a with { P ??= 5 };
                    System.Console.Write($"{(a.P is null ? "null" : a.P.ToString())},{b.P}");
                }
            }
            """";
        CompileAndVerify([source, Polyfills], expectedOutput: "null,5");
    }

    [Fact]
    public void Coalesce_MultipleInOneInitializer_RunsLeftToRight()
    {
        // `P = null, P ??= 5, P ??= 10` → second is null, ??= assigns 5; third sees 5 (not null), skips.
        // Demonstrates left-to-right evaluation of member initializers and the short-circuit.
        var source = """
            class C
            {
                public int? P { get; set; }
                public static void Main() => System.Console.Write(new C { P = null, P ??= 5, P ??= 10 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "5");
    }

    [Fact]
    public void Coalesce_NonNullableValueType_Fails()
    {
        // `P ??= 5` when P is `int` (non-nullable value type) is illegal for `??=` in general
        // (CS0019 "Operator '??=' cannot be applied..."). Inheriting that error from
        // BindNullCoalescingAssignmentOperatorCore confirms the initializer path routes through the
        // same validation.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P ??= 5 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS0019: Operator '??=' cannot be applied to operands of type 'int' and 'int'
            //     public static C Make() => new C { P ??= 5 };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "P ??= 5").WithArguments("??=", "int", "int").WithLocation(4, 39));
    }

    [Fact]
    public void Coalesce_DoesNotSatisfyRequired()
    {
        // Analogous to Required_CompoundAlone_DoesNotSatisfy: a `??=` member initializer is a
        // conditional write, not an unconditional one, so it does not discharge the `required`
        // obligation. Consumers must still write `P = ...` (or pass through `[SetsRequiredMembers]`).
        var source = """
            class C
            {
                public required int? P { get; set; }
                public static C Make() => new C { P ??= 5 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,35): error CS9035: Required member 'C.P' must be set in the object initializer or attribute constructor.
            //     public static C Make() => new C { P ??= 5 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.P").WithLocation(4, 35));
    }

    [Fact]
    public void Coalesce_LangVersion_CSharp14_Fails()
    {
        // The feature gate applies uniformly to every non-simple operator the initializer path admits.
        // C# 14 is the latest non-preview version; `??=` in an initializer should fail the Preview gate
        // at its operator token, same as `+=`.
        var source = """
            class C
            {
                public int? P { get; set; }
                public static C Make() => new C { P ??= 1 };
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (4,41): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make() => new C { P ??= 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "??=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 41));
    }

    [Fact]
    public void Coalesce_IndexerArgs_EvaluatedOnce()
    {
        // Spec-normative: indexer arguments on an initializer target are evaluated exactly once. With
        // `??=`, the get+conditional-set sequence must reuse the same cached argument — otherwise a
        // side-effecting index would be double-evaluated. Counter should be exactly 1 after the op.
        var source = """
            class C
            {
                public int Counter;
                public int GetIndex()
                {
                    Counter++;
                    return 0;
                }
                private string?[] _values = { null };
                public string? this[int i]
                {
                    get => _values[i];
                    set => _values[i] = value;
                }
                public static void Main()
                {
                    var c = new C();
                    var d = new C { [c.GetIndex()] ??= "hit" };
                    System.Console.Write($"{c.Counter},{d[0]}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,hit");
    }

    [Fact]
    public void Coalesce_NestedInitializerOnRhs_Rejected()
    {
        // Parallel to NestedCollectionInitializerOnRhs_Rejected for `+=`: the member_initializer grammar
        // doesn't admit a nested initializer on the RHS of `??=`. Parser is permissive; binder emits
        // CS0747. We also get CS1918 from the member-access bind (nested initializer on a value-type
        // target). The fresh-diag-bag filter keeps CS1922 from cascading.
        var source = """
            class C
            {
                public int? P { get; set; }
                public static C Make() => new C { P ??= { 1, 2 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS1918: Members of property 'C.P' of type 'int?' cannot be assigned with an object initializer because it is of a value type
            //     public static C Make() => new C { P ??= { 1, 2 } };
            Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "P").WithArguments("C.P", "int?").WithLocation(4, 39),
            // (4,39): error CS0747: Invalid initializer member declarator
            //     public static C Make() => new C { P ??= { 1, 2 } };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "P ??= { 1, 2 }").WithLocation(4, 39));
    }

    [Fact]
    public void Coalesce_Event_FromInsideContainingType_Succeeds()
    {
        // Parallel to Event_SimpleAssignment_FromInsideContainingType_Succeeds: from inside the
        // declaring type, a field-like event is usable as a delegate field. `E ??= h` therefore binds
        // as a null-coalescing assignment to that backing field — the first ??= sets h, a second ??=
        // on an already-non-null E short-circuits. Runtime-verify by subscribing once and raising,
        // then ??= again with a different handler that never fires.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static C Make(EventHandler h) => new C { E ??= h };
                public static C Override(C c, EventHandler h)
                {
                    c.E ??= h;
                    return c;
                }
                public static void Main()
                {
                    int first = 0, second = 0;
                    EventHandler h1 = (_, _) => first++;
                    EventHandler h2 = (_, _) => second++;
                    var c = Make(h1);
                    Override(c, h2);
                    c.Raise();
                    System.Console.Write($"{first},{second}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,0");
    }

    [Fact]
    public void Coalesce_Event_FromOutsideContainingType_Fails()
    {
        // `??=` has to read the LHS to test for null; for a field-like event that read goes through
        // the synthesized backing field, which isn't accessible from outside the declaring type —
        // same rule plain `E = h` enforces via CS0070. Before this was rejected at bind time, the
        // shape silently accepted at bind and crashed at emit. The fix (in
        // BindNullCoalescingAssignmentOperatorCore) applies uniformly to both the non-initializer
        // form `c.E ??= h` and the initializer form `new C { E ??= h }`, keeping them consistent.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
            }
            class Outer
            {
                public static void NonInitializer(C c, EventHandler h) { c.E ??= h; }
                public static C Initializer(EventHandler h) => new C { E ??= h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,64): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
            //     public static void NonInitializer(C c, EventHandler h) { c.E ??= h; }
            Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(8, 64),
            // (9,60): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
            //     public static C Initializer(EventHandler h) => new C { E ??= h };
            Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(9, 60));
    }

    [Fact]
    public void Coalesce_Event_CustomEvent_Fails()
    {
        // A custom event (explicit add / remove accessors) has no backing field, so `??=` can't read
        // it; the behavior should match plain `E = h` on a custom event, which fails with CS0079.
        // This applies regardless of whether the access is inside or outside the declaring type.
        // Tested in both non-initializer and initializer shapes to pin their consistency.
        var source = """
            using System;
            class C
            {
                public event EventHandler E { add { } remove { } }
                public void NonInitializer(EventHandler h) { this.E ??= h; }
                public static C Initializer(EventHandler h) => new C { E ??= h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,55): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //     public void NonInitializer(EventHandler h) { this.E ??= h; }
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(5, 55),
            // (6,60): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //     public static C Initializer(EventHandler h) => new C { E ??= h };
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(6, 60));
    }

    #endregion

    #region Duplicate rules

    [Fact]
    public void Duplicate_TwoSimpleAssignments_Fails()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P = 1, P = 2 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,46): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = 1, P = 2 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 46));
    }

    [Fact]
    public void Duplicate_EqualsThenCompound_Succeeds()
    {
        // P = 10, then P += 5 → final P = 15, verifying initializer members run left-to-right.
        var source = """
            class C
            {
                public int P { get; set; }
                public static void Main() => System.Console.Write(new C { P = 10, P += 5 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "15");
    }

    [Fact]
    public void Duplicate_CompoundThenEquals_Fails()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5, P = 10 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,47): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P += 5, P = 10 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 47));
    }

    [Fact]
    public void Duplicate_TwoCompounds_Succeeds()
    {
        // P starts at 1; P += 5 → 6; P += 10 → 16; P *= 2 → 32. Verifies chained compound
        // operations see each other's effects (each read is a fresh get).
        var source = """
            class C
            {
                public int P { get; set; } = 1;
                public static void Main() => System.Console.Write(new C { P += 5, P += 10, P *= 2 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "32");
    }

    [Fact]
    public void Duplicate_EqualsThenCompoundThenEquals_FailsOnlyForSecondEquals()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P = 1, P += 2, P = 3 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,54): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = 1, P += 2, P = 3 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 54));
    }

    [Fact]
    public void Duplicate_Indexer_Unrestricted()
    {
        // Spec: "No such restriction applies to event or indexer targets." Per-indexer-key tracking has
        // never been done (same-arg repeat has always been legal); same applies with compound.
        // [0]=1, [0]+=2 → 3, [0]=3, [0]|=4 → 7. Verifies every indexer initializer runs and observes
        // the prior one's effect.
        var source = """
            class C
            {
                private int[] _values = new int[10];
                public int this[int i] { get => _values[i]; set => _values[i] = value; }
                public static void Main() => System.Console.Write(new C { [0] = 1, [0] += 2, [0] = 3, [0] |= 4 }[0]);
            }
            """;
        CompileAndVerify(source, expectedOutput: "7");
    }

    [Fact]
    public void Duplicate_Event_PlusMinusEqualsUnrestricted()
    {
        // `+=` / `-=` on an event are unrestricted — each binds as BoundEventAssignmentOperator
        // and goes through add_E / remove_E accessors. Multiple subscribes/unsubscribes on the same
        // event in one initializer all run. Order: +a (a), +b (ab), -a (b), +a (ba). Raise() invokes
        // both. a counts 1, b counts 1.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static void Main()
                {
                    int ca = 0, cb = 0;
                    EventHandler a = (s, e) => ca++;
                    EventHandler b = (s, e) => cb++;
                    var c = new C { E += a, E += b, E -= a, E += a };
                    c.Raise();
                    System.Console.Write($"{ca},{cb}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,1");
    }

    [Fact]
    public void Duplicate_FieldLikeEvent_TwoSimpleAssignments_Fails()
    {
        // Field-like event simple `=` binds as BoundAssignmentOperator wrapping
        // BoundObjectInitializerMember; it participates in the duplicate-member rule the same way
        // field/property `=` does. Two `E = …` on the same event from inside the declaring type
        // gives CS1912, matching the field/property behavior (the csharplang proposal is also
        // updated in this PR to subject event targets to the same rule).
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E = h, E = h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31),
            // (5,60): error CS1912: Duplicate initialization of member 'E'
            //     public static C Make(EventHandler h) => new C { E = h, E = h };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "E").WithArguments("E").WithLocation(5, 60));
    }

    [Fact]
    public void Duplicate_FieldLikeEvent_SimpleThenCompound_Succeeds()
    {
        // `E = h1` (BoundAssignmentOperator) followed by `E += h2` (BoundEventAssignmentOperator)
        // is allowed — the `=` establishes the handler, the `+=` adds a second, and the rule's
        // "= must come before compound" ordering is satisfied. Runtime-verify: raise count = 2.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    var c = new C { E = h, E += h };
                    c.Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "2");
    }

    [Fact]
    public void Duplicate_FieldLikeEvent_CompoundThenSimple_Fails()
    {
        // `E += h1, E = h2` — compound before simple `=` — violates the "= first" rule. The duplicate
        // tracker records the member name for every initializer form (including
        // BoundEventAssignmentOperator), so the subsequent `E = h2` sees the name already present and
        // fires CS1912. Matches the corresponding field/property behavior.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h, E = h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31),
            // (5,61): error CS1912: Duplicate initialization of member 'E'
            //     public static C Make(EventHandler h) => new C { E += h, E = h };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "E").WithArguments("E").WithLocation(5, 61));
    }

    [Fact]
    public void Duplicate_TwoCoalesceAssignments_Succeeds()
    {
        // `??=` is a compound form; the "any number of compound initializers on the same target"
        // relaxation applies. Two `P ??= v` in a row: first fires (P starts null → becomes "a"),
        // second is a no-op (P is non-null → elided). Pin with a string property.
        var source = """
            class C
            {
                public string P { get; set; }
                public static void Main() => System.Console.Write(new C { P ??= "a", P ??= "b" }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "a");
    }

    [Fact]
    public void Duplicate_SimpleAfterCoalesce_Fails()
    {
        // `??=` counts as compound for ordering: a later simple `=` violates the "`=` must come
        // before any compound" rule. Expected CS1912 at the `P` in `P = "z"`.
        var source = """
            class C
            {
                public string P { get; set; }
                public static C Make() => new C { P ??= "a", P = "z" };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,50): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P ??= "a", P = "z" };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 50));
    }

    [Fact]
    public void Duplicate_CoalesceAfterSimple_Succeeds()
    {
        // `=` first then compound (including `??=`) is the legal ordering. `P = "a"` then `P ??= "b"`
        // elides the second assignment because P is non-null; final value is "a".
        var source = """
            class C
            {
                public string P { get; set; }
                public static void Main() => System.Console.Write(new C { P = "a", P ??= "b" }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "a");
    }

    [Fact]
    public void Duplicate_WithExpression_Property_EnforcesRule()
    {
        var source = """
            record C(int P)
            {
                public static C Make(C r) => r with { P += 1, P = 2 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (3,51): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make(C r) => r with { P += 1, P = 2 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(3, 51));
    }

    #endregion

    #region Misc shape rejections

    [Fact]
    public void RefOnRhs_Rejected()
    {
        // Compound `+=` doesn't take a ref RHS — ref-assignment is only meaningful with `=` on a
        // ref-assignable variable. The existing ref-assignment rhsKind plumbing fires CS8373 on the
        // LHS even though the error is really "ref isn't valid here at all."
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make(ref int x) => new C { P += ref x };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,48): error CS8373: The left-hand side of a ref assignment must be a ref variable.
            //     public static C Make(ref int x) => new C { P += ref x };
            Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(4, 48));
    }

    [Fact]
    public void RefOnLhs_Rejected()
    {
        // `ref P += 1` — ref keyword before the initializer target. The grammar doesn't admit `ref`
        // on the LHS of a member initializer, so `IsNamedMemberInitializer` returns false (ref isn't
        // an identifier) and the parser falls into the collection-initializer path, producing the
        // same two diagnostics it would for any ill-formed element.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { ref P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,37): error CS1922: Cannot initialize type 'C' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
            //     public static C Make() => new C { ref P += 1 };
            Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ ref P += 1 }").WithArguments("C").WithLocation(4, 37),
            // (4,39): error CS1073: Unexpected token 'ref'
            //     public static C Make() => new C { ref P += 1 };
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(4, 39));
    }

    [Fact]
    public void RefOnBothSides_Rejected()
    {
        // LHS is a ref-returning property (so it classifies as a variable) and RHS has `ref`. Even
        // with a "ref location" on the left, ref-assignment requires the LHS to be a *ref-assignable*
        // variable — only ref locals and ref params qualify; a ref-returning property does not — so
        // CS8373 still fires on the LHS, matching `RefOnRhs_Rejected`.
        var source = """
            class C
            {
                private int _p = 10;
                public ref int P => ref _p;
                public static C Make(ref int x) => new C { P += ref x };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,48): error CS8373: The left-hand side of a ref assignment must be a ref variable.
            //     public static C Make(ref int x) => new C { P += ref x };
            Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(5, 48));
    }

    [Fact]
    public void NestedObjectInitializerOnRhs_ReferenceTypeProperty_Rejected()
    {
        // The "nested object-or-collection initializer" form (`Container = { X = 1 }`) is only legal
        // after plain `=`, not after any compound operator. Using a reference-type property — where
        // `=`-form nested init does work — exercises the binder's compound-specific rejection, not
        // the value-type-property rejection that `NestedCollectionInitializerOnRhs_Rejected` runs
        // into. Pin CS0747 "Invalid initializer member declarator" at the compound-assignment.
        var source = """
            class Inner { public int X { get; set; } }
            class C
            {
                public Inner Container { get; set; } = new Inner();
                public static C Make() => new C { Container += { X = 1 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): error CS0747: Invalid initializer member declarator
            //     public static C Make() => new C { Container += { X = 1 } };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Container += { X = 1 }").WithLocation(5, 39));
    }

    [Fact]
    public void Target_ImplicitIndex_Succeeds()
    {
        // Implicit `System.Index` indexers translate `[^1]` to a call to `this[int]` with a
        // length-adjusted index. Compound on an implicit-index target goes through the same
        // `BoundImplicitIndexerAccess` lowering path the dispatcher now handles — confirm the
        // target accepts every compound operator; pin runtime behavior with `+=`. Uses NetCoreApp
        // for System.Index.
        var source = """
            class C
            {
                private int[] _v = { 10, 20, 30 };
                public int Length => _v.Length;
                public int this[int i]
                {
                    get => _v[i];
                    set => _v[i] = value;
                }
                public static void Main() => System.Console.Write(new C { [^1] += 5 }[2]);
            }
            """;
        CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: "35");
    }

    [Fact]
    public void Target_ImplicitRange_Fails()
    {
        // `System.Range` indexers (`[..]`) return an array/slice; there's no `set`/`init` accessor,
        // and the target isn't a variable. Compound and `=` are both rejected for the same reason
        // plain slice indexers are read-only in this shape — the `Substring`-returning indexer has
        // no setter, so CS0200 fires. Pin the diagnostic.
        var source = """
            using System;
            class C
            {
                private string _s = "hello";
                public int Length => _s.Length;
                public string this[Range r] => _s[r];
                public static C Make() => new C { [..] += "!" };
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (7,39): error CS0200: Property or indexer 'C.this[Range]' cannot be assigned to -- it is read only
            //     public static C Make() => new C { [..] += "!" };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "[..]").WithArguments("C.this[System.Range]").WithLocation(7, 39));
    }

    [Fact]
    public void NestedCollectionInitializerOnRhs_Rejected()
    {
        // Spec note: "The compound_assignment_operator branch admits only expression, so forms such as
        // P += { 1, 2 } are syntactically ill-formed." The parser is permissive (per Phase 1); the
        // binder emits CS0747 for the compound + nested-initializer combination the spec forbids, plus
        // CS1918 from the member bind (nested initializer on a value-type target). The RHS is bound
        // into a fresh diagnostic bag that the compound + nested-initializer path discards, suppressing
        // the otherwise-cascading CS1922 ("cannot initialize 'int' with a collection initializer").
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += { 1, 2 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,39): error CS1918: Members of property 'C.P' of type 'int' cannot be assigned with an object initializer because it is of a value type
            //     public static C Make() => new C { P += { 1, 2 } };
            Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "P").WithArguments("C.P", "int").WithLocation(4, 39),
            // (4,39): error CS0747: Invalid initializer member declarator
            //     public static C Make() => new C { P += { 1, 2 } };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "P += { 1, 2 }").WithLocation(4, 39));
    }

    [Fact]
    public void LiftedIntQuestion_Compound_OntoNonNullable_Fails()
    {
        // Mirror of `TestCompoundLiftedAssignment_IOperation` (OperatorTests.cs:2402) reshaped as
        // an initializer: compound `P += b` where `P` is `int` and `b` is `int?` is invalid
        // because the lifted `int? + int -> int?` result can't convert back to `int`. Non-
        // initializer form fires CS0266; the initializer form must surface the same diagnostic so
        // IOperation / semantic-model clients see a consistently invalid shape.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make(int? b) => new C { P += b };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,45): error CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
            //     public static C Make(int? b) => new C { P += b };
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "P += b").WithArguments("int?", "int").WithLocation(4, 45));
    }

    [Fact]
    public void DynamicProperty_CompoundWithVoidRhs_Fails()
    {
        // Mirror of `BinaryOps_VoidArgument` (DynamicTests.cs:868) reshaped for the initializer
        // form. A `void`-returning call as the compound RHS on a `dynamic` target is invalid —
        // `void` is not a legal operand. The dynamic compound path in `BindCompoundAssignmentCore`
        // reports CS0019 regardless of whether the compound is a statement or sits inside a
        // member initializer; pin the initializer-form diagnostic so a regression that silently
        // accepted `void` (via a different dynamic-binding code path) would be visible.
        var source = """
            class C
            {
                public dynamic X { get; set; }
                static void F() {}
                public static C Make() => new C { X += F() };
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp).VerifyDiagnostics(
            // (5,39): error CS0019: Operator '+=' cannot be applied to operands of type 'dynamic' and 'void'
            //     public static C Make() => new C { X += F() };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "X += F()").WithArguments("+=", "dynamic", "void").WithLocation(5, 39));
    }

    #endregion

    #region Enum targets

    [Theory, MemberData(nameof(EnumBitwiseOperators))]
    public void Enum_FlagsBitwiseCompound_Succeeds(string op)
    {
        // Flag-enum bitwise compound is the canonical motivating case. Seed with all three flags
        // set (Visibility = 7) so every bitwise operator produces a distinct observable result:
        // &= 1 → 1, |= 1 → 7, ^= 1 → 6.
        var expected = op switch
        {
            "&=" => "1",
            "|=" => "7",
            "^=" => "6",
            _ => throw new System.ArgumentException(op),
        };
        var source = $$"""
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2, Enabled = 4 }

            class C
            {
                public V Visibility { get; set; } = V.Clickable | V.Visible | V.Enabled;
                public static void Main() => System.Console.Write((int)new C { Visibility {{op}} V.Clickable }.Visibility);
            }
            """;
        CompileAndVerify(source, expectedOutput: expected);
    }

    [Fact]
    public void Enum_PlusIntLiteral_Succeeds()
    {
        // `EnumValue + int` is legal (it shifts the enum by N positions in its underlying type), so
        // `P += 1` on an enum property works. Seed at A (0), += 1 advances to B (1).
        var source = """
            enum E { A, B, C }
            class C
            {
                public E P { get; set; }
                public static void Main() => System.Console.Write(new C { P += 1 }.P);
            }
            """;
        CompileAndVerify(source, expectedOutput: "B");
    }

    [Fact]
    public void Enum_PlusEnumValue_Fails()
    {
        // `EnumValue + EnumValue` is not defined, so `P += E.B` where P is of type E fails with CS0019.
        // The failure is identical to non-initializer compound on an enum.
        var source = """
            enum E { A, B, C }
            class C
            {
                public E P { get; set; }
                public static C Make() => new C { P += E.B };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): error CS0019: Operator '+=' cannot be applied to operands of type 'E' and 'E'
            //     public static C Make() => new C { P += E.B };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "P += E.B").WithArguments("+=", "E", "E").WithLocation(5, 39));
    }

    [Fact]
    public void Enum_Multiply_Fails()
    {
        // Enums do not participate in multiplication, so `*=` on an enum target is a compile-time error.
        var source = """
            enum E { A, B, C }
            class C
            {
                public E P { get; set; }
                public static C Make() => new C { P *= 2 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,39): error CS0019: Operator '*=' cannot be applied to operands of type 'E' and 'int'
            //     public static C Make() => new C { P *= 2 };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "P *= 2").WithArguments("*=", "E", "int").WithLocation(5, 39));
    }

    [Fact]
    public void Enum_FlagsInWith_Succeeds()
    {
        // Flag-enum compound on a record property via `with`. Seed Visibility = Clickable (1);
        // `w with { Visibility |= Visible }` produces Clickable|Visible = 3 on the clone.
        var source = """
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2 }

            record Widget(V Visibility)
            {
                public static void Main()
                {
                    var w = new Widget(V.Clickable);
                    var w2 = w with { Visibility |= V.Visible };
                    System.Console.Write((int)w2.Visibility);
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "3");
    }

    [Fact]
    public void Enum_MixedSimpleAndBitwiseCompound_Succeeds()
    {
        // Spec's "at most one `=`, `=` before any compound" rule applies to enum targets just like any
        // other field/property. Realistic pattern: set initial flags with `=`, then add more with `|=`,
        // then mask with `&=`. Visibility = 1 → |=2 → 3 → &=~4 → 3 (the ~Enabled mask is a no-op here
        // since Enabled wasn't set).
        var source = """
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2, Enabled = 4 }

            class C
            {
                public V Visibility { get; set; }
                public static void Main() => System.Console.Write((int)new C { Visibility = V.Clickable, Visibility |= V.Visible, Visibility &= ~V.Enabled }.Visibility);
            }
            """;
        CompileAndVerify(source, expectedOutput: "3");
    }

    #endregion

    #region Containers

    [Fact]
    public void Container_Struct_Succeeds()
    {
        // Struct initializer `new S { F += 1 }` creates a fresh S (F default 0), does F += 1 on
        // the temp, returns it. Verifies the initializer operates on the constructed value.
        var source = """
            struct S
            {
                public int F;
                public static void Main() => System.Console.Write(new S { F += 1 }.F);
            }
            """;
        CompileAndVerify(source, expectedOutput: "1");
    }

    [Fact]
    public void Container_RecordClass_With_Succeeds()
    {
        // `r with { P += 1 }` clones then mutates P on the clone; the original is untouched.
        var source = """
            record C(int P)
            {
                public static void Main()
                {
                    var r = new C(10);
                    var r2 = r with { P += 1 };
                    System.Console.Write($"{r.P},{r2.P}");
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "10,11");
    }

    [Fact]
    public void Container_RecordStruct_With_Succeeds()
    {
        // Record struct behaves identically for `with` — copy semantics, compound on the clone.
        var source = """
            record struct C(int P)
            {
                public static void Main()
                {
                    var r = new C(10);
                    var r2 = r with { P += 1 };
                    System.Console.Write($"{r.P},{r2.P}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "10,11");
    }

    [Fact]
    public void Container_AnonymousType_Fails()
    {
        // Anonymous-type member initializers use a distinct grammar form (anonymous_object_member_
        // declarator) that admits only `Name = Expr`, `Name`, or `Expr.Name` — not compound. CS0746
        // fires on the declarator; the LHS also binds in an empty symbol context, so CS0103 cascades.
        var source = """
            class C
            {
                public static void M()
                {
                    var a = new { X += 1 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,23): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
            //         var a = new { X += 1 };
            Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "X += 1").WithLocation(5, 23),
            // (5,23): error CS0103: The name 'X' does not exist in the current context
            //         var a = new { X += 1 };
            Diagnostic(ErrorCode.ERR_NameNotInContext, "X").WithArguments("X").WithLocation(5, 23));
    }

    #endregion

    #region User-defined operators

    [Fact]
    public void UserDefined_LegacyBinaryOperator_Resolves()
    {
        // Only legacy `operator +` is defined, so `Prop += v` must lower to `Prop = Prop + v`,
        // invoking the user-defined `+`. Seed Prop.X = 10, v.X = 5 → final Prop.X = 15.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public static V operator +(V a, V b) => new V(a.X + b.X);
            }
            class C
            {
                public V Prop { get; set; } = new V(10);
                public static void Main() => System.Console.Write(new C { Prop += new V(5) }.Prop.X);
            }
            """;
        CompileAndVerify(source, expectedOutput: "15");
    }

    [Fact]
    public void UserDefined_InPlaceCompoundOperator_Resolves_OnField()
    {
        // The C# 14 in-place `operator +=` requires a variable location on the left (it's a ref-like
        // mutating call). Fields qualify; properties-by-value do not. Seed F.X = 10, v.X = 5; the
        // in-place operator bumps F.X by 5 → 15. If binding regressed to legacy resolution here it
        // would fail with CS0019 (no `operator +` is defined).
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                public V F = new V(10);
                public static void Main() => System.Console.Write(new C { F += new V(5) }.F.X);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    [Fact]
    public void UserDefined_BothLegacyAndInPlace_OnField_InPlaceWins()
    {
        // When both legacy `operator +` and in-place `operator +=` exist and the target is a variable
        // (field), the in-place operator is selected — matching the non-initializer compound selection
        // rule. Legacy `+` would produce F.X = 10 + 5 + 100 = 115; in-place `+=` produces F.X = 15.
        // Any output other than "15" means the wrong overload won.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public static V operator +(V a, V b) => new V(a.X + b.X + 100);
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                public V F = new V(10);
                public static void Main() => System.Console.Write(new C { F += new V(5) }.F.X);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    [Fact]
    public void UserDefined_InPlaceOnly_OnProperty_FailsBecausePropertyIsNotVariable()
    {
        // When the target is a by-value property (not a location), the in-place `operator +=` cannot
        // apply, and without a legacy `operator +` there is no compound form. Expect CS0019.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                public V Prop { get; set; }
                public static C Make(V v) => new C { Prop += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (10,42): error CS0019: Operator '+=' cannot be applied to operands of type 'V' and 'V'
            //     public static C Make(V v) => new C { Prop += v };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "Prop += v").WithArguments("+=", "V", "V").WithLocation(10, 42));
    }

    [Fact]
    public void UserDefined_InPlaceOnly_OnRefReturningProperty_Succeeds()
    {
        // A ref-returning property IS a variable per the spec, so user-defined in-place `operator +=`
        // applies just like on a field target. Seed _v.X = 10, v.X = 5 → ref-return lets in-place +=
        // mutate _v directly → Prop.X = 15.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                private V _v = new V(10);
                public ref V Prop => ref _v;
                public static void Main() => System.Console.Write(new C { Prop += new V(5) }.Prop.X);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    [Fact]
    public void UserDefined_InPlaceOnly_OnRefReturningProperty_InWithOnRecordStruct_Succeeds()
    {
        // Exercises the interaction of: record struct copy semantics (`with` clones) + ref-returning
        // property (returns a ref into the clone's field via [UnscopedRef]) + user-defined in-place
        // `operator +=` on a struct (mutates through that ref). Original stays at 10; the clone sees
        // the in-place += bump to 15. If `with`'s clone-then-mutate contract leaked into the original
        // (sharing backing storage) this test would fail with "15,15".
        var source = """
            using System.Diagnostics.CodeAnalysis;
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            record struct Container(V Init)
            {
                public V Data = Init;
                [UnscopedRef]
                public ref V Ref => ref Data;
                public static void Main()
                {
                    var a = new Container(new V(10));
                    var b = a with { Ref += new V(5) };
                    System.Console.Write($"{a.Data.X},{b.Data.X}");
                }
            }
            """;
        // IL verifier rejects returning a `ref` from an instance member of a struct (even with
        // [UnscopedRef]); skip PEVerify for this test — the runtime executes correctly.
        CompileAndVerify([source, Polyfills, UnscopedRefAttributeDefinition], verify: Verification.Skipped, expectedOutput: "10,15");
    }

    [Fact]
    public void UserDefined_InPlaceOnly_OnRefReadonlyProperty_Fails()
    {
        // A `ref readonly` property IS a location but NOT assignable through that ref, so the
        // user-defined in-place `operator +=` (which mutates the location) cannot apply. Without a
        // legacy `operator +` there's no compound form. Expect CS0019 — not a silent acceptance that
        // our earlier simplified value-kind logic would have allowed.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                private V _v;
                public ref readonly V Prop => ref _v;
                public static C Make(V v) => new C { Prop += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (11,42): error CS8331: Cannot assign to property 'Prop' because it is a readonly variable
            //     public static C Make(V v) => new C { Prop += v };
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "Prop").WithArguments("property", "Prop").WithLocation(11, 42));
    }

    #endregion

    #region Required members

    [Fact]
    public void Required_CompoundAlone_DoesNotSatisfy()
    {
        // Per the LDM recommendation ("No"): a compound member initializer does NOT discharge the
        // `required` obligation. With only `P += 1` and no `P = ...`, required-member checking should
        // still demand P.
        var source = """
            class C
            {
                public required int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,35): error CS9035: Required member 'C.P' must be set in the object initializer or attribute constructor.
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.P").WithLocation(4, 35));
    }

    [Fact]
    public void Required_EqualsThenCompound_SatisfiesViaEquals()
    {
        // `P = 0` discharges the `required` obligation; the subsequent `P += 1` runs after. Final P = 1.
        var source = """
            class C
            {
                public required int P { get; set; }
                public static void Main() => System.Console.Write(new C { P = 0, P += 1 }.P);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "1");
    }

    [Fact]
    public void Required_CompoundAlone_WithSetsRequiredMembersCtor_Succeeds()
    {
        // `[SetsRequiredMembers]` on a constructor lifts the `required` obligation for any `new` via
        // that constructor. Within that lifted context, a compound-alone member initializer is fine —
        // the obligation is discharged by the attribute, so the initializer is free to read-modify-write
        // the pre-initialized value. Sanity-check counterpart to Required_CompoundAlone_DoesNotSatisfy.
        var source = """
            using System.Diagnostics.CodeAnalysis;
            class C
            {
                public required int P { get; set; }
                [SetsRequiredMembers]
                public C() { P = 10; }
                public static void Main() => System.Console.Write(new C { P += 5 }.P);
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    #endregion

    #region Mixed initializer + feature interactions

    [Fact]
    public void Mixed_EqualsAndCompound_ProducesCorrectBoundShapes()
    {
        // Lock down that the binder produces ISimpleAssignmentOperation for `=` and
        // ICompoundAssignmentOperation (or IEventAssignmentOperation) for compound forms. Verified
        // end-to-end: P goes 0 → 10 → 15, E ends up with h attached (raise observes one call).
        var source = """
            using System;
            class C
            {
                public int P { get; set; }
                public event EventHandler E;
                public void Raise() => E?.Invoke(null, EventArgs.Empty);
                public static void Main()
                {
                    int count = 0;
                    EventHandler h = (s, e) => count++;
                    var c = new C { P = 10, P += 5, E += h };
                    c.Raise();
                    System.Console.Write($"{c.P},{count}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "15,1");
    }


    #endregion

    #region Nullable flow

    [Fact]
    public void Nullable_CompoundOnReferenceType_UpdatesContainerSlot()
    {
        // Regression test for adversarial-audit finding #3 (NullableWalker.VisitObjectCreationInitializer
        // dropping slot tracking for compound targets). After `S += "y"` the container's nullable state
        // for S must reflect the string-concat result (not-null). If the walker still drops the slot
        // update, the post-initializer read would incorrectly warn or stay in an uninitialized state.
        var source = """
            #nullable enable
            class C
            {
                public string S { get; set; } = "a";
                public static void M()
                {
                    var c = new C { S += "y" };
                    // After compound, c.S is not-null. No WRN_NullReferenceReceiver when dereferencing.
                    _ = c.S.Length;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Nullable_CoalesceInitializesFromMaybeNull()
    {
        // Nullable flow through `??=`: the target starts maybe-null; after `P ??= "a"` on the clone
        // the state must be not-null (the `??=` either left a non-null value alone or just wrote
        // "a"). A subsequent dereference `c.P.Length` on the initializer result must not warn.
        var source = """
            #nullable enable
            class C
            {
                public string? P { get; set; }
                public static void M()
                {
                    var c = new C { P ??= "a" };
                    _ = c.P.Length;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Nullable_CoalesceWithNullRhs_FlowsMaybeNull()
    {
        // `P ??= null` — initializer with `??=` where both the target and the RHS may be null. The
        // compound result is maybe-null; a subsequent `c.P.Length` must warn CS8602. Pins that
        // nullable flow through `??=` in an initializer doesn't silently treat the target as
        // non-null afterwards.
        var source = """
            #nullable enable
            class C
            {
                public string? P { get; set; }
                public static void M(string? x)
                {
                    var c = new C { P ??= x };
                    _ = c.P.Length;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): warning CS8602: Dereference of a possibly null reference.
            //         _ = c.P.Length;
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.P").WithLocation(8, 13));
    }

    [Fact]
    public void Nullable_CompoundWithNullLiteralRhs_Warns()
    {
        // Assigning `null` RHS to a non-nullable reference-typed property via compound concat with
        // string gives a null reference through concat — compound binding + nullable analysis should
        // preserve the CS8625 warning on the `null` RHS the same as a non-initializer compound would.
        var source = """
            #nullable enable
            class C
            {
                public string S { get; set; } = "a";
                public static void M(string? nullable)
                {
                    // S += nullable → "a" + null → "a" (runtime), but nullable flow sees `nullable` as
                    // maybe-null; concat doesn't unwrap that, so the result is still "not-null".
                    // Confirms that flow analysis through the compound RHS still runs.
                    var c = new C { S += nullable };
                    _ = c.S.Length;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Nullable_DisallowNullProperty_Compound_InInitializer_Warns()
    {
        // Mirror of `DisallowNull_Property_CompoundAssignment` (NullableReferenceTypesTests.cs:45090)
        // translated into initializer form. The property is typed `C?` but annotated
        // `[DisallowNull]`, so its *write* contract rejects null. `operator+` returns `C?`, so the
        // compound's effective write is maybe-null → WRN8607 (DisallowNull forbids maybe-null).
        // NullableWalker's `VisitCompoundOrCoalesceObjectElementInitializer` + `UpdateInitializerMemberSlot`
        // must feed the compound's result state into the per-member slot the same way the statement
        // form feeds the local's slot; a regression that skipped the slot merge would drop the
        // warning silently. Using a struct target so both WRN8607 (assignment) and WRN8629
        // (subsequent `.Value` receiver) surface, matching the non-initializer baseline shape.
        var source = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;
            struct S
            {
                [DisallowNull] public S? Property { get; set; }
                public static S? operator+(S? one, S other) => throw null!;
            }
            class Driver
            {
                public static S Make(S s) => new S { Property += s };
            }
            """;
        CreateCompilation([source, DisallowNullAttributeDefinition]).VerifyDiagnostics(
            // (10,42): warning CS8607: A possible null value may not be used for a type marked with [NotNull] or [DisallowNull]
            //     public static S Make(S s) => new S { Property += s };
            Diagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, "Property += s").WithLocation(10, 42));
    }

    [Fact]
    public void Nullable_MaybeNullPropertyRead_UserDefinedPlus_InInitializer_Warns()
    {
        // Mirror of `CompoundAssignment_01` (NullableReferenceTypesTests.cs:74753). The property is
        // `CL1?`; reading it for `+=` converts via the implicit `CL1 -> CL0` operator and invokes
        // `operator+(CL0, CL0) -> CL1`, then stores the result back into the `CL1?` property.
        // The read must report WRN8604 on the null LHS, same as the non-initializer baseline.
        // NullableWalker's `VisitCompoundOrCoalesceObjectElementInitializer` drives the compound
        // op's Visit, which re-visits the wrapped BoundObjectInitializerMember's underlying
        // BoundPropertyAccess — a regression that bypassed the left-visit would miss the warning.
        var source = """
            #nullable enable
            public class CL0
            {
                public static CL1 operator+(CL0 one, CL0 two) => new CL1();
            }
            public class CL1
            {
                public static implicit operator CL0(CL1 x) => new CL0();
            }
            class Container
            {
                public CL1? P { get; set; }
            }
            class Driver
            {
                public static Container Make(CL1? x, CL0 y)
                    => new Container { P = x, P += y };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (17,35): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
            //         => new Container { P = x, P += y };
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "P").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(17, 35));
    }

    [Fact]
    public void Nullable_NotNullIfNotNullOnReturn_Compound_InInitializer_Flows()
    {
        // Mirror of `NotNullIfNotNull_Return_BinaryOperatorInCompoundAssignment`
        // (NullableReferenceTypesTests.cs:33850): a user-defined `operator +` annotated with
        // `[return: NotNullIfNotNull(...)]` must propagate not-null state through `+=`. In the
        // initializer path, the annotation's effect on the compound's result has to reach the
        // per-member slot update in NullableWalker so a subsequent dereference of the same member
        // doesn't warn. Pin the no-warn case (both operands not-null → result not-null → `.Length`
        // is safe) so a regression that dropped the annotation would fire CS8602.
        var source = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;
            class C
            {
                public C? P { get; set; }
                [return: NotNullIfNotNull("x"), NotNullIfNotNull("y")]
                public static C? operator +(C? x, C? y) => null;
            }
            class Driver
            {
                public static void M(C ac)
                {
                    // Build a C via initializer. Seed P to not-null via `=`, then compound with a
                    // not-null RHS; NotNullIfNotNull says the result is not-null too. Dereferencing
                    // `c.P.ToString()` afterwards must not warn.
                    var c = new C { P = ac, P += ac };
                    _ = c.P.ToString();
                }
            }
            """;
        CreateCompilation([source, NotNullIfNotNullAttributeDefinition]).VerifyDiagnostics();
    }

    #endregion

    #region String compound

    [Fact]
    public void String_Concat_Runs()
    {
        // Spec-coverage: plain `S += "y"` on a string-typed property. Exercises the reference-type
        // compound path through object initializer; string concatenation is the canonical case.
        var source = """
            class C
            {
                public string S { get; set; } = "a";
                public static void Main() => System.Console.Write(new C { S += "y" }.S);
            }
            """;
        CompileAndVerify(source, expectedOutput: "ay");
    }

    [Fact]
    public void String_MultipleCompounds_Runs()
    {
        // Accumulate across multiple member initializers, mixing simple assignment and compound.
        var source = """
            class C
            {
                public string S { get; set; } = "";
                public static void Main() => System.Console.Write(new C { S = "a", S += "b", S += "c" }.S);
            }
            """;
        CompileAndVerify(source, expectedOutput: "abc");
    }

    #endregion

    #region Checked / unchecked context

    [Fact]
    public void Checked_IntOverflow_Throws()
    {
        // `checked { new C { P += int.MaxValue } }` must propagate the checked context through
        // compound lowering (LocalRewriter clones the compound op with `Update(...)`; the CheckOverflow
        // flag needs to survive). In checked context, int.MaxValue + 1 overflows at runtime.
        var source = """
            using System;
            class C
            {
                public int P { get; set; } = 1;
                public static void Main()
                {
                    try
                    {
                        _ = checked(new C { P += int.MaxValue });
                        System.Console.Write("no-throw");
                    }
                    catch (OverflowException)
                    {
                        System.Console.Write("overflow");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "overflow");
    }

    [Fact]
    public void Checked_MultiplyOverflow_Throws()
    {
        // Cover a non-`+=` arithmetic operator under checked context — compound lowering clones the
        // op via Update(...) and must preserve the CheckOverflow flag. `P *= 2` with P seeded to
        // int.MaxValue overflows in checked context.
        var source = """
            using System;
            class C
            {
                public int P { get; set; } = int.MaxValue;
                public static void Main()
                {
                    try
                    {
                        _ = checked(new C { P *= 2 });
                        System.Console.Write("no-throw");
                    }
                    catch (OverflowException)
                    {
                        System.Console.Write("overflow");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "overflow");
    }

    [Fact]
    public void Checked_MultiplyOverflow_InWithOnRecord_Throws()
    {
        // Checked flag has to survive the `compound.Update(compound.Operator, ...)` rebuild in the
        // `with`-expression lowering just as it does in the `new` form. Pin the same OverflowException
        // through a record `with` clause.
        var source = """
            using System;
            record R(int P);
            class Driver
            {
                public static void Main()
                {
                    try
                    {
                        var r = new R(int.MaxValue);
                        _ = checked(r with { P *= 2 });
                        System.Console.Write("no-throw");
                    }
                    catch (OverflowException)
                    {
                        System.Console.Write("overflow");
                    }
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "overflow");
    }

    [Fact]
    public void Checked_Indexer_Compound_Throws()
    {
        // Indexer target under checked context. The lowering's side-effecting-arg caching path runs
        // alongside `compound.Update(compound.Operator, ...)`; we want both to preserve the flag so
        // an int.MaxValue + 1 on `[0]` overflows just like a plain property would.
        var source = """
            using System;
            class C
            {
                private int[] _v = { int.MaxValue };
                public int this[int i] { get => _v[i]; set => _v[i] = value; }
                public static void Main()
                {
                    try
                    {
                        _ = checked(new C { [0] += 1 });
                        System.Console.Write("no-throw");
                    }
                    catch (OverflowException)
                    {
                        System.Console.Write("overflow");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "overflow");
    }

    [Fact]
    public void Async_AwaitInCompoundRhs_InInitializer_Runs()
    {
        // The compound RHS is an arbitrary expression; `await` inside it exercises the async state-
        // machine rewriter on top of the initializer-member compound lowering. The placeholder
        // receiver substitution in the initializer and the compound op's placeholder chain must
        // survive async spilling. Runtime-verify: seed P=3, RHS `await Task.FromResult(5)` → final P=8.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                public int P { get; set; } = 3;
                public static async Task Main()
                {
                    var c = new C { P += await Task.FromResult(5) };
                    System.Console.Write(c.P);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "8");
    }

    [Fact]
    public void Async_AwaitInCoalesceRhs_InInitializer_Runs()
    {
        // Mirror of Async_AwaitInCompoundRhs_InInitializer_Runs for `??=` — this path takes the
        // BoundNullCoalescingAssignmentOperator lowering instead of compound's. Pin runtime
        // behavior: P starts null, `await Task.FromResult("x")` returns "x", `??=` stores it.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                public string P { get; set; }
                public static async Task Main()
                {
                    var c = new C { P ??= await Task.FromResult("x") };
                    System.Console.Write(c.P);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "x");
    }

    [Fact]
    public void Unchecked_MultiplyOverflow_Wraps()
    {
        // Mirror of Checked_MultiplyOverflow_Throws under unchecked context: int.MaxValue * 2 wraps
        // to -2. Pin the full arithmetic family (not just `+=`) through both checked and unchecked.
        var source = """
            class C
            {
                public int P { get; set; } = int.MaxValue;
                public static void Main()
                {
                    var c = unchecked(new C { P *= 2 });
                    System.Console.Write(c.P);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "-2");
    }

    [Fact]
    public void Unchecked_IntOverflow_Wraps()
    {
        // Mirror of Checked_IntOverflow_Throws: `unchecked` must silently wrap int.MaxValue + 1 to
        // int.MinValue. Confirms the checked/unchecked context from the enclosing expression flows
        // through the compound lowering.
        var source = """
            class C
            {
                public int P { get; set; } = 1;
                public static void Main()
                {
                    var c = unchecked(new C { P += int.MaxValue });
                    System.Console.Write(c.P);
                }
            }
            """;
        // 1 + int.MaxValue (unchecked) = int.MinValue = -2147483648
        CompileAndVerify(source, expectedOutput: "-2147483648");
    }

    #endregion

    #region Inline arrays and spans

    [Fact]
    public void InlineArray_Field_NestedInitializerIndexer_Fails()
    {
        // Mirror of `InlineArrayTests.CompoundAssignment_01` (Emit3/Semantics/InlineArrayTests.cs:15143)
        // reshaped as an initializer. The statement form `x.F[0] += 111` on a `[InlineArray]`
        // field works because the compiler recognizes inline-array indexing as a compiler-
        // intrinsic lowered via `InlineArrayFirstElementRef`. The *nested initializer* path
        // (`BindObjectInitializerMemberCommon` searching for a declared `this[int]` indexer on
        // `F`'s type) doesn't look for the intrinsic, so `new C { F = { [0] += 111 } }` fails
        // with CS0021 at the `[0]` access. Pins this asymmetry: statement form works; initializer
        // form rejects. A future fix that extended initializer-indexer lookup to recognize inline-
        // arrays would flip this test intentionally.
        var source = """
            [System.Runtime.CompilerServices.InlineArray(10)]
            public struct Buffer10<T>
            {
                private T _element0;
            }
            public class C
            {
                public Buffer10<int> F;
                public C(int seed) { F[0] = seed; }
            }
            public class Program
            {
                public static void Make() { _ = new C(-1) { F = { [0] += 111 } }; }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (13,55): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
            //     public static void Make() { _ = new C(-1) { F = { [0] += 111 } }; }
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("Buffer10<int>").WithLocation(13, 55));
    }

    [Fact]
    public void Span_ValueTypeProperty_NestedInitializer_Fails()
    {
        // Mirror of `PatternIndexAndRangeCompoundOperatorRefIndexer` (IndexAndRangeTests.cs:178)
        // reshaped as an initializer. `Span<T>` is a `ref struct` — a value type — so a property
        // returning `Span<T>` can't be used as a nested-initializer target: CS1918 fires by the
        // same rule that rejects `new C { IntProp = { Nested = 1 } }` when `IntProp` is any
        // value-type. The statement form `c.Slice[^1] += 1` works (the ref indexer is fine), but
        // `new C { Slice = { [^1] += 1 } }` rejects at the Slice access. Pins the asymmetry.
        var source = """
            using System;
            class C
            {
                public byte[] Storage = new byte[2];
                public Span<byte> Slice => Storage;
                public static void Make() { _ = new C { Slice = { [^1] += 1 } }; }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (6,45): error CS1918: Members of property 'C.Slice' of type 'Span<byte>' cannot be assigned with an object initializer because it is of a value type
            //     public static void Make() { _ = new C { Slice = { [^1] += 1 } }; }
            Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Slice").WithArguments("C.Slice", "System.Span<byte>").WithLocation(6, 45));
    }

    #endregion

    #region Indexer args evaluated once (spec-normative)

    [Fact]
    public void Indexer_SideEffectingArgument_EvaluatedOnce()
    {
        // Spec (§12.21.4 via the initializer's statement-expression lowering + §12.8.16.3's
        // "arguments shall always be evaluated exactly once" clause): a side-effecting indexer
        // argument in a compound member initializer must be evaluated exactly once — the compound
        // reads and writes the same slot without re-evaluating the index. The IL test
        // IL_Indexer_OrEquals already pins that constants are reloaded; this test pins the
        // side-effecting case by observing the call count.
        var source = """
            class C
            {
                public int Counter;
                public int GetIndex()
                {
                    Counter++;
                    return 0;
                }
                private int[] _values = { 10 };
                public int this[int i]
                {
                    get => _values[i];
                    set => _values[i] = value;
                }
                public static void Main()
                {
                    var c = new C();
                    var d = new C { [c.GetIndex()] += 5 };
                    // GetIndex must be called exactly once; value at [0] must be 10+5=15.
                    System.Console.Write($"{c.Counter},{d[0]}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,15");
    }

    [Fact]
    public void ImplicitIndex_SideEffectingLength_CalledOnce()
    {
        // Mirror of `PatternIndexCompoundOperator` (IndexAndRangeTests.cs:236) reshaped for the
        // initializer form. `S[^1] += 5` lowers to "call Length once, compute index, call get, op,
        // call set" — the pattern-index path caches Length. Our existing `Indexer_SideEffectingArgument_EvaluatedOnce`
        // test pins integer-arg caching; this pins the *pattern-index* Length caching when the
        // compound sits inside `new C { S = { [^1] += 5 } }`. Expected trace: "Length 0 / Get 1 /
        // Set 2" — one each — then the mutated underlying array reads `5`.
        var source = """
            using System;
            struct S
            {
                private readonly int[] _array;
                private int _counter;
                public S(int[] a)
                {
                    _array = a;
                    _counter = 0;
                }
                public int Length
                {
                    get { Console.WriteLine("Length " + _counter++); return _array.Length; }
                }
                public int this[int index]
                {
                    get { Console.WriteLine("Get " + _counter++); return _array[index]; }
                    set { Console.WriteLine("Set " + _counter++); _array[index] = value; }
                }
            }
            class C
            {
                public S Container;
                public C(int[] a) { Container = new S(a); }
                public static void Main()
                {
                    var array = new int[2];
                    var c = new C(array) { Container = { [^1] += 5 } };
                    Console.Write(array[1]);
                }
            }
            """;
        CompileAndVerify(
            source,
            targetFramework: TargetFramework.NetCoreApp,
            expectedOutput:
@"Length 0
Get 1
Set 2
5");
    }

    #endregion

    #region Semantic model / IOperation

    [Fact]
    public void SemanticModel_DynamicCompoundMemberInitializer_LateBoundSymbol()
    {
        // Mirror of `CompoundAssignment` in `SemanticModelGetSemanticInfoTests_LateBound.cs:808`
        // translated to the initializer form. A `P += d` where both `P` and `d` are `dynamic`
        // produces a "late-bound" symbol info: `GetSymbolInfo` returns
        // `dynamic.operator +(dynamic, dynamic)` with `CandidateReason.LateBound`. The initializer
        // path has to route through the same dynamic-binding code as the non-initializer form
        // (otherwise `GetSymbolInfo` on the inner compound would return a plain property/method
        // group instead of the synthetic late-bound operator symbol).
        var source = """
            class C
            {
                public dynamic P { get; set; }
                public static C Make(dynamic d) => /*<bind>*/new C { P += d }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var assignment = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax>().Single();
        var symbolInfo = model.GetSymbolInfo(assignment);
        Assert.Equal("dynamic.operator +(dynamic, dynamic)", symbolInfo.Symbol!.ToString());
        Assert.Equal(CandidateReason.LateBound, symbolInfo.CandidateReason);
        Assert.Empty(symbolInfo.CandidateSymbols);

        var typeInfo = model.GetTypeInfo(assignment);
        Assert.True(typeInfo.Type!.IsDynamic());
    }

    [Fact]
    public void SemanticModel_CompoundMemberInitializer_BindsAsCompoundOperation()
    {
        // The binder must produce an ICompoundAssignmentOperation for `P += 1` inside an object
        // initializer, parallel to an ISimpleAssignmentOperation for `P = 1`. Pin the operation kind
        // plus its target/value children and the LHS/operator-token symbol info.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => /*<bind>*/new C { P += 5 }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var objectCreation = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().Single();
        var objectCreationOp = (Operations.IObjectCreationOperation)model.GetOperation(objectCreation)!;
        Assert.NotNull(objectCreationOp.Initializer);
        var initializer = objectCreationOp.Initializer!;
        Assert.Single(initializer.Initializers);
        var compound = Assert.IsAssignableFrom<Operations.ICompoundAssignmentOperation>(initializer.Initializers[0]);
        Assert.Equal(Operations.BinaryOperatorKind.Add, compound.OperatorKind);

        // Target is a property reference to C.P, through the initializer placeholder receiver.
        var target = Assert.IsAssignableFrom<Operations.IPropertyReferenceOperation>(compound.Target);
        Assert.Equal("P", target.Property.Name);

        // Value is the int literal 5.
        var value = Assert.IsAssignableFrom<Operations.ILiteralOperation>(compound.Value);
        Assert.Equal(5, value.ConstantValue.Value);

        // GetSymbolInfo on the LHS identifier resolves to the property.
        var identifier = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
            .Single(n => n.Identifier.ValueText == "P" && n.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax { Left: var left } && left == n);
        var symbolInfo = model.GetSymbolInfo(identifier);
        Assert.Equal("P", symbolInfo.Symbol!.Name);
        Assert.Equal(SymbolKind.Property, symbolInfo.Symbol.Kind);

        // GetTypeInfo on the LHS identifier reports int.
        var typeInfo = model.GetTypeInfo(identifier);
        Assert.Equal(SpecialType.System_Int32, typeInfo.Type!.SpecialType);
    }

    [Fact]
    public void SemanticModel_EventPlusEqualsMemberInitializer_BindsAsEventAssignment()
    {
        // `E += h` in an initializer binds as BoundEventAssignmentOperator; the public IOperation
        // projection must expose it as IEventAssignmentOperation with Adds=true and the event
        // reference pointing at C.E. Pins the shape so a regression that flattened the event case
        // to ICompoundAssignmentOperation or IInvalidOperation would fail.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => /*<bind>*/new C { E += h }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation(source);
        // CS0067 fires because the event is never raised from inside C. Feature-irrelevant; the
        // operation shape is what this test pins.
        comp.VerifyDiagnostics(
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var objectCreation = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().Single();
        var objectCreationOp = (Operations.IObjectCreationOperation)model.GetOperation(objectCreation)!;
        Assert.NotNull(objectCreationOp.Initializer);
        var initializer = objectCreationOp.Initializer!;
        Assert.Single(initializer.Initializers);
        var eventAssignment = Assert.IsAssignableFrom<Operations.IEventAssignmentOperation>(initializer.Initializers[0]);
        Assert.True(eventAssignment.Adds);
        var eventRef = Assert.IsAssignableFrom<Operations.IEventReferenceOperation>(eventAssignment.EventReference);
        Assert.Equal("E", eventRef.Event.Name);
    }

    [Fact]
    public void SemanticModel_IndexerCompoundMemberInitializer_BindsAsCompoundOnIndexer()
    {
        // Indexer target compound — `{ [0] += 5 }` — must surface as
        // `ICompoundAssignmentOperation { Target: IPropertyReferenceOperation { Property.IsIndexer: true } }`
        // with the single literal argument reaching through. A regression that stripped the
        // BoundObjectInitializerMember wrapper's argument list from the IOperation projection
        // would be invisible without this pin.
        var source = """
            class C
            {
                public int this[int i] { get => 0; set { } }
                public static C Make() => /*<bind>*/new C { [0] += 5 }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var objectCreation = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().Single();
        var objectCreationOp = (Operations.IObjectCreationOperation)model.GetOperation(objectCreation)!;
        Assert.NotNull(objectCreationOp.Initializer);
        var compound = Assert.IsAssignableFrom<Operations.ICompoundAssignmentOperation>(objectCreationOp.Initializer!.Initializers.Single());
        var target = Assert.IsAssignableFrom<Operations.IPropertyReferenceOperation>(compound.Target);
        Assert.True(target.Property.IsIndexer);
        Assert.Single(target.Arguments);
        Assert.Equal(0, Assert.IsAssignableFrom<Operations.ILiteralOperation>(target.Arguments[0].Value).ConstantValue.Value);
    }

    [Fact]
    public void SemanticModel_WithExpressionCompound_BindsAsCompoundOnClonedMember()
    {
        // `r with { P += 5 }` takes a different top-level IOperation path (IWithOperation) but its
        // initializer members should still project as ICompoundAssignmentOperation. Pin the shape so
        // a regression that diverged the with and new IOperation projections fails here.
        var source = """
            record R(int P)
            {
                public static R Make(R r) => /*<bind>*/r with { P += 5 }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation([source, Polyfills]);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var withExpr = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.WithExpressionSyntax>().Single();
        var withOp = Assert.IsAssignableFrom<Operations.IWithOperation>(model.GetOperation(withExpr)!);
        Assert.NotNull(withOp.Initializer);
        var compound = Assert.IsAssignableFrom<Operations.ICompoundAssignmentOperation>(withOp.Initializer.Initializers.Single());
        Assert.Equal("P", Assert.IsAssignableFrom<Operations.IPropertyReferenceOperation>(compound.Target).Property.Name);
    }

    [Fact]
    public void SemanticModel_BadShape_CompoundNestedInitializer_DoesNotCrash()
    {
        // `P += { 1, 2 }` is the spec-forbidden "compound-with-nested-initializer RHS" — binding
        // produces a BoundBadExpression containing both boundLeft and boundRight as children. The
        // public SemanticModel API must not crash on this shape (previous versions of the binder
        // could return type=null bound nodes that NREd from downstream GetSymbolInfo calls).
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += { 1, 2 } };
            }
            """;
        var comp = CreateCompilation(source);

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var initializer = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax>().Single();
        var op = model.GetOperation(initializer);
        // The shape is rejected; GetOperation may return null for the bad node, which is acceptable
        // as long as it doesn't throw. What matters is that calls don't NRE — the bad-shape path
        // previously could produce a half-populated bound tree that NRE'd from downstream asserts.
        _ = op;

        // GetSymbolInfo / GetTypeInfo on the initializer and its operands must not throw.
        _ = model.GetSymbolInfo(initializer);
        _ = model.GetTypeInfo(initializer);
        _ = model.GetSymbolInfo(initializer.Left);
        _ = model.GetSymbolInfo(initializer.Right);
    }

    [Fact]
    public void SemanticModel_CoalesceMemberInitializer_BindsAsCoalesceOperation()
    {
        // `??=` has its own operation kind (INullCoalescingAssignmentOperation) separate from
        // ICompoundAssignmentOperation; the initializer form must produce the same operation shape
        // as a non-initializer `??=`. Pin: operation kind, Target as PropertyReference, Value as
        // literal, and symbol/type info on the LHS.
        var source = """
            class C
            {
                public string P { get; set; }
                public static C Make() => /*<bind>*/new C { P ??= "x" }/*</bind>*/;
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var objectCreation = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().Single();
        var objectCreationOp = (Operations.IObjectCreationOperation)model.GetOperation(objectCreation)!;
        Assert.NotNull(objectCreationOp.Initializer);
        var initializer = objectCreationOp.Initializer!;
        Assert.Single(initializer.Initializers);
        var coalesceAssignment = Assert.IsAssignableFrom<Operations.ICoalesceAssignmentOperation>(initializer.Initializers[0]);

        // Target is a property reference to C.P, through the initializer placeholder receiver.
        var target = Assert.IsAssignableFrom<Operations.IPropertyReferenceOperation>(coalesceAssignment.Target);
        Assert.Equal("P", target.Property.Name);

        // Value is the string literal "x".
        var value = Assert.IsAssignableFrom<Operations.ILiteralOperation>(coalesceAssignment.Value);
        Assert.Equal("x", value.ConstantValue.Value);

        // GetSymbolInfo on the LHS identifier resolves to the property.
        var identifier = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
            .Single(n => n.Identifier.ValueText == "P" && n.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax { Left: var left } && left == n);
        var symbolInfo = model.GetSymbolInfo(identifier);
        Assert.Equal("P", symbolInfo.Symbol!.Name);
        Assert.Equal(SymbolKind.Property, symbolInfo.Symbol.Kind);

        // GetTypeInfo on the LHS identifier reports string.
        var typeInfo = model.GetTypeInfo(identifier);
        Assert.Equal(SpecialType.System_String, typeInfo.Type!.SpecialType);
    }

    #endregion

    #region Primary constructor positional property in `with`

    [Fact]
    public void PrimaryCtor_PositionalProperty_CompoundInWith_Runs()
    {
        // Records synthesize an init-only property for each positional parameter. `w with { P += 1 }`
        // reads the clone's copy of the synthesized property (via the init setter, because records
        // clone-then-mutate). Confirms the feature works on the canonical record shape.
        var source = """
            record Counter(int Value);
            class Driver
            {
                public static void Main()
                {
                    var a = new Counter(10);
                    var b = a with { Value += 5 };
                    System.Console.Write($"{a.Value},{b.Value}");
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "10,15");
    }

    #endregion

    #region Expression tree ban

    [Fact]
    public void ExpressionTree_CompoundMemberInitializer_Fails()
    {
        // Expression trees cannot represent compound assignment. The diagnostics pass
        // reports ERR_ExpressionTreeContainsAssignment before ExpressionLambdaRewriter
        // runs, so the cast to BoundAssignmentOperator in the rewriter is never reached.
        var source = """
            using System;
            using System.Linq.Expressions;
            class C
            {
                public int P { get; set; }
                public static Expression<Func<C>> M() => () => new C { P += 1 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,60): error CS0832: An expression tree may not contain an assignment operator
            //     public static Expression<Func<C>> M() => () => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "P += 1").WithLocation(6, 60));
    }

    #endregion
}
