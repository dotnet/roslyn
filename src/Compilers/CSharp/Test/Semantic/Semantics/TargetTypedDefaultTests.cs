// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.TargetTypedDefault)]
    public class TargetTypedDefaultTests : CompilingTestBase
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
                // (6,17): error CS8058: Feature 'target-typed default operator' is experimental and unsupported; use '/features:targetTypedDefault' to enable.
                //         int x = default;
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "default").WithArguments("target-typed default operator", "targetTypedDefault").WithLocation(6, 17)
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
        public void AssignmentToRef()
        {
            string source = @"
class C
{
    static void Main()
    {
        C x1 = default;
        int? x2 = default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS8300: Cannot convert default to 'C' because it is a value or nullable type
                //         C x1 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("C").WithLocation(6, 16),
                // (7,19): error CS8300: Cannot convert default to 'int?' because it is a value or nullable type
                //         int? x2 = default;
                Diagnostic(ErrorCode.ERR_RefCantBeDefault, "default").WithArguments("int?").WithLocation(7, 19)
                );
        }
    }
}
