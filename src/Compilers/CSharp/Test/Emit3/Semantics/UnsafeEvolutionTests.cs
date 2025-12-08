// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Unsafe)]
public sealed class UnsafeEvolutionTests : CompilingTestBase
{
    private void CompileAndVerify(
        string lib,
        string caller,
        object[] unsafeSymbols,
        object[] safeSymbols,
        params DiagnosticDescription[] expectedDiagnostics)
    {
        CreateCompilation([lib, caller],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        var libUpdated = CompileAndVerify(lib,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: module =>
            {
                VerifyMemorySafetyRulesAttribute(module, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true);
                VerifyRequiresUnsafeAttribute(module, includesAttributeDefinition: true, isSynthesized: true, unsafeSymbols: unsafeSymbols, safeSymbols: safeSymbols);
            })
            .VerifyDiagnostics()
            .GetImageReference();

        CreateCompilation(caller, [libUpdated],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        var libLegacy = CreateCompilation(lib,
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics()
            .EmitToImageReference();

        CreateCompilation(caller, [libLegacy],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    private static Func<ModuleSymbol, Symbol> ExtensionMember(string containerName, string memberName)
    {
        return module => module.GlobalNamespace
            .GetMember<NamedTypeSymbol>(containerName)
            .GetMembers("")
            .Cast<NamedTypeSymbol>()
            .SelectMany(block => block.GetMembers(memberName))
            .SingleOrDefault()
            ?? throw new InvalidOperationException($"Cannot find '{containerName}.{memberName}'.");
    }

    private static void VerifyMemorySafetyRulesAttribute(
        ModuleSymbol module,
        bool includesAttributeDefinition,
        bool includesAttributeUse,
        bool? isSynthesized = null)
    {
        const string Name = "MemorySafetyRulesAttribute";
        const string FullName = $"System.Runtime.CompilerServices.{Name}";
        var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(FullName);
        var attribute = module.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name == Name);

        if (includesAttributeDefinition)
        {
            Assert.NotNull(type);

            Assert.NotNull(isSynthesized);
            if (isSynthesized.Value)
            {
                var attributeAttributes = type.GetAttributes()
                    .Select(a => a.AttributeClass.ToTestDisplayString())
                    .OrderBy(StringComparer.Ordinal);
                Assert.Equal(
                    [
                        "Microsoft.CodeAnalysis.EmbeddedAttribute",
                        "System.AttributeUsageAttribute",
                        "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                    ],
                    attributeAttributes);
            }
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
            Assert.NotNull(isSynthesized);
            Assert.Equal(isSynthesized.Value ? Accessibility.Internal : Accessibility.Public, type.DeclaredAccessibility);
        }
        else
        {
            Assert.Null(isSynthesized);
        }

        if (includesAttributeUse)
        {
            Assert.NotNull(attribute);
            Assert.Equal(type, attribute.AttributeClass);
            Assert.Equal([2], attribute.ConstructorArguments.Select(a => a.Value));
            Assert.Equal([], attribute.NamedArguments);
        }
        else
        {
            Assert.Null(attribute);
        }
    }

    private static void VerifyRequiresUnsafeAttribute(
        ModuleSymbol module,
        bool includesAttributeDefinition,
        ReadOnlySpan<object> unsafeSymbols,
        ReadOnlySpan<object> safeSymbols,
        bool? isSynthesized = null)
    {
        const string Name = "RequiresUnsafeAttribute";
        const string FullName = $"System.Runtime.CompilerServices.{Name}";
        var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(FullName);

        if (includesAttributeDefinition)
        {
            Assert.NotNull(type);

            Assert.NotNull(isSynthesized);
            Assert.Equal(isSynthesized.Value ? Accessibility.Internal : Accessibility.Public, type.DeclaredAccessibility);

            if (isSynthesized.Value)
            {
                var attributeAttributes = type.GetAttributes()
                    .Select(a => a.AttributeClass.ToTestDisplayString())
                    .OrderBy(StringComparer.Ordinal);
                Assert.Equal(
                    [
                        "Microsoft.CodeAnalysis.EmbeddedAttribute",
                        "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                    ],
                    attributeAttributes);
            }
        }
        else
        {
            Assert.Null(type);
            Assert.Null(isSynthesized);
        }

        var seenSymbols = new HashSet<Symbol>();

        foreach (var symbol in unsafeSymbols)
        {
            verifySymbol(symbol, shouldBeUnsafe: true);
        }

        foreach (var symbol in safeSymbols)
        {
            verifySymbol(symbol, shouldBeUnsafe: false);
        }

        void verifySymbol(object symbolGetter, bool shouldBeUnsafe)
        {
            var symbol = symbolGetter switch
            {
                string symbolName => module.GlobalNamespace.GetMember(symbolName),
                Func<ModuleSymbol, Symbol> func => func(module),
                _ => throw ExceptionUtilities.UnexpectedValue(symbolGetter),
            };
            Assert.False(symbol is null, $"Cannot find symbol '{symbolGetter}'");

            var attribute = symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name == Name);
            Assert.True(attribute is null, $"Attribute should not be exposed by '{symbol.ToTestDisplayString()}'");

            Assert.True(shouldBeUnsafe == symbol.IsCallerUnsafe, $"Expected '{symbol.ToTestDisplayString()}' to be unsafe");

            Assert.True(seenSymbols.Add(symbol), $"Symbol '{symbol.ToTestDisplayString()}' specified multiple times.");
        }
    }

    [Fact]
    public void RulesAttribute_Synthesized()
    {
        var source = """
            class C;
            """;

        CompileAndVerify(source,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false))
            .VerifyDiagnostics();

        var ref1 = CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true))
            .VerifyDiagnostics()
            .GetImageReference();

        CompileAndVerify("", [ref1],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true))
            .VerifyDiagnostics();

        var source2 = """
            class B;
            """;

        CompileAndVerify(source2, [ref1],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule,
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false))
            .VerifyDiagnostics();

        CreateCompilation(source,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(1, 1));

        // Script compilation.
        source = "System.Console.WriteLine();";

        CompileAndVerify(source,
            parseOptions: TestOptions.Script,
            options: TestOptions.ReleaseModule,
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false))
            .VerifyDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Script,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(1, 1));

        // No types and members in the compilation, but the attribute is still synthesized if updated rules are enabled.
        source = """
            [assembly: System.Reflection.AssemblyDescriptionAttribute(null)]
            """;

        CompileAndVerify(source,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true))
            .VerifyDiagnostics();

        CreateCompilation(source,
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute").WithLocation(1, 1));
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
        var comp = CreateCompilation(sourceA,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRulesA))
            .VerifyDiagnostics();
        var refA = AsReference(comp, useCompilationReference);
        Assert.Equal(updatedRulesA, comp.SourceModule.UseUpdatedMemorySafetyRules);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: updatedRulesA, includesAttributeUse: updatedRulesA, isSynthesized: updatedRulesA ? true : null))
            .VerifyDiagnostics();

        var sourceB = """
            using System.Runtime.CompilerServices;
            [assembly: TypeForwardedTo(typeof(A))]
            """;
        comp = CreateCompilation(sourceB, [refA], options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRulesB));
        Assert.Equal(updatedRulesB, comp.SourceModule.UseUpdatedMemorySafetyRules);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: updatedRulesB, includesAttributeUse: updatedRulesB, isSynthesized: updatedRulesB ? true : null))
            .VerifyDiagnostics();
    }

    [Fact]
    public void RulesAttribute_Reflection()
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
                    var prop = attribute.GetType().GetProperty("Version");
                    return (int)prop.GetValue(attribute);
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
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, isSynthesized: false))
            .VerifyDiagnostics();

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: false))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_FromMetadata(bool useCompilationReference)
    {
        var comp = CreateCompilation(MemorySafetyRulesAttributeDefinition);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, isSynthesized: false))
            .VerifyDiagnostics();
        var ref1 = AsReference(comp, useCompilationReference);

        var source = """
            class C;
            """;

        CompileAndVerify(source, [ref1],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: true, isSynthesized: false))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_FromMetadata_Multiple(bool useCompilationReference)
    {
        var comp1 = CreateCompilation(MemorySafetyRulesAttributeDefinition).VerifyDiagnostics();
        var ref1 = AsReference(comp1, useCompilationReference);

        var comp2 = CreateCompilation(MemorySafetyRulesAttributeDefinition).VerifyDiagnostics();
        var ref2 = AsReference(comp2, useCompilationReference);

        var source = """
            class C;
            """;

        // Ambiguous attribute definitions from references => synthesize our own.
        CompileAndVerify(source, [ref1, ref2],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true))
            .VerifyDiagnostics();

        // Also defined in source.
        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition], [ref1, ref2],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: false))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RulesAttribute_FromMetadata_Multiple_AndCorLib(bool useCompilationReference)
    {
        var corlibSource = """
            namespace System
            {
                public class Object;
                public class ValueType;
                public class Attribute;
                public struct Void;
                public struct Int32;
                public struct Boolean;
                public class AttributeUsageAttribute
                {
                    public AttributeUsageAttribute(AttributeTargets t) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
                public class Enum;
                public enum AttributeTargets;
            }
            """;

        var corlib = CreateEmptyCompilation([corlibSource, MemorySafetyRulesAttributeDefinition]).VerifyDiagnostics();
        var corlibRef = AsReference(corlib, useCompilationReference);

        var comp1 = CreateEmptyCompilation(MemorySafetyRulesAttributeDefinition, [corlibRef]).VerifyDiagnostics();
        var ref1 = AsReference(comp1, useCompilationReference);

        var comp2 = CreateEmptyCompilation(MemorySafetyRulesAttributeDefinition, [corlibRef]).VerifyDiagnostics();
        var ref2 = AsReference(comp2, useCompilationReference);

        var source = """
            class C;
            """;

        // Using the attribute from corlib even if there are ambiguous definitions in other references.
        var verifier = CompileAndVerify(CreateEmptyCompilation(source, [ref1, ref2, corlibRef],
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules()),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: true, isSynthesized: false));

        verifier.Diagnostics.WhereAsArray(d => d.Code != (int)ErrorCode.WRN_NoRuntimeMetadataVersion).Verify();

        var comp = (CSharpCompilation)verifier.Compilation;
        Assert.Same(comp.Assembly.CorLibrary, comp.GetReferencedAssemblySymbol(corlibRef));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2, true)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void RulesAttribute_FromMetadata_Version(int version, bool correctVersion = false)
    {
        // [module: MemorySafetyRules({version})]
        // public class A { public static void M() => throw null; }
        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{version}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
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

    [Theory]
    [InlineData(2, 0, true)]
    [InlineData(0, 2, false)]
    public void RulesAttribute_FromMetadata_Version_Multiple(int version1, int version2, bool correctVersion)
    {
        // [module: MemorySafetyRules({version1})]
        // [module: MemorySafetyRules({version2})]
        // public class A { public static void M() => throw null; }
        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{version1}}) }
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{version2}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
            }
            .class public A
            {
                .method public static void M() { ldnull throw }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var a = CreateCompilation("", [refA]).GetReferencedAssemblySymbol(refA);
        Assert.Equal(correctVersion, a.Modules.Single().UseUpdatedMemorySafetyRules);

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
        // public class A { public static void M() => throw null; }
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
        // public class A { public static void M() => throw null; }
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
            // (1,1): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.MemorySafetyRulesAttribute..ctor'
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.MemorySafetyRulesAttribute", ".ctor").WithLocation(1, 1));
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
    public void Pointer_Variable_SafeContext_InIterator()
    {
        var source = """
            unsafe
            {
                M();
                System.Collections.Generic.IEnumerable<int> M()
                {
                    int* p = null;
                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 9));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (6,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(6, 9));

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "int*").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 9),
        };

        CreateCompilation(source,
            parseOptions: TestOptions.Regular12,
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular12,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_Variable_UnsafeContext()
    {
        var source = """
            unsafe { int* x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Variable_UsingAlias_SafeContext()
    {
        var source = """
            using X = int*;
            X x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = int*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 11),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // X x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(2, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using X = int*;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 11),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // X x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Variable_UsingAlias_UnsafeContext()
    {
        var source = """
            using unsafe X = int*;
            unsafe { X x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,7): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // using unsafe X = int*;
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 7),
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { X x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

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
            // (2,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = *x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "*").WithArguments("updated memory safety rules").WithLocation(2, 9));
    }

    [Fact]
    public void Pointer_Dereference_SafeContext_InIterator()
    {
        var source = """
            unsafe
            {
                M();
                System.Collections.Generic.IEnumerable<int> M()
                {
                    int* p = null;
                    int y = *p;
                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 9),
            // (7,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         int y = *p;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(7, 18));

        var expectedDiagnostics = new[]
        {
            // (7,17): error CS9500: This operation may only be used in an unsafe context
            //         int y = *p;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(7, 17),
        };

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (6,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(6, 9),
            // (7,18): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         int y = *p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(7, 18),
            // (7,17): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         int y = *p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "*").WithArguments("updated memory safety rules").WithLocation(7, 17));

        expectedDiagnostics =
        [
            // (6,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "int*").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 9),
            // (7,18): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         int y = *p;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "p").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 18),
        ];

        CreateCompilation(source,
            parseOptions: TestOptions.Regular12,
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular12,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
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
            // (2,13): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "->").WithArguments("updated memory safety rules").WithLocation(2, 13));
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
            // (2,13): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "*").WithArguments("updated memory safety rules").WithLocation(2, 13));
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
            // (2,2): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("updated memory safety rules").WithLocation(2, 2),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("updated memory safety rules").WithLocation(3, 10));
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

    [Fact]
    public void Pointer_Function_Variable_SafeContext()
    {
        var source = """
            delegate*<void> f = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<void> f = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<void> f = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Function_Variable_UnsafeContext()
    {
        var source = """
            unsafe { delegate*<void> f = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { delegate*<void> f = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Function_Variable_UsingAlias_SafeContext()
    {
        var source = """
            using X = delegate*<void>;
            X x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 11),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // X x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(2, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        // https://github.com/dotnet/roslyn/issues/77389
        expectedDiagnostics = PlatformInformation.IsWindows
            ? [
                // error CS8911: Using a function pointer type in this context is not supported.
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
            ]
            : [];

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 11),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // X x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Function_Variable_UsingAlias_UnsafeContext()
    {
        var source = """
            using unsafe X = delegate*<void>;
            unsafe { X x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,7): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // using unsafe X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 7),
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { X x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        // https://github.com/dotnet/roslyn/issues/77389
        expectedDiagnostics = PlatformInformation.IsWindows
            ? [
                // error CS8911: Using a function pointer type in this context is not supported.
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
            ]
            : [];

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_Function_Call_SafeContext()
    {
        var source = """
            delegate*<string> x = null;
            string s = x();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
            // (2,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x()").WithLocation(2, 12));

        var expectedDiagnostics = new[]
        {
            // (2,12): error CS9500: This operation may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "x()").WithLocation(2, 12),
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
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x()").WithArguments("updated memory safety rules").WithLocation(2, 12));
    }

    [Fact]
    public void Pointer_Function_Call_UnsafeContext()
    {
        var source = """
            delegate*<string> x = null;
            unsafe { string s = x(); }
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
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_AddressOf_SafeContext()
    {
        var source = """
            int x;
            int* p = &x;
            """;

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(2, 10),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "&x").WithArguments("updated memory safety rules").WithLocation(2, 10));
    }

    [Fact]
    public void Pointer_AddressOf_SafeContext_Const()
    {
        var source = """
            const int x = 1;
            int* p = &x;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11));

        var expectedDiagnostics = new[]
        {
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11));
    }

    [Fact]
    public void Pointer_AddressOf_UnsafeContext()
    {
        var source = """
            int x;
            unsafe { int* p = &x; }
            """;

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* p = &x; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Fixed_SafeContext()
    {
        var source = """
            class C
            {
                static int x;
                static void Main()
                {
                    fixed (int* p = &x) { }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = &x) { }").WithLocation(6, 9),
            // (6,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 16),
            // (6,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(6, 25),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (6,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = &x) { }").WithArguments("updated memory safety rules").WithLocation(6, 9),
            // (6,16): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(6, 16),
            // (6,25): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "&x").WithArguments("updated memory safety rules").WithLocation(6, 25));
    }

    [Fact]
    public void Pointer_Fixed_PatternBased()
    {
        var source = """
            class C
            {
                static void Main()
                {
                    fixed (int* p = new S()) { }
                }
            }

            struct S
            {
                public ref readonly int GetPinnableReference() => throw null;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (5,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = new S()) { }").WithLocation(5, 9),
            // (5,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(5, 16),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (5,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = new S()) { }").WithArguments("updated memory safety rules").WithLocation(5, 9),
            // (5,16): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(5, 16));
    }

    [Fact]
    public void Pointer_Fixed_SafeContext_AlreadyFixed()
    {
        var source = """
            int x;
            fixed (int* p = &x) { }
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = &x) { }").WithLocation(2, 1),
            // (2,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 8),
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17));

        var expectedDiagnostics = new[]
        {
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = &x) { }").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,8): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 8),
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17));
    }

    [Fact]
    public void Pointer_Fixed_UnsafeContext()
    {
        var source = """
            class C
            {
                static int x;
                static void Main()
                {
                    unsafe { fixed (int* p = &x) { } }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS0227: Unsafe code may only appear if compiling with /unsafe
            //         unsafe { fixed (int* p = &x) { } }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(6, 9),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Arithmetic_SafeContext()
    {
        var source = """
            int* p = null;
            p++;
            int* p2 = p + 2;
            long x = p - p;
            bool b = p > p2;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // p++;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // p++;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p++").WithLocation(2, 1),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 1),
            // (3,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(3, 11),
            // (3,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p + 2").WithLocation(3, 11),
            // (4,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(4, 10),
            // (4,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(4, 14),
            // (5,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(5, 10),
            // (5,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p2").WithLocation(5, 14),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // p++;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // p++;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p++").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (3,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(3, 1),
            // (3,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(3, 11),
            // (3,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p + 2").WithArguments("updated memory safety rules").WithLocation(3, 11),
            // (4,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(4, 10),
            // (4,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(4, 14),
            // (5,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(5, 10),
            // (5,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p2").WithArguments("updated memory safety rules").WithLocation(5, 14));
    }

    [Fact]
    public void SizeOf_SafeContext()
    {
        var source = """
            _ = sizeof(int);
            _ = sizeof(nint);
            _ = sizeof(S);
            struct S;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,5): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
            // _ = sizeof(nint);
            Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(2, 5),
            // (3,5): error CS0233: 'S' does not have a predefined size, therefore sizeof can only be used in an unsafe context
            // _ = sizeof(S);
            Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(S)").WithArguments("S").WithLocation(3, 5));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = sizeof(nint);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "sizeof(nint)").WithArguments("updated memory safety rules").WithLocation(2, 5),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = sizeof(S);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "sizeof(S)").WithArguments("updated memory safety rules").WithLocation(3, 5));
    }

    [Fact]
    public void FixedSizeBuffer_SafeContext()
    {
        var source = """
            var s = new S();
            int* p = s.y;
            int z = s.x[100];

            struct S
            {
                public fixed int x[5], y[10];
            }
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.y").WithLocation(2, 10),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.x").WithLocation(3, 9),
            // (7,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public fixed int x[5], y[10];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[5]").WithLocation(7, 22));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9500: This operation may only be used in an unsafe context
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 12),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.y").WithArguments("updated memory safety rules").WithLocation(2, 10),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("updated memory safety rules").WithLocation(3, 12),
            // (7,22): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public fixed int x[5], y[10];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[5]").WithArguments("updated memory safety rules").WithLocation(7, 22));
    }

    [Fact]
    public void SkipLocalsInit_NeedsUnsafe()
    {
        var source = """
            class C { [System.Runtime.CompilerServices.SkipLocalsInit] void M() { } }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,12): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // class C { [System.Runtime.CompilerServices.SkipLocalsInit] void M() { } }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "System.Runtime.CompilerServices.SkipLocalsInit").WithLocation(1, 12),
        };

        CreateCompilation(source, options: TestOptions.ReleaseDll)
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll)
            .VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    [Fact]
    public void StackAlloc_SafeContext()
    {
        var source = """
            int* x = stackalloc int[3];
            System.Span<int> y = stackalloc int[5];
            M();

            [System.Runtime.CompilerServices.SkipLocalsInit]
            void M()
            {
                System.Span<int> a = stackalloc int[5];
                System.Span<int> b = stackalloc int[] { 1 };
                System.Span<int> d = stackalloc int[2] { 1, 2 };
                System.Span<int> e = stackalloc int[3] { 1, 2 };
            }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (1,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[3]").WithLocation(1, 10),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26));

        var expectedDiagnostics = new[]
        {
            // (8,26): error CS9501: stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_UnsafeUninitializedStackAlloc, "stackalloc int[5]").WithLocation(8, 26),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26),
        };

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (1,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "stackalloc int[3]").WithArguments("updated memory safety rules").WithLocation(1, 10),
            // (8,26): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "stackalloc int[5]").WithArguments("updated memory safety rules").WithLocation(8, 26),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26));
    }

    [Fact]
    public void StackAlloc_UnsafeContext()
    {
        var source = $$"""
            unsafe { System.Span<int> y = stackalloc int[5]; }
            M();

            [System.Runtime.CompilerServices.SkipLocalsInit]
            void M()
            {
                unsafe { System.Span<int> a = stackalloc int[5]; }
                unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (8,35): error CS0847: An array initializer of length '3' is expected
            //     unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(8, 35),
        };

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void StackAlloc_Lambda()
    {
        var source = """
            var lam = [System.Runtime.CompilerServices.SkipLocalsInit] () =>
            {
                System.Span<int> a = stackalloc int[5];
                int* b = stackalloc int[3];
                unsafe { System.Span<int> c = stackalloc int[1]; }
            };

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,26): error CS9501: stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_UnsafeUninitializedStackAlloc, "stackalloc int[5]").WithLocation(3, 26));
    }

    [Theory, CombinatorialData]
    public void Member_Method_Invocation(
        bool apiUpdatedRules,
        bool apiUnsafe,
        [CombinatorialValues(LanguageVersion.CSharp14, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion callerLangVersion,
        bool callerAllowUnsafe,
        bool callerUpdatedRules,
        bool callerUnsafeBlock,
        bool? compilationReference)
    {
        var api = $$"""
            public class C
            {
                public {{(apiUnsafe ? "unsafe" : "")}} void M() => System.Console.Write(111);
            }
            """;

        var caller = $"""
            var c = new C();
            {(callerUnsafeBlock ? "unsafe { c.M(); }" : "c.M();")}
            """;

        var expectedOutput = "111";

        CSharpCompilation comp;
        List<DiagnosticDescription> expectedDiagnostics = [];

        if (compilationReference is { } useCompilationReference)
        {
            var apiCompilation = CreateCompilation(api,
                options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(apiUpdatedRules))
                .VerifyDiagnostics();
            var apiReference = AsReference(apiCompilation, useCompilationReference);
            comp = CreateCompilation(caller, [apiReference],
                parseOptions: TestOptions.Regular.WithLanguageVersion(callerLangVersion),
                options: TestOptions.ReleaseExe.WithAllowUnsafe(callerAllowUnsafe).WithUpdatedMemorySafetyRules(callerUpdatedRules));
        }
        else
        {
            if (apiUpdatedRules != callerUpdatedRules)
            {
                return;
            }

            comp = CreateCompilation([api, caller],
                parseOptions: TestOptions.Regular.WithLanguageVersion(callerLangVersion),
                options: TestOptions.ReleaseExe.WithAllowUnsafe(callerAllowUnsafe).WithUpdatedMemorySafetyRules(callerUpdatedRules));

            if (!callerAllowUnsafe && apiUnsafe)
            {
                expectedDiagnostics.Add(
                    // (3,24): error CS0227: Unsafe code may only appear if compiling with /unsafe
                    //     public unsafe void M() => System.Console.Write(111);
                    Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(3, 24));
            }
        }

        if (!callerAllowUnsafe && callerUnsafeBlock)
        {
            expectedDiagnostics.Add(
                // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // unsafe { c.M(); }
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1));
        }

        if (apiUnsafe && apiUpdatedRules && callerUpdatedRules && !callerUnsafeBlock)
        {
            if (callerLangVersion >= LanguageVersionFacts.CSharpNext)
            {
                expectedDiagnostics.Add(
                    // (2,1): error CS9502: Using 'C.M()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                    // c.M();
                    Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M()").WithArguments("C.M()").WithLocation(2, 1));
            }
            else
            {
                expectedDiagnostics.Add(
                    // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // c.M();
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "c.M()").WithArguments("updated memory safety rules").WithLocation(2, 1));
            }
        }

        comp.VerifyDiagnostics([.. expectedDiagnostics]);

        if (!comp.GetDiagnostics().HasAnyErrors())
        {
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        }
    }

    [Fact]
    public void Member_Method_OverloadResolution()
    {
        var source = """
            C.M(1);
            C.M("s");
            _ = nameof(C.M);

            class C
            {
                public static void M(int x) { }
                public static unsafe void M(string s) { }
            }
            """;
        CreateCompilation(source,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,1): error CS9502: Using 'C.M(string)' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
            // C.M("s");
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, @"C.M(""s"")").WithArguments("C.M(string)").WithLocation(2, 1));
    }

    [Fact]
    public void Member_Method_SafeBoundary()
    {
        CompileAndVerify(
            lib: """
                public class C
                {
                    public void M1() { unsafe { M2(); } }
                    public unsafe void M2() { }
                }
                """,
            caller: """
                var c = new C();
                c.M1();
                c.M2();
                """,
            unsafeSymbols: ["C.M2"],
            safeSymbols: ["C.M1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: Using 'C.M2()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // c.M2();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M2()").WithArguments("C.M2()").WithLocation(3, 1),
            ]);
    }

    [Fact]
    public void Member_Method_NameOf()
    {
        var source = """
            _ = nameof(C.M);

            class C
            {
                public static unsafe void M() { }
            }
            """;
        CreateCompilation(source,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    [Fact]
    public void Member_Method_Extension()
    {
        CompileAndVerify(
            lib: """
                public static class E
                {
                    public static unsafe void M1(this int x) { }

                    extension(int x)
                    {
                        public unsafe void M2() { }
                    }
                }
                """,
            caller: """
                123.M1();
                123.M2();
                """,
            unsafeSymbols: ["E.M1", "E.M2", ExtensionMember("E", "M2")],
            safeSymbols: [],
            expectedDiagnostics:
            [
                // (1,1): error CS9502: Using 'E.M1(int)' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // 123.M1();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "123.M1()").WithArguments("E.M1(int)").WithLocation(1, 1),
                // (2,1): error CS9502: Using 'E.extension(int).M2()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // 123.M2();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "123.M2()").WithArguments("E.extension(int).M2()").WithLocation(2, 1),
            ]);
    }

    [Fact]
    public void Member_Method_InUnsafeClass()
    {
        // PROTOTYPE: unsafe modifier on a class should result in a warning
        CompileAndVerify(
            lib: """
                using System.Collections.Generic;
                public unsafe class C
                {
                    public void M1() { }
                    public IEnumerable<int> M2()
                    {
                        yield return 1;
                    }
                    public unsafe void M3() { }
                }
                """,
            caller: """
                var c = new C();
                c.M1();
                c.M2();
                c.M3();
                """,
            unsafeSymbols: ["C.M3"],
            safeSymbols: ["C.M1", "C.M2"],
            expectedDiagnostics:
            [
                // (4,1): error CS9502: Using 'C.M3()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // c.M3();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M3()").WithArguments("C.M3()").WithLocation(4, 1),
            ]);
    }

    [Fact]
    public void Member_Method_ConvertToFunctionPointer()
    {
        var source = """
            unsafe
            {
                delegate*<void> p = &C.M;
            }

            public static class C
            {
                public static unsafe void M() { }
            }
            """;
        CreateCompilation(source,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    // PROTOTYPE: Test also lambdas and delegates.
    [Fact]
    public void Member_LocalFunction()
    {
        var source = """
            M1();
            M2();
            static unsafe void M1() { }
            static void M2() { }
            """;
        CreateCompilation(source,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS9502: Using 'M1()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
            // M1();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "M1()").WithArguments("M1()").WithLocation(1, 1));
    }

    [Fact]
    public void Member_Property()
    {
        CompileAndVerify(
            lib: """
                public class C
                {
                    public int P1 { get; set; }
                    public unsafe int P2 { get; set; }
                }
                """,
            caller: """
                var c = new C();
                c.P1 = c.P1 + 123;
                c.P2 = c.P2 + 123;
                """,
            unsafeSymbols: ["C.P2", "C.get_P2", "C.set_P2"],
            safeSymbols: ["C.P1", "C.get_P1", "C.set_P1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: Using 'C.P2' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // c.P2 = c.P2 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2").WithLocation(3, 1),
                // (3,8): error CS9502: Using 'C.P2' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // c.P2 = c.P2 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2").WithLocation(3, 8),
            ]);
    }

    [Fact]
    public void Member_Property_Extension()
    {
        CompileAndVerify(
            lib: """
                public static class E
                {
                    extension(int x)
                    {
                        public int P1 { get => x; set { } }
                        public unsafe int P2 { get => x; set { } }
                    }
                }
                """,
            caller: """
                var x = 111;
                x.P1 = x.P1 + 222;
                x.P2 = x.P2 + 333;
                """,
            unsafeSymbols: [ExtensionMember("E", "P2"), "E.get_P2", ExtensionMember("E", "get_P2"), "E.set_P2", ExtensionMember("E", "set_P2")],
            safeSymbols: [ExtensionMember("E", "P1"), "E.get_P1", ExtensionMember("E", "get_P1"), "E.set_P1", ExtensionMember("E", "set_P1")],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: Using 'E.extension(int).P2' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // x.P2 = x.P2 + 333;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "x.P2").WithArguments("E.extension(int).P2").WithLocation(3, 1),
                // (3,8): error CS9502: Using 'E.extension(int).P2' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
                // x.P2 = x.P2 + 333;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "x.P2").WithArguments("E.extension(int).P2").WithLocation(3, 8),
            ]);
    }

    [Fact]
    public void RequiresUnsafeAttribute_Synthesized()
    {
        var source = """
            class C
            {
                unsafe void M1() { }
                void M2() { }
            }
            """;

        CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M1", "C.M2"]))
            .VerifyDiagnostics();

        var ref1 = CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: true,
                unsafeSymbols: ["C.M1"],
                safeSymbols: ["C", "C.M2"]))
            .VerifyDiagnostics()
            .GetImageReference();

        CompileAndVerify("", [ref1],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: []))
            .VerifyDiagnostics();

        var source2 = """
            class B
            {
                void M3() { }
                unsafe void M4() { }
            }
            """;

        CompileAndVerify(source2, [ref1],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: true,
                unsafeSymbols: ["B.M4"],
                safeSymbols: ["B", "B.M3"]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M1", "C.M2"]))
            .VerifyDiagnostics();

        CreateCompilation([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     unsafe void M1() { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "M1").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(3, 17));
    }

    [Fact]
    public void RequiresUnsafeAttribute_NotSynthesized()
    {
        var source = """
            public class C
            {
                public void M() { }
            }
            """;

        CompileAndVerify(source,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();
    }

    [Fact]
    public void RequiresUnsafeAttribute_Reflection()
    {
        var sourceA = """
            using System;
            using System.Linq;
            using System.Reflection;
            public class A
            {
                public unsafe void M1() { }
                public void M2() { }
                public static void RequiresUnsafe(MethodInfo method)
                {
                    var count = method.GetCustomAttributes(false).Count(a => a.GetType().Name == "RequiresUnsafeAttribute");
                    Console.Write(count);
                }
            }
            """;
        var refA = CreateCompilation(sourceA,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .EmitToImageReference();

        var sourceB = """
            class B : A
            {
                public unsafe void M3() { }
                public void M4() { }
                static void Main()
                {
                    RequiresUnsafe(typeof(A).GetMethod("M1"));
                    RequiresUnsafe(typeof(A).GetMethod("M2"));
                    RequiresUnsafe(typeof(B).GetMethod("M3"));
                    RequiresUnsafe(typeof(B).GetMethod("M4"));
                }
            }
            """;
        CompileAndVerify(sourceB, [refA],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            expectedOutput: "1010")
            .VerifyDiagnostics();
    }

    [Fact]
    public void RequiresUnsafeAttribute_FromSource()
    {
        var source = """
            public class C
            {
                public unsafe void M() { }
            }
            """;

        CompileAndVerify([source, RequiresUnsafeAttributeDefinition],
            options: TestOptions.UnsafeReleaseDll,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: false,
                unsafeSymbols: [],
                safeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify([source, RequiresUnsafeAttributeDefinition],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: false,
                unsafeSymbols: ["C.M"],
                safeSymbols: ["C"]))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RequiresUnsafeAttribute_FromMetadata(bool useCompilationReference)
    {
        var comp = CreateCompilation(RequiresUnsafeAttributeDefinition);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: false,
                unsafeSymbols: [],
                safeSymbols: [AttributeDescription.RequiresUnsafeAttribute.FullName]))
            .VerifyDiagnostics();
        var ref1 = AsReference(comp, useCompilationReference);

        var source = """
            public class C
            {
                public unsafe void M() { }
            }
            """;

        CompileAndVerify(source, [ref1],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: ["C.M"],
                safeSymbols: ["C"]))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RequiresUnsafeAttribute_FromMetadata_Multiple(bool useCompilationReference)
    {
        var comp1 = CreateCompilation(RequiresUnsafeAttributeDefinition).VerifyDiagnostics();
        var ref1 = AsReference(comp1, useCompilationReference);

        var comp2 = CreateCompilation(RequiresUnsafeAttributeDefinition).VerifyDiagnostics();
        var ref2 = AsReference(comp2, useCompilationReference);

        var source = """
            public class C
            {
                public unsafe void M() { }
            }
            """;

        // Ambiguous attribute definitions from references => synthesize our own.
        CompileAndVerify(source, [ref1, ref2],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: true,
                unsafeSymbols: ["C.M"],
                safeSymbols: ["C"]))
            .VerifyDiagnostics();

        // Also defined in source.
        CompileAndVerify([source, RequiresUnsafeAttributeDefinition], [ref1, ref2],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: true, isSynthesized: false,
                unsafeSymbols: ["C.M"],
                safeSymbols: ["C"]))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RequiresUnsafeAttribute_FromMetadata_Multiple_AndCorLib(bool useCompilationReference)
    {
        var corlibSource = """
            namespace System
            {
                public class Object;
                public class ValueType;
                public class Attribute;
                public struct Void;
                public struct Int32;
                public struct Boolean;
                public class AttributeUsageAttribute
                {
                    public AttributeUsageAttribute(AttributeTargets t) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
                public class Enum;
                public enum AttributeTargets;
            }
            """;

        var corlib = CreateEmptyCompilation([corlibSource, RequiresUnsafeAttributeDefinition]).VerifyDiagnostics();
        var corlibRef = AsReference(corlib, useCompilationReference);

        var comp1 = CreateEmptyCompilation(RequiresUnsafeAttributeDefinition, [corlibRef]).VerifyDiagnostics();
        var ref1 = AsReference(comp1, useCompilationReference);

        var comp2 = CreateEmptyCompilation(RequiresUnsafeAttributeDefinition, [corlibRef]).VerifyDiagnostics();
        var ref2 = AsReference(comp2, useCompilationReference);

        var source = """
            public class C
            {
                public unsafe void M() { }
            }
            """;

        // Using the attribute from corlib even if there are ambiguous definitions in other references.
        var verifier = CompileAndVerify(CreateEmptyCompilation(source, [ref1, ref2, corlibRef],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules()),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(m, includesAttributeDefinition: false,
                unsafeSymbols: ["C.M"],
                safeSymbols: ["C"]));

        verifier.Diagnostics.WhereAsArray(d => d.Code != (int)ErrorCode.WRN_NoRuntimeMetadataVersion).Verify();

        var comp = (CSharpCompilation)verifier.Compilation;
        Assert.Same(comp.Assembly.CorLibrary, comp.GetReferencedAssemblySymbol(corlibRef));
    }

    [Fact]
    public void RequiresUnsafeAttribute_FromMetadata_UnrecognizedConstructor()
    {
        // [module: MemorySafetyRules(2)]
        // public class A
        // {
        //     [RequiresUnsafe(1), RequiresUnsafe(0)]
        //     public static void M() => throw null;
        // }
        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{CSharpCompilationOptions.UpdatedMemorySafetyRulesVersion}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
            }
            .class private System.Runtime.CompilerServices.RequiresUnsafeAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
            }
            .class public A
            {
                .method public static void M()
                {
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor(int32) = { int32(1) }
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor(int32) = { int32(0) }
                    ldnull throw
                }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var a = CreateCompilation("", [refA]).VerifyDiagnostics().GetReferencedAssemblySymbol(refA);
        Assert.False(a.GlobalNamespace.GetMember("A.M").IsCallerUnsafe);

        var sourceB = """
            A.M();
            """;
        CreateCompilation(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    [Fact]
    public void RequiresUnsafeAttribute_FromMetadata_UnrecognizedAndRecognizedConstructor()
    {
        // [module: MemorySafetyRules(2)]
        // public class A
        // {
        //     [RequiresUnsafe(1), RequiresUnsafe(0)]
        //     public static void M() => throw null;
        // }
        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{CSharpCompilationOptions.UpdatedMemorySafetyRulesVersion}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
            }
            .class private System.Runtime.CompilerServices.RequiresUnsafeAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
            }
            .class public A
            {
                .method public static void M()
                {
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor(int32) = { int32(1) }
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor()
                    ldnull throw
                }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var a = CreateCompilation("", [refA]).VerifyDiagnostics().GetReferencedAssemblySymbol(refA);
        Assert.True(a.GlobalNamespace.GetMember("A.M").IsCallerUnsafe);

        var sourceB = """
            A.M();
            """;
        CreateCompilation(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS9502: Using 'A.M()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
            // A.M();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "A.M()").WithArguments("A.M()").WithLocation(1, 1));
    }

    [Fact]
    public void RequiresUnsafeAttribute_FromMetadata_AppliedMultipleTimes()
    {
        // [module: MemorySafetyRules(2)]
        // public class A
        // {
        //     [RequiresUnsafe, RequiresUnsafe]
        //     public static void M() => throw null;
        // }
        var sourceA = $$"""
            .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
            .assembly '<<GeneratedFileName>>' { }
            .module '<<GeneratedFileName>>.dll'
            .custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{CSharpCompilationOptions.UpdatedMemorySafetyRulesVersion}}) }
            .class private System.Runtime.CompilerServices.MemorySafetyRulesAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(int32 version) cil managed { ret }
            }
            .class private System.Runtime.CompilerServices.RequiresUnsafeAttribute extends [mscorlib]System.Attribute
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
            }
            .class public A
            {
                .method public static void M()
                {
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor()
                    .custom instance void System.Runtime.CompilerServices.RequiresUnsafeAttribute::.ctor()
                    ldnull throw
                }
            }
            """;
        var refA = CompileIL(sourceA, prependDefaultHeader: false);

        var a = CreateCompilation("", [refA]).VerifyDiagnostics().GetReferencedAssemblySymbol(refA);
        Assert.True(a.GlobalNamespace.GetMember("A.M").IsCallerUnsafe);

        var sourceB = """
            A.M();
            """;
        CreateCompilation(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS9502: Using 'A.M()' is only permitted in an unsafe context because it is marked as 'unsafe' under the updated memory safety rules
            // A.M();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "A.M()").WithArguments("A.M()").WithLocation(1, 1));
    }

    [Theory, CombinatorialData]
    public void RequiresUnsafeAttribute_ReferencedInSource(
        bool updatedRules,
        bool useCompilationReference)
    {
        var comp = CreateCompilation(RequiresUnsafeAttributeDefinition).VerifyDiagnostics();
        var ref1 = AsReference(comp, useCompilationReference);

        var source = """
            using System.Runtime.CompilerServices;
            [RequiresUnsafeAttribute] class C
            {
                [RequiresUnsafeAttribute] void M() { }
                [RequiresUnsafeAttribute] int P { get; set; }
            }
            """;

        comp = CreateCompilation(source, [ref1], options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRules));
        comp.VerifyDiagnostics(
            // (4,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] void M() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 6),
            // (5,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] int P { get; set; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(5, 6));
    }
}
