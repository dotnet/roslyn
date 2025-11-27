// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class UnsafeEvolutionTests : CompilingTestBase
{
    private static void VerifyMemorySafetyRulesAttribute(ModuleSymbol module, bool includesAttributeDefinition, bool includesAttributeUse, bool publicDefinition)
    {
        const string name = "MemorySafetyRulesAttribute";
        const string fullName = $"System.Runtime.CompilerServices.{name}";
        var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(fullName);
        var attribute = module.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name == name);

        if (includesAttributeDefinition)
        {
            Assert.NotNull(type);
        }
        else
        {
            Assert.Null(type);
            if (includesAttributeUse)
            {
                Assert.NotNull(attribute);
                type = attribute.AttributeClass;
            }
        }

        if (type is { })
        {
            Assert.Equal(publicDefinition ? Accessibility.Public : Accessibility.Internal, type.DeclaredAccessibility);
        }

        if (includesAttributeUse)
        {
            Assert.NotNull(attribute);
            Assert.Equal(type, attribute.AttributeClass);
            Assert.Equal([2], attribute.ConstructorArguments.Select(a => a.Value));
            Assert.Equal([], attribute.NamedArguments);

            var otherModuleAttributes = module.GetAttributes()
                .Except([attribute])
                .Select(a => a.AttributeClass.ToTestDisplayString());
            Assert.Equal(["System.Runtime.CompilerServices.RefSafetyRulesAttribute"], otherModuleAttributes);
        }
        else
        {
            Assert.Null(attribute);
        }
    }

    [Fact]
    public void RulesAttribute_Synthesized()
    {
        var source = """
            class C;
            """;

        CompileAndVerify(source,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        var ref1 = CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: false))
            .VerifyDiagnostics()
            .GetImageReference();

        CompileAndVerify("", [ref1],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule,
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        CreateCompilation(source,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute' is not defined or imported
            // class C;
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(1, 7));

        source = "System.Console.WriteLine();";

        CompileAndVerify(source,
            parseOptions: TestOptions.Script,
            options: TestOptions.ReleaseModule,
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Script,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute' is not defined or imported
            // System.Console.WriteLine();
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "System.Console.WriteLine();").WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(1, 1));
    }

    [Fact]
    public void RulesAttribute_NotSynthesized()
    {
        var source = """
            [assembly: System.Reflection.AssemblyDescriptionAttribute(null)]
            """;

        CompileAndVerify(source,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_TypeForwardedTo(
        bool updatedRulesA,
        bool updatedRulesB,
        bool useCompilationReference)
    {
        var sourceA = """
            public class A { }
            """;
        var comp = CreateCompilation(sourceA, options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRulesA));
        var refA = AsReference(comp, useCompilationReference);
        Assert.Equal(updatedRulesA, comp.SourceModule.UseUpdatedMemorySafetyRules);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: updatedRulesA, includesAttributeUse: updatedRulesA, publicDefinition: false))
            .VerifyDiagnostics();

        var sourceB = """
            using System.Runtime.CompilerServices;
            [assembly: TypeForwardedTo(typeof(A))]
            """;
        comp = CreateCompilation(sourceB, [refA], options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRulesB));
        Assert.Equal(updatedRulesB, comp.SourceModule.UseUpdatedMemorySafetyRules);
        CompileAndVerify(comp, symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false));
    }

    [Fact]
    public void RulesAttribute_Field()
    {
        var sourceA = """
            using System;
            using System.Linq;
            public class A
            {
                public static int GetAttributeValue(Type type)
                {
                    var module = type.Assembly.Modules.Single();
                    var attribute = module.GetCustomAttributes(false).Single(a => a.GetType().Name == "MemorySafetyRulesAttribute");
                    var field = attribute.GetType().GetField("Version");
                    return (int)field.GetValue(attribute);
                }
            }
            """;
        var refA = CreateCompilation(sourceA,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .EmitToImageReference();

        var sourceB = """
            using System;
            class B : A
            {
                static void Main()
                {
                    Console.Write(GetAttributeValue(typeof(A)));
                    Console.Write(" ");
                    Console.Write(GetAttributeValue(typeof(B)));
                }
            }
            """;
        CompileAndVerify(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules(),
            expectedOutput: "2 2")
            .VerifyDiagnostics();
    }

    [Fact]
    public void RulesAttribute_FromSource()
    {
        var source = """
            class C;
            """;

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true))
            .VerifyDiagnostics();

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: true))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_FromMetadata(bool useCompilationReference)
    {
        var comp = CreateCompilation(MemorySafetyRulesAttributeDefinition);
        CompileAndVerify(comp, symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));
        var ref1 = AsReference(comp, useCompilationReference);

        var source = """
            class C;
            """;

        CompileAndVerify(source, [ref1],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: true, publicDefinition: true))
            .VerifyDiagnostics();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void RulesAttribute_FromMetadata_Version(int version)
    {
        bool correctVersion = version == CSharpCompilationOptions.UpdatedMemorySafetyRulesVersion;

        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{version}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
                .field public int32 Version
            }
            .class public A
            {
                .method public static void M() { ldnull throw }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var sourceB = """
            class B
            {
                void M() => A.M();
            }
            """;
        var comp = CreateCompilation(sourceB, [refA]);
        if (correctVersion)
        {
            comp.VerifyEmitDiagnostics();
        }
        else
        {
            comp.VerifyDiagnostics(
                // (3,17): error CS9103: 'A.M()' is defined in a module with an unrecognized System.Runtime.CompilerServices.MemorySafetyRulesAttribute version, expecting '2'.
                //     void M() => A.M();
                Diagnostic(ErrorCode.ERR_UnrecognizedAttributeVersion, "A.M").WithArguments("A.M()", "System.Runtime.CompilerServices.MemorySafetyRulesAttribute", "2").WithLocation(3, 17));
        }

        var method = comp.GetMember<MethodSymbol>("B.M");
        Assert.False(method.ContainingModule.UseUpdatedMemorySafetyRules);

        // 'A.M' not used => no error.
        CreateCompilation("class C;", references: [refA]).VerifyEmitDiagnostics();
    }

    [Fact]
    public void RulesAttribute_FromMetadata_UnrecognizedConstructor_NoArguments()
    {
        // [module: MemorySafetyRules()]
        var sourceA = """
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor() = ( 01 00 00 00 ) 
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
            }
            .class public A
            {
                .method public static void M() { ldnull throw }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var sourceB = """
            class B
            {
                void M() => A.M();
            }
            """;
        var comp = CreateCompilation(sourceB, [refA]);
        comp.VerifyDiagnostics(
            // (3,17): error CS9103: 'A.M()' is defined in a module with an unrecognized System.Runtime.CompilerServices.MemorySafetyRulesAttribute version, expecting '2'.
            //     void M() => A.M();
            Diagnostic(ErrorCode.ERR_UnrecognizedAttributeVersion, "A.M").WithArguments("A.M()", "System.Runtime.CompilerServices.MemorySafetyRulesAttribute", "2").WithLocation(3, 17));

        var method = comp.GetMember<MethodSymbol>("B.M");
        Assert.False(method.ContainingModule.UseUpdatedMemorySafetyRules);

        // 'A.M' not used => no error.
        CreateCompilation("class C;", references: [refA]).VerifyEmitDiagnostics();
    }

    [Fact]
    public void RulesAttribute_FromMetadata_UnrecognizedConstructor_StringArgument()
    {
        // [module: MemorySafetyRules("2")]
        var sourceA = """
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(string) = {string('2')}
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(string version) cil managed { ret }
            }
            .class public A
            {
                .method public static void M() { ldnull throw }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var sourceB = """
            class B
            {
                void M() => A.M();
            }
            """;
        var comp = CreateCompilation(sourceB, [refA]);
        comp.VerifyDiagnostics(
            // (3,17): error CS9103: 'A.M()' is defined in a module with an unrecognized System.Runtime.CompilerServices.MemorySafetyRulesAttribute version, expecting '2'.
            //     void M() => A.M();
            Diagnostic(ErrorCode.ERR_UnrecognizedAttributeVersion, "A.M").WithArguments("A.M()", "System.Runtime.CompilerServices.MemorySafetyRulesAttribute", "2").WithLocation(3, 17));

        var method = comp.GetMember<MethodSymbol>("B.M");
        Assert.False(method.ContainingModule.UseUpdatedMemorySafetyRules);

        // 'A.M' not used => no error.
        CreateCompilation("class C;", references: [refA]).VerifyEmitDiagnostics();
    }

    [Fact]
    public void RulesAttribute_MissingConstructor()
    {
        var source1 = """
            namespace System.Runtime.CompilerServices
            {
                public sealed class MemorySafetyRulesAttribute : Attribute { }
            }
            """;
        var source2 = """
            class C;
            """;

        CreateCompilation([source1, source2]).VerifyEmitDiagnostics();

        CreateCompilation([source1, source2],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(
            // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute..ctor'
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute", ".ctor").WithLocation(1, 1));

        CreateCompilation([source1, source2],
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute..ctor'
            // class C;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute", ".ctor").WithLocation(1, 7),
            // (3,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute..ctor'
            //     public sealed class MemorySafetyRulesAttribute : Attribute { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "MemorySafetyRulesAttribute").WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute", ".ctor").WithLocation(3, 25));
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_ReferencedInSource(
        bool updatedRules,
        bool useCompilationReference)
    {
        var comp = CreateCompilation(MemorySafetyRulesAttributeDefinition).VerifyDiagnostics();
        var ref1 = AsReference(comp, useCompilationReference);

        var source = """
            using System.Runtime.CompilerServices;
            [assembly: MemorySafetyRules(2)]
            [module: MemorySafetyRules(2)]
            """;

        comp = CreateCompilation(source, [ref1], options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRules));
        comp.VerifyDiagnostics(
            // (3,10): error CS8335: Do not use 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute'. This is reserved for compiler usage.
            // [module: MemorySafetyRules(2)]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "MemorySafetyRules(2)").WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(3, 10));
    }

    [Theory, CombinatorialData]
    public void Pointer_Variable_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Variable_SafeContext_Var()
    {
        var source = """
            var x = GetPointer();
            int* GetPointer() => null;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // var x = GetPointer();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "GetPointer()").WithArguments("updated memory safety rules").WithLocation(1, 9),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* GetPointer() => null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Variable_UnsafeContext()
    {
        var source = """
            unsafe { int* x = null; }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyDiagnostics(
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Pointer_Dereference_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            int y = *x;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe))
            .VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 10));

        var expectedDiagnostics = new[]
        {
            // (2,9): error CS9500: This operation may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 9),
        };

        CreateCompilation(source,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = *x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 10),
            // (2,9): error CS9500: This operation may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 9));
    }

    [Fact]
    public void Pointer_Dereference_SafeContext_Null()
    {
        var source = """
            int x = *null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,9): error CS0193: The * or -> operator must be applied to a pointer
            // int x = *null;
            Diagnostic(ErrorCode.ERR_PtrExpected, "*null").WithLocation(1, 9),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_Dereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { int y = *x; }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_MemberAccess_SafeContext()
    {
        var source = """
            int* x = null;
            string s = x->ToString();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 12));

        var expectedDiagnostics = new[]
        {
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "->").WithLocation(2, 13),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 12),
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "->").WithLocation(2, 13));
    }

    [Fact]
    public void Pointer_MemberAccess_SafeContext_Null()
    {
        var source = """
            string s = null->ToString();
            """;

        var expectedDiagnostics = new[]
        {
            // (1,12): error CS0193: The * or -> operator must be applied to a pointer
            // string s = null->ToString();
            Diagnostic(ErrorCode.ERR_PtrExpected, "null->ToString").WithLocation(1, 12),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_MemberAccess_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { string s = x->ToString(); }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_MemberAccessViaDereference_SafeContext()
    {
        var source = """
            int* x = null;
            string s = (*x).ToString();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 14));

        var expectedDiagnostics = new[]
        {
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 13),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 14),
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 13));
    }

    [Fact]
    public void Pointer_MemberAccessViaDereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { string s = (*x).ToString(); }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext()
    {
        var source = """
            int* x = null;
            x[0] = 1;
            int y = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 9));

        var expectedDiagnostics = new[]
        {
            // (2,2): error CS9500: This operation may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(2, 2),
            // (3,10): error CS9500: This operation may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 10),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,2): error CS9500: This operation may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(2, 2),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,10): error CS9500: This operation may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 10));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_MultipleIndices()
    {
        var source = """
            int* x = null;
            x[0, 1] = 1;
            int y = x[2, 3];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 9),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9));

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_ArrayOfPointers()
    {
        var source = """
            int*[] x = [];
            x[0] = null;
            _ = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int*[] x = [];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0]").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0] = null").WithLocation(2, 1),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 5),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[1]").WithLocation(3, 5),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "_ = x[1]").WithLocation(3, 1));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int*[] x = [];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[0]").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[0] = null").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[1]").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "_ = x[1]").WithArguments("updated memory safety rules").WithLocation(3, 1));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_FunctionPointer()
    {
        var source = """
            delegate*<void> x = null;
            x[0] = null;
            _ = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<void> x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 5),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5));

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<void> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5));
    }

    [Fact]
    public void Pointer_ElementAccess_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe
            {
                x[0] = 1;
                int y = x[1];
            }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }
}
