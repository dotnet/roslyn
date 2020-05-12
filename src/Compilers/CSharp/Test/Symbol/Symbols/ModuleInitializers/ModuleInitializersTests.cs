// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class ModuleInitializersTests : CSharpTestBase
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
            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                    Assert.NotNull(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void MayBeDeclaredByStruct()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

struct S
{
    [ModuleInitializer]
    internal static void M() => Console.WriteLine(""S.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
S.M
Program.Main");
        }

        [Fact]
        public void MayBeDeclaredByInterface()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

interface I
{
    [ModuleInitializer]
    internal static void M() => Console.WriteLine(""I.M"");
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
                targetFramework: TargetFramework.NetStandardLatest,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"
I.M
Program.Main" : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped);
        }

        [Fact]
        public void MultipleInitializers_SingleFile()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void M1() => Console.Write(1);

    internal class C2
    {
        [ModuleInitializer]
        internal static void M2() => Console.Write(2);
    }

    [ModuleInitializer]
    internal static void M3() => Console.Write(3);
}

class Program 
{
    static void Main() => Console.Write(4);
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";

            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                expectedOutput: "1234");
        }

        [Fact]
        public void MultipleInitializers_DifferentContainingTypeKinds()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void M1() => Console.Write(1);
}

struct S1
{
    [ModuleInitializer]
    internal static void M2() => Console.Write(2);
}

interface I1
{
    [ModuleInitializer]
    internal static void M3() => Console.Write(3);
}

class Program 
{
    static void Main() => Console.Write(4);
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";

            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                targetFramework: TargetFramework.NetStandardLatest,
                expectedOutput: !ExecutionConditionUtil.IsMonoOrCoreClr ? null : "1234",
                verify: !ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Skipped : Verification.Passes);
        }

        [Fact]
        public void MultipleInitializers_MultipleFiles()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void M1() => Console.Write(1);
    [ModuleInitializer]
    internal static void M2() => Console.Write(2);
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            string source2 = @"
using System;
using System.Runtime.CompilerServices;

class C2
{
    internal class C3
    {
        [ModuleInitializer]
        internal static void M3() => Console.Write(3);
    }

    [ModuleInitializer]
    internal static void M4() => Console.Write(4);
}

class Program 
{
    static void Main() => Console.Write(6);
}
";

            string source3 = @"
using System;
using System.Runtime.CompilerServices;

class C4
{
    // shouldn't be called
    internal static void M() => Console.Write(0);

    [ModuleInitializer]
    internal static void M5() => Console.Write(5);
}
";

            CompileAndVerify(
                new[] { source1, source2, source3 },
                parseOptions: s_parseOptions,
                expectedOutput: "123456");
        }

        [Fact]
        public void StaticConstructor_Ordering()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void Init() => Console.Write(1);
}

class C2
{
    static C2() => Console.Write(2);

    static void Main()
    {
        Console.Write(3);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(text, parseOptions: s_parseOptions, expectedOutput: "123");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void StaticConstructor_Ordering_SameType()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    static C() => Console.Write(1);

    [ModuleInitializer]
    internal static void Init() => Console.Write(2);

    static void Main()
    {
        Console.Write(3);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(text, parseOptions: s_parseOptions, expectedOutput: "123");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void StaticConstructor_DefaultInitializer_SameType()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    internal static string s1 = null;

    [ModuleInitializer]
    internal static void Init()
    {
        s1 = ""hello"";
    }

    static void Main()
    {
        Console.Write(s1);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                expectedOutput: "hello",
                symbolValidator: validator);
            verifier.VerifyDiagnostics();

            void validator(ModuleSymbol module)
            {
                var cType = module.ContainingAssembly.GetTypeByMetadataName("C");
                // static constructor should be optimized out
                Assert.Null(cType.GetMember<MethodSymbol>(".cctor"));

                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [Fact]
        public void StaticConstructor_EffectingInitializer_SameType()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    internal static int i = InitField();

    internal static int InitField()
    {
        Console.Write(1);
        return -1;
    }

    [ModuleInitializer]
    internal static void Init()
    {
        i = 2;
    }

    static void Main()
    {
        Console.Write(i);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                expectedOutput: "12",
                symbolValidator: validator);
            verifier.VerifyDiagnostics();

            void validator(ModuleSymbol module)
            {
                var cType = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.NotNull(cType.GetMember<MethodSymbol>(".cctor"));

                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [Fact]
        public void StaticConstructor_DefaultInitializer_OtherType()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void Init()
    {
        C2.s1 = ""hello"";
    }
}

class C2
{
    internal static string s1 = null;

    static void Main()
    {
        Console.Write(s1);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                expectedOutput: "hello",
                symbolValidator: validator);
            verifier.VerifyDiagnostics();

            void validator(ModuleSymbol module)
            {
                var c2Type = module.ContainingAssembly.GetTypeByMetadataName("C2");
                // static constructor should be optimized out
                Assert.Null(c2Type.GetMember<MethodSymbol>(".cctor"));

                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [Fact]
        public void StaticConstructor_EffectingInitializer_OtherType()
        {
            const string text = @"
using System;
using System.Runtime.CompilerServices;

class C1
{
    [ModuleInitializer]
    internal static void Init()
    {
        C2.i = 2;
    }
}

class C2
{
    internal static int i = InitField();

    static int InitField()
    {
        Console.Write(1);
        return -1;
    }

    static void Main()
    {
        Console.Write(i);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                expectedOutput: "12",
                symbolValidator: validator);
            verifier.VerifyDiagnostics();

            void validator(ModuleSymbol module)
            {
                var c2Type = module.ContainingAssembly.GetTypeByMetadataName("C2");
                Assert.NotNull(c2Type.GetMember<MethodSymbol>(".cctor"));

                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [Fact]
        public void ModuleInitializerAttributeIncludedByConditionalAttribute()
        {
            string source = @"
#define INCLUDE

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

namespace System.Runtime.CompilerServices
{
    [System.Diagnostics.Conditional(""INCLUDE"")]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Fact]
        public void ModuleInitializerAttributeExcludedByConditionalAttribute()
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

namespace System.Runtime.CompilerServices
{
    [System.Diagnostics.Conditional(""EXCLUDE"")]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Fact]
        public void ModuleInitializerMethodIncludedByConditionalAttribute()
        {
            string source = @"
#define INCLUDE

using System;
using System.Runtime.CompilerServices;

class C
{
    [System.Diagnostics.Conditional(""INCLUDE""), ModuleInitializer]
    internal static void Preceding() => Console.WriteLine(""C.Preceding"");

    [ModuleInitializer, System.Diagnostics.Conditional(""INCLUDE"")]
    internal static void Following() => Console.WriteLine(""C.Following"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.Preceding
C.Following
Program.Main");
        }

        [Fact]
        public void ModuleInitializerMethodExcludedByConditionalAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [System.Diagnostics.Conditional(""EXCLUDE""), ModuleInitializer]
    internal static void Preceding() { }

    [ModuleInitializer, System.Diagnostics.Conditional(""EXCLUDE"")]
    internal static void Following() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }
    }
}
