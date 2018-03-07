// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
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

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
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
            var reference = CreateCompilation(@"
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

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
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
            var reference = CreateCompilation(@"
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

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
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
            var reference = CreateCompilation(@"
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

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
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

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
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
            CreateCompilation($@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
                // (12,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.Enum", "T", "int").WithLocation(12, 14)
                );
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Enum;
}").EmitToImageReference();

            CreateCompilation(@"
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
                // (8,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.Enum", "T", "int").WithLocation(8, 14)
                );
        }

        [Fact]
        public void EnumConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateCompilation(@"
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
            var comp = CreateCompilation(@"
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
            CreateEmptyCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : System.Delegate
{
}").EmitToImageReference();

            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : class, System.Delegate
{
}").EmitToImageReference();

            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : System.Delegate, new()
{
}").EmitToImageReference();

            CreateCompilation(@"
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

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.Delegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.Delegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Fact]
        public void DelegateConstraint_InheritanceChain()
        {
            CreateCompilation(@"
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
            var compilation = CreateCompilation("public class Test<T> where T : struct, System.Delegate { }")
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
            CreateCompilation(@"
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
                // (13,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.Delegate", "T", "int").WithLocation(13, 14)
                );
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.Delegate;
}").EmitToImageReference();

            CreateCompilation(@"
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
                // (9,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.Delegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.Delegate", "T", "int").WithLocation(9, 14)
                );
        }

        [Fact]
        public void DelegateConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateCompilation(@"
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
            var comp = CreateCompilation(@"
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
            CreateEmptyCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : System.MulticastDelegate
{
}").EmitToImageReference();

            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : class, System.MulticastDelegate
{
}").EmitToImageReference();

            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public class Test<T> where T : System.MulticastDelegate, new()
{
}").EmitToImageReference();

            CreateCompilation(@"
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

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.MulticastDelegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Fact]
        public void MulticastDelegateConstraint_InheritanceChain()
        {
            CreateCompilation(@"
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
            var compilation = CreateCompilation("public class Test<T> where T : struct, System.MulticastDelegate { }")
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
            CreateCompilation(@"
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
                // (13,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.MulticastDelegate", "T", "int").WithLocation(13, 14)
                );
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : System.MulticastDelegate;
}").EmitToImageReference();

            CreateCompilation(@"
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
                // (9,14): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. There is no boxing conversion from 'int' to 'System.MulticastDelegate'.
                //         this.M<int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int>").WithArguments("B.M<T>()", "System.MulticastDelegate", "T", "int").WithLocation(9, 14)
                );
        }

        [Fact]
        public void MulticastDelegateConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateCompilation(@"
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
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateCompilation(@"
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
            var comp = CreateCompilation(@"
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
            CreateEmptyCompilation(@"
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

            CreateCompilation(code).VerifyDiagnostics();

            code = @"
class A<T> where T : System.MulticastDelegate { }
class B<T> : A<T> where T : System.Delegate { }";

            CreateCompilation(code).VerifyDiagnostics(
                // (3,7): error CS0311: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. There is no implicit reference conversion from 'T' to 'System.MulticastDelegate'.
                // class B<T> : A<T> where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "B").WithArguments("A<T>", "System.MulticastDelegate", "T", "T").WithLocation(3, 7));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Alone_Type()
        {
            CreateCompilation(@"
public class Test<T> where T : unmanaged
{
}
public struct GoodType { public int I; }
public struct BadType { public string S; }
public class Test2
{
    public void M<U, W>() where U : unmanaged
    {
        var a = new Test<GoodType>();           // unmanaged struct
        var b = new Test<BadType>();            // managed struct
        var c = new Test<string>();             // reference type
        var d = new Test<int>();                // value type
        var e = new Test<U>();                  // generic type constrained to unmanaged
        var f = new Test<W>();                  // unconstrained generic type
    }
}").VerifyDiagnostics(
                // (12,26): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "BadType").WithArguments("Test<T>", "T", "BadType").WithLocation(12, 26),
                // (13,26): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(13, 26),
                // (16,26): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var f = new Test<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "W").WithArguments("Test<T>", "T", "W").WithLocation(16, 26));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Alone_Method()
        {
            CreateCompilation(@"
public class Test
{
    public int M<T>() where T : unmanaged => 0;
}
public struct GoodType { public int I; }
public struct BadType { public string S; }
public class Test2
{
    public void M<U, W>() where U : unmanaged
    {
        var a = new Test().M<GoodType>();           // unmanaged struct
        var b = new Test().M<BadType>();            // managed struct
        var c = new Test().M<string>();             // reference type
        var d = new Test().M<int>();                // value type
        var e = new Test().M<U>();                  // generic type constrained to unmanaged
        var f = new Test().M<W>();                  // unconstrained generic type
    }
}").VerifyDiagnostics(
                // (13,28): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var b = new Test().M<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<BadType>").WithArguments("Test.M<T>()", "T", "BadType").WithLocation(13, 28),
                // (14,28): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var c = new Test().M<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("Test.M<T>()", "T", "string").WithLocation(14, 28),
                // (17,28): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var f = new Test().M<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<W>").WithArguments("Test.M<T>()", "T", "W").WithLocation(17, 28)
                );
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Alone_Delegate()
        {
            CreateCompilation(@"
public delegate void D<T>() where T : unmanaged;
public struct GoodType { public int I; }
public struct BadType { public string S; }
public abstract class Test2<U, W> where U : unmanaged
{
    public abstract D<GoodType> a();                // unmanaged struct
    public abstract D<BadType> b();                 // managed struct
    public abstract D<string> c();                  // reference type
    public abstract D<int> d();                     // value type
    public abstract D<U> e();                       // generic type constrained to unmanaged
    public abstract D<W> f();                       // unconstrained generic type
}").VerifyDiagnostics(
                // (8,32): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<BadType> b();                 // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "b").WithArguments("D<T>", "T", "BadType").WithLocation(8, 32),
                // (9,31): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<string> c();                  // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "c").WithArguments("D<T>", "T", "string").WithLocation(9, 31),
                // (12,26): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<W> f();                       // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "f").WithArguments("D<T>", "T", "W").WithLocation(12, 26));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Alone_LocalFunction()
        {
            CreateCompilation(@"
public abstract class Test2<U, W> where U : unmanaged
{
    public void M()
    {
        void local<T>() where T : unmanaged { }

        local<int>();
    }
}").VerifyDiagnostics(
                // (6,20): error CS8376: Using unmanaged constraint on local functions type parameters is not supported.
                //         void local<T>() where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintWithLocalFunctions, "T").WithLocation(6, 20));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_ReferenceType()
        {
            CreateCompilation("public class Test<T> where T : class, unmanaged {}").VerifyDiagnostics(
                // (1,39): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T> where T : class, unmanaged {}
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "unmanaged").WithLocation(1, 39));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_ValueType()
        {
            CreateCompilation("public class Test<T> where T : struct, unmanaged {}").VerifyDiagnostics(
                // (1,40): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T> where T : struct, unmanaged {}
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "unmanaged").WithLocation(1, 40));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Constructor()
        {
            CreateCompilation("public class Test<T> where T : unmanaged, new() {}").VerifyDiagnostics(
                // (1,43): error CS8373: The 'new()' constraint cannot be used with the 'unmanaged' constraint
                // public class Test<T> where T : unmanaged, new() {}
                Diagnostic(ErrorCode.ERR_NewBoundWithUnmanaged, "new").WithLocation(1, 43));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherType_Before()
        {
            CreateCompilation("public class Test<T> where T : unmanaged, System.Exception { }").VerifyDiagnostics(
                // (1,43): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T> where T : unmanaged, System.Exception { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "System.Exception").WithLocation(1, 43));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherType_After()
        {
            CreateCompilation("public class Test<T> where T : System.Exception, unmanaged { }").VerifyDiagnostics(
                // (1,50): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T> where T : System.Exception, unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "unmanaged").WithLocation(1, 50));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherParameter_After()
        {
            CreateCompilation("public class Test<T, U> where T : U, unmanaged { }").VerifyDiagnostics(
                // (1,38): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T, U> where T : U, unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "unmanaged").WithLocation(1, 38));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherParameter_Before()
        {
            CreateCompilation("public class Test<T, U> where T : unmanaged, U { }").VerifyDiagnostics(
                // (1,46): error CS8374: The 'unmanaged' constraint cannot be specified with other constraints.
                // public class Test<T, U> where T : unmanaged, U { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeAlone, "U").WithLocation(1, 46));
        }

        [Fact]
        public void UnmanagedConstraint_UnmanagedEnumNotAvailable()
        {
            CreateEmptyCompilation(@"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
}
public class Test<T> where T : unmanaged
{
}").VerifyDiagnostics(
                // (8,32): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                // public class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(8, 32));
        }

        [Fact]
        public void UnmanagedConstraint_ValueTypeNotAvailable()
        {
            CreateEmptyCompilation(@"
namespace System
{
    public class Object {}
    public class Void {}
    public class Enum {}
    public class Int32 {}
    namespace Runtime
    {
        namespace InteropServices
        {
            public enum UnmanagedType {}
        }
    }
}
public class Test<T> where T : unmanaged
{
}").VerifyDiagnostics(
                // (16,32): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                // public class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.ValueType").WithLocation(16, 32));
        }

        [Fact]
        public void UnmanagedConstraint_Reference_Alone_Type()
        {
            var reference = CreateCompilation(@"
public class Test<T> where T : unmanaged
{
}").EmitToImageReference();

            var code = @"
public struct GoodType { public int I; }
public struct BadType { public string S; }
public class Test2
{
    public void M<U, W>() where U : unmanaged
    {
        var a = new Test<GoodType>();           // unmanaged struct
        var b = new Test<BadType>();            // managed struct
        var c = new Test<string>();             // reference type
        var d = new Test<int>();                // value type
        var e = new Test<U>();                  // generic type constrained to unmanaged
        var f = new Test<W>();                  // unconstrained generic type
    }
}";
            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,26): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "BadType").WithArguments("Test<T>", "T", "BadType").WithLocation(9, 26),
                // (10,26): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(10, 26),
                // (13,26): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var f = new Test<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "W").WithArguments("Test<T>", "T", "W").WithLocation(13, 26));
        }

        [Fact]
        public void UnmanagedConstraint_Reference_Alone_Method()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public int M<T>() where T : unmanaged => 0;
}").EmitToImageReference();

            var code = @"
public struct GoodType { public int I; }
public struct BadType { public string S; }
public class Test2
{
    public void M<U, W>() where U : unmanaged
    {
        var a = new Test().M<GoodType>();           // unmanaged struct
        var b = new Test().M<BadType>();            // managed struct
        var c = new Test().M<string>();             // reference type
        var d = new Test().M<int>();                // value type
        var e = new Test().M<U>();                  // generic type constrained to unmanaged
        var f = new Test().M<W>();                  // unconstrained generic type
    }
}";
            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,28): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var b = new Test().M<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<BadType>").WithArguments("Test.M<T>()", "T", "BadType").WithLocation(9, 28),
                // (10,28): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var c = new Test().M<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("Test.M<T>()", "T", "string").WithLocation(10, 28),
                // (13,28): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var f = new Test().M<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<W>").WithArguments("Test.M<T>()", "T", "W").WithLocation(13, 28)
                );
        }

        [Fact]
        public void UnmanagedConstraint_Reference_Alone_Delegate()
        {
            var reference = CreateCompilation(@"
public delegate void D<T>() where T : unmanaged;
").EmitToImageReference();

            var code = @"
public struct GoodType { public int I; }
public struct BadType { public string S; }
public abstract class Test2<U, W> where U : unmanaged
{
    public abstract D<GoodType> a();                // unmanaged struct
    public abstract D<BadType> b();                 // managed struct
    public abstract D<string> c();                  // reference type
    public abstract D<int> d();                     // value type
    public abstract D<U> e();                       // generic type constrained to unmanaged
    public abstract D<W> f();                       // unconstrained generic type
}";
            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,32): error CS8375: The type 'BadType' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<BadType> b();                 // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "b").WithArguments("D<T>", "T", "BadType").WithLocation(7, 32),
                // (8,31): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<string> c();                  // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "c").WithArguments("D<T>", "T", "string").WithLocation(8, 31),
                // (11,26): error CS8375: The type 'W' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<W> f();                       // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "f").WithArguments("D<T>", "T", "W").WithLocation(11, 26));
        }

        [Fact]
        public void UnmanagedConstraint_Before_7_3()
        {
            var code = @"
public class Test<T> where T : unmanaged
{
}";

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'unmanaged generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "unmanaged").WithArguments("unmanaged generic type constraints", "7.3").WithLocation(2, 32));
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedinSymbols_Alone_Type()
        {
            var code = "public class Test<T> where T : unmanaged { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedinSymbols_Alone_Method()
        {
            var code = @"
public class Test
{
    public void M<T>() where T : unmanaged {}
}";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedinSymbols_Alone_Delegate()
        {
            var code = "public delegate void D<T>() where T : unmanaged;";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_EnforcedInInheritanceChain_Downwards_Source()
        {
            CreateCompilation(@"
struct Test
{
    public string RefMember { get; set; }
}
public abstract class A
{
    public abstract void M<T>() where T : unmanaged;
}
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<string>();
        this.M<Test>();
    }
}").VerifyDiagnostics(
                // (17,14): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<string>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("B.M<T>()", "T", "string").WithLocation(17, 14),
                // (18,14): error CS8375: The type 'Test' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<Test>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<Test>").WithArguments("B.M<T>()", "T", "Test").WithLocation(18, 14)
                );
        }

        [Fact]
        public void UnmanagedConstraint_EnforcedInInheritanceChain_Downwards_Reference()
        {
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>() where T : unmanaged;
}").EmitToImageReference();

            CreateCompilation(@"
struct Test
{
    public string RefMember { get; set; }
}
public class B : A
{
    public override void M<T>() { }

    public void Test()
    {
        this.M<int>();
        this.M<string>();
        this.M<Test>();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (13,14): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<string>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("B.M<T>()", "T", "string").WithLocation(13, 14),
                // (14,14): error CS8375: The type 'Test' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<Test>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<Test>").WithArguments("B.M<T>()", "T", "Test").WithLocation(14, 14)
                );
        }

        [Fact]
        public void UnmanagedConstraint_EnforcedInInheritanceChain_Upwards_Source()
        {
            CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}
public class B : A
{
    public override void M<T>() where T : unmanaged { }
}").VerifyDiagnostics(
                // (8,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(8, 33));
        }

        [Fact]
        public void UnmanagedConstraint_EnforcedInInheritanceChain_Upwards_Reference()
        {
            var reference = CreateCompilation(@"
public abstract class A
{
    public abstract void M<T>();
}").EmitToImageReference();

            CreateCompilation(@"
public class B : A
{
    public override void M<T>() where T : unmanaged { }
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,33): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                //     public override void M<T>() where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(4, 33));
        }

        [Fact]
        public void UnmanagedConstraints_PointerOperations_Invalid()
        {
            CreateCompilation(@"
class Test
{
    void M<T>(T arg) where T : unmanaged
    {
    }
    void N()
    {
        M(""test"");
    }
}").VerifyDiagnostics(
                // (9,9): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>(T)'
                //         M("test");
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("Test.M<T>(T)", "T", "string").WithLocation(9, 9));
        }

        [Theory]
        [InlineData("(sbyte)1", "System.SByte", 1)]
        [InlineData("(byte)1", "System.Byte", 1)]
        [InlineData("(short)1", "System.Int16", 2)]
        [InlineData("(ushort)1", "System.UInt16", 2)]
        [InlineData("(int)1", "System.Int32", 4)]
        [InlineData("(uint)1", "System.UInt32", 4)]
        [InlineData("(long)1", "System.Int64", 8)]
        [InlineData("(ulong)1", "System.UInt64", 8)]
        [InlineData("'a'", "System.Char", 2)]
        [InlineData("(float)1", "System.Single", 4)]
        [InlineData("(double)1", "System.Double", 8)]
        [InlineData("(decimal)1", "System.Decimal", 16)]
        [InlineData("false", "System.Boolean", 1)]
        [InlineData("E.A", "E", 4)]
        [InlineData("new S { a = 1, b = 2, c = 3 }", "S", 12)]
        public void UnmanagedConstraints_PointerOperations_SimpleTypes(string arg, string type, int size)
        {
            CompileAndVerify(@"
enum E
{
    A
}
struct S
{
    public int a;
    public int b;
    public int c;
}
unsafe class Test
{
    static T* M<T>(T arg) where T : unmanaged
    {
        T* ptr = &arg;
        System.Console.WriteLine(ptr->GetType());   // method access
        System.Console.WriteLine(sizeof(T));        // sizeof operator
    
        T* ar = stackalloc T [10];
        return ar;
    }
    static void Main()
    {
        M(" + arg + @");
    }
}",
    options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: string.Join(Environment.NewLine, type, size)).VerifyIL("Test.M<T>", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  conv.u
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""System.Type object.GetType()""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  sizeof     ""T""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.s   10
  IL_0020:  conv.u
  IL_0021:  sizeof     ""T""
  IL_0027:  mul.ovf.un
  IL_0028:  localloc
  IL_002a:  ret
}");
        }

        [Fact]
        public void UnmanagedConstraints_NestedStructs_Flat()
        {
            CompileAndVerify(@"
struct TestData
{
    public int A;
    public TestData(int a)
    {
        A = a;
    }
}
unsafe class Test
{
    public static void Main()
    {
        N<TestData>();
    }
    static void N<T>() where T : unmanaged
    {
        System.Console.WriteLine(sizeof(T));
    }
}", options: TestOptions.UnsafeReleaseExe, verify: Verification.Passes, expectedOutput: "4");
        }

        [Fact]
        public void UnmanagedConstraints_NestedStructs_Nested()
        {
            CompileAndVerify(@"
struct InnerTestData
{
    public int B;
    public InnerTestData(int b)
    {
        B = b;
    }
}
struct TestData
{
    public int A;
    public InnerTestData B;
    public TestData(int a, int b)
    {
        A = a;
        B = new InnerTestData(b);
    }
}
unsafe class Test
{
    public static void Main()
    {
        N<TestData>();
    }
    static void N<T>() where T : unmanaged
    {
        System.Console.WriteLine(sizeof(T));
    }
}", options: TestOptions.UnsafeReleaseExe, verify: Verification.Passes, expectedOutput: "8");
        }

        [Fact]
        public void UnmanagedConstraints_NestedStructs_Error()
        {
            CreateCompilation(@"
struct InnerTestData
{
    public string B;
    public InnerTestData(string b)
    {
        B = b;
    }
}
struct TestData
{
    public int A;
    public InnerTestData B;
    public TestData(int a, string b)
    {
        A = a;
        B = new InnerTestData(b);
    }
}
class Test
{
    public static void Main()
    {
        N<TestData>();
    }
    static void N<T>() where T : unmanaged
    {
    }
}").VerifyDiagnostics(
                // (24,9): error CS8375: The type 'TestData' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.N<T>()'
                //         N<TestData>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "N<TestData>").WithArguments("Test.N<T>()", "T", "TestData").WithLocation(24, 9));
        }

        [Fact]
        public void UnmanagedConstraints_ExistingUnmanagedKeywordType_InScope()
        {
            CompileAndVerify(@"
class unmanaged
{
    public void Print()
    {
        System.Console.WriteLine(""success"");
    }
}
class Test
{
    public static void Main()
    {
        M(new unmanaged());
    }
    static void M<T>(T arg) where T : unmanaged
    {
        arg.Print();
    }
}", expectedOutput: "success");
        }

        [Fact]
        public void UnmanagedConstraints_ExistingUnmanagedKeywordType_OutOfScope()
        {
            CreateCompilation(@"
namespace hidden
{
    class unmanaged
    {
        public void Print()
        {
            System.Console.WriteLine(""success"");
        }
    }
}
class Test
{
    public static void Main()
    {
        M(""test"");
    }
    static void M<T>(T arg) where T : unmanaged
    {
        arg.Print();
    }
}").VerifyDiagnostics(
                // (16,9): error CS8375: The type 'string' cannot be a reference type, or contain reference type fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>(T)'
                //         M("test");
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("Test.M<T>(T)", "T", "string").WithLocation(16, 9),
                // (20,13): error CS1061: 'T' does not contain a definition for 'Print' and no extension method 'Print' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         arg.Print();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Print").WithArguments("T", "Print").WithLocation(20, 13));
        }

        [Fact]
        public void UnmanagedConstraints_UnmanagedIsValidForStructConstraint_Methods()
        {
            CompileAndVerify(@"
class Program
{
    static void A<T>(T arg) where T : struct
    {
        System.Console.WriteLine(arg);
    }
    static void B<T>(T arg) where T : unmanaged
    {
        A(arg);
    }
    static void Main()
    {
        B(5);
    }
}", expectedOutput: "5");
        }

        [Fact]
        public void UnmanagedConstraints_UnmanagedIsValidForStructConstraint_Types()
        {
            CompileAndVerify(@"
class A<T> where T : struct
{
    public void M(T arg)
    {
        System.Console.WriteLine(arg);
    }
}
class B<T> : A<T> where T : unmanaged
{
}
class Program
{
    static void Main()
    {
        new B<int>().M(5);
    }
}", expectedOutput: "5");
        }

        [Fact]
        public void UnmanagedConstraints_UnmanagedIsValidForStructConstraint_Interfaces()
        {
            CompileAndVerify(@"
interface A<T> where T : struct
{
    void M(T arg);
}
class B<T> : A<T> where T : unmanaged
{
    public void M(T arg)
    {
        System.Console.WriteLine(arg);
    }
}
class Program
{
    static void Main()
    {
        new B<int>().M(5);
    }
}", expectedOutput: "5");
        }

        [Fact]
        public void UnmanagedConstraints_PointerTypeSubstitution()
        {
            var compilation = CreateCompilation(@"
unsafe public class Test
{
    public T* M<T>() where T : unmanaged => throw null;
    
    public void N()
    {
        var result = M<int>();
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var value = ((VariableDeclaratorSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.VariableDeclarator)).Initializer.Value;
            Assert.Equal("M<int>()", value.ToFullString());

            var symbol = (MethodSymbol)model.GetSymbolInfo(value).Symbol;
            Assert.Equal("System.Int32*", symbol.ReturnType.ToTestDisplayString());
        }

        [Fact]
        public void UnmanagedConstraints_CannotConstraintToTypeParameterConstrainedByUnmanaged()
        {
            CreateCompilation(@"
class Test<U> where U : unmanaged
{
    void M<T>() where T : U
    {
    }
}").VerifyDiagnostics(
                // (4,12): error CS8377: Type parameter 'U' has the 'unmanaged' constraint so 'U' cannot be used as a constraint for 'T'
                //     void M<T>() where T : U
                Diagnostic(ErrorCode.ERR_ConWithUnmanagedCon, "T").WithArguments("T", "U").WithLocation(4, 12));
        }

        [Fact]
        public void UnmanagedConstraints_UnmanagedAsTypeConstraintName()
        {
            CreateCompilation(@"
class Test<unmanaged> where unmanaged : System.IDisposable
{
    void M<T>(T arg) where T : unmanaged
    {
        arg.Dispose();
        arg.NonExistentMethod();
    }
}").VerifyDiagnostics(
                // (7,13): error CS1061: 'T' does not contain a definition for 'NonExistentMethod' and no extension method 'NonExistentMethod' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         arg.NonExistentMethod();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "NonExistentMethod").WithArguments("T", "NonExistentMethod").WithLocation(7, 13));
        }

        [Fact]
        public void UnmanagedConstraints_CircularReferenceToUnmanagedTypeWillBindSuccessfully()
        {
            CreateCompilation(@"
public unsafe class C<U> where U : unmanaged
{
    public void M1<T>() where T : T* { }
    public void M2<T>() where T : U* { }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,35): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //     public void M2<T>() where T : U* { }
                Diagnostic(ErrorCode.ERR_BadConstraintType, "U*").WithLocation(5, 35),
                // (4,35): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //     public void M1<T>() where T : T* { }
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T*").WithLocation(4, 35));
        }
    }
}
