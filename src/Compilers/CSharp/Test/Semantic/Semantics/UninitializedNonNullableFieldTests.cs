// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class UninitializedNonNullableFieldTests : CSharpTestBase
    {
        [Fact]
        public void UninitializedEvents()
        {
            var src = @"
using System;
class C
{
    public event Action<object?> E1;
    public event Action<object?> E2 { add { } remove { } }
#pragma warning disable 0414
    public event Action<object?> E3 = null!;
    public event Action<object?>? E4 = null;
#pragma warning restore 0414
#pragma warning disable 0626
    public extern event Action<object?> E5;
#pragma warning restore 0626

    internal C()
    {
    }

    internal C(Action<object?> e)
    {
        E1 = e;
        E2 += e;
    }

    internal C(object o)
    {
        E1 += p => {};
        E2 += p => {};
    }
}";
            var comp = CreateCompilation(src, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (15,14): warning CS8618: Non-nullable event 'E1' is uninitialized. Consider declaring the event as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("event", "E1").WithLocation(15, 14));
        }

        [Fact]
        public void NoExplicitConstructors_CSharp7_01()
        {
            var source =
@"class C
{
    internal object F;
    static object G;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (3,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(3, 21),
                // (4,19): warning CS0169: The field 'C.G' is never used
                //     static object G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(4, 19));
        }

        [Fact]
        public void NoExplicitConstructors_CSharp7_02()
        {
            var source =
@"class C
{
#nullable enable
    internal object F;
    static object G;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7, skipUsesIsNullable: true);
            comp.VerifyDiagnostics(
                // (3,2): error CS8107: Feature 'nullable reference types' is not available in C# 7.0. Please use language version 8.0 or greater.
                // #nullable enable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(3, 2),
                // (4,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(4, 21),
                // (5,19): warning CS0169: The field 'C.G' is never used
                //     static object G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(5, 19));
        }

        [Fact]
        public void NoExplicitConstructors()
        {
            var source =
@"class C
{
    internal object F;
    static object G;
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (3,21): warning CS8618: Non-nullable field 'F' is uninitialized. Consider declaring the field as nullable.
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F").WithArguments("field", "F").WithLocation(3, 21),
                // (3,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(3, 21),
                // (4,19): warning CS8618: Non-nullable field 'G' is uninitialized. Consider declaring the field as nullable.
                //     static object G;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G").WithArguments("field", "G").WithLocation(4, 19),
                // (4,19): warning CS0169: The field 'C.G' is never used
                //     static object G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(4, 19));
        }

        [Fact]
        public void ExplicitConstructors_Disabled_01()
        {
            var source =
@"#pragma warning disable 169, 649
#nullable enable
class C
{
    internal object F;
    static object G;
    static C() { }
#nullable disable
    C() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,12): warning CS8618: Non-nullable field 'G' is uninitialized. Consider declaring the field as nullable.
                //     static C() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "G").WithLocation(7, 12));
        }

        [Fact]
        public void ExplicitConstructors_Disabled_02()
        {
            var source =
@"#pragma warning disable 169, 649
#nullable enable
class C
{
    internal object F;
    static object G;
    C() { }
#nullable disable
    static C() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,5): warning CS8618: Non-nullable field 'F' is uninitialized. Consider declaring the field as nullable.
                //     C() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F").WithLocation(7, 5));
        }

        [Fact]
        public void ExplicitConstructors_Disabled_03()
        {
            var source =
@"#pragma warning disable 169, 649
#nullable disable
class C
{
    internal object F;
    static object G;
#nullable enable
    C() { }
    static C() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Constants()
        {
            var source =
@"class C
{
    private const object? C1 = null;
    internal const object C2 = null!;
    protected const string? C3 = """";
    public const string C4 = """";
}
struct S
{
    internal const string? C5 = null;
    private const string C6 = null!;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(29849, "https://github.com/dotnet/roslyn/issues/29849")]
        public void UnconstrainedGenericType()
        {
            var source =
@"internal class C<T> where T : new()
{
    internal T F1;
    internal T F2 = new T();
    internal T F3 = default;
    internal T F4 = default(T);
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (3,16): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(3, 16),
                // (3,16): warning CS0649: Field 'C<T>.F1' is never assigned to, and will always have its default value 
                //     internal T F1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("C<T>.F1", "").WithLocation(3, 16),
                // (5,21): warning CS8601: Possible null reference assignment.
                //     internal T F3 = default;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default").WithLocation(5, 21),
                // (6,21): warning CS8601: Possible null reference assignment.
                //     internal T F4 = default(T);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default(T)").WithLocation(6, 21));
        }

        [Fact]
        public void ReadWriteFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
#pragma warning disable 0649
    private object F1;
    internal object? F2;
    internal object?[] F3;
    private object[]? F4;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,20): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 20),
                // (7,24): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal object?[] F3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("field", "F3").WithLocation(7, 24));
        }

        [Fact]
        public void ReadWriteFields_ExplicitConstructors()
        {
            var source =
@"class C
{
    internal object F1;
    private object? F2;
    private object?[] F3;
    internal object[]? F4;
    internal C()
    {
    }
    internal C(object o)
    {
        F1 = o;
        F3 = new[] { o, null };
    }
    internal C(object x, object y)
    {
        F2 = x;
        F4 = new[] { x, y };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(15, 14));
        }

        [Fact]
        public void ReadOnlyFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
#pragma warning disable 0649
    private readonly object F1;
    internal readonly object? F2;
    internal readonly object?[] F3;
    private readonly object[]? F4;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,29): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private readonly object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 29),
                // (7,33): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal readonly object?[] F3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("field", "F3").WithLocation(7, 33));
        }

        [Fact]
        public void ReadOnlyFields_ExplicitConstructors()
        {
            var source =
@"class C
{
    internal readonly object F1;
    private readonly object? F2;
    private readonly object?[] F3;
    internal readonly object[]? F4;
    internal C()
    {
    }
    internal C(object o)
    {
        F1 = o;
        F3 = new[] { o, null };
    }
    internal C(object x, object y)
    {
        F2 = x;
        F4 = new[] { x, y };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(15, 14));
        }

        [Fact]
        public void FieldInitializers_DefaultConstructor()
        {
            var source =
@"class C
{
    private object F1 = new object();
    internal object? F2 = new object();
    internal object?[] F3 = new [] { new object(), null };
    private object[]? F4 = new [] { new object() };
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_DefaultConstructor_NoInitializer_Field()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F;
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (4,27): warning CS8618: Non-nullable field 'F' is uninitialized. Consider declaring the field as nullable.
                //     private static object F;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F").WithArguments("field", "F").WithLocation(4, 27));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_DefaultConstructor_NoInitializer_Property()
        {
            var source =
@"class C
{
    private static object P { get; set; }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (3,27): warning CS8618: Non-nullable property 'P' is uninitialized. Consider declaring the property as nullable.
                //     private static object P { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P").WithArguments("property", "P").WithLocation(3, 27));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_DefaultConstructor_NoInitializer_Event()
        {
            var source =
@"delegate void D();
#pragma warning disable 0067
class C
{
    private static event D E;
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (5,28): warning CS8618: Non-nullable event 'E' is uninitialized. Consider declaring the event as nullable.
                //     private static event D E;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(5, 28));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_DefaultConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F1;
    private static object F2 = new object();
    private readonly static object F3;
    private readonly static object F4 = new object();
    private static object P1 { get; }
    private static object P2 { get; } = new object();
    private static object P3 { get; set; }
    private static object P4 { get; set; } = new object();
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (4,27): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private static object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(4, 27),
                // (6,36): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     private readonly static object F3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("field", "F3").WithLocation(6, 36),
                // (8,27): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private static object P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(8, 27),
                // (10,27): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     private static object P3 { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P3").WithArguments("property", "P3").WithLocation(10, 27));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_ExplicitConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F1;
    private static object F2 = new object();
    private static object F3;
    private readonly static object F4;
    private readonly static object F5 = new object();
    private readonly static object F6;
    private static object P1 { get; }
    private static object P2 { get; } = new object();
    private static object P3 { get; }
    private static object P4 { get; set; }
    private static object P5 { get; set; } = new object();
    private static object P6 { get; set; }
    static C()
    {
        F3 = new object();
        F6 = new object();
        P3 = new object();
        P6 = new object();
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (16,12): warning CS8618: Non-nullable field 'F4' is uninitialized. Consider declaring the field as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F4").WithLocation(16, 12),
                // (16,12): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(16, 12),
                // (16,12): warning CS8618: Non-nullable property 'P4' is uninitialized. Consider declaring the property as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P4").WithLocation(16, 12),
                // (16,12): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(16, 12));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        [WorkItem(37511, "https://github.com/dotnet/roslyn/issues/37511")]
        public void StaticFields_GenericTypes()
        {
            var source =
@"#pragma warning disable 0169
using System.Diagnostics.CodeAnalysis;
class C<T, U, V>
    where U : class
    where V : struct
{
    private static T P1 { get; }
    private static T P2 { get; } = default!;
    [MaybeNull] private static T P3 { get; }
    [MaybeNull] private static T P4 { get; } = default;
    private static U P5 { get; set; }
    private static U P6 { get; set; } = default!;
    private static U? P7 { get; }
    private static U? P8 { get; } = default;
    private static V P9 { get; set; }
    private static V P10 { get; set; } = default;
    private static V? P11 { get; }
    private static V? P12 { get; } = default;
}";
            var comp = CreateCompilation(new[] { MaybeNullAttributeDefinition, source }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (7,22): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private static T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(7, 22),
                // (9,34): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     [MaybeNull] private static T P3 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P3").WithArguments("property", "P3").WithLocation(9, 34),
                // (10,48): warning CS8601: Possible null reference assignment.
                //     [MaybeNull] private static T P4 { get; } = default;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default").WithLocation(10, 48),
                // (11,22): warning CS8618: Non-nullable property 'P5' is uninitialized. Consider declaring the property as nullable.
                //     private static U P5 { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P5").WithArguments("property", "P5").WithLocation(11, 22));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticEvents()
        {
            var source =
@"#pragma warning disable 0067
#pragma warning disable 0414
delegate void D();
class C
{
    private static event D E1;
    private static event D E2 = null!;
    private static event D E3;
    private static event D E4;
    private static event D? E5;
    private static event D? E6 = null;
    static C()
    {
        D d = () => { };
        E1 = d;
        E3 += d;
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (12,12): warning CS8618: Non-nullable event 'E4' is uninitialized. Consider declaring the event as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("event", "E4").WithLocation(12, 12));
        }

        [Fact]
        [WorkItem(34668, "https://github.com/dotnet/roslyn/issues/34668")]
        public void StaticFields_NotAllPaths()
        {
            var source =
@"#pragma warning disable 0169
class C
{
    private static bool F() => true;
    private static object F1;
    private static object F2;
    private static object F3;
    static C()
    {
        if (F()) F1 = new object();
        else F2 = new object();
        F3 = new object();
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (8,12): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(8, 12),
                // (8,12): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     static C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(8, 12));
        }

        [Fact]
        public void NestedType()
        {
            var source =
@"#pragma warning disable 0067
#pragma warning disable 0169
#pragma warning disable 0414
delegate void D();
struct S
{
    class C
    {
        static object F1;
        static object P1 { get; }
        static event D E1;
        object F2;
        object P2 { get; }
        event D E2;
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (9,23): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //         static object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(9, 23),
                // (10,23): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //         static object P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(10, 23),
                // (11,24): warning CS8618: Non-nullable event 'E1' is uninitialized. Consider declaring the event as nullable.
                //         static event D E1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E1").WithArguments("event", "E1").WithLocation(11, 24),
                // (12,16): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //         object F2;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F2").WithArguments("field", "F2").WithLocation(12, 16),
                // (13,16): warning CS8618: Non-nullable property 'P2' is uninitialized. Consider declaring the property as nullable.
                //         object P2 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P2").WithArguments("property", "P2").WithLocation(13, 16),
                // (14,17): warning CS8618: Non-nullable event 'E2' is uninitialized. Consider declaring the event as nullable.
                //         event D E2;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E2").WithArguments("event", "E2").WithLocation(14, 17));
        }

        // Each constructor is handled in isolation.
        [Fact]
        public void ChainedConstructors()
        {
            var source =
@"class C
{
    private object F1;
    private object F2;
    private readonly object F3;
    private object F4 = new object();
    private C()
    {
        F1 = new object();
    }
    internal C(object x) : this()
    {
        F2 = x;
    }
    internal C(object x, object y) : this(x)
    {
        F3 = y;
    }
    internal C(object x, object y, object z) : this()
    {
        F3 = z;
    }
    internal C(object x, object y, string z) : base()
    {
        F3 = z;
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(7, 13),
                // (7,13): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 13),
                // (23,14): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y, string z) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(23, 14),
                // (23,14): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object x, object y, string z) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(23, 14));
        }

        [Fact]
        public void ReadWriteAutoProperties_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; set; }
    internal object? P2 { get; set; }
    internal object?[] P3 { get; set; }
    private object[]? P4 { get; set; }
    internal C()
    {
    }
    internal C(object o)
    {
        P1 = o;
        P3 = new[] { o,  null };
    }
    internal C(object x, object y)
    {
        P2 = x;
        P4 = new[] { x, y };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(15, 14));
        }

        [Fact]
        public void ReadOnlyAutoProperties_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; }
    internal object? P2 { get; }
    internal object?[] P3 { get; }
    private object[]? P4 { get; }
    internal C()
    {
    }
    internal C(object o)
    {
        P1 = o;
        P3 = new[] { o,  null };
    }
    internal C(object x, object y)
    {
        P2 = x;
        P4 = new[] { x, y };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(15, 14));
        }

        [Fact]
        public void AutoPropertyInitializers_DefaultConstructor()
        {
            var source =
@"class C
{
    private object P1 { get; } = new object();
    internal object P2 { get; set; } = new object();
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AutoPropertyInitializers_ExplicitConstructors()
        {
            var source =
@"class C
{
    private object P1 { get; } = new object();
    internal object?[] P2 { get; } = new object?[0];
    internal object P3 { get; set; } = new object();
    private object?[] P4 { get; set; } = new object?[0];
    internal C(object o)
    {
        P1 = o;
        P2 = new object?[] { o };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Properties_ExplicitImplementations()
        {
            var source =
@"using System;
class C
{
    private object P1 { get { throw new NotImplementedException(); } }
    private object[] P2 { get { throw new NotImplementedException(); } set { } }
    private object P3 { set { } }
    private static object P4 { get { throw new NotImplementedException(); } }
    private static object P5 { set { } }
    internal C()
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(37511, "https://github.com/dotnet/roslyn/issues/37511")]
        public void GenericType()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
class C<T>
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T F2;
    [MaybeNull] private readonly T F3;
    private T P1 { get; }
    private T P2 { get; set; }
    internal T P3 { get; }
    internal T P4 { get; set; }
    private C(T t)
    {
        F1 = t;
        P1 = t;
        P4 = t;
    }
}";
            var comp = CreateCompilation(new[] { MaybeNullAttributeDefinition, source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(12, 13),
                // (12,13): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(12, 13),
                // (12,13): warning CS8618: Non-nullable property 'P2' is uninitialized. Consider declaring the property as nullable.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(12, 13),
                // (12,13): warning CS8618: Non-nullable property 'P3' is uninitialized. Consider declaring the property as nullable.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(12, 13));
        }

        [Fact]
        public void GenericType_ClassConstraint()
        {
            var source =
@"class C<T> where T : class
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T? F2;
    private T P1 { get; }
    private T? P2 { get; set; }
    internal T? P3 { get; }
    internal T P4{ get; set; }
    private C()
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable property 'P4' is uninitialized. Consider declaring the property as nullable.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P4").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(10, 13));
        }

        [Fact]
        public void GenericType_StructConstraint()
        {
            var source =
@"class C<T> where T : struct
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T? F2;
    private C()
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [WorkItem(29065, "https://github.com/dotnet/roslyn/issues/29065")]
        [WorkItem(30021, "https://github.com/dotnet/roslyn/issues/30021")]
        [Fact]
        public void GenericType_NonNullTypes()
        {
            var source =
@"#pragma warning disable 0169
class A<T>
{
    T F1; // warning: uninitialized
    A() { }
}
class B<T> where T : class
{
    T F2; // warning: uninitialized
    T? F3;
    B() { }
}
class C<T> where T : struct
{
    T F4;
    T? F5;
    C() { }
}";

            // [NonNullTypes(true)]
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,5): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     B() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "B").WithArguments("field", "F2").WithLocation(11, 5),
                // (5,5): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     A() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "A").WithArguments("field", "F1").WithLocation(5, 5));

            // [NonNullTypes(false)]
            comp = CreateCompilation(new[] { source }, options: WithNonNullTypesFalse(), parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (10,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     T? F3;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 6),
                // (10,5): error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type. Consider adding a 'class', 'struct', or type constraint.
                //     T? F3;
                Diagnostic(ErrorCode.ERR_NullableUnconstrainedTypeParameter, "T?").WithLocation(10, 5)
            );

            // [NonNullTypes] missing
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     T? F3;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 6),
                // (10,5): error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type. Consider adding a 'class', 'struct', or type constraint.
                //     T? F3;
                Diagnostic(ErrorCode.ERR_NullableUnconstrainedTypeParameter, "T?").WithLocation(10, 5)
            );

            // https://github.com/dotnet/roslyn/issues/29976: Test with [NonNullTypes(Warnings=false)].
        }

        // https://github.com/dotnet/roslyn/issues/29976: Test `where T : unmanaged`.
        [Fact]
        public void TypeParameterConstraints()
        {
            var source =
@"#pragma warning disable 0169
interface I { }
class A { }
class C1<T, U> where U : T
{
    T F1;
    U G1;
}
class C2<T> where T : struct
{
    T F2;
}
class C3<T, U> where T : class where U : T
{
    T F3;
    U G3;
}
class C4<T, U> where T : I where U : T
{
    T F4;
    U G4;
}
class C5<T, U> where T : A where U : T
{
    T F5;
    U G5;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,7): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(6, 7),
                // (7,7): warning CS8618: Non-nullable field 'G1' is uninitialized. Consider declaring the field as nullable.
                //     U G1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G1").WithArguments("field", "G1").WithLocation(7, 7),
                // (15,7): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     T F3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("field", "F3").WithLocation(15, 7),
                // (16,7): warning CS8618: Non-nullable field 'G3' is uninitialized. Consider declaring the field as nullable.
                //     U G3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G3").WithArguments("field", "G3").WithLocation(16, 7),
                // (20,7): warning CS8618: Non-nullable field 'F4' is uninitialized. Consider declaring the field as nullable.
                //     T F4;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F4").WithArguments("field", "F4").WithLocation(20, 7),
                // (21,7): warning CS8618: Non-nullable field 'G4' is uninitialized. Consider declaring the field as nullable.
                //     U G4;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G4").WithArguments("field", "G4").WithLocation(21, 7),
                // (25,7): warning CS8618: Non-nullable field 'F5' is uninitialized. Consider declaring the field as nullable.
                //     T F5;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F5").WithArguments("field", "F5").WithLocation(25, 7),
                // (26,7): warning CS8618: Non-nullable field 'G5' is uninitialized. Consider declaring the field as nullable.
                //     U G5;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G5").WithArguments("field", "G5").WithLocation(26, 7));
        }

        [Fact]
        public void Tuple()
        {
            var source =
@"class C
{
#pragma warning disable 0649
    internal readonly (object A, object B) F1;
    internal readonly (object? A, object) F2;
    internal readonly (object, object? B) F3;
    internal readonly (object?, object?) F4;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotInitializedInAllPaths_Class()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; }
    internal C(string s)
    {
        if (s.Length > 0)
            F = s;
        else
            P = new [] { s };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,14): warning CS8618: Non-nullable property 'P' is uninitialized. Consider declaring the property as nullable.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P").WithLocation(6, 14),
                // (6,14): warning CS8618: Non-nullable field 'F' is uninitialized. Consider declaring the field as nullable.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F").WithLocation(6, 14));
        }

        [Fact]
        public void NotInitializedInAllPaths_Struct()
        {
            var source =
@"struct S
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; set; }
    internal S(string s)
    {
        if (s.Length > 0)
            F = s;
        else
            P = new [] { s };
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,14): error CS0843: Auto-implemented property 'S.P' must be fully assigned before control is returned to the caller.
                //     internal S(string s)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S").WithArguments("S.P").WithLocation(6, 14),
                // (6,14): error CS0171: Field 'S.F' must be fully assigned before control is returned to the caller
                //     internal S(string s)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S").WithArguments("S.F").WithLocation(6, 14));
        }

        [Fact]
        public void EmptyStruct()
        {
            var source =
@"struct S
{
    S(object o)
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(30022, "https://github.com/dotnet/roslyn/issues/30022")]
        public void StructAssignThis()
        {
            var source =
@"struct S
{
#pragma warning disable 0169
    private readonly string F;
    private string[] P { get; set; }
    internal S(S s)
    {
        this = s;
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StructObjectCreation()
        {
            var source =
@"#pragma warning disable 0649
struct S
{
    internal string F;
    internal string G;
}
class C
{
    static void F(S s) { }
    static void Main()
    {
        F(new S());
        F(new S() { F = string.Empty });
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ValueTypeFields()
        {
            var source =
@"#pragma warning disable 0169
struct S { }
class C
{
    private readonly S s;
    private int i;
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ValueType_NoConstructors()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
struct S
{
    object F1;
    static object F2;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     static object F2;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F2").WithArguments("field", "F2").WithLocation(6, 19));
        }

        [Fact]
        public void ValueType_InstanceConstructor()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
struct S
{
    object F1;
    static object F2;
    S(int i)
    {
        F1 = new object();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     static object F2;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F2").WithArguments("field", "F2").WithLocation(6, 19));
        }

        [Fact]
        public void ValueType_StaticConstructor()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
struct S
{
    object F1;
    static object F2;
    static object F3;
    static S()
    {
        F2 = new object();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,12): warning CS8618: Non-nullable field 'F3' is uninitialized. Consider declaring the field as nullable.
                //     static S()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "F3").WithLocation(8, 12));
        }

        [Fact]
        public void Interface()
        {
            var source =
@"#nullable enable
interface I
{
    object F1;
    public static object F2;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,12): error CS0525: Interfaces cannot contain instance fields
                //     object F1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 12),
                // (4,12): warning CS0649: Field 'I.F1' is never assigned to, and will always have its default value null
                //     object F1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("I.F1", "null").WithLocation(4, 12),
                // (5,26): error CS8701: Target runtime doesn't support default interface implementation.
                //     public static object F2;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "F2").WithLocation(5, 26),
                // (5,26): warning CS0649: Field 'I.F2' is never assigned to, and will always have its default value null
                //     public static object F2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("I.F2", "null").WithLocation(5, 26));
        }

        [Fact]
        public void Enum()
        {
            var source =
@"#nullable enable
enum E
{
    A,
    B = A,
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LocalFunction()
        {
            var source =
@"#pragma warning disable 0169
class C
{
    private object F;
    private object G;
    C()
    {
        void L(object o)
        {
            F = o;
        }
        L(new object());
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,5): warning CS8618: Non-nullable field 'G' is uninitialized. Consider declaring the field as nullable.
                //     C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "G").WithLocation(6, 5));
        }

        [Fact]
        [WorkItem(25529, "https://github.com/dotnet/roslyn/issues/25529")]
        public void UnassignedNonNullFieldsUnreachable()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    private object _f;
    internal C()
    {
        throw new NotImplementedException();
    }

    internal C(object o) { }

    internal C(string s)
    {
        return;
        throw new NotImplementedException();
    }

    internal C(int x)
    {
        if (x == 0)
        {
            return;
        }
        throw new NotImplementedException();
    }

    internal C(char c)
    {
        return;
    }
}
", options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (5,20): warning CS0169: The field 'C._f' is never used
                //     private object _f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(5, 20),
                // (11,14): warning CS8618: Non-nullable field '_f' is uninitialized. Consider declaring the field as nullable.
                //     internal C(object o) { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(11, 14),
                // (13,14): warning CS8618: Non-nullable field '_f' is uninitialized. Consider declaring the field as nullable.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(13, 14),
                // (19,14): warning CS8618: Non-nullable field '_f' is uninitialized. Consider declaring the field as nullable.
                //     internal C(int x)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(19, 14),
                // (28,14): warning CS8618: Non-nullable field '_f' is uninitialized. Consider declaring the field as nullable.
                //     internal C(char c)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(28, 14));
        }
    }
}
