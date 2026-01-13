// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Unsafe)]
public sealed class UnsafeEvolutionTests : CompilingTestBase
{
    /// <param name="expectedUnsafeSymbols">See <see cref="VerifyRequiresUnsafeAttribute"/>.</param>
    /// <param name="expectedSafeSymbols">See <see cref="VerifyRequiresUnsafeAttribute"/>.</param>
    private void CompileAndVerifyUnsafe(
        string lib,
        string caller,
        object[] expectedUnsafeSymbols,
        object[] expectedSafeSymbols,
        DiagnosticDescription[] expectedDiagnostics,
        ReadOnlySpan<string> additionalSources = default,
        Verification verify = default,
        CallerUnsafeMode expectedUnsafeMode = CallerUnsafeMode.Explicit)
    {
        CreateCompilation([lib, caller, .. additionalSources],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        var libUpdated = CompileAndVerify([lib, .. additionalSources],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            verify: verify,
            symbolValidator: symbolValidator)
            .VerifyDiagnostics();

        var libUpdatedRefs = new MetadataReference[] { libUpdated.GetImageReference(), libUpdated.Compilation.ToMetadataReference() };

        foreach (var libUpdatedRef in libUpdatedRefs)
        {
            var libAssemblySymbol = CreateCompilation([caller, .. additionalSources], [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All))
                .VerifyDiagnostics(expectedDiagnostics)
                .GetReferencedAssemblySymbol(libUpdatedRef);

            symbolValidator(libAssemblySymbol.Modules.Single());
        }

        var libLegacy = CompileAndVerify([lib, .. additionalSources],
            options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
            verify: verify,
            symbolValidator: module =>
            {
                VerifyMemorySafetyRulesAttribute(module, includesAttributeDefinition: false, includesAttributeUse: false);
                VerifyRequiresUnsafeAttribute(
                    module,
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: [.. expectedUnsafeSymbols, .. expectedSafeSymbols]);
            })
            .VerifyDiagnostics()
            .GetImageReference();

        CreateCompilation([caller, .. additionalSources], [libLegacy],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();

        void symbolValidator(ModuleSymbol module)
        {
            if (module is SourceModuleSymbol)
            {
                VerifyMemorySafetyRulesAttribute(module, includesAttributeDefinition: false, includesAttributeUse: false);
                VerifyRequiresUnsafeAttribute(
                    module,
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: expectedUnsafeSymbols,
                    expectedSafeSymbols: expectedSafeSymbols,
                    expectedUnsafeMode: expectedUnsafeMode);
            }
            else
            {
                VerifyMemorySafetyRulesAttribute(module, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true);
                VerifyRequiresUnsafeAttribute(
                    module,
                    includesAttributeDefinition: true,
                    isSynthesized: true,
                    expectedUnsafeSymbols: expectedUnsafeSymbols,
                    expectedSafeSymbols: expectedSafeSymbols,
                    expectedUnsafeMode: expectedUnsafeMode);
            }
        }
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

    private static Func<ModuleSymbol, Symbol> Overload(string qualifiedName, int parameterCount)
    {
        return module => module.GlobalNamespace
            .GetMembersByQualifiedName<MethodSymbol>(qualifiedName)
            .SingleOrDefault(m => m.Parameters.Length == parameterCount)
            ?? throw new InvalidOperationException($"Cannot find '{qualifiedName}' with {parameterCount} parameters.");
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

    /// <remarks>
    /// <paramref name="expectedUnsafeSymbols"/> (and <paramref name="expectedSafeSymbols"/>) should be symbol names (<see cref="string"/>)
    /// or symbol getters (<c><![CDATA[Func<ModuleSymbol, Symbol>]]></c>) of symbols that are expected to be unsafe (or safe, respectively).
    /// </remarks>
    private static void VerifyRequiresUnsafeAttribute(
        ModuleSymbol module,
        bool includesAttributeDefinition,
        ReadOnlySpan<object> expectedUnsafeSymbols,
        ReadOnlySpan<object> expectedSafeSymbols,
        bool? isSynthesized = null,
        CallerUnsafeMode expectedUnsafeMode = CallerUnsafeMode.Explicit)
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

        foreach (var symbol in expectedUnsafeSymbols)
        {
            verifySymbol(symbol, shouldBeUnsafe: true);
        }

        foreach (var symbol in expectedSafeSymbols)
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

            var symbolExpectedUnsafeMode = shouldBeUnsafe ? expectedUnsafeMode : CallerUnsafeMode.None;

            if (symbol.ContainingModule is PEModuleSymbol peModuleSymbol)
            {
                var unfilteredAttributes = peModuleSymbol.GetCustomAttributesForToken(MetadataTokens.EntityHandle(symbol.MetadataToken));
                var unfilteredAttribute = unfilteredAttributes.SingleOrDefault(a => a.AttributeClass?.Name == Name);
                var expectedUnfilteredAttribute = symbolExpectedUnsafeMode.NeedsRequiresUnsafeAttribute();
                Assert.True((unfilteredAttribute != null) == expectedUnfilteredAttribute, $"Attribute should{(expectedUnfilteredAttribute ? "" : " not")} be in metadata for '{symbol.ToTestDisplayString()}'");
            }
            else
            {
                Assert.True(symbol.ContainingModule is SourceModuleSymbol or null);
            }

            Assert.True(symbolExpectedUnsafeMode == symbol.CallerUnsafeMode, $"Expected '{symbol.ToTestDisplayString()}' to have {nameof(CallerUnsafeMode)}.{symbolExpectedUnsafeMode} (got {symbol.CallerUnsafeMode})");

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
                    var attribute = module.GetCustomAttributes(inherit: false).Single(a => a.GetType().Name == "MemorySafetyRulesAttribute");
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
    public void Pointer_Function_Call_UsingAlias()
    {
        var source = """
            using X = delegate*<string>;
            X x = null;
            string s = x();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = delegate*<string>;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 11),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // X x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(2, 1),
            // (3,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x()").WithLocation(3, 12));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9500: This operation may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "x()").WithLocation(3, 12),
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
            // (1,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using X = delegate*<string>;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 11),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // X x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (3,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x()").WithArguments("updated memory safety rules").WithLocation(3, 12));
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
        var source = """
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

    [Fact]
    public void Member_LangVersion()
    {
        CSharpTestSource source =
        [
            """
            #pragma warning disable CS8321 // unused local function
            unsafe void F() { }
            class C
            {
                unsafe void M() { }
                unsafe int P { get; set; }
            #pragma warning disable CS0067 // unused event
                unsafe event System.Action E { add { } remove { } }
                unsafe int this[int i] { get => i; set { } }
                unsafe C() { }
                unsafe ~C() { }
                public unsafe static C operator +(C c1, C c2) => c1;
                public unsafe void operator +=(C c) { }
            #pragma warning disable CS0169 // unused field
                unsafe int F;
            }
            unsafe class U;
            unsafe delegate void D();
            """,
            CompilerFeatureRequiredAttribute,
        ];

        string[] safeSymbols = ["C", "C.F", "U", "D"];
        string[] unsafeSymbols =
        [
            "Program.<<Main>$>g__F|0_0",
            "C.M",
            "C.P", "C.get_P", "C.set_P",
            "C.E", "C.add_E", "C.remove_E",
            "C.this[]", "C.get_Item", "C.set_Item",
            "C..ctor",
            "C.Finalize",
            "C.op_Addition",
            "C.op_AdditionAssignment",
        ];

        CompileAndVerify(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m =>
            {
                VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false);
                VerifyRequiresUnsafeAttribute(
                    m,
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: [.. safeSymbols, .. unsafeSymbols]);
            })
            .VerifyDiagnostics();

        CompileAndVerify(source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m =>
            {
                VerifyMemorySafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, isSynthesized: true);
                VerifyRequiresUnsafeAttribute(
                    m,
                    includesAttributeDefinition: true,
                    isSynthesized: true,
                    expectedUnsafeSymbols: [.. unsafeSymbols],
                    expectedSafeSymbols: [.. safeSymbols]);
            })
            .VerifyDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,13): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // unsafe void F() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "F").WithArguments("updated memory safety rules").WithLocation(2, 13),
            // (5,17): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe void M() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("updated memory safety rules").WithLocation(5, 17),
            // (6,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int P { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int").WithArguments("updated memory safety rules").WithLocation(6, 12),
            // (6,20): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int P { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "get").WithArguments("updated memory safety rules").WithLocation(6, 20),
            // (6,25): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int P { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "set").WithArguments("updated memory safety rules").WithLocation(6, 25),
            // (8,32): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "E").WithArguments("updated memory safety rules").WithLocation(8, 32),
            // (8,36): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "add").WithArguments("updated memory safety rules").WithLocation(8, 36),
            // (8,44): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "remove").WithArguments("updated memory safety rules").WithLocation(8, 44),
            // (9,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int").WithArguments("updated memory safety rules").WithLocation(9, 12),
            // (9,30): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "get").WithArguments("updated memory safety rules").WithLocation(9, 30),
            // (9,40): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "set").WithArguments("updated memory safety rules").WithLocation(9, 40),
            // (10,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe C() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("updated memory safety rules").WithLocation(10, 12),
            // (11,13): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     unsafe ~C() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("updated memory safety rules").WithLocation(11, 13),
            // (12,37): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public unsafe static C operator +(C c1, C c2) => c1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+").WithArguments("updated memory safety rules").WithLocation(12, 37),
            // (13,33): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public unsafe void operator +=(C c) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "+=").WithArguments("updated memory safety rules").WithLocation(13, 33));
    }

    // PROTOTYPE: Test also implicit methods used in patterns like GetEnumerator in foreach.
    // PROTOTYPE: Should some synthesized members be unsafe (like state machine methods that are declared unsafe)?
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

            if (apiUnsafe && apiUpdatedRules && callerUpdatedRules && callerLangVersion < LanguageVersionFacts.CSharpNext)
            {
                expectedDiagnostics.Add(
                    // (3,24): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public unsafe void M() => System.Console.Write(111);
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("updated memory safety rules").WithLocation(3, 24));
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
                    // (2,1): error CS9502: 'C.M()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
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
        CompileAndVerifyUnsafe(
            lib: """
                public class C
                {
                    public static void M() { }
                    public static unsafe void M(int x) { }
                }
                """,
            caller: """
                C.M();
                C.M(1);
                _ = nameof(C.M);
                unsafe { C.M(1); }
                """,
            expectedUnsafeSymbols: [Overload("C.M", 1)],
            expectedSafeSymbols: ["C", Overload("C.M", 0)],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.M(int)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // C.M(1);
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "C.M(1)").WithArguments("C.M(int)").WithLocation(2, 1),
            ]);
    }

    [Fact]
    public void Member_Method_SafeBoundary()
    {
        CompileAndVerifyUnsafe(
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
            expectedUnsafeSymbols: ["C.M2"],
            expectedSafeSymbols: ["C.M1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.M2()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M2();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M2()").WithArguments("C.M2()").WithLocation(3, 1),
            ]);
    }

    [Fact]
    public void Member_Method_NameOf()
    {
        CompileAndVerifyUnsafe(
            lib: """
                public class C
                {
                    public static unsafe void M() { }
                }
                """,
            caller: """
                _ = nameof(C.M);
                """,
            expectedUnsafeSymbols: ["C.M"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics: []);
    }

    [Fact]
    public void Member_Method_Extension()
    {
        CompileAndVerifyUnsafe(
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
            expectedUnsafeSymbols: ["E.M1", "E.M2", ExtensionMember("E", "M2")],
            expectedSafeSymbols: [],
            expectedDiagnostics:
            [
                // (1,1): error CS9502: 'E.M1(int)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // 123.M1();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "123.M1()").WithArguments("E.M1(int)").WithLocation(1, 1),
                // (2,1): error CS9502: 'E.extension(int).M2()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // 123.M2();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "123.M2()").WithArguments("E.extension(int).M2()").WithLocation(2, 1),
            ]);
    }

    [Fact]
    public void Member_Method_InUnsafeClass()
    {
        // PROTOTYPE: unsafe modifier on a class should result in a warning
        CompileAndVerifyUnsafe(
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
            expectedUnsafeSymbols: ["C.M3"],
            expectedSafeSymbols: ["C.M1", "C.M2"],
            expectedDiagnostics:
            [
                // (4,1): error CS9502: 'C.M3()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M3();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M3()").WithArguments("C.M3()").WithLocation(4, 1),
            ]);
    }

    [Fact]
    public void Member_Method_ConvertToFunctionPointer()
    {
        CompileAndVerifyUnsafe(
            lib: """
                public static class C
                {
                    public static unsafe void M() { }
                }
                """,
            caller: """
                delegate*<void> p1 = &C.M;
                unsafe { delegate*<void> p2 = &C.M; }
                """,
            expectedUnsafeSymbols: ["C.M"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics: []);
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
            // (1,1): error CS9502: 'M1()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
            // M1();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "M1()").WithArguments("M1()").WithLocation(1, 1));
    }

    [Fact]
    public void Member_Property()
    {
        var lib = """
            public class C
            {
                public int P1 { get; set; }
                public unsafe int P2 { get; set; }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                c.P1 = c.P1 + 123;
                c.P2 = c.P2 + 123;
                """,
            expectedUnsafeSymbols: ["C.P2", "C.get_P2", "C.set_P2"],
            expectedSafeSymbols: ["C.P1", "C.get_P1", "C.set_P1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = c.P2 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.set").WithLocation(3, 1),
                // (3,8): error CS9502: 'C.P2.get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = c.P2 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.get").WithLocation(3, 8)
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int P2 { get; set; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 19),
            // (4,28): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int P2 { get; set; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "get").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 28),
            // (4,33): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int P2 { get; set; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 33));
    }

    [Fact]
    public void Member_Property_Extension()
    {
        CompileAndVerifyUnsafe(
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
            expectedUnsafeSymbols: [ExtensionMember("E", "P2"), "E.get_P2", ExtensionMember("E", "get_P2"), "E.set_P2", ExtensionMember("E", "set_P2")],
            expectedSafeSymbols: [ExtensionMember("E", "P1"), "E.get_P1", ExtensionMember("E", "get_P1"), "E.set_P1", ExtensionMember("E", "set_P1")],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'E.extension(int).P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // x.P2 = x.P2 + 333;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "x.P2").WithArguments("E.extension(int).P2.set").WithLocation(3, 1),
                // (3,8): error CS9502: 'E.extension(int).P2.get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // x.P2 = x.P2 + 333;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "x.P2").WithArguments("E.extension(int).P2.get").WithLocation(3, 8),
            ]);
    }

    [Fact]
    public void Member_Property_Record()
    {
        CompileAndVerifyUnsafe(
            lib: """
                public record C(int P1, int P2)
                {
                    public unsafe int P2 { get; set; } = P2;
                }
                """,
            caller: """
                var c = new C(1, 2);
                c.P2 = c.P1 + c.P2;
                """,
            additionalSources: [IsExternalInitTypeDefinition],
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.P2", "C.get_P2", "C.set_P2"],
            expectedSafeSymbols: ["C.P1", "C.get_P1", "C.set_P1"],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = c.P1 + c.P2;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.set").WithLocation(2, 1),
                // (2,15): error CS9502: 'C.P2.get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = c.P1 + c.P2;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.get").WithLocation(2, 15),
            ]);
    }

    [Fact]
    public void Member_Property_Accessors()
    {
        var lib = """
            public class C
            {
                public int P1 { unsafe get; set; }
                public int P2 { get; unsafe set; }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                c.P1 = c.P1 + 123;
                c.P2 = c.P2 + 123;
                """,
            expectedUnsafeSymbols: ["C.get_P1", "C.set_P2"],
            expectedSafeSymbols: ["C.P1", "C.P2", "C.get_P2", "C.set_P1"],
            expectedDiagnostics:
            [
                // (2,8): error CS9502: 'C.P1.get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P1 = c.P1 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P1").WithArguments("C.P1.get").WithLocation(2, 8),
                // (3,1): error CS9502: 'C.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = c.P2 + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.set").WithLocation(3, 1),
            ]);

        CreateCompilation(lib, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,21): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public int P1 { unsafe get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "unsafe").WithArguments("updated memory safety rules").WithLocation(3, 21),
            // (4,26): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public int P2 { get; unsafe set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "unsafe").WithArguments("updated memory safety rules").WithLocation(4, 26));

        CreateCompilation(lib, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
        CreateCompilation(lib, parseOptions: TestOptions.RegularPreview).VerifyEmitDiagnostics();

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (3,28): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public int P1 { unsafe get; set; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "get").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(3, 28),
            // (4,33): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public int P2 { get; unsafe set; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 33));
    }

    [Fact]
    public void Member_Property_Attribute()
    {
        CompileAndVerifyUnsafe(
            lib: """
                public class A : System.Attribute
                {
                    public int P1 { get; set; }
                    public unsafe int P2 { get; set; }
                    public int P3 { unsafe get; set; }
                    public int P4 { get; unsafe set; }
                    public unsafe int F;
                }
                """,
            caller: """
                var c = new C1();
                [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] class C1;
                [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] unsafe class C2;
                partial class C3
                {
                    [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] void M1() { }
                }
                unsafe partial class C3
                {
                    [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] void M2() { }
                }
                """,
            expectedUnsafeSymbols: ["A.P2", "A.get_P2", "A.set_P2", "A.get_P3", "A.set_P4"],
            expectedSafeSymbols: ["A.P1", "A.get_P1", "A.set_P1", "A.set_P3", "A.get_P4", "A.F"],
            expectedDiagnostics:
            [
                // (2,12): error CS9502: 'A.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] class C1;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "P2 = 0").WithArguments("A.P2.set").WithLocation(2, 12),
                // (2,28): error CS9502: 'A.P4.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] class C1;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "P4 = 0").WithArguments("A.P4.set").WithLocation(2, 28),
                // (6,16): error CS9502: 'A.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                //     [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] void M1() { }
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "P2 = 0").WithArguments("A.P2.set").WithLocation(6, 16),
                // (6,32): error CS9502: 'A.P4.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                //     [A(P1 = 0, P2 = 0, P3 = 0, P4 = 0, F = 0)] void M1() { }
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "P4 = 0").WithArguments("A.P4.set").WithLocation(6, 32),
            ]);
    }

    // PROTOTYPE: Test extension indexers if merged before this feature.
    [Fact]
    public void Member_Indexer()
    {
        var lib = """
            public class C1
            {
                public int this[int i] { get => i; set { } }
            }
            public class C2
            {
                public unsafe int this[int i] { get => i; set { } }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c1 = new C1();
                c1[0] = c1[0] + 123;
                var c2 = new C2();
                c2[0] = c2[0] + 123;
                """,
            expectedUnsafeSymbols: ["C2.this[]", "C2.get_Item", "C2.set_Item"],
            expectedSafeSymbols: ["C1.this[]", "C1.get_Item", "C1.set_Item"],
            expectedDiagnostics:
            [
                // (4,1): error CS9502: 'C2.this[int].set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c2[0] = c2[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c2[0]").WithArguments("C2.this[int].set").WithLocation(4, 1),
                // (4,9): error CS9502: 'C2.this[int].get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c2[0] = c2[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c2[0]").WithArguments("C2.this[int].get").WithLocation(4, 9),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (7,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 19),
            // (7,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "get").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 37),
            // (7,47): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 47));
    }

    [Fact]
    public void Member_Indexer_Accessors()
    {
        var lib = """
            public class C1
            {
                public int this[int i] { unsafe get => i; set { } }
            }
            public class C2
            {
                public int this[int i] { get => i; unsafe set { } }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c1 = new C1();
                c1[0] = c1[0] + 123;
                var c2 = new C2();
                c2[0] = c2[0] + 123;
                """,
            expectedUnsafeSymbols: ["C1.get_Item", "C2.set_Item"],
            expectedSafeSymbols: ["C1.this[]", "C2.this[]", "C2.get_Item", "C1.set_Item"],
            expectedDiagnostics:
            [
                // (2,9): error CS9502: 'C1.this[int].get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c1[0] = c1[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c1[0]").WithArguments("C1.this[int].get").WithLocation(2, 9),
                // (4,1): error CS9502: 'C2.this[int].set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c2[0] = c2[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c2[0]").WithArguments("C2.this[int].set").WithLocation(4, 1),
            ]);

        CreateCompilation(lib, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,30): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public int this[int i] { unsafe get => i; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "unsafe").WithArguments("updated memory safety rules").WithLocation(3, 30),
            // (7,40): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public int this[int i] { get => i; unsafe set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "unsafe").WithArguments("updated memory safety rules").WithLocation(7, 40));

        CreateCompilation(lib, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
        CreateCompilation(lib, parseOptions: TestOptions.RegularPreview).VerifyEmitDiagnostics();

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (3,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public int this[int i] { unsafe get => i; set { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "get").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(3, 37),
            // (7,47): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public int this[int i] { get => i; unsafe set { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 47));
    }

    [Fact]
    public void Member_Event()
    {
        var lib = """
            #pragma warning disable CS0067 // unused event
            public class C
            {
                public event System.Action E1 { add { } remove { } }
                public unsafe event System.Action E2 { add { } remove { } }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                c.E1 += null;
                c.E2 += null;
                """,
            expectedUnsafeSymbols: ["C.E2", "C.add_E2", "C.remove_E2"],
            expectedSafeSymbols: ["C.E1", "C.add_E1", "C.remove_E1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.E2' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.E2 += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.E2").WithArguments("C.E2").WithLocation(3, 1),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (5,39): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe event System.Action E2 { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E2").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(5, 39),
            // (5,44): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe event System.Action E2 { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "add").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(5, 44),
            // (5,52): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe event System.Action E2 { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "remove").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(5, 52));

        CreateCompilation("""
            class C
            {
                event System.Action E1, E2;
                unsafe event System.Action E3, E4;
                void M()
                {
                    E1();
                    E2();
                    E3();
                    E4();
                }
            }
            """,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (9,9): error CS9502: 'C.E3' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
            //         E3();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "E3").WithArguments("C.E3").WithLocation(9, 9),
            // (10,9): error CS9502: 'C.E4' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
            //         E4();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "E4").WithArguments("C.E4").WithLocation(10, 9));
    }

    [Fact]
    public void Member_Event_Accessors()
    {
        var source = """
            public class C
            {
                public event System.Action E1 { unsafe add { } remove { } }
                public event System.Action E2 { add { } unsafe remove { } }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,37): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     public event System.Action E1 { unsafe add { } remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "unsafe").WithLocation(3, 37),
            // (4,45): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     public event System.Action E2 { add { } unsafe remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "unsafe").WithLocation(4, 45),
        };

        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules()).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Member_Event_NoAccessors()
    {
        var lib = """
            #pragma warning disable CS0067 // unused event
            public class C
            {
                public unsafe event System.Action E { }
            }
            """;

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,39): error CS0065: 'C.E': event property must have both add and remove accessors
            //     public unsafe event System.Action E { }
            Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("C.E").WithLocation(4, 39),
            // (4,39): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe event System.Action E { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 39));
    }

    [Fact]
    public void Member_Constructor()
    {
        var lib = """
            public class C
            {
                public C(int i) { }
                public unsafe C() { }
            }
            public unsafe class C2(int x)
            {
                int _x = x;
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                _ = new C(0);
                _ = new C();
                unsafe { _ = new C(); }
                _ = new C2(0);
                """,
            expectedUnsafeSymbols: [Overload("C..ctor", parameterCount: 0)],
            expectedSafeSymbols: ["C", Overload("C..ctor", parameterCount: 1), "C2", "C2..ctor"],
            expectedDiagnostics:
            [
                // (2,5): error CS9502: 'C.C()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // _ = new C();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "new C()").WithArguments("C.C()").WithLocation(2, 5),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe C() { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 19));
    }

    [Fact]
    public void Member_Constructor_Static()
    {
        var lib = """
            public class C
            {
                public static readonly int F = 42;
                static unsafe C() { }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                _ = C.F;
                """,
            expectedUnsafeSymbols: ["C..cctor"],
            expectedSafeSymbols: ["C", "C..ctor"],
            expectedDiagnostics: []);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     static unsafe C() { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 19));
    }

    [Fact]
    public void Member_Destructor()
    {
        var lib = """
            public class C
            {
                unsafe ~C() { }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                _ = new C();
                """,
            expectedUnsafeSymbols: ["C.Finalize"],
            expectedSafeSymbols: [],
            expectedDiagnostics: []);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (3,13): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     unsafe ~C() { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(3, 13));

        CreateCompilation("""
            class C
            {
                unsafe ~C() { }
                void M() { Finalize(); }
            }
            class D : C
            {
                ~D() { } // implicitly calls base finalizer
            }
            """,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (4,16): error CS9502: 'C.~C()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
            //     void M() { Finalize(); }
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "Finalize()").WithArguments("C.~C()").WithLocation(4, 16),
            // (4,16): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
            //     void M() { Finalize(); }
            Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "Finalize()").WithLocation(4, 16));
    }

    [Fact]
    public void Member_Operator_Static()
    {
        var lib = """
            public class C
            {
                public static C operator +(C c1, C c2) => c1;
                public static unsafe C operator -(C c1, C c2) => c1;
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                _ = c + c;
                _ = c - c;
                unsafe { _ = c - c; }
                """,
            expectedUnsafeSymbols: ["C.op_Subtraction"],
            expectedSafeSymbols: ["C.op_Addition"],
            expectedDiagnostics:
            [
                // (3,5): error CS9502: 'C.operator -(C, C)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // _ = c - c;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c - c").WithArguments("C.operator -(C, C)").WithLocation(3, 5),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public static unsafe C operator -(C c1, C c2) => c1;
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "-").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 37));
    }

    [Fact]
    public void Member_Operator_Static_Extension()
    {
        var lib = """
            public class C;
            public static class E
            {
                extension(C)
                {
                    public static C operator +(C c1, C c2) => c1;
                    public static unsafe C operator -(C c1, C c2) => c1;
                }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                _ = c + c;
                _ = c - c;
                unsafe { _ = c - c; }
                """,
            expectedUnsafeSymbols: ["E.op_Subtraction", ExtensionMember("E", "op_Subtraction")],
            expectedSafeSymbols: ["E.op_Addition", ExtensionMember("E", "op_Addition")],
            expectedDiagnostics:
            [
                // (3,5): error CS9502: 'E.extension(C).operator -(C, C)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // _ = c - c;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c - c").WithArguments("E.extension(C).operator -(C, C)").WithLocation(3, 5),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition, ExtensionMarkerAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (7,41): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //         public static unsafe C operator -(C c1, C c2) => c1;
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "-").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 41));
    }

    [Fact]
    public void Member_Operator_Instance()
    {
        var lib = """
            public class C
            {
                public void operator +=(C c) { }
                public unsafe void operator -=(C c) { }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                c += c;
                c -= c;
                unsafe { c -= c; }
                """,
            additionalSources: [CompilerFeatureRequiredAttribute],
            expectedUnsafeSymbols: ["C.op_SubtractionAssignment"],
            expectedSafeSymbols: ["C.op_AdditionAssignment"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.operator -=(C)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c -= c;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c -= c").WithArguments("C.operator -=(C)").WithLocation(3, 1),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition, CompilerFeatureRequiredAttribute],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (4,33): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //     public unsafe void operator -=(C c) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "-=").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 33));
    }

    [Fact]
    public void Member_Operator_Instance_Extension()
    {
        var lib = """
            public class C;
            public static class E
            {
                extension(C c1)
                {
                    public void operator +=(C c2) { }
                    public unsafe void operator -=(C c2) { }
                }
            }
            """;

        CompileAndVerifyUnsafe(
            lib: lib,
            caller: """
                var c = new C();
                c += c;
                c -= c;
                unsafe { c -= c; }
                """,
            additionalSources: [CompilerFeatureRequiredAttribute],
            expectedUnsafeSymbols: ["E.op_SubtractionAssignment", ExtensionMember("E", "op_SubtractionAssignment")],
            expectedSafeSymbols: ["E.op_AdditionAssignment", ExtensionMember("E", "op_AdditionAssignment")],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'E.extension(C).operator -=(C)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c -= c;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c -= c").WithArguments("E.extension(C).operator -=(C)").WithLocation(3, 1),
            ]);

        CreateCompilation([lib, MemorySafetyRulesAttributeDefinition, CompilerFeatureRequiredAttribute, ExtensionMarkerAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (7,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //         public unsafe void operator -=(C c2) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "-=").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(7, 37));
    }

    [Theory, CombinatorialData]
    public void Member_FunctionPointer(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public unsafe class C
            {
                public delegate*<string> F;
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            string s = c.F();
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,12): error CS9500: This operation may only be used in an unsafe context
            // string s = c.F();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "c.F()").WithLocation(2, 12));

        CompileAndVerify("""
            var c = new C();
            unsafe { string s = c.F(); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.F", (object)getFunctionPointerType, (object)getFunctionPointerMethod],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (2,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = c.F();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.F()").WithLocation(2, 12));

        static Symbol getFunctionPointerType(ModuleSymbol module)
        {
            return module.GlobalNamespace.GetMember("C.F").GetTypeOrReturnType().Type;
        }

        static Symbol getFunctionPointerMethod(ModuleSymbol module)
        {
            var functionPointerType = (FunctionPointerTypeSymbol)getFunctionPointerType(module);
            return functionPointerType.Signature;
        }
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_ParameterType(
        [CombinatorialValues("int*", "int*[]", "delegate*<void>")] string parameterType,
        bool useCompilationReference)
    {
        var lib = CreateCompilation($$"""
            public class C
            {
                public unsafe void M1(int x) { }
                public unsafe void M2({{parameterType}} y) { }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.M1(0);
            c.M2(null);
            unsafe { c.M2(null); }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'C.M2(int*)' must be used in an unsafe context because it has pointers in its signature
            // c.M2(null);
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.M2(null)").WithArguments($"C.M2({parameterType})").WithLocation(3, 1));

        CompileAndVerify("""
            var c = new C();
            c.M1(0);
            unsafe { c.M2(null); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.M2"],
                expectedSafeSymbols: ["C", "C.M1"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (3,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.M2(null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null").WithLocation(3, 6),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.M2(null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.M2(null)").WithLocation(3, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_ReturnType(
        [CombinatorialValues("int*", "int*[]", "delegate*<void>")] string returnType,
        bool useCompilationReference)
    {
        var lib = CreateCompilation($$"""
            public class C
            {
                public unsafe int M1(int i) => i;
                public unsafe {{returnType}} M2(string s) => null;
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.M1(0);
            c.M2(null);
            unsafe { c.M2(null); }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'C.M2(string)' must be used in an unsafe context because it has pointers in its signature
            // c.M2(null);
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.M2(null)").WithArguments("C.M2(string)").WithLocation(3, 1));

        CompileAndVerify("""
            var c = new C();
            c.M1(0);
            unsafe { c.M2(null); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.M2"],
                expectedSafeSymbols: ["C", "C.M1"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.M2(null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.M2(null)").WithLocation(3, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_ConstraintType(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public class C
            {
                public unsafe void M<T>(T t) where T : I<int*[]> { }
            }
            public interface I<T>;
            public unsafe class D : I<int*[]>;
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.M<D>(null);
            """;

        CompileAndVerify(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            symbolValidator: validate)
            .VerifyDiagnostics();

        CompileAndVerify(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe,
            symbolValidator: validate)
            .VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            VerifyRequiresUnsafeAttribute(
                module.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "I", "C.M", "D"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_DefaultParameterValue(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public class C
            {
                public unsafe void M(string s = nameof(I<int*[]>)) { }
            }
            public interface I<T>;
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.M(s: null);
            """;

        CompileAndVerify(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            symbolValidator: validate)
            .VerifyDiagnostics();

        CompileAndVerify(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe,
            symbolValidator: validate)
            .VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            VerifyRequiresUnsafeAttribute(
                module.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M", "I"]);
        }
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_ExtensionMethod_ReceiverType(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public static class E
            {
                public static unsafe void M1(this int x) { }
                public static unsafe void M2(this int*[] y) { }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            123.M1();
            new int*[0].M2();
            unsafe { new int*[0].M2(); }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,1): error CS9503: 'E.M2(int*[])' must be used in an unsafe context because it has pointers in its signature
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new int*[0].M2()").WithArguments("E.M2(int*[])").WithLocation(2, 1));

        CompileAndVerify("""
            123.M1();
            unsafe { new int*[0].M2(); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["E.M2"],
                expectedSafeSymbols: ["E", "E.M1"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (2,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 5),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0]").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0].M2()").WithLocation(2, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Method_ExtensionMember_ReceiverType(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public unsafe static class E
            {
                extension(int x)
                {
                    public void M1() { }
                }

                extension(int*[] y)
                {
                    public void M2() { }
                }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            123.M1();
            new int*[0].M2();
            unsafe { new int*[0].M2(); }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,1): error CS9503: 'E.extension(int*[]).M2()' must be used in an unsafe context because it has pointers in its signature
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new int*[0].M2()").WithArguments("E.extension(int*[]).M2()").WithLocation(2, 1));

        CompileAndVerify("""
            123.M1();
            unsafe { new int*[0].M2(); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["E.M2", ExtensionMember("E", "M2")],
                expectedSafeSymbols: ["E", "E.M1", ExtensionMember("E", "M1")],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (2,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 5),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0]").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].M2();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0].M2()").WithLocation(2, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Property(
        [CombinatorialValues("int*", "int*[]", "delegate*<void>")] string type,
        bool useCompilationReference)
    {
        var lib = CreateCompilation($$"""
            public class C
            {
                public unsafe int P1 { get; set; }
                public unsafe {{type}} P2 { get; set; }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.P1 = c.P1;
            c.P2 = c.P2;
            unsafe { c.P2 = c.P2; }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'C.P2.set' must be used in an unsafe context because it has pointers in its signature
            // c.P2 = c.P2;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.P2").WithArguments("C.P2.set").WithLocation(3, 1),
            // (3,8): error CS9503: 'C.P2.get' must be used in an unsafe context because it has pointers in its signature
            // c.P2 = c.P2;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.P2").WithArguments("C.P2.get").WithLocation(3, 8));

        CompileAndVerify("""
            var c = new C();
            c.P1 = c.P1;
            unsafe { c.P2 = c.P2; }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.P2", "C.get_P2", "C.set_P2"],
                expectedSafeSymbols: ["C", "C.P1", "C.get_P1", "C.set_P1"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.P2 = c.P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.P2").WithLocation(3, 1),
            // (3,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.P2 = c.P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.P2").WithLocation(3, 8),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.P2 = c.P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.P2 = c.P2").WithLocation(3, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Property_Extension_ReceiverType(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public unsafe static class E
            {
                extension(int x)
                {
                    public int P1 { get => 0; set { } }
                }

                extension(int*[] y)
                {
                    public int P2 { get => 0; set { } }
                }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var x = 123;
            x.P1 = x.P1;
            new int*[0].P2 = new int*[0].P2;
            unsafe { new int*[0].P2 = new int*[0].P2; }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'E.extension(int*[]).P2.set' must be used in an unsafe context because it has pointers in its signature
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new int*[0].P2").WithArguments("E.extension(int*[]).P2.set").WithLocation(3, 1),
            // (3,18): error CS9503: 'E.extension(int*[]).P2.get' must be used in an unsafe context because it has pointers in its signature
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new int*[0].P2").WithArguments("E.extension(int*[]).P2.get").WithLocation(3, 18));

        CompileAndVerify("""
            var x = 123;
            x.P1 = x.P1;
            unsafe { new int*[0].P2 = new int*[0].P2; }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [ExtensionMember("E", "P2"), "E.get_P2", ExtensionMember("E", "get_P2"), "E.set_P2", ExtensionMember("E", "set_P2")],
                expectedSafeSymbols: ["E", ExtensionMember("E", "P1"), "E.get_P1", ExtensionMember("E", "get_P1"), "E.set_P1", ExtensionMember("E", "set_P1")],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 5),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0]").WithLocation(3, 1),
            // (3,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 22),
            // (3,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // new int*[0].P2 = new int*[0].P2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[0]").WithLocation(3, 18));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Indexer(
        [CombinatorialValues("int*", "int*[]", "delegate*<void>")] string type,
        bool useCompilationReference)
    {
        var lib = CreateCompilation($$"""
            public class C1
            {
                public unsafe int this[int i] { get => i; set { } }
            }
            public class C2
            {
                public unsafe {{type}} this[int i] { get => null; set { } }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c1 = new C1();
            c1[0] = c1[0];
            var c2 = new C2();
            c2[0] = c2[0];
            unsafe { c2[0] = c2[0]; }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (4,1): error CS9503: 'C2.this[int].set' must be used in an unsafe context because it has pointers in its signature
            // c2[0] = c2[0];
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c2[0]").WithArguments("C2.this[int].set").WithLocation(4, 1),
            // (4,9): error CS9503: 'C2.this[int].get' must be used in an unsafe context because it has pointers in its signature
            // c2[0] = c2[0];
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c2[0]").WithArguments("C2.this[int].get").WithLocation(4, 9));

        CompileAndVerify("""
            var c1 = new C1();
            c1[0] = c1[0];
            var c2 = new C2();
            unsafe { c2[0] = c2[0]; }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C2.this[]", "C2.get_Item", "C2.set_Item"],
                expectedSafeSymbols: ["C1", "C2", "C1.this[]", "C1.get_Item", "C1.set_Item"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (4,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c2[0] = c2[0];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c2[0]").WithLocation(4, 1),
            // (4,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c2[0] = c2[0];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c2[0]").WithLocation(4, 9),
            // (4,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c2[0] = c2[0];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c2[0] = c2[0]").WithLocation(4, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Event(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            #pragma warning disable CS0067 // unused event
            public class C
            {
                public unsafe event System.Action E1;
                public unsafe event System.Action<int*[]> E2;
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c.E1 += null;
            c.E2 += null;
            unsafe { c.E2 += null; }
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'C.E2' must be used in an unsafe context because it has pointers in its signature
            // c.E2 += null;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.E2").WithArguments("C.E2").WithLocation(3, 1));

        CompileAndVerify("""
            var c = new C();
            c.E1 += null;
            unsafe { c.E2 += null; }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.E2", "C.add_E2", "C.remove_E2"],
                expectedSafeSymbols: ["C", "C.E1", "C.add_E1", "C.remove_E1"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // c.E2 += null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.E2").WithLocation(3, 1));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Constructor(bool useCompilationReference)
    {
        var lib = CreateCompilation("""
            public class C
            {
                public unsafe C() { }
                public unsafe C(int* p) { }
            }
            """,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            _ = new C();
            _ = new C(null);
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,5): error CS9503: 'C.C(int*)' must be used in an unsafe context because it has pointers in its signature
            // _ = new C(null);
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new C(null)").WithArguments("C.C(int*)").WithLocation(2, 5));

        CompileAndVerify("""
            _ = new C();
            unsafe { _ = new C(null); }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [Overload("C..ctor", parameterCount: 1)],
                expectedSafeSymbols: ["C", Overload("C..ctor", parameterCount: 0)],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics(
            // (2,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = new C(null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(null)").WithLocation(2, 5),
            // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = new C(null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null").WithLocation(2, 11));
    }

    [Theory, CombinatorialData]
    public void CompatMode_Operator(bool useCompilationReference)
    {
        var lib = CreateCompilation(
            [
                """
                public class C
                {
                    public unsafe void operator +=(int i) { }
                    public unsafe void operator -=(int* p) { }
                }
                """,
                CompilerFeatureRequiredAttribute,
            ],
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();
        var libRef = AsReference(lib, useCompilationReference);

        var source = """
            var c = new C();
            c += 0;
            c -= null;
            """;

        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,1): error CS9503: 'C.operator -=(int*)' must be used in an unsafe context because it has pointers in its signature
            // c -= null;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c -= null").WithArguments("C.operator -=(int*)").WithLocation(3, 1));

        CompileAndVerify("""
            var c = new C();
            c += 0;
            unsafe { c -= null; }
            """,
            [libRef],
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.op_SubtractionAssignment"],
                expectedSafeSymbols: ["C", "C.op_AdditionAssignment"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit))
            .VerifyDiagnostics();

        // https://github.com/dotnet/roslyn/issues/81967: operator invocations involving pointers are allowed outside unsafe context
        CreateCompilation(source,
            [libRef],
            options: TestOptions.UnsafeReleaseExe)
            .VerifyEmitDiagnostics();
    }

    [Fact]
    public void Extern_Method()
    {
        var libSource = """
            #pragma warning disable CS0626 // extern without attributes
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            public class C
            {
                public void M1() { }
                public extern void M2();
                [DllImport("test")] public static extern void M3();
                [MethodImpl(MethodImplOptions.InternalCall)] public extern void M4();
            }
            """;

        var callerSource = """
            var c = new C();
            c.M1();
            c.M2();
            C.M3();
            c.M4();
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.M2", "C.M3", "C.M4"],
            expectedSafeSymbols: ["C", "C.M1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.M2()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M2();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M2()").WithArguments("C.M2()").WithLocation(3, 1),
                // (4,1): error CS9502: 'C.M3()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // C.M3();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "C.M3()").WithArguments("C.M3()").WithLocation(4, 1),
                // (5,1): error CS9502: 'C.M4()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M4();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M4()").WithArguments("C.M4()").WithLocation(5, 1),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(libSource,
            assemblyName: "lib")
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.M1", "C.M2", "C.M3", "C.M4"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Method_WithPointers()
    {
        static string getLibSource(string modifiers) => $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} int* M();
            }
            """;

        var callerSource = """
            var c = new C();
            c.M();
            """;

        var libUpdated = CreateCompilation(
            getLibSource("extern"),
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9502: 'C.M()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M()").WithArguments("C.M()").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]);
        }

        CreateCompilation(getLibSource("extern")).VerifyDiagnostics(
            // (4,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public extern int* M();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 19));

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        var libLegacy = CreateCompilation(
            getLibSource("unsafe extern"),
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9503: 'C.M()' must be used in an unsafe context because it has pointers in its signature
                // c.M();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.M()").WithArguments("C.M()").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Fact]
    public void Extern_Method_Explicit()
    {
        var libSource = """
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public unsafe extern void M();
            }
            """;

        var callerSource = """
            var c = new C();
            c.M();
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.M"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.M()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.M();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.M()").WithArguments("C.M()").WithLocation(2, 1),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(
            libSource,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.M"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Property()
    {
        var libSource = """
            #pragma warning disable CS0626 // extern without attributes
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            public class C
            {
                public int P1 { set { } }
                public extern int P2 { set; }
                public static extern int P3 { [DllImport("test")] set; }
                public extern int P4 { [MethodImpl(MethodImplOptions.InternalCall)] set; }
            }
            """;

        var callerSource = """
            var c = new C();
            c.P1 = 0;
            c.P2 = 0;
            C.P3 = 0;
            c.P4 = 0;
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.P2", "C.set_P2", "C.P3", "C.set_P3", "C.P4", "C.set_P4"],
            expectedSafeSymbols: ["C", "C.P1", "C.set_P1"],
            expectedDiagnostics:
            [
                // (3,1): error CS9502: 'C.P2.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P2 = 0;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P2").WithArguments("C.P2.set").WithLocation(3, 1),
                // (4,1): error CS9502: 'C.P3.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // C.P3 = 0;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "C.P3").WithArguments("C.P3.set").WithLocation(4, 1),
                // (5,1): error CS9502: 'C.P4.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P4 = 0;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P4").WithArguments("C.P4.set").WithLocation(5, 1),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(libSource,
            assemblyName: "lib")
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.P1", "C.set_P1", "C.P2", "C.set_P2", "C.P3", "C.set_P3", "C.P4", "C.set_P4"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Property_WithPointers()
    {
        static string getLibSource(string modifiers) => $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} int* P { set; }
            }
            """;

        var callerSource = """
            var c = new C();
            c.P = null;
            """;

        var libUpdated = CreateCompilation(
            getLibSource("extern"),
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9502: 'C.P.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P = null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P").WithArguments("C.P.set").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C.P", "C.set_P"],
                expectedSafeSymbols: ["C"]);
        }

        CreateCompilation(getLibSource("extern")).VerifyDiagnostics(
            // (4,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public extern int* P { set; }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 19));

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        var libLegacy = CreateCompilation(
            getLibSource("unsafe extern"),
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9503: 'C.P.set' must be used in an unsafe context because it has pointers in its signature
                // c.P = null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c.P").WithArguments("C.P.set").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.P", "C.set_P"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Fact]
    public void Extern_Property_Explicit()
    {
        var libSource = """
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public unsafe extern int P { set; }
            }
            """;

        var callerSource = """
            var c = new C();
            c.P = 0;
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.P", "C.set_P"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.P.set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c.P = 0;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c.P").WithArguments("C.P.set").WithLocation(2, 1),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(
            libSource,
            options: TestOptions.UnsafeReleaseDll,
            assemblyName: "lib")
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.P", "C.set_P"]))
                .VerifyDiagnostics();
        }
    }

    [Theory, CombinatorialData]
    public void Extern_Indexer([CombinatorialValues("      ", "unsafe")] string modifiers)
    {
        var libSource = $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} extern int this[int i] { get; set; }
            }
            """;

        var callerSource = """
            var c = new C();
            c[0] = c[0] + 123;
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.this[]", "C.get_Item", "C.set_Item"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.this[int].set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c[0]").WithArguments("C.this[int].set").WithLocation(2, 1),
                // (2,8): error CS9502: 'C.this[int].get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c[0]").WithArguments("C.this[int].get").WithLocation(2, 8),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(libSource,
            assemblyName: "lib",
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.this[]", "C.get_Item", "C.set_Item"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Indexer_WithPointers()
    {
        static string getLibSource(string modifiers) => $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} int* this[int i] { get; set; }
            }
            """;

        var callerSource = """
            var c = new C();
            c[0] = c[0] + 123;
            """;

        var libUpdated = CreateCompilation(
            getLibSource("extern"),
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9502: 'C.this[int].set' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c[0]").WithArguments("C.this[int].set").WithLocation(2, 1),
                // (2,8): error CS9502: 'C.this[int].get' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c[0]").WithArguments("C.this[int].get").WithLocation(2, 8))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C.this[]", "C.get_Item", "C.set_Item"],
                expectedSafeSymbols: ["C"]);
        }

        CreateCompilation(getLibSource("extern")).VerifyDiagnostics(
            // (4,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public extern int* P { set; }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 19));

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        var libLegacy = CreateCompilation(
            getLibSource("unsafe extern"),
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9503: 'C.this[int].set' must be used in an unsafe context because it has pointers in its signature
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c[0]").WithArguments("C.this[int].set").WithLocation(2, 1),
                // (2,8): error CS9503: 'C.this[int].get' must be used in an unsafe context because it has pointers in its signature
                // c[0] = c[0] + 123;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c[0]").WithArguments("C.this[int].get").WithLocation(2, 8))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.this[]", "C.get_Item", "C.set_Item"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Theory, CombinatorialData]
    public void Extern_Event([CombinatorialValues("      ", "unsafe")] string modifiers)
    {
        var libSource = $$"""
            #pragma warning disable CS0067 // unused event
            public class C
            {
                [method: System.Runtime.InteropServices.DllImport("test")]
                public static {{modifiers}} extern event System.Action E;
            }
            """;

        var callerSource = """
            C.E += null;
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.E", "C.add_E", "C.remove_E"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (1,1): error CS9502: 'C.E' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // C.E += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "C.E").WithArguments("C.E").WithLocation(1, 1)
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(libSource,
            assemblyName: "lib",
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.E", "C.add_E", "C.remove_E"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Event_WithPointers()
    {
        var libSource = """
            #pragma warning disable CS0067 // unused event
            public class C
            {
                [method: System.Runtime.InteropServices.DllImport("test")]
                public static extern event System.Action<int*[]> E;
            }
            """;

        var callerSource = """
            C.E += null;
            """;

        var libUpdated = CreateCompilation(
            libSource,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (1,1): error CS9502: 'C.E' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // C.E += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "C.E").WithArguments("C.E").WithLocation(1, 1))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C.E", "C.add_E", "C.remove_E"],
                expectedSafeSymbols: ["C"]);
        }

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        // https://github.com/dotnet/roslyn/issues/81944: There is no error for the pointer even though `unsafe` is missing.
        var libLegacy = CreateCompilation(
            libSource)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (1,1): error CS9503: 'C.E' must be used in an unsafe context because it has pointers in its signature
                // C.E += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "C.E").WithArguments("C.E").WithLocation(1, 1))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.E", "C.add_E", "C.remove_E"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Theory, CombinatorialData]
    public void Extern_Constructor([CombinatorialValues("      ", "unsafe")] string modifiers)
    {
        var libSource = $$"""
            #pragma warning disable CS0824 // extern constructor
            public class C
            {
                public {{modifiers}} extern C();
            }
            """;

        var callerSource = """
            _ = new C();
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C..ctor"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (1,5): error CS9502: 'C.C()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // _ = new C();
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "new C()").WithArguments("C.C()").WithLocation(1, 5),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation(libSource,
            assemblyName: "lib",
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C..ctor"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Constructor_WithPointers()
    {
        static string getLibSource(string modifiers) => $$"""
            #pragma warning disable CS0824 // extern constructor
            public class C
            {
                public {{modifiers}} C(int* p);
            }
            """;

        var callerSource = """
            _ = new C(null);
            """;

        var libUpdated = CreateCompilation(
            getLibSource("extern"),
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (1,5): error CS9502: 'C.C(int*)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // _ = new C(null);
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "new C(null)").WithArguments("C.C(int*)").WithLocation(1, 5))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C..ctor"],
                expectedSafeSymbols: ["C"]);
        }

        CreateCompilation(getLibSource("extern")).VerifyDiagnostics(
            // (4,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public extern C(int* p);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 21));

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        var libLegacy = CreateCompilation(
            getLibSource("unsafe extern"),
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (1,5): error CS9503: 'C.C(int*)' must be used in an unsafe context because it has pointers in its signature
                // _ = new C(null);
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "new C(null)").WithArguments("C.C(int*)").WithLocation(1, 5))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C..ctor"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
    }

    [Theory, CombinatorialData]
    public void Extern_Operator([CombinatorialValues("      ", "unsafe")] string modifiers)
    {
        var libSource = $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} extern void operator +=(C c);
            }
            """;

        var callerSource = """
            var c = new C();
            c += c;
            """;

        CompileAndVerifyUnsafe(
            libSource,
            callerSource,
            additionalSources: [CompilerFeatureRequiredAttribute],
            verify: Verification.Skipped,
            expectedUnsafeSymbols: ["C.op_AdditionAssignment"],
            expectedSafeSymbols: ["C"],
            expectedDiagnostics:
            [
                // (2,1): error CS9502: 'C.operator +=(C)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c += c;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c += c").WithArguments("C.operator +=(C)").WithLocation(2, 1),
            ]);

        // When compiling the lib under legacy rules, extern members are not unsafe.
        var lib = CreateCompilation([libSource, CompilerFeatureRequiredAttribute],
            assemblyName: "lib",
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            CompileAndVerify(callerSource,
                [AsReference(lib, useCompilationReference)],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(),
                verify: Verification.Skipped,
                symbolValidator: m => VerifyRequiresUnsafeAttribute(
                    m.ReferencedAssemblySymbols.Single(a => a.Name == "lib").Modules.Single(),
                    includesAttributeDefinition: false,
                    expectedUnsafeSymbols: [],
                    expectedSafeSymbols: ["C", "C.op_AdditionAssignment"]))
                .VerifyDiagnostics();
        }
    }

    [Fact]
    public void Extern_Operator_WithPointers()
    {
        static string getLibSource(string modifiers) => $$"""
            #pragma warning disable CS0626 // extern without attributes
            public class C
            {
                public {{modifiers}} void operator +=(int* p);
            }
            """;

        var callerSource = """
            var c = new C();
            c += null;
            """;

        var libUpdated = CreateCompilation(
            [getLibSource("extern"), CompilerFeatureRequiredAttribute],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libUpdatedRef = AsReference(libUpdated, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libUpdatedRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9502: 'C.operator +=(int*)' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
                // c += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "c += null").WithArguments("C.operator +=(int*)").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libUpdatedRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: !useCompilationReference,
                isSynthesized: useCompilationReference ? null : true,
                expectedUnsafeSymbols: ["C.op_AdditionAssignment"],
                expectedSafeSymbols: ["C"]);
        }

        CreateCompilation([getLibSource("extern"), CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
            // (4,36): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public extern void operator +=(int* p);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 36));

        // When compiling the lib under legacy rules, extern members are not unsafe, but members with pointers are.
        var libLegacy = CreateCompilation(
            [getLibSource("unsafe extern"), CompilerFeatureRequiredAttribute],
            options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics();

        foreach (var useCompilationReference in new[] { false, true })
        {
            var libLegacyRef = AsReference(libLegacy, useCompilationReference);

            var libAssemblySymbol = CreateCompilation(callerSource,
                [libLegacyRef],
                options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
                .VerifyDiagnostics(
                // (2,1): error CS9503: 'C.operator +=(int*)' must be used in an unsafe context because it has pointers in its signature
                // c += null;
                Diagnostic(ErrorCode.ERR_UnsafeMemberOperationCompat, "c += null").WithArguments("C.operator +=(int*)").WithLocation(2, 1))
                .GetReferencedAssemblySymbol(libLegacyRef);

            VerifyRequiresUnsafeAttribute(
                libAssemblySymbol.Modules.Single(),
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.op_AdditionAssignment"],
                expectedSafeSymbols: ["C"],
                expectedUnsafeMode: CallerUnsafeMode.Implicit);
        }
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M1", "C.M2"]))
            .VerifyDiagnostics();

        var ref1 = CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: true,
                expectedUnsafeSymbols: ["C.M1"],
                expectedSafeSymbols: ["C", "C.M2"]))
            .VerifyDiagnostics()
            .GetImageReference();

        CompileAndVerify("", [ref1],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: []))
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: true,
                expectedUnsafeSymbols: ["B.M4"],
                expectedSafeSymbols: ["B", "B.M3"]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M1", "C.M2"]))
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithUpdatedMemorySafetyRules(),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();
    }

    [Fact]
    public void RequiresUnsafeAttribute_LocalFunction()
    {
        var source = """
            #pragma warning disable CS8321 // Local function is declared but never used
            class C
            {
                void M()
                {
                    unsafe void M1() { }
                    void M2() { }
                }
            }
            """;

        var m1 = "C.<M>g__M1|0_0";
        var m2 = "C.<M>g__M2|0_1";

        CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [m1, m2]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: true,
                expectedUnsafeSymbols: [m1],
                expectedSafeSymbols: [m2]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [m1, m2]))
            .VerifyDiagnostics();

        CreateCompilation([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics(
            // (6,21): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresUnsafeAttribute' is not defined or imported
            //         unsafe void M1() { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "M1").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(6, 21));
    }

    /// <summary>
    /// Lambdas cannot be marked <see langword="unsafe"/>. If that changes, we should synthesize the attribute similarly to <see cref="RequiresUnsafeAttribute_LocalFunction"/>.
    /// </summary>
    [Fact]
    public void RequiresUnsafeAttribute_Lambda()
    {
        var source = """
            class C
            {
                void M()
                {
                    var lam1 = unsafe () => { };
                    var lam2 = () => { };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS1525: Invalid expression term 'unsafe'
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "unsafe").WithArguments("unsafe").WithLocation(5, 20),
            // (5,20): error CS1002: ; expected
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "unsafe").WithLocation(5, 20),
            // (5,20): error CS0106: The modifier 'unsafe' is not valid for this item
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "unsafe").WithArguments("unsafe").WithLocation(5, 20),
            // (5,28): error CS8124: Tuple must contain at least two elements.
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 28),
            // (5,30): error CS1001: Identifier expected
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(5, 30),
            // (5,30): error CS1003: Syntax error, ',' expected
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(5, 30),
            // (5,33): error CS1002: ; expected
            //         var lam1 = unsafe () => { };
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(5, 33));

        source = """
            class C
            {
                void M()
                {
                    var lam = () => { };
                }
            }
            """;

        var lam = "C.<>c.<M>b__0_0";

        CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [lam]))
            .VerifyDiagnostics();

        CompileAndVerify(source,
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [lam]))
            .VerifyDiagnostics();

        CompileAndVerify([source, MemorySafetyRulesAttributeDefinition],
            options: TestOptions.ReleaseModule.WithAllowUnsafe(true).WithUpdatedMemorySafetyRules().WithMetadataImportOptions(MetadataImportOptions.All),
            verify: Verification.Skipped,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [lam]))
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
                    var count = method.GetCustomAttributes(inherit: false).Count(a => a.GetType().Name == "RequiresUnsafeAttribute");
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: ["C", "C.M"]))
            .VerifyDiagnostics();

        CompileAndVerify([source, RequiresUnsafeAttributeDefinition],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: false,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]))
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RequiresUnsafeAttribute_FromMetadata(bool useCompilationReference)
    {
        var comp = CreateCompilation(RequiresUnsafeAttributeDefinition);
        CompileAndVerify(comp,
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: false,
                expectedUnsafeSymbols: [],
                expectedSafeSymbols: [AttributeDescription.RequiresUnsafeAttribute.FullName]))
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]))
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: true,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]))
            .VerifyDiagnostics();

        // Also defined in source.
        CompileAndVerify([source, RequiresUnsafeAttributeDefinition], [ref1, ref2],
            options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules(),
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: true,
                isSynthesized: false,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]))
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
            symbolValidator: m => VerifyRequiresUnsafeAttribute(
                m,
                includesAttributeDefinition: false,
                expectedUnsafeSymbols: ["C.M"],
                expectedSafeSymbols: ["C"]));

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
        Assert.Equal(CallerUnsafeMode.None, a.GlobalNamespace.GetMember("A.M").CallerUnsafeMode);

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
        //     [RequiresUnsafe(1), RequiresUnsafe()]
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
        Assert.Equal(CallerUnsafeMode.Explicit, a.GlobalNamespace.GetMember("A.M").CallerUnsafeMode);

        var sourceB = """
            A.M();
            """;
        CreateCompilation(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS9502: 'A.M()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
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
        Assert.Equal(CallerUnsafeMode.Explicit, a.GlobalNamespace.GetMember("A.M").CallerUnsafeMode);

        var sourceB = """
            A.M();
            """;
        CreateCompilation(sourceB, [refA],
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS9502: 'A.M()' must be used in an unsafe context because it is marked as 'unsafe' or 'extern'
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

        CSharpTestSource source =
        [
            """
            using System.Runtime.CompilerServices;
            [RequiresUnsafeAttribute] class C
            {
                [RequiresUnsafeAttribute] void M() { }
                [RequiresUnsafeAttribute] int P { get; set; }
                int P2 { [RequiresUnsafeAttribute] get; [RequiresUnsafeAttribute] set; }
            #pragma warning disable CS0067 // unused event
                [RequiresUnsafeAttribute] event System.Action E1;
                event System.Action E2 { [RequiresUnsafeAttribute] add { } [RequiresUnsafeAttribute] remove { } }
                [RequiresUnsafeAttribute] int this[int i] { get => i; set { } }
                [RequiresUnsafeAttribute] C() { }
                [RequiresUnsafeAttribute] ~C() { }
                [RequiresUnsafeAttribute] public static C operator +(C c1, C c2) => c1;
                [RequiresUnsafeAttribute] public void operator +=(C c) { }
                public void M([RequiresUnsafeAttribute] int x) { }
                [return: RequiresUnsafeAttribute] public int Func() => 0;
                public void M<[RequiresUnsafeAttribute] T>() { }
            #pragma warning disable CS0169 // unused field
                [RequiresUnsafeAttribute] int F;
            }
            [RequiresUnsafeAttribute] delegate void D();
            [RequiresUnsafeAttribute] enum E { X }
            """, """
            using System.Runtime.CompilerServices;
            [module: RequiresUnsafeAttribute]
            [assembly: RequiresUnsafeAttribute]
            """,
        ];

        comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], [ref1], options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules(updatedRules));
        comp.VerifyDiagnostics(
            // (4,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] void M() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(4, 6),
            // (5,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] int P { get; set; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(5, 6),
            // (6,15): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     int P2 { [RequiresUnsafeAttribute] get; [RequiresUnsafeAttribute] set; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(6, 15),
            // (6,46): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     int P2 { [RequiresUnsafeAttribute] get; [RequiresUnsafeAttribute] set; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(6, 46),
            // (8,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] event System.Action E1;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(8, 6),
            // (9,31): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     event System.Action E2 { [RequiresUnsafeAttribute] add { } [RequiresUnsafeAttribute] remove { } }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(9, 31),
            // (9,65): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     event System.Action E2 { [RequiresUnsafeAttribute] add { } [RequiresUnsafeAttribute] remove { } }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(9, 65),
            // (10,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] int this[int i] { get => i; set { } }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(10, 6),
            // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] C() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(11, 6),
            // (12,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] ~C() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(12, 6),
            // (13,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] public static C operator +(C c1, C c2) => c1;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(13, 6),
            // (14,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresUnsafeAttribute'. This is reserved for compiler usage.
            //     [RequiresUnsafeAttribute] public void operator +=(C c) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresUnsafeAttribute").WithArguments("System.Runtime.CompilerServices.RequiresUnsafeAttribute").WithLocation(14, 6));
    }
}
