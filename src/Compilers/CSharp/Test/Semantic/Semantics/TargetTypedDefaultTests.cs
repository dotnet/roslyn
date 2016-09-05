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
class C
{
    static void Main()
    {
        C x1 = default;
        int? x2 = default;
        dynamic x3 = default;
        var x4 = default;
        ITest x5 = default;
    }
}
interface ITest { }
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS8300: Cannot convert default to 'C' because it is a value or nullable type
                //         C x1 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("C").WithLocation(6, 16),
                // (7,19): error CS8300: Cannot convert default to 'int?' because it is a value or nullable type
                //         int? x2 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("int?").WithLocation(7, 19),
                // (8,22): error CS8300: Cannot convert default to 'dynamic' because it is a value or nullable type
                //         dynamic x3 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("dynamic").WithLocation(8, 22),
                // (9,13): error CS0815: Cannot assign default to an implicitly-typed variable
                //         var x4 = default;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x4 = default").WithArguments("default").WithLocation(9, 13),
                // (10,20): error CS8300: Cannot convert default to 'ITest' because it is a value or nullable type
                //         ITest x5 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("ITest").WithLocation(10, 20)
                );
        }

        [Fact]
        public void ResolveMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(int x) { System.Console.Write(""picked"");  }
    static void M(string x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "picked");
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
    static void M(string x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,11): error CS1503: Argument 1: cannot convert from 'default' to 'string'
                //         M(default);
                Diagnostic(ErrorCode.ERR_BadArgType, "default").WithArguments("1", "default", "string").WithLocation(6, 11)
                );
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
    static void M(int? x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,11): error CS1503: Argument 1: cannot convert from 'default' to 'int?'
                //         M(default);
                Diagnostic(ErrorCode.ERR_BadArgType, "default").WithArguments("1", "default", "int?").WithLocation(6, 11)
                );
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
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS0023: Operator '.' cannot be applied to operand of type 'default'
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "default").WithLocation(6, 16)
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
        public void BadReturn()
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
            comp.VerifyDiagnostics(
                // (6,16): error CS8300: Cannot convert default to 'int?' because it is a value or nullable type
                //         return default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("int?").WithLocation(6, 16)
                );
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
        public void Generic()
        {
            string source = @"
class C1<T>
{
    static void M()
    {
        T t = default;
    }
}
class C2<T> where T : struct
{
    static void M()
    {
        T t = default;
    }
}
";
            // PROTOTYPE(default) The constrained T should be ok to infer "default" from

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (13,14): error CS8300: Cannot convert default to 'T' because it is a value or nullable type
                //        T t = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("T").WithLocation(13, 14),
                // (6,14): error CS8300: Cannot convert default to 'T' because it is a value or nullable type
                //        T t = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("T").WithLocation(6, 14)
                );
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
    }
}
