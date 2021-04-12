﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            var comp = CreateCompilation(src, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (15,14): warning CS8618: Non-nullable event 'E1' is uninitialized. Consider declaring the event as nullable.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("event", "E1").WithLocation(15, 14));
        }

        [Fact]
        public void Event_InitialState()
        {
            var src = @"
using System;
class C
{
    public event Action E1 = () => { };
    public event Action E2;
    internal C()
    {
        E1.Invoke();
        E2.Invoke(); // 1
    }
}";
            var comp = CreateCompilation(src, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         E2.Invoke(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E2").WithLocation(10, 9));
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable());
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable());
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
        public void FieldInitializer_Simple_01()
        {
            var source = @"
class C
{
    string field = ""hello"";
    public C()
    {
        field.ToString();
    }
}
";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void FieldInitializer_Simple_02()
        {
            var source = @"
class C
{
    string Prop { get; set; } = ""hello"";
    public C()
    {
        Prop.ToString();
    }
}
";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void PropertyInitializer_AllowNullT_01()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class C<T>
{
    [AllowNull]
    T Prop { get; set; }
    public C()
    {
        Prop = default;
    }
}
";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void PropertyInitializer_AllowNullT_02()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class C<T>
{
    [AllowNull]
    T Prop { get; set; }
    public C()
    {
    }
}
";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void FieldInitializer_AllowNullT_01()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class C<T>
{
    [AllowNull]
    T field;
    public C()
    {
        field = default;
    }
}
";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (7,7): warning CS0414: The field 'C<T>.field' is assigned but its value is never used
                //     T field;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C<T>.field").WithLocation(7, 7)
                );
        }

        [Fact]
        public void FieldInitializer_AllowNullT_02()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class C<T>
{
    [AllowNull]
    T field;
    public C()
    {
    }
}
";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (7,7): warning CS0169: The field 'C<T>.field' is never used
                //     T field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("C<T>.field").WithLocation(7, 7)
                );
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
            var comp = CreateCompilation(new[] { MaybeNullAttributeDefinition, source }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (7,22): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private static T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(7, 22),
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(source, options: WithNullableEnable());
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
        public void StructConstructorInitializer_UninitializedProperty()
        {
            var source = @"
struct S1
{
    public string Prop { get; set; }
    public S1(string s) // 1
    {
        Prop.ToString(); // 2
    }

    public S1(object obj) : this()
    {
        Prop.ToString(); // 3
    }

    public S1(object obj1, object obj2) : this() // 4
    {
    }

    public S1(string s1, string s2) : this(s1)
    {
        Prop.ToString();
    }
}
";

            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (5,12): error CS0843: Auto-implemented property 'S1.Prop' must be fully assigned before control is returned to the caller.
                //     public S1(string s) // 1
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S1").WithArguments("S1.Prop").WithLocation(5, 12),
                // (7,9): error CS8079: Use of possibly unassigned auto-implemented property 'Prop'
                //         Prop.ToString(); // 2
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Prop").WithArguments("Prop").WithLocation(7, 9),
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         Prop.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop").WithLocation(12, 9),
                // (15,12): warning CS8618: Non-nullable property 'Prop' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public S1(object obj1, object obj2) : this() // 4
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S1").WithArguments("property", "Prop").WithLocation(15, 12));
        }

        [Fact, WorkItem(48574, "https://github.com/dotnet/roslyn/issues/48574")]
        public void StructConstructorInitializer_UninitializedField()
        {
            var source = @"
struct S1
{
    public string field; // 0
    public S1(string s) // 1
    {
        field.ToString(); // 2
    }

    public S1(object obj) : this()
    {
        field.ToString(); // 3
    }

    public S1(object obj1, object obj2) : this() // 4
    {
    }

    public S1(string s1, string s2) : this(s1)
    {
        field.ToString();
    }
}
";

            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,19): warning CS0649: Field 'S1.field' is never assigned to, and will always have its default value null
                //     public string field; // 0
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("S1.field", "null").WithLocation(4, 19),
                // (5,12): error CS0171: Field 'S1.field' must be fully assigned before control is returned to the caller
                //     public S1(string s) // 1
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.field").WithLocation(5, 12),
                // (7,9): error CS0170: Use of possibly unassigned field 'field'
                //         field.ToString(); // 2
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "field").WithArguments("field").WithLocation(7, 9),
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         field.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "field").WithLocation(12, 9),
                // (15,12): warning CS8618: Non-nullable field 'field' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S1(object obj1, object obj2) : this() // 4
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S1").WithArguments("field", "field").WithLocation(15, 12)
                );
        }

        [Fact, WorkItem(43215, "https://github.com/dotnet/roslyn/issues/43215")]
        public void FieldInitializer_CallWithOutParam()
        {
            var source = @"
class C
{
    static string field1;
    static string field2 = M(out field1);

    public static string M(out string param1)
    {
        param1 = ""hello"";
        return ""world"";
    }
}";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics();
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { MaybeNullAttributeDefinition, source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(12, 13),
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,5): warning CS8618: Non-nullable field 'F2' is uninitialized. Consider declaring the field as nullable.
                //     B() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "B").WithArguments("field", "F2").WithLocation(11, 5),
                // (5,5): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     A() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "A").WithArguments("field", "F1").WithLocation(5, 5));

            // [NonNullTypes(false)]
            comp = CreateCompilation(new[] { source }, options: WithNullableDisable(), parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (10,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     T? F3;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 6),
                // (10,5): error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type unless language version '9.0' or greater is used. Consider changing the language version or adding a 'class', 'struct', or type constraint.
                //     T? F3;
                Diagnostic(ErrorCode.ERR_NullableUnconstrainedTypeParameter, "T?").WithArguments("9.0").WithLocation(10, 5));

            // [NonNullTypes] missing
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     T? F3;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 6),
                // (10,5): error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type unless language version '9.0' or greater is used. Consider changing the language version or adding a 'class', 'struct', or type constraint.
                //     T? F3;
                Diagnostic(ErrorCode.ERR_NullableUnconstrainedTypeParameter, "T?").WithArguments("9.0").WithLocation(10, 5));

            // https://github.com/dotnet/roslyn/issues/29976: Test with [NonNullTypes(Warnings=false)].
        }

        [Fact]
        public void GenericType_NoConstraint()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
class C<T>
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,15): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 15),
                // (7,15): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(7, 15),
                // (11,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? F4;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(11, 14),
                // (13,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? P4 { get; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 14));
        }

        [Fact]
        public void GenericType_NullableClassConstraint()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
class C<T> where T : class?
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,15): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 15),
                // (7,15): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(7, 15),
                // (11,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? F4;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(11, 14),
                // (13,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? P4 { get; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 14));
        }

        [Fact]
        public void GenericType_NotNullConstraint()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
class C<T> where T : notnull
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,15): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 15),
                // (7,15): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(7, 15),
                // (11,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? F4;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(11, 14),
                // (13,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? P4 { get; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 14));
        }

        [Fact]
        public void GenericType_UnmanagedConstraint()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
class C<T> where T : unmanaged
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GenericType_InterfaceConstraint_01()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
interface I { }
class C<T> where T : I
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,15): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(6, 15),
                // (8,15): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(8, 15),
                // (12,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? F4;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(12, 14),
                // (14,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? P4 { get; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 14));
        }

        [Fact]
        public void GenericType_InterfaceConstraint_02()
        {
            var source =
@"#nullable enable
#pragma warning disable 0169
interface I { }
class C<T> where T : I?
{
    private T F1;
    private T? F2;
    private T P1 { get; }
    private T? P2 { get; }
#nullable disable
    private T F3;
    private T? F4;
    private T P3 { get; }
    private T? P4 { get; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,15): warning CS8618: Non-nullable field 'F1' is uninitialized. Consider declaring the field as nullable.
                //     private T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(6, 15),
                // (8,15): warning CS8618: Non-nullable property 'P1' is uninitialized. Consider declaring the property as nullable.
                //     private T P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(8, 15),
                // (12,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? F4;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(12, 14),
                // (14,14): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private T? P4 { get; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 14));
        }

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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
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
        public void Interface_01()
        {
            var source =
@"#nullable enable
public interface I
{
    public object F1; // 1
    public static object F2; // 2
    public static object F3 { get; set; } // 3
    public static event System.Action E1; // 4, 5
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,19): error CS0525: Interfaces cannot contain instance fields
                //     public object F1; // 1
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 19),
                // (5,26): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public static object F2; // 2
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F2").WithArguments("field", "F2").WithLocation(5, 26),
                // (6,26): warning CS8618: Non-nullable property 'F3' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public static object F3 { get; set; } // 3
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("property", "F3").WithLocation(6, 26),
                // (7,39): warning CS8618: Non-nullable event 'E1' must contain a non-null value when exiting constructor. Consider declaring the event as nullable.
                //     public static event System.Action E1; // 4, 5
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E1").WithArguments("event", "E1").WithLocation(7, 39),
                // (7,39): warning CS0067: The event 'I.E1' is never used
                //     public static event System.Action E1; // 4, 5
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("I.E1").WithLocation(7, 39)
                );
        }

        [Fact]
        public void Interface_02()
        {
            var source =
@"#nullable enable
public interface I
{
    public static object F1;
    public static object F2 { get; set; }
    public static event System.Action E1; // 1

    public static object F3 = new object();
    public static object F4 { get; set; } = new object();
    public static event System.Action E2 = () => {};

    public static object F5;
    public static object F6 { get; set; }
    public static event System.Action E3;

    static I() // 2, 3, 4
    {
        F5 = new object();
        F6 = new object();
        E3 = () => {};
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,39): warning CS0067: The event 'I.E1' is never used
                //     public static event System.Action E1; // 1
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("I.E1").WithLocation(6, 39),
                // (16,12): warning CS8618: Non-nullable property 'F2' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     static I() // 2, 3, 4
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "I").WithArguments("property", "F2").WithLocation(16, 12),
                // (16,12): warning CS8618: Non-nullable event 'E1' must contain a non-null value when exiting constructor. Consider declaring the event as nullable.
                //     static I() // 2, 3, 4
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "I").WithArguments("event", "E1").WithLocation(16, 12),
                // (16,12): warning CS8618: Non-nullable field 'F1' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     static I() // 2, 3, 4
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "I").WithArguments("field", "F1").WithLocation(16, 12)
                );
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
            // Null state does not flow out of local functions https://github.com/dotnet/roslyn/issues/45770
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,5): warning CS8618: Non-nullable field 'G' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "G").WithLocation(6, 5),
                // (6,5): warning CS8618: Non-nullable field 'F' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F").WithLocation(6, 5));
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
", options: WithNullableEnable());
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

        [Fact]
        [WorkItem(43523, "https://github.com/dotnet/roslyn/issues/43523")]
        [WorkItem(44046, "https://github.com/dotnet/roslyn/issues/44046")]
        public void IndirectInitialization_WithAssertsOrThrows()
        {
            var source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C1
{
    public string Prop { get; set; }
    void Init() => Prop = ""hello"";
    public C1() // 1
    {
        Init();
    }
}

class C2
{
    public string Prop { get; set; }
    void Init() => Prop = ""hello"";

    static void MyAssert([DoesNotReturnIf(false)] bool b) { if (!b) throw null!; }

    public C2()
    {
        Init();
        MyAssert(Prop is object);
    }
}

class C3
{
    public string Prop { get; set; }
    void Init() => Prop = ""hello"";
    public C3()
    {
        Init();
        if (Prop is null)
        {
            throw new Exception();
        }
    }
}";
            var comp = CreateCompilation(new[] { source, DoesNotReturnIfAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (9,12): warning CS8618: Non-nullable property 'Prop' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public C1() // 1
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C1").WithArguments("property", "Prop").WithLocation(9, 12));
        }

        [Fact]
        [WorkItem(41110, "https://github.com/dotnet/roslyn/issues/41110")]
        public void TrackMemberStateAcrossInitializers()
        {
            var source = @"
class C
{
  static string? P1 { get; set; } = """";

  static string P2 { get; set; } = P1;
  static string f1 = P1;
}

class D
{
  static string? f1 = """";

  static string f2 = f1;
  static string P1 { get; set; } = f1;
}";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44180, "https://github.com/dotnet/roslyn/issues/44180")]
        public void MemberNotNull_PropertiesFields()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class Property
{
  public string P { get; set; }

  public Property() { Init(); P.ToString(); }

  [MemberNotNull(nameof(P))]  void Init() => P = """";
}

class Field
{
  public string F;

  public Field() { Init(); F.ToString(); }

  [MemberNotNull(nameof(F))] void Init() => F = """";
}";
            var comp = CreateCompilation(new[] { source, MemberNotNullAttributeDefinition }, options: WithNullableEnable(), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(41296, "https://github.com/dotnet/roslyn/issues/41296")]
        public void InitializeInTryAndInCatch()
        {
            var source = @"
using System;
public class C
{
    string field;

    public C()
    {
        try
        {
            M2(out field);
        }
        catch (Exception)
        {
           if (field is null)
           {
              field  =  """";
           }
        }
    }

    static void M2(out string s) => throw null!;
}";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44212, "https://github.com/dotnet/roslyn/issues/44212")]
        public void InitializeUsingNullCoalescingAssignment()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    string f;

    public C()
    {
        Prop ??= """";
        f.ToString();
    }

    public C(byte b)
    {
        if (Prop == null)
        {
            Prop = """";
        }
        f.ToString();
    }

    [MemberNotNull(nameof(f))]
    string? Prop
    {
        get => f = """";
        set => f = value ?? """";
    }
}
";
            var comp = CreateCompilation(new[] { source, MemberNotNullAttributeDefinition }, options: WithNullableEnable(), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void BaseMembersHaveDeclaredStateInDerivedCtor()
        {
            var source = @"
class Base
{
    public string BaseProp { get; set; } // 1
}

class Derived : Base
{
    string DerivedProp { get; set; }

    public Derived()
    {
        BaseProp.ToString();
        DerivedProp.ToString(); // 2
    }
}
";
            var comp = CreateCompilation(source, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,19): warning CS8618: Non-nullable property 'BaseProp' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public string BaseProp { get; set; } // 1
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "BaseProp").WithArguments("property", "BaseProp").WithLocation(4, 19),
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         DerivedProp.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "DerivedProp").WithLocation(14, 9));
        }

        [Fact]
        public void NullableEnableWarnings_InitialState()
        {
            var source = @"
#nullable enable warnings
class C
{
    public string Prop { get; set; }
    public C()
    {
        Prop.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableEnableWarnings_NoExitWarning()
        {
            var source = @"
#nullable enable warnings
class C
{
    public string Prop { get; set; }
    public C()
    {
        Prop = null;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotNullIfNotNull_StaticInitializers_01()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    static string Field1 = M(Field2); // 1
    static string Field2 = M(Field1); // 2

    [return: NotNullIfNotNull(""input"")]
    public static string? M(string? input) => input;
}
";
            var comp = CreateCompilation(new[] { source, NotNullIfNotNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (6,28): warning CS8601: Possible null reference assignment.
                //     static string Field1 = M(Field2); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M(Field2)").WithLocation(6, 28),
                // (7,28): warning CS8601: Possible null reference assignment.
                //     static string Field2 = M(Field1); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M(Field1)").WithLocation(7, 28)
                );
        }

        [Fact]
        public void NotNullIfNotNull_StaticInitializers_02()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    static string? Field1 = ""a"";
    static string Field2 = M(Field1);

    [return: NotNullIfNotNull(""input"")]
    public static string? M(string? input) => input;
}
";
            var comp = CreateCompilation(new[] { source, NotNullIfNotNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotNullIfNotNull_StaticInitializers_03()
        {
            var source = @"
public class C
{
    static string Field1 = Field2.ToString(); // 1
    static string Field2 = ""a"";
}
";
            var comp = CreateCompilation(new[] { source, NotNullIfNotNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,28): warning CS8602: Dereference of a possibly null reference.
                //     static string Field1 = Field2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field2").WithLocation(4, 28));
        }

        [Fact]
        [WorkItem(46121, "https://github.com/dotnet/roslyn/issues/46121")]
        public void StaticInitializers_MultipleFiles_01()
        {
            var source1 = @"
partial class C
{
    static readonly string s1;
}";
            var source2 = @"
partial class C
{
    static C()
    {
        s1 = ""hello"";
    }
}
";
            var comp = CreateCompilation(new[] { source1, source2 }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,28): warning CS0414: The field 'C.s1' is assigned but its value is never used
                //     static readonly string s1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 28)
                );
        }

        [Fact]
        [WorkItem(46121, "https://github.com/dotnet/roslyn/issues/46121")]
        public void StaticInitializers_MultipleFiles_02()
        {
            var source1 = @"
partial class C
{
    static readonly string Field1 = Field2.ToString(); // 1
}";
            var source2 = @"
partial class C
{
    static readonly string Field2 = ""a"";
}
";
            var comp = CreateCompilation(new[] { source1, source2 }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,37): warning CS8602: Dereference of a possibly null reference.
                //     static readonly string Field1 = Field2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field2").WithLocation(4, 37));
        }

        [Fact]
        [WorkItem(1090263, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/1090263")]
        public void PropertyNoGetter()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public string P { }
    public string P2 { set { } }
    public string P3 { } = string.Empty;
    public C()
    {
        P = """";
        Console.WriteLine(P2);
        P2 += """";
    }
}", options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (5,19): error CS0548: 'C.P': property or indexer must have at least one accessor
                //     public string P { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P").WithArguments("C.P").WithLocation(5, 19),
                // (7,19): error CS0548: 'C.P3': property or indexer must have at least one accessor
                //     public string P3 { } = string.Empty;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P3").WithArguments("C.P3").WithLocation(7, 19),
                // (10,9): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //         P = "";
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(10, 9),
                // (11,27): error CS0154: The property or indexer 'C.P2' cannot be used in this context because it lacks the get accessor
                //         Console.WriteLine(P2);
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P2").WithArguments("C.P2").WithLocation(11, 27),
                // (12,9): error CS0154: The property or indexer 'C.P2' cannot be used in this context because it lacks the get accessor
                //         P2 += "";
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P2").WithArguments("C.P2").WithLocation(12, 9)
            );
        }

        [Fact]
        public void MaybeNullT_Uninitialized()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
public class C<T>
{
    [MaybeNull] public T F;
    [MaybeNull] public T P { get; set; }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void MaybeNull_ClassT_Uninitialized()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
public class C<T> where T : class
{
    [MaybeNull] public T F;
    [MaybeNull] public T P { get; set; }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void MaybeNull_NotNullT_Uninitialized()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
public class C<T> where T : notnull
{
    [MaybeNull] public T F;
    [MaybeNull] public T P { get; set; }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void MaybeNull_Uninitialized()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
public class C
{
    [MaybeNull] public string F;
    [MaybeNull] public string P { get; set; }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void MaybeNull_NullInitializer()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
public class C
{
    [MaybeNull] public string F = null;
    [MaybeNull] public string P { get; set; } = null;
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition }, options: WithNullableEnable());
            comp.VerifyDiagnostics(
                // (4,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     [MaybeNull] public string F = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 35),
                // (5,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     [MaybeNull] public string P { get; set; } = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 49)
                );
        }
    }
}
