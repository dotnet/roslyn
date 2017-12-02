﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking : CSharpTestBase
    {
        private const string NullableAttributeDefinition = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Event | // The type of the event is nullable, or has a nullable reference type as one of its constituents  
                    AttributeTargets.Field | // The type of the field is a nullable reference type, or has a nullable reference type as one of its constituents  
                    AttributeTargets.GenericParameter | // The generic parameter is a nullable reference type
                    AttributeTargets.Module | // Nullable reference types in this module are annotated by means of NullableAttribute applied to other targets in it
                    AttributeTargets.Parameter | // The type of the parameter is a nullable reference type, or has a nullable reference type as one of its constituents  
                    AttributeTargets.ReturnValue | // The return type is a nullable reference type, or has a nullable reference type as one of its constituents  
                    AttributeTargets.Property | // The type of the property is a nullable reference type, or has a nullable reference type as one of its constituents 
                    AttributeTargets.Class , // Base type has a nullable reference type as one of its constituents
                   AllowMultiple = false)]
    public class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] transformFlags)
        {
        }
    }
}
";

        private const string NullableOptOutAttributesDefinition = @"
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Opt out of nullability warnings that could originate from definitions in the given assembly. 
    /// The attribute is not preserved in metadata and ignored if present in metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    class NullableOptOutForAssemblyAttribute : Attribute
    {
        /// <param name=""assemblyName"">An assembly name - a simple name plus its PublicKey, if any.""/></param>
        public NullableOptOutForAssemblyAttribute(string assemblyName) { }
    }

    /// <summary>
    /// Opt-out or opt into nullability warnings that could originate from source code and definition(s) ...
    /// </summary>
    [AttributeUsage(AttributeTargets.Module | // in this module. If nullable reference types feature is enabled, the warnings are opted into on the module level by default
                    AttributeTargets.Class | // in this class
                    AttributeTargets.Constructor | // of this constructor
                    AttributeTargets.Delegate | // of this delegate
                    AttributeTargets.Event | // of this event
                    AttributeTargets.Field | // of this field
                    AttributeTargets.Interface | // in this interface
                    AttributeTargets.Method | // of this method
                    AttributeTargets.Property | // of this property
                    AttributeTargets.Struct, // in this structure
                    AllowMultiple = false)]
    class NullableOptOutAttribute : Attribute
    {
        public NullableOptOutAttribute(bool flag = true) { }
    }
}
";

        [Fact]
        public void Test0()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
        string? x = null;
    }
}
", parseOptions: TestOptions.Regular7);

            c.VerifyDiagnostics(
                 // (6,9): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //         string? x = null;
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string?").WithArguments("System.Nullable<T>", "T", "string").WithLocation(6, 9),
                 // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
                 //         string? x = null;
                 Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void NullableAttribute_NotRequiredCSharp7_01()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    static async Task<string> F()
    {
        return await Task.FromResult(default(string));
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular7);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NullableAttribute_NotRequiredCSharp7_02()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static async Task F<T>(Func<Task> f)
    {
        await G(async () =>
        {
            await f();
            return default(object);
        });
    }
    static async Task<TResult> G<TResult>(Func<Task<TResult>> f)
    {
        throw new NotImplementedException();
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular7);
            comp.VerifyEmitDiagnostics(
                // (13,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<TResult> G<TResult>(Func<Task<TResult>> f)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "G").WithLocation(13, 32));
        }

        [Fact]
        public void MissingInt()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Enum { }
    public class Attribute { }
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            comp0.VerifyDiagnostics();
            var ref0 = comp0.EmitToImageReference();

            var source =
@"enum E { A }
class C
{
    int F() => (int)E.A;
}";
            var comp = CreateCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (1,6): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // enum E { A }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Int32").WithLocation(1, 6),
                // (4,5): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     int F() => (int)E.A;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32").WithLocation(4, 5),
                // (4,17): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     int F() => (int)E.A;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32").WithLocation(4, 17));
        }

        [Fact]
        public void ReferenceTypesFromUnannotatedAssembliesAreNotNullable()
        {
            var source0 =
@"public class A
{
    public static void F(string s) { }
}";
            var comp0 = CreateStandardCompilation(source0, parseOptions: TestOptions.Regular7);
            comp0.VerifyDiagnostics();
            var ref0 = comp0.EmitToImageReference();

            var source1 =
@"class B
{
    static void Main()
    {
        A.F(string.Empty);
        A.F(null);
    }
}";
            var comp1 = CreateStandardCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp1.VerifyDiagnostics(
                // (6,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         A.F(null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 13));
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_01()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            //var a = compilation.GetTypeByMetadataName("A");
            //var aFoo = a.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", aFoo.ToTestDisplayString());

            //var b = compilation.GetTypeByMetadataName("B");
            //var bFoo = b.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", bFoo.OverriddenMethod.ToTestDisplayString());

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_02()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_03()
        {
            var source = @"
class A
{
    public virtual System.Nullable<T> Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_04()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(System.Nullable<T> x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_05()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override System.Nullable<T> Foo<T>()
    {
        return null;
    }
} 
";
            CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_06()
        {
            var source = @"
class A
{
    public virtual void M1<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void M1<T>(System.Nullable<T> x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.True(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.Parameters[0].Type.IsValueType);
            Assert.True(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_01()
        {
            var source = @"
class A
{
    public virtual T? M1<T>() where T : class 
    { 
        return null; 
    }
}

class B : A
{
    public override T? M1<T>()
    {
        return null;
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.ReturnType.IsNullableType());
            Assert.True(m1.ReturnType.IsNullable);
            Assert.True(m1.ReturnType.IsReferenceType);
            Assert.False(m1.OverriddenMethod.ReturnType.IsNullableType());
        }

        [Fact]
        public void Overriding_02()
        {
            var source = @"
class A
{
    public virtual void M1<T>(T? x) where T : class 
    { 
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.Parameters[0].Type.IsNullable);
            Assert.True(m1.Parameters[0].Type.IsReferenceType);
            Assert.False(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_03()
        {
            var source = @"
class A
{
    public virtual void M1<T>(T? x) where T : class 
    { 
    }

    public virtual T? M2<T>() where T : class 
    { 
        return null;
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }

    public override T? M2<T>()
    { 
        return null;
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.Parameters[0].Type.IsNullable);
            Assert.True(m1.Parameters[0].Type.IsReferenceType);
            Assert.False(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());

            var m2 = b.GetMember<MethodSymbol>("M2");
            Assert.False(m2.ReturnType.IsNullableType());
            Assert.True(m2.ReturnType.IsNullable);
            Assert.True(m2.ReturnType.IsReferenceType);
            Assert.False(m2.OverriddenMethod.ReturnType.IsNullableType());
        }

        // PROTOTYPE(NullableReferenceTypes): Override matches other M3<T>.
        [Fact(Skip = "TODO")]
        public void Overriding_04()
        {
            var source = @"
class A
{
    public virtual void M1<T>(T? x) where T : struct 
    { 
    }

    public virtual void M1<T>(T x) 
    { 
    }

    public virtual void M2<T>(T? x) where T : struct 
    { 
    }

    public virtual void M2<T>(T x) 
    { 
    }

    public virtual void M3<T>(T x) 
    { 
    }

    public virtual void M3<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }

    public override void M2<T>(T x)
    {
    }

    public override void M3<T>(T? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.True(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());

            var m2 = b.GetMember<MethodSymbol>("M2");
            Assert.False(m2.Parameters[0].Type.IsNullableType());
            Assert.False(m2.OverriddenMethod.Parameters[0].Type.IsNullableType());

            var m3 = b.GetMember<MethodSymbol>("M3");
            Assert.True(m3.Parameters[0].Type.IsNullableType());
            Assert.True(m3.OverriddenMethod.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_05()
        {
            var source = @"
class A
{
    public virtual void M1<T>(T? x) where T : struct 
    { 
    }

    public virtual void M1<T>(T? x) where T : class 
    { 
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            // PROTOTYPE(NullableReferenceTypes): The overriding is ambiguous.
            // We simply matched the first candidate. Should this be an error?
            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.True(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_06()
        {
            var source = @"
class A
{
    public virtual void M1<T>(System.Nullable<T> x) where T : struct
    {
    }

    public virtual void M2<T>(T? x) where T : struct
    {
    }

    public virtual void M3<T>(C<T?> x) where T : struct
    {
    }

    public virtual void M4<T>(C<System.Nullable<T>> x) where T : struct
    {
    }

    public virtual void M5<T>(C<T?> x) where T : class
    {
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }

    public override void M2<T>(T? x)
    {
    }

    public override void M3<T>(C<T?> x)
    {
    }

    public override void M4<T>(C<T?> x)
    {
    }

    public override void M5<T>(C<T?> x)
    {
    }
}

class C<T> {}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m3 = b.GetMember<MethodSymbol>("M3");
            var m4 = b.GetMember<MethodSymbol>("M4");
            var m5 = b.GetMember<MethodSymbol>("M5");
            Assert.True(((NamedTypeSymbol)m3.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.True(((NamedTypeSymbol)m3.OverriddenMethod.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.True(((NamedTypeSymbol)m4.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.True(((NamedTypeSymbol)m4.OverriddenMethod.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.False(((NamedTypeSymbol)m5.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.False(((NamedTypeSymbol)m5.OverriddenMethod.Parameters[0].Type.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
        }

        [Fact]
        public void Overriding_07()
        {
            var source = @"
class A
{
    public void M1<T>(T x) 
    {
    }
}

class B : A
{
    public void M1<T>(T? x) where T : struct
    {
    }
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.True(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.Parameters[0].Type.TypeSymbol.StrippedType().IsValueType);
            Assert.Null(m1.OverriddenMethod);
        }

        [Fact]
        public void Overriding_08()
        {
            var source = @"
class A
{
    public void M1<T>(T x) 
    {
    }
}

class B : A
{
    public override void M1<T>(T? x) where T : struct
    {
    }
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                // (11,38): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M1<T>(T? x) where T : struct
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(11, 38),
                // (11,26): error CS0506: 'B.M1<T>(T?)': cannot override inherited member 'A.M1<T>(T)' because it is not marked virtual, abstract, or override
                //     public override void M1<T>(T? x) where T : struct
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M1").WithArguments("B.M1<T>(T?)", "A.M1<T>(T)").WithLocation(11, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.False(m1.Parameters[0].Type.TypeSymbol.StrippedType().IsValueType);
            Assert.False(m1.Parameters[0].Type.TypeSymbol.StrippedType().IsReferenceType);
            Assert.Null(m1.OverriddenMethod);
        }

        [Fact]
        public void Overriding_09()
        {
            var source = @"
class A
{
    public void M1<T>(T x) 
    {
    }

    public void M2<T>(T? x) 
    {
    }

    public void M3<T>(T? x) where T : class
    {
    }

    public void M4<T>(T? x) where T : struct
    {
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }

    public override void M2<T>(T? x)
    {
    }

    public override void M3<T>(T? x)
    {
    }

    public override void M4<T>(T? x)
    {
    }
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                // (27,26): error CS0506: 'B.M2<T>(T?)': cannot override inherited member 'A.M2<T>(T?)' because it is not marked virtual, abstract, or override
                //     public override void M2<T>(T? x)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M2").WithArguments("B.M2<T>(T?)", "A.M2<T>(T?)").WithLocation(27, 26),
                // (31,26): error CS0506: 'B.M3<T>(T?)': cannot override inherited member 'A.M3<T>(T?)' because it is not marked virtual, abstract, or override
                //     public override void M3<T>(T? x)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M3").WithArguments("B.M3<T>(T?)", "A.M3<T>(T?)").WithLocation(31, 26),
                // (35,26): error CS0506: 'B.M4<T>(T?)': cannot override inherited member 'A.M4<T>(T?)' because it is not marked virtual, abstract, or override
                //     public override void M4<T>(T? x)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M4").WithArguments("B.M4<T>(T?)", "A.M4<T>(T?)").WithLocation(35, 26),
                // (23,26): error CS0506: 'B.M1<T>(T?)': cannot override inherited member 'A.M1<T>(T)' because it is not marked virtual, abstract, or override
                //     public override void M1<T>(T? x)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M1").WithArguments("B.M1<T>(T?)", "A.M1<T>(T)").WithLocation(23, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            var m2 = b.GetMember<MethodSymbol>("M2");
            var m3 = b.GetMember<MethodSymbol>("M3");
            var m4 = b.GetMember<MethodSymbol>("M4");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.False(m2.Parameters[0].Type.IsNullableType());
            Assert.False(m3.Parameters[0].Type.IsNullableType());
            Assert.False(m4.Parameters[0].Type.IsNullableType());

            Assert.Null(m1.OverriddenMethod);
            Assert.Null(m2.OverriddenMethod);
            Assert.Null(m3.OverriddenMethod);
            Assert.Null(m4.OverriddenMethod);
        }

        [Fact]
        public void Overriding_10()
        {
            var source = @"
class A
{
    public virtual void M1<T>(System.Nullable<T> x) where T : class
    { 
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                // (4,50): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     public virtual void M1<T>(System.Nullable<T> x) where T : class
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "T").WithLocation(4, 50),
                // (11,26): error CS0115: 'B.M1<T>(T?)': no suitable method found to override
                //     public override void M1<T>(T? x)
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("B.M1<T>(T?)").WithLocation(11, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.Null(m1.OverriddenMethod);
        }

        [Fact]
        public void Overriding_11()
        {
            var source = @"
class A
{
    public virtual C<System.Nullable<T>> M1<T>() where T : class
    { 
        throw new System.NotImplementedException();
    }
}

class B : A
{
    public override C<T?> M1<T>()
    {
        throw new System.NotImplementedException();
    }
} 

class C<T> {}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                 // (4,42): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //     public virtual C<System.Nullable<T>> M1<T>() where T : class
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "M1").WithArguments("System.Nullable<T>", "T", "T").WithLocation(4, 42),
                 // (12,27): error CS0508: 'B.M1<T>()': return type must be 'C<T?>' to match overridden member 'A.M1<T>()'
                 //     public override C<T?> M1<T>()
                 Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M1").WithArguments("B.M1<T>()", "A.M1<T>()", "C<T?>").WithLocation(12, 27)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(((NamedTypeSymbol)m1.ReturnType.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
            Assert.True(((NamedTypeSymbol)m1.OverriddenMethod.ReturnType.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0].IsNullableType());
        }

        [Fact]
        public void Overriding_12()
        {
            var source = @"
class A
{
    public virtual string M1()
    { 
        throw new System.NotImplementedException();
    }

    public virtual string? M2()
    { 
        throw new System.NotImplementedException();
    }

    public virtual string? M3()
    { 
        throw new System.NotImplementedException();
    }

    public virtual System.Nullable<string> M4()
    { 
        throw new System.NotImplementedException();
    }

    public System.Nullable<string> M5()
    { 
        throw new System.NotImplementedException();
    }
}

class B : A
{
    public override string? M1()
    {
        throw new System.NotImplementedException();
    }

    public override string? M2()
    {
        throw new System.NotImplementedException();
    }

    public override string M3()
    {
        throw new System.NotImplementedException();
    }

    public override string? M4()
    {
        throw new System.NotImplementedException();
    }

    public override string? M5()
    {
        throw new System.NotImplementedException();
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                 // (32,29): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override string? M1()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M1").WithLocation(32, 29),
                 // (42,28): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override string M3()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M3").WithLocation(42, 28),
                 // (47,29): error CS0508: 'B.M4()': return type must be 'string?' to match overridden member 'A.M4()'
                 //     public override string? M4()
                 Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M4").WithArguments("B.M4()", "A.M4()", "string?").WithLocation(47, 29),
                 // (52,29): error CS0506: 'B.M5()': cannot override inherited member 'A.M5()' because it is not marked virtual, abstract, or override
                 //     public override string? M5()
                 Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M5").WithArguments("B.M5()", "A.M5()").WithLocation(52, 29),
                 // (19,44): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //     public virtual System.Nullable<string> M4()
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "M4").WithArguments("System.Nullable<T>", "T", "string").WithLocation(19, 44),
                 // (24,36): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //     public System.Nullable<string> M5()
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "M5").WithArguments("System.Nullable<T>", "T", "string").WithLocation(24, 36)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.ReturnType.IsNullableType());
            Assert.False(m1.OverriddenMethod.ReturnType.IsNullableType());

            var m4 = b.GetMember<MethodSymbol>("M4");
            Assert.False(m4.ReturnType.IsNullableType());
            Assert.True(m4.OverriddenMethod.ReturnType.IsNullableType());

            var m5 = b.GetMember<MethodSymbol>("M4");
            Assert.False(m5.ReturnType.IsNullableType());
        }

        [Fact]
        public void Overriding_13()
        {
            var source = @"
class A
{
    public virtual void M1(string x)
    { 
    }

    public virtual void M2(string? x)
    { 
    }

    public virtual void M3(string? x)
    { 
    }

    public virtual void M4(System.Nullable<string> x)
    { 
    }

    public void M5(System.Nullable<string> x)
    { 
    }
}

class B : A
{
    public override void M1(string? x)
    {
    }

    public override void M2(string? x)
    {
    }

    public override void M3(string x)
    {
    }

    public override void M4(string? x)
    {
    }

    public override void M5(string? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (27,26): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override void M1(string? x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("x").WithLocation(27, 26),
                 // (35,26): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override void M3(string x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M3").WithArguments("x").WithLocation(35, 26),
                 // (39,26): error CS0115: 'B.M4(string)': no suitable method found to override
                 //     public override void M4(string? x)
                 Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M4").WithArguments("B.M4(string?)").WithLocation(39, 26),
                 // (43,26): error CS0115: 'B.M5(string)': no suitable method found to override
                 //     public override void M5(string? x)
                 Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M5").WithArguments("B.M5(string?)").WithLocation(43, 26),
                 // (16,52): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //     public virtual void M4(System.Nullable<string> x)
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "string").WithLocation(16, 52),
                 // (20,44): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                 //     public void M5(System.Nullable<string> x)
                 Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "string").WithLocation(20, 44)
                );

            var b = compilation.GetTypeByMetadataName("B");
            var m1 = b.GetMember<MethodSymbol>("M1");
            Assert.False(m1.Parameters[0].Type.IsNullableType());
            Assert.True(m1.Parameters[0].Type.IsNullable);
            Assert.True(m1.Parameters[0].Type.IsReferenceType);
            Assert.False(m1.OverriddenMethod.Parameters[0].Type.IsNullableType());

            var m4 = b.GetMember<MethodSymbol>("M4");
            Assert.False(m4.Parameters[0].Type.IsNullableType());
            Assert.Null(m4.OverriddenMethod);

            var m5 = b.GetMember<MethodSymbol>("M4");
            Assert.False(m5.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_14()
        {
            var source = @"
class A
{
    public virtual int M1()
    { 
        throw new System.NotImplementedException();
    }

    public virtual int? M2()
    { 
        throw new System.NotImplementedException();
    }

    public virtual int? M3()
    { 
        throw new System.NotImplementedException();
    }

    public virtual System.Nullable<int> M4()
    { 
        throw new System.NotImplementedException();
    }

    public System.Nullable<int> M5()
    { 
        throw new System.NotImplementedException();
    }
}

class B : A
{
    public override int? M1()
    {
        throw new System.NotImplementedException();
    }

    public override int? M2()
    {
        throw new System.NotImplementedException();
    }

    public override int M3()
    {
        throw new System.NotImplementedException();
    }

    public override int? M4()
    {
        throw new System.NotImplementedException();
    }

    public override int? M5()
    {
        throw new System.NotImplementedException();
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                 // (42,25): error CS0508: 'B.M3()': return type must be 'int?' to match overridden member 'A.M3()'
                 //     public override int M3()
                 Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M3").WithArguments("B.M3()", "A.M3()", "int?").WithLocation(42, 25),
                 // (52,26): error CS0506: 'B.M5()': cannot override inherited member 'A.M5()' because it is not marked virtual, abstract, or override
                 //     public override int? M5()
                 Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M5").WithArguments("B.M5()", "A.M5()").WithLocation(52, 26),
                 // (32,26): error CS0508: 'B.M1()': return type must be 'int' to match overridden member 'A.M1()'
                 //     public override int? M1()
                 Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M1").WithArguments("B.M1()", "A.M1()", "int").WithLocation(32, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            Assert.True(b.GetMember<MethodSymbol>("M1").ReturnType.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M2").ReturnType.IsNullableType());
            Assert.False(b.GetMember<MethodSymbol>("M3").ReturnType.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M4").ReturnType.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M5").ReturnType.IsNullableType());
        }

        [Fact]
        public void Overriding_15()
        {
            var source = @"
class A
{
    public virtual void M1(int x)
    { 
    }

    public virtual void M2(int? x)
    { 
    }

    public virtual void M3(int? x)
    { 
    }

    public virtual void M4(System.Nullable<int> x)
    { 
    }

    public void M5(System.Nullable<int> x)
    { 
    }
}

class B : A
{
    public override void M1(int? x)
    {
    }

    public override void M2(int? x)
    {
    }

    public override void M3(int x)
    {
    }

    public override void M4(int? x)
    {
    }

    public override void M5(int? x)
    {
    }
} 
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (35,26): error CS0115: 'B.M3(int)': no suitable method found to override
                 //     public override void M3(int x)
                 Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M3").WithArguments("B.M3(int)").WithLocation(35, 26),
                 // (43,26): error CS0506: 'B.M5(int?)': cannot override inherited member 'A.M5(int?)' because it is not marked virtual, abstract, or override
                 //     public override void M5(int? x)
                 Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M5").WithArguments("B.M5(int?)", "A.M5(int?)").WithLocation(43, 26),
                 // (27,26): error CS0115: 'B.M1(int?)': no suitable method found to override
                 //     public override void M1(int? x)
                 Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("B.M1(int?)").WithLocation(27, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            Assert.True(b.GetMember<MethodSymbol>("M1").Parameters[0].Type.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M2").Parameters[0].Type.IsNullableType());
            Assert.False(b.GetMember<MethodSymbol>("M3").Parameters[0].Type.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M4").Parameters[0].Type.IsNullableType());
            Assert.True(b.GetMember<MethodSymbol>("M5").Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void Overriding_16()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract event System.Action<string> E1; 
    public abstract event System.Action<string>? E2; 
    public abstract event System.Action<string?>? E3; 
}

class B1 : A
{
    public override event System.Action<string?> E1 {add {} remove{}}
    public override event System.Action<string> E2 {add {} remove{}}
    public override event System.Action<string?>? E3 {add {} remove{}}
}

class B2 : A
{
    public override event System.Action<string?> E1; // 2
    public override event System.Action<string> E2; // 2
    public override event System.Action<string?>? E3; // 2

    void Dummy()
    {
        var e1 = E1;
        var e2 = E2;
        var e3 = E3;
    }
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (19,49): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override event System.Action<string> E2;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E2").WithLocation(19, 49),
                 // (18,50): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override event System.Action<string?> E1;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E1").WithLocation(18, 50),
                 // (26,49): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override event System.Action<string> E2; // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E2").WithLocation(26, 49),
                 // (25,50): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override event System.Action<string?> E1; // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E1").WithLocation(25, 50)
                );

            foreach (string typeName in new[] { "B1", "B2" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (string memberName in new[] { "E1", "E2" })
                {
                    var member = type.GetMember<EventSymbol>(memberName);
                    Assert.False(member.Type.Equals(member.OverriddenEvent.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }

                var e3 = type.GetMember<EventSymbol>("E3");
                Assert.True(e3.Type.Equals(e3.OverriddenEvent.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "A", "B1", "B2" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var ev in type.GetMembers().OfType<EventSymbol>())
                {
                    Assert.True(ev.Type.Equals(ev.AddMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(ev.Type.Equals(ev.RemoveMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Overriding_21()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract event System.Action<string> E1; 
    public abstract event System.Action<string>? E2; 
}

class B1 : A
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public override event System.Action<string?> E1 {add {} remove{}}
    [System.Runtime.CompilerServices.NullableOptOut]
    public override event System.Action<string> E2 {add {} remove{}}
}

class B2 : A
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public override event System.Action<string?> E1; // 2
    [System.Runtime.CompilerServices.NullableOptOut]
    public override event System.Action<string> E2; // 2

    void Dummy()
    {
        var e1 = E1;
        var e2 = E2;
    }
}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Implementing_01()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA
{
    event System.Action<string> E1; 
    event System.Action<string>? E2; 
    event System.Action<string?>? E3; 
}

class B1 : IA
{
    public event System.Action<string?> E1 {add {} remove{}}
    public event System.Action<string> E2 {add {} remove{}}
    public event System.Action<string?>? E3 {add {} remove{}}
}

class B2 : IA
{
    public event System.Action<string?> E1; // 2
    public event System.Action<string> E2; // 2
    public event System.Action<string?>? E3; // 2

    void Dummy()
    {
        var e1 = E1;
        var e2 = E2;
        var e3 = E3;
    }
}

";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (26,40): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action<string>? IA.E2'.
                 //     public event System.Action<string> E2; // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E2").WithArguments("event Action<string>? IA.E2").WithLocation(26, 40),
                 // (25,41): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action<string> IA.E1'.
                 //     public event System.Action<string?> E1; // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E1").WithArguments("event Action<string> IA.E1").WithLocation(25, 41),
                 // (19,40): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action<string>? IA.E2'.
                 //     public event System.Action<string> E2 {add {} remove{}}
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E2").WithArguments("event Action<string>? IA.E2").WithLocation(19, 40),
                 // (18,41): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action<string> IA.E1'.
                 //     public event System.Action<string?> E1 {add {} remove{}}
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E1").WithArguments("event Action<string> IA.E1").WithLocation(18, 41)
                );

            var ia = compilation.GetTypeByMetadataName("IA");

            foreach (string memberName in new[] { "E1", "E2" })
            {
                var member = ia.GetMember<EventSymbol>(memberName);

                foreach (string typeName in new[] { "B1", "B2" })
                {
                    var type = compilation.GetTypeByMetadataName(typeName);

                    var impl = (EventSymbol)type.FindImplementationForInterfaceMember(member);
                    Assert.False(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }

            var e3 = ia.GetMember<EventSymbol>("E3");

            foreach (string typeName in new[] { "B1", "B2" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                var impl = (EventSymbol)type.FindImplementationForInterfaceMember(e3);
                Assert.True(impl.Type.Equals(e3.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA", "B1", "B2" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var ev in type.GetMembers().OfType<EventSymbol>())
                {
                    Assert.True(ev.Type.Equals(ev.AddMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(ev.Type.Equals(ev.RemoveMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Implementing_02()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA
{
    event System.Action<string> E1; 
    event System.Action<string>? E2; 
    event System.Action<string?>? E3; 
}

class B1 : IA
{
    event System.Action<string?> IA.E1 {add {} remove{}}
    event System.Action<string> IA.E2 {add {} remove{}}
    event System.Action<string?>? IA.E3 {add {} remove{}}
}

interface IB
{
    //event System.Action<string> E1; 
    //event System.Action<string>? E2; 
    event System.Action<string?>? E3; 
}

class B2 : IB
{
    //event System.Action<string?> IB.E1; // 2
    //event System.Action<string> IB.E2; // 2
    event System.Action<string?>? IB.E3; // 2
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (34,37): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                 //     event System.Action<string?>? IB.E3; // 2
                 Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, ".").WithLocation(34, 37),
                 // (34,40): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                 //     event System.Action<string?>? IB.E3; // 2
                 Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(34, 40),
                 // (34,40): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                 //     event System.Action<string?>? IB.E3; // 2
                 Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(34, 40),
                 // (34,38): error CS0539: 'B2.' in explicit interface declaration is not a member of interface
                 //     event System.Action<string?>? IB.E3; // 2
                 Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "").WithArguments("B2.").WithLocation(34, 38),
                 // (34,38): error CS0065: 'B2.': event property must have both add and remove accessors
                 //     event System.Action<string?>? IB.E3; // 2
                 Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "").WithArguments("B2.").WithLocation(34, 38),
                 // (30,12): error CS0535: 'B2' does not implement interface member 'IB.E3'
                 // class B2 : IB
                 Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IB").WithArguments("B2", "IB.E3").WithLocation(30, 12),
                 // (18,37): warning CS8615: Nullability of reference types in type doesn't match implemented member 'event Action<string> IA.E1'.
                 //     event System.Action<string?> IA.E1 {add {} remove{}}
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "E1").WithArguments("event Action<string> IA.E1").WithLocation(18, 37),
                 // (19,36): warning CS8615: Nullability of reference types in type doesn't match implemented member 'event Action<string>? IA.E2'.
                 //     event System.Action<string> IA.E2 {add {} remove{}}
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "E2").WithArguments("event Action<string>? IA.E2").WithLocation(19, 36)
                );

            var ia = compilation.GetTypeByMetadataName("IA");
            var b1 = compilation.GetTypeByMetadataName("B1");

            foreach (string memberName in new[] { "E1", "E2" })
            {
                var member = ia.GetMember<EventSymbol>(memberName);

                var impl = (EventSymbol)b1.FindImplementationForInterfaceMember(member);
                Assert.False(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            var e3 = ia.GetMember<EventSymbol>("E3");
            {
                var impl = (EventSymbol)b1.FindImplementationForInterfaceMember(e3);
                Assert.True(impl.Type.Equals(e3.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA", "B1" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var ev in type.GetMembers().OfType<EventSymbol>())
                {
                    Assert.True(ev.Type.Equals(ev.AddMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(ev.Type.Equals(ev.RemoveMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Overriding_17()
        {
            var source =
@"#pragma warning disable 8618
class C
{
    public static void Main()
    { 
    }
}

abstract class A1
{
    public abstract string?[] P1 {get; set;} 
    public abstract string[] P2 {get; set;} 

    public abstract string?[] this[int x] {get; set;} 
    public abstract string[] this[short x] {get; set;} 
}

abstract class A2
{
    public abstract string?[]? P3 {get; set;} 

    public abstract string?[]? this[long x] {get; set;} 
}

class B1 : A1
{
    public override string[] P1 {get; set;} 
    public override string[]? P2 {get; set;} 
    
    public override string[] this[int x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
    
    public override string[]? this[short x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}

class B2 : A2
{
    public override string?[]? P3 {get; set;} 
    
    public override string?[]? this[long x] // 3
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (28,31): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override string[]? P2 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "P2").WithLocation(28, 31),
                 // (30,30): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override string[] this[int x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "this").WithLocation(30, 30),
                 // (36,31): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override string[]? this[short x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "this").WithLocation(36, 31),
                 // (27,30): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                 //     public override string[] P1 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "P1").WithLocation(27, 30)
                );

            foreach (var member in compilation.GetTypeByMetadataName("B1").GetMembers().OfType<PropertySymbol>())
            {
                Assert.False(member.Type.Equals(member.OverriddenProperty.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (var member in compilation.GetTypeByMetadataName("B2").GetMembers().OfType<PropertySymbol>())
            {
                Assert.True(member.Type.Equals(member.OverriddenProperty.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "A1", "B1", "A2", "B2" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Overriding_22()
        {
            var source =
@"#pragma warning disable 8618
class C
{
    public static void Main()
    { 
    }
}

abstract class A1
{
    public abstract string?[] P1 {get; set;} 
    public abstract string[] P2 {get; set;} 

    public abstract string?[] this[int x] {get; set;} 
    public abstract string[] this[short x] {get; set;} 
}

class B1 : A1
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public override string[] P1 {get; set;} 
    [System.Runtime.CompilerServices.NullableOptOut]
    public override string[]? P2 {get; set;} 
    
    [System.Runtime.CompilerServices.NullableOptOut]
    public override string[] this[int x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
    
    [System.Runtime.CompilerServices.NullableOptOut]
    public override string[]? this[short x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Implementing_03()
        {
            var source =
@"#pragma warning disable 8618
class C
{
    public static void Main()
    { 
    }
}
interface IA
{
    string?[] P1 {get; set;} 
    string[] P2 {get; set;} 
    string?[] this[int x] {get; set;} 
    string[] this[short x] {get; set;} 
}
interface IA2
{
    string?[]? P3 {get; set;} 
    string?[]? this[long x] {get; set;} 
}
class B : IA, IA2
{
    public string[] P1 {get; set;} 
    public string[]? P2 {get; set;} 
    public string?[]? P3 {get; set;} 

    public string[] this[int x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 

    public string[]? this[short x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 

    public string?[]? this[long x]
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (23,22): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string[] IA.P2'.
                 //     public string[]? P2 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "P2").WithArguments("string[] IA.P2").WithLocation(23, 22),
                 // (26,21): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string?[] IA.this[int x]'.
                 //     public string[] this[int x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "this").WithArguments("string?[] IA.this[int x]").WithLocation(26, 21),
                 // (32,22): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string[] IA.this[short x]'.
                 //     public string[]? this[short x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "this").WithArguments("string[] IA.this[short x]").WithLocation(32, 22),
                 // (22,21): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string?[] IA.P1'.
                 //     public string[] P1 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "P1").WithArguments("string?[] IA.P1").WithLocation(22, 21)
                );

            var b = compilation.GetTypeByMetadataName("B");

            foreach (var member in compilation.GetTypeByMetadataName("IA").GetMembers().OfType<PropertySymbol>())
            {
                var impl = (PropertySymbol)b.FindImplementationForInterfaceMember(member);
                Assert.False(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (var member in compilation.GetTypeByMetadataName("IA2").GetMembers().OfType<PropertySymbol>())
            {
                var impl = (PropertySymbol)b.FindImplementationForInterfaceMember(member);
                Assert.True(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA", "IA2", "B" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Implementing_04()
        {
            var source =
@"#pragma warning disable 8618
class C
{
    public static void Main()
    { 
    }
}
interface IA
{
    string?[] P1 {get; set;} 
    string[] P2 {get; set;} 
    string?[] this[int x] {get; set;} 
    string[] this[short x] {get; set;} 
}
interface IA2
{
    string?[]? P3 {get; set;} 
    string?[]? this[long x] {get; set;} 
}
class B : IA, IA2
{
    string[] IA.P1 {get; set;} 
    string[]? IA.P2 {get; set;} 
    string?[]? IA2.P3 {get; set;} 

    string[] IA.this[int x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 

    string[]? IA.this[short x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 

    string?[]? IA2.this[long x]
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (22,17): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string?[] IA.P1'.
                 //     string[] IA.P1 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "P1").WithArguments("string?[] IA.P1").WithLocation(22, 17),
                 // (23,18): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string[] IA.P2'.
                 //     string[]? IA.P2 {get; set;} 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "P2").WithArguments("string[] IA.P2").WithLocation(23, 18),
                 // (26,17): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string?[] IA.this[int x]'.
                 //     string[] IA.this[int x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "this").WithArguments("string?[] IA.this[int x]").WithLocation(26, 17),
                 // (32,18): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string[] IA.this[short x]'.
                 //     string[]? IA.this[short x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "this").WithArguments("string[] IA.this[short x]").WithLocation(32, 18)
                );

            var b = compilation.GetTypeByMetadataName("B");

            foreach (var member in compilation.GetTypeByMetadataName("IA").GetMembers().OfType<PropertySymbol>())
            {
                var impl = (PropertySymbol)b.FindImplementationForInterfaceMember(member);
                Assert.False(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (var member in compilation.GetTypeByMetadataName("IA2").GetMembers().OfType<PropertySymbol>())
            {
                var impl = (PropertySymbol)b.FindImplementationForInterfaceMember(member);
                Assert.True(impl.Type.Equals(member.Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA", "IA2", "B" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Overriding_18()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract string[] M1(); 
    public abstract T[] M2<T>() where T : class; 
    public abstract T?[]? M3<T>() where T : class; 
}

class B : A
{
    public override string?[] M1()
    {
        return new string?[] {};
    } 

    public override S?[] M2<S>()
    {
        return new S?[] {};
    } 

    public override S?[]? M3<S>()
    {
        return new S?[] {};
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (23,26): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override S?[] M2<S>()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M2").WithLocation(23, 26),
                 // (18,31): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override string?[] M1()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M1").WithLocation(18, 31)
                );

            var b = compilation.GetTypeByMetadataName("B");
            foreach (string memberName in new[] { "M1", "M2" })
            {
                var member = b.GetMember<MethodSymbol>(memberName);
                Assert.False(member.ReturnType.Equals(member.OverriddenMethod.ConstructIfGeneric(member.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations)).ReturnType,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            var m3 = b.GetMember<MethodSymbol>("M3");
            Assert.True(m3.ReturnType.Equals(m3.OverriddenMethod.ConstructIfGeneric(m3.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations)).ReturnType,
                TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
        }

        [Fact]
        public void Overriding_23()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract string[] M1(); 
    public abstract T[] M2<T>() where T : class; 
}

class B : A
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public override string?[] M1()
    {
        return new string?[] {};
    } 

    [System.Runtime.CompilerServices.NullableOptOut]
    public override S?[] M2<S>()
    {
        return new S?[] {};
    } 
}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Implementing_05()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA
{
    string[] M1(); 
    T[] M2<T>() where T : class; 
    T?[]? M3<T>() where T : class; 
}

class B : IA
{
    public string?[] M1()
    {
        return new string?[] {};
    } 

    public S?[] M2<S>() where S : class
    {
        return new S?[] {};
    } 

    public S?[]? M3<S>() where S : class
    {
        return new S?[] {};
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (23,17): warning CS8613: Nullability of reference types in return type doesn't match implicitly implemented member 'T[] IA.M2<T>()'.
                 //     public S?[] M2<S>() where S : class
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation, "M2").WithArguments("T[] IA.M2<T>()").WithLocation(23, 17),
                 // (18,22): warning CS8613: Nullability of reference types in return type doesn't match implicitly implemented member 'string[] IA.M1()'.
                 //     public string?[] M1()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation, "M1").WithArguments("string[] IA.M1()").WithLocation(18, 22)
                );

            var ia = compilation.GetTypeByMetadataName("IA");
            var b = compilation.GetTypeByMetadataName("B");

            foreach (var memberName in new[] { "M1", "M2" })
            {
                var member = ia.GetMember<MethodSymbol>(memberName);
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.False(implementing.ReturnType.Equals(implemented.ReturnType,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var member = ia.GetMember<MethodSymbol>("M3");
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.True(implementing.ReturnType.Equals(implemented.ReturnType,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }
        }

        [Fact]
        public void Implementing_06()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA
{
    string[] M1(); 
    T[] M2<T>() where T : class; 
    T?[]? M3<T>() where T : class; 
}

class B : IA
{
    string?[] IA.M1()
    {
        return new string?[] {};
    } 

    S?[] IA.M2<S>() 
    {
        return new S?[] {};
    } 

    S?[]? IA.M3<S>() 
    {
        return new S?[] {};
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (23,13): warning CS8616: Nullability of reference types in return type doesn't match implemented member 'T[] IA.M2<T>()'.
                 //     S?[] IA.M2<S>() 
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation, "M2").WithArguments("T[] IA.M2<T>()").WithLocation(23, 13),
                 // (18,18): warning CS8616: Nullability of reference types in return type doesn't match implemented member 'string[] IA.M1()'.
                 //     string?[] IA.M1()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation, "M1").WithArguments("string[] IA.M1()").WithLocation(18, 18)
                );

            var ia = compilation.GetTypeByMetadataName("IA");
            var b = compilation.GetTypeByMetadataName("B");

            foreach (var memberName in new[] { "M1", "M2" })
            {
                var member = ia.GetMember<MethodSymbol>(memberName);
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.False(implementing.ReturnType.Equals(implemented.ReturnType,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var member = ia.GetMember<MethodSymbol>("M3");
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.True(implementing.ReturnType.Equals(implemented.ReturnType,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Checking NullableOptOut can result in cycle
        // when decoding attributes. See NullableOptOut_DecodeAttributeCycle_*.
        [Fact(Skip = "TODO")]
        public void Implementing_11()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA
{
    string[] M1(); 
    T[] M2<T>() where T : class; 
}

class B : IA
{
    [System.Runtime.CompilerServices.NullableOptOut]
    string?[] IA.M1()
    {
        return new string?[] {};
    } 

    [System.Runtime.CompilerServices.NullableOptOut]
    S?[] IA.M2<S>() 
    {
        return new S?[] {};
    } 
}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Overriding_19()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract void M1(string[] x); 
    public abstract void M2<T>(T[] x) where T : class; 
    public abstract void M3<T>(T?[]? x) where T : class; 
}

class B : A
{
    public override void M1(string?[] x)
    {
    } 

    public override void M2<T>(T?[] x)
    {
    } 

    public override void M3<T>(T?[]? x)
    {
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (22,26): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override void M2<T>(T?[] x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M2").WithArguments("x").WithLocation(22, 26),
                 // (18,26): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override void M1(string?[] x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("x").WithLocation(18, 26)
                );

            var b = compilation.GetTypeByMetadataName("B");
            foreach (string memberName in new[] { "M1", "M2" })
            {
                var member = b.GetMember<MethodSymbol>(memberName);
                Assert.False(member.Parameters[0].Type.Equals(member.OverriddenMethod.ConstructIfGeneric(member.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations)).Parameters[0].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            var m3 = b.GetMember<MethodSymbol>("M3");
            Assert.True(m3.Parameters[0].Type.Equals(m3.OverriddenMethod.ConstructIfGeneric(m3.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations)).Parameters[0].Type,
                TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
        }

        [Fact]
        public void Overriding_24()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A
{
    public abstract void M1(string[] x); 
    public abstract void M2<T>(T[] x) where T : class; 
}

class B : A
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public override void M1(string?[] x)
    {
    } 

    [System.Runtime.CompilerServices.NullableOptOut]
    public override void M2<T>(T?[] x)
    {
    } 
}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Overriding_25()
        {
            var ilSource = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly '<<GeneratedFileName>>'
{
}

.module '<<GeneratedFileName>>.dll'
.custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor() = ( 01 00 00 00 ) 

.class public auto ansi beforefieldinit C`2<T,S>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C`2::.ctor

} // end of class C`2

.class public abstract auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance class C`2<string modopt([mscorlib]System.Runtime.CompilerServices.IsConst),string> 
          M1() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 01 00 00 00 ) 
  } // end of method A::M1

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method A::.ctor

} // end of class A

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.NullableAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 86 6B 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ...k....T..Allow
                                                                                                                         4D 75 6C 74 69 70 6C 65 00 )              // Multiple.
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method NullableAttribute::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(bool[] transformFlags) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method NullableAttribute::.ctor

} // end of class System.Runtime.CompilerServices.NullableAttribute
";

            var source = @"
class C
{
    public static void Main()
    { 
    }
}

class B : A
{
    public override C<string, string?> M1()
    {
        return new C<string, string?>();
    } 
}
";
            var compilation = CreateStandardCompilation(source, new[] { CompileIL(ilSource, prependDefaultHeader: false) },
                                                            options: TestOptions.ReleaseDll,
                                                            parseOptions: TestOptions.Regular8);

            var m1 = compilation.GetTypeByMetadataName("B").GetMember<MethodSymbol>("M1");
            Assert.Equal("C<System.String? modopt(System.Runtime.CompilerServices.IsConst), System.String>", m1.OverriddenMethod.ReturnType.ToTestDisplayString());
            Assert.Equal("C<System.String modopt(System.Runtime.CompilerServices.IsConst), System.String?>", m1.ReturnType.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                 // (11,40): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override C<string, string?> M1()
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M1").WithLocation(11, 40)
                );
        }

        [Fact]
        public void Implementing_07()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}
interface IA
{
    void M1(string[] x); 
    void M2<T>(T[] x) where T : class; 
    void M3<T>(T?[]? x) where T : class; 
}
class B : IA
{
    public void M1(string?[] x)
    {
    } 

    public void M2<T>(T?[] x)  where T : class
    {
    } 

    public void M3<T>(T?[]? x)  where T : class
    {
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (20,17): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'void IA.M2<T>(T[] x)'.
                 //     public void M2<T>(T?[] x)  where T : class
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "M2").WithArguments("x", "void IA.M2<T>(T[] x)").WithLocation(20, 17),
                 // (16,17): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'void IA.M1(string[] x)'.
                 //     public void M1(string?[] x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "M1").WithArguments("x", "void IA.M1(string[] x)").WithLocation(16, 17)
                );

            var ia = compilation.GetTypeByMetadataName("IA");
            var b = compilation.GetTypeByMetadataName("B");

            foreach (var memberName in new[] { "M1", "M2" })
            {
                var member = ia.GetMember<MethodSymbol>(memberName);
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.False(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var member = ia.GetMember<MethodSymbol>("M3");
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.True(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }
        }

        [Fact]
        public void Implementing_08()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}
interface IA
{
    void M1(string[] x); 
    void M2<T>(T[] x) where T : class; 
    void M3<T>(T?[]? x) where T : class; 
}
class B : IA
{
    void IA.M1(string?[] x)
    {
    } 

    void IA.M2<T>(T?[] x)  
    {
    } 

    void IA.M3<T>(T?[]? x)  
    {
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (20,13): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'void IA.M2<T>(T[] x)'.
                 //     void IA.M2<T>(T?[] x)  
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "M2").WithArguments("x", "void IA.M2<T>(T[] x)").WithLocation(20, 13),
                 // (16,13): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'void IA.M1(string[] x)'.
                 //     void IA.M1(string?[] x)
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "M1").WithArguments("x", "void IA.M1(string[] x)").WithLocation(16, 13)
                );

            var ia = compilation.GetTypeByMetadataName("IA");
            var b = compilation.GetTypeByMetadataName("B");

            foreach (var memberName in new[] { "M1", "M2" })
            {
                var member = ia.GetMember<MethodSymbol>(memberName);
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.False(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var member = ia.GetMember<MethodSymbol>("M3");
                var implementing = (MethodSymbol)b.FindImplementationForInterfaceMember(member);
                var implemented = member.ConstructIfGeneric(implementing.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));
                Assert.True(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }
        }

        [Fact]
        public void Overriding_20()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

abstract class A1
{
    public abstract int this[string?[] x] {get; set;} 
}
abstract class A2
{
    public abstract int this[string[] x] {get; set;} 
}
abstract class A3
{
    public abstract int this[string?[]? x] {get; set;} 
}

class B1 : A1
{
    public override int this[string[] x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B2 : A2
{
    public override int this[string[]? x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B3 : A3
{
    public override int this[string?[]? x] // 3
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (24,25): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override int this[string[] x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "this").WithArguments("x").WithLocation(24, 25),
                 // (32,25): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                 //     public override int this[string[]? x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "this").WithArguments("x").WithLocation(32, 25)
                );

            foreach (string typeName in new[] { "B1", "B2" })
            {
                foreach (var member in compilation.GetTypeByMetadataName(typeName).GetMembers().OfType<PropertySymbol>())
                {
                    Assert.False(member.Parameters[0].Type.Equals(member.OverriddenProperty.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }

            foreach (var member in compilation.GetTypeByMetadataName("B3").GetMembers().OfType<PropertySymbol>())
            {
                Assert.True(member.Parameters[0].Type.Equals(member.OverriddenProperty.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "A1", "A2", "A3", "B1", "B2", "B3" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Implementing_09()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA1
{
    int this[string?[] x] {get; set;} 
}
interface IA2
{
    int this[string[] x] {get; set;} 
}
interface IA3
{
    int this[string?[]? x] {get; set;} 
}

class B1 : IA1
{
    public int this[string[] x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B2 : IA2
{
    public int this[string[]? x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B3 : IA3
{
    public int this[string?[]? x] // 3
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (32,16): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'int IA2.this[string[] x]'.
                 //     public int this[string[]? x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "this").WithArguments("x", "int IA2.this[string[] x]").WithLocation(32, 16),
                 // (24,16): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'int IA1.this[string?[] x]'.
                 //     public int this[string[] x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "this").WithArguments("x", "int IA1.this[string?[] x]").WithLocation(24, 16)
                );

            foreach (string[] typeName in new[] { new[] { "IA1", "B1" }, new[] { "IA2", "B2" } })
            {
                var implemented = compilation.GetTypeByMetadataName(typeName[0]).GetMembers().OfType<PropertySymbol>().Single();
                var implementing = (PropertySymbol)compilation.GetTypeByMetadataName(typeName[1]).FindImplementationForInterfaceMember(implemented);
                Assert.False(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var implemented = compilation.GetTypeByMetadataName("IA3").GetMembers().OfType<PropertySymbol>().Single();
                var implementing = (PropertySymbol)compilation.GetTypeByMetadataName("B3").FindImplementationForInterfaceMember(implemented);
                Assert.True(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA1", "IA2", "IA3", "B1", "B2", "B3" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void Implementing_10()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

interface IA1
{
    int this[string?[] x] {get; set;} 
}
interface IA2
{
    int this[string[] x] {get; set;} 
}
interface IA3
{
    int this[string?[]? x] {get; set;} 
}

class B1 : IA1
{
    int IA1.this[string[] x] // 1
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B2 : IA2
{
    int IA2.this[string[]? x] // 2
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
class B3 : IA3
{
    int IA3.this[string?[]? x] // 3
    {
        get {throw new System.NotImplementedException();}
        set {}
    } 
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (24,13): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'int IA1.this[string?[] x]'.
                 //     int IA1.this[string[] x] // 1
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "this").WithArguments("x", "int IA1.this[string?[] x]").WithLocation(24, 13),
                 // (32,13): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'int IA2.this[string[] x]'.
                 //     int IA2.this[string[]? x] // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "this").WithArguments("x", "int IA2.this[string[] x]").WithLocation(32, 13)
                );

            foreach (string[] typeName in new[] { new[] { "IA1", "B1" }, new[] { "IA2", "B2" } })
            {
                var implemented = compilation.GetTypeByMetadataName(typeName[0]).GetMembers().OfType<PropertySymbol>().Single();
                var implementing = (PropertySymbol)compilation.GetTypeByMetadataName(typeName[1]).FindImplementationForInterfaceMember(implemented);
                Assert.False(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            {
                var implemented = compilation.GetTypeByMetadataName("IA3").GetMembers().OfType<PropertySymbol>().Single();
                var implementing = (PropertySymbol)compilation.GetTypeByMetadataName("B3").FindImplementationForInterfaceMember(implemented);
                Assert.True(implementing.Parameters[0].Type.Equals(implemented.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            foreach (string typeName in new[] { "IA1", "IA2", "IA3", "B1", "B2", "B3" })
            {
                var type = compilation.GetTypeByMetadataName(typeName);

                foreach (var property in type.GetMembers().OfType<PropertySymbol>())
                {
                    Assert.True(property.Type.Equals(property.GetMethod.ReturnType, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                    Assert.True(property.Type.Equals(property.SetMethod.Parameters.Last().Type, TypeCompareKind.CompareNullableModifiersForReferenceTypes));
                }
            }
        }

        [Fact]
        public void PartialMethods_01()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

partial class C1
{
    partial void M1<T>(T x, T?[] y, System.Action<T> z, System.Action<T?[]?>?[]? u) where T : class;
}

partial class C1
{
    partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
    { }
}";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (16,18): warning CS8611: Nullability of reference types in type of parameter 'x' doesn't match partial method declaration.
                 //     partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "M1").WithArguments("x").WithLocation(16, 18),
                 // (16,18): warning CS8611: Nullability of reference types in type of parameter 'y' doesn't match partial method declaration.
                 //     partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "M1").WithArguments("y").WithLocation(16, 18),
                 // (16,18): warning CS8611: Nullability of reference types in type of parameter 'z' doesn't match partial method declaration.
                 //     partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "M1").WithArguments("z").WithLocation(16, 18)
                );

            var c1 = compilation.GetTypeByMetadataName("C1");

            var m1 = c1.GetMember<MethodSymbol>("M1");
            var m1Impl = m1.PartialImplementationPart;
            var m1Def = m1.ConstructIfGeneric(m1Impl.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations));

            for (int i = 0; i < 3; i++)
            {
                Assert.False(m1Impl.Parameters[i].Type.Equals(m1Def.Parameters[i].Type,
                    TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
            }

            Assert.True(m1Impl.Parameters[3].Type.Equals(m1Def.Parameters[3].Type,
                TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes));
        }

        [Fact]
        public void PartialMethods_02()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

partial class C1
{
    [System.Runtime.CompilerServices.NullableOptOut]
    partial void M1<T>(T x, T?[] y, System.Action<T> z, System.Action<T?[]?>?[]? u) where T : class;
}

partial class C1
{
    partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
    { }
}";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void PartialMethods_03()
        {
            var source = @"
class C
{
    public static void Main()
    { 
    }
}

partial class C1
{
    partial void M1<T>(T x, T?[] y, System.Action<T> z, System.Action<T?[]?>?[]? u) where T : class;
}

partial class C1
{
    [System.Runtime.CompilerServices.NullableOptOut]
    partial void M1<T>(T? x, T[]? y, System.Action<T?> z, System.Action<T?[]?>?[]? u) where T : class
    { }
}";
            var compilation = CreateStandardCompilation(new[] { source, NullableOptOutAttributesDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Overloading_01()
        {
            var source = @"
class A
{
    void Test1(string? x1) {}
    void Test1(string x2) {}

    string Test2(string y1) { return y1; }
    string? Test2(string y2) { return y2; }
}
";
            CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8).
                VerifyDiagnostics(
                 // (5,10): error CS0111: Type 'A' already defines a member called 'Test1' with the same parameter types
                 //     void Test1(string x2) {}
                 Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Test1").WithArguments("Test1", "A").WithLocation(5, 10),
                 // (8,13): error CS0111: Type 'A' already defines a member called 'Test2' with the same parameter types
                 //     string? Test2(string y2) { return y2; }
                 Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Test2").WithArguments("Test2", "A").WithLocation(8, 13)
                );
        }

        [Fact]
        public void Overloading_02()
        {
            var source = @"
class A
{
    public void M1<T>(T? x) where T : struct 
    { 
    }

    public void M1<T>(T? x) where T : class 
    { 
    }
}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics();
        }

        [Fact()]
        public void Test1()
        {
            CSharpCompilation c = CreateStandardCompilation(
@"#pragma warning disable 8618
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        string? x1 = null;
        string? y1 = x1; 
        string z1 = x1; 
    }

    void Test2()
    {
        string? x2 = """";
        string z2 = x2; 
    }

    void Test3()
    {
        string? x3;
        string z3 = x3; 
    }

    void Test4()
    {
        string x4;
        string z4 = x4; 
    }

    void Test5()
    {
        string? x5 = """";
        x5 = null;
        string? y5;
        y5 = x5; 
        string z5;
        z5 = x5; 
    }

    void Test6()
    {
        string? x6 = """";
        string z6;
        z6 = x6; 
    }

    void Test7()
    {
        CL1? x7 = null;
        CL1 y7 = x7.P1; 
        CL1 z7 = x7?.P1;
        x7 = new CL1();
        CL1 u7 = x7.P1; 
    }

    void Test8()
    {
        CL1? x8 = new CL1();
        CL1 y8 = x8.M1(); 
        x8 = null;
        CL1 u8 = x8.M1(); 
        CL1 z8 = x8?.M1();
    }

    void Test9(CL1? x9, CL1 y9)
    {
        CL1 u9; 
        u9 = x9;
        u9 = y9;
        x9 = y9;
        CL1 v9; 
        v9 = x9;
        y9 = null;
    }

    void Test10(CL1 x10)
    {
        CL1 u10; 
        u10 = x10.P1;
        u10 = x10.P2;
        u10 = x10.M1();
        u10 = x10.M2();
        CL1? v10;
        v10 = x10.P2;
        v10 = x10.M2();
    }

    void Test11(CL1 x11, CL1? y11)
    {
        CL1 u11; 
        u11 = x11.F1;
        u11 = x11.F2;
        CL1? v11;
        v11 = x11.F2;
        x11.F2 = x11.F1;
        u11 = x11.F2;

        v11 = y11.F1;
    }

    void Test12(CL1 x12)
    {
        S1 y12;
        CL1 u12; 
        u12 = y12.F3;
        u12 = y12.F4;
    }

    void Test13(CL1 x13)
    {
        S1 y13;
        CL1? u13; 
        u13 = y13.F3;
        u13 = y13.F4;
    }

    void Test14(CL1 x14)
    {
        S1 y14;
        y14.F3 = null;
        y14.F4 = null;
        y14.F3 = x14;
        y14.F4 = x14;
    }

    void Test15(CL1 x15)
    {
        S1 y15;
        CL1 u15; 
        y15.F3 = null;
        y15.F4 = null;
        u15 = y15.F3;
        u15 = y15.F4;

        CL1? v15;
        v15 = y15.F4;
        y15.F4 = x15;
        u15 = y15.F4;
    }

    void Test16()
    {
        S1 y16;
        CL1 u16; 
        y16 = new S1();
        u16 = y16.F3;
        u16 = y16.F4;
    }

    void Test17(CL1 z17)
    {
        S1 x17;
        x17.F4 = z17;
        S1 y17 = new S1();
        CL1 u17; 
        u17 = y17.F4;

        y17 = x17;
        CL1 v17; 
        v17 = y17.F4;
    }

    void Test18(CL1 z18)
    {
        S1 x18;
        x18.F4 = z18;
        S1 y18 = x18;
        CL1 u18; 
        u18 = y18.F4;
    }

    void Test19(S1 x19, CL1 z19)
    {
        S1 y19;
        y19.F4 = null; 
        CL1 u19; 
        u19 = y19.F4;

        x19.F4 = z19;
        y19 = x19;
        CL1 v19;
        v19 = y19.F4;
    }

    void Test20(S1 x20, CL1 z20)
    {
        S1 y20;
        y20.F4 = z20;
        CL1 u20; 
        u20 = y20.F4;

        y20 = x20;
        CL1 v20;
        v20 = y20.F4;
    }

    S1 GetS1()
    {
        return new S1();
    }
    void Test21(CL1 z21)
    {
        S1 y21;
        y21.F4 = z21;
        CL1 u21; 
        u21 = y21.F4;

        y21 = GetS1();
        CL1 v21;
        v21 = y21.F4;
    }

    void Test22()
    {
        S1 y22;
        CL1 u22; 
        u22 = y22.F4;

        y22 = GetS1();
        CL1 v22;
        v22 = y22.F4;
    }

    void Test23(CL1 z23)
    {
        S2 y23;
        y23.F5.F4 = z23;
        CL1 u23; 
        u23 = y23.F5.F4;

        y23 = GetS2();
        CL1 v23;
        v23 = y23.F5.F4;
    }

    S2 GetS2()
    {
        return new S2();
    }

    void Test24()
    {
        S2 y24;
        CL1 u24; 
        u24 = y24.F5.F4; // 1
        u24 = y24.F5.F4; // 2

        y24 = GetS2();
        CL1 v24;
        v24 = y24.F5.F4;
    }

    void Test25(CL1 z25)
    {
        S2 y25;
        S2 x25 = GetS2();
        x25.F5.F4 = z25;
        y25 = x25;
        CL1 v25;
        v25 = y25.F5.F4;
    }

    void Test26(CL1 x26, CL1? y26, CL1 z26)
    {
        x26.P1 = y26;
        x26.P1 = z26;
    }

    void Test27(CL1 x27, CL1? y27, CL1 z27)
    {
        x27[x27] = y27;
        x27[x27] = z27;
    }

    void Test28(CL1 x28, CL1? y28, CL1 z28)
    {
        x28[y28] = z28;
    }

    void Test29(CL1 x29, CL1 y29, CL1 z29)
    {
        z29 = x29[y29];
        z29 = x29[1];
    }

    void Test30(CL1? x30, CL1 y30, CL1 z30)
    {
        z30 = x30[y30];
    }

    void Test31(CL1 x31)
    {
        x31 = default(CL1);
    }

    void Test32(CL1 x32)
    {
        var y32 = new CL1() ?? x32;
    }

    void Test33(object x33)
    {
        var y33 = new { p = (object)null } ?? x33;
    }
}

class CL1
{
    public CL1()
    {
        F1 = this;
    }

    public CL1 F1;
    public CL1? F2;

    public CL1 P1 { get; set; }
    public CL1? P2 { get; set; }

    public CL1 M1() { return new CL1(); }
    public CL1? M2() { return null; }

    public CL1 this[CL1 x]
    {
        get { return x; }
        set { }
    }

    public CL1? this[int x]
    {
        get { return null; }
        set { }
    }
}

struct S1
{
    public CL1 F3;
    public CL1? F4;
}

struct S2
{
    public S1 F5;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,21): warning CS8601: Possible null reference assignment.
                //         string z1 = x1; 
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 21),
                // (24,21): error CS0165: Use of unassigned local variable 'x3'
                //         string z3 = x3; 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(24, 21),
                // (30,21): error CS0165: Use of unassigned local variable 'x4'
                //         string z4 = x4; 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(30, 21),
                // (40,14): warning CS8601: Possible null reference assignment.
                //         z5 = x5; 
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5").WithLocation(40, 14),
                // (53,18): warning CS8602: Possible dereference of a null reference.
                //         CL1 y7 = x7.P1; 
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x7").WithLocation(53, 18),
                // (54,18): warning CS8601: Possible null reference assignment.
                //         CL1 z7 = x7?.P1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7?.P1").WithLocation(54, 18),
                // (64,18): warning CS8602: Possible dereference of a null reference.
                //         CL1 u8 = x8.M1(); 
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x8").WithLocation(64, 18),
                // (65,18): warning CS8601: Possible null reference assignment.
                //         CL1 z8 = x8?.M1();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x8?.M1()").WithLocation(65, 18),
                // (71,14): warning CS8601: Possible null reference assignment.
                //         u9 = x9;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x9").WithLocation(71, 14),
                // (76,14): warning CS8600: Cannot convert null to non-nullable reference.
                //         y9 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(76, 14),
                // (83,15): warning CS8601: Possible null reference assignment.
                //         u10 = x10.P2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x10.P2").WithLocation(83, 15),
                // (85,15): warning CS8601: Possible null reference assignment.
                //         u10 = x10.M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x10.M2()").WithLocation(85, 15),
                // (95,15): warning CS8601: Possible null reference assignment.
                //         u11 = x11.F2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11.F2").WithLocation(95, 15),
                // (101,15): warning CS8602: Possible dereference of a null reference.
                //         v11 = y11.F1;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y11").WithLocation(101, 15),
                // (108,15): error CS0170: Use of possibly unassigned field 'F3'
                //         u12 = y12.F3;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y12.F3").WithArguments("F3").WithLocation(108, 15),
                // (109,15): error CS0170: Use of possibly unassigned field 'F4'
                //         u12 = y12.F4;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y12.F4").WithArguments("F4").WithLocation(109, 15),
                // (116,15): error CS0170: Use of possibly unassigned field 'F3'
                //         u13 = y13.F3;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y13.F3").WithArguments("F3").WithLocation(116, 15),
                // (117,15): error CS0170: Use of possibly unassigned field 'F4'
                //         u13 = y13.F4;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y13.F4").WithArguments("F4").WithLocation(117, 15),
                // (123,18): warning CS8600: Cannot convert null to non-nullable reference.
                //         y14.F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(123, 18),
                // (133,18): warning CS8600: Cannot convert null to non-nullable reference.
                //         y15.F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(133, 18),
                // (136,15): warning CS8601: Possible null reference assignment.
                //         u15 = y15.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y15.F4").WithLocation(136, 15),
                // (150,15): warning CS8601: Possible null reference assignment.
                //         u16 = y16.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y16.F4").WithLocation(150, 15),
                // (161,15): error CS0165: Use of unassigned local variable 'x17'
                //         y17 = x17;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x17").WithArguments("x17").WithLocation(161, 15),
                // (159,15): warning CS8601: Possible null reference assignment.
                //         u17 = y17.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y17.F4").WithLocation(159, 15),
                // (170,18): error CS0165: Use of unassigned local variable 'x18'
                //         S1 y18 = x18;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x18").WithArguments("x18").WithLocation(170, 18),
                // (180,15): warning CS8601: Possible null reference assignment.
                //         u19 = y19.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y19.F4").WithLocation(180, 15),
                // (197,15): warning CS8601: Possible null reference assignment.
                //         v20 = y20.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y20.F4").WithLocation(197, 15),
                // (213,15): warning CS8601: Possible null reference assignment.
                //         v21 = y21.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y21.F4").WithLocation(213, 15),
                // (220,15): error CS0170: Use of possibly unassigned field 'F4'
                //         u22 = y22.F4;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y22.F4").WithArguments("F4").WithLocation(220, 15),
                // (224,15): warning CS8601: Possible null reference assignment.
                //         v22 = y22.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y22.F4").WithLocation(224, 15),
                // (236,15): warning CS8601: Possible null reference assignment.
                //         v23 = y23.F5.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y23.F5.F4").WithLocation(236, 15),
                // (248,15): error CS0170: Use of possibly unassigned field 'F4'
                //         u24 = y24.F5.F4; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y24.F5.F4").WithArguments("F4").WithLocation(248, 15),
                // (253,15): warning CS8601: Possible null reference assignment.
                //         v24 = y24.F5.F4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y24.F5.F4").WithLocation(253, 15),
                // (268,18): warning CS8601: Possible null reference assignment.
                //         x26.P1 = y26;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y26").WithLocation(268, 18),
                // (274,20): warning CS8601: Possible null reference assignment.
                //         x27[x27] = y27;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y27").WithLocation(274, 20),
                // (280,13): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1 CL1.this[CL1 x].get'.
                //         x28[y28] = z28;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y28").WithArguments("x", "CL1 CL1.this[CL1 x].get").WithLocation(280, 13),
                // (286,15): warning CS8601: Possible null reference assignment.
                //         z29 = x29[1];
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x29[1]").WithLocation(286, 15),
                // (291,15): warning CS8602: Possible dereference of a null reference.
                //         z30 = x30[y30];
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x30").WithLocation(291, 15),
                // (296,15): warning CS8600: Cannot convert null to non-nullable reference.
                //         x31 = default(CL1);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(CL1)").WithLocation(296, 15),
                // (301,19): hidden CS8607: Expression is probably never null.
                //         var y32 = new CL1() ?? x32;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new CL1()").WithLocation(301, 19),
                // (306,19): hidden CS8607: Expression is probably never null.
                //         var y33 = new { p = (object)null } ?? x33;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new { p = (object)null }").WithLocation(306, 19)
                );
        }

        [Fact]
        public void PassingParameters_1()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void M1(CL1 p) {}

    void Test1(CL1? x1, CL1 y1)
    {
        M1(x1);
        M1(y1);
    }

    void Test2()
    {
        CL1? x2;
        M1(x2);
    }

    void M2(ref CL1? p) {}

    void Test3()
    {
        CL1 x3;
        M2(ref x3);
    }

    void Test4(CL1 x4)
    {
        M2(ref x4);
    }

    void M3(out CL1? p) { p = null; }

    void Test5()
    {
        CL1 x5;
        M3(out x5);
    }

    void M4(ref CL1 p) {}

    void Test6()
    {
        CL1? x6 = null;
        M4(ref x6);
    }

    void M5(out CL1 p) { p = new CL1(); }

    void Test7()
    {
        CL1? x7 = null;
        CL1 u7 = x7;
        M5(out x7);
        CL1 v7 = x7;
    }

    void M6(CL1 p1, CL1? p2) {}

    void Test8(CL1? x8, CL1? y8)
    {
        M6(p2: x8, p1: y8);
    }

    void M7(params CL1[] p1) {}

    void Test9(CL1 x9, CL1? y9)
    {
        M7(x9, y9);
    }

    void Test10(CL1? x10, CL1 y10)
    {
        M7(x10, y10);
    }

    void M8(CL1 p1, params CL1[] p2) {}

    void Test11(CL1? x11, CL1 y11, CL1? z11)
    {
        M8(x11, y11, z11);
    }

    void Test12(CL1? x12, CL1 y12)
    {
        M8(p2: x12, p1: y12);
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,12): warning CS8604: Possible null reference argument for parameter 'p' in 'void C.M1(CL1 p)'.
                //         M1(x1);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("p", "void C.M1(CL1 p)").WithLocation(12, 12),
                // (19,12): error CS0165: Use of unassigned local variable 'x2'
                //         M1(x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(19, 12),
                // (27,16): error CS0165: Use of unassigned local variable 'x3'
                //         M2(ref x3);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(27, 16),
                // (27,16): warning CS8601: Possible null reference assignment.
                //         M2(ref x3);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(27, 16),
                // (32,16): warning CS8601: Possible null reference assignment.
                //         M2(ref x4);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4").WithLocation(32, 16),
                // (40,16): warning CS8601: Possible null reference assignment.
                //         M3(out x5);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5").WithLocation(40, 16),
                // (48,16): warning CS8604: Possible null reference argument for parameter 'p' in 'void C.M4(ref CL1 p)'.
                //         M4(ref x6);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x6").WithArguments("p", "void C.M4(ref CL1 p)").WithLocation(48, 16),
                // (56,18): warning CS8601: Possible null reference assignment.
                //         CL1 u7 = x7;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7").WithLocation(56, 18),
                // (65,24): warning CS8604: Possible null reference argument for parameter 'p1' in 'void C.M6(CL1 p1, CL1? p2)'.
                //         M6(p2: x8, p1: y8);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y8").WithArguments("p1", "void C.M6(CL1 p1, CL1? p2)").WithLocation(65, 24),
                // (72,16): warning CS8604: Possible null reference argument for parameter 'p1' in 'void C.M7(params CL1[] p1)'.
                //         M7(x9, y9);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y9").WithArguments("p1", "void C.M7(params CL1[] p1)").WithLocation(72, 16),
                // (77,12): warning CS8604: Possible null reference argument for parameter 'p1' in 'void C.M7(params CL1[] p1)'.
                //         M7(x10, y10);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x10").WithArguments("p1", "void C.M7(params CL1[] p1)").WithLocation(77, 12),
                // (84,12): warning CS8604: Possible null reference argument for parameter 'p1' in 'void C.M8(CL1 p1, params CL1[] p2)'.
                //         M8(x11, y11, z11);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x11").WithArguments("p1", "void C.M8(CL1 p1, params CL1[] p2)").WithLocation(84, 12),
                // (84,22): warning CS8604: Possible null reference argument for parameter 'p2' in 'void C.M8(CL1 p1, params CL1[] p2)'.
                //         M8(x11, y11, z11);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "z11").WithArguments("p2", "void C.M8(CL1 p1, params CL1[] p2)").WithLocation(84, 22),
                // (89,16): warning CS8604: Possible null reference argument for parameter 'p2' in 'void C.M8(CL1 p1, params CL1[] p2)'.
                //         M8(p2: x12, p1: y12);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x12").WithArguments("p2", "void C.M8(CL1 p1, params CL1[] p2)").WithLocation(89, 16)
                );
        }

        [Fact]
        public void PassingParameters_2()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0 x1)
    {
        var y1 = new CL0() { [null] = x1 };
    }
}

class CL0
{
    public CL0 this[CL0 x]
    {
        get { return x; }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,31): warning CS8600: Cannot convert null to non-nullable reference.
                //         var y1 = new CL0() { [null] = x1 };
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 31)
                );
        }

        [Fact]
        public void PassingParameters_3()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0 x1)
    {
        var y1 = new CL0() { null };
    }
}

class CL0 : System.Collections.IEnumerable 
{
    public void Add(CL0 x)
    {
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,30): warning CS8600: Cannot convert null to non-nullable reference.
                //         var y1 = new CL0() { null };
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 30)
                );
        }

        [Fact]
        public void RefOutParameters_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(ref CL1 x1, CL1 y1)
    {
        y1 = x1;
    }

    void Test2(ref CL1? x2, CL1 y2)
    {
        y2 = x2;
    }

    void Test3(ref CL1? x3, CL1 y3)
    {
        x3 = y3;
        y3 = x3;
    }

    void Test4(out CL1 x4, CL1 y4)
    {
        y4 = x4;
        x4 = y4;
    }

    void Test5(out CL1? x5, CL1 y5)
    {
        y5 = x5;
        x5 = y5;
    }

    void Test6(out CL1? x6, CL1 y6)
    {
        x6 = y6;
        y6 = x6;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,14): warning CS8601: Possible null reference assignment.
                 //         y2 = x2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(15, 14),
                 // (21,14): warning CS8601: Possible null reference assignment.
                 //         y3 = x3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(21, 14),
                 // (26,14): error CS0269: Use of unassigned out parameter 'x4'
                 //         y4 = x4;
                 Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x4").WithArguments("x4").WithLocation(26, 14),
                 // (32,14): error CS0269: Use of unassigned out parameter 'x5'
                 //         y5 = x5;
                 Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x5").WithArguments("x5").WithLocation(32, 14),
                 // (39,14): warning CS8601: Possible null reference assignment.
                 //         y6 = x6;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6").WithLocation(39, 14)
                );
        }

        [Fact]
        public void RefOutParameters_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(ref S1 x1, CL1 y1)
    {
        y1 = x1.F1;
    }

    void Test2(ref S1 x2, CL1 y2)
    {
        y2 = x2.F2;
    }

    void Test3(ref S1 x3, CL1 y3)
    {
        x3.F2 = y3;
        y3 = x3.F2;
    }

    void Test4(out S1 x4, CL1 y4)
    {
        y4 = x4.F1;
        x4.F1 = y4;
        x4.F2 = y4;
    }

    void Test5(out S1 x5, CL1 y5)
    {
        y5 = x5.F2;
        x5.F1 = y5;
        x5.F2 = y5;
    }

    void Test6(out S1 x6, CL1 y6)
    {
        x6.F1 = y6;
        x6.F2 = y6;
        y6 = x6.F2;
    }
}

class CL1
{
}

struct S1
{
    public CL1 F1;
    public CL1? F2;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,14): warning CS8601: Possible null reference assignment.
                 //         y2 = x2.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2.F2").WithLocation(15, 14),
                 // (21,14): warning CS8601: Possible null reference assignment.
                 //         y3 = x3.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3.F2").WithLocation(21, 14),
                 // (26,14): error CS0170: Use of possibly unassigned field 'F1'
                 //         y4 = x4.F1;
                 Diagnostic(ErrorCode.ERR_UseDefViolationField, "x4.F1").WithArguments("F1").WithLocation(26, 14),
                 // (33,14): error CS0170: Use of possibly unassigned field 'F2'
                 //         y5 = x5.F2;
                 Diagnostic(ErrorCode.ERR_UseDefViolationField, "x5.F2").WithArguments("F2").WithLocation(33, 14),
                 // (42,14): warning CS8601: Possible null reference assignment.
                 //         y6 = x6.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6.F2").WithLocation(42, 14)
                );
        }

        [Fact]
        public void RefOutParameters_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test3(ref S1 x3, CL1 y3)
    {
        S1 z3;
        z3.F1 = y3;
        z3.F2 = y3;
        x3 = z3;
        y3 = x3.F2;
    }

    void Test6(out S1 x6, CL1 y6)
    {
        S1 z6;
        z6.F1 = y6;
        z6.F2 = y6;
        x6 = z6;
        y6 = x6.F2;
    }
}

class CL1
{
}

struct S1
{
    public CL1 F1;
    public CL1? F2;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): warning CS8601: Possible null reference assignment.
                 //         y3 = x3.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3.F2").WithLocation(14, 14),
                 // (23,14): warning CS8601: Possible null reference assignment.
                 //         y6 = x6.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6.F2").WithLocation(23, 14)
                );
        }

        [Fact]
        public void RefOutParameters_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void M1(ref CL0<string> x) {}

    void Test1(CL0<string?> x1)
    {
        M1(ref x1);
    }

    void M2(out CL0<string?> x) { throw new System.NotImplementedException(); }

    void Test2(CL0<string> x2)
    {
        M2(out x2);
    }

    void M3(CL0<string> x) {}

    void Test3(CL0<string?> x3)
    {
        M3(x3);
    }
}

class CL0<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,16): warning CS8620: Nullability of reference types in argument of type 'CL0<string?>' doesn't match target type 'CL0<string>' for parameter 'x' in 'void C.M1(ref CL0<string> x)'.
                //         M1(ref x1);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x1").WithArguments("CL0<string?>", "CL0<string>", "x", "void C.M1(ref CL0<string> x)").WithLocation(12, 16),
                // (12,16): warning CS8619: Nullability of reference types in value of type 'CL0<string>' doesn't match target type 'CL0<string?>'.
                //         M1(ref x1);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("CL0<string>", "CL0<string?>").WithLocation(12, 16),
                // (19,16): warning CS8619: Nullability of reference types in value of type 'CL0<string?>' doesn't match target type 'CL0<string>'.
                //         M2(out x2);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x2").WithArguments("CL0<string?>", "CL0<string>").WithLocation(19, 16),
                // (26,12): warning CS8620: Nullability of reference types in argument of type 'CL0<string?>' doesn't match target type 'CL0<string>' for parameter 'x' in 'void C.M3(CL0<string> x)'.
                //         M3(x3);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x3").WithArguments("CL0<string?>", "CL0<string>", "x", "void C.M3(CL0<string> x)").WithLocation(26, 12)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_01()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public object F1;

    public object P1 { get; set;}

    public object this[object x]
    {
        get { return null; }
        set { }
    }

    public S1 M1() { return new S1(); }
}

public struct S1
{
    public CL0 F1;
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    bool Test1(string? x1, string y1)
    {
        return string.Equals(x1, y1);
    }

    object Test2(ref object? x2, object? y2)
    {
        System.Threading.Interlocked.Exchange(ref x2, y2);
        return x2 ?? new object(); 
    }    

    object Test3(ref object? x3, object? y3)
    {
        return System.Threading.Interlocked.Exchange(ref x3, y3) ?? new object(); 
    }    

    object Test4(System.Delegate x4)
    {
        return x4.Target ?? new object(); 
    }    

    object Test5(CL0 x5)
    {
        return x5.F1 ?? new object(); 
    }    

    void Test6(CL0 x6, object? y6)
    {
        x6.F1 = y6;
    }    

    void Test7(CL0 x7, object? y7)
    {
        x7.P1 = y7;
    }    

    void Test8(CL0 x8, object? y8, object? z8)
    {
        x8[y8] = z8;
    }    

    object Test9(CL0 x9)
    {
        return x9[1] ?? new object(); 
    }    

    object Test10(CL0 x10)
    {
        return x10.M1().F1 ?? new object(); 
    }    

    object Test11(CL0 x11, CL0? z11)
    {
        S1 y11 = x11.M1();
        y11.F1 = z11;
        return y11.F1; 
    }    

    object Test12(CL0 x12)
    {
        S1 y12 = x12.M1();
        y12.F1 = x12;
        return y12.F1 ?? new object(); 
    }    

    void Test13(CL0 x13, object? y13)
    {
        y13 = x13.F1;
        object z13 = y13;
        z13 = y13 ?? new object();
    }    

    void Test14(CL0 x14)
    {
        object? y14 = x14.F1;
        object z14 = y14;
        z14 = y14 ?? new object();
    }    

    void Test15(CL0 x15)
    {
        S2 y15;
        y15.F2 = x15.F1;
        object z15 = y15.F2;
        z15 = y15.F2 ?? new object();
    }    

    struct Test16
    {
        object? y16 {get;}

        public Test16(CL0 x16)
        {
            y16 = x16.F1;
            object z16 = y16;
            z16 = y16 ?? new object();
        }    
    }

    void Test17(CL0 x17)
    {
        var y17 = new { F2 = x17.F1 };
        object z17 = y17.F2;
        z17 = y17.F2 ?? new object();
    }    
}

public struct S2
{
    public object? F2;
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (10,30): warning CS8604: Possible null reference argument for parameter 'a' in 'bool string.Equals(string a, string b)'.
                //         return string.Equals(x1, y1);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("a", "bool string.Equals(string a, string b)").WithLocation(10, 30),
                // (15,51): warning CS8604: Possible null reference argument for parameter 'location1' in 'object Interlocked.Exchange(ref object location1, object value)'.
                //         System.Threading.Interlocked.Exchange(ref x2, y2);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("location1", "object Interlocked.Exchange(ref object location1, object value)").WithLocation(15, 51),
                // (15,55): warning CS8604: Possible null reference argument for parameter 'value' in 'object Interlocked.Exchange(ref object location1, object value)'.
                //         System.Threading.Interlocked.Exchange(ref x2, y2);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y2").WithArguments("value", "object Interlocked.Exchange(ref object location1, object value)").WithLocation(15, 55),
                // (21,58): warning CS8604: Possible null reference argument for parameter 'location1' in 'object Interlocked.Exchange(ref object location1, object value)'.
                //         return System.Threading.Interlocked.Exchange(ref x3, y3) ?? new object(); 
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x3").WithArguments("location1", "object Interlocked.Exchange(ref object location1, object value)").WithLocation(21, 58),
                // (21,62): warning CS8604: Possible null reference argument for parameter 'value' in 'object Interlocked.Exchange(ref object location1, object value)'.
                //         return System.Threading.Interlocked.Exchange(ref x3, y3) ?? new object(); 
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y3").WithArguments("value", "object Interlocked.Exchange(ref object location1, object value)").WithLocation(21, 62),
                // (21,16): hidden CS8607: Expression is probably never null.
                //         return System.Threading.Interlocked.Exchange(ref x3, y3) ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "System.Threading.Interlocked.Exchange(ref x3, y3)").WithLocation(21, 16),
                // (26,16): hidden CS8607: Expression is probably never null.
                //         return x4.Target ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4.Target").WithLocation(26, 16),
                // (31,16): hidden CS8607: Expression is probably never null.
                //         return x5.F1 ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x5.F1").WithLocation(31, 16),
                // (36,17): warning CS8601: Possible null reference assignment.
                //         x6.F1 = y6;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y6").WithLocation(36, 17),
                // (41,17): warning CS8601: Possible null reference assignment.
                //         x7.P1 = y7;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y7").WithLocation(41, 17),
                // (46,12): warning CS8604: Possible null reference argument for parameter 'x' in 'object CL0.this[object x].get'.
                //         x8[y8] = z8;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y8").WithArguments("x", "object CL0.this[object x].get").WithLocation(46, 12),
                // (46,18): warning CS8601: Possible null reference assignment.
                //         x8[y8] = z8;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z8").WithLocation(46, 18),
                // (51,16): hidden CS8607: Expression is probably never null.
                //         return x9[1] ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x9[1]").WithLocation(51, 16),
                // (56,16): hidden CS8607: Expression is probably never null.
                //         return x10.M1().F1 ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x10.M1().F1").WithLocation(56, 16),
                // (62,18): warning CS8601: Possible null reference assignment.
                //         y11.F1 = z11;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z11").WithLocation(62, 18),
                // (70,16): hidden CS8607: Expression is probably never null.
                //         return y12.F1 ?? new object(); 
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y12.F1").WithLocation(70, 16),
                // (77,15): hidden CS8607: Expression is probably never null.
                //         z13 = y13 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y13").WithLocation(77, 15),
                // (84,15): hidden CS8607: Expression is probably never null.
                //         z14 = y14 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y14").WithLocation(84, 15),
                // (92,15): hidden CS8607: Expression is probably never null.
                //         z15 = y15.F2 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y15.F2").WithLocation(92, 15),
                // (103,19): hidden CS8607: Expression is probably never null.
                //             z16 = y16 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y16").WithLocation(103, 19),
                // (111,15): hidden CS8607: Expression is probably never null.
                //         z17 = y17.F2 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y17.F2").WithLocation(111, 15)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_02()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }

    void Test1()
    {
        object? x1 = CL0.M1() ?? M2();
        object y1 = x1;
        object z1 = x1 ?? new object();
    }

    void Test2()
    {
        object? x2 = CL0.M1() ?? M3();
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object? x3 = M3() ?? M2();
        object z3 = x3 ?? new object();
    }

    void Test4()
    {
        object? x4 = CL0.M1() ?? CL0.M1();
        object y4 = x4;
        object z4 = x4 ?? new object();
    }

    void Test5()
    {
        object x5 = M2() ?? M2();
    }

    void Test6()
    {
        object? x6 = M3() ?? M3();
        object z6 = x6 ?? new object();
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (13,22): hidden CS8607: Expression is probably never null.
                //         object? x1 = CL0.M1() ?? M2();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "CL0.M1()").WithLocation(13, 22),
                // (15,21): hidden CS8607: Expression is probably never null.
                //         object z1 = x1 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1").WithLocation(15, 21),
                // (20,22): hidden CS8607: Expression is probably never null.
                //         object? x2 = CL0.M1() ?? M3();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "CL0.M1()").WithLocation(20, 22),
                // (21,21): hidden CS8607: Expression is probably never null.
                //         object z2 = x2 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(21, 21),
                // (26,22): hidden CS8607: Expression is probably never null.
                //         object? x3 = M3() ?? M2();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M3()").WithLocation(26, 22),
                // (27,21): hidden CS8607: Expression is probably never null.
                //         object z3 = x3 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x3").WithLocation(27, 21),
                // (32,22): hidden CS8607: Expression is probably never null.
                //         object? x4 = CL0.M1() ?? CL0.M1();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "CL0.M1()").WithLocation(32, 22),
                // (34,21): hidden CS8607: Expression is probably never null.
                //         object z4 = x4 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4").WithLocation(34, 21),
                // (39,21): warning CS8601: Possible null reference assignment.
                //         object x5 = M2() ?? M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M2() ?? M2()").WithLocation(39, 21),
                // (44,22): hidden CS8607: Expression is probably never null.
                //         object? x6 = M3() ?? M3();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M3()").WithLocation(44, 22),
                // (45,21): hidden CS8607: Expression is probably never null.
                //         object z6 = x6 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x6").WithLocation(45, 21)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_03()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }

    void Test1()
    {
        object? x1 = M2() ?? CL0.M1();
        object y1 = x1;
        object z1 = x1 ?? new object();
    }

    void Test2()
    {
        object? x2 = M3() ?? CL0.M1();
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object? x3 = M2() ?? M3();
        object z3 = x3 ?? new object();
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                 // (20,22): hidden CS8607: Expression is probably never null.
                 //         object? x2 = M3() ?? CL0.M1();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M3()").WithLocation(20, 22),
                 // (21,21): hidden CS8607: Expression is probably never null.
                 //         object z2 = x2 ?? new object();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(21, 21),
                 // (27,21): hidden CS8607: Expression is probably never null.
                 //         object z3 = x3 ?? new object();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x3").WithLocation(27, 21)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_04()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }
    public static bool M4() {return false;}

    void Test1()
    {
        object x1 = M4() ? CL0.M1() : M2();
    }

    void Test2()
    {
        object? x2 = M4() ? CL0.M1() : M3();
        object y2 = x2;
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object x3 =  M4() ? M3() : M2();
    }

    void Test4()
    {
        object? x4 =  M4() ? CL0.M1() : CL0.M1();
        object y4 = x4;
        object z4 = x4 ?? new object();
    }

    void Test5()
    {
        object x5 =  M4() ? M2() : M2();
    }

    void Test6()
    {
        object? x6 =  M4() ? M3() : M3();
        object z6 = x6 ?? new object();
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (14,21): warning CS8601: Possible null reference assignment.
                //         object x1 = M4() ? CL0.M1() : M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M4() ? CL0.M1() : M2()").WithLocation(14, 21),
                // (26,22): warning CS8601: Possible null reference assignment.
                //         object x3 =  M4() ? M3() : M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M4() ? M3() : M2()").WithLocation(26, 22),
                // (38,22): warning CS8601: Possible null reference assignment.
                //         object x5 =  M4() ? M2() : M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M4() ? M2() : M2()").WithLocation(38, 22),
                // (44,21): hidden CS8607: Expression is probably never null.
                //         object z6 = x6 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x6").WithLocation(44, 21)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_05()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }
    public static bool M4() {return false;}

    void Test1()
    {
        object x1 = M4() ? M2() : CL0.M1();
    }

    void Test2()
    {
        object? x2 = M4() ? M3() : CL0.M1();
        object y2 = x2;
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object x3 =  M4() ? M2() : M3();
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                 // (14,21): warning CS8601: Possible null reference assignment.
                 //         object x1 = M4() ? M2() : CL0.M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M4() ? M2() : CL0.M1()").WithLocation(14, 21),
                 // (26,22): warning CS8601: Possible null reference assignment.
                 //         object x3 =  M4() ? M2() : M3();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M4() ? M2() : M3()").WithLocation(26, 22)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_06()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }
    public static bool M4() {return false;}

    void Test1()
    {
        object? x1;
        if (M4()) x1 = CL0.M1(); else x1 = M2();
        object y1 = x1;
    }

    void Test2()
    {
        object? x2;
        if (M4()) x2 = CL0.M1(); else x2 = M3();
        object y2 = x2;
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object? x3;
        if (M4()) x3 = M3(); else x3 = M2();
        object y3 = x3;
    }

    void Test4()
    {
        object? x4;
        if (M4()) x4 = CL0.M1(); else x4 = CL0.M1();
        object y4 = x4;
        object z4 = x4 ?? new object();
    }

    void Test5()
    {
        object? x5;
        if (M4()) x5 = M2(); else x5 = M2();
        object y5 = x5;
    }

    void Test6()
    {
        object? x6;
        if (M4()) x6 = M3(); else x6 = M3();
        object z6 = x6 ?? new object();
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (16,21): warning CS8601: Possible null reference assignment.
                //         object y1 = x1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(16, 21),
                // (24,21): hidden CS8607: Expression is probably never null.
                //         object z2 = x2 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(24, 21),
                // (31,21): warning CS8601: Possible null reference assignment.
                //         object y3 = x3;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(31, 21),
                // (39,21): hidden CS8607: Expression is probably never null.
                //         object z4 = x4 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4").WithLocation(39, 21),
                // (46,21): warning CS8601: Possible null reference assignment.
                //         object y5 = x5;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5").WithLocation(46, 21),
                // (53,21): hidden CS8607: Expression is probably never null.
                //         object z6 = x6 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x6").WithLocation(53, 21)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_07()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public static object M1() { return new object(); }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main()
    {
    }

    public static object? M2() { return null; }
    public static object M3() { return new object(); }
    public static bool M4() {return false;}

    void Test1()
    {
        object? x1;
        if (M4()) x1 = M2(); else x1 = CL0.M1();
        object y1 = x1;
    }

    void Test2()
    {
        object? x2;
        if (M4()) x2 = M3(); else x2 = CL0.M1();
        object y2 = x2;
        object z2 = x2 ?? new object();
    }

    void Test3()
    {
        object? x3;
        if (M4()) x3 = M2(); else x3 = M3();
        object y3 = x3;
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (16,21): warning CS8601: Possible null reference assignment.
                //         object y1 = x1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(16, 21),
                // (31,21): warning CS8601: Possible null reference assignment.
                //         object y3 = x3;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(31, 21)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs_08()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public abstract class A1
{
    public abstract event System.Action E1;
    public abstract string P2 { get; set; }
    public abstract string M3(string x);
    public abstract event System.Action E4;
    public abstract string this[string x] { get; set; }
}

public interface IA2
{
    event System.Action E5;
    string P6 { get; set; }
    string M7(string x);
    event System.Action E8;
    string this[string x] { get; set; }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class B1 : A1
{
    static void Main()
    {
    }

    public override string? P2 { get; set; }
    public override event System.Action? E1;
    public override string? M3(string? x)
    {
        var dummy = E1;
        throw new System.NotImplementedException();
    }
    public override event System.Action? E4
    {
        add { }
        remove { }
    }

    public override string? this[string? x]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}

class B2 : IA2
{
    public string? P6 { get; set; }
    public event System.Action? E5;
    public event System.Action? E8
    {
        add { }
        remove { }
    }

    public string? M7(string? x)
    {
        var dummy = E5;
        throw new System.NotImplementedException();
    }

    public string? this[string? x]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}

class B3 : IA2
{
    string? IA2.P6 { get; set; }

    event System.Action? IA2.E5
    {
        add { }
        remove { }
    }

    event System.Action? IA2.E8
    {
        add { }
        remove { }
    }

    string? IA2.M7(string? x)
    {
        throw new System.NotImplementedException();
    }
    
    string? IA2.this[string? x]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }

}
", parseOptions: TestOptions.Regular8, references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (8,29): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                //     public override string? P2 { get; set; }
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "P2").WithLocation(8, 29),
                // (9,42): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                //     public override event System.Action? E1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E1").WithLocation(9, 42),
                // (10,29): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override string? M3(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M3").WithLocation(10, 29),
                // (10,29): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                //     public override string? M3(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M3").WithArguments("x").WithLocation(10, 29),
                // (15,42): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                //     public override event System.Action? E4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "E4").WithLocation(15, 42),
                // (21,29): warning CS8608: Nullability of reference types in type doesn't match overridden member.
                //     public override string? this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, "this").WithLocation(21, 29),
                // (21,29): warning CS8610: Nullability of reference types in type of parameter 'x' doesn't match overridden member.
                //     public override string? this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "this").WithArguments("x").WithLocation(21, 29),
                // (39,33): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action IA2.E8'.
                //     public event System.Action? E8
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E8").WithArguments("event Action IA2.E8").WithLocation(39, 33),
                // (45,20): warning CS8613: Nullability of reference types in return type doesn't match implicitly implemented member 'string IA2.M7(string x)'.
                //     public string? M7(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation, "M7").WithArguments("string IA2.M7(string x)").WithLocation(45, 20),
                // (45,20): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'string IA2.M7(string x)'.
                //     public string? M7(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "M7").WithArguments("x", "string IA2.M7(string x)").WithLocation(45, 20),
                // (37,20): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string IA2.P6'.
                //     public string? P6 { get; set; }
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "P6").WithArguments("string IA2.P6").WithLocation(37, 20),
                // (51,20): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'string IA2.this[string x]'.
                //     public string? this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "this").WithArguments("string IA2.this[string x]").WithLocation(51, 20),
                // (51,20): warning CS8614: Nullability of reference types in type of parameter 'x' doesn't match implicitly implemented member 'string IA2.this[string x]'.
                //     public string? this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation, "this").WithArguments("x", "string IA2.this[string x]").WithLocation(51, 20),
                // (38,33): warning CS8612: Nullability of reference types in type doesn't match implicitly implemented member 'event Action IA2.E5'.
                //     public event System.Action? E5;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation, "E5").WithArguments("event Action IA2.E5").WithLocation(38, 33),
                // (67,17): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string IA2.P6'.
                //     string? IA2.P6 { get; set; }
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "P6").WithArguments("string IA2.P6").WithLocation(67, 17),
                // (69,30): warning CS8615: Nullability of reference types in type doesn't match implemented member 'event Action IA2.E5'.
                //     event System.Action? IA2.E5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "E5").WithArguments("event Action IA2.E5").WithLocation(69, 30),
                // (75,30): warning CS8615: Nullability of reference types in type doesn't match implemented member 'event Action IA2.E8'.
                //     event System.Action? IA2.E8
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "E8").WithArguments("event Action IA2.E8").WithLocation(75, 30),
                // (86,17): warning CS8615: Nullability of reference types in type doesn't match implemented member 'string IA2.this[string x]'.
                //     string? IA2.this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation, "this").WithArguments("string IA2.this[string x]").WithLocation(86, 17),
                // (86,17): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'string IA2.this[string x]'.
                //     string? IA2.this[string? x]
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "this").WithArguments("x", "string IA2.this[string x]").WithLocation(86, 17),
                // (81,17): warning CS8616: Nullability of reference types in return type doesn't match implemented member 'string IA2.M7(string x)'.
                //     string? IA2.M7(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation, "M7").WithArguments("string IA2.M7(string x)").WithLocation(81, 17),
                // (81,17): warning CS8617: Nullability of reference types in type of parameter 'x' doesn't match implemented member 'string IA2.M7(string x)'.
                //     string? IA2.M7(string? x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation, "M7").WithArguments("x", "string IA2.M7(string x)").WithLocation(81, 17)
                );
        }

        [Fact]
        public void ReturningValues_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1 Test1(CL1? x1)
    {
        return x1;
    }

    CL1? Test2(CL1? x2)
    {
        return x2;
    }

    CL1? Test3(CL1 x3)
    {
        return x3;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,16): warning CS8603: Possible null reference return.
                 //         return x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x1").WithLocation(10, 16)
                );
        }

        [Fact]
        public void ReturningValues_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1<string?> Test1(CL1<string> x1)
    {
        return x1;
    }

    CL1<string> Test2(CL1<string?> x2)
    {
        return x2;
    }
}

class CL1<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,16): warning CS8619: Nullability of reference types in value of type 'CL1<string>' doesn't match target type 'CL1<string?>'.
                //         return x1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("CL1<string>", "CL1<string?>").WithLocation(10, 16),
                // (15,16): warning CS8619: Nullability of reference types in value of type 'CL1<string?>' doesn't match target type 'CL1<string>'.
                //         return x2;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x2").WithArguments("CL1<string?>", "CL1<string>").WithLocation(15, 16)
                );
        }

        [Fact]
        public void ConditionalBranching_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1)
    {
        if (y1 != null)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2)
    {
        if (y2 == null)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    void Test3(CL2 x3, CL2? y3, CL2 z3)
    {
        if (y3 != null)
        {
            x3 = y3;
        }
        else
        {
            z3 = y3;
        }
    }

    void Test4(CL2 x4, CL2? y4, CL2 z4)
    {
        if (y4 == null)
        {
            x4 = y4;
        }
        else
        {
            z4 = y4;
        }
    }

    void Test5(CL1 x5, CL1 y5, CL1 z5)
    {
        if (y5 != null)
        {
            x5 = y5;
        }
        else
        {
            z5 = y5;
        }
    }
}

class CL1
{
}

class CL2
{
    public static bool operator == (CL2? x, CL2? y) { return false; }
    public static bool operator != (CL2? x, CL2? y) { return false; }
    public override bool Equals(object obj) { return false; }
    public override int GetHashCode() { return 0; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //             z1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
                 // (24,18): warning CS8601: Possible null reference assignment.
                 //             x2 = y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(24, 18),
                 // (40,18): warning CS8601: Possible null reference assignment.
                 //             z3 = y3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y3").WithLocation(40, 18),
                 // (48,18): warning CS8601: Possible null reference assignment.
                 //             x4 = y4;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4").WithLocation(48, 18),
                 // (58,13): hidden CS8605: Result of the comparison is possibly always true.
                 //         if (y5 != null)
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "y5 != null").WithLocation(58, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1)
    {
        if (null != y1)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2)
    {
        if (null == y2)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    void Test3(CL2 x3, CL2? y3, CL2 z3)
    {
        if (null != y3)
        {
            x3 = y3;
        }
        else
        {
            z3 = y3;
        }
    }

    void Test4(CL2 x4, CL2? y4, CL2 z4)
    {
        if (null == y4)
        {
            x4 = y4;
        }
        else
        {
            z4 = y4;
        }
    }

    void Test5(CL1 x5, CL1 y5, CL1 z5)
    {
        if (null == y5)
        {
            x5 = y5;
        }
        else
        {
            z5 = y5;
        }
    }
}

class CL1
{
}

class CL2
{
    public static bool operator == (CL2? x, CL2? y) { return false; }
    public static bool operator != (CL2? x, CL2? y) { return false; }
    public override bool Equals(object obj) { return false; }
    public override int GetHashCode() { return 0; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //             z1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
                 // (24,18): warning CS8601: Possible null reference assignment.
                 //             x2 = y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(24, 18),
                 // (40,18): warning CS8601: Possible null reference assignment.
                 //             z3 = y3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y3").WithLocation(40, 18),
                 // (48,18): warning CS8601: Possible null reference assignment.
                 //             x4 = y4;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4").WithLocation(48, 18),
                 // (58,13): hidden CS8606: Result of the comparison is possibly always false.
                 //         if (null == y5)
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "null == y5").WithLocation(58, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1, bool u1)
    {
        if (null != y1 || u1)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2, bool u2)
    {
        if (y2 != null && u2)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    bool Test3(CL1? x3)
    {
        return x3.M1();
    }

    bool Test4(CL1? x4)
    {
        return x4 != null && x4.M1();
    }

    bool Test5(CL1? x5)
    {
        return x5 == null && x5.M1();
    }
}

class CL1
{
    public bool M1() { return true; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,18): warning CS8601: Possible null reference assignment.
                 //             x1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(12, 18),
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //             z1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
                 // (28,18): warning CS8601: Possible null reference assignment.
                 //             z2 = y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(28, 18),
                 // (34,16): warning CS8602: Possible dereference of a null reference.
                 //         return x3.M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(34, 16),
                 // (44,30): warning CS8602: Possible dereference of a null reference.
                 //         return x5 == null && x5.M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x5").WithLocation(44, 30)
                );
        }

        [Fact]
        public void ConditionalBranching_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 ?? x1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 ?? x2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 ?? y3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 ?? x4.M1();
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 ?? x5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 ?? x6.M2();
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (15,18): warning CS8601: Possible null reference assignment.
                //         CL1 z2 = y2 ?? x2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 ?? x2").WithLocation(15, 18),
                // (20,18): hidden CS8607: Expression is probably never null.
                //         CL1 z3 = x3 ?? y3;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x3").WithLocation(20, 18),
                // (26,18): hidden CS8607: Expression is probably never null.
                //         CL1 z4 = x4 ?? x4.M1();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4").WithLocation(26, 18)
                );
        }

        [Fact]
        public void ConditionalBranching_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1)
    {
        CL1 z1 = x1?.M1();
    }

    void Test2(CL1? x2, CL1 y2)
    {
        x2 = y2;
        CL1 z2 = x2?.M1();
    }

    void Test3(CL1? x3, CL1 y3)
    {
        x3 = y3;
        CL1 z3 = x3?.M2();
    }

    void Test4(CL1? x4)
    {
        x4?.M3(x4);
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public CL1? M2() { return null; }
    public void M3(CL1 x) { }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,18): warning CS8601: Possible null reference assignment.
                 //         CL1 z1 = x1?.M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1?.M1()").WithLocation(10, 18),
                 // (16,18): hidden CS8607: Expression is probably never null.
                 //         CL1 z2 = x2?.M1();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(16, 18),
                 // (22,18): hidden CS8607: Expression is probably never null.
                 //         CL1 z3 = x3?.M2();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x3").WithLocation(22, 18),
                 // (22,18): warning CS8601: Possible null reference assignment.
                 //         CL1 z3 = x3?.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3?.M2()").WithLocation(22, 18)
                );
        }

        [Fact]
        public void ConditionalBranching_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 != null ? y1 : x1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 != null ? y2 : x2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 != null ? x3 : y3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 != null ? x4 : x4.M1();
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 != null ? y5 : x5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 != null ? y6 : x6.M2();
    }

    void Test7(CL1 x7)
    {
        const string? y7 = null;
        string z7 = y7 != null ? y7 : x7.M2();
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (15,18): warning CS8601: Possible null reference assignment.
                //         CL1 z2 = y2 != null ? y2 : x2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 != null ? y2 : x2").WithLocation(15, 18),
                // (20,18): hidden CS8605: Result of the comparison is possibly always true.
                //         CL1 z3 = x3 != null ? x3 : y3;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "x3 != null").WithLocation(20, 18),
                // (20,18): warning CS8601: Possible null reference assignment.
                //         CL1 z3 = x3 != null ? x3 : y3;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 != null ? x3 : y3").WithLocation(20, 18),
                // (26,18): hidden CS8605: Result of the comparison is possibly always true.
                //         CL1 z4 = x4 != null ? x4 : x4.M1();
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "x4 != null").WithLocation(26, 18),
                // (38,21): hidden CS8605: Result of the comparison is possibly always true.
                //         string z6 = y6 != null ? y6 : x6.M2();
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "y6 != null").WithLocation(38, 21),
                // (44,21): warning CS8601: Possible null reference assignment.
                //         string z7 = y7 != null ? y7 : x7.M2();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y7 != null ? y7 : x7.M2()").WithLocation(44, 21)
                );
        }

        [Fact]
        public void ConditionalBranching_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 == null ? x1 : y1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 == null ? x2 : y2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 == null ? y3 : x3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 == null ? x4.M1() : x4;
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 == null ? x5 : y5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 == null ? x6.M2() : y6;
    }

    void Test7(CL1 x7)
    {
        const string? y7 = null;
        string z7 = y7 == null ? x7.M2() : y7;
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (15,18): warning CS8601: Possible null reference assignment.
                //         CL1 z2 = y2 == null ? x2 : y2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 == null ? x2 : y2").WithLocation(15, 18),
                // (20,18): hidden CS8606: Result of the comparison is possibly always false.
                //         CL1 z3 = x3 == null ? y3 : x3;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "x3 == null").WithLocation(20, 18),
                // (20,18): warning CS8601: Possible null reference assignment.
                //         CL1 z3 = x3 == null ? y3 : x3;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 == null ? y3 : x3").WithLocation(20, 18),
                // (26,18): hidden CS8606: Result of the comparison is possibly always false.
                //         CL1 z4 = x4 == null ? x4.M1() : x4;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "x4 == null").WithLocation(26, 18),
                // (38,21): hidden CS8606: Result of the comparison is possibly always false.
                //         string z6 = y6 == null ? x6.M2() : y6;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "y6 == null").WithLocation(38, 21),
                // (44,21): warning CS8601: Possible null reference assignment.
                //         string z7 = y7 == null ? x7.M2() : y7;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y7 == null ? x7.M2() : y7").WithLocation(44, 21)
                );
        }

        [Fact(Skip = "Unexpected warning")]
        public void ConditionalBranching_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    bool Test1(CL1? x1)
    {
        if (x1?.P1 == true)
        {
            return x1.P2;
        }

        return false;
    }
}

class CL1
{
    public bool P1 { get { return true;} }
    public bool P2 { get { return true;} }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void ConditionalBranching_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(object x1, object? y1)
    {
        y1 = x1;
        y1.ToString();
        object z1 = y1 ?? x1;
        y1.ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,21): hidden CS8607: Expression is probably never null.
                 //         object z1 = y1 ?? x1;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y1").WithLocation(12, 21)
                );
        }

        [Fact]
        public void ConditionalBranching_10()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(object x1, object? y1)
    {
        y1 = x1;
        y1.ToString();
        object z1 = y1 != null ? y1 : x1;
        y1.ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,21): hidden CS8605: Result of the comparison is possibly always true.
                 //         object z1 = y1 != null ? y1 : x1;
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "y1 != null").WithLocation(12, 21)
                );
        }

        [Fact]
        public void ConditionalBranching_11()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(object x1, object? y1)
    {
        y1 = x1;
        y1.ToString();
        y1?.GetHashCode();
        y1.ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,9): hidden CS8607: Expression is probably never null.
                 //         y1?.GetHashCode();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y1").WithLocation(12, 9)
                );
        }

        [Fact]
        public void ConditionalBranching_12()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(object x1, object? y1)
    {
        y1 = x1;
        y1.ToString();

        if (y1 == null)
        {
            System.Console.WriteLine(1);
        }
        else
        {
            System.Console.WriteLine(2);
        }

        y1.ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (13,13): hidden CS8606: Result of the comparison is possibly always false.
                 //         if (y1 == null)
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "y1 == null").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_13()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(object x1, object? y1)
    {
        y1 = x1;
        y1.ToString();

        if (y1 != null)
        {
            System.Console.WriteLine(1);
        }
        else
        {
            System.Console.WriteLine(2);
        }

        y1.ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (13,13): hidden CS8605: Result of the comparison is possibly always true.
                 //         if (y1 != null)
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "y1 != null").WithLocation(13, 13)
                );
        }

        [Fact]
        public void Loop_1()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL1 y1, CL1? z1)
    {
        x1 = y1;
        x1.M1(); // 1

        for (int i = 0; i < 2; i++)
        {
            x1.M1(); // 2
            x1 = z1;
        }
    }

    CL1 Test2(CL1? x2, CL1 y2, CL1? z2)
    {
        x2 = y2;
        x2.M1(); // 1

        for (int i = 0; i < 2; i++)
        {
            x2 = z2;
            x2.M1(); // 2
            y2 = x2;
            y2.M2(x2);

            if (i == 1)
            {
                return x2;
            }
        }

        return y2;
    }
}

class CL1
{
    public void M1() { }
    public void M2(CL1 x) { }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,13): warning CS8602: Possible dereference of a null reference.
                 //             x1.M1(); // 2
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(15, 13),
                 // (28,13): warning CS8602: Possible dereference of a null reference.
                 //             x2.M1(); // 2
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(28, 13),
                 // (29,18): warning CS8601: Possible null reference assignment.
                 //             y2 = x2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(29, 18),
                 // (30,19): warning CS8604: Possible null reference argument for parameter 'x' in 'void CL1.M2(CL1 x)'.
                 //             y2.M2(x2);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "void CL1.M2(CL1 x)").WithLocation(30, 19),
                 // (34,24): warning CS8603: Possible null reference return.
                 //                 return x2;
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x2").WithLocation(34, 24)
                );
        }

        [Fact]
        public void Loop_2()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL1 y1, CL1? z1)
    {
        x1 = y1;
        if (x1 == null) {} // 1

        for (int i = 0; i < 2; i++)
        {
            if (x1 == null) {} // 2
            x1 = z1;
        }
    }

    void Test2(CL1? x2, CL1 y2, CL1? z2)
    {
        x2 = y2;
        if (x2 == null) {} // 1

        for (int i = 0; i < 2; i++)
        {
            x2 = z2;
            if (x2 == null) {} // 2
        }
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (11,13): hidden CS8606: Result of the comparison is possibly always false.
                 //         if (x1 == null) {} // 1
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "x1 == null").WithLocation(11, 13),
                 // (23,13): hidden CS8606: Result of the comparison is possibly always false.
                 //         if (x2 == null) {} // 1
                 Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, "x2 == null").WithLocation(23, 13)
                );
        }

        [Fact]
        public void Var_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? Test1()
    {
        var x1 = (CL1)null;
        return x1;
    }

    CL1? Test2(CL1 x2)
    {
        var y2 = x2;
        y2 = null;
        return y2;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (17,14): warning CS8600: Cannot convert null to non-nullable reference.
                //         y2 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(17, 14)
                );
        }

        [Fact]
        public void Array_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? [] x1)
    {
        CL1? y1 = x1[0];
        CL1 z1 = x1[0];
    }

    void Test2(CL1 [] x2, CL1 y2, CL1? z2)
    {
        x2[0] = y2;
        x2[1] = z2;
    }

    void Test3(CL1 [] x3)
    {
        CL1? y3 = x3[0];
        CL1 z3 = x3[0];
    }

    void Test4(CL1? [] x4, CL1 y4, CL1? z4)
    {
        x4[0] = y4;
        x4[1] = z4;
    }

    void Test5(CL1 y5, CL1? z5)
    {
        var x5 = new CL1 [] { y5, z5 };
    }

    void Test6(CL1 y6, CL1? z6)
    {
        var x6 = new CL1 [,] { {y6}, {z6} };
    }

    void Test7(CL1 y7, CL1? z7)
    {
        var u7 = new CL1? [] { y7, z7 };
        var v7 = new CL1? [,] { {y7}, {z7} };
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (11,18): warning CS8601: Possible null reference assignment.
                 //         CL1 z1 = x1[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1[0]").WithLocation(11, 18),
                 // (17,17): warning CS8601: Possible null reference assignment.
                 //         x2[1] = z2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z2").WithLocation(17, 17),
                 // (34,35): warning CS8601: Possible null reference assignment.
                 //         var x5 = new CL1 [] { y5, z5 };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z5").WithLocation(34, 35),
                 // (39,39): warning CS8601: Possible null reference assignment.
                 //         var x6 = new CL1 [,] { {y6}, {z6} };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z6").WithLocation(39, 39)
                );
        }

        [Fact]
        public void Array_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 y1, CL1? z1)
    {
        CL1? [] u1 = new [] { y1, z1 };
        CL1? [,] v1 = new [,] { {y1}, {z1} };
    }

    void Test2(CL1 y2, CL1? z2)
    {
        var u2 = new [] { y2, z2 };
        var v2 = new [,] { {y2}, {z2} };

        u2[0] = z2;
        v2[0,0] = z2;
    }

    void Test3(CL1 y3, CL1? z3)
    {
        CL1? [] u3;
        CL1? [,] v3;

        u3 = new [] { y3, z3 };
        v3 = new [,] { {y3}, {z3} };
    }

    void Test4(CL1 y4, CL1? z4)
    {
        var u4 = new [] { y4 };
        var v4 = new [,] {{y4}};

        u4[0] = z4;
        v4[0,0] = z4;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (37,17): warning CS8601: Possible null reference assignment.
                //         u4[0] = z4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z4").WithLocation(37, 17),
                // (38,19): warning CS8601: Possible null reference assignment.
                //         v4[0,0] = z4;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z4").WithLocation(38, 19)
                );
        }

        [Fact]
        public void Array_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        int[]? u1 = new [] { 1, 2 };
        u1 = null;
        var z1 = u1[0];
    }

    void Test2()
    {
        int[]? u1 = new [] { 1, 2 };
        u1 = null;
        var z1 = u1?[u1[0]];
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,18): warning CS8602: Possible dereference of a null reference.
                //         var z1 = u1[0];
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u1").WithLocation(12, 18)
                );
        }

        [Fact]
        public void Array_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 y1, CL1? z1)
    {
        CL1 [] u1;
        CL1 [,] v1;

        u1 = new [] { y1, z1 };
        v1 = new [,] { {y1}, {z1} };
    }

    void Test3(CL1 y2, CL1? z2)
    {
        CL1 [] u2;
        CL1 [,] v2;

        var a2 = new [] { y2, z2 };
        var b2 = new [,] { {y2}, {z2} };

        u2 = a2;
        v2 = b2;
    }

    void Test8(CL1 y8, CL1? z8)
    {
        CL1 [] x8 = new [] { y8, z8 };
    }

    void Test9(CL1 y9, CL1? z9)
    {
        CL1 [,] x9 = new [,] { {y9}, {z9} };
    }

    void Test11(CL1 y11, CL1? z11)
    {
        CL1? [] u11;
        CL1? [,] v11;

        u11 = new [] { y11, z11 };
        v11 = new [,] { {y11}, {z11} };
    }

    void Test13(CL1 y12, CL1? z12)
    {
        CL1? [] u12;
        CL1? [,] v12;

        var a12 = new [] { y12, z12 };
        var b12 = new [,] { {y12}, {z12} };

        u12 = a12;
        v12 = b12;
    }

    void Test18(CL1 y18, CL1? z18)
    {
        CL1? [] x18 = new [] { y18, z18 };
    }

    void Test19(CL1 y19, CL1? z19)
    {
        CL1? [,] x19 = new [,] { {y19}, {z19} };
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (13,14): warning CS8619: Nullability of reference types in value of type 'CL1?[]' doesn't match target type 'CL1[]'.
                 //         u1 = new [] { y1, z1 };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new [] { y1, z1 }").WithArguments("CL1?[]", "CL1[]").WithLocation(13, 14),
                 // (14,14): warning CS8619: Nullability of reference types in value of type 'CL1?[*,*]' doesn't match target type 'CL1[*,*]'.
                 //         v1 = new [,] { {y1}, {z1} };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new [,] { {y1}, {z1} }").WithArguments("CL1?[*,*]", "CL1[*,*]").WithLocation(14, 14),
                 // (25,14): warning CS8619: Nullability of reference types in value of type 'CL1?[]' doesn't match target type 'CL1[]'.
                 //         u2 = a2;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "a2").WithArguments("CL1?[]", "CL1[]").WithLocation(25, 14),
                 // (26,14): warning CS8619: Nullability of reference types in value of type 'CL1?[*,*]' doesn't match target type 'CL1[*,*]'.
                 //         v2 = b2;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b2").WithArguments("CL1?[*,*]", "CL1[*,*]").WithLocation(26, 14),
                 // (31,21): warning CS8619: Nullability of reference types in value of type 'CL1?[]' doesn't match target type 'CL1[]'.
                 //         CL1 [] x8 = new [] { y8, z8 };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new [] { y8, z8 }").WithArguments("CL1?[]", "CL1[]").WithLocation(31, 21),
                 // (36,22): warning CS8619: Nullability of reference types in value of type 'CL1?[*,*]' doesn't match target type 'CL1[*,*]'.
                 //         CL1 [,] x9 = new [,] { {y9}, {z9} };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new [,] { {y9}, {z9} }").WithArguments("CL1?[*,*]", "CL1[*,*]").WithLocation(36, 22)
                );
        }

        [Fact]
        public void Array_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        int[]? u1 = new [] { 1, 2 };
        var z1 = u1.Length;
    }

    void Test2()
    {
        int[]? u2 = new [] { 1, 2 };
        u2 = null;
        var z2 = u2.Length;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (18,18): warning CS8602: Possible dereference of a null reference.
                //         var z2 = u2.Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u2").WithLocation(18, 18)
                );
        }

        [Fact]
        public void Array_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    object Test1()
    {
        object []? u1 = null;
        return u1;
    }
    object Test2()
    {
        object [][]? u2 = null;
        return u2;
    }
    object Test3()
    {
        object []?[]? u3 = null;
        return u3;
    }
}
", parseOptions: TestOptions.Regular7);

            c.VerifyDiagnostics(
                // (10,18): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         object []? u1 = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "?").WithArguments("static null checking", "8.0").WithLocation(10, 18),
                // (15,20): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         object [][]? u2 = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "?").WithArguments("static null checking", "8.0").WithLocation(15, 20),
                // (20,18): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         object []?[]? u3 = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "?").WithArguments("static null checking", "8.0").WithLocation(20, 18),
                // (20,21): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         object []?[]? u3 = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "?").WithArguments("static null checking", "8.0").WithLocation(20, 21)
                );
        }

        [Fact]
        public void Array_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        object? [] u1 = new [] { null, new object() };
        u1 = null;
    }

    void Test2()
    {
        object [] u2 = new [] { null, new object() };
    }

    void Test3()
    {
        var u3 = new object [] { null, new object() };
    }

    object? Test4()
    {
        object []? u4 = null;
        return u4;
    }

    object Test5()
    {
        object? [] u5 = null;
        return u5;
    }

    void Test6()
    {
        object [][,]? u6 = null;
        u6[0] = null;
        u6[0][0,0] = null;
        u6[0][0,0].ToString();
    }

    void Test7()
    {
        object [][,] u7 = null;
        u7[0] = null;
        u7[0][0,0] = null;
    }

    void Test8()
    {
        object []?[,] u8 = null;
        u8[0] = null;
        u8[0][0,0] = null;
        u8[0][0,0].ToString();
    }

    void Test9()
    {
        object []?[,]? u9 = null;
        u9[0] = null;
        u9[0][0,0] = null;
        u9[0][0,0].ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (11,14): warning CS8600: Cannot convert null to non-nullable reference.
                //         u1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 14),
                // (16,24): warning CS8619: Nullability of reference types in value of type 'object?[]' doesn't match target type 'object[]'.
                //         object [] u2 = new [] { null, new object() };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new [] { null, new object() }").WithArguments("object?[]", "object[]").WithLocation(16, 24),
                // (21,34): warning CS8600: Cannot convert null to non-nullable reference.
                //         var u3 = new object [] { null, new object() };
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(21, 34),
                // (32,25): warning CS8600: Cannot convert null to non-nullable reference.
                //         object? [] u5 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(32, 25),
                // (38,28): warning CS8600: Cannot convert null to non-nullable reference.
                //         object [][,]? u6 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(38, 28),
                // (40,9): warning CS8602: Possible dereference of a null reference.
                //         u6[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u6[0]").WithLocation(40, 9),
                // (40,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u6[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(40, 22),
                // (41,9): warning CS8602: Possible dereference of a null reference.
                //         u6[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u6[0]").WithLocation(41, 9),
                // (46,27): warning CS8600: Cannot convert null to non-nullable reference.
                //         object [][,] u7 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(46, 27),
                // (47,17): warning CS8600: Cannot convert null to non-nullable reference.
                //         u7[0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(47, 17),
                // (48,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u7[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(48, 22),
                // (54,9): warning CS8602: Possible dereference of a null reference.
                //         u8[0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u8").WithLocation(54, 9),
                // (54,17): warning CS8600: Cannot convert null to non-nullable reference.
                //         u8[0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(54, 17),
                // (55,9): warning CS8602: Possible dereference of a null reference.
                //         u8[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u8").WithLocation(55, 9),
                // (55,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u8[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(55, 22),
                // (56,9): warning CS8602: Possible dereference of a null reference.
                //         u8[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u8").WithLocation(56, 9),
                // (62,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9").WithLocation(62, 9),
                // (63,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9").WithLocation(63, 9),
                // (63,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9[0]").WithLocation(63, 9),
                // (63,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u9[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(63, 22),
                // (64,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9").WithLocation(64, 9),
                // (64,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9[0]").WithLocation(64, 9)
                );
        }

        [Fact]
        public void Array_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test3()
    {
        var u3 = new object? [] { null };
    }

    void Test6()
    {
        var u6 = new object [][,]? {null, 
                                    new object[,]? {{null}}};
        u6[0] = null;
        u6[0][0,0] = null;
        u6[0][0,0].ToString();
    }

    void Test7()
    {
        var u7 = new object [][,] {null, 
                                   new object[,] {{null}}};
        u7[0] = null;
        u7[0][0,0] = null;
    }

    void Test8()
    {
        var u8 = new object []?[,] {null, 
                                    new object[,] {{null}}};
        u8[0] = null;
        u8[0][0,0] = null;
        u8[0][0,0].ToString();
    }

    void Test9()
    {
        var u9 = new object []?[,]? {null, 
                                     new object[,]? {{null}}};
        u9[0] = null;
        u9[0][0,0] = null;
        u9[0][0,0].ToString();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (16,54): warning CS8600: Cannot convert null to non-nullable reference.
                //                                     new object[,]? {{null}}};
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(16, 54),
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         u6[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u6[0]").WithLocation(18, 9),
                // (18,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u6[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(18, 22),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         u6[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u6[0]").WithLocation(19, 9),
                // (24,36): warning CS8600: Cannot convert null to non-nullable reference.
                //         var u7 = new object [][,] {null, 
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(24, 36),
                // (25,52): warning CS8600: Cannot convert null to non-nullable reference.
                //                                    new object[,] {{null}}};
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(25, 52),
                // (26,17): warning CS8600: Cannot convert null to non-nullable reference.
                //         u7[0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(26, 17),
                // (27,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u7[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(27, 22),
                // (32,37): warning CS8600: Cannot convert null to non-nullable reference.
                //         var u8 = new object []?[,] {null, 
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(32, 37),
                // (33,53): warning CS8600: Cannot convert null to non-nullable reference.
                //                                     new object[,] {{null}}};
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(33, 53),
                // (34,17): warning CS8600: Cannot convert null to non-nullable reference.
                //         u8[0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(34, 17),
                // (35,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u8[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(35, 22),
                // (42,55): warning CS8600: Cannot convert null to non-nullable reference.
                //                                      new object[,]? {{null}}};
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(42, 55),
                // (44,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9[0]").WithLocation(44, 9),
                // (44,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         u9[0][0,0] = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(44, 22),
                // (45,9): warning CS8602: Possible dereference of a null reference.
                //         u9[0][0,0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u9[0]").WithLocation(45, 9)
                );
        }

        [Fact]
        public void Array_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0<string?> x1, CL0<string> y1)
    {
        var u1 = new [] { x1, y1 };
        var a1 = new [] { y1, x1 };
        var v1 = new CL0<string?>[] { x1, y1 };
        var w1 = new CL0<string>[] { x1, y1 };
    }
}

class CL0<T>
{}

", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,31): warning CS8619: Nullability of reference types in value of type 'CL0<string>' doesn't match target type 'CL0<string?>'.
                //         var u1 = new [] { x1, y1 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y1").WithArguments("CL0<string>", "CL0<string?>").WithLocation(10, 31),
                // (11,31): warning CS8619: Nullability of reference types in value of type 'CL0<string?>' doesn't match target type 'CL0<string>'.
                //         var a1 = new [] { y1, x1 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("CL0<string?>", "CL0<string>").WithLocation(11, 31),
                // (12,43): warning CS8619: Nullability of reference types in value of type 'CL0<string>' doesn't match target type 'CL0<string?>'.
                //         var v1 = new CL0<string?>[] { x1, y1 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y1").WithArguments("CL0<string>", "CL0<string?>").WithLocation(12, 43),
                // (13,38): warning CS8619: Nullability of reference types in value of type 'CL0<string?>' doesn't match target type 'CL0<string>'.
                //         var w1 = new CL0<string>[] { x1, y1 };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("CL0<string?>", "CL0<string>").WithLocation(13, 38)
                );
        }

        [Fact]
        public void ObjectInitializer_01()
        {
            CSharpCompilation c = CreateStandardCompilation(
@"#pragma warning disable 8618
class C
{
    static void Main()
    {}

    void Test1(CL1? x1, CL1? y1)
    {
        var z1 = new CL1() { F1 = x1, F2 = y1 };
    }

    void Test2(CL1? x2, CL1? y2)
    {
        var z2 = new CL1() { P1 = x2, P2 = y2 };
    }

    void Test3(CL1 x3, CL1 y3)
    {
        var z31 = new CL1() { F1 = x3, F2 = y3 };
        var z32 = new CL1() { P1 = x3, P2 = y3 };
    }
}

class CL1
{
    public CL1 F1;
    public CL1? F2;

    public CL1 P1 {get; set;}
    public CL1? P2 {get; set;}
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,35): warning CS8601: Possible null reference assignment.
                 //         var z1 = new CL1() { F1 = x1, F2 = y1 };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(9, 35),
                 // (14,35): warning CS8601: Possible null reference assignment.
                 //         var z2 = new CL1() { P1 = x2, P2 = y2 };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(14, 35)
                );
        }

        [Fact]
        public void Structs_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        S1 y1 = new S1();
        y1.F1 = x1;
        y1 = new S1();
        x1 = y1.F1;
    }

    void M1(ref S1 x) {}

    void Test2(CL1 x2)
    {
        S1 y2 = new S1();
        y2.F1 = x2;
        M1(ref y2);
        x2 = y2.F1;
    }

    void Test3(CL1 x3)
    {
        S1 y3 = new S1() { F1 = x3 };
        x3 = y3.F1;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        var y4 = new S2() { F2 = new S1() { F1 = x4, F3 = z4 } };
        x4 = y4.F2.F1 ?? x4;
        x4 = y4.F2.F3;
    }

    void Test5(CL1 x5, CL1? z5)
    {
        var y5 = new S2() { F2 = new S1() { F1 = x5, F3 = z5 } };
        var u5 = y5.F2;
        x5 = u5.F1 ?? x5;
        x5 = u5.F3;
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
    public CL1? F3;
}

struct S2
{
    public S1 F2;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,14): warning CS8601: Possible null reference assignment.
                 //         x1 = y1.F1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1.F1").WithLocation(12, 14),
                 // (22,14): warning CS8601: Possible null reference assignment.
                 //         x2 = y2.F1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2.F1").WithLocation(22, 14),
                 // (34,14): hidden CS8607: Expression is probably never null.
                 //         x4 = y4.F2.F1 ?? x4;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y4.F2.F1").WithLocation(34, 14),
                 // (35,14): warning CS8601: Possible null reference assignment.
                 //         x4 = y4.F2.F3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4.F2.F3").WithLocation(35, 14),
                 // (42,14): hidden CS8607: Expression is probably never null.
                 //         x5 = u5.F1 ?? x5;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u5.F1").WithLocation(42, 14),
                 // (43,14): warning CS8601: Possible null reference assignment.
                 //         x5 = u5.F3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u5.F3").WithLocation(43, 14)
                );
        }

        [Fact]
        public void Structs_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        S1 y1;
        y1.F1 = x1;
        S1 z1 = y1;
        x1 = z1.F3;
        x1 = z1.F3 ?? x1;
        z1.F3 = null;
    }

    struct Test2
    {
        S1 z2 {get;}

        public Test2(CL1 x2)
        {
            S1 y2;
            y2.F1 = x2;
            z2 = y2;
            x2 = z2.F3;
            x2 = z2.F3 ?? x2;
        }
    }

    void Test3(CL1 x3)
    {
        S1 y3;
        CL1? z3 = y3.F3;
        x3 = z3;
        x3 = z3 ?? x3;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        S1 y4;
        z4 = y4.F3;
        x4 = z4;
        x4 = z4 ?? x4;
    }

    void Test5(CL1 x5)
    {
        S1 y5;
        var z5 = new { F3 = y5.F3 };
        x5 = z5.F3;
        x5 = z5.F3 ?? x5;
    }

    void Test6(CL1 x6, S1 z6)
    {
        S1 y6;
        y6.F1 = x6;
        z6 = y6;
        x6 = z6.F3;
        x6 = z6.F3 ?? x6;
    }

    void Test7(CL1 x7)
    {
        S1 y7;
        y7.F1 = x7;
        var z7 = new { F3 = y7 };
        x7 = z7.F3.F3;
        x7 = z7.F3.F3 ?? x7;
    }

    struct Test8
    {
        CL1? z8 {get;}

        public Test8(CL1 x8)
        {
            S1 y8;
            y8.F1 = x8;
            z8 = y8.F3;
            x8 = z8;
            x8 = z8 ?? x8;
        }
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
    public CL1? F3;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (11,17): error CS0165: Use of unassigned local variable 'y1'
                //         S1 z1 = y1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(11, 17),
                // (25,18): error CS0165: Use of unassigned local variable 'y2'
                //             z2 = y2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(25, 18),
                // (34,19): error CS0170: Use of possibly unassigned field 'F3'
                //         CL1? z3 = y3.F3;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y3.F3").WithArguments("F3").WithLocation(34, 19),
                // (42,14): error CS0170: Use of possibly unassigned field 'F3'
                //         z4 = y4.F3;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y4.F3").WithArguments("F3").WithLocation(42, 14),
                // (50,29): error CS0170: Use of possibly unassigned field 'F3'
                //         var z5 = new { F3 = y5.F3 };
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y5.F3").WithArguments("F3").WithLocation(50, 29),
                // (59,14): error CS0165: Use of unassigned local variable 'y6'
                //         z6 = y6;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y6").WithArguments("y6").WithLocation(59, 14),
                // (68,29): error CS0165: Use of unassigned local variable 'y7'
                //         var z7 = new { F3 = y7 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y7").WithArguments("y7").WithLocation(68, 29),
                // (81,18): error CS0170: Use of possibly unassigned field 'F3'
                //             z8 = y8.F3;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "y8.F3").WithArguments("F3").WithLocation(81, 18)
                );
        }

        [Fact]
        public void Structs_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        x1 = new S1().F1;
    }

    void Test2(CL1 x2)
    {
        x2 = new S1() {F1 = x2}.F1;
    }

    void Test3(CL1 x3)
    {
        x3 = new S1() {F1 = x3}.F1 ?? x3;
    }

    void Test4(CL1 x4)
    {
        x4 = new S2().F2;
    }

    void Test5(CL1 x5)
    {
        x5 = new S2().F2 ?? x5;
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
}

struct S2
{
    public CL1 F2;

    S2(CL1 x) { F2 = x; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,14): warning CS8601: Possible null reference assignment.
                 //         x1 = new S1().F1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "new S1().F1").WithLocation(9, 14),
                 // (19,14): hidden CS8607: Expression is probably never null.
                 //         x3 = new S1() {F1 = x3}.F1 ?? x3;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new S1() {F1 = x3}.F1").WithLocation(19, 14),
                 // (29,14): hidden CS8607: Expression is probably never null.
                 //         x5 = new S2().F2 ?? x5;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new S2().F2").WithLocation(29, 14)
                );
        }

        [Fact]
        public void Structs_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}
}

struct TS2
{
    System.Action? E2;

    TS2(System.Action x2)
    {
        this = new TS2();
        System.Action z2 = E2;
        System.Action y2 = E2 ?? x2;
    }

    void Dummy()
    {
        E2 = null;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,28): warning CS8601: Possible null reference assignment.
                 //         System.Action z2 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(15, 28)
                );
        }

        [Fact]
        public void AnonymousTypes_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1, CL1? z1)
    {
        var y1 = new { p1 = x1, p2 = z1 };
        x1 = y1.p1 ?? x1;
        x1 = y1.p2;
    }

    void Test2(CL1 x2, CL1? z2)
    {
        var u2 = new { p1 = x2, p2 = z2 };
        var v2 = new { p1 = z2, p2 = x2 };
        u2 = v2;
        x2 = u2.p2 ?? x2;
        x2 = u2.p1;
        x2 = v2.p2 ?? x2;
        x2 = v2.p1;
    }

    void Test3(CL1 x3, CL1? z3)
    {
        var u3 = new { p1 = x3, p2 = z3 };
        var v3 = u3;
        x3 = v3.p1 ?? x3;
        x3 = v3.p2;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        var u4 = new { p0 = new { p1 = x4, p2 = z4 } };
        var v4 = new { p0 = new { p1 = z4, p2 = x4 } };
        u4 = v4;
        x4 = u4.p0.p2 ?? x4;
        x4 = u4.p0.p1;
        x4 = v4.p0.p2 ?? x4;
        x4 = v4.p0.p1;
    }

    void Test5(CL1 x5, CL1? z5)
    {
        var u5 = new { p0 = new { p1 = x5, p2 = z5 } };
        var v5 = u5;
        x5 = v5.p0.p1 ?? x5;
        x5 = v5.p0.p2;
    }

    void Test6(CL1 x6, CL1? z6)
    {
        var u6 = new { p0 = new { p1 = x6, p2 = z6 } };
        var v6 = u6.p0;
        x6 = v6.p1 ?? x6;
        x6 = v6.p2;
    }

    void Test7(CL1 x7, CL1? z7)
    {
        var u7 = new { p0 = new S1() { p1 = x7, p2 = z7 } };
        var v7 = new { p0 = new S1() { p1 = z7, p2 = x7 } };
        u7 = v7;
        x7 = u7.p0.p2 ?? x7;
        x7 = u7.p0.p1;
        x7 = v7.p0.p2 ?? x7;
        x7 = v7.p0.p1;
    }

    void Test8(CL1 x8, CL1? z8)
    {
        var u8 = new { p0 = new S1() { p1 = x8, p2 = z8 } };
        var v8 = u8;
        x8 = v8.p0.p1 ?? x8;
        x8 = v8.p0.p2;
    }

    void Test9(CL1 x9, CL1? z9)
    {
        var u9 = new { p0 = new S1() { p1 = x9, p2 = z9 } };
        var v9 = u9.p0;
        x9 = v9.p1 ?? x9;
        x9 = v9.p2;
    }

    void M1<T>(ref T x) {}

    void Test10(CL1 x10)
    {
        var u10 = new { a0 = x10, a1 = new { p1 = x10 }, a2 = new S1() { p2 = x10 } };
        x10 = u10.a0; // 1
        x10 = u10.a1.p1; // 2
        x10 = u10.a2.p2; // 3 

        M1(ref u10);

        x10 = u10.a0; // 4
        x10 = u10.a1.p1; // 5
        x10 = u10.a2.p2; // 6 
    }
}

class CL1
{
}

struct S1
{
    public CL1? p1;
    public CL1? p2;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,14): hidden CS8607: Expression is probably never null.
                //         x1 = y1.p1 ?? x1;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "y1.p1").WithLocation(10, 14),
                // (11,14): warning CS8601: Possible null reference assignment.
                //         x1 = y1.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1.p2").WithLocation(11, 14),
                // (19,14): hidden CS8607: Expression is probably never null.
                //         x2 = u2.p2 ?? x2;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u2.p2").WithLocation(19, 14),
                // (20,14): warning CS8601: Possible null reference assignment.
                //         x2 = u2.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u2.p1").WithLocation(20, 14),
                // (21,14): hidden CS8607: Expression is probably never null.
                //         x2 = v2.p2 ?? x2;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v2.p2").WithLocation(21, 14),
                // (22,14): warning CS8601: Possible null reference assignment.
                //         x2 = v2.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v2.p1").WithLocation(22, 14),
                // (29,14): hidden CS8607: Expression is probably never null.
                //         x3 = v3.p1 ?? x3;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v3.p1").WithLocation(29, 14),
                // (30,14): warning CS8601: Possible null reference assignment.
                //         x3 = v3.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v3.p2").WithLocation(30, 14),
                // (38,14): hidden CS8607: Expression is probably never null.
                //         x4 = u4.p0.p2 ?? x4;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4.p0.p2").WithLocation(38, 14),
                // (39,14): warning CS8601: Possible null reference assignment.
                //         x4 = u4.p0.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u4.p0.p1").WithLocation(39, 14),
                // (40,14): hidden CS8607: Expression is probably never null.
                //         x4 = v4.p0.p2 ?? x4;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v4.p0.p2").WithLocation(40, 14),
                // (41,14): warning CS8601: Possible null reference assignment.
                //         x4 = v4.p0.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v4.p0.p1").WithLocation(41, 14),
                // (48,14): hidden CS8607: Expression is probably never null.
                //         x5 = v5.p0.p1 ?? x5;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v5.p0.p1").WithLocation(48, 14),
                // (49,14): warning CS8601: Possible null reference assignment.
                //         x5 = v5.p0.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v5.p0.p2").WithLocation(49, 14),
                // (56,14): hidden CS8607: Expression is probably never null.
                //         x6 = v6.p1 ?? x6;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v6.p1").WithLocation(56, 14),
                // (57,14): warning CS8601: Possible null reference assignment.
                //         x6 = v6.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v6.p2").WithLocation(57, 14),
                // (65,14): hidden CS8607: Expression is probably never null.
                //         x7 = u7.p0.p2 ?? x7;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u7.p0.p2").WithLocation(65, 14),
                // (66,14): warning CS8601: Possible null reference assignment.
                //         x7 = u7.p0.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u7.p0.p1").WithLocation(66, 14),
                // (67,14): hidden CS8607: Expression is probably never null.
                //         x7 = v7.p0.p2 ?? x7;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v7.p0.p2").WithLocation(67, 14),
                // (68,14): warning CS8601: Possible null reference assignment.
                //         x7 = v7.p0.p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v7.p0.p1").WithLocation(68, 14),
                // (75,14): hidden CS8607: Expression is probably never null.
                //         x8 = v8.p0.p1 ?? x8;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v8.p0.p1").WithLocation(75, 14),
                // (76,14): warning CS8601: Possible null reference assignment.
                //         x8 = v8.p0.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v8.p0.p2").WithLocation(76, 14),
                // (83,14): hidden CS8607: Expression is probably never null.
                //         x9 = v9.p1 ?? x9;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "v9.p1").WithLocation(83, 14),
                // (84,14): warning CS8601: Possible null reference assignment.
                //         x9 = v9.p2;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v9.p2").WithLocation(84, 14),
                // (98,15): warning CS8601: Possible null reference assignment.
                //         x10 = u10.a0; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a0").WithLocation(98, 15),
                // (99,15): warning CS8602: Possible dereference of a null reference.
                //         x10 = u10.a1.p1; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u10.a1").WithLocation(99, 15),
                // (99,15): warning CS8601: Possible null reference assignment.
                //         x10 = u10.a1.p1; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a1.p1").WithLocation(99, 15),
                // (100,15): warning CS8601: Possible null reference assignment.
                //         x10 = u10.a2.p2; // 6 
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a2.p2").WithLocation(100, 15)
                );
        }

        [Fact]
        public void AnonymousTypes_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1? x1)
    {
        var y1 = new { p1 = x1 };
        y1.p1?.
               M1(y1.p1);
    }

    void Test2(CL1? x2)
    {
        var y2 = new { p1 = x2 };
        if (y2.p1 != null)
        {
            y2.p1.M1(y2.p1);
        }
    }

    void Test3(out CL1? x3, CL1 z3)
    {
        var y3 = new { p1 = x3 };
        x3 = y3.p1 ?? 
                      z3.M1(y3.p1);
        CL1 v3 = y3.p1;
    }
}

class CL1
{
    public CL1? M1(CL1 x) { return null; }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (25,29): error CS0269: Use of unassigned out parameter 'x3'
                 //         var y3 = new { p1 = x3 };
                 Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x3").WithArguments("x3").WithLocation(25, 29)
                );
        }

        [Fact]
        public void AnonymousTypes_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test2(CL1 x2)
    {
        x2 = new {F1 = x2}.F1;
    }

    void Test3(CL1 x3)
    {
        x3 = new {F1 = x3}.F1 ?? x3;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x3 = new {F1 = x3}.F1 ?? x3;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new {F1 = x3}.F1").WithLocation(14, 14)
                );
        }

        [Fact]
        public void AnonymousTypes_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL1<string> x1, CL1<string?> y1)
    {
        var u1 = new { F1 = x1 };
        var v1 = new { F1 = y1 };

        u1 = v1;
        v1 = u1;
    }
}

class CL1<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,14): warning CS8619: Nullability of reference types in value of type '<anonymous type: CL1<string?> F1>' doesn't match target type '<anonymous type: CL1<string> F1>'.
                 //         u1 = v1;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "v1").WithArguments("<anonymous type: CL1<string?> F1>", "<anonymous type: CL1<string> F1>").WithLocation(12, 14),
                 // (13,14): warning CS8619: Nullability of reference types in value of type '<anonymous type: CL1<string> F1>' doesn't match target type '<anonymous type: CL1<string?> F1>'.
                 //         v1 = u1;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "u1").WithArguments("<anonymous type: CL1<string> F1>", "<anonymous type: CL1<string?> F1>").WithLocation(13, 14)
                );
        }

        [Fact]
        public void AnonymousTypes_05()
        {
            var source =
@"class C
{
    static void F(string x, string y)
    {
        x = new { x, y }.x ?? x;
        y = new { x, y = y }.y ?? y;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report ErrorCode.HDN_ExpressionIsProbablyNeverNull.
            // See comment in DataFlowPass.VisitAnonymousObjectCreationExpression.
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void This()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1()
    {
        this.Test2();
    }

    void Test2()
    {
        this?.Test1();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,9): hidden CS8607: Expression is probably never null.
                 //         this?.Test1();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "this").WithLocation(14, 9)
                );
        }

        [Fact]
        public void ReadonlyAutoProperties_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C1
{
    static void Main()
    {
    }

    C1 P1 {get;}

    public C1(C1? x1)
    {
        P1 = x1;
    }
}

class C2
{
    C2? P2 {get;}

    public C2(C2 x2)
    {
        x2 = P2;
    }
}

class C3
{
    C3? P3 {get;}

    public C3(C3 x3, C3? y3)
    {
        P3 = y3;
        x3 = P3;
    }
}

class C4
{
    C4? P4 {get;}

    public C4(C4 x4)
    {
        P4 = x4;
        x4 = P4;
    }
}

class C5
{
    S1 P5 {get;}

    public C5(C0 x5)
    {
        P5 = new S1() { F1 = x5 };
        x5 = P5.F1;
    }
}

class C0
{}

struct S1
{
    public C0? F1;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,14): warning CS8601: Possible null reference assignment.
                 //         P1 = x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 14),
                 // (33,14): warning CS8601: Possible null reference assignment.
                 //         x3 = P3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P3").WithLocation(33, 14),
                 // (22,14): warning CS8601: Possible null reference assignment.
                 //         x2 = P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P2").WithLocation(22, 14)
                );
        }

        [Fact]
        public void ReadonlyAutoProperties_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
struct C1
{
    static void Main()
    {
    }

    C0 P1 {get;}

    public C1(C0? x1)
    {
        P1 = x1;
    }
}

struct C2
{
    C0? P2 {get;}

    public C2(C0 x2)
    {
        x2 = P2;
        P2 = null;
    }
}

struct C3
{
    C0? P3 {get;}

    public C3(C0 x3, C0? y3)
    {
        P3 = y3;
        x3 = P3;
    }
}

struct C4
{
    C0? P4 {get;}

    public C4(C0 x4)
    {
        P4 = x4;
        x4 = P4;
    }
}

struct C5
{
    S1 P5 {get;}

    public C5(C0 x5)
    {
        P5 = new S1() { F1 = x5 };
        x5 = P5.F1 ?? x5;
    }
}

class C0
{}

struct S1
{
    public C0? F1;
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (34,14): warning CS8601: Possible null reference assignment.
                 //         x3 = P3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P3").WithLocation(34, 14),
                 // (12,14): warning CS8601: Possible null reference assignment.
                 //         P1 = x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 14),
                 // (22,14): error CS8079: Use of possibly unassigned auto-implemented property 'P2'
                 //         x2 = P2;
                 Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "P2").WithArguments("P2").WithLocation(22, 14),
                 // (56,14): hidden CS8607: Expression is probably never null.
                 //         x5 = P5.F1 ?? x5;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "P5.F1").WithLocation(56, 14)
                );
        }

        [Fact]
        public void NotAssigned()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(object? x1)
    {
        CL1? y1;

        if (x1 == null)
        {
            y1 = null;
            return;
        }

        CL1 z1 = y1;
    }

    void Test2(object? x2, out CL1? y2)
    {
        if (x2 == null)
        {
            y2 = null;
            return;
        }

        CL1 z2 = y2;
        y2 = null;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (17,18): error CS0165: Use of unassigned local variable 'y1'
                 //         CL1 z1 = y1;
                 Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(17, 18),
                 // (28,18): error CS0269: Use of unassigned out parameter 'y2'
                 //         CL1 z2 = y2;
                 Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y2").WithArguments("y2").WithLocation(28, 18)
                );
        }

        [Fact]
        public void Lambda_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Func<CL1?> x1 = () => M1();
    }

    void Test2()
    {
        System.Func<CL1?> x2 = delegate { return M1(); };
    }

    delegate CL1? D1();

    void Test3()
    {
        D1 x3 = () => M1();
    }

    void Test4()
    {
        D1 x4 = delegate { return M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Lambda_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1?> x1 = (p1) => p1 = M1();
    }

    delegate void D1(CL1? p);

    void Test3()
    {
        D1 x3 = (p3) => p3 = M1();
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Lambda_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Func<CL1> x1 = () => M1();
    }

    void Test2()
    {
        System.Func<CL1> x2 = delegate { return M1(); };
    }

    delegate CL1 D1();

    void Test3()
    {
        D1 x3 = () => M1();
    }

    void Test4()
    {
        D1 x4 = delegate { return M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,37): warning CS8603: Possible null reference return.
                 //         System.Func<CL1> x1 = () => M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(12, 37),
                 // (17,49): warning CS8603: Possible null reference return.
                 //         System.Func<CL1> x2 = delegate { return M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(17, 49),
                 // (24,23): warning CS8603: Possible null reference return.
                 //         D1 x3 = () => M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(24, 23),
                 // (29,35): warning CS8603: Possible null reference return.
                 //         D1 x4 = delegate { return M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(29, 35)
                );
        }

        [Fact]
        public void Lambda_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1> x1 = (p1) => p1 = M1();
    }

    delegate void D1(CL1 p);

    void Test3()
    {
        D1 x3 = (p3) => p3 = M1();
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,46): warning CS8601: Possible null reference assignment.
                 //         System.Action<CL1> x1 = (p1) => p1 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(12, 46),
                 // (19,30): warning CS8601: Possible null reference assignment.
                 //         D1 x3 = (p3) => p3 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(19, 30)
                );
        }

        [Fact]
        public void Lambda_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate CL1 D1();
    delegate CL1? D2();

    void M2(int x, D1 y) {}
    void M2(long x, D2 y) {}

    void M3(long x, D2 y) {}
    void M3(int x, D1 y) {}

    void Test1(int x1)
    {
        M2(x1, () => M1());
    }

    void Test2(int x2)
    {
        M3(x2, () => M1());
    }

    void Test3(int x3)
    {
        M2(x3, delegate { return M1(); });
    }

    void Test4(int x4)
    {
        M3(x4, delegate { return M1(); });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (20,22): warning CS8603: Possible null reference return.
                 //         M2(x1, () => M1());
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(20, 22),
                 // (25,22): warning CS8603: Possible null reference return.
                 //         M3(x2, () => M1());
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(25, 22),
                 // (30,34): warning CS8603: Possible null reference return.
                 //         M2(x3, delegate { return M1(); });
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(30, 34),
                 // (35,34): warning CS8603: Possible null reference return.
                 //         M3(x4, delegate { return M1(); });
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(35, 34)
                );
        }

        [Fact]
        public void Lambda_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate CL1 D1();
    delegate CL1? D2();

    void M2(int x, D2 y) {}
    void M2(long x, D1 y) {}

    void M3(long x, D1 y) {}
    void M3(int x, D2 y) {}

    void Test1(int x1)
    {
        M2(x1, () => M1());
    }

    void Test2(int x2)
    {
        M3(x2, () => M1());
    }

    void Test3(int x3)
    {
        M2(x3, delegate { return M1(); });
    }

    void Test4(int x4)
    {
        M3(x4, delegate { return M1(); });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Lambda_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate T D<T>();

    void M2(int x, D<CL1> y) {}
    void M2<T>(int x, D<T> y) {}

    void M3<T>(int x, D<T> y) {}
    void M3(int x, D<CL1> y) {}

    void Test1(int x1)
    {
        M2(x1, () => M1());
    }

    void Test2(int x2)
    {
        M3(x2, () => M1());
    }

    void Test3(int x3)
    {
        M2(x3, delegate { return M1(); });
    }

    void Test4(int x4)
    {
        M3(x4, delegate { return M1(); });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (19,22): warning CS8603: Possible null reference return.
                 //         M2(x1, () => M1());
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(19, 22),
                 // (24,22): warning CS8603: Possible null reference return.
                 //         M3(x2, () => M1());
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(24, 22),
                 // (29,34): warning CS8603: Possible null reference return.
                 //         M2(x3, delegate { return M1(); });
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(29, 34),
                 // (34,34): warning CS8603: Possible null reference return.
                 //         M3(x4, delegate { return M1(); });
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "M1()").WithLocation(34, 34)
                );
        }

        [Fact]
        public void Lambda_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate T D<T>();

    void M2(int x, D<CL1?> y) {}
    void M2<T>(int x, D<T> y) {}

    void M3<T>(int x, D<T> y) {}
    void M3(int x, D<CL1?> y) {}

    void Test1(int x1)
    {
        M2(x1, () => M1());
    }

    void Test2(int x2)
    {
        M3(x2, () => M1());
    }

    void Test3(int x3)
    {
        M2(x3, delegate { return M1(); });
    }

    void Test4(int x4)
    {
        M3(x4, delegate { return M1(); });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Lambda_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate T1 D<T1, T2>(T2 y);

    void M2(int x, D<CL1, CL1> y) {}
    void M2<T>(int x, D<T, CL1> y) {}

    void M3<T>(int x, D<T, CL1> y) {}
    void M3(int x, D<CL1, CL1> y) {}

    void Test1(int x1)
    {
        M2(x1, (y1) => 
                {
                    y1 = M1();
                    return y1;
                });
    }

    void Test2(int x2)
    {
        M3(x2, (y2) => 
                {
                    y2 = M1();
                    return y2;
                });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (21,26): warning CS8601: Possible null reference assignment.
                 //                     y1 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(21, 26),
                 // (30,26): warning CS8601: Possible null reference assignment.
                 //                     y2 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(30, 26)
                );
        }

        [Fact]
        public void Lambda_10()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }
    delegate T1 D<T1, T2>(T2 y);

    void M2(int x, D<CL1, CL1?> y) {}
    void M2<T>(int x, D<T, CL1> y) {}

    void M3<T>(int x, D<T, CL1> y) {}
    void M3(int x, D<CL1, CL1?> y) {}

    void Test1(int x1)
    {
        M2(x1, (y1) => 
                {
                    y1 = M1();
                    return y1;
                });
    }

    void Test2(int x2)
    {
        M3(x2, (y2) => 
                {
                    y2 = M1();
                    return y2;
                });
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (22,28): warning CS8603: Possible null reference return.
                 //                     return y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y1").WithLocation(22, 28),
                 // (31,28): warning CS8603: Possible null reference return.
                 //                     return y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y2").WithLocation(31, 28)
                );
        }

        [Fact]
        public void Lambda_11()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1> x1 = (CL1 p1) => p1 = M1();
    }

    void Test2()
    {
        System.Action<CL1> x2 = delegate (CL1 p2) { p2 = M1(); };
    }

    delegate void D1(CL1 p);

    void Test3()
    {
        D1 x3 = (CL1 p3) => p3 = M1();
    }

    void Test4()
    {
        D1 x4 = delegate (CL1 p4) { p4 = M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,50): warning CS8601: Possible null reference assignment.
                 //         System.Action<CL1> x1 = (CL1 p1) => p1 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(12, 50),
                 // (17,58): warning CS8601: Possible null reference assignment.
                 //         System.Action<CL1> x2 = delegate (CL1 p2) { p2 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(17, 58),
                 // (24,34): warning CS8601: Possible null reference assignment.
                 //         D1 x3 = (CL1 p3) => p3 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(24, 34),
                 // (29,42): warning CS8601: Possible null reference assignment.
                 //         D1 x4 = delegate (CL1 p4) { p4 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(29, 42)
                );
        }

        [Fact]
        public void Lambda_12()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1?> x1 = (CL1 p1) => p1 = M1();
    }

    void Test2()
    {
        System.Action<CL1?> x2 = delegate (CL1 p2) { p2 = M1(); };
    }

    delegate void D1(CL1? p);

    void Test3()
    {
        D1 x3 = (CL1 p3) => p3 = M1();
    }

    void Test4()
    {
        D1 x4 = delegate (CL1 p4) { p4 = M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,51): warning CS8601: Possible null reference assignment.
                 //         System.Action<CL1?> x1 = (CL1 p1) => p1 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(12, 51),
                 // (12,34): warning CS8622: Nullability of reference types in type of parameter 'p1' of 'lambda expression' doesn't match the target delegate 'Action<CL1?>'.
                 //         System.Action<CL1?> x1 = (CL1 p1) => p1 = M1();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1 p1) => p1 = M1()").WithArguments("p1", "lambda expression", "System.Action<CL1?>").WithLocation(12, 34),
                 // (17,59): warning CS8601: Possible null reference assignment.
                 //         System.Action<CL1?> x2 = delegate (CL1 p2) { p2 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(17, 59),
                 // (17,34): warning CS8622: Nullability of reference types in type of parameter 'p2' of 'lambda expression' doesn't match the target delegate 'Action<CL1?>'.
                 //         System.Action<CL1?> x2 = delegate (CL1 p2) { p2 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "delegate (CL1 p2) { p2 = M1(); }").WithArguments("p2", "lambda expression", "System.Action<CL1?>").WithLocation(17, 34),
                 // (24,34): warning CS8601: Possible null reference assignment.
                 //         D1 x3 = (CL1 p3) => p3 = M1();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(24, 34),
                 // (24,17): warning CS8622: Nullability of reference types in type of parameter 'p3' of 'lambda expression' doesn't match the target delegate 'C.D1'.
                 //         D1 x3 = (CL1 p3) => p3 = M1();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1 p3) => p3 = M1()").WithArguments("p3", "lambda expression", "C.D1").WithLocation(24, 17),
                 // (29,42): warning CS8601: Possible null reference assignment.
                 //         D1 x4 = delegate (CL1 p4) { p4 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "M1()").WithLocation(29, 42),
                 // (29,17): warning CS8622: Nullability of reference types in type of parameter 'p4' of 'lambda expression' doesn't match the target delegate 'C.D1'.
                 //         D1 x4 = delegate (CL1 p4) { p4 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "delegate (CL1 p4) { p4 = M1(); }").WithArguments("p4", "lambda expression", "C.D1").WithLocation(29, 17)
                );
        }

        [Fact]
        public void Lambda_13()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1> x1 = (CL1? p1) => p1 = M1();
    }

    void Test2()
    {
        System.Action<CL1> x2 = delegate (CL1? p2) { p2 = M1(); };
    }

    delegate void D1(CL1 p);

    void Test3()
    {
        D1 x3 = (CL1? p3) => p3 = M1();
    }

    void Test4()
    {
        D1 x4 = delegate (CL1? p4) { p4 = M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,33): warning CS8622: Nullability of reference types in type of parameter 'p1' of 'lambda expression' doesn't match the target delegate 'Action<CL1>'.
                 //         System.Action<CL1> x1 = (CL1? p1) => p1 = M1();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1? p1) => p1 = M1()").WithArguments("p1", "lambda expression", "System.Action<CL1>").WithLocation(12, 33),
                 // (17,33): warning CS8622: Nullability of reference types in type of parameter 'p2' of 'lambda expression' doesn't match the target delegate 'Action<CL1>'.
                 //         System.Action<CL1> x2 = delegate (CL1? p2) { p2 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "delegate (CL1? p2) { p2 = M1(); }").WithArguments("p2", "lambda expression", "System.Action<CL1>").WithLocation(17, 33),
                 // (24,17): warning CS8622: Nullability of reference types in type of parameter 'p3' of 'lambda expression' doesn't match the target delegate 'C.D1'.
                 //         D1 x3 = (CL1? p3) => p3 = M1();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1? p3) => p3 = M1()").WithArguments("p3", "lambda expression", "C.D1").WithLocation(24, 17),
                 // (29,17): warning CS8622: Nullability of reference types in type of parameter 'p4' of 'lambda expression' doesn't match the target delegate 'C.D1'.
                 //         D1 x4 = delegate (CL1? p4) { p4 = M1(); };
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "delegate (CL1? p4) { p4 = M1(); }").WithArguments("p4", "lambda expression", "C.D1").WithLocation(29, 17)
                );
        }

        [Fact]
        public void Lambda_14()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL1? M1() { return null; }

    void Test1()
    {
        System.Action<CL1?> x1 = (CL1? p1) => p1 = M1();
    }

    void Test2()
    {
        System.Action<CL1?> x2 = delegate (CL1? p2) { p2 = M1(); };
    }

    delegate void D1(CL1? p);

    void Test3()
    {
        D1 x3 = (CL1? p3) => p3 = M1();
    }

    void Test4()
    {
        D1 x4 = delegate (CL1? p4) { p4 = M1(); };
    }
}

class CL1
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Lambda_15()
        {
            CSharpCompilation notAnnotated = CreateStandardCompilation(@"
public class CL0 
{
    public static void M1(System.Func<CL1<CL0>, CL0> x) {}
}

public class CL1<T>
{
    public T F1;

    public CL1()
    {
        F1 = default(T);
    }
}
", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CSharpCompilation c = CreateStandardCompilation(@"
class C 
{
    static void Main() {}

    static void Test1()
    {
        CL0.M1( p1 =>
                {
                    p1.F1 = null;
                    p1 = null;
                    return null; // 1
                });
    }

    static void Test2()
    {
        System.Func<CL1<CL0>, CL0> l2 = p2 =>
                {
                    p2.F1 = null;
                    p2 = null;
                    return null; // 2
                };
    }
}
", parseOptions: TestOptions.Regular8, references: new[] { notAnnotated.EmitToImageReference() });

            c.VerifyDiagnostics(
                // (10,29): warning CS8600: Cannot convert null to non-nullable reference.
                //                     p1.F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 29),
                // (11,26): warning CS8600: Cannot convert null to non-nullable reference.
                //                     p1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 26),
                // (12,28): warning CS8600: Cannot convert null to non-nullable reference.
                //                     return null; // 1
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 28),
                // (20,29): warning CS8600: Cannot convert null to non-nullable reference.
                //                     p2.F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(20, 29),
                // (21,26): warning CS8600: Cannot convert null to non-nullable reference.
                //                     p2 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(21, 26),
                // (22,28): warning CS8600: Cannot convert null to non-nullable reference.
                //                     return null; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(22, 28)
                );
        }

        [Fact]
        public void Lambda_16()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        System.Action<CL1<string?>> x1 = (CL1<string> p1) => System.Console.WriteLine();
    }

    void Test2()
    {
        System.Action<CL1<string>> x2 = (CL1<string?> p2) => System.Console.WriteLine();
    }
}

class CL1<T>
{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,42): warning CS8622: Nullability of reference types in type of parameter 'p1' of 'lambda expression' doesn't match the target delegate 'Action<CL1<string?>>'.
                 //         System.Action<CL1<string?>> x1 = (CL1<string> p1) => System.Console.WriteLine();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1<string> p1) => System.Console.WriteLine()").WithArguments("p1", "lambda expression", "System.Action<CL1<string?>>").WithLocation(10, 42),
                 // (15,41): warning CS8622: Nullability of reference types in type of parameter 'p2' of 'lambda expression' doesn't match the target delegate 'Action<CL1<string>>'.
                 //         System.Action<CL1<string>> x2 = (CL1<string?> p2) => System.Console.WriteLine();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1<string?> p2) => System.Console.WriteLine()").WithArguments("p2", "lambda expression", "System.Action<CL1<string>>").WithLocation(15, 41)
                );
        }

        [Fact]
        public void Lambda_17()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
using System.Linq.Expressions;

class C
{
    static void Main()
    {
    }

    void Test1()
    {
        Expression<System.Action<CL1<string?>>> x1 = (CL1<string> p1) => System.Console.WriteLine();
    }

    void Test2()
    {
        Expression<System.Action<CL1<string>>> x2 = (CL1<string?> p2) => System.Console.WriteLine();
    }
}

class CL1<T>
{}
", new[] { SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,54): warning CS8622: Nullability of reference types in type of parameter 'p1' of 'lambda expression' doesn't match the target delegate 'Action<CL1<string?>>'.
                 //         Expression<System.Action<CL1<string?>>> x1 = (CL1<string> p1) => System.Console.WriteLine();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1<string> p1) => System.Console.WriteLine()").WithArguments("p1", "lambda expression", "System.Action<CL1<string?>>").WithLocation(12, 54),
                 // (17,53): warning CS8622: Nullability of reference types in type of parameter 'p2' of 'lambda expression' doesn't match the target delegate 'Action<CL1<string>>'.
                 //         Expression<System.Action<CL1<string>>> x2 = (CL1<string?> p2) => System.Console.WriteLine();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(CL1<string?> p2) => System.Console.WriteLine()").WithArguments("p2", "lambda expression", "System.Action<CL1<string>>").WithLocation(17, 53)
                );
        }

        [Fact]
        public void NewT_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1<T1>(T1 x1) where T1 : class, new()
    {
        x1 = new T1();
    }

    void Test2<T2>(T2 x2) where T2 : class, new()
    {
        x2 = new T2() ?? x2;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = new T2() ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new T2()").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicObjectCreation_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = new CL0((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        x2 = new CL0((dynamic)0) ?? x2;
    }
}

class CL0
{
    public CL0(int x) {}
    public CL0(long x) {}
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = new CL0((dynamic)0) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new CL0((dynamic)0)").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        x2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public CL0 this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0 this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2[(dynamic)0] ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2[(dynamic)0]").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public CL0? this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0 this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1[(dynamic)0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1[(dynamic)0]").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public CL0 this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0? this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1[(dynamic)0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1[(dynamic)0]").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public CL0? this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0? this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1[(dynamic)0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1[(dynamic)0]").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        x2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public int this[int x]
    {
        get { return x; }
        set { }
    }

    public int this[long x]
    {
        get { return (int)x; }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2[(dynamic)0] ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2[(dynamic)0]").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1[(dynamic)0];
    }

    void Test2(CL0 x2)
    {
        x2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0
{
    public int this[int x]
    {
        get { return x; }
        set { }
    }

    public long this[long x]
    {
        get { return x; }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2[(dynamic)0] ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2[(dynamic)0]").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(dynamic x1)
    {
        x1 = x1[0];
    }

    void Test2(dynamic x2)
    {
        x2 = x2[0] ?? x2;
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void DynamicIndexerAccess_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1<T>(CL0<T> x1)
    {
        x1 = x1[(dynamic)0];
    }

    void Test2<T>(CL0<T> x2)
    {
        x2 = x2[(dynamic)0] ?? x2;
    }
}

class CL0<T>
{
    public T this[int x]
    {
        get { return default(T); }
        set { }
    }

    public long this[long x]
    {
        get { return x; }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void DynamicIndexerAccess_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1, dynamic y1)
    {
        x1[(dynamic)0] = y1;
    }

    void Test2(CL0 x2, dynamic? y2, CL1 z2)
    {
        x2[(dynamic)0] = y2;
        z2[0] = y2;
    }
}

class CL0
{
    public CL0 this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0 this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}

class CL1
{
    public dynamic this[int x]
    {
        get { return new CL0(); }
        set { }
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,17): warning CS8601: Possible null reference assignment.
                 //         z2[0] = y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(15, 17)
                );
        }

        [Fact]
        public void DynamicIndexerAccess_10()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0? x1)
    {
        x1 = x1[(dynamic)0];
    }

    void Test2(CL0? x2)
    {
        x2 = x2[0];
    }
}

class CL0
{
    public CL0 this[int x]
    {
        get { return new CL0(); }
        set { }
    }

    public CL0 this[long x]
    {
        get { return new CL0(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,14): warning CS8602: Possible dereference of a null reference.
                 //         x1 = x1[(dynamic)0];
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(9, 14),
                 // (14,14): warning CS8602: Possible dereference of a null reference.
                 //         x2 = x2[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        x2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public CL0 M1(int x)
    {
        return new CL0(); 
    }

    public CL0 M1(long x)
    {
        return new CL0(); 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2.M1((dynamic)0) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2.M1((dynamic)0)").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public CL0? M1(int x)
    {
        return new CL0(); 
    }

    public CL0  M1(long x)
    {
        return new CL0(); 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1.M1((dynamic)0);
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1.M1((dynamic)0)").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicInvocation_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public CL0 M1(int x)
    {
        return new CL0(); 
    }

    public CL0? M1(long x)
    {
        return new CL0(); 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1.M1((dynamic)0);
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1.M1((dynamic)0)").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicInvocation_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        dynamic y1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        dynamic y2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public CL0? M1(int x)
    {
        return new CL0(); 
    }

    public CL0? M1(long x)
    {
        return new CL0(); 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,22): warning CS8601: Possible null reference assignment.
                 //         dynamic y1 = x1.M1((dynamic)0);
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1.M1((dynamic)0)").WithLocation(9, 22)
                );
        }

        [Fact]
        public void DynamicInvocation_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        x2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public int M1(int x)
    {
        return x; 
    }

    public int M1(long x)
    {
        return (int)x; 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2.M1((dynamic)0) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2.M1((dynamic)0)").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0 x1)
    {
        x1 = x1.M1((dynamic)0);
    }

    void Test2(CL0 x2)
    {
        x2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0
{
    public int M1(int x)
    {
        return x; 
    }

    public long M1(long x)
    {
        return x; 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (14,14): hidden CS8607: Expression is probably never null.
                 //         x2 = x2.M1((dynamic)0) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2.M1((dynamic)0)").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(dynamic x1)
    {
        x1 = x1.M1(0);
    }

    void Test2(dynamic x2)
    {
        x2 = x2.M1(0) ?? x2;
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void DynamicInvocation_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1<T>(CL0<T> x1)
    {
        x1 = x1.M1((dynamic)0);
    }

    void Test2<T>(CL0<T> x2)
    {
        x2 = x2.M1((dynamic)0) ?? x2;
    }
}

class CL0<T>
{
    public T M1(int x)
    {
        return default(T); 
    }
    public long M1(long x)
    {
        return x; 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void DynamicInvocation_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(CL0? x1)
    {
        x1 = x1.M1((dynamic)0);
    }

    void Test2(CL0? x2)
    {
        x2 = x2.M1(0);
    }
}

class CL0
{
    public CL0 M1(int x)
    {
        return new CL0(); 
    }

    public CL0 M1(long x)
    {
        return new CL0(); 
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (9,14): warning CS8602: Possible dereference of a null reference.
                 //         x1 = x1.M1((dynamic)0);
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(9, 14),
                 // (14,14): warning CS8602: Possible dereference of a null reference.
                 //         x2 = x2.M1(0);
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(14, 14)
                );
        }

        [Fact]
        public void DynamicMemberAccess_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1(dynamic x1)
    {
        x1 = x1.M1;
    }

    void Test2(dynamic x2)
    {
        x2 = x2.M1 ?? x2;
    }

    void Test3(dynamic? x3)
    {
        dynamic y3 = x3.M1;
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (19,22): warning CS8602: Possible dereference of a null reference.
                 //         dynamic y3 = x3.M1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(19, 22)
                );
        }

        [Fact]
        public void DynamicObjectCreationExpression_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {}

    void Test1()
    {
        dynamic? x1 = null;
        CL0 y1 = new CL0(x1);
    }

    void Test2(CL0 y2)
    {
        dynamic? x2 = null;
        CL0 z2 = new CL0(x2) ?? y2;
    }
}

class CL0
{
    public CL0(int x)
    {
    }

    public CL0(long x)
    {
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,18): hidden CS8607: Expression is probably never null.
                 //         CL0 z2 = new CL0(x2) ?? y2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new CL0(x2)").WithLocation(16, 18)
                );
        }

        [Fact]
        public void NameOf_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(string x1, string? y1)
    {
        x1 = nameof(y1);
    }

    void Test2(string x2, string? y2)
    {
        string? z2 = nameof(y2);
        x2 = z2 ?? x2;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,14): hidden CS8607: Expression is probably never null.
                 //         x2 = z2 ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "z2").WithLocation(16, 14)
                );
        }

        [Fact]
        public void StringInterpolation_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(string x1, string? y1)
    {
        x1 = $""{y1}"";
    }

    void Test2(string x2, string? y2)
    {
        x2 = $""{y2}"" ?? x2;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,14): hidden CS8607: Expression is probably never null.
                 //         x2 = $"{y2}" ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, @"$""{y2}""").WithLocation(15, 14)
                );
        }

        [Fact]
        public void DelegateCreation_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(System.Action x1)
    {
        x1 = new System.Action(Main);
    }

    void Test2(System.Action x2)
    {
        x2 = new System.Action(Main) ?? x2;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,14): hidden CS8607: Expression is probably never null.
                 //         x2 = new System.Action(Main) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new System.Action(Main)").WithLocation(15, 14)
                );
        }

        // WRN_NullabilityMismatch* warnings should not be
        // reported for explicit delegate creation.
        [Fact]
        public void DelegateCreation_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    CL0<string?> M1(CL0<string> x) { throw new System.Exception(); }
    delegate CL0<string> D1(CL0<string?> x);

    void Test1()
    {
        D1 x1 = new D1(M1);
    }

    CL0<string> M2(CL0<string?> x) { throw new System.Exception(); }
    delegate CL0<string?> D2(CL0<string> x);

    void Test2()
    {
        D2 x2 = new D2(M2);
    }
}

class CL0<T>{}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Base_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Base
{
    public virtual void Test() {}
}

class C : Base
{
    static void Main()
    {
    }

    public override void Test()
    {
        base.Test();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void TypeOf_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(System.Type x1)
    {
        x1 = typeof(C);
    }

    void Test2(System.Type x2)
    {
        x2 = typeof(C) ?? x2;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,14): hidden CS8607: Expression is probably never null.
                 //         x2 = typeof(C) ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "typeof(C)").WithLocation(15, 14)
                );
        }

        [Fact]
        public void Default_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(C x1)
    {
        x1 = default(C);
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,14): warning CS8600: Cannot convert null to non-nullable reference.
                //         x1 = default(C);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(C)").WithLocation(10, 14)
                );
        }

        [Fact]
        public void BinaryOperator_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(string? x1, string? y1)
    {
        string z1 = x1 + y1;
    }

    void Test2(string? x2, string? y2)
    {
        string z2 = x2 + y2 ?? """";
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,21): hidden CS8607: Expression is probably never null.
                 //         string z2 = x2 + y2 ?? "";
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2 + y2").WithLocation(15, 21)
                );
        }

        [Fact]
        public void BinaryOperator_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(dynamic? x1, dynamic? y1)
    {
        dynamic z1 = x1 + y1;
    }

    void Test2(dynamic? x2, dynamic? y2)
    {
        dynamic z2 = x2 + y2 ?? """";
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void BinaryOperator_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(string? x1, CL0? y1)
    {
        CL0? z1 = x1 + y1;
        CL0 u1 = z1 ?? new CL0();
    }

    void Test2(string? x2, CL1? y2)
    {
        CL1 z2 = x2 + y2;
    }

    void Test3(string x3, CL0? y3, CL2 z3)
    {
        CL2 u3 = x3 + y3 + z3;
    }

    void Test4(string x4, CL1 y4, CL2 z4)
    {
        CL2 u4 = x4 + y4 + z4;
    }
}

class CL0 
{

    public static CL0 operator + (string? x, CL0 y)
    {
        return y;
    }
}

class CL1 
{

    public static CL1? operator + (string x, CL1? y)
    {
        return y;
    }
}

class CL2 
{

    public static CL2 operator + (CL0 x, CL2 y)
    {
        return y;
    }

    public static CL2 operator + (CL1 x, CL2 y)
    {
        return y;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,24): warning CS8604: Possible null reference argument for parameter 'y' in 'CL0 CL0.operator +(string? x, CL0 y)'.
                 //         CL0? z1 = x1 + y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y1").WithArguments("y", "CL0 CL0.operator +(string? x, CL0 y)").WithLocation(10, 24),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL0 u1 = z1 ?? new CL0();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "z1").WithLocation(11, 18),
                 // (16,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1? CL1.operator +(string x, CL1? y)'.
                 //         CL1 z2 = x2 + y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "CL1? CL1.operator +(string x, CL1? y)").WithLocation(16, 18),
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //         CL1 z2 = x2 + y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2 + y2").WithLocation(16, 18),
                 // (21,23): warning CS8604: Possible null reference argument for parameter 'y' in 'CL0 CL0.operator +(string? x, CL0 y)'.
                 //         CL2 u3 = x3 + y3 + z3;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y3").WithArguments("y", "CL0 CL0.operator +(string? x, CL0 y)").WithLocation(21, 23),
                 // (26,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL2 CL2.operator +(CL1 x, CL2 y)'.
                 //         CL2 u4 = x4 + y4 + z4;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x4 + y4").WithArguments("x", "CL2 CL2.operator +(CL1 x, CL2 y)").WithLocation(26, 18)
                );
        }

        [Fact]
        public void BinaryOperator_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1)
    {
        CL0? z1 = x1 && y1;
        CL0 u1 = z1;
    }

    void Test2(CL0 x2, CL0? y2)
    {
        CL0? z2 = x2 && y2;
        CL0 u2 = z2 ?? new CL0();
    }
}

class CL0
{
    public static CL0 operator &(CL0 x, CL0? y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0 x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'bool CL0.operator false(CL0 x)'.
                 //         CL0? z1 = x1 && y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "bool CL0.operator false(CL0 x)").WithLocation(10, 19),
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator &(CL0 x, CL0? y)'.
                 //         CL0? z1 = x1 && y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL0 CL0.operator &(CL0 x, CL0? y)").WithLocation(10, 19),
                 // (11,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u1 = z1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z1").WithLocation(11, 18),
                 // (17,18): hidden CS8607: Expression is probably never null.
                 //         CL0 u2 = z2 ?? new CL0();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "z2").WithLocation(17, 18)
                );
        }

        [Fact]
        public void BinaryOperator_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1)
    {
        CL0? z1 = x1 && y1;
    }
}

class CL0
{
    public static CL0 operator &(CL0? x, CL0 y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0 x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'bool CL0.operator false(CL0 x)'.
                 //         CL0? z1 = x1 && y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "bool CL0.operator false(CL0 x)").WithLocation(10, 19),
                 // (10,25): warning CS8604: Possible null reference argument for parameter 'y' in 'CL0 CL0.operator &(CL0? x, CL0 y)'.
                 //         CL0? z1 = x1 && y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y1").WithArguments("y", "CL0 CL0.operator &(CL0? x, CL0 y)").WithLocation(10, 25)
                );
        }

        [Fact]
        public void BinaryOperator_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1)
    {
        CL0? z1 = x1 && y1;
    }
}

class CL0
{
    public static CL0 operator &(CL0? x, CL0? y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0? x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void BinaryOperator_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1)
    {
        CL0? z1 = x1 || y1;
    }
}

class CL0
{
    public static CL0 operator |(CL0? x, CL0? y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0? x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'bool CL0.operator true(CL0 x)'.
                 //         CL0? z1 = x1 || y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "bool CL0.operator true(CL0 x)").WithLocation(10, 19)
                );
        }

        [Fact]
        public void BinaryOperator_08()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1)
    {
        CL0? z1 = x1 || y1;
    }
}

class CL0
{
    public static CL0 operator |(CL0? x, CL0? y)
    {
        return new CL0();
    }

    public static bool operator true(CL0? x)
    {
        return false;
    }

    public static bool operator false(CL0 x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void BinaryOperator_09()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0 x1, CL0 y1, CL0 z1)
    {
        CL0? u1 = x1 && y1 || z1;
    }
}

class CL0
{
    public static CL0? operator &(CL0 x, CL0 y)
    {
        return new CL0();
    }

    public static CL0 operator |(CL0 x, CL0 y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0 x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'bool CL0.operator true(CL0 x)'.
                 //         CL0? u1 = x1 && y1 || z1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1 && y1").WithArguments("x", "bool CL0.operator true(CL0 x)").WithLocation(10, 19),
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator |(CL0 x, CL0 y)'.
                 //         CL0? u1 = x1 && y1 || z1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1 && y1").WithArguments("x", "CL0 CL0.operator |(CL0 x, CL0 y)").WithLocation(10, 19)
                );
        }

        [Fact]
        public void BinaryOperator_10()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1, CL0? y1, CL0? z1)
    {
        CL0? u1 = x1 && y1 || z1;
    }

    void Test2(CL0 x2, CL0? y2, CL0? z2)
    {
        CL0? u1 = x2 && y2 || z2;
    }
}

class CL0
{
    public static CL0 operator &(CL0? x, CL0? y)
    {
        return new CL0();
    }

    public static CL0 operator |(CL0 x, CL0? y)
    {
        return new CL0();
    }

    public static bool operator true(CL0 x)
    {
        return false;
    }

    public static bool operator false(CL0? x)
    {
        return false;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'bool CL0.operator true(CL0 x)'.
                 //         CL0? u1 = x1 && y1 || z1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1 && y1").WithArguments("x", "bool CL0.operator true(CL0 x)").WithLocation(10, 19),
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator |(CL0 x, CL0? y)'.
                 //         CL0? u1 = x1 && y1 || z1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1 && y1").WithArguments("x", "CL0 CL0.operator |(CL0 x, CL0? y)").WithLocation(10, 19)
                );
        }

        [Fact]
        public void BinaryOperator_11()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(System.Action x1, System.Action y1)
    {
        System.Action u1 = x1 + y1;
    }

    void Test2(System.Action x2, System.Action y2)
    {
        System.Action u2 = x2 + y2 ?? x2;
    }

    void Test3(System.Action? x3, System.Action y3)
    {
        System.Action u3 = x3 + y3;
    }

    void Test4(System.Action? x4, System.Action y4)
    {
        System.Action u4 = x4 + y4 ?? y4;
    }

    void Test5(System.Action x5, System.Action? y5)
    {
        System.Action u5 = x5 + y5;
    }

    void Test6(System.Action x6, System.Action? y6)
    {
        System.Action u6 = x6 + y6 ?? x6;
    }

    void Test7(System.Action? x7, System.Action? y7)
    {
        System.Action u7 = x7 + y7;
    }

    void Test8(System.Action x8, System.Action y8)
    {
        System.Action u8 = x8 - y8;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (15,28): hidden CS8607: Expression is probably never null.
                 //         System.Action u2 = x2 + y2 ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2 + y2").WithLocation(15, 28),
                 // (25,28): hidden CS8607: Expression is probably never null.
                 //         System.Action u4 = x4 + y4 ?? y4;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4 + y4").WithLocation(25, 28),
                 // (35,28): hidden CS8607: Expression is probably never null.
                 //         System.Action u6 = x6 + y6 ?? x6;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x6 + y6").WithLocation(35, 28),
                 // (40,28): warning CS8601: Possible null reference assignment.
                 //         System.Action u7 = x7 + y7;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7 + y7").WithLocation(40, 28),
                 // (45,28): warning CS8601: Possible null reference assignment.
                 //         System.Action u8 = x8 - y8;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x8 - y8").WithLocation(45, 28)
                );
        }

        [Fact]
        public void BinaryOperator_12()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0 x1, CL0 y1)
    {
        CL0? u1 = x1 && !y1;
    }

    void Test2(bool x2, bool y2)
    {
        bool u2 = x2 && !y2;
    }
}

class CL0
{
    public static CL0 operator &(CL0? x, CL0 y)
    {
        return new CL0();
    }

    public static bool operator true(CL0? x)
    {
        return false;
    }

    public static bool operator false(CL0? x)
    {
        return false;
    }

    public static CL0? operator !(CL0 x)
    {
        return null;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,25): warning CS8604: Possible null reference argument for parameter 'y' in 'CL0 CL0.operator &(CL0? x, CL0 y)'.
                 //         CL0? u1 = x1 && !y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "!y1").WithArguments("y", "CL0 CL0.operator &(CL0? x, CL0 y)").WithLocation(10, 25)
                );
        }

        [Fact]
        public void MethodGroupConversion_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1)
    {
        System.Action u1 = x1.M1;
    }

    void Test2(CL0 x2)
    {
        System.Action u2 = x2.M1;
    }
}

class CL0
{
    public void M1() {}
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,28): warning CS8602: Possible dereference of a null reference.
                 //         System.Action u1 = x1.M1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(10, 28)
                );
        }

        [Fact]
        public void MethodGroupConversion_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void M1<T>(T x){}

    void Test1()
    {
        System.Action<string?> u1 = M1<string>;
    }

    void Test2()
    {
        System.Action<string> u2 = M1<string?>;
    }

    void Test3()
    {
        System.Action<CL0<string?>> u3 = M1<CL0<string>>;
    }

    void Test4()
    {
        System.Action<CL0<string>> u4 = M1<CL0<string?>>;
    }
}

class CL0<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,37): warning CS8622: Nullability of reference types in type of parameter 'x' of 'void C.M1<string>(string x)' doesn't match the target delegate 'Action<string?>'.
                //         System.Action<string?> u1 = M1<string>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "M1<string>").WithArguments("x", "void C.M1<string>(string x)", "System.Action<string?>").WithLocation(12, 37),
                // (17,36): warning CS8622: Nullability of reference types in type of parameter 'x' of 'void C.M1<string?>(string? x)' doesn't match the target delegate 'Action<string>'.
                //         System.Action<string> u2 = M1<string?>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "M1<string?>").WithArguments("x", "void C.M1<string?>(string? x)", "System.Action<string>").WithLocation(17, 36),
                // (22,42): warning CS8622: Nullability of reference types in type of parameter 'x' of 'void C.M1<CL0<string>>(CL0<string> x)' doesn't match the target delegate 'Action<CL0<string?>>'.
                //         System.Action<CL0<string?>> u3 = M1<CL0<string>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "M1<CL0<string>>").WithArguments("x", "void C.M1<CL0<string>>(CL0<string> x)", "System.Action<CL0<string?>>").WithLocation(22, 42),
                // (27,41): warning CS8622: Nullability of reference types in type of parameter 'x' of 'void C.M1<CL0<string?>>(CL0<string?> x)' doesn't match the target delegate 'Action<CL0<string>>'.
                //         System.Action<CL0<string>> u4 = M1<CL0<string?>>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "M1<CL0<string?>>").WithArguments("x", "void C.M1<CL0<string?>>(CL0<string?> x)", "System.Action<CL0<string>>").WithLocation(27, 41)
                );
        }

        [Fact]
        public void MethodGroupConversion_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void M1<T>(T x){}

    void Test1()
    {
        System.Action<string?> u1 = (System.Action<string?>)M1<string>;
    }

    void Test2()
    {
        System.Action<string> u2 = (System.Action<string>)M1<string?>;
    }

    void Test3()
    {
        System.Action<CL0<string?>> u3 = (System.Action<CL0<string?>>)M1<CL0<string>>;
    }

    void Test4()
    {
        System.Action<CL0<string>> u4 = (System.Action<CL0<string>>)M1<CL0<string?>>;
    }
}

class CL0<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void MethodGroupConversion_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    T M1<T>(){throw new System.Exception();}

    void Test1()
    {
        System.Func<string?> u1 = M1<string>;
    }

    void Test2()
    {
        System.Func<string> u2 = M1<string?>;
    }

    void Test3()
    {
        System.Func<CL0<string?>> u3 = M1<CL0<string>>;
    }

    void Test4()
    {
        System.Func<CL0<string>> u4 = M1<CL0<string?>>;
    }
}

class CL0<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,35): warning CS8621: Nullability of reference types in return type of 'string C.M1<string>()' doesn't match the target delegate 'Func<string?>'.
                 //         System.Func<string?> u1 = M1<string>;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "M1<string>").WithArguments("string C.M1<string>()", "System.Func<string?>").WithLocation(12, 35),
                 // (17,34): warning CS8621: Nullability of reference types in return type of 'string? C.M1<string?>()' doesn't match the target delegate 'Func<string>'.
                 //         System.Func<string> u2 = M1<string?>;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "M1<string?>").WithArguments("string? C.M1<string?>()", "System.Func<string>").WithLocation(17, 34),
                 // (22,40): warning CS8621: Nullability of reference types in return type of 'CL0<string> C.M1<CL0<string>>()' doesn't match the target delegate 'Func<CL0<string?>>'.
                 //         System.Func<CL0<string?>> u3 = M1<CL0<string>>;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "M1<CL0<string>>").WithArguments("CL0<string> C.M1<CL0<string>>()", "System.Func<CL0<string?>>").WithLocation(22, 40),
                 // (27,39): warning CS8621: Nullability of reference types in return type of 'CL0<string?> C.M1<CL0<string?>>()' doesn't match the target delegate 'Func<CL0<string>>'.
                 //         System.Func<CL0<string>> u4 = M1<CL0<string?>>;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "M1<CL0<string?>>").WithArguments("CL0<string?> C.M1<CL0<string?>>()", "System.Func<CL0<string>>").WithLocation(27, 39)
                );
        }

        [Fact]
        public void MethodGroupConversion_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    T M1<T>(){throw new System.Exception();}

    void Test1()
    {
        System.Func<string?> u1 = (System.Func<string?>)M1<string>;
    }

    void Test2()
    {
        System.Func<string> u2 = (System.Func<string>)M1<string?>;
    }

    void Test3()
    {
        System.Func<CL0<string?>> u3 = (System.Func<CL0<string?>>)M1<CL0<string>>;
    }

    void Test4()
    {
        System.Func<CL0<string>> u4 = (System.Func<CL0<string>>)M1<CL0<string?>>;
    }
}

class CL0<T>
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void UnaryOperator_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1)
    {
        CL0 u1 = !x1;
    }

    void Test2(CL1 x2)
    {
        CL1 u2 = !x2;
    }

    void Test3(CL2? x3)
    {
        CL2 u3 = !x3;
    }

    void Test4(CL1 x4)
    {
        dynamic y4 = x4; 
        CL1 u4 = !y4;
        dynamic v4 = !y4 ?? y4; 
    }

    void Test5(bool x5)
    {
        bool u5 = !x5;
    }
}

class CL0
{
    public static CL0 operator !(CL0 x)
    {
        return new CL0();
    }
}

class CL1
{
    public static CL1? operator !(CL1 x)
    {
        return new CL1();
    }
}

class CL2
{
    public static CL2 operator !(CL2? x)
    {
        return new CL2();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator !(CL0 x)'.
                 //         CL0 u1 = !x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL0 CL0.operator !(CL0 x)").WithLocation(10, 19),
                 // (15,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u2 = !x2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "!x2").WithLocation(15, 18)
                );
        }

        [Fact]
        public void Conversion_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1)
    {
        CL1 u1 = x1;
    }

    void Test2(CL0? x2, CL0 y2)
    {
        int u2 = x2;
        long v2 = x2;
        int w2 = y2;
    }

    void Test3(CL0 x3)
    {
        CL2 u3 = x3;
    }

    void Test4(CL0 x4)
    {
        CL3? u4 = x4;
        CL3 v4 = u4 ?? new CL3();
    }

    void Test5(dynamic? x5)
    {
        CL3 u5 = x5;
    }

    void Test6(dynamic? x6)
    {
        CL3? u6 = x6;
        CL3 v6 = u6 ?? new CL3();
    }

    void Test7(CL0? x7)
    {
        dynamic u7 = x7;
    }

    void Test8(CL0 x8)
    {
        dynamic? u8 = x8;
        dynamic v8 = u8 ?? x8;
    }

    void Test9(dynamic? x9)
    {
        object u9 = x9;
    }

    void Test10(object? x10)
    {
        dynamic u10 = x10;
    }

    void Test11(CL4? x11)
    {
        CL3 u11 = x11;
    }

    void Test12(CL3? x12)
    {
        CL4 u12 = (CL4)x12;
    }

    void Test13(int x13)
    {
        object? u13 = x13;
        object v13 = u13 ?? new object();
    }

    void Test14<T>(T x14)
    {
        object u14 = x14;
        object v14 = ((object)x14) ?? new object();
    }

    void Test15(int? x15)
    {
        object u15 = x15;
    }

    void Test16()
    {
        System.IFormattable? u16 = $""{3}"";
        object v16 = u16 ?? new object();
    }
}

class CL0
{
    public static implicit operator CL1(CL0 x) { return new CL1(); }
    public static implicit operator int(CL0 x) { return 0; }
    public static implicit operator long(CL0? x) { return 0; }
    public static implicit operator CL2?(CL0 x) { return new CL2(); }
    public static implicit operator CL3(CL0? x) { return new CL3(); }
}

class CL1 {}
class CL2 {}
class CL3 {}
class CL4 : CL3 {}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (10,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0.implicit operator CL1(CL0 x)'.
                //         CL1 u1 = x1;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL0.implicit operator CL1(CL0 x)").WithLocation(10, 18),
                // (15,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0.implicit operator int(CL0 x)'.
                //         int u2 = x2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "CL0.implicit operator int(CL0 x)").WithLocation(15, 18),
                // (22,18): warning CS8601: Possible null reference assignment.
                //         CL2 u3 = x3;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(22, 18),
                // (28,18): hidden CS8607: Expression is probably never null.
                //         CL3 v4 = u4 ?? new CL3();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(28, 18),
                // (44,22): warning CS8601: Possible null reference assignment.
                //         dynamic u7 = x7;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7").WithLocation(44, 22),
                // (50,22): hidden CS8607: Expression is probably never null.
                //         dynamic v8 = u8 ?? x8;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u8").WithLocation(50, 22),
                // (55,21): warning CS8601: Possible null reference assignment.
                //         object u9 = x9;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x9").WithLocation(55, 21),
                // (60,23): warning CS8601: Possible null reference assignment.
                //         dynamic u10 = x10;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x10").WithLocation(60, 23),
                // (65,19): warning CS8601: Possible null reference assignment.
                //         CL3 u11 = x11;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(65, 19),
                // (70,19): warning CS8601: Possible null reference assignment.
                //         CL4 u12 = (CL4)x12;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "(CL4)x12").WithLocation(70, 19),
                // (76,22): hidden CS8607: Expression is probably never null.
                //         object v13 = u13 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u13").WithLocation(76, 22),
                // (87,22): warning CS8601: Possible null reference assignment.
                //         object u15 = x15;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x15").WithLocation(87, 22),
                // (93,22): hidden CS8607: Expression is probably never null.
                //         object v16 = u16 ?? new object();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u16").WithLocation(93, 22)
                );
        }

        [Fact]
        public void Conversion_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0<string?> x1)
    {
        CL0<string> u1 = x1;
        CL0<string> v1 = (CL0<string>)x1;
    }
}

class CL0<T>
{
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,26): warning CS8619: Nullability of reference types in value of type 'CL0<string?>' doesn't match target type 'CL0<string>'.
                 //         CL0<string> u1 = x1;
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("CL0<string?>", "CL0<string>").WithLocation(10, 26)
                );
        }

        [Fact]
        public void IncrementOperator_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL0? x1)
    {
        CL0? u1 = ++x1;
        CL0 v1 = u1 ?? new CL0(); 
        CL0 w1 = x1 ?? new CL0(); 
    }
    void Test2(CL0? x2)
    {
        CL0 u2 = x2++;
        CL0 v2 = x2 ?? new CL0();
    }
    void Test3(CL1? x3)
    {
        CL1 u3 = --x3;
        CL1 v3 = x3;
    }
    void Test4(CL1 x4)
    {
        CL1? u4 = x4--; // Result of increment is nullable, storing it in not nullable parameter.
        CL1 v4 = u4 ?? new CL1(); 
        CL1 w4 = x4 ?? new CL1();
    }
    void Test5(CL1 x5)
    {
        CL1 u5 = --x5;
    }

    void Test6(CL1 x6)
    {
        x6--; 
    }
}

class CL0
{
    public static CL0 operator ++(CL0 x)
    {
        return new CL0();
    }
}

class CL1
{
    public static CL1? operator --(CL1? x)
    {
        return new CL1();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,21): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0? u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(10, 21),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL0 v1 = u1 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (12,18): hidden CS8607: Expression is probably never null.
                 //         CL0 w1 = x1 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1").WithLocation(12, 18),
                 // (16,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0 u2 = x2++;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(16, 18),
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u2 = x2++;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2++").WithLocation(16, 18),
                 // (17,18): hidden CS8607: Expression is probably never null.
                 //         CL0 v2 = x2 ?? new CL0();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(17, 18),
                 // (21,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u3 = --x3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x3").WithLocation(21, 18),
                 // (22,18): warning CS8601: Possible null reference assignment.
                 //         CL1 v3 = x3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(22, 18),
                 // (26,19): warning CS8601: Possible null reference assignment.
                 //         CL1? u4 = x4--; // Result of increment is nullable, storing it in not nullable parameter.
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4--").WithLocation(26, 19),
                 // (27,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v4 = u4 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(27, 18),
                 // (28,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w4 = x4 ?? new CL1();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4").WithLocation(28, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5").WithLocation(32, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5").WithLocation(32, 18),
                 // (37,9): warning CS8601: Possible null reference assignment.
                 //         x6--; 
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6--").WithLocation(37, 9)
                );
        }

        [Fact]
        public void IncrementOperator_02()
        {
            CSharpCompilation c = CreateStandardCompilation(
@"#pragma warning disable 8618
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        CL0? u1 = ++x1;
        CL0 v1 = u1 ?? new CL0(); 
    }

    void Test2()
    {
        CL0 u2 = x2++;
    }

    void Test3()
    {
        CL1 u3 = --x3;
    }

    void Test4()
    {
        CL1? u4 = x4--; // Result of increment is nullable, storing it in not nullable property.
        CL1 v4 = u4 ?? new CL1(); 
    }

    void Test5(CL1 x5)
    {
        CL1 u5 = --x5;
    }

    CL0? x1 {get; set;}
    CL0? x2 {get; set;}
    CL1? x3 {get; set;}
    CL1 x4 {get; set;}
    CL1 x5 {get; set;}
}

class CL0
{
    public static CL0 operator ++(CL0 x)
    {
        return new CL0();
    }
}

class CL1
{
    public static CL1? operator --(CL1? x)
    {
        return new CL1();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,21): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0? u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(10, 21),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL0 v1 = u1 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (16,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0 u2 = x2++;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(16, 18),
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u2 = x2++;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2++").WithLocation(16, 18),
                 // (21,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u3 = --x3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x3").WithLocation(21, 18),
                 // (26,19): warning CS8601: Possible null reference assignment.
                 //         CL1? u4 = x4--; // Result of increment is nullable, storing it in not nullable property.
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4--").WithLocation(26, 19),
                 // (27,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v4 = u4 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(27, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5").WithLocation(32, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5").WithLocation(32, 18)
                );
        }

        [Fact]
        public void IncrementOperator_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(X1 x1)
    {
        CL0? u1 = ++x1[0];
        CL0 v1 = u1 ?? new CL0(); 
    }

    void Test2(X1 x2)
    {
        CL0 u2 = x2[0]++;
    }

    void Test3(X3 x3)
    {
        CL1 u3 = --x3[0];
    }

    void Test4(X4 x4)
    {
        CL1? u4 = x4[0]--; // Result of increment is nullable, storing it in not nullable parameter.
        CL1 v4 = u4 ?? new CL1(); 
    }

    void Test5(X4 x5)
    {
        CL1 u5 = --x5[0];
    }
}

class CL0
{
    public static CL0 operator ++(CL0 x)
    {
        return new CL0();
    }
}

class CL1
{
    public static CL1? operator --(CL1? x)
    {
        return new CL1();
    }
}

class X1
{
    public CL0? this[int x]
    {
        get { return null; }
        set { }
    }
}

class X3
{
    public CL1? this[int x]
    {
        get { return null; }
        set { }
    }
}

class X4
{
    public CL1 this[int x]
    {
        get { return new CL1(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,21): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0? u1 = ++x1[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1[0]").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(10, 21),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL0 v1 = u1 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (16,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL0 CL0.operator ++(CL0 x)'.
                 //         CL0 u2 = x2[0]++;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2[0]").WithArguments("x", "CL0 CL0.operator ++(CL0 x)").WithLocation(16, 18),
                 // (16,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u2 = x2[0]++;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2[0]++").WithLocation(16, 18),
                 // (21,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u3 = --x3[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x3[0]").WithLocation(21, 18),
                 // (26,19): warning CS8601: Possible null reference assignment.
                 //         CL1? u4 = x4[0]--; // Result of increment is nullable, storing it in not nullable parameter.
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4[0]--").WithLocation(26, 19),
                 // (27,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v4 = u4 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(27, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5[0]").WithLocation(32, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u5 = --x5[0];
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x5[0]").WithLocation(32, 18)
                );
        }

        [Fact]
        public void IncrementOperator_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class C
{
    static void Main()
    {
    }

    void Test1(dynamic? x1)
    {
        dynamic? u1 = ++x1;
        dynamic v1 = u1 ?? new object(); 
    }

    void Test2(dynamic? x2)
    {
        dynamic u2 = x2++;
    }

    void Test3(dynamic? x3)
    {
        dynamic u3 = --x3;
    }

    void Test4(dynamic x4)
    {
        dynamic? u4 = x4--; 
        dynamic v4 = u4 ?? new object(); 
    }

    void Test5(dynamic x5)
    {
        dynamic u5 = --x5;
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,22): warning CS8601: Possible null reference assignment.
                 //         dynamic u2 = x2++;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2++").WithLocation(16, 22),
                 // (27,22): hidden CS8607: Expression is probably never null.
                 //         dynamic v4 = u4 ?? new object(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(27, 22)
                );
        }

        [Fact]
        public void IncrementOperator_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(B? x1)
    {
        B? u1 = ++x1;
        B v1 = u1 ?? new B(); 
    }
}

class A
{
    public static C? operator ++(A x)
    {
        return new C();
    }
}

class C : A
{
    public static implicit operator B(C x)
    {
        return new B();
    }
}

class B : A
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'C? A.operator ++(A x)'.
                 //         B? u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "C? A.operator ++(A x)").WithLocation(10, 19),
                 // (10,17): warning CS8604: Possible null reference argument for parameter 'x' in 'C.implicit operator B(C x)'.
                 //         B? u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "++x1").WithArguments("x", "C.implicit operator B(C x)").WithLocation(10, 17),
                 // (11,16): hidden CS8607: Expression is probably never null.
                 //         B v1 = u1 ?? new B(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void IncrementOperator_06()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(B x1)
    {
        B u1 = ++x1;
    }
}

class A
{
    public static C operator ++(A x)
    {
        return new C();
    }
}

class C : A
{
    public static implicit operator B?(C x)
    {
        return new B();
    }
}

class B : A
{
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,16): warning CS8601: Possible null reference assignment.
                 //         B u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "++x1").WithLocation(10, 16),
                 // (10,16): warning CS8601: Possible null reference assignment.
                 //         B u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "++x1").WithLocation(10, 16)
                );
        }

        [Fact]
        public void IncrementOperator_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(Convertible? x1)
    {
        Convertible? u1 = ++x1;
        Convertible v1 = u1 ?? new Convertible(); 
    }

    void Test2(int? x2)
    {
        var u2 = ++x2;
    }

    void Test3(byte x3)
    {
        var u3 = ++x3;
    }
}

class Convertible
{
    public static implicit operator int(Convertible c)
    {
        return 0;
    }

    public static implicit operator Convertible(int i)
    {
        return new Convertible();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,29): warning CS8604: Possible null reference argument for parameter 'c' in 'Convertible.implicit operator int(Convertible c)'.
                 //         Convertible? u1 = ++x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("c", "Convertible.implicit operator int(Convertible c)").WithLocation(10, 29),
                 // (11,26): hidden CS8607: Expression is probably never null.
                 //         Convertible v1 = u1 ?? new Convertible(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 26)
                );
        }

        [Fact]
        public void CompoundAssignment_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL0 y1)
    {
        CL1? u1 = x1 += y1;
        CL1 v1 = u1 ?? new CL1(); 
        CL1 w1 = x1 ?? new CL1(); 
    }
}

class CL0
{
    public static CL1 operator +(CL0 x, CL0 y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0(CL1 x)
    {
        return new CL0();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
                 //         CL1? u1 = x1 += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(10, 19),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v1 = u1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (12,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w1 = x1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1").WithLocation(12, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL0? y1)
    {
        CL1? u1 = x1 += y1;
        CL1 v1 = u1 ?? new CL1(); 
        CL1 w1 = x1 ?? new CL1(); 
    }
}

class CL0
{
    public static CL1 operator +(CL0 x, CL0 y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0(CL1? x)
    {
        return new CL0();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,25): warning CS8604: Possible null reference argument for parameter 'y' in 'CL1 CL0.operator +(CL0 x, CL0 y)'.
                 //         CL1? u1 = x1 += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y1").WithArguments("y", "CL1 CL0.operator +(CL0 x, CL0 y)").WithLocation(10, 25),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v1 = u1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (12,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w1 = x1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1").WithLocation(12, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL0? y1)
    {
        CL1? u1 = x1 += y1;
        CL1 v1 = u1 ?? new CL1(); 
        CL1 w1 = x1 ?? new CL1(); 
    }

    void Test2(CL0? x2, CL0 y2)
    {
        CL0 u2 = x2 += y2;
        CL0 w2 = x2; 
    }

    void Test3(CL0? x3, CL0 y3)
    {
        x3 = new CL0();
        CL0 u3 = x3 += y3;
        CL0 w3 = x3; 
    }

    void Test4(CL0? x4, CL0 y4)
    {
        x4 = new CL0();
        x4 += y4;
        CL0 w4 = x4; 
    }
}

class CL0
{
    public static CL1 operator +(CL0 x, CL0? y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0?(CL1? x)
    {
        return new CL0();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1 CL0.operator +(CL0 x, CL0? y)'.
                 //         CL1? u1 = x1 += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL1 CL0.operator +(CL0 x, CL0? y)").WithLocation(10, 19),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v1 = u1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (12,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w1 = x1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1").WithLocation(12, 18),
                 // (17,18): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1 CL0.operator +(CL0 x, CL0? y)'.
                 //         CL0 u2 = x2 += y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("x", "CL1 CL0.operator +(CL0 x, CL0? y)").WithLocation(17, 18),
                 // (17,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u2 = x2 += y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2 += y2").WithLocation(17, 18),
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //         CL0 w2 = x2; 
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(18, 18),
                 // (24,18): warning CS8601: Possible null reference assignment.
                 //         CL0 u3 = x3 += y3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 += y3").WithLocation(24, 18),
                 // (25,18): warning CS8601: Possible null reference assignment.
                 //         CL0 w3 = x3; 
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(25, 18),
                 // (32,18): warning CS8601: Possible null reference assignment.
                 //         CL0 w4 = x4; 
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4").WithLocation(32, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL0? y1)
    {
        x1 = new CL1();
        CL1? u1 = x1 += y1;
        CL1 w1 = x1;
        w1 = u1; 
    }

    void Test2(CL1 x2, CL0 y2)
    {
        CL1 u2 = x2 += y2;
        CL1 w2 = x2; 
    }

    void Test3(CL1 x3, CL0 y3)
    {
        x3 += y3;
    }

    void Test4(CL0? x4, CL0 y4)
    {
        CL0? u4 = x4 += y4;
        CL0 v4 = u4 ?? new CL0(); 
        CL0 w4 = x4 ?? new CL0(); 
    }

    void Test5(CL0 x5, CL0 y5)
    {
        x5 += y5;
    }
}

class CL0
{
    public static CL1? operator +(CL0 x, CL0? y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0(CL1 x)
    {
        return new CL0();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,18): warning CS8601: Possible null reference assignment.
                 //         CL1 w1 = x1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 18),
                 // (13,14): warning CS8601: Possible null reference assignment.
                 //         w1 = u1; 
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u1").WithLocation(13, 14),
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u2 = x2 += y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2 += y2").WithLocation(18, 18),
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //         CL1 u2 = x2 += y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2 += y2").WithLocation(18, 18),
                 // (24,9): warning CS8601: Possible null reference assignment.
                 //         x3 += y3;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 += y3").WithLocation(24, 9),
                 // (29,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1? CL0.operator +(CL0 x, CL0? y)'.
                 //         CL0? u4 = x4 += y4;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x4").WithArguments("x", "CL1? CL0.operator +(CL0 x, CL0? y)").WithLocation(29, 19),
                 // (29,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
                 //         CL0? u4 = x4 += y4;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x4 += y4").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(29, 19),
                 // (30,18): hidden CS8607: Expression is probably never null.
                 //         CL0 v4 = u4 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u4").WithLocation(30, 18),
                 // (31,18): hidden CS8607: Expression is probably never null.
                 //         CL0 w4 = x4 ?? new CL0(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x4").WithLocation(31, 18),
                 // (36,9): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
                 //         x5 += y5;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x5 += y5").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(36, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(int x1, int y1)
    {
        var u1 = x1 += y1;
    }

    void Test2(int? x2, int y2)
    {
        var u2 = x2 += y2;
    }

    void Test3(dynamic? x3, dynamic? y3)
    {
        dynamic? u3 = x3 += y3;
        dynamic v3 = u3;
        dynamic w3 = u3 ?? v3;
    }

    void Test4(dynamic? x4, dynamic? y4)
    {
        dynamic u4 = x4 += y4;
    }
}
", new[] { CSharpRef, SystemCoreRef }, parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void CompoundAssignment_06()
        {
            CSharpCompilation c = CreateStandardCompilation(
@"#pragma warning disable 8618
class Test
{
    static void Main()
    {
    }

    void Test1(CL0 y1)
    {
        CL1? u1 = x1 += y1;
        CL1 v1 = u1 ?? new CL1(); 
        CL1 w1 = x1 ?? new CL1(); 
    }

    void Test2(CL0 y2)
    {
        CL1? u2 = x2 += y2;
        CL1 v2 = u2 ?? new CL1(); 
        CL1 w2 = x2 ?? new CL1(); 
    }

    CL1? x1 {get; set;}
    CL1 x2 {get; set;}
}

class CL0
{
    public static CL1 operator +(CL0 x, CL0 y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0(CL1 x)
    {
        return new CL0();
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
                 //         CL1? u1 = x1 += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(10, 19),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v1 = u1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (18,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v2 = u2 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u2").WithLocation(18, 18),
                 // (19,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w2 = x2 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2").WithLocation(19, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_07()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL2 x1, CL0 y1)
    {
        CL1? u1 = x1[0] += y1;
        CL1 v1 = u1 ?? new CL1(); 
        CL1 w1 = x1[0] ?? new CL1(); 
    }

    void Test2(CL3 x2, CL0 y2)
    {
        CL1? u2 = x2[0] += y2;
        CL1 v2 = u2 ?? new CL1(); 
        CL1 w2 = x2[0] ?? new CL1(); 
    }
}

class CL0
{
    public static CL1 operator +(CL0 x, CL0 y)
    {
        return new CL1();
    }
}

class CL1
{
    public static implicit operator CL0(CL1 x)
    {
        return new CL0();
    }
}

class CL2
{
    public CL1? this[int x]
    {
        get { return new CL1(); }
        set { }
    }
}

class CL3
{
    public CL1 this[int x]
    {
        get { return new CL1(); }
        set { }
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,19): warning CS8604: Possible null reference argument for parameter 'x' in 'CL1.implicit operator CL0(CL1 x)'.
                 //         CL1? u1 = x1[0] += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1[0]").WithArguments("x", "CL1.implicit operator CL0(CL1 x)").WithLocation(10, 19),
                 // (11,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v1 = u1 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u1").WithLocation(11, 18),
                 // (18,18): hidden CS8607: Expression is probably never null.
                 //         CL1 v2 = u2 ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "u2").WithLocation(18, 18),
                 // (19,18): hidden CS8607: Expression is probably never null.
                 //         CL1 w2 = x2[0] ?? new CL1(); 
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2[0]").WithLocation(19, 18)
                );
        }

        [Fact]
        public void Events_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    event System.Action? E1;

    void Test1()
    {
        E1();
    }

    delegate void D2 (object x);
    event D2 E2;

    void Test2()
    {
        E2(null);
    }

    delegate object? D3 ();
    event D3 E3;

    void Test3()
    {
        object x3 = E3();
    }

    void Test4()
    {
                    //E1?();
        System.Action? x4 = E1;
                    //x4?();
    }

    void Test5()
    {
        System.Action x5 = E1;
    }

    void Test6(D2? x6)
    {
        E2 = x6;
    }

    void Test7(D2? x7)
    {
        E2 += x7;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         E1();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E1").WithLocation(12, 9),
                // (20,12): warning CS8600: Cannot convert null to non-nullable reference.
                //         E2(null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(20, 12),
                // (28,21): warning CS8601: Possible null reference assignment.
                //         object x3 = E3();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E3()").WithLocation(28, 21),
                // (40,28): warning CS8601: Possible null reference assignment.
                //         System.Action x5 = E1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E1").WithLocation(40, 28),
                // (45,14): warning CS8601: Possible null reference assignment.
                //         E2 = x6;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6").WithLocation(45, 14)
                );
        }

        [Fact]
        public void Events_02()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }
}

struct TS1
{
    event System.Action? E1;

    TS1(System.Action x1) 
    {
        E1 = x1;
        System.Action y1 = E1 ?? x1;

        E1 = x1;
        TS1 z1 = this;
        y1 = z1.E1 ?? x1;
    }

    void Test3(System.Action x3)
    {
        TS1 s3;
        s3.E1 = x3;
        System.Action y3 = s3.E1 ?? x3;

        s3.E1 = x3;
        TS1 z3 = s3;
        y3 = z3.E1 ?? x3;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,28): hidden CS8607: Expression is probably never null.
                 //         System.Action y1 = E1 ?? x1;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(16, 28),
                 // (20,14): hidden CS8607: Expression is probably never null.
                 //         y1 = z1.E1 ?? x1;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "z1.E1").WithLocation(20, 14),
                 // (27,28): hidden CS8607: Expression is probably never null.
                 //         System.Action y3 = s3.E1 ?? x3;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "s3.E1").WithLocation(27, 28),
                 // (31,14): hidden CS8607: Expression is probably never null.
                 //         y3 = z3.E1 ?? x3;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "z3.E1").WithLocation(31, 14)
                );
        }

        [Fact]
        public void Events_03()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }
}

struct TS2
{
    event System.Action? E2;

    TS2(System.Action x2) 
    {
        this = new TS2();
        System.Action z2 = E2;
        System.Action y2 = E2 ?? x2;
    }
}

", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (16,28): warning CS8601: Possible null reference assignment.
                 //         System.Action z2 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(16, 28)
                );
        }

        [Fact]
        public void Events_04()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL0? x1, System.Action? y1)
    {
        System.Action v1 = x1.E1 += y1;
    }

    void Test2(CL0? x2, System.Action? y2)
    {
        System.Action v2 = x2.E1 -= y2;
    }
}

class CL0
{
    public event System.Action? E1;

    void Dummy()
    {
        var x = E1;
    }
}

", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,28): error CS0029: Cannot implicitly convert type 'void' to 'System.Action'
                 //         System.Action v1 = x1.E1 += y1;
                 Diagnostic(ErrorCode.ERR_NoImplicitConv, "x1.E1 += y1").WithArguments("void", "System.Action").WithLocation(10, 28),
                 // (10,28): warning CS8602: Possible dereference of a null reference.
                 //         System.Action v1 = x1.E1 += y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(10, 28),
                 // (15,28): error CS0029: Cannot implicitly convert type 'void' to 'System.Action'
                 //         System.Action v2 = x2.E1 -= y2;
                 Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2.E1 -= y2").WithArguments("void", "System.Action").WithLocation(15, 28),
                 // (15,28): warning CS8602: Possible dereference of a null reference.
                 //         System.Action v2 = x2.E1 -= y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(15, 28)
                );
        }

        [Fact]
        public void Events_05()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    public event System.Action E1;

    void Test1(Test? x1)
    {
        System.Action v1 = x1.E1;
    }
}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (12,28): warning CS8602: Possible dereference of a null reference.
                 //         System.Action v1 = x1.E1;
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(12, 28)
                );
        }

        [Fact]
        public void AsOperator_01()
        {
            CSharpCompilation c = CreateStandardCompilation(@"
class Test
{
    static void Main()
    {
    }

    void Test1(CL1 x1)
    {
        object y1 = x1 as object ?? new object();
    }

    void Test2(int x2)
    {
        object y2 = x2 as object ?? new object();
    }

    void Test3(CL1? x3)
    {
        object y3 = x3 as object;
    }

    void Test4(int? x4)
    {
        object y4 = x4 as object;
    }

    void Test5(object x5)
    {
        CL1 y5 = x5 as CL1;
    }

    void Test6()
    {
        CL1 y6 = null as CL1;
    }

    void Test7<T>(T x7)
    {
        CL1 y7 = x7 as CL1;
    }
}

class CL1 {}
", parseOptions: TestOptions.Regular8);

            c.VerifyDiagnostics(
                 // (10,21): hidden CS8607: Expression is probably never null.
                 //         object y1 = x1 as object ?? new object();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x1 as object").WithLocation(10, 21),
                 // (15,21): hidden CS8607: Expression is probably never null.
                 //         object y2 = x2 as object ?? new object();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x2 as object").WithLocation(15, 21),
                 // (20,21): warning CS8601: Possible null reference assignment.
                 //         object y3 = x3 as object;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 as object").WithLocation(20, 21),
                 // (25,21): warning CS8601: Possible null reference assignment.
                 //         object y4 = x4 as object;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4 as object").WithLocation(25, 21),
                 // (30,18): warning CS8601: Possible null reference assignment.
                 //         CL1 y5 = x5 as CL1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5 as CL1").WithLocation(30, 18),
                 // (35,18): warning CS8601: Possible null reference assignment.
                 //         CL1 y6 = null as CL1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null as CL1").WithLocation(35, 18),
                 // (40,18): warning CS8601: Possible null reference assignment.
                 //         CL1 y7 = x7 as CL1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7 as CL1").WithLocation(40, 18)
                );
        }

        [Fact]
        public void Await_01()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        object x = await new D() ?? new object();
    }
}

class D
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public object GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                 // (10,20): hidden CS8607: Expression is probably never null.
                 //         object x = await new D() ?? new object();
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "await new D()").WithLocation(10, 20)
                );
        }

        [Fact]
        public void Await_02()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        object x = await new D();
    }
}

class D
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public object? GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                 // (10,20): warning CS8601: Possible null reference assignment.
                 //         object x = await new D();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "await new D()").WithLocation(10, 20)
                );
        }

        [Fact]
        public void NoPiaObjectCreation_01()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest28 //: ITest28
{
    public ClassITest28(int x){} 
}
";

            var piaCompilation = CreateCompilation(pia, new MetadataReference[] { MscorlibRef_v4_0_30316_17626 }, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7);

            CompileAndVerify(piaCompilation);

            string source = @"
class UsePia
{
    public static void Main()
    {
    }

    void Test1(ITest28 x1)
    {
        x1 = new ITest28();
    }

    void Test2(ITest28 x2)
    {
        x2 = new ITest28() ?? x2;
    }
}";

            var compilation = CreateCompilation(source,
                                                new MetadataReference[] { MscorlibRef_v4_0_30316_17626, new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) },
                                                options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                 // (15,14): hidden CS8607: Expression is probably never null.
                 //         x2 = new ITest28() ?? x2;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "new ITest28()").WithLocation(15, 14)
                );
        }

        [Fact]
        public void SymbolDisplay_01()
        {
            var source = @"
abstract class B
{
    string? F1; 
    event System.Action? E1;
    string? P1 {get; set;}
    string?[][,] P2 {get; set;}
    System.Action<string?> M1(string? x) {return null;}
    string[]?[,] M2(string[][,]? x) {return null;}
    void M3(string?* x) {}
    public abstract string? this[System.Action? x] {get; set;} 

    public static implicit operator B?(int x) { return null; }
}

delegate string? D1();

interface I1<T>{}
interface I2<T>{}

class C<T> {}

class F : C<F?>, I1<C<B?>>, I2<C<B>?>
{}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            var b = compilation.GetTypeByMetadataName("B");
            Assert.Equal("System.String? B.F1", b.GetMember("F1").ToTestDisplayString());
            Assert.Equal("event System.Action? B.E1", b.GetMember("E1").ToTestDisplayString());
            Assert.Equal("System.String? B.P1 { get; set; }", b.GetMember("P1").ToTestDisplayString());
            Assert.Equal("System.String?[][,] B.P2 { get; set; }", b.GetMember("P2").ToTestDisplayString());
            Assert.Equal("System.Action<System.String?> B.M1(System.String? x)", b.GetMember("M1").ToTestDisplayString());
            Assert.Equal("System.String[]?[,] B.M2(System.String[][,]? x)", b.GetMember("M2").ToTestDisplayString());
            Assert.Equal("void B.M3(System.String?* x)", b.GetMember("M3").ToTestDisplayString());
            Assert.Equal("System.String? B.this[System.Action? x] { get; set; }", b.GetMember("this[]").ToTestDisplayString());
            Assert.Equal("B.implicit operator B?(int)", b.GetMember("op_Implicit").ToDisplayString());
            Assert.Equal("String? D1()", compilation.GetTypeByMetadataName("D1").ToDisplayString(new SymbolDisplayFormat(delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature)));

            var f = compilation.GetTypeByMetadataName("F");
            Assert.Equal("C<F?>", f.BaseType.ToTestDisplayString());
            Assert.Equal("I1<C<B?>>", f.Interfaces[0].ToTestDisplayString());
            Assert.Equal("I2<C<B>?>", f.Interfaces[1].ToTestDisplayString());
        }

        [Fact]
        public void DifferentParseOptions_01()
        {
            var source = @"";
            var optionsWithoutFeature = TestOptions.Regular8;
            var optionsWithFeature = optionsWithoutFeature.WithNullCheckingFeature(NullableReferenceFlags.None);
            Assert.Throws<System.ArgumentException>(() => CreateStandardCompilation(new[] { CSharpSyntaxTree.ParseText(source, optionsWithFeature),
                                                                                                CSharpSyntaxTree.ParseText(source, optionsWithoutFeature) },
                                                                                        options: TestOptions.ReleaseDll));

            Assert.Throws<System.ArgumentException>(() => CreateStandardCompilation(new[] { CSharpSyntaxTree.ParseText(source, optionsWithoutFeature),
                                                                                                CSharpSyntaxTree.ParseText(source, optionsWithFeature) },
                                                                                        options: TestOptions.ReleaseDll));

            CreateStandardCompilation(new[] { CSharpSyntaxTree.ParseText(source, optionsWithFeature),
                                                  CSharpSyntaxTree.ParseText(source, optionsWithFeature) },
                                          options: TestOptions.ReleaseDll);

            CreateStandardCompilation(new[] { CSharpSyntaxTree.ParseText(source, optionsWithoutFeature),
                                                  CSharpSyntaxTree.ParseText(source, optionsWithoutFeature) },
                                          options: TestOptions.ReleaseDll);
        }

        [Fact]
        public void NullableAttribute_01()
        {
            var source =
@"#pragma warning disable 8618
public abstract class B
{
    public string? F1; 
    public event System.Action? E1;
    public string? P1 {get; set;}
    public string?[][,] P2 {get; set;}
    public System.Action<string?> M1(string? x) {throw new System.NotImplementedException();}
    public string[]?[,] M2(string[][,]? x) {throw new System.NotImplementedException();}
    public abstract string? this[System.Action? x] {get; set;} 

    public static implicit operator B?(int x) {throw new System.NotImplementedException();}
    public event System.Action? E2
    {
        add { }
        remove { }
    }
}

public delegate string? D1();

public interface I1<T>{}
public interface I2<T>{}

public class C<T> {}

public class F : C<F?>, I1<C<B?>>, I2<C<B>?>
{}
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                // (5,33): warning CS0067: The event 'B.E1' is never used
                //     public event System.Action? E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("B.E1").WithLocation(5, 33)
                );

            CompileAndVerify(compilation,
                             symbolValidator: m =>
                                {
                                    var b = ((PEModuleSymbol)m).GlobalNamespace.GetTypeMember("B");
                                    Assert.Equal("System.String? B.F1", b.GetMember("F1").ToTestDisplayString());
                                    Assert.Equal("event System.Action? B.E1", b.GetMember("E1").ToTestDisplayString());
                                    Assert.Equal("System.String? B.P1 { get; set; }", b.GetMember("P1").ToTestDisplayString());
                                    Assert.Equal("System.String?[][,] B.P2 { get; set; }", b.GetMember("P2").ToTestDisplayString());
                                    Assert.Equal("System.Action<System.String?> B.M1(System.String? x)", b.GetMember("M1").ToTestDisplayString());
                                    Assert.Equal("System.String[]?[,] B.M2(System.String[][,]? x)", b.GetMember("M2").ToTestDisplayString());
                                    Assert.Equal("System.String? B.this[System.Action? x] { get; set; }", b.GetMember("this[]").ToTestDisplayString());
                                    Assert.Equal("B.implicit operator B?(int)", b.GetMember("op_Implicit").ToDisplayString());
                                    Assert.Equal("event System.Action? B.E2", b.GetMember("E2").ToTestDisplayString());
                                    Assert.Equal("String? D1()", compilation.GetTypeByMetadataName("D1").ToDisplayString(new SymbolDisplayFormat(delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature)));

                                    var f = ((PEModuleSymbol)m).GlobalNamespace.GetTypeMember("F");
                                    Assert.Equal("C<F?>", f.BaseType.ToTestDisplayString());

                                    // PROTOTYPE(NullableReferenceTypes): Should we round-trip
                                    // nullable modifiers for implemented interfaces too?
                                    Assert.Equal("I1<C<B>>", f.Interfaces[0].ToTestDisplayString());
                                    Assert.Equal("I2<C<B>>", f.Interfaces[1].ToTestDisplayString());
                                });
        }

        [Fact]
        public void NullableAttribute_02()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public object F1;

    public object? P1 { get; set;}
}
", parseOptions: TestOptions.Regular8, options: TestOptions.DebugDll);

            string source = @"
class C 
{
    static void Main()
    {
    }

    void Test1(CL0 x1, object? y1)
    {
        x1.F1 = y1;
    }

    void Test2(CL0 x2, object y2)
    {
        y2 = x2.P1;
    }
}
";

            var expected = new[]
            {
                // (10,17): warning CS8601: Possible null reference assignment.
                //         x1.F1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(10, 17),
                // (15,14): warning CS8601: Possible null reference assignment.
                //         y2 = x2.P1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2.P1").WithLocation(15, 14)
            };

            CSharpCompilation c = CreateStandardCompilation(source,
                                                                parseOptions: TestOptions.Regular8,
                                                                references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(source,
                                                                parseOptions: TestOptions.Regular8,
                                                                references: new[] { c0.ToMetadataReference() });

            c.VerifyDiagnostics(expected);
        }

        [Fact]
        public void NullableAttribute_03()
        {
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public object F1;
}
", parseOptions: TestOptions.Regular8, options: TestOptions.DebugDll);

            string source = @"
class C 
{
    static void Main()
    {
    }

    void Test1(CL0 x1, object? y1)
    {
        x1.F1 = y1;
    }
}
";

            var expected = new[]
            {
                // (10,17): warning CS8601: Possible null reference assignment.
                //         x1.F1 = y1;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(10, 17)
            };

            CSharpCompilation c = CreateStandardCompilation(source,
                                                                parseOptions: TestOptions.Regular8,
                                                                references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(source,
                                                                parseOptions: TestOptions.Regular8,
                                                                references: new[] { c0.ToMetadataReference() });

            c.VerifyDiagnostics(expected);
        }

        [Fact]
        public void NullableAttribute_04()
        {
            var source =
@"#pragma warning disable 8618
using System.Runtime.CompilerServices;

public abstract class B
{
    [Nullable] public string F1; 
    [Nullable] public event System.Action E1;
    [Nullable] public string[][,] P2 {get; set;}
    [return:Nullable] public System.Action<string?> M1(string? x) 
    {throw new System.NotImplementedException();}
    public string[][,] M2([Nullable] string[][,] x) 
    {throw new System.NotImplementedException();}
}

public class C<T> {}

[Nullable] public class F : C<F>
{}
";
            var compilation = CreateStandardCompilation(new[] { source, NullableAttributeDefinition }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                // (7,6): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                //     [Nullable] public event System.Action E1;
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (8,6): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                //     [Nullable] public string[][,] P2 {get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (9,13): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                //     [return:Nullable] public System.Action<string?> M1(string? x) 
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (11,28): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                //     public string[][,] M2([Nullable] string[][,] x) 
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (6,6): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                //     [Nullable] public string F1; 
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (17,2): error CS8623: Explicit application of 'System.Runtime.CompilerServices.NullableAttribute' is not allowed.
                // [Nullable] public class F : C<F>
                Diagnostic(ErrorCode.ERR_ExplicitNullableAttribute, "Nullable"),
                // (7,43): warning CS0067: The event 'B.E1' is never used
                //     [Nullable] public event System.Action E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("B.E1").WithLocation(7, 43)
                );
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOutForAssembly] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForAssembly.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void OptOutFromAssembly_01()
        {
            var parseOptions = TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowAssemblyOptOut | NullableReferenceFlags.AllowMemberOptOut);
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public object F1;
}
", parseOptions: parseOptions, options: TestOptions.DebugDll, assemblyName: "OptOutFromAssembly_01_Lib");

            string source = @"
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""OptOutFromAssembly_01_Lib"")]

class C 
{
    static void Main()
    {
    }

    void Test1(CL0 x1, object? y1)
    {
        x1.F1 = y1;
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: parseOptions,
                                                                references: new[] { c0.EmitToImageReference() });

            CompileAndVerify(c, symbolValidator: m =>
                                                 {
                                                     Assert.Equal("System.Runtime.CompilerServices.NullableAttribute", (((PEModuleSymbol)m).GetAttributes().Single().ToString()));
                                                 });

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: parseOptions,
                                                                references: new[] { c0.ToMetadataReference() });

            c.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOutForAssembly] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForAssembly.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void OptOutFromAssembly_02()
        {
            var parseOptions = TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowAssemblyOptOut | NullableReferenceFlags.AllowMemberOptOut);
            CSharpCompilation c0 = CreateStandardCompilation(@"
public class CL0 
{
    public object F1;
}
", parseOptions: parseOptions, options: TestOptions.DebugDll, assemblyName: "OptOutFromAssembly_02_Lib1");

            CSharpCompilation c1 = CreateStandardCompilation(@"
public class CL1 
{
    public object F2;
}
", parseOptions: parseOptions, options: TestOptions.DebugDll, assemblyName: "OptOutFromAssembly_02_Lib2");

            string source = @"
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""OptOutFromAssembly_02_Lib1"")]

class C 
{
    static void Main()
    {
    }

    void Test1(CL0 x1, object? y1)
    {
        x1.F1 = y1;
    }

    void Test2(CL1 x2, object? y2)
    {
        x2.F2 = y2;
    }
}
";

            var expected = new[]
            {
                // (17,17): warning CS8601: Possible null reference assignment.
                //         x2.F2 = y2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(17, 17)
            };

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: parseOptions,
                                                                references: new[] { c0.EmitToImageReference(), c1.EmitToImageReference() });

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: parseOptions,
                                                                references: new[] { c0.ToMetadataReference(), c1.ToMetadataReference() });

            c.VerifyDiagnostics(expected);
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOutForAssembly] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForAssembly.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void OptOutFromAssembly_03()
        {
            var parseOptions = TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowAssemblyOptOut | NullableReferenceFlags.AllowMemberOptOut);
            string source = @"
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(null)]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""invalid, assembly, name"")]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name1, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name2, PublicKeyToken=aaabbbcccdddeee"")]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name3, Version=1"")]

[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name1, PublicKey=01240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name1, PublicKey=02240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
[module:System.Runtime.CompilerServices.NullableOptOutForAssembly(""name1"")]

class C 
{
    static void Main()
    {
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: parseOptions);

            c.VerifyDiagnostics(
                 // (2,9): warning CS1700: Assembly reference 'null' is invalid and cannot be resolved
                 // [module:System.Runtime.CompilerServices.NullableOptOutForAssembly(null)]
                 Diagnostic(ErrorCode.WRN_InvalidAssemblyName, "System.Runtime.CompilerServices.NullableOptOutForAssembly(null)").WithArguments("null").WithLocation(2, 9),
                 // (3,9): warning CS1700: Assembly reference 'invalid, assembly, name' is invalid and cannot be resolved
                 // [module:System.Runtime.CompilerServices.NullableOptOutForAssembly("invalid, assembly, name")]
                 Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"System.Runtime.CompilerServices.NullableOptOutForAssembly(""invalid, assembly, name"")").WithArguments("invalid, assembly, name").WithLocation(3, 9),
                 // (5,9): warning CS1700: Assembly reference 'name2, PublicKeyToken=aaabbbcccdddeee' is invalid and cannot be resolved
                 // [module:System.Runtime.CompilerServices.NullableOptOutForAssembly("name2, PublicKeyToken=aaabbbcccdddeee")]
                 Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"System.Runtime.CompilerServices.NullableOptOutForAssembly(""name2, PublicKeyToken=aaabbbcccdddeee"")").WithArguments("name2, PublicKeyToken=aaabbbcccdddeee").WithLocation(5, 9),
                 // (6,9): warning CS1700: Assembly reference 'name3, Version=1' is invalid and cannot be resolved
                 // [module:System.Runtime.CompilerServices.NullableOptOutForAssembly("name3, Version=1")]
                 Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"System.Runtime.CompilerServices.NullableOptOutForAssembly(""name3, Version=1"")").WithArguments("name3, Version=1").WithLocation(6, 9)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_01()
        {
            string lib = @"
using System;

public class CL0 
{
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

partial class C 
{
    partial class B 
    {
        public event Action E1;
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact]
        public void NullableOptOut_02()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(true)]
";

            string lib =
@"#pragma warning disable 8618
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
public class CL0 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    public class CL1 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action F1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? F2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action P1 { get; set; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? P2 { get; set; }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action M1() { throw new System.NotImplementedException(); }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? M2() { return null; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public void M3(Action x3) {}
    }
}
";

            string source1 =
@"#pragma warning disable 8618
using System;


partial class C 
{

    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action E1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 =
@"#pragma warning disable 8618
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
partial class C 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //             E1 = x11;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(18, 18),
                 // (24,19): hidden CS8607: Expression is probably never null.
                 //             x12 = E1 ?? x12;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(24, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x13 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(30, 19),
                 // (13,20): warning CS8601: Possible null reference assignment.
                 //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                 // (14,20): warning CS8601: Possible null reference assignment.
                 //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                 // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                 //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                 // (21,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                 // (22,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                 // (23,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                 // (29,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                 // (31,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                );

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            var expected = new[] {
                // (13,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                // (14,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                // (21,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                // (22,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                // (23,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                // (29,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                // (30,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                // (31,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                };

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOut] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForDefinition.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void NullableOptOut_03()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(true)]
";

            string lib = @"
using System;

public class CL0 
{
    public class CL1 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action F1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? F2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action P1 { get; set; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? P2 { get; set; }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action M1() { throw new System.NotImplementedException(); }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? M2() { return null; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;


partial class C 
{

    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action E1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action? E2;

        void Test11(Action? x11)
        {
            E1 = x11;
        }

        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                // (17,18): warning CS8601: Possible null reference assignment.
                //             E1 = x11;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(17, 18),
                // (22,19): hidden CS8607: Expression is probably never null.
                //             x12 = E1 ?? x12;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(22, 19),
                // (10,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(10, 20),
                // (11,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(11, 20),
                // (12,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(12, 18),
                // (17,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(17, 19),
                // (18,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(18, 19),
                // (19,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(19, 19));

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                // (10,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(10, 20),
                // (11,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(11, 20),
                // (12,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(12, 18),
                // (17,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(17, 19),
                // (18,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(18, 19),
                // (19,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(19, 19));

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                // (10,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(10, 20),
                // (11,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(11, 20),
                // (12,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(12, 18),
                // (17,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(17, 19),
                // (18,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(18, 19),
                // (19,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(19, 19));
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_04()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
public class CL0 
{
    public class CL1 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action F1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? F2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action P1 { get; set; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? P2 { get; set; }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action M1() { throw new System.NotImplementedException(); }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? M2() { return null; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action E1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action? E2;

        void Test11(Action? x11)
        {
            E1 = x11;
        }

        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
partial class C 
{
    partial class B 
    {
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_05()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
public class CL0 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    public class CL1 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action F1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? F2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action P1 { get; set; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? P2 { get; set; }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action M1() { throw new System.NotImplementedException(); }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public Action? M2() { return null; }
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action E1;
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        public event Action? E2;

        void Test11(Action? x11)
        {
            E1 = x11;
        }

        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
partial class C 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    partial class B 
    {
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_06()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
public class CL0 
{
    [System.Runtime.CompilerServices.NullableOptOut(false)]
    public class CL1 
    {
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action F1;
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action? F2;

        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action P1 { get; set; }
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action? P2 { get; set; }

        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action M1() { throw new System.NotImplementedException(); }
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public Action? M2() { return null; }
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
partial class C 
{
    [System.Runtime.CompilerServices.NullableOptOut(false)]
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public event Action E1;
        [System.Runtime.CompilerServices.NullableOptOut(true)]
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_07()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
public class CL0 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
partial class C 
{
    [System.Runtime.CompilerServices.NullableOptOut(true)]
    partial class B 
    {
        public event Action E1;
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_08()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
public class CL0 
{
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
partial class C 
{
    partial class B 
    {
        public event Action E1;
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_09()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(true)]
";

            string lib = @"
using System;

public class CL0 
{
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;

partial class C 
{
    partial class B 
    {
        public event Action E1;
        public event Action? E2;

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

partial class C 
{
    partial class B 
    {
        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        [System.Runtime.CompilerServices.NullableOptOut(false)]
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics();
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_10()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(true)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
public class CL0 
{
    [System.Runtime.CompilerServices.NullableOptOut(false)]
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;


partial class C 
{

    partial class B 
    {
        
        public event Action E1;
        
        public event Action? E2;

        
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(true)]
partial class C 
{
    [System.Runtime.CompilerServices.NullableOptOut(false)]
    partial class B 
    {
        
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //             E1 = x11;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(18, 18),
                 // (24,19): hidden CS8607: Expression is probably never null.
                 //             x12 = E1 ?? x12;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(24, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x13 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(30, 19),
                 // (13,20): warning CS8601: Possible null reference assignment.
                 //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                 // (14,20): warning CS8601: Possible null reference assignment.
                 //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                 // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                 //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                 // (21,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                 // (22,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                 // (23,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                 // (29,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                 // (31,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                );

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            var expected = new[] {
                // (13,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                // (14,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                // (21,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                // (22,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                // (23,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                // (29,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                // (30,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                // (31,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                };

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_11()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(true)]
";

            string lib = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
public class CL0 
{
    
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;


partial class C 
{

    partial class B 
    {
        
        public event Action E1;
        
        public event Action? E2;

        
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;

[System.Runtime.CompilerServices.NullableOptOut(false)]
partial class C 
{
    
    partial class B 
    {
        
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //             E1 = x11;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(18, 18),
                 // (24,19): hidden CS8607: Expression is probably never null.
                 //             x12 = E1 ?? x12;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(24, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x13 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(30, 19),
                 // (13,20): warning CS8601: Possible null reference assignment.
                 //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                 // (14,20): warning CS8601: Possible null reference assignment.
                 //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                 // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                 //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                 // (21,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                 // (22,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                 // (23,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                 // (29,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                 // (31,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                );

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            var expected = new[] {
                // (13,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                // (14,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                // (21,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                // (22,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                // (23,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                // (29,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                // (30,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                // (31,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                };

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_12()
        {
            string moduleAttributes = @"
[module:System.Runtime.CompilerServices.NullableOptOut(false)]
";

            string lib = @"
using System;


public class CL0 
{
    
    public class CL1 
    {
        public Action F1;
        public Action? F2;

        public Action P1 { get; set; }
        public Action? P2 { get; set; }

        public Action M1() { throw new System.NotImplementedException(); }
        public Action? M2() { return null; }
        public void M3(Action x3) {}
    }
}
";

            string source1 = @"
using System;


partial class C 
{

    partial class B 
    {
        
        public event Action E1;
        
        public event Action? E2;

        
        void Test11(Action? x11)
        {
            E1 = x11;
        }

        
        void Test12(Action x12)
        {
            x12 = E1 ?? x12;
        }

        
        void Test13(Action x13)
        {
            x13 = E2;
        }
    }
}
";

            string source2 = @"
using System;


partial class C 
{
    
    partial class B 
    {
        
        void Test21(CL0.CL1 c, Action? x21)
        {
            c.F1 = x21;
            c.P1 = x21;
            c.M3(x21);
        }

        
        void Test22(CL0.CL1 c, Action x22)
        {
            x22 = c.F1 ?? x22;
            x22 = c.P1 ?? x22;
            x22 = c.M1() ?? x22;
        }

        
        void Test23(CL0.CL1 c, Action x23)
        {
            x23 = c.F2;
            x23 = c.P2;
            x23 = c.M2();
        }
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib, source1, source2 },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (18,18): warning CS8601: Possible null reference assignment.
                 //             E1 = x11;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11").WithLocation(18, 18),
                 // (24,19): hidden CS8607: Expression is probably never null.
                 //             x12 = E1 ?? x12;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "E1").WithLocation(24, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x13 = E2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "E2").WithLocation(30, 19),
                 // (13,20): warning CS8601: Possible null reference assignment.
                 //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                 // (14,20): warning CS8601: Possible null reference assignment.
                 //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                 // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                 //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                 // (21,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                 // (22,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                 // (23,19): hidden CS8607: Expression is probably never null.
                 //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                 // (29,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                 // (30,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                 // (31,19): warning CS8601: Possible null reference assignment.
                 //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                );

            CSharpCompilation c1 = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, lib },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c1.VerifyDiagnostics();

            var expected = new[] {
                // (13,20): warning CS8601: Possible null reference assignment.
                //             c.F1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(13, 20),
                // (14,20): warning CS8601: Possible null reference assignment.
                //             c.P1 = x21;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x21").WithLocation(14, 20),
                // (15,18): warning CS8604: Possible null reference argument for parameter 'x3' in 'void CL1.M3(Action x3)'.
                //             c.M3(x21);
                 Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x21").WithArguments("x3", "void CL1.M3(Action x3)").WithLocation(15, 18),
                // (21,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.F1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.F1").WithLocation(21, 19),
                // (22,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.P1 ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.P1").WithLocation(22, 19),
                // (23,19): hidden CS8607: Expression is probably never null.
                //             x22 = c.M1() ?? x22;
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "c.M1()").WithLocation(23, 19),
                // (29,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.F2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F2").WithLocation(29, 19),
                // (30,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.P2;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.P2").WithLocation(30, 19),
                // (31,19): warning CS8601: Possible null reference assignment.
                //             x23 = c.M2();
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.M2()").WithLocation(31, 19)
                };

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.ToMetadataReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);

            c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, moduleAttributes, source2 }, new[] { c1.EmitToImageReference() },
                                              parseOptions: TestOptions.Regular8,
                                              options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(expected);
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_13()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    string?[]? M1()
    {
        return null;
    }

    void Test1()
    {
        M1().ToString();
        M1()[0].ToString();
        var x1 = M1()[0] ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    string[] M2()
    {
        return null;
    }

    void Test2()
    {
        M2()[0] = null;
        var x2 = M2()[0] ?? """";
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_14()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string?>? M1()
    {
        return null;
    }

    void Test1()
    {
        M1().ToString();
        M1().P1.ToString();
        var x1 = M1().P1 ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string> M2()
    {
        return null;
    }

    void Test2()
    {
        M2().P1 = null;
        var x2 = M2().P1 ?? """";
    }

    CL1<string?> M3()
    {
         return new CL1<string?>();
    }

    void Test3()
    {
        M3().ToString();
        M3().P1.ToString();
        var x3 = M3().P1 ?? """";
    }

    CL1<string> M4()
    {
        return new CL1<string>();
    }

    void Test4()
    {
        M4().P1 = null;
        var x4 = M4().P1 ?? """";
    }
}

class CL1<T>
{
    public T P1;
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (39,9): warning CS8602: Possible dereference of a null reference.
                 //         M3().P1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M3().P1").WithLocation(39, 9),
                 // (50,19): warning CS8601: Possible null reference assignment.
                 //         M4().P1 = null;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(50, 19),
                 // (51,18): hidden CS8607: Expression is probably never null.
                 //         var x4 = M4().P1 ?? "";
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M4().P1").WithLocation(51, 18)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_15()
        {
            string source = @"
class C 
{
    void Main() {}
}

class CL1<T>
{
    public virtual CL1<T> M1()
    {
        return new CL1<T>();
    }
}

class CL2 : CL1<string>
{
    public override CL1<string?> M1() // 2
    {
        return base.M1();
    }
}

class CL3 : CL1<string?>
{
    public override CL1<string?> M1()
    {
        return base.M1();
    }
}

class CL4<T> where T : class
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public virtual CL4<T?> M4()
    {
        return new CL4<T?>();
    }
}

class CL5 : CL4<string>
{
    public override CL4<string> M4()
    {
        return base.M4();
    }
}

class CL6 : CL4<string?>
{
    public override CL4<string> M4() // 6
    {
        return base.M4();
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (17,34): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override CL1<string?> M1() // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M1").WithLocation(17, 34),
                 // (19,16): warning CS8619: Nullability of reference types in value of type 'CL1<string>' doesn't match target type 'CL1<string?>'.
                 //         return base.M1();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "base.M1()").WithArguments("CL1<string>", "CL1<string?>").WithLocation(19, 16),
                 // (50,33): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                 //     public override CL4<string> M4() // 6
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M4").WithLocation(50, 33),
                 // (52,16): warning CS8619: Nullability of reference types in value of type 'CL4<string?>' doesn't match target type 'CL4<string>'.
                 //         return base.M4();
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "base.M4()").WithArguments("CL4<string?>", "CL4<string>").WithLocation(52, 16)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_16()
        {
            string source = @"
class C 
{
    void Main() {}
}

class CL1<T>
{
    public virtual void M1(CL1<T> x1)
    {
    }
}

class CL2 : CL1<string>
{
    public override void M1(CL1<string?> x2) // 2
    {
    }
}

class CL3 : CL1<string?>
{
    public override void M1(CL1<string?> x3)
    {
    }
}

class CL4<T> where T : class
{
    [System.Runtime.CompilerServices.NullableOptOut]
    public virtual void M4(CL4<T?> x4)
    {
    }
}

class CL5 : CL4<string>
{
    public override void M4(CL4<string> x5)
    {
    }
}

class CL6 : CL4<string?>
{
    public override void M4(CL4<string> x6) // 6
    {
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (16,26): warning CS8610: Nullability of reference types in type of parameter 'x2' doesn't match overridden member.
                 //     public override void M1(CL1<string?> x2) // 2
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("x2").WithLocation(16, 26),
                 // (45,26): warning CS8610: Nullability of reference types in type of parameter 'x6' doesn't match overridden member.
                 //     public override void M4(CL4<string> x6) // 6
                 Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M4").WithArguments("x6").WithLocation(45, 26)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_17()
        {
            string source = @"
class C 
{
    void Main() {}

    void Test1()
    {
        CL0<string?>.M1().ToString();
        var x1 = CL0<string?>.M1() ?? """";
    }
}

[System.Runtime.CompilerServices.NullableOptOut]
class CL0<T>
{
    public static T M1()
    {
        return default(T);
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (8,9): warning CS8602: Possible dereference of a null reference.
                 //         CL0<string?>.M1().ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "CL0<string?>.M1()").WithLocation(8, 9)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_18()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string?>? M1 { get; set; }

    void Test1()
    {
        M1.ToString();
        M1.P1.ToString();
        var x1 = M1.P1 ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string> M2 { get; set; }

    void Test2()
    {
        M2.P1 = null;
        var x2 = M2.P1 ?? """";
    }

    CL1<string?> M3 { get; set; }

    void Test3()
    {
        M3.ToString();
        M3.P1.ToString();
        var x3 = M3.P1 ?? """";
    }

    CL1<string> M4 { get; set; }

    void Test4()
    {
        M4.P1 = null;
        var x4 = M4.P1 ?? """";
    }
}

class CL1<T>
{
    public T P1;
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (30,9): warning CS8602: Possible dereference of a null reference.
                 //         M3.P1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M3.P1").WithLocation(30, 9),
                 // (38,17): warning CS8601: Possible null reference assignment.
                 //         M4.P1 = null;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(38, 17),
                 // (39,18): hidden CS8607: Expression is probably never null.
                 //         var x4 = M4.P1 ?? "";
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M4.P1").WithLocation(39, 18)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_19()
        {
            string source = @"
class C 
{
    void Main() {}

    void Test1()
    {
        CL0<string?>.M1.ToString();
        var x1 = CL0<string?>.M1 ?? """";
    }
}

[System.Runtime.CompilerServices.NullableOptOut]
class CL0<T>
{
    public static T M1 { get; set; }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (8,9): warning CS8602: Possible dereference of a null reference.
                 //         CL0<string?>.M1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "CL0<string?>.M1").WithLocation(8, 9)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_20()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string?>? M1;

    void Test1()
    {
        M1.ToString();
        M1.P1.ToString();
        var x1 = M1.P1 ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    CL1<string> M2;

    void Test2()
    {
        M2.P1 = null;
        var x2 = M2.P1 ?? """";
    }

    CL1<string?> M3;

    void Test3()
    {
        M3.ToString();
        M3.P1.ToString();
        var x3 = M3.P1 ?? """";
    }

    CL1<string> M4;

    void Test4()
    {
        M4.P1 = null;
        var x4 = M4.P1 ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void Assign()
    {
        M1 = null;
        M2 = null;
        M3 = null;
        M4 = null;
    }
}

class CL1<T>
{
    public T P1;
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (30,9): warning CS8602: Possible dereference of a null reference.
                 //         M3.P1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M3.P1").WithLocation(30, 9),
                 // (38,17): warning CS8601: Possible null reference assignment.
                 //         M4.P1 = null;
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(38, 17),
                 // (39,18): hidden CS8607: Expression is probably never null.
                 //         var x4 = M4.P1 ?? "";
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M4.P1").WithLocation(39, 18)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_21()
        {
            string source = @"
class C 
{
    void Main() {}

    void Test1()
    {
        CL0<string?>.M1.ToString();
        var x1 = CL0<string?>.M1 ?? """";
    }
}

[System.Runtime.CompilerServices.NullableOptOut]
class CL0<T>
{
    public static T M1 = default(T);
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (8,9): warning CS8602: Possible dereference of a null reference.
                 //         CL0<string?>.M1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "CL0<string?>.M1").WithLocation(8, 9)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_22()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    event System.Func<string?>? M1;

    void Test1()
    {
        M1.ToString();
        M1().ToString();
        var x1 = M1() ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    event System.Func<string> M2;

    void Test2()
    {
        var x2 = M2() ?? """";
    }

    event System.Func<string?> M3;

    void Test3()
    {
        M3.ToString();
        M3().ToString();
        var x3 = M3() ?? """";
    }

    event System.Func<string> M4;

    void Test4()
    {
        var x4 = M4() ?? """";
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void Assign()
    {
        M1 = null;
        M2 = null;
        M3 = null;
        M4 = null;
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (29,9): warning CS8602: Possible dereference of a null reference.
                 //         M3().ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M3()").WithLocation(29, 9),
                 // (37,18): hidden CS8607: Expression is probably never null.
                 //         var x4 = M4() ?? "";
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "M4()").WithLocation(37, 18)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_23()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    delegate void D1 (CL1<string?>? x1);

    void M1(D1 x1) {}

    void Test1()
    {
        M1(a1 => a1.ToString());
        M1(b1 => b1.P1.ToString());
        M1(c1 => {var x1 = c1.P1 ?? """";});
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    delegate void D2(CL1<string> x2);
    
    void M2(D2 x2) {}

    void Test2()
    {
        M2(a2 => {a2.P1 = null;});
        M2(b2 => {var x2 = b2.P1 ?? """";});
    }

    delegate void D3 (CL1<string?> x3);
    void M3(D3 x3) {}

    void Test3()
    {
        M3(a3 => a3.ToString());
        M3(b3 => b3.P1.ToString());
        M3(c3 => {var x3 = c3.P1 ?? """";});
    }

    delegate void D4(CL1<string> x4);
    void M4(D4 x4) {}

    void Test4()
    {
        M4(a4 => {a4.P1 = null;});
        M4(b4 => {var x4 = b4.P1 ?? """";});
    }

    void Test11()
    {
        D1 u11 = a11 => a11.ToString();
        D1 v11 = b11 => b11.P1.ToString();
        D1 w11 = c11 => {var x11 = c11.P1 ?? """";};
    }

    void Test21()
    {
        D2 u21 = a21 => {a21.P1 = null;};
        D2 v21 = b21 => {var x21 = b21.P1 ?? """";};
    }

    void Test31()
    {
        D3 u31 = a31 => a31.ToString();
        D3 v31 = b31 => b31.P1.ToString();
        D3 w31 = c31 => {var x31 = c31.P1 ?? """";};
    }

    void Test41()
    {
        D4 u41 = a41 => {a41.P1 = null;};
        D4 v41 = b41 => {var x41 = b41.P1 ?? """";};
    }
}

class CL1<T>
{
    public T P1;
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (35,18): warning CS8602: Possible dereference of a null reference.
                 //         M3(b3 => b3.P1.ToString());
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b3.P1").WithLocation(35, 18),
                 // (44,27): warning CS8601: Possible null reference assignment.
                 //         M4(a4 => {a4.P1 = null;});
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(44, 27),
                 // (45,28): hidden CS8607: Expression is probably never null.
                 //         M4(b4 => {var x4 = b4.P1 ?? "";});
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "b4.P1").WithLocation(45, 28),
                 // (64,25): warning CS8602: Possible dereference of a null reference.
                 //         D3 v31 = b31 => b31.P1.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b31.P1").WithLocation(64, 25),
                 // (70,35): warning CS8601: Possible null reference assignment.
                 //         D4 u41 = a41 => {a41.P1 = null;};
                 Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(70, 35),
                 // (71,36): hidden CS8607: Expression is probably never null.
                 //         D4 v41 = b41 => {var x41 = b41.P1 ?? "";};
                 Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "b41.P1").WithLocation(71, 36)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_24()
        {
            string source = @"
class C 
{
    void Main() {}

    void M1(CL0<string?> x1) {}

    void Test1()
    {
        M1(a1 => a1.ToString());
        M1(b1 => {var x1 = b1 ?? """";});
    }

    void Test2()
    {
        CL0<string?> u2 = a2 => a2.ToString();
        CL0<string?> v2 = b2 => {var x2 = b2 ?? """";};
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void M2(CL0<string?> x1) {}

    void Test3()
    {
        M2(a3 => a3.ToString());
        M2(b3 => {var x3 = b3 ?? """";});
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void M3(CL1<string?> x1) {}

    void Test4()
    {
        M3(a4 => a4.ToString());
        M3(b4 => {var x4 = b4 ?? """";});
    }

    void M4(CL1<string?> x1) {}

    void Test5()
    {
        M4(a5 => a5.ToString());
        M4(b5 => {var x5 = b5 ?? """";});
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void M5(CL2<string?> x1) {}

    void Test6()
    {
        M5(a6 => a6.ToString());
        M5(b6 => {var x6 = b6 ?? """";});
    }
}

[System.Runtime.CompilerServices.NullableOptOut]
delegate void CL0<T>(T x); 

[System.Runtime.CompilerServices.NullableOptOut]
delegate void CL1<T>(T? x) where T : class; 

delegate void CL2<T>(T? x) where T : class; 
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (10,18): warning CS8602: Possible dereference of a null reference.
                 //         M1(a1 => a1.ToString());
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a1").WithLocation(10, 18),
                 // (16,33): warning CS8602: Possible dereference of a null reference.
                 //         CL0<string?> u2 = a2 => a2.ToString();
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a2").WithLocation(16, 33),
                 // (42,18): warning CS8602: Possible dereference of a null reference.
                 //         M4(a5 => a5.ToString());
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a5").WithLocation(42, 18),
                 // (51,18): warning CS8602: Possible dereference of a null reference.
                 //         M5(a6 => a6.ToString());
                 Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "a6").WithLocation(51, 18)
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_25()
        {
            string source = @"
class C 
{
    void Main() {}

    [System.Runtime.CompilerServices.NullableOptOut]
    delegate string D1 ();

    void M1(D1 x1) {}

    void Test1()
    {
        M1(() => null);
    }

    void Test2()
    {
        D1 x2 = () => null;
    }

    delegate T D3<T> ();

    [System.Runtime.CompilerServices.NullableOptOut]
    void M3(D3<string> x3) {}

    void Test3()
    {
        M3(() => null);
    }
}
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "NullableOptOut does not control warnings")] // PROTOTYPE(NullableReferenceTypes): Update or remove test.
        public void NullableOptOut_26()
        {
            string source = @"
class C 
{
    void Main() {}

    void M1(CL0<string> x1) {}

    void Test1()
    {
        M1(() => null);
    }

    void Test2()
    {
        CL0<string> x2 =() => null;
    }

    [System.Runtime.CompilerServices.NullableOptOut]
    void M2(D2 x2) {}

    void Test3()
    {
        M2(() => null);
    }
}

[System.Runtime.CompilerServices.NullableOptOut]
delegate T CL0<T>(); 

delegate string D2();
";

            CSharpCompilation c = CreateStandardCompilation(new[] { NullableOptOutAttributesDefinition, source },
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                 // (10,18): warning CS8603: Possible null reference return.
                 //         M1(() => null);
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(10, 18),
                 // (15,31): warning CS8603: Possible null reference return.
                 //         CL0<string> x2 =() => null;
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(15, 31),
                 // (23,18): warning CS8603: Possible null reference return.
                 //         M2(() => null);
                 Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(23, 18)
                );
        }

        [Fact(Skip = "Variance")]
        public void Covariance_Interface()
        {
            var source =
@"interface I<out T> { }
class C
{
    static I<string?> F1(I<string> i) => i;
    static I<object?> F2(I<string> i) => i;
    static I<string> F3(I<string?> i) => i;
    static I<object> F4(I<string?> i) => i;
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,42): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //     static I<string> F3(I<string?> i) => i;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "i").WithArguments("I<string?>", "I<string>").WithLocation(6, 42),
                // (7,43): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<object>'.
                //     static I<object> F4(I<string?> i) => i;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "i").WithArguments("I<string?>", "I<object>").WithLocation(7, 43));
        }

        [Fact(Skip = "Variance")]
        public void Contravariance_Interface()
        {
            var source =
@"interface I<in T> { }
class C
{
    static I<string?> F1(I<string> i) => i;
    static I<string?> F2(I<object> i) => i;
    static I<string> F3(I<string?> i) => i;
    static I<string> F4(I<object?> i) => i;
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,42): warning CS8619: Nullability of reference types in value of type 'I<string?>' doesn't match target type 'I<string>'.
                //     static I<string> F3(I<string?> i) => i;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "i").WithArguments("I<string?>", "I<string>").WithLocation(6, 42),
                // (7,42): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<string>'.
                //     static I<string> F4(I<object?> i) => i;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "i").WithArguments("I<object?>", "I<string>").WithLocation(7, 42));
        }

        [Fact(Skip = "Variance")]
        public void Covariance_Delegate()
        {
            var source =
@"delegate void D<in T>(T t);
class C
{
    static void F1(string s) { }
    static void F2(string? s) { }
    static void F3(object o) { }
    static void F4(object? o) { }
    static void F<T>(D<T> d) { }
    static void Main()
    {
        F<string>(F1);
        F<string>(F2);
        F<string>(F3);
        F<string>(F4);
        F<string?>(F1); // warning
        F<string?>(F2);
        F<string?>(F3); // warning
        F<string?>(F4);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (15,20): warning CS8622: Nullability of reference types in type of parameter 's' of 'void C.F1(string s)' doesn't match the target delegate 'D<string?>'.
                //         F<string?>(F1); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F1").WithArguments("s", "void C.F1(string s)", "D<string?>").WithLocation(15, 20),
                // (17,20): warning CS8622: Nullability of reference types in type of parameter 'o' of 'void C.F3(object o)' doesn't match the target delegate 'D<string?>'.
                //         F<string?>(F3); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "F3").WithArguments("o", "void C.F3(object o)", "D<string?>").WithLocation(17, 20));
        }

        [Fact(Skip = "Variance")]
        public void Contravariance_Delegate()
        {
            var source =
@"delegate T D<out T>();
class C
{
    static string F1() => string.Empty;
    static string? F2() => string.Empty;
    static object F3() => string.Empty;
    static object? F4() => string.Empty;
    static T F<T>(D<T> d) => d();
    static void Main()
    {
        F<object>(F1);
        F<object>(F2); // warning
        F<object>(F3);
        F<object>(F4); // warning
        F<object?>(F1);
        F<object?>(F2);
        F<object?>(F3);
        F<object?>(F4);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (14,19): warning CS8621: Nullability of reference types in return type of 'object? C.F4()' doesn't match the target delegate 'D<object>'.
                //         F<object>(F4); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F4").WithArguments("object? C.F4()", "D<object>").WithLocation(14, 19),
                // (17,20): warning CS8621: Nullability of reference types in return type of 'object C.F3()' doesn't match the target delegate 'D<object?>'.
                //         F<object?>(F3);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "F3").WithArguments("object C.F3()", "D<object?>").WithLocation(17, 20));
        }

        [Fact]
        public void TypeArgumentInference_01()
        {
            string source = @"
class C 
{
    void Main() {}

    T M1<T>(T? x) where T: class {throw new System.NotImplementedException();}

    void Test1(string? x1)
    {
        M1(x1).ToString();
    }

    void Test2(string?[] x2)
    {
        M1(x2)[0].ToString();
    }

    void Test3(CL0<string?>? x3)
    {
        M1(x3).P1.ToString();
    }

    void Test11(string? x11)
    {
        M1<string?>(x11).ToString();
    }

    void Test12(string?[] x12)
    {
        M1<string?[]>(x12)[0].ToString();
    }

    void Test13(CL0<string?>? x13)
    {
        M1<CL0<string?>?>(x13).P1.ToString();
    }
}

class CL0<T>
{
    public T P1 {get;set;}
}
";

            CSharpCompilation c = CreateStandardCompilation(source,
                                                                parseOptions: TestOptions.Regular8,
                                                                options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         M1(x2)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1(x2)[0]").WithLocation(15, 9),
                // (20,9): warning CS8602: Possible dereference of a null reference.
                //         M1(x3).P1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1(x3).P1").WithLocation(20, 9),
                // (25,9): warning CS8602: Possible dereference of a null reference.
                //         M1<string?>(x11).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1<string?>(x11)").WithLocation(25, 9),
                // (30,9): warning CS8602: Possible dereference of a null reference.
                //         M1<string?[]>(x12)[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1<string?[]>(x12)[0]").WithLocation(30, 9),
                // (35,9): warning CS8602: Possible dereference of a null reference.
                //         M1<CL0<string?>?>(x13).P1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1<CL0<string?>?>(x13)").WithLocation(35, 9),
                // (35,9): warning CS8602: Possible dereference of a null reference.
                //         M1<CL0<string?>?>(x13).P1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M1<CL0<string?>?>(x13).P1").WithLocation(35, 9)
                );
        }

        [Fact]
        public void ExplicitImplementations_LazyMethodChecks()
        {
            var source =
@"interface I
{
    void M<T>(T? x) where T : class;
}
class C : I
{
    void I.M<T>(T? x) { }
}";
            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            var method = compilation.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");
            var implementations = method.ExplicitInterfaceImplementations;
            Assert.Equal(new[] { "void I.M<T>(T? x)" }, implementations.SelectAsArray(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void EmptyStructDifferentAssembly()
        {
            var sourceA =
@"using System.Collections;
public struct S
{
    public S(string f, IEnumerable g)
    {
        F = f;
        G = g;
    }
    private string F { get; }
    private IEnumerable G { get; }
}";
            var compA = CreateStandardCompilation(sourceA, parseOptions: TestOptions.Regular7);
            var sourceB =
@"using System.Collections.Generic;
class C
{
    static void Main()
    {
        var c = new List<object>();
        c.Add(new S(string.Empty, new object[0]));
    }
}";
            var compB = CreateStandardCompilation(
                sourceB,
                options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular8,
                references: new[] { compA.EmitToImageReference() });
            CompileAndVerify(compB, expectedOutput: "");
        }

        [Fact]
        public void EmptyStructField()
        {
            var source =
@"#pragma warning disable 8618
class A { }
struct B { }
struct S
{
    public readonly A A;
    public readonly B B;
    public S(B b) : this(null, b)
    {
    }
    public S(A a, B b)
    {
        this.A = a;
        this.B = b;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,26): warning CS8600: Cannot convert null to non-nullable reference.
                //     public S(B b) : this(null, b)
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 26));
        }

        // PROTOTYPE(NullableReferenceTypes): Update other tests with WithNullCheckingFeature(NullableReferenceFlags.None) to verify expected changes.

        [Fact]
        public void WarningOnConversion_Assignment()
        {
            var source =
@"#pragma warning disable 8618
class Person
{
    internal string FirstName { get; set; }
    internal string LastName { get; set; }
    internal string? MiddleName { get; set; }
}
class Program
{
    static void F(Person p)
    {
        p.LastName = null;
        p.LastName = (string)null;
        p.LastName = (string?)null;
        p.LastName = null as string;
        p.LastName = null as string?;
        p.LastName = default(string);
        p.LastName = default;
        p.FirstName = p.MiddleName;
        p.LastName = p.MiddleName ?? null;
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics(
                // (15,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = null as string;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null as string").WithLocation(15, 22),
                // (16,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = null as string?;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null as string?").WithLocation(16, 22),
                // (19,23): warning CS8601: Possible null reference assignment.
                //         p.FirstName = p.MiddleName;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p.MiddleName").WithLocation(19, 23),
                // (20,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = p.MiddleName ?? null;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p.MiddleName ?? null").WithLocation(20, 22));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         p.LastName = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 22),
                // (13,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         p.LastName = (string)null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string)null").WithLocation(13, 22),
                // (14,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         p.LastName = (string?)null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string?)null").WithLocation(14, 22),
                // (15,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = null as string;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null as string").WithLocation(15, 22),
                // (16,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = null as string?;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null as string?").WithLocation(16, 22),
                // (17,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         p.LastName = default(string);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(string)").WithLocation(17, 22),
                // (18,22): warning CS8600: Cannot convert null to non-nullable reference.
                //         p.LastName = default;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default").WithLocation(18, 22),
                // (19,23): warning CS8601: Possible null reference assignment.
                //         p.FirstName = p.MiddleName;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p.MiddleName").WithLocation(19, 23),
                // (20,22): warning CS8601: Possible null reference assignment.
                //         p.LastName = p.MiddleName ?? null;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p.MiddleName ?? null").WithLocation(20, 22));
        }

        [Fact]
        public void WarningOnConversion_Receiver()
        {
            var source =
@"#pragma warning disable 8618
class Person
{
    internal string FirstName { get; set; }
    internal string LastName { get; set; }
    internal string? MiddleName { get; set; }
}
class Program
{
    static void F(Person p)
    {
        ((string)null).F();
        ((string?)null).F();
        (null as string).F();
        (null as string?).F();
        default(string).F();
        ((p != null) ? p.MiddleName : null).F();
        (p.MiddleName ?? null).F();
    }
}
static class Extensions
{
    internal static void F(this string s)
    {
    }
}";

            var comp = CreateCompilationWithMscorlib45(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics(
                // (14,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (null as string).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string").WithArguments("s", "void Extensions.F(string s)").WithLocation(14, 10),
                // (15,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (null as string?).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string?").WithArguments("s", "void Extensions.F(string s)").WithLocation(15, 10),
                // (17,11): hidden CS8605: Result of the comparison is possibly always true.
                //         ((p != null) ? p.MiddleName : null).F();
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(17, 11),
                // (17,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         ((p != null) ? p.MiddleName : null).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(p != null) ? p.MiddleName : null").WithArguments("s", "void Extensions.F(string s)").WithLocation(17, 10),
                // (18,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (p.MiddleName ?? null).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "p.MiddleName ?? null").WithArguments("s", "void Extensions.F(string s)").WithLocation(18, 10));

            comp = CreateCompilationWithMscorlib45(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,10): warning CS8600: Cannot convert null to non-nullable reference.
                //         ((string)null).F();
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string)null").WithLocation(12, 10),
                // (13,10): warning CS8600: Cannot convert null to non-nullable reference.
                //         ((string?)null).F();
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string?)null").WithLocation(13, 10),
                // (14,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (null as string).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string").WithArguments("s", "void Extensions.F(string s)").WithLocation(14, 10),
                // (15,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (null as string?).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string?").WithArguments("s", "void Extensions.F(string s)").WithLocation(15, 10),
                // (16,9): warning CS8600: Cannot convert null to non-nullable reference.
                //         default(string).F();
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(string)").WithLocation(16, 9),
                // (17,11): hidden CS8605: Result of the comparison is possibly always true.
                //         ((p != null) ? p.MiddleName : null).F();
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(17, 11),
                // (17,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         ((p != null) ? p.MiddleName : null).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(p != null) ? p.MiddleName : null").WithArguments("s", "void Extensions.F(string s)").WithLocation(17, 10),
                // (18,10): warning CS8604: Possible null reference argument for parameter 's' in 'void Extensions.F(string s)'.
                //         (p.MiddleName ?? null).F();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "p.MiddleName ?? null").WithArguments("s", "void Extensions.F(string s)").WithLocation(18, 10));
        }

        [Fact]
        public void WarningOnConversion_Argument()
        {
            var source =
@"#pragma warning disable 8618
class Person
{
    internal string FirstName { get; set; }
    internal string LastName { get; set; }
    internal string? MiddleName { get; set; }
}
class Program
{
    static void F(Person p)
    {
        G(null);
        G((string)null);
        G((string?)null);
        G(null as string);
        G(null as string?);
        G(default(string));
        G(default);
        G((p != null) ? p.MiddleName : null);
        G(p.MiddleName ?? null);
    }
    static void G(string name)
    {
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics(
                // (15,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(null as string);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string").WithArguments("name", "void Program.G(string name)").WithLocation(15, 11),
                // (16,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(null as string?);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string?").WithArguments("name", "void Program.G(string name)").WithLocation(16, 11),
                // (19,12): hidden CS8605: Result of the comparison is possibly always true.
                //         G((p != null) ? p.MiddleName : null);
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(19, 12),
                // (19,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G((p != null) ? p.MiddleName : null);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(p != null) ? p.MiddleName : null").WithArguments("name", "void Program.G(string name)").WithLocation(19, 11),
                // (20,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(p.MiddleName ?? null);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "p.MiddleName ?? null").WithArguments("name", "void Program.G(string name)").WithLocation(20, 11));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,11): warning CS8600: Cannot convert null to non-nullable reference.
                //         G(null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 11),
                // (13,11): warning CS8600: Cannot convert null to non-nullable reference.
                //         G((string)null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string)null").WithLocation(13, 11),
                // (14,11): warning CS8600: Cannot convert null to non-nullable reference.
                //         G((string?)null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string?)null").WithLocation(14, 11),
                // (15,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(null as string);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string").WithArguments("name", "void Program.G(string name)").WithLocation(15, 11),
                // (16,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(null as string?);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null as string?").WithArguments("name", "void Program.G(string name)").WithLocation(16, 11),
                // (17,11): warning CS8600: Cannot convert null to non-nullable reference.
                //         G(default(string));
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(string)").WithLocation(17, 11),
                // (18,11): warning CS8600: Cannot convert null to non-nullable reference.
                //         G(default);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default").WithLocation(18, 11),
                // (19,12): hidden CS8605: Result of the comparison is possibly always true.
                //         G((p != null) ? p.MiddleName : null);
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(19, 12),
                // (19,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G((p != null) ? p.MiddleName : null);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(p != null) ? p.MiddleName : null").WithArguments("name", "void Program.G(string name)").WithLocation(19, 11),
                // (20,11): warning CS8604: Possible null reference argument for parameter 'name' in 'void Program.G(string name)'.
                //         G(p.MiddleName ?? null);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "p.MiddleName ?? null").WithArguments("name", "void Program.G(string name)").WithLocation(20, 11));
        }

        [Fact]
        public void WarningOnConversion_Return()
        {
            var source =
@"#pragma warning disable 8618
class Person
{
    internal string FirstName { get; set; }
    internal string LastName { get; set; }
    internal string? MiddleName { get; set; }
}
class Program
{
    static string F1() => null;
    static string F2() => (string)null;
    static string F3() => (string?)null;
    static string F4() => null as string;
    static string F5() => null as string?;
    static string F6() => default(string);
    static string F7() => default;
    static string F8(Person p) => (p != null) ? p.MiddleName : null;
    static string F9(Person p) => p.MiddleName ?? null;
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics(
                // (13,27): warning CS8603: Possible null reference return.
                //     static string F4() => null as string;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null as string").WithLocation(13, 27),
                // (14,27): warning CS8603: Possible null reference return.
                //     static string F5() => null as string?;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null as string?").WithLocation(14, 27),
                // (17,36): hidden CS8605: Result of the comparison is possibly always true.
                //     static string F8(Person p) => (p != null) ? p.MiddleName : null;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(17, 36),
                // (17,35): warning CS8603: Possible null reference return.
                //     static string F8(Person p) => (p != null) ? p.MiddleName : null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "(p != null) ? p.MiddleName : null").WithLocation(17, 35),
                // (18,35): warning CS8603: Possible null reference return.
                //     static string F9(Person p) => p.MiddleName ?? null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "p.MiddleName ?? null").WithLocation(18, 35));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,27): warning CS8600: Cannot convert null to non-nullable reference.
                //     static string F1() => null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 27),
                // (11,27): warning CS8600: Cannot convert null to non-nullable reference.
                //     static string F2() => (string)null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string)null").WithLocation(11, 27),
                // (12,27): warning CS8600: Cannot convert null to non-nullable reference.
                //     static string F3() => (string?)null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "(string?)null").WithLocation(12, 27),
                // (13,27): warning CS8603: Possible null reference return.
                //     static string F4() => null as string;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null as string").WithLocation(13, 27),
                // (14,27): warning CS8603: Possible null reference return.
                //     static string F5() => null as string?;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null as string?").WithLocation(14, 27),
                // (15,27): warning CS8600: Cannot convert null to non-nullable reference.
                //     static string F6() => default(string);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(string)").WithLocation(15, 27),
                // (16,27): warning CS8600: Cannot convert null to non-nullable reference.
                //     static string F7() => default;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default").WithLocation(16, 27),
                // (17,36): hidden CS8605: Result of the comparison is possibly always true.
                //     static string F8(Person p) => (p != null) ? p.MiddleName : null;
                Diagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue, "p != null").WithLocation(17, 36),
                // (17,35): warning CS8603: Possible null reference return.
                //     static string F8(Person p) => (p != null) ? p.MiddleName : null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "(p != null) ? p.MiddleName : null").WithLocation(17, 35),
                // (18,35): warning CS8603: Possible null reference return.
                //     static string F9(Person p) => p.MiddleName ?? null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "p.MiddleName ?? null").WithLocation(18, 35));
        }

        [Fact]
        public void SuppressNullableWarning()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        G(null!);
        G((null as string)!);
        G(default(string)!);
        G(default!);
        G(s!);
    }
    static void G(string s)
    {
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (5,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(null!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "null!").WithArguments("static null checking", "8.0").WithLocation(5, 11),
                // (6,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G((null as string)!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "(null as string)!").WithArguments("static null checking", "8.0").WithLocation(6, 11),
                // (7,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(default(string)!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default(string)!").WithArguments("static null checking", "8.0").WithLocation(7, 11),
                // (8,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(default!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default!").WithArguments("static null checking", "8.0").WithLocation(8, 11),
                // (8,11): error CS8107: Feature 'default literal' is not available in C# 7. Please use language version 7.1 or greater.
                //         G(default!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(8, 11),
                // (9,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(s!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "s!").WithArguments("static null checking", "8.0").WithLocation(9, 11),
                // (3,27): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     static void F(string? s)
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "s").WithArguments("System.Nullable<T>", "T", "string").WithLocation(3, 27));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SuppressNullableWarning_ReferenceType()
        {
            var source =
@"class C
{
    static C F(C? o)
    {
        C other;
        other = o!;
        o = other;
        o!.F();
        G(o!);
        return o!;
    }
    void F() { }
    static void G(C o) { }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SuppressNullableWarning_Array()
        {
            var source =
@"class C
{
    static object[] F(object?[] o)
    {
        object[] other;
        other = o!;
        o = other!;
        o!.F();
        G(o!);
        return o!;
    }
    static void G(object[] o) { }
}
static class E
{
    internal static void F(this object[] o) { }
}";
            var comp = CreateCompilationWithMscorlib45(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SuppressNullableWarning_ConstructedType()
        {
            var source =
@"class C
{
    static C<object> F(C<object?> o)
    {
        C<object> other;
        other = o!;
        o = other!;
        o!.F();
        G(o!);
        return o!;
    }
    static void G(C<object> o) { }
}
class C<T>
{
    internal void F() { }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Binder should report an error for `!!`.
        [Fact]
        public void SuppressNullableWarning_Multiple()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        G(default!!);
        G(s!!);
        G((s!)!);
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(/* ... */);
        }

        [Fact]
        public void SuppressNullableWarning_Nested()
        {
            var source =
@"class C<T> where T : class
{
    static T? F(T t) => t;
    static T? G(T t) => t;
    static void M(T? t)
    {
        F(G(t!));
        F(G(t)!);
        F(G(t!)!);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,11): warning CS8604: Possible null reference argument for parameter 't' in 'T? C<T>.F(T t)'.
                //         F(G(t!));
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "G(t!)").WithArguments("t", "T? C<T>.F(T t)").WithLocation(7, 11),
                // (8,13): warning CS8604: Possible null reference argument for parameter 't' in 'T? C<T>.G(T t)'.
                //         F(G(t)!);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "t").WithArguments("t", "T? C<T>.G(T t)").WithLocation(8, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Best type for conditional.
        [Fact(Skip = "TODO")]
        public void SuppressNullableWarning_Conditional()
        {
            var source =
@"class C<T> { }
class C
{
    static void F(C<object>? x, C<object?> y, bool c)
    {
        C<object> a;
        a = c ? x : y;
        a = c ? y : x;
        a = c ? x : y!;
        a = c ? x! : y;
        a = c ? x! : y!;
        C<object?> b;
        b = c ? x : y;
        b = c ? x : y!;
        b = c ? x! : y;
        b = c ? x! : y!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,21): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //         a = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("C<object?>", "C<object>").WithLocation(7, 21),
                // (8,21): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //         a = c ? y : x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("C<object>", "C<object?>").WithLocation(8, 21),
                // (8,13): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //         a = c ? y : x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? y : x").WithArguments("C<object?>", "C<object>").WithLocation(8, 13),
                // (13,21): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("C<object?>", "C<object>").WithLocation(13, 21),
                // (13,13): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? x : y").WithArguments("C<object>", "C<object?>").WithLocation(13, 13),
                // (14,13): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //         b = c ? x : y!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "c ? x : y!").WithArguments("C<object>", "C<object?>").WithLocation(14, 13),
                // (7,13): warning CS8601: Possible null reference assignment.
                //         a = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c ? x : y").WithLocation(7, 13),
                // (8,13): warning CS8601: Possible null reference assignment.
                //         a = c ? y : x;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c ? y : x").WithLocation(8, 13),
                // (9,13): warning CS8601: Possible null reference assignment.
                //         a = c ? x : y!;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c ? x : y!").WithLocation(9, 13),
                // (13,13): warning CS8601: Possible null reference assignment.
                //         b = c ? x : y;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c ? x : y").WithLocation(13, 13),
                // (14,13): warning CS8601: Possible null reference assignment.
                //         b = c ? x : y!;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c ? x : y!").WithLocation(14, 13));
        }

        [Fact]
        public void SuppressNullableWarning_IdentityConversion()
        {
            var source =
@"class C<T> { }
class C
{
    static void F(C<object?> x, C<object> y)
    {
        C<object> a;
        a = x;
        a = x!;
        C<object?> b;
        b = y;
        b = y!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
                //         a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("C<object?>", "C<object>").WithLocation(7, 13),
                // (10,13): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
                //         b = y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("C<object>", "C<object?>").WithLocation(10, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatch*.
        [Fact(Skip = "TODO")]
        public void SuppressNullableWarning_ImplicitConversion()
        {
            var source =
@"interface I<T> { }
class C<T> : I<T> { }
class C
{
    static void F(C<object?> x, C<object> y)
    {
        I<object> a;
        a = x;
        a = x!;
        I<object?> b;
        b = y;
        b = y!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,13): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'I<object>'.
                //         a = x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x").WithArguments("C<object?>", "I<object>").WithLocation(8, 13),
                // (11,13): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'I<object?>'.
                //         b = y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y").WithArguments("C<object>", "I<object?>").WithLocation(11, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatch*.
        [Fact(Skip = "TODO")]
        public void SuppressNullableWarning_ImplicitExtensionMethodThisConversion()
        {
            var source =
@"interface I<T> { }
class C<T> : I<T> { }
class C
{
    static void F(C<object?> x, C<object> y)
    {
        x.F1();
        x!.F1();
        y.F2();
        y!.F2();
    }
}
static class E
{
    internal static void F1(this I<object> o) { }
    internal static void F2(this I<object?> o) { }
}";
            var comp = CreateCompilationWithMscorlib45(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         x.F1();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x.F1").WithArguments("I<object?>", "I<object>").WithLocation(7, 9),
                // (9,9): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         y.F2();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "y.F2").WithArguments("I<object>", "I<object?>").WithLocation(9, 9));
        }

        [Fact]
        public void SuppressNullableWarning_ImplicitUserDefinedConversion()
        {
            var source =
@"class A<T> { }
class B<T>
{
    public static implicit operator A<T>(B<T> b) => new A<T>();
}
class C
{
    static void F(B<object?> b1, B<object> b2)
    {
        A<object> a1;
        a1 = b1;
        a1 = b1!;
        A<object?> a2;
        a2 = b2;
        a2 = b2!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,14): warning CS8619: Nullability of reference types in value of type 'A<object?>' doesn't match target type 'A<object>'.
                //         a1 = b1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b1").WithArguments("A<object?>", "A<object>").WithLocation(11, 14),
                // (14,14): warning CS8619: Nullability of reference types in value of type 'A<object>' doesn't match target type 'A<object?>'.
                //         a2 = b2;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b2").WithArguments("A<object>", "A<object?>").WithLocation(14, 14));
        }

        [Fact]
        public void SuppressNullableWarning_ExplicitConversion()
        {
            var source =
@"interface I<T> { }
class C<T> { }
class C
{
    static void F(C<object?> x, C<object> y)
    {
        I<object> a;
        a = (I<object?>)x;
        a = ((I<object?>)x)!;
        I<object?> b;
        b = (I<object>)y;
        b = ((I<object>)y)!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,13): warning CS8619: Nullability of reference types in value of type 'I<object?>' doesn't match target type 'I<object>'.
                //         a = (I<object?>)x;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(I<object?>)x").WithArguments("I<object?>", "I<object>").WithLocation(8, 13),
                // (11,13): warning CS8619: Nullability of reference types in value of type 'I<object>' doesn't match target type 'I<object?>'.
                //         b = (I<object>)y;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(I<object>)y").WithArguments("I<object>", "I<object?>").WithLocation(11, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatch*.
        [Fact(Skip = "TODO")]
        public void SuppressNullableWarning_Ref()
        {
            var source =
@"class C
{
    static void F(ref string s, ref string? t)
    {
    }
    static void Main()
    {
        string? s = null;
        string t = string.Empty;
        F(ref s, ref t);
        F(ref s!, ref t!);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (10,15): warning CS8620: Nullability of reference types in argument of type 'string' doesn't match target type 'string' for parameter 's' in 'void C.F(ref string s, ref string? t)'.
                //         F(ref s, ref t);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s").WithArguments("string", "string", "s", "void C.F(ref string s, ref string? t)").WithLocation(10, 15),
                // (10,22): warning CS8620: Nullability of reference types in argument of type 'string' doesn't match target type 'string' for parameter 't' in 'void C.F(ref string s, ref string? t)'.
                //         F(ref s, ref t);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "t").WithArguments("string", "string", "t", "void C.F(ref string s, ref string? t)").WithLocation(10, 22),
                // (10,15): warning CS8604: Possible null reference argument for parameter 's' in 'void C.F(ref string s, ref string? t)'.
                //         F(ref s, ref t);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.F(ref string s, ref string? t)").WithLocation(10, 15),
                // (10,22): warning CS8601: Possible null reference assignment.
                //         F(ref s, ref t);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "t").WithLocation(10, 22));
        }

        // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatch*.
        [Fact(Skip = "TODO")]
        public void SuppressNullableWarning_Out()
        {
            var source =
@"class C
{
    static void F(out string s, out string? t)
    {
        s = string.Empty;
        t = string.Empty;
    }
    static void Main()
    {
        string? s;
        string t;
        F(out s, out t);
        F(out s!, out t!);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8620: Nullability of reference types in argument of type 'string' doesn't match target type 'string' for parameter 's' in 'void C.F(out string s, out string? t)'.
                //         F(out s, out t);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s").WithArguments("string", "string", "s", "void C.F(out string s, out string? t)").WithLocation(12, 15),
                // (12,22): warning CS8620: Nullability of reference types in argument of type 'string' doesn't match target type 'string' for parameter 't' in 'void C.F(out string s, out string? t)'.
                //         F(out s, out t);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "t").WithArguments("string", "string", "t", "void C.F(out string s, out string? t)").WithLocation(12, 22),
                // (12,22): warning CS8601: Possible null reference assignment.
                //         F(out s, out t);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "t").WithLocation(12, 22));
        }

        // PROTOTYPE(NullableReferenceTypes): 't! = s' should be an error.
        [Fact]
        public void SuppressNullableWarning_Assignment()
        {
            var source =
@"class C
{
    static void Main()
    {
        string? s = null;
        string t = string.Empty;
        t! = s;
        t! += s;
        (t!) = s;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(/* ... */);
        }

        [Fact]
        public void SuppressNullableWarning_Conversion()
        {
            var source =
@"class A
{
    public static implicit operator B(A a) => new B();
}
class B
{
}
class C
{
    static void F(A? a)
    {
        G((B)a);
        G((B)a!);
    }
    static void G(B b)
    {
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,14): warning CS8604: Possible null reference argument for parameter 'a' in 'A.implicit operator B(A a)'.
                //         G((B)a);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("a", "A.implicit operator B(A a)").WithLocation(12, 14));
        }

        // PROTOTYPE(NullableReferenceTypes): PreciseAbstractFlowPass.VisitSuppressNullableWarningExpression
        // should not assume node.Expression is an rvalue.
        [Fact(Skip = "CS0165: Use of unassigned local variable 'o'")]
        public void SuppressNullableWarning_Condition()
        {
            var source =
@"class C
{
    static object? F(bool b)
    {
        return (b && G(out var o))!? o : null;
    }
    static bool G(out object o)
    {
        o = new object();
        return true;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,16): error CS8624: The ! operator can only be applied to reference types.
                //         return (b && G(out var o))!? o : null;
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "(b && G(out var o))!").WithLocation(5, 16));
        }

        [Fact]
        public void SuppressNullableWarning_ValueType_01()
        {
            var source =
@"struct S
{
    static void F()
    {
        G(1!);
        G(((int?)null)!);
        G(default(S)!);
    }
    static void G(object o)
    {
    }
    static void G<T>(T? t) where T : struct
    {
    }
}";

            // Feature enabled.
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(1!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "1!").WithLocation(5, 11),
                // (6,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(((int?)null)!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "((int?)null)!").WithLocation(6, 11),
                // (7,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(default(S)!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "default(S)!").WithLocation(7, 11));

            // Feature disabled.
            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (5,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(1!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1!").WithArguments("static null checking", "8.0").WithLocation(5, 11),
                // (6,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(((int?)null)!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "((int?)null)!").WithArguments("static null checking", "8.0").WithLocation(6, 11),
                // (7,11): error CS8107: Feature 'static null checking' is not available in C# 7. Please use language version 8.0 or greater.
                //         G(default(S)!);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default(S)!").WithArguments("static null checking", "8.0").WithLocation(7, 11),
                // (5,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(1!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "1!").WithLocation(5, 11),
                // (6,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(((int?)null)!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "((int?)null)!").WithLocation(6, 11),
                // (7,11): error CS8624: The ! operator can only be applied to reference types.
                //         G(default(S)!);
                Diagnostic(ErrorCode.ERR_NotNullableOperatorNotReferenceType, "default(S)!").WithLocation(7, 11));
        }

        [Fact]
        public void SuppressNullableWarning_ValueType_02()
        {
            var source =
@"struct S<T> where T : class
{
    static S<object> F(S<object?> s) => s!;
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Report error for `!` with value type for F2 and F3.
        [Fact(Skip = "Untyped value type expression")]
        public void SuppressNullableWarning_Constraint()
        {
            var source =
@"class C
{
    static T F1<T>() where T : class => default!;
    static T F2<T>() where T : struct => default!;
    static T F3<T>() => default!;
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // ...
                );
        }

        [Fact]
        public void SuppressNullableWarning_NonNullOperand()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        G(""""!);
        G((new string('a', 1))!);
        G((s ?? """")!);
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SuppressNullableWarning_InvalidOperand()
        {
            var source =
@"class C
{
    static void F(C c)
    {
        G(F!);
        G(c.P!);
    }
    static void G(object o)
    {
    }
    object P { set { } }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,11): error CS1503: Argument 1: cannot convert from 'method group!' to 'object'
                //         G(F!);
                Diagnostic(ErrorCode.ERR_BadArgType, "F!").WithArguments("1", "method group!", "object").WithLocation(5, 11),
                // (6,11): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                //         G(c.P!);
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "c.P").WithArguments("C.P").WithLocation(6, 11));
        }

        [Fact]
        public void SuppressNullableWarning_IndexedProperty()
        {
            var source0 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Public ReadOnly Property P(i As Integer) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property Q(Optional i As Integer = 0) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var ref0 = BasicCompilationUtils.CompileToMetadata(source0);
            var source =
@"class B
{
    static object F(A a, int i)
    {
        if (i > 0)
        {
            return a.P!;
        }
        return a.Q!;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,20): error CS0856: Indexed property 'A.P' has non-optional arguments which must be provided
                //             return a.P!;
                Diagnostic(ErrorCode.ERR_IndexedPropertyRequiresParams, "a.P").WithArguments("A.P").WithLocation(7, 20));
        }

        [Fact]
        public void LocalTypeInference()
        {
            var source =
@"class C
{
    static void F(string? s, string? t)
    {
        if (s != null)
        {
            var x = s;
            G(x); // no warning
            x = t;
        }
        else
        {
            var y = s;
            G(y); // warning
            y = t;
        }
    }
    static void G(string s)
    {
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,17): warning CS8601: Possible null reference assignment.
                //             x = t;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "t").WithLocation(9, 17),
                // (14,15): warning CS8604: Possible null reference argument for parameter 's' in 'void C.G(string s)'.
                //             G(y); // warning
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y").WithArguments("s", "void C.G(string s)").WithLocation(14, 15));
        }

        [Fact]
        public void AssignmentInCondition_01()
        {
            var source =
@"class C
{
    object P => null;
    static void F(object o)
    {
        C? c;
        while ((c = o as C) != null)
        {
            o = c.P;
        }
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssignmentInCondition_02()
        {
            var source =
@"class C
{
    object? P => null;
    static void F(object? o)
    {
        C? c;
        while ((c = o as C) != null)
        {
            o = c.P;
        }
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StructAsNullableInterface()
        {
            var source =
@"interface I
{
    void F();
}
struct S : I
{
    void I.F()
    {
    }
}
class C
{
    static void F(I? i)
    {
        i.F();
    }
    static void Main()
    {
        F(new S());
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowNullAsNonNull));
            comp.VerifyDiagnostics(
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         i.F();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "i").WithLocation(15, 9));
        }

        [Fact]
        public void IsNull()
        {
            var source =
@"class C
{
    static void F1(object o) { }
    static void F2(object o) { }
    static void G(object? o)
    {
        if (o is null)
        {
            F1(o);
        }
        else
        {
            F2(o);
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,16): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F1(object o)'.
                //             F1(o);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o").WithArguments("o", "void C.F1(object o)").WithLocation(9, 16));
        }

        [Fact]
        public void IsInvalidConstant()
        {
            var source =
@"class C
{
    static void F(object o) { }
    static void G(object? o)
    {
        if (o is F)
        {
            F(o);
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,18): error CS0428: Cannot convert method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //         if (o is F)
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 18),
                // (8,15): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F(object o)'.
                //             F(o);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o").WithArguments("o", "void C.F(object o)").WithLocation(8, 15));
        }

        // PROTOTYPE(NullableReferenceTypes): Should not warn on either call to F(string).
        [Fact(Skip = "TODO")]
        public void IsPattern()
        {
            var source =
@"class C
{
    static void F(string s) { }
    static void G(string? s)
    {
        if (s is string t)
        {
            F(t);
            F(s);
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Should only warn on F(x) in `case null`.
        [Fact(Skip = "TODO")]
        public void PatternSwitch()
        {
            var source =
@"class C
{
    static void F(object o) { }
    static void G(object? x)
    {
        switch (x)
        {
            case string s:
                F(s);
                F(x); // string s
                break;
            case object y when y is string t:
                F(y);
                F(t);
                F(x); // object y
                break;
            case null:
                F(x); // null
                break;
            default:
                F(x); // default
                break;
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (18,19): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F(object o)'.
                //                 F(x); // null
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("o", "void C.F(object o)").WithLocation(18, 19));
        }

        [Fact]
        public void GetNullableReferenceFlags()
        {
            // C# 7
            Assert.Equal(NullableReferenceFlags.None,
                TestOptions.Regular7.GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.None,
                TestOptions.Regular7.WithFeature("staticNullChecking").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.None,
                TestOptions.Regular7.WithFeature("staticNullChecking", "3").GetNullableReferenceFlags());

            // C# 8
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.WithFeature("staticNullChecking").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.WithFeature("staticNullChecking", "0").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled | NullableReferenceFlags.AllowNullAsNonNull | NullableReferenceFlags.InferLocalNullability,
                TestOptions.Regular8.WithFeature("staticNullChecking", "3").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled | NullableReferenceFlags.AllowMemberOptOut | NullableReferenceFlags.AllowAssemblyOptOut,
                TestOptions.Regular8.WithFeature("staticNullChecking", "12").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled | (NullableReferenceFlags)0x123,
                TestOptions.Regular8.WithFeature("staticNullChecking", 0x123.ToString()).GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.WithFeature("staticNullChecking", "false").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.WithFeature("staticNullChecking", "true").GetNullableReferenceFlags());
            Assert.Equal(NullableReferenceFlags.Enabled,
                TestOptions.Regular8.WithFeature("staticNullChecking", "other").GetNullableReferenceFlags());
        }

        [Fact]
        public void Feature()
        {
            var source =
@"class C
{
    static object F() => null;
    static object F(object? o) => o;
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithFeature("staticNullChecking"));
            comp.VerifyDiagnostics(
                // (3,26): warning CS8600: Cannot convert null to non-nullable reference.
                //     static object F() => null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 26),
                // (4,35): warning CS8603: Possible null reference return.
                //     static object F(object? o) => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(4, 35));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithFeature("staticNullChecking", "0"));
            comp.VerifyDiagnostics(
                // (3,26): warning CS8600: Cannot convert null to non-nullable reference.
                //     static object F() => null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 26),
                // (4,35): warning CS8603: Possible null reference return.
                //     static object F(object? o) => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(4, 35));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithFeature("staticNullChecking", "1"));
            comp.VerifyDiagnostics(
                // (4,35): warning CS8603: Possible null reference return.
                //     static object F(object? o) => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(4, 35));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithFeature("staticNullChecking", "2"));
            comp.VerifyDiagnostics(
                // (3,26): warning CS8600: Cannot convert null to non-nullable reference.
                //     static object F() => null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 26),
                // (4,35): warning CS8603: Possible null reference return.
                //     static object F(object? o) => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(4, 35));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithFeature("staticNullChecking", "3"));
            comp.VerifyDiagnostics(
                // (4,35): warning CS8603: Possible null reference return.
                //     static object F(object? o) => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(4, 35));
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOut] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForDefinition.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void AllowMemberOptOut()
        {
            var source =
@"class C
{
    [System.Runtime.CompilerServices.NullableOptOut]
    static void F(object o) { }
    static void G(object o) { }
    static void M(object? o)
    {
        F(o);
        G(o);
    }
}";

            var comp = CreateStandardCompilation(
                new[] { source, NullableOptOutAttributesDefinition },
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowMemberOptOut));
            comp.VerifyDiagnostics(
                // (9,11): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.G(object o)'.
                //         G(o);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o").WithArguments("o", "void C.G(object o)").WithLocation(9, 11));

            comp = CreateStandardCompilation(
                new[] { source, NullableOptOutAttributesDefinition },
                parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should warn that [NullableOptOut] is ignored.
            comp.VerifyDiagnostics(
                // (8,11): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.F(object o)'.
                //         F(o);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o").WithArguments("o", "void C.F(object o)").WithLocation(8, 11),
                // (9,11): warning CS8604: Possible null reference argument for parameter 'o' in 'void C.G(object o)'.
                //         G(o);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o").WithArguments("o", "void C.G(object o)").WithLocation(9, 11));
        }

        // PROTOTYPE(NullableReferenceTypes): [NullableOptOutForAssembly] is disabled.
        // See CSharpCompilation.HaveNullableOptOutForAssembly.
        [Fact(Skip = "[NullableOptOut] is disabled")]
        public void AllowAssemblyOptOut()
        {
            var source0 =
@"public class A
{
    public static object? F(object o) => o;
}";
            var source1 =
@"[module: System.Runtime.CompilerServices.NullableOptOutForAssembly(""A.dll"")]
class B
{
    static object G(object? x) => A.F(x);
}";

            var comp0 = CreateStandardCompilation(
                new[] { source0, NullableOptOutAttributesDefinition },
                parseOptions: TestOptions.Regular8,
                assemblyName: "A.dll");
            comp0.VerifyDiagnostics();
            var ref0 = comp0.EmitToImageReference();

            var comp1 = CreateStandardCompilation(
                new[] { source1, NullableOptOutAttributesDefinition },
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.AllowAssemblyOptOut),
                references: new[] { ref0 });
            comp1.VerifyDiagnostics();

            comp1 = CreateStandardCompilation(
                new[] { source1, NullableOptOutAttributesDefinition },
                parseOptions: TestOptions.Regular8,
                references: new[] { ref0 });
            // PROTOTYPE(NullableReferenceTypes): Should warn that [NullableOptOutForAssembly] is ignored.
            comp1.VerifyDiagnostics(
                // (4,39): warning CS8604: Possible null reference argument for parameter 'o' in 'object? A.F(object o)'.
                //     static object G(object? x) => A.F(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("o", "object? A.F(object o)").WithLocation(4, 39),
                // (4,35): warning CS8603: Possible null reference return.
                //     static object G(object? x) => A.F(x);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "A.F(x)").WithLocation(4, 35));
        }

        [Fact]
        public void InferLocalNullability()
        {
            var source =
@"class C
{
    static string? F(string s) => s;
    static void G(string s)
    {
        string x;
        x = F(s);
        F(x);
        string? y = s;
        y = F(y);
        F(y);
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8601: Possible null reference assignment.
                //         x = F(s);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "F(s)").WithLocation(7, 13),
                // (11,11): warning CS8604: Possible null reference argument for parameter 's' in 'string? C.F(string s)'.
                //         F(y);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y").WithArguments("s", "string? C.F(string s)").WithLocation(11, 11));

            comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8.WithNullCheckingFeature(NullableReferenceFlags.InferLocalNullability));
            comp.VerifyDiagnostics(
                // (8,11): warning CS8604: Possible null reference argument for parameter 's' in 'string? C.F(string s)'.
                //         F(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("s", "string? C.F(string s)").WithLocation(8, 11),
                // (11,11): warning CS8604: Possible null reference argument for parameter 's' in 'string? C.F(string s)'.
                //         F(y);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y").WithArguments("s", "string? C.F(string s)").WithLocation(11, 11));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void InferLocalType_UsedInDeclaration()
        {
            var source =
@"using System;
using System.Collections.Generic;
class C
{
    static T F<T>(IEnumerable<T> e)
    {
        throw new NotImplementedException();
    }
    static void G()
    {
        var a = new[] { F(a) };
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,27): error CS0841: Cannot use local variable 'a' before it is declared
                //         var a = new[] { F(a) };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "a").WithArguments("a").WithLocation(11, 27),
                // (11,27): error CS0165: Use of unassigned local variable 'a'
                //         var a = new[] { F(a) };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(11, 27));
        }

        [Fact]
        public void InferLocalType_UsedInDeclaration_Script()
        {
            var source =
@"using System;
using System.Collections.Generic;
static T F<T>(IEnumerable<T> e)
{
    throw new NotImplementedException();
}
var a = new[] { F(a) };";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp8));
            comp.VerifyDiagnostics(
                // (7,5): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var a = new[] { F(a) };
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a").WithLocation(7, 5));
        }

        [Fact]
        public void InferLocalType_UsedBeforeDeclaration()
        {
            var source =
@"using System;
using System.Collections.Generic;
class C
{
    static T F<T>(IEnumerable<T> e)
    {
        throw new NotImplementedException();
    }
    static void G()
    {
        var a = new[] { F(b) };
        var b = a;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,27): error CS0841: Cannot use local variable 'b' before it is declared
                //         var a = new[] { F(b) };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b").WithLocation(11, 27));
        }

        [Fact]
        public void InferLocalType_OutVarError()
        {
            var source =
@"using System;
using System.Collections.Generic;
class C
{
    static T F<T>(IEnumerable<T> e)
    {
        throw new NotImplementedException();
    }
    static void G()
    {
        dynamic d = null!;
        d.F(out var v);
        F(v).ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,21): error CS8197: Cannot infer the type of implicitly-typed out variable 'v'.
                //         d.F(out var v);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, "v").WithArguments("v").WithLocation(12, 21));
        }

        [Fact]
        public void InferLocalType_OutVarError_Script()
        {
            var source =
@"using System;
using System.Collections.Generic;
static T F<T>(IEnumerable<T> e)
{
    throw new NotImplementedException();
}
dynamic d = null!;
d.F(out var v);
F(v).ToString();";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp8));
            comp.VerifyDiagnostics(
                // (8,13): error CS8197: Cannot infer the type of implicitly-typed out variable 'v'.
                // d.F(out var v);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, "v").WithArguments("v").WithLocation(8, 13));
        }

        /// <summary>
        /// Default value for non-nullable parameter
        /// should not result in a warning at the call site.
        /// </summary>
        [Fact]
        public void NullDefaultValueFromSource()
        {
            var source =
@"class C
{
    public static void F(object o = null)
    {
    }
}
class Program
{
    static void Main()
    {
        C.F();
        C.F(null);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         C.F(null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 13));
        }

        /// <summary>
        /// Default value for non-nullable parameter
        /// should not result in a warning at the call site.
        /// </summary>
        [Fact]
        public void NullDefaultValueFromMetadata()
        {
            var source0 =
@"public class C
{
    public static void F(object o = null)
    {
    }
}";
            var comp0 = CreateStandardCompilation(
                source0,
                parseOptions: TestOptions.Regular8);
            comp0.VerifyDiagnostics();
            var ref0 = comp0.EmitToImageReference();

            var source1 =
@"class Program
{
    static void Main()
    {
        C.F();
        C.F(null);
    }
}";
            var comp1 = CreateStandardCompilation(
                source1,
                parseOptions: TestOptions.Regular8,
                references: new[] { ref0 });
            comp1.VerifyDiagnostics(
                // (6,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         C.F(null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 13));
        }

        [Fact]
        public void InvalidThrowTerm()
        {
            var source =
@"class C
{
    static string F(string s) => s + throw new System.Exception();
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,38): error CS1525: Invalid expression term 'throw'
                //     static string F(string s) => s + throw new System.Exception();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "throw new System.Exception()").WithArguments("throw").WithLocation(3, 38));
        }

        [Fact]
        public void UnboxingConversion()
        {
            var source =
@"using System.Collections.Generic;
class Program
{
    static IEnumerator<T> M<T>() => default(T);
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,37): error CS0266: Cannot implicitly convert type 'T' to 'System.Collections.Generic.IEnumerator<T>'. An explicit conversion exists (are you missing a cast?)
                //     static IEnumerator<T> M<T>() => default(T);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "default(T)").WithArguments("T", "System.Collections.Generic.IEnumerator<T>").WithLocation(4, 37));
        }

        [Fact]
        public void DeconstructionConversion_NoDeconstructMethod()
        {
            var source =
@"class C
{
    static void F(C c)
    {
        var (x, y) = c;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,22): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         var (x, y) = c;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "c").WithArguments("C", "Deconstruct").WithLocation(5, 22),
                // (5,22): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         var (x, y) = c;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "c").WithArguments("C", "2").WithLocation(5, 22),
                // (5,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                //         var (x, y) = c;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(5, 14),
                // (5,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                //         var (x, y) = c;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(5, 17));
        }

        // PROTOTYPE(NullableReferenceTypes): Error is reported on `type 'T'` rather than `type 'Func<T>'`.
        [Fact(Skip = "TODO")]
        public void ConditionalAccessDelegateInvoke()
        {
            var source =
@"using System;
class C<T>
{
    static T F(Func<T>? f)
    {
        return f?.Invoke();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,17): error CS0023: Operator '?' cannot be applied to operand of type 'Func<T>'
                //         return f?.Invoke();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "Func<T>").WithLocation(6, 17));
        }

        [Fact]
        public void NullableOptOut_DecodeAttributeCycle_01()
        {
            var source =
@"using System.Runtime.InteropServices;
interface I
{
    int P { get; }
}
[StructLayout(LayoutKind.Auto)]
struct S : I
{
    int I.P => 0;
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableOptOut_DecodeAttributeCycle_02()
        {
            var source =
@"[A(P)]
class A : System.Attribute
{
    string P => null;
}";
            var comp = CreateStandardCompilation(
                 source,
                 parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,4): error CS0120: An object reference is required for the non-static field, method, or property 'A.P'
                // [A(P)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P").WithArguments("A.P").WithLocation(1, 4),
                // (1,2): error CS1729: 'A' does not contain a constructor that takes 1 arguments
                // [A(P)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "A(P)").WithArguments("A", "1").WithLocation(1, 2),
                // (4,17): warning CS8600: Cannot convert null to non-nullable reference.
                //     string P => null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 17));
        }

        // PROTOTYPE(NullableReferenceTypes): Should not report warning for
        // c.ToString(); // 3
        [Fact(Skip = "TODO")]
        public void UnassignedOutParameterClass()
        {
            var source =
@"class C
{
    static void G(out C? c)
    {
        c.ToString(); // 1
        c = null;
        c.ToString(); // 2
        c = new C();
        c.ToString(); // 3
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,9): error CS0269: Use of unassigned out parameter 'c'
                //         c.ToString(); // 1
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "c").WithArguments("c").WithLocation(5, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         c.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(7, 9));
        }

        [Fact]
        public void UnassignedOutParameterClassField()
        {
            var source =
@"class C
{
#pragma warning disable 0649
    object? F;
    static void G(out C c)
    {
        object o = c.F;
        c.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,20): error CS0269: Use of unassigned out parameter 'c'
                //         object o = c.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "c").WithArguments("c").WithLocation(7, 20),
                // (5,17): error CS0177: The out parameter 'c' must be assigned to before control leaves the current method
                //     static void G(out C c)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "G").WithArguments("c").WithLocation(5, 17),
                // (7,20): warning CS8601: Possible null reference assignment.
                //         object o = c.F;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "c.F").WithLocation(7, 20),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         c.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c.F").WithLocation(8, 9));
        }

        [Fact]
        public void UnassignedOutParameterStructField()
        {
            var source =
@"struct S
{
#pragma warning disable 0649
    object? F;
    static void G(out S s)
    {
        object o = s.F;
        s.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,20): error CS0170: Use of possibly unassigned field 'F'
                //         object o = s.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.F").WithArguments("F").WithLocation(7, 20),
                // (5,17): error CS0177: The out parameter 's' must be assigned to before control leaves the current method
                //     static void G(out S s)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "G").WithArguments("s").WithLocation(5, 17));
        }

        [Fact]
        public void UnassignedLocalField()
        {
            var source =
@"class C
{
    static void F()
    {
        S s;
        C c;
        c = s.F;
        s.F.ToString();
    }
}
struct S
{
    internal C? F;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): error CS0170: Use of possibly unassigned field 'F'
                //         c = s.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.F").WithArguments("F").WithLocation(7, 13),
                // (13,17): warning CS0649: Field 'S.F' is never assigned to, and will always have its default value null
                //     internal C? F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("S.F", "null").WithLocation(13, 17));
        }

        [Fact]
        public void UnassignedLocalField_Conditional()
        {
            var source =
@"class C
{
    static void F(bool b)
    {
        S s;
        object o;
        if (b)
        {
            s.F = new object();
            s.G = new object();
        }
        else
        {
            o = s.F;
        }
        o = s.G;
    }
}
struct S
{
    internal object? F;
    internal object? G;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (14,17): error CS0170: Use of possibly unassigned field 'F'
                //             o = s.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.F").WithArguments("F").WithLocation(14, 17),
                // (16,13): error CS0170: Use of possibly unassigned field 'G'
                //         o = s.G;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.G").WithArguments("G").WithLocation(16, 13));
        }

        [Fact]
        public void UnassignedLocalProperty()
        {
            var source =
@"class C
{
    static void F()
    {
        S s;
        C c;
        c = s.P;
        s.P.ToString();
    }
}
struct S
{
    internal C? P { get => null; }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8601: Possible null reference assignment.
                //         c = s.P;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "s.P").WithLocation(7, 13),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         s.P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.P").WithLocation(8, 9));
        }

        [Fact]
        public void UnassignedClassAutoProperty()
        {
            var source =
@"class C
{
    object? P { get; }
    void M(out object o)
    {
        o = P;
        P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): warning CS8601: Possible null reference assignment.
                //         o = P;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P").WithLocation(6, 13),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "P").WithLocation(7, 9));
        }

        [Fact]
        public void UnassignedClassAutoProperty_Constructor()
        {
            var source =
@"class C
{
    object? P { get; }
    C(out object o)
    {
        o = P;
        P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): warning CS8601: Possible null reference assignment.
                //         o = P;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P").WithLocation(6, 13),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "P").WithLocation(7, 9));
        }

        [Fact]
        public void UnassignedStructAutoProperty()
        {
            var source =
@"struct S
{
    object? P { get; }
    void M(out object o)
    {
        o = P;
        P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): warning CS8601: Possible null reference assignment.
                //         o = P;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P").WithLocation(6, 13),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         P.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "P").WithLocation(7, 9));
        }

        [Fact]
        public void UnassignedStructAutoProperty_Constructor()
        {
            var source =
@"struct S
{
    object? P { get; }
    S(out object o)
    {
        o = P;
        P.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): error CS8079: Use of possibly unassigned auto-implemented property 'P'
                //         o = P;
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "P").WithArguments("P").WithLocation(6, 13),
                // (4,5): error CS0843: Auto-implemented property 'S.P' must be fully assigned before control is returned to the caller.
                //     S(out object o)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S").WithArguments("S.P").WithLocation(4, 5));
        }

        [Fact]
        public void ParameterField_Class()
        {
            var source =
@"class C
{
#pragma warning disable 0649
    object? F;
    static void M(C x)
    {
        C y = x;
        object z = y.F;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,20): warning CS8601: Possible null reference assignment.
                //         object z = y.F;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y.F").WithLocation(8, 20));
        }

        [Fact]
        public void ParameterField_Struct()
        {
            var source =
@"struct S
{
#pragma warning disable 0649
    object? F;
    static void M(S x)
    {
        S y = x;
        object z = y.F;
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,20): warning CS8601: Possible null reference assignment.
                //         object z = y.F;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y.F").WithLocation(8, 20));
        }

        [Fact]
        public void InstanceFieldStructTypeExpressionReceiver()
        {
            var source =
@"struct S
{
#pragma warning disable 0649
    object? F;
    void M()
    {
        S.F.ToString();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): error CS0120: An object reference is required for the non-static field, method, or property 'S.F'
                //         S.F.ToString();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "S.F").WithArguments("S.F").WithLocation(7, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         S.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "S.F").WithLocation(7, 9));
        }

        [Fact]
        public void InstanceFieldPrimitiveRecursiveStruct()
        {
            var source =
@"#pragma warning disable 0649
namespace System
{
    public class Object
    {
        public int GetHashCode() => 0;
    }
    public abstract class ValueType { }
    public struct Void { }
    public struct Int32
    {
        Int32 _value;
        object? _f;
        void M()
        {
            _value = _f.GetHashCode();
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (16,22): warning CS8602: Possible dereference of a null reference.
                //             _value = _f.GetHashCode();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_f").WithLocation(16, 22));
        }
    }
}
