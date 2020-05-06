// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed partial class ModuleInitializersTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.RegularPreview;

        [Fact]
        public static void LastLanguageVersionNotSupportingModuleInitializersIs8()
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
        public static void FirstLanguageVersionSupportingModuleInitializersIs9()
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void ModuleTypeStaticConstructorIsNotEmittedWhenNoMethodIsMarkedWithModuleInitializerAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    internal static void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";

            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                },
                expectedOutput: @"
Program.Main");
        }

        [Fact]
        public void ModuleTypeStaticConstructorCallsMethodMarkedWithModuleInitializerAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";

            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    var staticConstructor = (PEMethodSymbol)rootModuleType.GetMember(".cctor");

                    Assert.NotNull(staticConstructor);
                    Assert.Equal(MethodKind.StaticConstructor, staticConstructor.MethodKind);

                    var expectedFlags =
                        MethodAttributes.Private
                        | MethodAttributes.Static
                        | MethodAttributes.SpecialName
                        | MethodAttributes.RTSpecialName
                        | MethodAttributes.HideBySig;

                    Assert.Equal(expectedFlags, staticConstructor.Flags);
                },
                expectedOutput: @"
C.M
Program.Main");
        }

        [Fact]
        public void SingleCallIsGeneratedWhenMethodIsMarkedTwice()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer, ModuleInitializer]
    internal static void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() { }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class ModuleInitializerAttribute : System.Attribute { } 
}
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: "C.M");
        }

        [Fact]
        public void AttributeCanBeAppliedWithinItsOwnDefinition()
        {
            string source = @"
using System;

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices
{
    class ModuleInitializerAttribute : System.Attribute 
    { 
        [ModuleInitializer]
        internal static void M() => Console.WriteLine(""ModuleInitializerAttribute.M"");
    } 
}
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
ModuleInitializerAttribute.M
Program.Main");
        }

        [Fact]
        public void ExternMethodCanBeModuleInitializer()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    [ModuleInitializer, DllImport(""dllName"")]
    internal static extern void M();
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions);
        }
    }
}
