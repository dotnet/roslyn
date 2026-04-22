// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CompoundAssignmentInitializerBindingTests : CSharpTestBase
{
    /// <summary>
    /// Polyfills for types the default reference set doesn't include: <c>IsExternalInit</c> (records /
    /// init-only / <c>with</c>), <c>CompilerFeatureRequiredAttribute</c> (user-defined <c>operator +=</c>),
    /// and <c>Required</c> / <c>SetsRequiredMembers</c> attributes. Bundled into every compilation so
    /// individual tests don't need to pick a target framework.
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

    #region Core operator coverage

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Property_AllOperators_CompileClean(string op)
    {
        var source = $$"""
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P {{op}} 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Field_AllOperators_CompileClean(string op)
    {
        var source = $$"""
            class C
            {
                public int F;
                public static C Make() => new C { F {{op}} 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void WithExpression_AllOperators_CompileClean(string op)
    {
        var source = $$"""
            record C(int P)
            {
                public static C Make(C r) => r with { P {{op}} 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Indexer_AllOperators_CompileClean(string op)
    {
        var source = $$"""
            class C
            {
                private int[] _values = new int[10];
                public int this[int i] { get => _values[i]; set => _values[i] = value; }
                public static C Make() => new C { [0] {{op}} 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    #endregion

    #region Target kinds

    [Fact]
    public void Target_WritableInstanceField_Succeeds()
    {
        var source = """
            class C
            {
                public int F;
                public static C Make() => new C { F += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
                public int P { get; init; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Target_InitOnlyProperty_SucceedsInWith()
    {
        var source = """
            record C(int V)
            {
                public int P { get; init; }
                public static C Make(C r) => r with { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Target_RefReturningProperty_Succeeds()
    {
        var source = """
            class C
            {
                private int _p;
                public ref int P => ref _p;
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
                private int[] _values = new int[10];
                public int this[int i] { get => _values[i]; set => _values[i] = value; }
                public static C Make() => new C { [0] |= 1, [1] &= 2, [2] += 3 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Target_FieldLikeEvent_PlusEqualsAndMinusEquals_Succeed()
    {
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h, E -= h };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
    }

    [Fact]
    public void Target_CustomEvent_PlusEqualsAndMinusEquals_Succeed()
    {
        var source = """
            using System;
            class C
            {
                public event EventHandler E
                {
                    add { }
                    remove { }
                }
                public static C Make(EventHandler h) => new C { E += h, E -= h };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Target_DynamicProperty_Succeeds()
    {
        var source = """
            class C
            {
                public dynamic X { get; set; }
                public static C Make() => new C { X += 1 };
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp)
            .VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E = h };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (9,48): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //     public C Fluent(EventHandler h) => new C { E = h };
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(9, 48));
    }

    [Fact]
    public void Event_InWith_PlusEquals_Succeeds()
    {
        var source = """
            using System;
            record C(int V)
            {
                public event EventHandler E;
                public static C Make(C r, EventHandler h) => r with { E += h };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
    }

    #endregion

    #region Language version gating

    [Fact]
    public void LangVersion_CSharp13_Fails()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills], parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,41): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 41));
    }

    [Fact]
    public void LangVersion_CSharp14_Fails()
    {
        // At C# 14, user-defined `operator +=` is available (it shipped in C# 14). Our feature is still
        // Preview-only; this case confirms the gate is specifically our feature, not a cascade from the
        // user-defined-`+=` feature.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills], parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (4,41): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 41));
    }

    [Fact]
    public void LangVersion_GatingAppliesToEveryCompoundOperator()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static void M() { var c = new C { P += 1, P -= 1, P *= 1, P /= 1, P %= 1, P &= 1, P |= 1, P ^= 1, P <<= 1, P >>= 1, P >>>= 1 }; }
            }
            """;
        // Expect a feature-gate diagnostic on each compound operator token (11 total).
        CreateCompilation([source, Polyfills], parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (4,48): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M() { var c = new C { P += 1, P -= 1, P *= 1, P /= 1, P %= 1, P &= 1, P |= 1, P ^= 1, P <<= 1, P >>= 1, P >>>= 1 }; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 48),
            // (4,56): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "-=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 56),
            // (4,64): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "*=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 64),
            // (4,72): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "/=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 72),
            // (4,80): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "%=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 80),
            // (4,88): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "&=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 88),
            // (4,96): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "|=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 96),
            // (4,104): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "^=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 104),
            // (4,112): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "<<=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 112),
            // (4,121): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, ">>=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 121),
            // (4,130): error CS8652: ...
            Diagnostic(ErrorCode.ERR_FeatureInPreview, ">>>=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 130));
    }

    #endregion

    #region ??= rejected

    [Fact]
    public void CoalesceAssignment_Rejected()
    {
        // `??=` is not in the spec's compound_assignment_operator set. The parser was permissive; the
        // binder must reject the shape with CS0747 (the existing "invalid initializer member declarator").
        var source = """
            class C
            {
                public int? P { get; set; }
                public static C Make() => new C { P ??= 1 };
            }
            """;
        // The parser accepted `P ??= 1` for resilience (Phase 1); the binder rejects. The default
        // fallback path binds the whole member-initializer expression as an RValue for recovery, which
        // incidentally produces a CS0120 "object reference required for P" in addition to CS0747.
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,39): error CS0120: An object reference is required for the non-static field, method, or property 'C.P'
            //     public static C Make() => new C { P ??= 1 };
            Diagnostic(ErrorCode.ERR_ObjectRequired, "P").WithArguments("C.P").WithLocation(4, 39),
            // (4,39): error CS0747: Invalid initializer member declarator
            //     public static C Make() => new C { P ??= 1 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "P ??= 1").WithLocation(4, 39));
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,46): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = 1, P = 2 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 46));
    }

    [Fact]
    public void Duplicate_EqualsThenCompound_Succeeds()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P = 10, P += 5 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,47): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P += 5, P = 10 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 47));
    }

    [Fact]
    public void Duplicate_TwoCompounds_Succeeds()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5, P += 10, P *= 2 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,54): error CS1912: Duplicate initialization of member 'P'
            //     public static C Make() => new C { P = 1, P += 2, P = 3 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "P").WithArguments("P").WithLocation(4, 54));
    }

    [Fact]
    public void Duplicate_Indexer_Unrestricted()
    {
        // Spec: "No such restriction applies to event or indexer targets." Per-indexer-key tracking has
        // never been done (same-arg repeat has always been legal); same applies with compound.
        var source = """
            class C
            {
                private int[] _values = new int[10];
                public int this[int i] { get => _values[i]; set => _values[i] = value; }
                public static C Make() => new C { [0] = 1, [0] += 2, [0] = 3, [0] |= 4 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Duplicate_Event_Unrestricted()
    {
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler a, EventHandler b) => new C { E += a, E += b, E -= a, E += a };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
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
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make(ref int x) => new C { P += ref x };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,48): error CS8373: The left-hand side of a ref assignment must be a ref variable.
            //     public static C Make(ref int x) => new C { P += ref x };
            Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(4, 48));
    }

    [Fact]
    public void NestedCollectionInitializerOnRhs_Rejected()
    {
        // Spec note: "The compound_assignment_operator branch admits only expression, so forms such as
        // P += { 1, 2 } are syntactically ill-formed." The parser is permissive (per Phase 1); the
        // binder binds both sides normally (producing CS1918 for the nested-initializer target and
        // CS1922 for the brace-list RHS against int) and additionally emits CS0747 for the compound +
        // nested-initializer combination the spec forbids outright.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += { 1, 2 } };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (4,39): error CS1918: Members of property 'C.P' of type 'int' cannot be assigned with an object initializer because it is of a value type
            //     public static C Make() => new C { P += { 1, 2 } };
            Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "P").WithArguments("C.P", "int").WithLocation(4, 39),
            // (4,39): error CS0747: Invalid initializer member declarator
            //     public static C Make() => new C { P += { 1, 2 } };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "P += { 1, 2 }").WithLocation(4, 39),
            // (4,44): error CS1922: Cannot initialize type 'int' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
            //     public static C Make() => new C { P += { 1, 2 } };
            Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1, 2 }").WithArguments("int").WithLocation(4, 44));
    }

    #endregion

    #region Enum targets

    [Theory, MemberData(nameof(EnumBitwiseOperators))]
    public void Enum_FlagsBitwiseCompound_Succeeds(string op)
    {
        // Flag-enum bitwise compound is the canonical motivating case: `new Widget { Visibility |= V.Clickable }`.
        var source = $$"""
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2, Enabled = 4 }

            class C
            {
                public V Visibility { get; set; }
                public static C Make(V flags) => new C { Visibility {{op}} flags };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Enum_PlusIntLiteral_Succeeds()
    {
        // `EnumValue + int` is legal (it shifts the enum by N positions in its underlying type), so
        // `P += 1` on an enum property works.
        var source = """
            enum E { A, B, C }
            class C
            {
                public E P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (5,39): error CS0019: Operator '*=' cannot be applied to operands of type 'E' and 'int'
            //     public static C Make() => new C { P *= 2 };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "P *= 2").WithArguments("*=", "E", "int").WithLocation(5, 39));
    }

    [Fact]
    public void Enum_FlagsInWith_Succeeds()
    {
        // Flag-enum compound on a record property via `with`.
        var source = """
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2 }

            record Widget(V Visibility)
            {
                public static Widget Set(Widget w, V flags) => w with { Visibility |= flags };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Enum_MixedSimpleAndBitwiseCompound_Succeeds()
    {
        // Spec's "at most one `=`, `=` before any compound" rule applies to enum targets just like any
        // other field/property. Realistic pattern: set initial flags with `=`, then add more with `|=`.
        var source = """
            using System;

            [Flags]
            enum V { None = 0, Clickable = 1, Visible = 2, Enabled = 4 }

            class C
            {
                public V Visibility { get; set; }
                public static C Make() => new C { Visibility = V.Clickable, Visibility |= V.Visible, Visibility &= ~V.Enabled };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    #endregion

    #region Containers

    [Fact]
    public void Container_Struct_Succeeds()
    {
        var source = """
            struct S
            {
                public int F;
                public static S Make() => new S { F += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Container_RecordClass_With_Succeeds()
    {
        var source = """
            record C(int P)
            {
                public static C Make(C r) => r with { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Container_RecordStruct_With_Succeeds()
    {
        var source = """
            record struct C(int P)
            {
                public static C Make(C r) => r with { P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void Container_AnonymousType_Fails()
    {
        // Anonymous-type properties are readonly; compound fails on write. Also, the `new { }` syntax
        // does not permit member initializers of the form `Name += Expr` grammatically at the
        // anonymous-type level — those are anonymous-object-member-declarators, a different grammar.
        var source = """
            class C
            {
                public static void M()
                {
                    var a = new { X = 1 };
                    // `new { X += 1 }` is not legal syntax for anonymous objects.
                    // Lock down that anonymous types are read-only via their projection initializers.
                }
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    #endregion

    #region User-defined operators

    [Fact]
    public void UserDefined_LegacyBinaryOperator_Resolves()
    {
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public static V operator +(V a, V b) => new V(a.X + b.X);
            }
            class C
            {
                public V Prop { get; set; }
                public static C Make(V v) => new C { Prop += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void UserDefined_InPlaceCompoundOperator_Resolves_OnField()
    {
        // The C# 14 in-place `operator +=` requires a variable location on the left (it's a ref-like
        // mutating call). Fields qualify; properties-by-value do not. This test pins that the binder
        // correctly resolves the in-place operator when the target is a field. If the raw-access fix
        // in BindInitializerMemberAssignment regresses, shouldTryUserDefinedInstanceOperator's
        // CheckValueKind would return false here and fall back to legacy resolution, which would
        // fail with CS0019 because there is no `operator +`.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public void operator +=(V b) { X += b.X; }
            }
            class C
            {
                public V F;
                public static C Make(V v) => new C { F += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    [Fact]
    public void UserDefined_BothLegacyAndInPlace_OnField_InPlaceWins()
    {
        // Pins that when both legacy `operator +` and in-place `operator +=` exist, and the target is
        // a variable (field), the in-place operator is selected — matching the non-initializer
        // compound assignment selection rule.
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
                public V F;
                public static C Make(V v) => new C { F += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        // applies just like on a field target.
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
                public ref V Prop => ref _v;
                public static C Make(V v) => new C { Prop += v };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
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
        var source = """
            class C
            {
                public required int P { get; set; }
                public static C Make() => new C { P = 0, P += 1 };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics();
    }

    #endregion

    #region Mixed initializer + feature interactions

    [Fact]
    public void Mixed_EqualsAndCompound_ProducesCorrectBoundShapes()
    {
        // Lock down that the binder produces ISimpleAssignmentOperation for `=` and
        // ICompoundAssignmentOperation (or IEventAssignmentOperation) for compound forms.
        var source = """
            using System;
            class C
            {
                public int P { get; set; }
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { P = 10, P += 5, E += h };
            }
            """;
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (5,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(5, 31));
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
        CreateCompilation([source, Polyfills]).VerifyDiagnostics(
            // (6,60): error CS0832: An expression tree may not contain an assignment operator
            //     public static Expression<Func<C>> M() => () => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "P += 1").WithLocation(6, 60));
    }

    #endregion
}
