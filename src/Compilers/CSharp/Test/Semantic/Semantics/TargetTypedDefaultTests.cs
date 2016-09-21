// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.DefaultLiteral)]
    public class DefaultLiteralTests : CompilingTestBase
    {
        [Fact]
        public void TestCSharp7()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8058: Feature 'target-typed default operator' is experimental and unsupported; use '/features:defaultLiteral' to enable.
                //         int x = default;
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "default").WithArguments("target-typed default operator", "defaultLiteral").WithLocation(6, 17)
                );
        }

        [Fact]
        public void AssignmentToInt()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = default;
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void BadAssignment()
        {
            string source = @"
class C<T>
{
    static void M()
    {
        var x4 = default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (6,13): error CS0815: Cannot assign default to an implicitly-typed variable
                //         var x4 = default;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x4 = default").WithArguments("default").WithLocation(6, 13)
                );
        }

        [Fact]
        public void AssignmentToRefType()
        {
            string source = @"
class C<T> where T : class
{
    static void M()
    {
        C<string> x1 = default;
        int? x2 = default;
        dynamic x3 = default;
        ITest x5 = default;
        T x6 = default;
    }
}
interface ITest { }
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (7,14): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         int? x2 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(7, 14)
                );
        }

        [Fact]
        public void AmbiguousMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(int x) { }
    static void M(string x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(int)' and 'C.M(string)'
                //         M(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(int)", "C.M(string)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void MethodWithRefParameters()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(string x) { System.Console.Write(x == null ? ""null"" : ""bad""); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void MethodWithNullableParameters()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(int? x) { System.Console.Write(x.HasValue ? ""bad"" : ""null""); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void CannotInferTypeArg()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M<T>(T x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void CannotInferTypeArg2()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default, null);
    }
    static void M<T>(T x, T y) where T : class { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(default, null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void InvocationOnDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        default.ToString();
        default[0].ToString();
        System.Console.Write(nameof(default));
        throw default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS0023: Operator '.' cannot be applied to operand of type 'default'
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "default").WithLocation(6, 16),
                // (7,9): error CS0021: Cannot apply indexing with [] to an expression of type 'default'
                //         default[0].ToString();
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default[0]").WithArguments("default").WithLocation(7, 9),
                // (8,37): error CS8081: Expression does not have a name.
                //         System.Console.Write(nameof(default));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "default").WithLocation(8, 37),
                // (9,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         throw default;
                Diagnostic(ErrorCode.ERR_BadExceptionType, "default").WithLocation(9, 15)
                );
        }

        [Fact]
        public void Cast()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = (int)default;
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void ImplicitlyTypedArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new[] { 1, default };
        System.Console.Write(t[1]);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
            // PROTOTYPE(default) Verify semantic model
        }

        [Fact]
        public void FailedImplicitlyTypedArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new[] { default, default };
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var t = new[] { default, default };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { default, default }").WithLocation(6, 17)
                );
        }

        [Fact]
        public void Tuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int, int) t = (1, default);
        System.Console.Write(t.Item2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe,
                        references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TypeInferenceSucceeds()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default, 1);
    }
    static void M<T>(T x, T y) { System.Console.Write(x); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void DefaultIdentifier()
        {
            string source = @"
class C
{
    static void Main()
    {
        int @default = 2;
        int x = default;
        System.Console.Write($""{x} {@default}"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 2");
        }

        [Fact]
        public void Return()
        {
            string source = @"
class C
{
    static int M()
    {
        return default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReturnNullableType()
        {
            string source = @"
class C
{
    static int? M()
    {
        return default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(default)")]
        public void ConstAndProperty()
        {
            string source = @"
class C
{
    const int x = default;
    static int P { get { return default; } }
    static void Main()
    {
        System.Console.Write($""{x} {P}"");
    }
}
";
            // PROTOTYPE(default) There is a problem with treating default literal as constant (should the constant value in the literal or in the conversion, or somewhere else?)
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 0");
        }

        [Fact(Skip = "PROTOTYPE(default)")]
        public void DynamicInvocation()
        {
            string source = @"
class C
{
    static void M1()
    {
        dynamic d = null;
        d.M2(default);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
            // PROTOTYPE(default) Crash
        }

        [Fact]
        public void IndexingIntoArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        int[] x = { 1, 2 };
        System.Console.Write(x[default]);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void Lambda()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write(M()());
    }
    static System.Func<int> M()
    {
        return () => default;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact(Skip = "PROTOTYPE(default)")]
        public void Switch()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 1;
        switch (x)
        {
            case default: // PROTOTYPE(default) default is not recognized as constant
            default:
                break;
            case 1:
                System.Console.Write(""1"");
                break;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void BinaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 0;
        if (x == default)
        {
            System.Console.Write(""0"");
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void OptionalParameter()
        {
            string source = @"
class C
{
    static void Main()
    {
        M();
    }
    static void M(int x = default)
    {
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void ArraySize()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = new int[default];
        System.Console.Write(a.Length);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TernaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        var x = flag ? default : 1;
        System.Console.Write($""{x} {x.GetType().ToString()}"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 System.Int32");
        }

        [Fact]
        public void RefTernaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        var x = flag ? default : ""hello"";
        System.Console.Write(x == null ? ""null"" : ""bad"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void ExplicitCast()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = (short)default;
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
            // PROTOTYPE(default) Verify semantic model. What is the type of default?
        }

        [Fact]
        public void NotAType()
        {
            string source = @"
class C
{
    static void Main()
    {
        default(System).ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0118: 'System' is a namespace but is used like a type
                //         default(System).ToString();
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "type").WithLocation(6, 17)
                );
        }
    }
}
