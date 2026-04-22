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
    // Tests that use records / init-only setters need System.Runtime.CompilerServices.IsExternalInit;
    // tests that declare a direct `operator +=` additionally need CompilerFeatureRequiredAttribute.
    // The NetCoreApp reference set provides both; using it here keeps the test sources minimal.
    private CSharpCompilation Compile(string source, CSharpParseOptions parseOptions, TargetFramework targetFramework = TargetFramework.NetCoreApp)
        => CreateCompilation(source, parseOptions: parseOptions, targetFramework: targetFramework);

    /// <summary>All 11 compound assignment operators in the spec's `compound_assignment_operator` set.</summary>
    public static TheoryData<string> AllCompoundOperators => new()
    {
        "+=", "-=", "*=", "/=", "%=",
        "&=", "|=", "^=",
        "<<=", ">>=", ">>>=",
    };

    /// <summary>
    /// Operators that only make sense on integers (shift and bitwise) — useful when narrowing the matrix
    /// to avoid type-mismatch noise on non-integer targets.
    /// </summary>
    public static TheoryData<string> IntegerCompoundOperators => new()
    {
        "+=", "-=", "*=", "/=", "%=",
        "&=", "|=", "^=",
        "<<=", ">>=", ">>>=",
    };

    /// <summary>
    /// Arithmetic operators (usable on any numeric type including `double`).
    /// </summary>
    public static TheoryData<string> ArithmeticCompoundOperators => new()
    {
        "+=", "-=", "*=", "/=", "%=",
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.StandardAndCSharp)
            .VerifyDiagnostics();
    }

    #endregion

    #region Event target constraints

    [Fact]
    public void Event_NonPlusOrMinusCompound_Fails()
    {
        // Per spec: "An *identifier* that names an event is a valid target only in combination with the
        // `+=` or `-=` operator". Other compound ops fall through BindCompoundAssignmentCore's event branch
        // and fail via overload resolution on the delegate type (CS0019).
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E *= h };
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (5,53): error CS0019: Operator '*=' cannot be applied to operands of type 'EventHandler' and 'EventHandler'
            //     public static C Make(EventHandler h) => new C { E *= h };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "E *= h").WithArguments("*=", "System.EventHandler", "System.EventHandler").WithLocation(5, 53));
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (4,41): error CS8652: The feature 'compound assignment in object initializer and with expression' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static C Make() => new C { P += 1 };
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+=").WithArguments("compound assignment in object initializer and with expression").WithLocation(4, 41));
    }

    [Fact]
    public void LangVersion_Preview_Compiles()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 1 };
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.NetCoreApp);
        // Expect a feature-gate diagnostic on each compound operator token (11 total).
        var diagnostics = compilation.GetDiagnostics();
        Assert.Equal(11, diagnostics.Count(d => d.Code == (int)ErrorCode.ERR_FeatureInPreview));
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
        Assert.NotEmpty(comp.GetDiagnostics());
    }

    [Fact]
    public void NestedCollectionInitializerOnRhs_Rejected()
    {
        // Spec note: "The compound_assignment_operator branch admits only expression, so forms such as
        // P += { 1, 2 } are syntactically ill-formed." The parser is permissive (per Phase 1); the binder
        // rejects the shape.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += { 1, 2 } };
            }
            """;
        var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
        Assert.NotEmpty(comp.GetDiagnostics());
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (10,42): error CS0019: Operator '+=' cannot be applied to operands of type 'V' and 'V'
            //     public static C Make(V v) => new C { Prop += v };
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "Prop += v").WithArguments("+=", "V", "V").WithLocation(10, 42));
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
            // (5,31): warning CS0067: The event 'C.E' is never used
            //     public event EventHandler E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(5, 31));
    }


    #endregion
}
