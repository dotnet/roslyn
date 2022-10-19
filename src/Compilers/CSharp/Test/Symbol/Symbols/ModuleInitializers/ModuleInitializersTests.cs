// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class ModuleInitializersTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.Regular9;

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
                // (5,6): error CS8400: Feature 'module initializers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "ModuleInitializer").WithArguments("module initializers", "9.0").WithLocation(5, 6)
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular9);

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
                targetFramework: TargetFramework.NetCoreApp,
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
                targetFramework: TargetFramework.NetCoreApp,
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

        [Fact]
        public void ModuleInitializerMethodIsObsolete()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [Obsolete, ModuleInitializer]
    internal static void Init() => Console.WriteLine(""C.Init"");
}

class Program
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.Init
Program.Main");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NetModulesNeedDesktop)]
        public void MultipleNetmodules()
        {
            var moduleOptions = TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All);
            var s1 = @"
using System;
using System.Runtime.CompilerServices;

public class A
{
    [ModuleInitializer]
    public static void M1()
    {
        Console.Write(1);
    }
}

namespace System.Runtime.CompilerServices { public class ModuleInitializerAttribute : System.Attribute { } }";
            var comp1 = CreateCompilation(s1, options: moduleOptions.WithModuleName("A"), parseOptions: s_parseOptions);
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();
            CompileAndVerify(comp1, symbolValidator: validateModuleInitializer, verify: Verification.Skipped);

            var s2 = @"
using System;
using System.Runtime.CompilerServices;

public class B
{
    [ModuleInitializer]
    public static void M2()
    {
        Console.Write(2);
    }
}";
            var comp2 = CreateCompilation(s2, options: moduleOptions.WithModuleName("B"), parseOptions: s_parseOptions, references: new[] { ref1 });
            comp2.VerifyDiagnostics();
            var ref2 = comp2.EmitToImageReference();
            CompileAndVerify(comp2, symbolValidator: validateModuleInitializer, verify: Verification.Skipped);

            var exeOptions = TestOptions.ReleaseExe
                .WithMetadataImportOptions(MetadataImportOptions.All)
                .WithModuleName("C");
            var s3 = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.Write(3);
    }
}";
            var comp3 = CreateCompilation(s3, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp3.VerifyDiagnostics();
            CompileAndVerify(comp3, symbolValidator: validateNoModuleInitializer, expectedOutput: "3");

            var s4 = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        new A();
        new B();
        Console.Write(3);
    }
}";
            var comp4 = CreateCompilation(s4, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp4.VerifyDiagnostics();
            CompileAndVerify(comp4, symbolValidator: validateNoModuleInitializer, expectedOutput: "123");

            var s5 = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        new B();
        Console.Write(3);
        new A();
    }
}";
            var comp5 = CreateCompilation(s5, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp5.VerifyDiagnostics();
            // This order seems surprising, but is likely related to the order in which types are loaded when a method is called.
            CompileAndVerify(comp5, symbolValidator: validateNoModuleInitializer, expectedOutput: "213");

            var s6 = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        new A();
        Console.Write(3);
    }
}";
            var comp6 = CreateCompilation(s6, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp6.VerifyDiagnostics();
            CompileAndVerify(comp6, symbolValidator: validateNoModuleInitializer, expectedOutput: "13");

            var s7 = @"
using System;
using System.Runtime.CompilerServices;

public class Program
{
    [ModuleInitializer]
    public static void Init()
    {
        Console.Write(0);
    }

    public static void Main(string[] args)
    {
        new B();
        Console.Write(3);
    }
}";
            var comp7 = CreateCompilation(s7, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp7.VerifyDiagnostics();
            CompileAndVerify(comp7, symbolValidator: validateModuleInitializer, expectedOutput: "023");

            var s8 = @"
using System;
using System.Runtime.CompilerServices;

public class Program
{
    [ModuleInitializer]
    public static void Init()
    {
        Console.Write(0);
        new A();
    }

    public static void Main(string[] args)
    {
        new A();
        new B();
        Console.Write(3);
    }
}";
            var comp8 = CreateCompilation(s8, options: exeOptions, parseOptions: s_parseOptions, references: new[] { ref1, ref2 });
            comp8.VerifyDiagnostics();
            CompileAndVerify(comp8, symbolValidator: validateModuleInitializer, expectedOutput: "1023");

            void validateModuleInitializer(ModuleSymbol module)
            {
                Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }

            void validateNoModuleInitializer(ModuleSymbol module)
            {
                Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.Null(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NetModulesNeedDesktop)]
        public void NetmoduleFromIL_InitializerNotCalled()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  M1() cil managed
  {
    .custom instance void System.Runtime.CompilerServices.ModuleInitializerAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  call       void [mscorlib]System.Console::Write(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method A::M1

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method A::.ctor

} // end of class A

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.ModuleInitializerAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ModuleInitializerAttribute::.ctor

} // end of class System.Runtime.CompilerServices.ModuleInitializerAttribute";

            var source1 = @"
using System;
using System.Runtime.CompilerServices;

public class A
{
    [ModuleInitializer]
    public static void M1()
    {
        Console.Write(1);
    }

    public static void Main()
    {
        Console.Write(2);
    }
}

namespace System.Runtime.CompilerServices { public class ModuleInitializerAttribute : System.Attribute { } }
";

            var source2 = @"
using System;
using System.Runtime.CompilerServices;

public class A
{
    public static void M1()
    {
        Console.Write(0);
    }

    public static void Main()
    {
        Console.Write(1);
    }
}

namespace System.Runtime.CompilerServices { public class ModuleInitializerAttribute : System.Attribute { } }
";
            var exeOptions = TestOptions.ReleaseExe
                .WithMetadataImportOptions(MetadataImportOptions.All)
                .WithModuleName("C");

            var comp = CreateCompilationWithIL(source1, il, parseOptions: s_parseOptions, options: exeOptions);
            CompileAndVerify(comp, symbolValidator: validateModuleInitializer, verify: Verification.Skipped, expectedOutput: "12");

            comp = CreateCompilationWithIL(source2, il, parseOptions: s_parseOptions, options: exeOptions);
            CompileAndVerify(comp, symbolValidator: validateNoModuleInitializer, verify: Verification.Skipped, expectedOutput: "1");

            void validateModuleInitializer(ModuleSymbol module)
            {
                Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.NotNull(moduleType.GetMember<MethodSymbol>(".cctor"));
            }

            void validateNoModuleInitializer(ModuleSymbol module)
            {
                Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                var moduleType = module.ContainingAssembly.GetTypeByMetadataName("<Module>");
                Assert.Null(moduleType.GetMember<MethodSymbol>(".cctor"));
            }
        }

        [Fact]
        public void MultipleAttributesViaExternAlias()
        {
            var source1 = @"
namespace System.Runtime.CompilerServices { public class ModuleInitializerAttribute : System.Attribute { } }
";

            var ref1 = CreateCompilation(source1).ToMetadataReference(aliases: ImmutableArray.Create("Alias1"));
            var ref2 = CreateCompilation(source1).ToMetadataReference(aliases: ImmutableArray.Create("Alias2"));

            var source = @"
extern alias Alias1;
extern alias Alias2;

using System;

class Program
{
    [Alias1::System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init1()
    {
        Console.Write(1);
    }
    
    [Alias2::System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init2()
    {
        Console.Write(2);
    }
    
    static void Main()
    {
        Console.Write(3);
    }
}
";
            CompileAndVerify(source, parseOptions: s_parseOptions, references: new[] { ref1, ref2 }, expectedOutput: "123");
        }

        [Fact]
        [WorkItem(56412, "https://github.com/dotnet/roslyn/issues/56412")]
        public void Issue56412()
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
                options: TestOptions.ReleaseExe,
                emitOptions: EmitOptions.Default.WithDebugInformationFormat(PathUtilities.IsUnixLikePlatform ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb),
                expectedOutput: @"
C.M
Program.Main");
        }
    }
}
