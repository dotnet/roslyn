using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
    public void M()
    {
        var a = new Test<E1>();             // enum
        var b = new Test<int>();            // value type
        var c = new Test<string>();         // reference type
        var d = new Test<System.Enum>();    // Enum type
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
        public void EnumConstraint_InheritenceChain()
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
    }
}").VerifyDiagnostics(
                // (13,33): error CS0315: The type 'E' cannot be used as type parameter 'U' in the generic type or method 'Test<T, U>'. There is no boxing conversion from 'E' to 'Test2'.
                //         var a = new Test<Test2, E>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "E").WithArguments("Test<T, U>", "Test2", "U", "E").WithLocation(13, 33));
        }
    }
}
