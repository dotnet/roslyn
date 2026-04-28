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
        // `with` counterpart to `Target_StaticProperty_Fails`; `with` rejects with CS0176 (instance-
        // reference on a static member) rather than `new`'s CS1914, because its binder treats the
        // clone receiver differently.
        var source = """
            record R(int V)
            {
                public static int P { get; set; }
                public static R Make(R r) => r with { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,43): error CS0176: Member 'R.P' cannot be accessed with an instance reference; qualify it with a type name instead
            //     public static R Make(R r) => r with { P += 1 };
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "P").WithArguments("R.P").WithLocation(4, 43));
    }

    [Fact]
    public void Target_InheritedProperty_Compound_RoutesThroughBaseSetter()
    {
        // Compound on an inherited property routes through the base's accessor pair. Runtime-verify
        // Base.P seeded to 10, `+= 5` via Derived → 15.
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
        // `new int P { … }` shadows the base; the initializer must resolve the derived P. Observable
        // via `d.P` (15 from 10+5) vs `((Base)d).P` (100, untouched).
        var source = """
            class Base { public int P { get; set; } = 100; }
            class Derived : Base { public new int P { get; set; } = 10; }
            class Driver
            {
                public static void Main()
                {
                    var d = new Derived { P += 5 };
                    System.Console.Write($"{d.P},{((Base)d).P}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "15,100");
    }

    [Fact]
    public void Target_InaccessiblePrivateSetter_Compound_Fails()
    {
        // Compound enforces setter accessibility the same as `=` does → CS0272 for `private set`
        // from outside the declaring type.
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
        // `[Obsolete]` on a property reports once per property reference; compound reads+writes the
        // same accessor pair, so it doesn't double-count.
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
        // Static events fail with CS1914 via the "static member in initializer" check before any
        // event-specific logic runs.
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
        // Bare BoundArrayAccess LHS through the dispatcher's ArrayAccess arm. Runtime-verify both
        // elements.
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
        // "Arguments evaluated exactly once" must also apply to bare BoundArrayAccess LHS (via
        // RewriteArrayInitializerAccess's arg lifting). Pin via a GetIndex counter.
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
        // Bare BoundPointerElementAccess LHS through the dispatcher's PointerElementAccess arm
        // (→ RewritePointerElementInitializerAccess). PEVerify skipped — pointer IL isn't verifiable.
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
        // Regression for a fixed lowering crash: `new Outer { Inner = { [0] += 5 } }` with dynamic
        // Inner used to trip `Debug.Assert(memberSymbol is object)`. The compound path now unwraps
        // the dynamic-indexer wrapper and routes through TransformDynamicIndexerAccess.
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
        // `+= / -=` dispatch through the event's public add/remove accessors — works from outside
        // the declaring type where `=` / `??=` reject (no accessible backing field).
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
        // `r with { E += h }` subscribes h on the clone's backing delegate. Raise on the clone → 1.
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
        // Every compound operator fires CS8652 at the operator token under C# 14 — no cascade.
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
        // `with` shares the feature gate — same CS8652 at the operator token.
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
        // `??=` in a `with` clause — same feature gate; diagnostic points at the `??=` token.
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

    // `??=` follows the C# 8 null-coalescing-assignment proposal's compound-assignment rules plus
    // the elide-if-non-null short-circuit; admitted here alongside the eleven regular compound ops.

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
        // Left-to-right: `P = null, P ??= 5, P ??= 10` → P = 5 (third sees non-null, skips).
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
        // Parallel to `Required_CompoundAlone_DoesNotSatisfy`: `??=` is a conditional write, so it
        // doesn't discharge `required`.
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
        // `??=` is feature-gated like the other compound operators — CS8652 at the operator token
        // under C# 14 (latest non-preview).
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
        // `??=`'s get+conditional-set pair reuses the cached argument — a side-effecting index
        // must be evaluated exactly once.
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
        // `??=` counterpart of `NestedCollectionInitializerOnRhs_Rejected`: CS0747 + the value-type
        // property's CS1918.
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
        // From inside the declaring type, `E ??= h` acts on the field-like event's backing field.
        // First `??=` sets h1; second `??=` is a no-op (E already non-null). Raise → h1 fires only.
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
        // `??=` reads the backing field, which isn't accessible from outside the declaring type →
        // CS0070. Pins both statement and initializer forms (fix uniformly in
        // BindNullCoalescingAssignmentOperatorCore).
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
        // Custom events have no backing field for `??=` to read → CS0079, same as `=`. Pinned in
        // both statement and initializer forms.
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
        // Spec: "No such restriction applies to indexer targets." Per-indexer-key tracking has never
        // been done (same-arg repeat has always been legal); the same applies with compound.
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
    public void Duplicate_Indexer_FirstForm_SameKeyUnrestricted()
    {
        // Spec: the first-form exclusivity rule applies to "field, property, or event" targets only;
        // indexer targets are deliberately excluded. So two `[k] = { ... }` initializers for the same
        // indexer key are permitted (each invokes `getter(k)` and configures the returned object).
        // Both nested initializers run on the same `Inner` instance returned by the indexer getter.
        var source = """
            class Inner { public int X; public int Y; }
            class C
            {
                private Inner _inner = new Inner();
                public Inner this[int i] { get => _inner; set => _inner = value; }
                public static void Main()
                {
                    var c = new C { [0] = { X = 1 }, [0] = { Y = 2 } };
                    System.Console.Write($"{c[0].X},{c[0].Y}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,2");
    }

    [Fact]
    public void Duplicate_Event_PlusMinusEqualsUnrestricted()
    {
        // `+=` / `-=` on an event are unrestricted. +a, +b, -a, +a → raise fires both (each once).
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
        // Field-like event `=` participates in the duplicate rule like field/property `=` →
        // CS1912 on the second.
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
        // `E = h, E += h` satisfies the ordering rule — runtime-verify both handlers fire
        // (raise count = 2).
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
        // `E += h, E = h` violates the "`=` before compound" rule → CS1912, matching field/property.
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
        // `??=` is a compound form (any number allowed on the same target). First sets "a"; second
        // short-circuits since P is now non-null.
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
        // `??=` counts as compound; a following simple `=` violates the ordering rule → CS1912.
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
        // `= then ??=` is legal ordering. `??=` short-circuits since P is already non-null.
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

    [Fact]
    public void Duplicate_NestedInit_ThenCompound_Fails()
    {
        // Resolution 2: `target = { … }` is exclusive — compound after nested init now fires CS1912.
        // Previously this combination silently compiled.
        var source = """
            class Inner
            {
                public int X { get; set; }
                public static Inner operator +(Inner a, Inner b) => a;
            }
            class C
            {
                public Inner P { get; set; } = new();
                public static C Make() => new C { P = { X = 1 }, P += new Inner() };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,54): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = { X = 1 }, P += new Inner() };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(9, 54));
    }

    [Fact]
    public void Duplicate_NestedInit_ThenSimple_Fails()
    {
        // Resolution 2: nested-init followed by another `=` (non-nested) → CS1912.
        var source = """
            class Inner { public int X { get; set; } }
            class C
            {
                public Inner P { get; set; } = new();
                public static C Make() => new C { P = { X = 1 }, P = new Inner() };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,54): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = { X = 1 }, P = new Inner() };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(5, 54));
    }

    [Fact]
    public void Duplicate_TwoNestedInits_Fails()
    {
        // Resolution 2: `target = { … }` is exclusive of itself too — at most one nested-init per
        // target. This case already errored under the existing "two `=`" rule, but pin it under
        // the new framing.
        var source = """
            using System.Collections.Generic;
            class C
            {
                public List<int> Items { get; set; } = new();
                public static C Make() => new C { Items = { 1, 2 }, Items = { 3 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,57): error CS1912: Duplicate initialization of member 'Items'
            //     public static C Make() => new C { Items = { 1, 2 }, Items = { 3 } };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "Items").WithArguments("Items").WithLocation(5, 57));
    }

    [Fact]
    public void Duplicate_Simple_ThenNestedInit_Fails()
    {
        // Simple `=` then nested-init → CS1912. Already errored under the existing rule (current
        // is `=`, name already seen) but exclusivity is the more precise framing.
        var source = """
            class Inner { public int X { get; set; } }
            class C
            {
                public Inner P { get; set; } = new();
                public static C Make() => new C { P = new Inner(), P = { X = 1 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,56): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = new Inner(), P = { X = 1 } };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(5, 56));
    }

    [Fact]
    public void Duplicate_Compound_ThenNestedInit_Fails()
    {
        // Compound followed by nested-init → CS1912. Already errored under the existing rule
        // (compound-then-`=`) — pin it under exclusivity.
        var source = """
            class Inner
            {
                public int X { get; set; }
                public static Inner operator +(Inner a, Inner b) => a;
            }
            class C
            {
                public Inner P { get; set; } = new();
                public static C Make() => new C { P += new Inner(), P = { X = 1 } };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,57): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P += new Inner(), P = { X = 1 } };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(9, 57));
    }

    [Fact]
    public void Duplicate_NestedInit_AsSoleInitializer_Succeeds()
    {
        // Sanity counterpart to the new exclusivity tests: a single `target = { … }` with no
        // sibling initializers continues to work (Resolution 2 forbids combinations, not the form
        // itself).
        var source = """
            using System.Collections.Generic;
            class C
            {
                public List<int> Items { get; } = new();
                public static void Main()
                {
                    var c = new C { Items = { 1, 2, 3 } };
                    System.Console.Write($"{c.Items[0]},{c.Items[1]},{c.Items[2]}");
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1,2,3");
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
        // Mirror of `TestCompoundLiftedAssignment_IOperation` (OperatorTests.cs:2402). `int P += int? b`
        // is CS0266 (lifted result can't convert back to `int`) — pins it in the initializer form.
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
        // Mirror of `BinaryOps_VoidArgument` (DynamicTests.cs:868). `void`-returning RHS on a
        // `dynamic` compound target → CS0019 in the initializer form same as the statement form.
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
        // `new S { F += 1 }` creates a fresh S (F defaults 0), does `F += 1` on the temp, returns it.
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
        // Anonymous-type member_initializer admits only `Name = Expr`, `Name`, or `Expr.Name` —
        // compound forms hit CS0746 (+ cascading CS0103 from the empty-context LHS bind).
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
        // Only legacy `operator +` defined → `Prop += v` lowers to `Prop = Prop + v`. 10 + 5 = 15.
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
        // In-place `operator +=` requires a variable LHS; a field qualifies (property-by-value
        // wouldn't). 10 += 5 via the in-place operator → 15. Regressing to legacy would fail CS0019.
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
        // Both legacy `+` and in-place `+=` defined; variable LHS (field) picks in-place. 10 += 5
        // via the in-place operator → 15; legacy would have produced 115 (+100 from its body).
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
        // In-place `+=` needs a variable LHS; a by-value property isn't one, and without legacy `+`
        // there's no compound form → CS0019.
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
        // A ref-returning property IS a variable → in-place `+=` applies. Mutates `_v` via the ref,
        // 10 → 15.
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
        // Record-struct `with` (clone) + ref-returning property (via [UnscopedRef]) + in-place
        // `operator +=` mutating through the ref. Original stays at 10; clone moves to 15. A
        // regression that shared backing storage would show "15,15".
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
        // `ref readonly` is a location but not assignable; in-place `+=` can't apply, no legacy `+`
        // exists → CS8331.
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
        // Compound alone doesn't discharge `required` (LDM recommendation: "No") → CS9035.
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
    public void Required_CompoundAlone_InWith_Succeeds()
    {
        // Resolution 1: `with` admits compound-only on a `required` target — the receiver was
        // already constructed with its required members satisfied, so no `=` is needed in the
        // `with` clause. Counterpart to `Required_CompoundAlone_DoesNotSatisfy` (object initializer).
        var source = """
            record C
            {
                public required int P { get; init; }
                public static void Main()
                {
                    var c = new C { P = 10 };
                    var d = c with { P += 5 };
                    System.Console.Write(d.P);
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "15");
    }

    [Fact]
    public void Required_CompoundAlone_WithSetsRequiredMembersCtor_Succeeds()
    {
        // `[SetsRequiredMembers]` lifts the obligation; compound-alone is then fine (counterpart to
        // `Required_CompoundAlone_DoesNotSatisfy`).
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
        // End-to-end: `=`, compound, and event `+=` composed in one initializer. P: 0 → 10 → 15.
        // E gets h; raise observes one fire.
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
        // Regression test for a fixed slot-tracking bug in VisitObjectCreationInitializer: after
        // `S += "y"` the container's nullable state for S must be not-null so the deref below
        // doesn't warn.
        var source = """
            #nullable enable
            class C
            {
                public string S { get; set; } = "a";
                public static void M()
                {
                    var c = new C { S += "y" };
                    _ = c.S.Length;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Nullable_CoalesceInitializesFromMaybeNull()
    {
        // After `P ??= "a"` the state is not-null — the deref below doesn't warn.
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
        // `P ??= x` with maybe-null `x` → maybe-null result → CS8602 on the deref.
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
        // Maybe-null RHS into a string-concat compound — pins that nullable flow through the
        // compound RHS still runs in the initializer form (CS8602 on the deref).
        var source = """
            #nullable enable
            class C
            {
                public string S { get; set; } = "a";
                public static void M(string? nullable)
                {
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
        // translated to the initializer form. Pins the WRN8607 through VisitCompoundOrCoalesceObjectElementInitializer's
        // slot update.
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
        // Mirror of `CompoundAssignment_01` (NullableReferenceTypesTests.cs:74753). WRN8604 on the
        // maybe-null LHS read (via the implicit CL1->CL0 operator) must fire the same in the
        // initializer form.
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
        // (NullableReferenceTypesTests.cs:33850). `[NotNullIfNotNull]` on `operator+` must
        // propagate through the initializer's slot update — no-warn for the not-null-both-sides
        // case; a regression that dropped the annotation would fire CS8602 on the deref below.
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
        // `await` in the compound RHS — the initializer placeholder and the compound op's
        // placeholder chain both must survive async spilling. Seed P=3, `+= await 5` → 8.
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
        // `??=` counterpart — takes BoundNullCoalescingAssignmentOperator's lowering path. P starts
        // null, `??= await "x"` stores it.
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
        // Statement form `x.F[0] += …` on a `[InlineArray]` field works via the compiler's
        // inline-array intrinsic; the nested-initializer indexer lookup doesn't recognize that
        // intrinsic and rejects with CS0021. Pins the asymmetry.
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
        // `Span<T>`-returning property isn't a legal nested-initializer target — it's a value type,
        // so CS1918 fires at the `Slice` access. Statement form `c.Slice[^1] += 1` would work.
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
        // Spec §12.8.16.3 "arguments shall always be evaluated exactly once" — the compound
        // reads and writes the same slot without re-evaluating the index. Pins the side-effecting
        // case via a counter (the IL test `IL_Indexer_OrEquals` pins the constant-arg reload shape).
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
        // Mirror of `PatternIndexCompoundOperator` (IndexAndRangeTests.cs:236). The pattern-index
        // path caches Length; pin it in the initializer form (Length / Get / Set each fire once).
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
            expectedOutput: """
                Length 0
                Get 1
                Set 2
                5
                """);
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
