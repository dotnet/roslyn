// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class GenericConstraintsTests : CompilingTestBase
    {
        [Fact]
        public void EnumConstraint_Compilation_Alone()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.Enum
{
}
public enum E1
{
    A
}
public class Test2
{
    public void M<U>() where U : System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}").VerifyDiagnostics(
                // (14,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(14, 26),
                // (15,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(15, 26));
        }

        [Fact]
        public void EnumConstraint_Compilation_ReferenceType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : class, System.Enum
{
}
public enum E1
{
    A
}
public class Test2
{
    public void M<U>() where U : class, System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}").VerifyDiagnostics(
                // (13,26): error CS0452: The type 'E1' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<E1>();             // enum
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "E1").WithArguments("Test<T>", "T", "E1").WithLocation(13, 26),
                // (14,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(14, 26),
                // (15,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(15, 26));
        }

        [Fact]
        public void EnumConstraint_Compilation_ValueType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : struct, System.Enum
{
}
public enum E1
{
    A
}
public class Test2
{
    public void M<U>() where U : struct, System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}").VerifyDiagnostics(
                // (14,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(14, 26),
                // (15,26): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(15, 26),
                // (16,26): error CS0453: The type 'Enum' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var d = new Test<System.Enum>();    // Enum type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "System.Enum").WithArguments("Test<T>", "T", "System.Enum").WithLocation(16, 26));
        }

        [Fact]
        public void EnumConstraint_Compilation_Constructor()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.Enum, new()
{
}
public enum E1
{
    A
}
public class Test2
{
    public void M<U>() where U : System.Enum, new()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}").VerifyDiagnostics(
                // (14,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(14, 26),
                // (15,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(15, 26),
                // (15,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(15, 26),
                // (16,26): error CS0310: 'Enum' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var d = new Test<System.Enum>();    // Enum type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "System.Enum").WithArguments("Test<T>", "T", "System.Enum").WithLocation(16, 26));
        }

        [Fact]
        public void EnumConstraint_Reference_Alone()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.Enum
{
}"
                ).EmitToImageReference();

            var code = @"
public enum E1
{
    A
}

public class Test2
{
    public void M<U>() where U : System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}";

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (12,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(12, 26),
                // (13,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(13, 26));
        }

        [Fact]
        public void EnumConstraint_Reference_ReferenceType()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : class, System.Enum
{
}"
                ).EmitToImageReference();

            var code = @"
public enum E1
{
    A
}

public class Test2
{
    public void M<U>() where U : class, System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}";

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (11,26): error CS0452: The type 'E1' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<E1>();             // enum
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "E1").WithArguments("Test<T>", "T", "E1").WithLocation(11, 26),
                // (12,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(12, 26),
                // (13,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(13, 26));
        }

        [Fact]
        public void EnumConstraint_Reference_ValueType()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : struct, System.Enum
{
}"
                ).EmitToImageReference();

            var code = @"
public enum E1
{
    A
}

public class Test2
{
    public void M<U>() where U : struct, System.Enum
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}";

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (12,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(12, 26),
                // (13,26): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(13, 26),
                // (14,26): error CS0453: The type 'Enum' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var d = new Test<System.Enum>();    // Enum type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "System.Enum").WithArguments("Test<T>", "T", "System.Enum").WithLocation(14, 26));
        }

        [Fact]
        public void EnumConstraint_Reference_Constructor()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.Enum, new()
{
}"
                ).EmitToImageReference();

            var code = @"
public enum E1
{
    A
}

public class Test2
{
    public void M<U>() where U : System.Enum, new()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
        var e = new Test<U>();              // Generic type constrained to enum
    }
}";

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (12,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(12, 26),
                // (13,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Enum", "T", "string").WithLocation(13, 26),
                // (13,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(13, 26),
                // (14,26): error CS0310: 'Enum' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var d = new Test<System.Enum>();    // Enum type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "System.Enum").WithArguments("Test<T>", "T", "System.Enum").WithLocation(14, 26));
        }

        [Fact]
        public void EnumConstraint_Before_7_3()
        {
            var code = @"
public class Test<T> where T : System.Enum
{
}";

            CreateStandardCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'enum generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.Enum
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.Enum").WithArguments("enum generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Theory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        public void EnumConstraint_DifferentBaseTypes(string type)
        {
            CreateStandardCompilation($@"
public class Test<T> where T : System.Enum
{{
}}
public enum E1 : {type}
{{
    A
}}
public class Test2
{{
    public void M()
    {{
        var a = new Test<E1>();     // Valid
        var b = new Test<int>();    // Invalid
    }}
}}
").VerifyDiagnostics(
                // (14,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var b = new Test<int>();    // Invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(14, 26));
        }

        [Fact]
        public void EnumConstraint_InheritanceChain()
        {
            CreateStandardCompilation(@"
public enum E
{
    A
}
public class Test<T, U> where U : System.Enum, T
{
}
public class Test2
{
    public void M()
    {
        var a = new Test<Test2, E>();

        var b = new Test<E, E>();
        var c = new Test<System.Enum, System.Enum>();

        var d = new Test<E, System.Enum>();
        var e = new Test<System.Enum, E>();
    }
}").VerifyDiagnostics(
                // (13,33): error CS0315: The type 'E' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no boxing conversion from 'E' to 'Test2'.
                //         var a = new Test<Test2, E>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "E").WithArguments("Test<T, U>", "Test2", "U", "E").WithLocation(13, 33),
                // (18,29): error CS0311: The type 'System.Enum' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.Enum' to 'E'.
                //         var d = new Test<E, System.Enum>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.Enum").WithArguments("Test<T, U>", "E", "U", "System.Enum").WithLocation(18, 29));
        }

        [Fact]
        public void EnumConstraint_IsReflectedinSymbols_Alone()
        {
            var code = "public class Test<T> where T : System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedinSymbols_ValueType()
        {
            var code = "public class Test<T> where T : struct, System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedinSymbols_ReferenceType()
        {
            var code = "public class Test<T> where T : class, System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedinSymbols_Constructor()
        {
            var code = "public class Test<T> where T : System.Enum, new() { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.True(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Downwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Enum;
}
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<E>();
    }
}
public enum E
{
}").VerifyDiagnostics(
                // (12,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.Enum", "T", "int").WithLocation(12, 9));
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Enum;
}").EmitToImageReference();

            CreateStandardCompilation(@"
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<E>();
    }
}
public enum E
{
}", references: new[] { reference }).VerifyDiagnostics(
                // (8,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.Enum", "T", "int").WithLocation(8, 9));
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}
public class B : A
{
    public override void M<T>() where T : System.Enum { }
}").VerifyDiagnostics(
                // (8,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.Enum { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(8, 33));
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Upwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateStandardCompilation(@"
public class B : A
{
    public override void M<T>() where T : System.Enum { }
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.Enum { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(4, 33));
        }

        [Fact]
        public void EnumConstraint_ResolveParentConstraints()
        {
            var comp = CreateStandardCompilation(@"
public enum MyEnum
{
}
public abstract class A<T>
{
    public abstract void F<U>() where U : System.Enum, T;
}
public class B : A<MyEnum>
{
    public override void F<U>() { }
}");

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.GlobalNamespace.GetTypeMember("B").GetMethod("F");
                var constraintTypeNames = method.TypeParameters.Single().ConstraintTypes().Select(type => type.ToTestDisplayString());

                AssertEx.SetEqual(new[] { "System.Enum", "MyEnum" }, constraintTypeNames);
            };

            CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_TypeNotAvailable()
        {
            CreateCompilation(@"
namespace System
{
    public class Object {}
    public class Void {}
}
public class Test<T> where T : System.Enum
{
}").VerifyDiagnostics(
                // (7,39): error CS0234: The type or namespace name 'Enum' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // public class Test<T> where T : System.Enum
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Enum").WithArguments("Enum", "System").WithLocation(7, 39));
        }

        [Fact]
        public void EnumConstraint_BindingToMethods()
        {
            var code = @"
enum A : short { a }
enum B : uint { b }
class Test
{
    public static void Main()
    {
        Print(A.a);
        Print(B.b);
    }
    static void Print<T>(T obj) where T : System.Enum
    {
        System.Console.WriteLine(obj.GetTypeCode());
    }
}";

            CompileAndVerify(code, expectedOutput: @"
Int16
UInt32");
        }

        [Fact]
        public void DelegateConstraint_Compilation_Alone()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.Delegate
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.Delegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}").VerifyDiagnostics(
                // (11,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Delegate", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void DelegateConstraint_Compilation_ReferenceType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : class, System.Delegate
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : class, System.Delegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}").VerifyDiagnostics(
                // (11,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void DelegateConstraint_Compilation_ValueType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : struct, System.Delegate
{
}").VerifyDiagnostics(
                // (2,19): error CS0455: Type parameter 'T' inherits conflicting constraints 'Delegate' and 'ValueType'
                // public class Test<T> where T : struct, System.Delegate
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "T").WithArguments("T", "System.Delegate", "System.ValueType").WithLocation(2, 19));
        }

        [Fact]
        public void DelegateConstraint_Compilation_Constructor()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.Delegate, new()
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.Delegate, new()
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}").VerifyDiagnostics(
                // (10,26): error CS0310: 'D1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<D1>();             // delegate
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "D1").WithArguments("Test<T>", "T", "D1").WithLocation(10, 26),
                // (11,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Delegate", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(12, 26),
                // (12,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void DelegateConstraint_Reference_Alone()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.Delegate
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.Delegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (8,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Delegate", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(9, 26));
        }

        [Fact]
        public void DelegateConstraint_Reference_ReferenceType()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : class, System.Delegate
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : class, System.Delegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (8,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(9, 26));
        }
        
        [Fact]
        public void DelegateConstraint_Reference_Constructor()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.Delegate, new()
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.Delegate, new()
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,26): error CS0310: 'D1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<D1>();             // delegate
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "D1").WithArguments("Test<T>", "T", "D1").WithLocation(7, 26),
                // (8,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Delegate", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.Delegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.Delegate", "T", "string").WithLocation(9, 26),
                // (9,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(9, 26));
        }

        [Fact]
        public void DelegateConstraint_Before_7_3()
        {
            var code = @"
public class Test<T> where T : System.Delegate
{
}";

            CreateStandardCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.Delegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.Delegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Fact]
        public void DelegateConstraint_InheritanceChain()
        {
            CreateStandardCompilation(@"
public delegate void D1();
public class Test<T, U> where U : System.Delegate, T
{
}
public class Test2
{
    public void M()
    {
        var a = new Test<Test2, D1>();
        
        var b = new Test<D1, D1>();
        var c = new Test<System.Delegate, System.Delegate>();
        var d = new Test<System.MulticastDelegate, System.Delegate>();
        var e = new Test<System.Delegate, System.MulticastDelegate>();
        var f = new Test<System.MulticastDelegate, System.MulticastDelegate>();

        var g = new Test<D1, System.Delegate>();
        var h = new Test<System.Delegate, D1>();
    }
}").VerifyDiagnostics(
                // (10,33): error CS0311: The type 'D1' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'D1' to 'Test2'.
                //         var a = new Test<Test2, D1>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "D1").WithArguments("Test<T, U>", "Test2", "U", "D1").WithLocation(10, 33),
                // (14,52): error CS0311: The type 'System.Delegate' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.Delegate' to 'System.MulticastDelegate'.
                //         var d = new Test<System.MulticastDelegate, System.Delegate>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.Delegate").WithArguments("Test<T, U>", "System.MulticastDelegate", "U", "System.Delegate").WithLocation(14, 52),
                // (18,30): error CS0311: The type 'System.Delegate' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.Delegate' to 'D1'.
                //         var g = new Test<D1, System.Delegate>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.Delegate").WithArguments("Test<T, U>", "D1", "U", "System.Delegate").WithLocation(18, 30));
        }

        [Fact]
        public void DelegateConstraint_IsReflectedinSymbols_Alone()
        {
            var code = "public class Test<T> where T : System.Delegate { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.Equal(SpecialType.System_Delegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void DelegateConstraint_IsReflectedinSymbols_ValueType()
        {
            var compilation = CreateStandardCompilation("public class Test<T> where T : struct, System.Delegate { }")
                    .VerifyDiagnostics(
                // (1,19): error CS0455: Type parameter 'T' inherits conflicting constraints 'Delegate' and 'ValueType'
                // public class Test<T> where T : struct, System.Delegate { }
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "T").WithArguments("T", "System.Delegate", "System.ValueType").WithLocation(1, 19));

            var typeParameter = compilation.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Equal(SpecialType.System_Delegate, typeParameter.ConstraintTypes().Single().SpecialType);
        }

        [Fact]
        public void DelegateConstraint_IsReflectedinSymbols_ReferenceType()
        {
            var code = "public class Test<T> where T : class, System.Delegate { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Delegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void DelegateConstraint_IsReflectedinSymbols_Constructor()
        {
            var code = "public class Test<T> where T : System.Delegate, new() { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.True(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Delegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Downwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Delegate;
}
public delegate void D1();
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<D1>();
    }
}").VerifyDiagnostics(
                // (13,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.Delegate", "T", "int").WithLocation(13, 9));
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Delegate;
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<D1>();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (9,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.Delegate", "T", "int").WithLocation(9, 9));
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}
public class B : A
{
    public override void M<T>() where T : System.Delegate { }
}").VerifyDiagnostics(
                // (8,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(8, 33));
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Upwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateStandardCompilation(@"
public class B : A
{
    public override void M<T>() where T : System.Delegate { }
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(4, 33));
        }

        [Fact]
        public void DelegateConstraint_ResolveParentConstraints()
        {
            var comp = CreateStandardCompilation(@"
public delegate void D1();
public abstract class A<T>
{
    public abstract void F<U>() where U : System.Delegate, T;
}
public class B : A<D1>
{
    public override void F<U>() { }
}");

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.GlobalNamespace.GetTypeMember("B").GetMethod("F");
                var constraintTypeNames = method.TypeParameters.Single().ConstraintTypes().Select(type => type.ToTestDisplayString());

                AssertEx.SetEqual(new[] { "System.Delegate", "D1" }, constraintTypeNames);
            };

            CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void DelegateConstraint_TypeNotAvailable()
        {
            CreateCompilation(@"
namespace System
{
    public class Object {}
    public class Void {}
}
public class Test<T> where T : System.Delegate
{
}").VerifyDiagnostics(
                // (7,39): error CS0234: The type or namespace name 'Delegate' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // public class Test<T> where T : System.Delegate
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Delegate").WithArguments("Delegate", "System").WithLocation(7, 39));
        }

        [Fact]
        public void DelegateConstraint_BindingToMethods()
        {
            var code = @"
delegate void D1(int a, int b);
class TestClass
{
    public static void Impl(int a, int b)
    {
        System.Console.WriteLine($""Got {a} and {b}"");
    }
    public static void Main()
    {
        Test<D1>(Impl);
    }
    public static void Test<T>(T obj) where T : System.Delegate
    {
        obj.DynamicInvoke(2, 3);
        obj.DynamicInvoke(7, 9);
    }
}";

            CompileAndVerify(code, expectedOutput: @"
Got 2 and 3
Got 7 and 9");
        }

        [Fact]
        public void MulticastDelegateConstraint_Compilation_Alone()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.MulticastDelegate
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.MulticastDelegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}").VerifyDiagnostics(
                // (11,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.MulticastDelegate", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Compilation_ReferenceType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : class, System.MulticastDelegate
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : class, System.MulticastDelegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}").VerifyDiagnostics(
                // (11,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Compilation_ValueType()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : struct, System.MulticastDelegate
{
}").VerifyDiagnostics(
                // (2,19): error CS0455: Type parameter 'T' inherits conflicting constraints 'MulticastDelegate' and 'ValueType'
                // public class Test<T> where T : struct, System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "T").WithArguments("T", "System.MulticastDelegate", "System.ValueType").WithLocation(2, 19));
        }

        [Fact]
        public void MulticastDelegateConstraint_Compilation_Constructor()
        {
            CreateStandardCompilation(@"
public class Test<T> where T : System.MulticastDelegate, new()
{
}
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.MulticastDelegate, new()
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}").VerifyDiagnostics(
                // (10,26): error CS0310: 'D1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<D1>();             // delegate
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "D1").WithArguments("Test<T>", "T", "D1").WithLocation(10, 26),
                // (11,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.MulticastDelegate", "T", "int").WithLocation(11, 26),
                // (12,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(12, 26),
                // (12,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(12, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Reference_Alone()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.MulticastDelegate
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.MulticastDelegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (8,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.MulticastDelegate", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(9, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Reference_ReferenceType()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : class, System.MulticastDelegate
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : class, System.MulticastDelegate
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (8,26): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("Test<T>", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(9, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Reference_Constructor()
        {
            var reference = CreateStandardCompilation(@"
public class Test<T> where T : System.MulticastDelegate, new()
{
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class Test2
{
    public void M<U>() where U : System.MulticastDelegate, new()
    {
        var a = new Test<D1>();             // delegate
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<U>();              // multicast delegate type
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,26): error CS0310: 'D1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var a = new Test<D1>();             // delegate
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "D1").WithArguments("Test<T>", "T", "D1").WithLocation(7, 26),
                // (8,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         var b = new Test<int>();            // value type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.MulticastDelegate", "T", "int").WithLocation(8, 26),
                // (9,26): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'string' to 'System.MulticastDelegate'.
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("Test<T>", "System.MulticastDelegate", "T", "string").WithLocation(9, 26),
                // (9,26): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();         // reference type
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(9, 26));
        }

        [Fact]
        public void MulticastDelegateConstraint_Before_7_3()
        {
            var code = @"
public class Test<T> where T : System.MulticastDelegate
{
}";

            CreateStandardCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.MulticastDelegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Fact]
        public void MulticastDelegateConstraint_InheritanceChain()
        {
            CreateStandardCompilation(@"
public delegate void D1();
public class Test<T, U> where U : System.MulticastDelegate, T
{
}
public class Test2
{
    public void M()
    {
        var a = new Test<Test2, D1>();
        
        var b = new Test<D1, D1>();
        var c = new Test<System.MulticastDelegate, System.MulticastDelegate>();
        var d = new Test<System.Delegate, System.MulticastDelegate>();
        var e = new Test<System.MulticastDelegate, System.Delegate>();
        var f = new Test<System.Delegate, System.Delegate>();

        var g = new Test<D1, System.MulticastDelegate>();
        var h = new Test<System.MulticastDelegate, D1>();
    }
}").VerifyDiagnostics(
                // (10,33): error CS0311: The type 'D1' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'D1' to 'Test2'.
                //         var a = new Test<Test2, D1>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "D1").WithArguments("Test<T, U>", "Test2", "U", "D1").WithLocation(10, 33),
                // (15,52): error CS0311: The type 'System.Delegate' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.Delegate' to 'System.MulticastDelegate'.
                //         var e = new Test<System.MulticastDelegate, System.Delegate>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.Delegate").WithArguments("Test<T, U>", "System.MulticastDelegate", "U", "System.Delegate").WithLocation(15, 52),
                // (16,43): error CS0311: The type 'System.Delegate' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.Delegate' to 'System.MulticastDelegate'.
                //         var f = new Test<System.Delegate, System.Delegate>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.Delegate").WithArguments("Test<T, U>", "System.MulticastDelegate", "U", "System.Delegate").WithLocation(16, 43),
                // (18,30): error CS0311: The type 'System.MulticastDelegate' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no implicit reference conversion from 'System.MulticastDelegate' to 'D1'.
                //         var g = new Test<D1, System.MulticastDelegate>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "System.MulticastDelegate").WithArguments("Test<T, U>", "D1", "U", "System.MulticastDelegate").WithLocation(18, 30));
        }

        [Fact]
        public void MulticastDelegateConstraint_IsReflectedinSymbols_Alone()
        {
            var code = "public class Test<T> where T : System.MulticastDelegate { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.Equal(SpecialType.System_MulticastDelegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void MulticastDelegateConstraint_IsReflectedinSymbols_ValueType()
        {
            var compilation = CreateStandardCompilation("public class Test<T> where T : struct, System.MulticastDelegate { }")
                    .VerifyDiagnostics(
                // (1,19): error CS0455: Type parameter 'T' inherits conflicting constraints 'MulticastDelegate' and 'ValueType'
                // public class Test<T> where T : struct, System.MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "T").WithArguments("T", "System.MulticastDelegate", "System.ValueType").WithLocation(1, 19));

            var typeParameter = compilation.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Equal(SpecialType.System_MulticastDelegate, typeParameter.ConstraintTypes().Single().SpecialType);
        }

        [Fact]
        public void MulticastDelegateConstraint_IsReflectedinSymbols_ReferenceType()
        {
            var code = "public class Test<T> where T : class, System.MulticastDelegate { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_MulticastDelegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void MulticastDelegateConstraint_IsReflectedinSymbols_Constructor()
        {
            var code = "public class Test<T> where T : System.MulticastDelegate, new() { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.True(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_MulticastDelegate, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Downwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.MulticastDelegate;
}
public delegate void D1();
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<D1>();
    }
}").VerifyDiagnostics(
                // (13,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.MulticastDelegate", "T", "int").WithLocation(13, 9));
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.MulticastDelegate;
}").EmitToImageReference();

            CreateStandardCompilation(@"
public delegate void D1();
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<D1>();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (9,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "this.M<int>").WithArguments("B.M<T>()", "System.MulticastDelegate", "T", "int").WithLocation(9, 9));
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}
public class B : A
{
    public override void M<T>() where T : System.MulticastDelegate { }
}").VerifyDiagnostics(
                // (8,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(8, 33));
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Upwards_Reference()
        {
            var reference = CreateStandardCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateStandardCompilation(@"
public class B : A
{
    public override void M<T>() where T : System.MulticastDelegate { }
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : System.MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(4, 33));
        }

        [Fact]
        public void MulticastDelegateConstraint_ResolveParentConstraints()
        {
            var comp = CreateStandardCompilation(@"
public delegate void D1();
public abstract class A<T>
{
    public abstract void F<U>() where U : System.MulticastDelegate, T;
}
public class B : A<D1>
{
    public override void F<U>() { }
}");

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.GlobalNamespace.GetTypeMember("B").GetMethod("F");
                var constraintTypeNames = method.TypeParameters.Single().ConstraintTypes().Select(type => type.ToTestDisplayString());

                AssertEx.SetEqual(new[] { "System.MulticastDelegate", "D1" }, constraintTypeNames);
            };

            CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void MulticastDelegateConstraint_TypeNotAvailable()
        {
            CreateCompilation(@"
namespace System
{
    public class Object {}
    public class Void {}
}
public class Test<T> where T : System.MulticastDelegate
{
}").VerifyDiagnostics(
                // (7,39): error CS0234: The type or namespace name 'MulticastDelegate' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // public class Test<T> where T : System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "MulticastDelegate").WithArguments("MulticastDelegate", "System").WithLocation(7, 39));
        }

        [Fact]
        public void MulticastDelegateConstraint_BindingToMethods()
        {
            var code = @"
delegate void D1(int a, int b);
class TestClass
{
    public static void Impl(int a, int b)
    {
        System.Console.WriteLine($""Got {a} and {b}"");
    }
    public static void Main()
    {
        Test<D1>(Impl);
    }
    public static void Test<T>(T obj) where T : System.MulticastDelegate
    {
        obj.DynamicInvoke(2, 3);
        obj.DynamicInvoke(7, 9);
    }
}";

            CompileAndVerify(code, expectedOutput: @"
Got 2 and 3
Got 7 and 9");
        }

        [Fact]
        public void ConversionInInheritanceChain_MulticastDelegate()
        {
            var code = @"
class A<T> where T : System.Delegate { }
class B<T> : A<T> where T : System.MulticastDelegate { }";

            CreateStandardCompilation(code).VerifyDiagnostics();

            code = @"
class A<T> where T : System.MulticastDelegate { }
class B<T> : A<T> where T : System.Delegate { }";

            CreateStandardCompilation(code).VerifyDiagnostics(
                // (3,7): error CS0311: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. There is no implicit reference conversion from 'T' to 'System.MulticastDelegate'.
                // class B<T> : A<T> where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "B").WithArguments("A<T>", "System.MulticastDelegate", "T", "T").WithLocation(3, 7));
        }
    }
}
