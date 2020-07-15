// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class GenericsTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.RegularPreview;

        [Fact]
        public void MustNotBeGenericMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M<T>() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8798: Module initializer method 'M' must not be generic and must not be contained in a generic type
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotBeContainedInGenericType()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C<T>
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8798: Module initializer method 'M' must not be generic and must not be contained in a generic type
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotBeGenericAndContainedInGenericType()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C<T>
{
    [ModuleInitializer]
    internal static void M<U>() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8816: Module initializer method 'M' must not be generic and must not be contained in a generic type
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Fact]
        public void MustNotBeContainedInGenericTypeWithParametersDeclaredByContainingGenericType()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C<T>
{
    internal class Nested
    {
        [ModuleInitializer]
        internal static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (8,10): error CS8798: Module initializer method 'M' must not be generic and must not be contained in a generic type
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, "ModuleInitializer").WithArguments("M").WithLocation(8, 10)
                );
        }
    }
}
