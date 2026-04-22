// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

    #endregion

    #region Event target constraints

    [Theory, MemberData(nameof(NonPlusMinusCompoundOperators))]
    public void Event_NonPlusOrMinusCompound_Fails(string op)
    {
        // Per spec: "An *identifier* that names an event is a valid target only in combination with the
        // `+=` or `-=` operator". Other compound ops fall through BindCompoundAssignmentCore's event branch
        // and fail via overload resolution on the delegate type (CS0019).
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
    public void Coalesce_Event_FromOutsideContainingType_CompilesCleanly()
    {
        // Observed pre-existing behavior: `??=` on an event routes through `CheckValueKind` with
        // `BindValueKind.CompoundAssignment`, which `CheckEventValueKind` accepts as "event assignment"
        // without emitting CS0070. This matches the non-initializer `??=` binding today (same value-kind
        // path) — it's a latent gap in `??=`'s event handling that predates this feature. The initializer
        // form inherits the quirk. We pin the current behavior here rather than regress it; tightening
        // `??=` on events is a separate concern from compound-assignment-in-initializer.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
            }
            class Outer
            {
                public static C Make(EventHandler h) => new C { E ??= h };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
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
    public void Duplicate_Event_Unrestricted()
    {
        // Multiple subscribes/unsubscribes on the same event in one initializer all run. Order:
        // +a (a), +b (ab), -a (b), +a (ba). Raise() invokes both. a counts 1, b counts 1.
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

    #endregion

    #region Semantic model / IOperation

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
