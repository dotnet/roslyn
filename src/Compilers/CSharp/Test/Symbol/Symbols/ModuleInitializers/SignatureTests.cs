// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class SignatureTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.RegularPreview;

        [Fact]
        public void MustNotBeInstanceMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotBeInstanceMethodInInterface()
        {
            string source = @"
using System.Runtime.CompilerServices;

interface i
{
    [ModuleInitializer]
    internal void M1();

    [ModuleInitializer]
    internal void M2() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions, targetFramework: TargetFramework.NetStandardLatest);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M1' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M1").WithLocation(6, 6),
                // (9,6): error CS8815: Module initializer method 'M2' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M2").WithLocation(9, 6)
                );
        }

        [Fact]
        public void MustNotHaveParameters()
        {
            string source = @"
using System.Runtime.CompilerServices;

static class C
{
    [ModuleInitializer]
    internal static void M(object p) { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotHaveOptionalParameters()
        {
            string source = @"
using System.Runtime.CompilerServices;

static class C
{
    [ModuleInitializer]
    internal static void M(object p = null) { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotHaveParamsArrayParameters()
        {
            string source = @"
using System.Runtime.CompilerServices;

static class C
{
    [ModuleInitializer]
    internal static void M(params object[] p) { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotReturnAValue()
        {
            string source = @"
using System.Runtime.CompilerServices;

static class C
{
    [ModuleInitializer]
    internal static object M() => null;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                    // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                    //     [ModuleInitializer]
                    Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MayBeAsyncVoid()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

static class C
{
    [ModuleInitializer]
    internal static async void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Fact]
        public void MayNotReturnAwaitableWithVoidResult()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static class C
{
    [ModuleInitializer]
    internal static async Task M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8815: Module initializer method 'M' must be static, must have no parameters, and must return 'void'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, "ModuleInitializer").WithArguments("M").WithLocation(7, 6),
                // (8,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     internal static async Task M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(8, 32));
        }
    }
}
