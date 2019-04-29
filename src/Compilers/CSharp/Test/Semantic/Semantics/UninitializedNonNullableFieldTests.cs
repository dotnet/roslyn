// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class UninitializedNonNullableFieldTests : CSharpTestBase
    {
        [Fact]
        public void NoNonNullWarnings_CSharp7()
        {
            var source =
@"class C
{
    internal object F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (3,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(3, 21));
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
                // (3,16): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(3, 16),
                // (3,16): warning CS0649: Field 'C<T>.F1' is never assigned to, and will always have its default value 
                //     internal T F1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("C<T>.F1", "").WithLocation(3, 16),
                // (5,21): warning CS8653: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //     internal T F3 = default;
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default").WithArguments("T").WithLocation(5, 21),
                // (6,21): warning CS8653: A default expression introduces a null value when 'T' is a non-nullable reference type.
                //     internal T F4 = default(T);
                Diagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, "default(T)").WithArguments("T").WithLocation(6, 21));
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
                // (5,20): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     private object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 20),
                // (7,24): warning CS8618: Non-nullable field 'F3' is uninitialized.
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
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
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
                // (5,29): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     private readonly object F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(5, 29),
                // (7,33): warning CS8618: Non-nullable field 'F3' is uninitialized.
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
                // (7,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
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

        // https://github.com/dotnet/roslyn/issues/30020: Report warnings for static fields.
        [Fact]
        [WorkItem(30020, "https://github.com/dotnet/roslyn/issues/30020")]
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
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            //// (8,12): warning CS8618: Non-nullable field 'F3' is uninitialized.
            ////     static C()
            //Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(8, 12),
            //// (8,12): warning CS8618: Non-nullable field 'F1' is uninitialized.
            ////     static C()
            //Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(8, 12));
        }

        // https://github.com/dotnet/roslyn/issues/30020: Report warnings for static fields.
        [Fact]
        [WorkItem(30020, "https://github.com/dotnet/roslyn/issues/30020")]
        public void StaticFields_ExplicitConstructor()
        {
            var source =
@"class C
{
#pragma warning disable 0169
    private static object F1;
    private static object F2;
    private readonly static object F3;
    private readonly static object F4;
    static C()
    {
        F2 = new object();
        F4 = new object();
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            //// (8,12): warning CS8618: Non-nullable field 'F3' is uninitialized.
            ////     static C()
            //Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(8, 12),
            //// (8,12): warning CS8618: Non-nullable field 'F1' is uninitialized.
            ////     static C()
            //Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F1").WithLocation(8, 12));
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
                // (7,13): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(7, 13),
                // (7,13): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F3").WithLocation(7, 13),
                // (23,14): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     internal C(object x, object y, string z) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(23, 14),
                // (23,14): warning CS8618: Non-nullable field 'F1' is uninitialized.
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
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
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
                // (7,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(7, 14),
                // (7,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     internal C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(7, 14),
                // (15,14): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     internal C(object x, object y)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(15, 14),
                // (15,14): warning CS8618: Non-nullable property 'P1' is uninitialized.
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
    private object P3 { set { } }
    private object[] P2 { get { throw new NotImplementedException(); } set { } }
    internal C()
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GenericType()
        {
            var source =
@"class C<T>
{
#pragma warning disable 0169
    private readonly T F1;
    private readonly T F2;
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable property 'P2' is uninitialized.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable property 'P3' is uninitialized.
                //     private C(T t)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P3").WithLocation(10, 13));
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
                // (10,13): warning CS8618: Non-nullable property 'P1' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P1").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable property 'P4' is uninitialized.
                //     private C()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P4").WithLocation(10, 13),
                // (10,13): warning CS8618: Non-nullable field 'F1' is uninitialized.
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
                // (11,5): warning CS8618: Non-nullable field 'F2' is uninitialized.
                //     B() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "B").WithArguments("field", "F2").WithLocation(11, 5),
                // (5,5): warning CS8618: Non-nullable field 'F1' is uninitialized.
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
                // (6,7): warning CS8618: Non-nullable field 'F1' is uninitialized.
                //     T F1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F1").WithArguments("field", "F1").WithLocation(6, 7),
                // (7,7): warning CS8618: Non-nullable field 'G1' is uninitialized.
                //     U G1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G1").WithArguments("field", "G1").WithLocation(7, 7),
                // (15,7): warning CS8618: Non-nullable field 'F3' is uninitialized.
                //     T F3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F3").WithArguments("field", "F3").WithLocation(15, 7),
                // (16,7): warning CS8618: Non-nullable field 'G3' is uninitialized.
                //     U G3;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G3").WithArguments("field", "G3").WithLocation(16, 7),
                // (20,7): warning CS8618: Non-nullable field 'F4' is uninitialized.
                //     T F4;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F4").WithArguments("field", "F4").WithLocation(20, 7),
                // (21,7): warning CS8618: Non-nullable field 'G4' is uninitialized.
                //     U G4;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "G4").WithArguments("field", "G4").WithLocation(21, 7),
                // (25,7): warning CS8618: Non-nullable field 'F5' is uninitialized.
                //     T F5;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F5").WithArguments("field", "F5").WithLocation(25, 7),
                // (26,7): warning CS8618: Non-nullable field 'G5' is uninitialized.
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
                // (6,14): warning CS8618: Non-nullable property 'P' is uninitialized.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P").WithLocation(6, 14),
                // (6,14): warning CS8618: Non-nullable field 'F' is uninitialized.
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
                // (6,5): warning CS8618: Non-nullable field 'G' is uninitialized.
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
                // (11,14): warning CS8618: Non-nullable field '_f' is uninitialized.
                //     internal C(object o) { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(11, 14),
                // (13,14): warning CS8618: Non-nullable field '_f' is uninitialized.
                //     internal C(string s)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(13, 14),
                // (19,14): warning CS8618: Non-nullable field '_f' is uninitialized.
                //     internal C(int x)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(19, 14),
                // (28,14): warning CS8618: Non-nullable field '_f' is uninitialized.
                //     internal C(char c)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_f").WithLocation(28, 14));
        }
    }
}
