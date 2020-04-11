// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class ModuleInitializersTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.RegularPreview;

        [Fact]
        public static void ModuleInitializerAttributeIsDecoded()
        {
            var source =
@"using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M1() { }

    internal static void M2() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);

            Assert.True(compilation.GetMember<SourceMethodSymbolWithAttributes>("C.M1").IsModuleInitializer);
            Assert.False(compilation.GetMember<SourceMethodSymbolWithAttributes>("C.M2").IsModuleInitializer);
        }

        [Fact]
        public static void ModuleInitializersNotUsableInCSharp8()
        {
            var source =
@"using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular8);

            compilation.VerifyDiagnostics(
                // (5,6): error CS8652: The feature 'module initializers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ModuleInitializer").WithArguments("module initializers").WithLocation(5, 6)
                );
        }

        [Fact]
        public void ModuleTypeStaticConstructorIsNotEmittedWhenNoMethodIsMarkedWithModuleInitializerAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void ModuleTypeStaticConstructorCallsMethodMarkedWithModuleInitializerAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyIL("<Module>..cctor", @"
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       ""void C.M()""
  IL_0005:  ret
}");
        }
    }
}
