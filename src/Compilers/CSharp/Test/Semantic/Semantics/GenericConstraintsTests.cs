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

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
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
        public void EnumConstraint_Compilation_Interface()
        {
            CreateCompilation(@"
public class Test<T> where T : System.Enum, System.IDisposable
{
}
public enum E1
{
    A
}
public class Test2
{
    public void M<U>() where U : System.IDisposable
    {
        var a = new Test<E1>();             // not disposable
        var b = new Test<U>();              // not enum
        var c = new Test<int>();            // neither disposable nor enum
    }
}").VerifyDiagnostics(
                // (13,26): error CS0315: The type 'E1' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'E1' to 'System.IDisposable'.
                //         var a = new Test<E1>();             // not disposable
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "E1").WithArguments("Test<T>", "System.IDisposable", "T", "E1").WithLocation(13, 26),
                // (14,26): error CS0314: The type 'U' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion or type parameter conversion from 'U' to 'System.Enum'.
                //         var b = new Test<U>();              // not enum
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "U").WithArguments("Test<T>", "System.Enum", "T", "U").WithLocation(14, 26),
                // (15,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         var c = new Test<int>();            // neither disposable nor enum
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.Enum", "T", "int").WithLocation(15, 26),
                // (15,26): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no boxing conversion from 'int' to 'System.IDisposable'.
                //         var c = new Test<int>();            // neither disposable nor enum
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "int").WithArguments("Test<T>", "System.IDisposable", "T", "int").WithLocation(15, 26));
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

            var oldOptions = new CSharpParseOptions(LanguageVersion.CSharp7_2);

            CreateCompilation(code, parseOptions: oldOptions).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'enum generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.Enum
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.Enum").WithArguments("enum generic type constraints", "7.3").WithLocation(2, 32));

            var reference = CreateCompilation(code).EmitToImageReference();

            var legacyCode = @"
enum E
{
}
class Legacy
{
    void M()
    {
        var a = new Test<E>();          // valid
        var b = new Test<Legacy>();     // invalid
    }
}";

            CreateCompilation(legacyCode, parseOptions: oldOptions, references: new[] { reference }).VerifyDiagnostics(
                // (10,26): error CS0311: The type 'Legacy' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'Legacy' to 'System.Enum'.
                //         var b = new Test<Legacy>();     // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Legacy").WithArguments("Test<T>", "System.Enum", "T", "Legacy").WithLocation(10, 26));
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
        public void EnumConstraint_IsReflectedInSymbols_Alone()
        {
            var code = "public class Test<T> where T : System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedInSymbols_ValueType()
        {
            var code = "public class Test<T> where T : struct, System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedInSymbols_ReferenceType()
        {
            var code = "public class Test<T> where T : class, System.Enum { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.IsValueType);
                Assert.True(typeParameter.IsReferenceType);
                Assert.False(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Equal(SpecialType.System_Enum, typeParameter.ConstraintTypes().Single().SpecialType);
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void EnumConstraint_IsReflectedInSymbols_Constructor()
        {
            var code = "public class Test<T> where T : System.Enum, new() { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
                Assert.False(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
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
                // (8,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.Enum { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.Enum").WithLocation(8, 43));
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
                // (4,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.Enum { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.Enum").WithLocation(4, 43));
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
        public void EnumConstraint_InheritingFromEnum()
        {
            var code = @"
public class Child : System.Enum
{
}

public enum E
{
    A
}

public class Test
{
    public void M<T>(T arg) where T : System.Enum
    {
    }

    public void N()
    {
        M(E.A);             // valid
        M(new Child());     // invalid
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (2,22): error CS0644: 'Child' cannot derive from special class 'Enum'
                // public class Child : System.Enum
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "System.Enum").WithArguments("Child", "System.Enum").WithLocation(2, 22),
                // (20,9): error CS0311: The type 'Child' cannot be used as type parameter 'T' in the generic type or method 'Test.M<T>(T)'. There is no implicit reference conversion from 'Child' to 'System.Enum'.
                //         M(new Child());     // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("Test.M<T>(T)", "System.Enum", "T", "Child").WithLocation(20, 9));
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
                // (2,40): error CS0450: 'Delegate': cannot specify both a constraint class and the 'class' or 'struct' constraint
                // public class Test<T> where T : struct, System.Delegate
                Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "System.Delegate").WithArguments("System.Delegate").WithLocation(2, 40)
            );
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
            var oldOptions = new CSharpParseOptions(LanguageVersion.CSharp7_2);

            CreateCompilation(code, parseOptions: oldOptions).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.Delegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.Delegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));

            var reference = CreateCompilation(code).EmitToImageReference();

            var legacyCode = @"
delegate void D();

class Legacy
{
    void M()
    {
        var a = new Test<D>();          // valid
        var b = new Test<Legacy>();     // invalid
    }
}";

            CreateCompilation(legacyCode, parseOptions: oldOptions, references: new[] { reference }).VerifyDiagnostics(
                // (9,26): error CS0311: The type 'Legacy' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'Legacy' to 'System.Delegate'.
                //         var b = new Test<Legacy>();     // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Legacy").WithArguments("Test<T>", "System.Delegate", "T", "Legacy").WithLocation(9, 26));
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
        public void DelegateConstraint_IsReflectedInSymbols_Alone()
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
        public void DelegateConstraint_IsReflectedInSymbols_ValueType()
        {
            var compilation = CreateCompilation("public class Test<T> where T : struct, System.Delegate { }")
                    .VerifyDiagnostics(
                        // (1,40): error CS0450: 'Delegate': cannot specify both a constraint class and the 'class' or 'struct' constraint
                        // public class Test<T> where T : struct, System.Delegate { }
                        Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "System.Delegate").WithArguments("System.Delegate").WithLocation(1, 40)
                    );

            var typeParameter = compilation.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Empty(typeParameter.ConstraintTypes());
        }

        [Fact]
        public void DelegateConstraint_IsReflectedInSymbols_ReferenceType()
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
        public void DelegateConstraint_IsReflectedInSymbols_Constructor()
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
                // (8,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.Delegate").WithLocation(8, 43));
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
                // (4,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.Delegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.Delegate").WithLocation(4, 43));
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
                // (2,40): error CS0450: 'MulticastDelegate': cannot specify both a constraint class and the 'class' or 'struct' constraint
                // public class Test<T> where T : struct, System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "System.MulticastDelegate").WithArguments("System.MulticastDelegate").WithLocation(2, 40)
            );
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

            var oldOptions = new CSharpParseOptions(LanguageVersion.CSharp7_2);

            CreateCompilation(code, parseOptions: oldOptions).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'delegate generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : System.MulticastDelegate
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "System.MulticastDelegate").WithArguments("delegate generic type constraints", "7.3").WithLocation(2, 32));

            var reference = CreateCompilation(code).EmitToImageReference();

            var legacyCode = @"
delegate void D();

class Legacy
{
    void M()
    {
        var a = new Test<D>();          // valid
        var b = new Test<Legacy>();     // invalid
    }
}";

            CreateCompilation(legacyCode, parseOptions: oldOptions, references: new[] { reference }).VerifyDiagnostics(
                // (9,26): error CS0311: The type 'Legacy' cannot be used as type parameter 'T' in the generic type or method 'Test<T>'. There is no implicit reference conversion from 'Legacy' to 'System.MulticastDelegate'.
                //         var b = new Test<Legacy>();     // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Legacy").WithArguments("Test<T>", "System.MulticastDelegate", "T", "Legacy").WithLocation(9, 26));
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
        public void MulticastDelegateConstraint_IsReflectedInSymbols_Alone()
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
        public void MulticastDelegateConstraint_IsReflectedInSymbols_ValueType()
        {
            var compilation = CreateCompilation("public class Test<T> where T : struct, System.MulticastDelegate { }")
                    .VerifyDiagnostics(
                        // (1,40): error CS0450: 'MulticastDelegate': cannot specify both a constraint class and the 'class' or 'struct' constraint
                        // public class Test<T> where T : struct, System.MulticastDelegate { }
                        Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "System.MulticastDelegate").WithArguments("System.MulticastDelegate").WithLocation(1, 40)
                     );

            var typeParameter = compilation.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Empty(typeParameter.ConstraintTypes());
        }

        [Fact]
        public void MulticastDelegateConstraint_IsReflectedInSymbols_ReferenceType()
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
        public void MulticastDelegateConstraint_IsReflectedInSymbols_Constructor()
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
                // (8,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.MulticastDelegate").WithLocation(8, 43));
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
                // (4,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : System.MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "System.MulticastDelegate").WithLocation(4, 43));
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (12,26): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "BadType").WithArguments("Test<T>", "T", "BadType").WithLocation(12, 26),
                // (13,26): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(13, 26),
                // (16,26): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var f = new Test<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "W").WithArguments("Test<T>", "T", "W").WithLocation(16, 26));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (13,28): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var b = new Test().M<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<BadType>").WithArguments("Test.M<T>()", "T", "BadType").WithLocation(13, 28),
                // (14,28): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var c = new Test().M<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("Test.M<T>()", "T", "string").WithLocation(14, 28),
                // (17,28): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var f = new Test().M<W>();                  // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<W>").WithArguments("Test.M<T>()", "T", "W").WithLocation(17, 28)
                );
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (8,32): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<BadType> b();                 // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "b").WithArguments("D<T>", "T", "BadType").WithLocation(8, 32),
                // (9,31): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<string> c();                  // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "c").WithArguments("D<T>", "T", "string").WithLocation(9, 31),
                // (12,26): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<W> f();                       // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "f").WithArguments("D<T>", "T", "W").WithLocation(12, 26));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraint_Compilation_Alone_LocalFunction()
        {
            CreateCompilation(@"
public struct GoodType { public int I; }
public struct BadType { public string S; }
public class Test2
{
    public void M<U, W>() where U : unmanaged
    {
        void M<T>() where T : unmanaged
        {
        }

        M<GoodType>();                       // unmanaged struct
        M<BadType>();                        // managed struct
        M<string>();                         // reference type
        M<int>();                            // value type
        M<U>();                              // generic type constrained to unmanaged
        M<W>();                              // unconstrained generic type
    }
}").VerifyDiagnostics(
                // (13,9): error CS8377: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'M<T>()'
                //         M<BadType>();                        // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<BadType>").WithArguments("M<T>()", "T", "BadType").WithLocation(13, 9),
                // (14,9): error CS8377: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'M<T>()'
                //         M<string>();                         // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("M<T>()", "T", "string").WithLocation(14, 9),
                // (17,9): error CS8377: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'M<T>()'
                //         M<W>();                              // unconstrained generic type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<W>").WithArguments("M<T>()", "T", "W").WithLocation(17, 9));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_ReferenceType()
        {
            var c = CreateCompilation("public class Test<T> where T : class, unmanaged {}");

            c.VerifyDiagnostics(
                // (1,39): error CS8380: The 'unmanaged' constraint must come before any other constraints
                // public class Test<T> where T : class, unmanaged {}
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, "unmanaged").WithLocation(1, 39));

            var typeParameter = c.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
            Assert.False(typeParameter.HasUnmanagedTypeConstraint);
            Assert.False(typeParameter.HasValueTypeConstraint);
            Assert.True(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Empty(typeParameter.ConstraintTypes());
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_ValueType()
        {
            var c = CreateCompilation("public class Test<T> where T : struct, unmanaged {}");

            c.VerifyDiagnostics(
                // (1,40): error CS8380: The 'unmanaged' constraint must come before any other constraints
                // public class Test<T> where T : struct, unmanaged {}
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, "unmanaged").WithLocation(1, 40));

            var typeParameter = c.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
            Assert.False(typeParameter.HasUnmanagedTypeConstraint);
            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
            Assert.Empty(typeParameter.ConstraintTypes());
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_Constructor()
        {
            CreateCompilation("public class Test<T> where T : unmanaged, new() {}").VerifyDiagnostics(
                // (1,43): error CS8379: The 'new()' constraint cannot be used with the 'unmanaged' constraint
                // public class Test<T> where T : unmanaged, new() {}
                Diagnostic(ErrorCode.ERR_NewBoundWithUnmanaged, "new").WithLocation(1, 43));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherClass_Before()
        {
            CreateCompilation("public class Test<T> where T : unmanaged, System.Exception { }").VerifyDiagnostics(
                // (1,43): error CS8380: 'Exception': cannot specify both a constraint class and the 'unmanaged' constraint
                // public class Test<T> where T : unmanaged, System.Exception { }
                Diagnostic(ErrorCode.ERR_UnmanagedBoundWithClass, "System.Exception").WithArguments("System.Exception").WithLocation(1, 43)
            );
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherClass_After()
        {
            CreateCompilation("public class Test<T> where T : System.Exception, unmanaged { }").VerifyDiagnostics(
                // (1,50): error CS8380: The 'unmanaged' constraint must come before any other constraints
                // public class Test<T> where T : System.Exception, unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, "unmanaged").WithLocation(1, 50));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_OtherValidTypes_After()
        {
            CreateCompilation("public class Test<T> where T : System.Enum, System.IDisposable, unmanaged { }").VerifyDiagnostics(
                // (1,65): error CS8376: The 'unmanaged' constraint must come before any other constraints
                // public class Test<T> where T : System.Enum, System.IDisposable, unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, "unmanaged").WithLocation(1, 65));
        }

        [Fact]
        public void UnmanagedConstraint_OtherValidTypes_Before()
        {
            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                AssertEx.Equal(new string[] { "Enum", "IDisposable" }, typeParameter.ConstraintTypes().Select(type => type.Name));
            };

            CompileAndVerify(
                "public class Test<T> where T : unmanaged, System.Enum, System.IDisposable { }",
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherParameter_After()
        {
            CreateCompilation("public class Test<T, U> where T : U, unmanaged { }").VerifyDiagnostics(
                // (1,38): error CS8380: The 'unmanaged' constraint must come before any other constraints
                // public class Test<T, U> where T : U, unmanaged { }
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, "unmanaged").WithLocation(1, 38));
        }

        [Fact]
        public void UnmanagedConstraint_Compilation_AnotherParameter_Before()
        {
            CreateCompilation("public class Test<T, U> where T : unmanaged, U { }").VerifyDiagnostics();
            CreateCompilation("public class Test<T, U> where U: class where T : unmanaged, U, System.IDisposable { }").VerifyDiagnostics();
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
                // (9,26): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "BadType").WithArguments("Test<T>", "T", "BadType").WithLocation(9, 26),
                // (10,26): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var c = new Test<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("Test<T>", "T", "string").WithLocation(10, 26),
                // (13,26): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
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
                // (9,28): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var b = new Test().M<BadType>();            // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<BadType>").WithArguments("Test.M<T>()", "T", "BadType").WithLocation(9, 28),
                // (10,28): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
                //         var c = new Test().M<string>();             // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("Test.M<T>()", "T", "string").WithLocation(10, 28),
                // (13,28): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>()'
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
                // (7,32): error CS8379: The type 'BadType' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<BadType> b();                 // managed struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "b").WithArguments("D<T>", "T", "BadType").WithLocation(7, 32),
                // (8,31): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     public abstract D<string> c();                  // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "c").WithArguments("D<T>", "T", "string").WithLocation(8, 31),
                // (11,26): error CS8379: The type 'W' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
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

            var oldOptions = new CSharpParseOptions(LanguageVersion.CSharp7_2);

            CreateCompilation(code, parseOptions: oldOptions).VerifyDiagnostics(
                // (2,32): error CS8320: Feature 'unmanaged generic type constraints' is not available in C# 7.2. Please use language version 7.3 or greater.
                // public class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "unmanaged").WithArguments("unmanaged generic type constraints", "7.3").WithLocation(2, 32));

            var reference = CreateCompilation(code).EmitToImageReference();

            var legacyCode = @"
class Legacy
{
    void M()
    {
        var a = new Test<int>();        // valid
        var b = new Test<Legacy>();     // invalid
    }
}";

            CreateCompilation(legacyCode, parseOptions: oldOptions, references: new[] { reference }).VerifyDiagnostics(
                // (7,26): error CS8377: The type 'Legacy' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         var b = new Test<Legacy>();     // invalid
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "Legacy").WithArguments("Test<T>", "T", "Legacy").WithLocation(7, 26));
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedInSymbols_Alone_Type()
        {
            var code = "public class Test<T> where T : unmanaged { }";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedInSymbols_Alone_Method()
        {
            var code = @"
public class Test
{
    public void M<T>() where T : unmanaged {}
}";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").GetMethod("M").TypeParameters.Single();

                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedInSymbols_Alone_Delegate()
        {
            var code = "public delegate void D<T>() where T : unmanaged;";

            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();

                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            };

            CompileAndVerify(code, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void UnmanagedConstraint_IsReflectedInSymbols_Alone_LocalFunction()
        {
            var code = @"
public class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }
    }
}";

            CompileAndVerify(code, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__N|0_0").TypeParameters.Single();

                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);
                Assert.Empty(typeParameter.ConstraintTypes());
            });
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
                // (17,14): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<string>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("B.M<T>()", "T", "string").WithLocation(17, 14),
                // (18,14): error CS8379: The type 'Test' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
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
                // (13,14): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
                //         this.M<string>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<string>").WithArguments("B.M<T>()", "T", "string").WithLocation(13, 14),
                // (14,14): error CS8379: The type 'Test' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'B.M<T>()'
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
                // (8,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(8, 43));
        }

        [Fact]
        public void UnmanagedConstraint_StructMismatchInImplements()
        {
            CreateCompilation(@"
public struct Segment<T> {
    public T[] array;
}

public interface I1<in T> where T : unmanaged
{
    void Test<G>(G x) where G : unmanaged;
}

public class C2<T> : I1<T> where T : struct
{
    public void Test<G>(G x) where G : struct
    {
        I1<T> i = this;
        i.Test(default(Segment<int>));
    }
}
").VerifyDiagnostics(
                // (11,14): error CS8377: The type 'T' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'I1<T>'
                // public class C2<T> : I1<T> where T : struct
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "C2").WithArguments("I1<T>", "T", "T").WithLocation(11, 14),
                // (13,17): error CS0425: The constraints for type parameter 'G' of method 'C2<T>.Test<G>(G)' must match the constraints for type parameter 'G' of interface method 'I1<T>.Test<G>(G)'. Consider using an explicit interface implementation instead.
                //     public void Test<G>(G x) where G : struct
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "Test").WithArguments("G", "C2<T>.Test<G>(G)", "G", "I1<T>.Test<G>(G)").WithLocation(13, 17),
                // (15,12): error CS8377: The type 'T' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'I1<T>'
                //         I1<T> i = this;
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "T").WithArguments("I1<T>", "T", "T").WithLocation(15, 12),
                // (16,11): error CS8377: The type 'Segment<int>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'G' in the generic type or method 'I1<T>.Test<G>(G)'
                //         i.Test(default(Segment<int>));
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "Test").WithArguments("I1<T>.Test<G>(G)", "G", "Segment<int>").WithLocation(16, 11)
                );
        }

        [Fact]
        public void UnmanagedConstraint_TypeMismatchInImplements()
        {
            CreateCompilation(@"
public interface I1<in T> where T : unmanaged, System.IDisposable
{
    void Test<G>(G x) where G : unmanaged, System.Enum;
}

public class C2<T> : I1<T> where T : unmanaged
{
    public void Test<G>(G x) where G : unmanaged
    {
        I1<T> i = this;
        i.Test(default(System.AttributeTargets)); // <-- this one is OK
        i.Test(0);
    }
}
").VerifyDiagnostics(
                // (7,14): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'I1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'System.IDisposable'.
                // public class C2<T> : I1<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "C2").WithArguments("I1<T>", "System.IDisposable", "T", "T").WithLocation(7, 14),
                // (9,17): error CS0425: The constraints for type parameter 'G' of method 'C2<T>.Test<G>(G)' must match the constraints for type parameter 'G' of interface method 'I1<T>.Test<G>(G)'. Consider using an explicit interface implementation instead.
                //     public void Test<G>(G x) where G : unmanaged
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "Test").WithArguments("G", "C2<T>.Test<G>(G)", "G", "I1<T>.Test<G>(G)").WithLocation(9, 17),
                // (11,12): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'I1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'System.IDisposable'.
                //         I1<T> i = this;
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T").WithArguments("I1<T>", "System.IDisposable", "T", "T").WithLocation(11, 12),
                // (13,11): error CS0315: The type 'int' cannot be used as type parameter 'G' in the generic type or method 'I1<T>.Test<G>(G)'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         i.Test(0);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Test").WithArguments("I1<T>.Test<G>(G)", "System.Enum", "G", "int").WithLocation(13, 11)
                );
        }

        [Fact]
        public void UnmanagedConstraint_TypeMismatchInImplementsMeta()
        {
            var reference = CreateCompilation(@"
public interface I1<in T> where T : unmanaged, System.IDisposable
{
    void Test<G>(G x) where G : unmanaged, System.Enum;
}
").EmitToImageReference();

            CreateCompilation(@"
public class C2<T> : I1<T> where T : unmanaged
{
    public void Test<G>(G x) where G : unmanaged
    {
        I1<T> i = this;
        i.Test(default(System.AttributeTargets)); // <-- this one is OK
        i.Test(0);
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (2,14): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'I1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'System.IDisposable'.
                // public class C2<T> : I1<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "C2").WithArguments("I1<T>", "System.IDisposable", "T", "T").WithLocation(2, 14),
                // (4,17): error CS0425: The constraints for type parameter 'G' of method 'C2<T>.Test<G>(G)' must match the constraints for type parameter 'G' of interface method 'I1<T>.Test<G>(G)'. Consider using an explicit interface implementation instead.
                //     public void Test<G>(G x) where G : unmanaged
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "Test").WithArguments("G", "C2<T>.Test<G>(G)", "G", "I1<T>.Test<G>(G)").WithLocation(4, 17),
                // (6,12): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'I1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'System.IDisposable'.
                //         I1<T> i = this;
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T").WithArguments("I1<T>", "System.IDisposable", "T", "T").WithLocation(6, 12),
                // (8,11): error CS0315: The type 'int' cannot be used as type parameter 'G' in the generic type or method 'I1<T>.Test<G>(G)'. There is no boxing conversion from 'int' to 'System.Enum'.
                //         i.Test(0);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Test").WithArguments("I1<T>.Test<G>(G)", "System.Enum", "G", "int").WithLocation(8, 11)
                );
        }

        [Fact]
        public void UnmanagedConstraint_TypeMismatchInImplementsMeta2()
        {
            var reference = CreateCompilation(@"
    public interface I1
    {
        void Test<G>(ref G x) where G : unmanaged, System.IDisposable;
    }
").EmitToImageReference();

            var reference1 = CreateCompilation(@"
public class C1 : I1
{
    void I1.Test<G>(ref G x)
    {
        x.Dispose();
    }
}", references: new[] { reference }).EmitToImageReference(); ;

            CompileAndVerify(@"
struct S : System.IDisposable
{
    public int a;

    public void Dispose()
    {
        a += 123;
    }
}

class Test
{
    static void Main()
    {
        S local = default;
        I1 i = new C1();
        i.Test(ref local);
        System.Console.WriteLine(local.a);
    }
}",

                // NOTE: must pass verification (IDisposable constraint is copied over to the implementing method) 
                options: TestOptions.UnsafeReleaseExe, references: new[] { reference, reference1 }, verify: Verification.Passes, expectedOutput: "123");
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
                // (4,43): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M<T>() where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(4, 43));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (9,9): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>(T)'
                //         M("test");
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("Test.M<T>(T)", "T", "string").WithLocation(9, 9));
        }

        [ConditionalTheory(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_InterfaceMethod()
        {
            CompileAndVerify(@"
struct S : System.IDisposable
{
    public int a;

    public void Dispose()
    {
        a += 123;
    }
}
unsafe class Test
{
    static void M<T>(ref T arg) where T : unmanaged, System.IDisposable
    {
        arg.Dispose();

        fixed(T* ptr = &arg)
        {
            ptr->Dispose();
        }
    }

    static void Main()
    {
        S local = default;
        M(ref local);
        System.Console.WriteLine(local.a);
    }
}",
    options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: "246").VerifyIL("Test.M<T>", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (pinned T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  constrained. ""T""
  IL_0007:  callvirt   ""void System.IDisposable.Dispose()""
  IL_000c:  ldarg.0
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  conv.u
  IL_0010:  constrained. ""T""
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  ldc.i4.0
  IL_001c:  conv.u
  IL_001d:  stloc.0
  IL_001e:  ret
}");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_CtorAndValueTypeAreEmitted()
        {
            CompileAndVerify(@"
using System.Linq;
class Program
{
    public static void M<T>() where T: unmanaged
    {
    }

    static void Main(string[] args)
    {
        var typeParam = typeof(Program).GetMethod(""M"").GetGenericArguments().First();
        System.Console.WriteLine(typeParam.GenericParameterAttributes);
    }
}",
    options: TestOptions.UnsafeReleaseExe, verify: Verification.Passes, expectedOutput: "NotNullableValueTypeConstraint, DefaultConstructorConstraint");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (24,9): error CS8379: The type 'TestData' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.N<T>()'
                //         N<TestData>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "N<TestData>").WithArguments("Test.N<T>()", "T", "TestData").WithLocation(24, 9));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
                // (16,9): error CS8379: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test.M<T>(T)'
                //         M("test");
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("Test.M<T>(T)", "T", "string").WithLocation(16, 9),
                // (20,13): error CS1061: 'T' does not contain a definition for 'Print' and no extension method 'Print' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         arg.Print();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Print").WithArguments("T", "Print").WithLocation(20, 13));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_UnmanagedIsValidForStructConstraint_LocalFunctions()
        {
            CompileAndVerify(@"
class Program
{
    static void Main()
    {
        void A<T>(T arg) where T : struct
        {
            System.Console.WriteLine(arg);
        }

        void B<T>(T arg) where T : unmanaged
        {
            A(arg);
        }

        B(5);
    }
}", expectedOutput: "5");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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
            Assert.Equal("System.Int32*", symbol.ReturnTypeWithAnnotations.ToTestDisplayString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_CannotConstraintToTypeParameterConstrainedByUnmanaged()
        {
            CreateCompilation(@"
class Test<U> where U : unmanaged
{
    void M<T>() where T : U
    {
    }
}").VerifyDiagnostics(
                // (4,12): error CS8379: Type parameter 'U' has the 'unmanaged' constraint so 'U' cannot be used as a constraint for 'T'
                //     void M<T>() where T : U
                Diagnostic(ErrorCode.ERR_ConWithUnmanagedCon, "T").WithArguments("T", "U").WithLocation(4, 12));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_EnumWithUnmanaged()
        {
            Action<ModuleSymbol> validator = module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();

                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.False(typeParameter.HasReferenceTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);

                Assert.Equal("Enum", typeParameter.ConstraintTypes().Single().Name);

                Assert.True(typeParameter.IsValueType);
                Assert.False(typeParameter.IsReferenceType);
            };

            CompileAndVerify(
                source: "public class Test<T> where T : unmanaged, System.Enum {}",
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_NestedInGenericType()
        {
            var code = @"
public class Wrapper<T>
{
    public enum E
    {
    }

    public struct S
    {
    }
}
public class Test
{
    void IsUnmanaged<T>() where T : unmanaged { }
    void IsEnum<T>() where T : System.Enum { }
    void IsStruct<T>() where T : struct { }
    void IsNew<T>() where T : new() { }
    

    void User()
    {
        IsUnmanaged<Wrapper<int>.E>();
        IsEnum<Wrapper<int>.E>();
        IsStruct<Wrapper<int>.E>();
        IsNew<Wrapper<int>.E>();

        IsUnmanaged<Wrapper<int>.S>();
        IsEnum<Wrapper<int>.S>();               // Invalid
        IsStruct<Wrapper<int>.S>();
        IsNew<Wrapper<int>.S>();

        IsUnmanaged<Wrapper<string>.E>();
        IsEnum<Wrapper<string>.E>();
        IsStruct<Wrapper<string>.E>();
        IsNew<Wrapper<string>.E>();

        IsUnmanaged<Wrapper<string>.S>();
        IsEnum<Wrapper<string>.S>();               // Invalid
        IsStruct<Wrapper<string>.S>();
        IsNew<Wrapper<string>.S>();
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (28,9): error CS0315: The type 'Wrapper<int>.S' cannot be used as type parameter 'T' in the generic type or method 'Test.IsEnum<T>()'. There is no boxing conversion from 'Wrapper<int>.S' to 'System.Enum'.
                //         IsEnum<Wrapper<int>.S>();               // Invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "IsEnum<Wrapper<int>.S>").WithArguments("Test.IsEnum<T>()", "System.Enum", "T", "Wrapper<int>.S").WithLocation(28, 9),
                // (38,9): error CS0315: The type 'Wrapper<string>.S' cannot be used as type parameter 'T' in the generic type or method 'Test.IsEnum<T>()'. There is no boxing conversion from 'Wrapper<string>.S' to 'System.Enum'.
                //         IsEnum<Wrapper<string>.S>();               // Invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "IsEnum<Wrapper<string>.S>").WithArguments("Test.IsEnum<T>()", "System.Enum", "T", "Wrapper<string>.S").WithLocation(38, 9));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraints_PointerInsideStruct()
        {
            CompileAndVerify(@"
unsafe struct S
{
    public int a;
    public int* b;

    public S(int a, int* b)
    {
        this.a = a;
        this.b = b;
    }
}
unsafe class Test
{
    static T* M<T>() where T : unmanaged
    {
        System.Console.WriteLine(typeof(T).FullName);

        T* ar = null;
        return ar;
    }
    static void Main()
    {
        S* ar = M<S>();
    }
}",
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails,
                expectedOutput: "S");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraint_LambdaTypeParameters()
        {
            CompileAndVerify(@"
public delegate T D<T>() where T : unmanaged;
public class Test<U1> where U1 : unmanaged
{
    public static void Print<T>(D<T> lambda) where T : unmanaged
    {
        System.Console.WriteLine(lambda());
    }
    public static void User1(U1 arg)
    {
        Print(() => arg);
    }
    public static void User2<U2>(U2 arg) where U2 : unmanaged
    {
        Print(() => arg);
    }
}
public class Program
{
    public static void Main()
    {
        // Testing the constraint when the lambda type parameter is both coming from an enclosing type, or copied to the generated lambda class

        Test<int>.User1(1);
        Test<int>.User2(2);
    }
}",
                expectedOutput: @"
1
2",
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.True(module.ContainingAssembly.GetTypeByMetadataName("D`1").TypeParameters.Single().HasUnmanagedTypeConstraint);
                    Assert.True(module.ContainingAssembly.GetTypeByMetadataName("Test`1").TypeParameters.Single().HasUnmanagedTypeConstraint);
                    Assert.True(module.ContainingAssembly.GetTypeByMetadataName("Test`1").GetTypeMember("<>c__DisplayClass2_0").TypeParameters.Single().HasUnmanagedTypeConstraint);
                });
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        public void UnmanagedConstraint_IsConsideredDuringOverloadResolution()
        {
            CompileAndVerify(@"
public class Program
{
    static void Test<T>(T arg) where T : unmanaged
    {
        System.Console.WriteLine(""Unmanaged: "" + arg);
    }
    static void Test(object arg)
    {
        System.Console.WriteLine(""Object: "" + arg);
    }

    static void User<U>(U arg) where U : unmanaged
    {
        Test(1);                // should pick up the first, as it is better than the second one (which requires a conversion)
        Test(""2"");            // should pick up the second, as it is the only candidate
        Test(arg);              // should pick up the first, as it is the only candidate
    }
    static void Main()
    {
        User(3);
    }
}",
                expectedOutput: @"
Unmanaged: 1
Object: 2
Unmanaged: 3");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference()
        {
            var compilation = CreateCompilation(@"
class C
{
    unsafe void M<T>(T* a) where T : unmanaged
    {
        var p = stackalloc T[10];
        M(p);
    }
}", options: TestOptions.UnsafeReleaseDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var inferredMethod = (MethodSymbol)model.GetSymbolInfo(call).Symbol;
            var declaredMethod = compilation.GlobalNamespace.GetTypeMember("C").GetMethod("M");

            Assert.Equal(declaredMethod, inferredMethod);
            Assert.Equal(declaredMethod.TypeParameters.Single(), inferredMethod.TypeArgumentsWithAnnotations.Single().Type);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference_CallFromADifferentMethod()
        {
            var compilation = CreateCompilation(@"
class C
{
    unsafe void M<T>(T* a) where T : unmanaged
    {
    }
    unsafe void N()
    {
        var p = stackalloc int[10];
        M(p);
    }
}", options: TestOptions.UnsafeReleaseDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var inferredMethod = (MethodSymbol)model.GetSymbolInfo(call).Symbol;
            var declaredMethod = compilation.GlobalNamespace.GetTypeMember("C").GetMethod("M");

            Assert.Equal(declaredMethod, inferredMethod.ConstructedFrom());
            Assert.Equal(SpecialType.System_Int32, inferredMethod.TypeArgumentsWithAnnotations.Single().SpecialType);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference_WithOtherArgs()
        {
            var compilation = CreateCompilation(@"
unsafe class C
{
    static void M<T>(T* a, T b) where T : unmanaged
    {
        M(null, b);
    }
}", options: TestOptions.UnsafeReleaseDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var inferredMethod = (MethodSymbol)model.GetSymbolInfo(call).Symbol;
            var declaredMethod = compilation.GlobalNamespace.GetTypeMember("C").GetMethod("M");

            Assert.Equal(declaredMethod, inferredMethod);
            Assert.Equal(declaredMethod.TypeParameters.Single(), inferredMethod.TypeArgumentsWithAnnotations.Single().Type);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference_WithOtherArgs_CallFromADifferentMethod()
        {
            var compilation = CreateCompilation(@"
unsafe class C
{
    static void M<T>(T* a, T b) where T : unmanaged
    {
    }
    static void N()
    {
        M(null, 5);
    }
}", options: TestOptions.UnsafeReleaseDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var inferredMethod = (MethodSymbol)model.GetSymbolInfo(call).Symbol;
            var declaredMethod = compilation.GlobalNamespace.GetTypeMember("C").GetMethod("M");

            Assert.Equal(declaredMethod, inferredMethod.ConstructedFrom());
            Assert.Equal(SpecialType.System_Int32, inferredMethod.TypeArgumentsWithAnnotations.Single().SpecialType);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10782")]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference_Errors()
        {
            CreateCompilation(@"
unsafe class C
{
    void Unmanaged<T>(T* a) where T : unmanaged
    {
    }
    void UnmanagedWithInterface<T>(T* a) where T : unmanaged, System.IDisposable
    {
    }
    void Test()
    {
        int a = 0;

        Unmanaged(0);                       // fail (type inference)
        Unmanaged(a);                       // fail (type inference)
        Unmanaged(&a);                      // succeed (unmanaged type pointer)

        UnmanagedWithInterface(0);          // fail (type inference)
        UnmanagedWithInterface(a);          // fail (type inference)
        UnmanagedWithInterface(&a);         // fail (does not match interface)
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (14,9): error CS0411: The type arguments for method 'C.Unmanaged<T>(T*)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Unmanaged(0);                       // fail (type inference)
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Unmanaged").WithArguments("C.Unmanaged<T>(T*)").WithLocation(14, 9),
                // (15,9): error CS0411: The type arguments for method 'C.Unmanaged<T>(T*)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Unmanaged(a);                       // fail (type inference)
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Unmanaged").WithArguments("C.Unmanaged<T>(T*)").WithLocation(15, 9),
                // (18,9): error CS0411: The type arguments for method 'C.UnmanagedWithInterface<T>(T*)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         UnmanagedWithInterface(0);          // fail (type inference)
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "UnmanagedWithInterface").WithArguments("C.UnmanagedWithInterface<T>(T*)").WithLocation(18, 9),
                // (19,9): error CS0411: The type arguments for method 'C.UnmanagedWithInterface<T>(T*)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         UnmanagedWithInterface(a);          // fail (type inference)
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "UnmanagedWithInterface").WithArguments("C.UnmanagedWithInterface<T>(T*)").WithLocation(19, 9),
                // (20,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'C.UnmanagedWithInterface<T>(T*)'. There is no boxing conversion from 'int' to 'System.IDisposable'.
                //         UnmanagedWithInterface(&a);         // fail (does not match interface)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "UnmanagedWithInterface").WithArguments("C.UnmanagedWithInterface<T>(T*)", "System.IDisposable", "T", "int").WithLocation(20, 9));
        }

        [Fact]
        public void UnmanagedGenericStructPointer()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<int> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2(MyStruct<int>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ManagedGenericStructPointer()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<string> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2<T>(MyStruct<T>* ms) where T : unmanaged { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (12,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<string>')
                    //         M2(&myStruct);
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "&myStruct").WithArguments("MyStruct<string>").WithLocation(12, 12));
        }

        [Fact]
        public void UnmanagedGenericConstraintStructPointer()
        {
            var code = @"
public struct MyStruct<T> where T : unmanaged
{
    public T field;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<int> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2(MyStruct<int>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedGenericConstraintNestedStructPointer()
        {
            var code = @"
public struct MyStruct<T> where T : unmanaged
{
    public T field;
}

public struct OuterStruct
{
    public int x;
    public InnerStruct inner;
}

public struct InnerStruct
{
    public int y;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<OuterStruct> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2(MyStruct<OuterStruct>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedGenericConstraintNestedGenericStructPointer()
        {
            var code = @"
public struct MyStruct<T> where T : unmanaged
{
    public T field;
}

public struct InnerStruct<U>
{
    public U value;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<InnerStruct<int>> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2(MyStruct<InnerStruct<int>>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics();

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (16,18): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         MyStruct<InnerStruct<int>> myStruct;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "InnerStruct<int>").WithArguments("unmanaged constructed types", "8.0").WithLocation(16, 18),
                // (17,12): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         M2(&myStruct);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "&myStruct").WithArguments("unmanaged constructed types", "8.0").WithLocation(17, 12),
                // (20,27): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public unsafe void M2(MyStruct<InnerStruct<int>>* ms) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "MyStruct<InnerStruct<int>>*").WithArguments("unmanaged constructed types", "8.0").WithLocation(20, 27),
                // (20,55): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public unsafe void M2(MyStruct<InnerStruct<int>>* ms) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "ms").WithArguments("unmanaged constructed types", "8.0").WithLocation(20, 55));
        }

        [Fact]
        public void UnmanagedGenericStructMultipleConstraints()
        {
            // A diagnostic will only be produced for the first violated constraint.
            var code = @"
public struct MyStruct<T> where T : unmanaged, System.IDisposable
{
    public T field;
}

public struct InnerStruct<U>
{
    public U value;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<InnerStruct<int>> myStruct = default;
        _ = myStruct;
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (16,18): error CS0315: The type 'InnerStruct<int>' cannot be used as type parameter 'T' in the generic type or method 'MyStruct<T>'. There is no boxing conversion from 'InnerStruct<int>' to 'System.IDisposable'.
                    //         MyStruct<InnerStruct<int>> myStruct = default;
                    Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "InnerStruct<int>").WithArguments("MyStruct<T>", "System.IDisposable", "T", "InnerStruct<int>").WithLocation(16, 18)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (16,18): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         MyStruct<InnerStruct<int>> myStruct = default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "InnerStruct<int>").WithArguments("unmanaged constructed types", "8.0").WithLocation(16, 18)
                );
        }

        [Fact]
        public void UnmanagedGenericConstraintPartialConstructedStruct()
        {
            var code = @"
public struct MyStruct<T> where T : unmanaged
{
    public T field;
}

public class C
{
    public unsafe void M<U>()
    {
        MyStruct<U> myStruct;
        M2<U>(&myStruct);
    }

    public unsafe void M2<V>(MyStruct<V>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (11,18): error CS8377: The type 'U' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'MyStruct<T>'
                    //         MyStruct<U> myStruct;
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "U").WithArguments("MyStruct<T>", "T", "U").WithLocation(11, 18),
                    // (12,15): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<U>')
                    //         M2<U>(&myStruct);
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "&myStruct").WithArguments("MyStruct<U>").WithLocation(12, 15),
                    // (15,30): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<V>')
                    //     public unsafe void M2<V>(MyStruct<V>* ms) { }
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "MyStruct<V>*").WithArguments("MyStruct<V>").WithLocation(15, 30),
                    // (15,43): error CS8377: The type 'V' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'MyStruct<T>'
                    //     public unsafe void M2<V>(MyStruct<V>* ms) { }
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "ms").WithArguments("MyStruct<T>", "T", "V").WithLocation(15, 43));
        }

        [Fact]
        public void GenericStructManagedFieldPointer()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class C
{
    public unsafe void M()
    {
        MyStruct<string> myStruct;
        M2(&myStruct);
    }

    public unsafe void M2(MyStruct<string>* ms) { }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (12,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<string>')
                    //         M2(&myStruct);
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "&myStruct").WithArguments("MyStruct<string>").WithLocation(12, 12),
                    // (15,27): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<string>')
                    //     public unsafe void M2(MyStruct<string>* ms) { }
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "MyStruct<string>*").WithArguments("MyStruct<string>").WithLocation(15, 27));
        }

        [Fact]
        public void UnmanagedRecursiveGenericStruct()
        {
            var code = @"
public unsafe struct MyStruct<T> where T : unmanaged
{
    public YourStruct<T>* field;
}

public unsafe struct YourStruct<T> where T : unmanaged
{
    public MyStruct<T>* field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics();
            Assert.False(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void UnmanagedRecursiveStruct()
        {
            var code = @"
public unsafe struct MyStruct
{
    public YourStruct* field;
}

public unsafe struct YourStruct
{
    public MyStruct* field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics();
            Assert.False(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);

        }

        [Fact]
        public void UnmanagedExpandingTypeArgument()
        {
            var code = @"
public struct MyStruct<T>
{
    public YourStruct<MyStruct<MyStruct<T>>> field;
}

public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                    // (4,46): error CS0523: Struct member 'MyStruct<T>.field' of type 'YourStruct<MyStruct<MyStruct<T>>>' causes a cycle in the struct layout
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("MyStruct<T>.field", "YourStruct<MyStruct<MyStruct<T>>>").WithLocation(4, 46));

            Assert.False(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void UnmanagedCyclic()
        {
            var code = @"
public struct MyStruct<T>
{
    public YourStruct<T> field;
}

public struct YourStruct<T> where T : unmanaged
{
    public MyStruct<T> field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                    // (4,26): error CS0523: Struct member 'MyStruct<T>.field' of type 'YourStruct<T>' causes a cycle in the struct layout
                    //     public YourStruct<T> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("MyStruct<T>.field", "YourStruct<T>").WithLocation(4, 26),
                    // (4,26): error CS8377: The type 'T' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'YourStruct<T>'
                    //     public YourStruct<T> field;
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "field").WithArguments("YourStruct<T>", "T", "T").WithLocation(4, 26),
                    // (9,24): error CS0523: Struct member 'YourStruct<T>.field' of type 'MyStruct<T>' causes a cycle in the struct layout
                    //     public MyStruct<T> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("YourStruct<T>.field", "MyStruct<T>").WithLocation(9, 24));

            Assert.False(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void UnmanagedExpandingTypeArgumentManagedGenericField()
        {
            var code = @"
public struct MyStruct<T>
{
    public YourStruct<MyStruct<MyStruct<T>>> field;
    public T myField;
}

public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                    // (4,46): error CS0523: Struct member 'MyStruct<T>.field' of type 'YourStruct<MyStruct<MyStruct<T>>>' causes a cycle in the struct layout
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("MyStruct<T>.field", "YourStruct<MyStruct<MyStruct<T>>>").WithLocation(4, 46));

            Assert.True(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void UnmanagedExpandingTypeArgumentConstraintViolation()
        {
            var code = @"
public struct MyStruct<T>
{
    public YourStruct<MyStruct<MyStruct<T>>> field;
    public string s;
}

public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                    // (4,46): error CS0523: Struct member 'MyStruct<T>.field' of type 'YourStruct<MyStruct<MyStruct<T>>>' causes a cycle in the struct layout
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("MyStruct<T>.field", "YourStruct<MyStruct<MyStruct<T>>>").WithLocation(4, 46),
                    // (4,46): error CS8377: The type 'MyStruct<MyStruct<T>>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'YourStruct<T>'
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "field").WithArguments("YourStruct<T>", "T", "MyStruct<MyStruct<T>>").WithLocation(4, 46));

            Assert.True(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void UnmanagedRecursiveTypeArgumentConstraintViolation_02()
        {
            var code = @"
public struct MyStruct<T>
{
    public YourStruct<MyStruct<MyStruct<T>>> field;
}

public struct YourStruct<T> where T : unmanaged
{
    public T field;
    public string s;
}
";
            var compilation = CreateCompilation(code, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                    // (4,46): error CS0523: Struct member 'MyStruct<T>.field' of type 'YourStruct<MyStruct<MyStruct<T>>>' causes a cycle in the struct layout
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_StructLayoutCycle, "field").WithArguments("MyStruct<T>.field", "YourStruct<MyStruct<MyStruct<T>>>").WithLocation(4, 46),
                    // (4,46): error CS8377: The type 'MyStruct<MyStruct<T>>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'YourStruct<T>'
                    //     public YourStruct<MyStruct<MyStruct<T>>> field;
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "field").WithArguments("YourStruct<T>", "T", "MyStruct<MyStruct<T>>").WithLocation(4, 46));

            Assert.True(compilation.GetMember<NamedTypeSymbol>("MyStruct").IsManagedType);
            Assert.True(compilation.GetMember<NamedTypeSymbol>("YourStruct").IsManagedType);
        }

        [Fact]
        public void NestedGenericStructContainingPointer()
        {
            var code = @"
public unsafe struct MyStruct<T> where T : unmanaged
{
    public T* field;

    public T this[int index]
    {
        get { return field[index]; }
    }
}

public class C
{
    public static unsafe void Main()
    {
        float f = 42;
        var ms = new MyStruct<float> { field = &f };
        var test = new MyStruct<MyStruct<float>> { field = &ms };
        float value = test[0][0];
        System.Console.Write(value);
    }
}
";
            CompileAndVerify(code, options: TestOptions.UnsafeReleaseExe, expectedOutput: "42", verify: Verification.Skipped);
        }

        [Fact]
        public void SimpleGenericStructPointer_ILValidation()
        {
            var code = @"
public unsafe struct MyStruct<T> where T : unmanaged
{
    public T field;

    public static void Test()
    {
        var ms = new MyStruct<int>();
        MyStruct<int>* ptr = &ms;
        ptr->field = 42;
    }
}
";
            var il = @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (MyStruct<int> V_0) //ms
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MyStruct<int>""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldc.i4.s   42
  IL_000d:  stfld      ""int MyStruct<int>.field""
  IL_0012:  ret
}
";
            CompileAndVerify(code, options: TestOptions.UnsafeReleaseDll, verify: Verification.Skipped)
                .VerifyIL("MyStruct<T>.Test", il);
        }

        [Fact, WorkItem(31439, "https://github.com/dotnet/roslyn/issues/31439")]
        public void CircularTypeArgumentUnmanagedConstraint()
        {
            var code = @"
public struct X<T>
    where T : unmanaged
{
}

public struct Z
{
    public X<Z> field;
}";
            var compilation = CreateCompilation(code);
            compilation.VerifyDiagnostics();

            Assert.False(compilation.GetMember<NamedTypeSymbol>("X").IsManagedType);
            Assert.False(compilation.GetMember<NamedTypeSymbol>("Z").IsManagedType);
        }

        [Fact]
        public void GenericStructAddressOfRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;

    public static unsafe void Test()
    {
        var ms = new MyStruct<int>();
        var ptr = &ms;
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (9,19): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var ptr = &ms;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "&ms").WithArguments("unmanaged constructed types", "8.0").WithLocation(9, 19)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void GenericStructFixedRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class MyClass
{
    public MyStruct<int> ms;
    public static unsafe void Test(MyClass c)
    {
        fixed (MyStruct<int>* ptr = &c.ms)
        {
        }
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (12,16): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         fixed (MyStruct<int>* ptr = &c.ms)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "MyStruct<int>*").WithArguments("unmanaged constructed types", "8.0").WithLocation(12, 16),
                // (12,37): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         fixed (MyStruct<int>* ptr = &c.ms)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "&c.ms").WithArguments("unmanaged constructed types", "8.0").WithLocation(12, 37));

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void GenericStructSizeofRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;

    public static unsafe void Test()
    {
        var size = sizeof(MyStruct<int>);
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (8,20): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var size = sizeof(MyStruct<int>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "sizeof(MyStruct<int>)").WithArguments("unmanaged constructed types", "8.0").WithLocation(8, 20)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void GenericImplicitStackallocRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;

    public static unsafe void Test()
    {
        var arr = stackalloc[] { new MyStruct<int>() };
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (8,19): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var arr = stackalloc[] { new MyStruct<int>() };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc[] { new MyStruct<int>() }").WithArguments("unmanaged constructed types", "8.0").WithLocation(8, 19));

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void GenericStackallocRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;

    public static unsafe void Test()
    {
        var arr = stackalloc MyStruct<int>[4];
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (8,30): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var arr = stackalloc MyStruct<int>[4];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "MyStruct<int>").WithArguments("unmanaged constructed types", "8.0").WithLocation(8, 30));

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void GenericStructPointerFieldRequiresCSharp8()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public unsafe struct OtherStruct
{
    public MyStruct<int>* ms;
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (9,12): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public MyStruct<int>* ms;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "MyStruct<int>*").WithArguments("unmanaged constructed types", "8.0").WithLocation(9, 12)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact, WorkItem(32103, "https://github.com/dotnet/roslyn/issues/32103")]
        public void StructContainingTuple_Unmanaged_RequiresCSharp8()
        {
            var code = @"
public struct MyStruct
{
    public (int, int) field;
}

public class C
{
    public unsafe void M<T>() where T : unmanaged { }

    public void M2()
    {
        M<MyStruct>();
        M<(int, int)>();
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (13,9): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         M<MyStruct>();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M<MyStruct>").WithArguments("unmanaged constructed types", "8.0").WithLocation(13, 9),
                // (14,9): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         M<(int, int)>();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M<(int, int)>").WithArguments("unmanaged constructed types", "8.0").WithLocation(14, 9)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact, WorkItem(32103, "https://github.com/dotnet/roslyn/issues/32103")]
        public void StructContainingGenericTuple_Unmanaged()
        {
            var code = @"
public struct MyStruct<T>
{
    public (T, T) field;
}

public class C
{
    public unsafe void M<T>() where T : unmanaged { }

    public void M2<U>() where U : unmanaged
    {
        M<MyStruct<U>>();
    }

    public void M3<V>()
    {
        M<MyStruct<V>>();
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (13,9): error CS8652: The feature 'unmanaged constructed types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         M<MyStruct<U>>();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M<MyStruct<U>>").WithArguments("unmanaged constructed types", "8.0").WithLocation(13, 9),
                // (18,9): error CS8377: The type 'MyStruct<V>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'C.M<T>()'
                //         M<MyStruct<V>>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<MyStruct<V>>").WithArguments("C.M<T>()", "T", "MyStruct<V>").WithLocation(18, 9)
                );

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (18,9): error CS8377: The type 'MyStruct<V>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'C.M<T>()'
                    //         M<MyStruct<V>>();
                    Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M<MyStruct<V>>").WithArguments("C.M<T>()", "T", "MyStruct<V>").WithLocation(18, 9)
                );
        }

        [Fact]
        public void GenericRefStructAddressOf_01()
        {
            var code = @"
public ref struct MyStruct<T>
{
    public T field;
}

public class MyClass
{
    public static unsafe void Main()
    {
        var ms = new MyStruct<int>() { field = 42 };
        var ptr = &ms;
        System.Console.Write(ptr->field);
    }
}
";

            CompileAndVerify(code,
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Skipped,
                expectedOutput: "42");
        }

        [Fact]
        public void GenericRefStructAddressOf_02()
        {
            var code = @"
public ref struct MyStruct<T>
{
    public T field;
}

public class MyClass
{
    public unsafe void M()
    {
        var ms = new MyStruct<object>();
        var ptr = &ms;
    }
}
";

            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (12,19): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('MyStruct<object>')
                //         var ptr = &ms;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&ms").WithArguments("MyStruct<object>").WithLocation(12, 19)
            );
        }

        [Fact]
        public void GenericStructFixedStatement()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class MyClass
{
    public MyStruct<int> ms;
    public static unsafe void Main()
    {
        var c = new MyClass();
        c.ms.field = 42;
        fixed (MyStruct<int>* ptr = &c.ms)
        {
            System.Console.Write(ptr->field);
        }
    }
}
";

            CompileAndVerify(code,
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Skipped,
                expectedOutput: "42");
        }

        [Fact]
        public void GenericStructLocalFixedStatement()
        {
            var code = @"
public struct MyStruct<T>
{
    public T field;
}

public class MyClass
{
    public static unsafe void Main()
    {
        var ms = new MyStruct<int>();
        fixed (int* ptr = &ms.field)
        {
        }
    }
}
";
            CreateCompilation(code, options: TestOptions.UnsafeReleaseDll)
                .VerifyDiagnostics(
                    // (12,27): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                    //         fixed (int* ptr = &ms.field)
                    Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&ms.field").WithLocation(12, 27)
                );
        }
    }
}
