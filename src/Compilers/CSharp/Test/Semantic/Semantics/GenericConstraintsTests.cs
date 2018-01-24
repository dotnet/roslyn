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
    }
}
