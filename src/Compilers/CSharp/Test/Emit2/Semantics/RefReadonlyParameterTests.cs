// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public partial class RefReadonlyParameterTests : CSharpTestBase
{
    private const string RequiresLocationAttributeName = "RequiresLocationAttribute";
    private const string RequiresLocationAttributeNamespace = "System.Runtime.CompilerServices";
    private const string RequiresLocationAttributeQualifiedName = $"{RequiresLocationAttributeNamespace}.{RequiresLocationAttributeName}";
    private const string InAttributeQualifiedName = "System.Runtime.InteropServices.InAttribute";

    private const string RequiresLocationAttributeDefinition = $$"""
        namespace {{RequiresLocationAttributeNamespace}}
        {
            class {{RequiresLocationAttributeName}} : System.Attribute
            {
            }
        }
        """;

    private static void VerifyRequiresLocationAttributeSynthesized(ModuleSymbol module)
    {
        var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName);
        if (module is SourceModuleSymbol)
        {
            Assert.Null(attributeType);
        }
        else
        {
            Assert.NotNull(attributeType);
        }
    }

    private enum VerifyModifiers
    {
        None,
        In,
        RequiresLocation,
        DoNotVerify
    }

    private static void VerifyRefReadonlyParameter(ParameterSymbol parameter,
        bool refKind = true,
        bool metadataIn = true,
        bool attributes = true,
        VerifyModifiers customModifiers = VerifyModifiers.None,
        bool useSiteError = false,
        bool isProperty = false)
    {
        Assert.Equal(refKind, RefKind.RefReadOnlyParameter == parameter.RefKind);

        Assert.Equal(metadataIn, parameter.IsMetadataIn);

        if (attributes)
        {
            Assert.Empty(parameter.GetAttributes());
        }

        switch (customModifiers)
        {
            case VerifyModifiers.None:
                Assert.Empty(parameter.RefCustomModifiers);
                break;
            case VerifyModifiers.In:
                verifyModifier(parameter, InAttributeQualifiedName, optional: false);
                break;
            case VerifyModifiers.RequiresLocation:
                verifyModifier(parameter, RequiresLocationAttributeQualifiedName, optional: true);
                break;
            case VerifyModifiers.DoNotVerify:
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(customModifiers);
        }

        var method = parameter.ContainingSymbol;
        Assert.IsAssignableFrom(isProperty ? typeof(PropertySymbol) : typeof(MethodSymbol), method);

        if (useSiteError)
        {
            Assert.True(method.HasUnsupportedMetadata);
            Assert.True(method.HasUseSiteError);
            Assert.Equal((int)ErrorCode.ERR_BindToBogus, method.GetUseSiteDiagnostic().Code);
        }
        else
        {
            Assert.False(method.HasUnsupportedMetadata);
            Assert.False(method.HasUseSiteError);
        }

        static void verifyModifier(ParameterSymbol parameter, string qualifiedName, bool optional)
        {
            var mod = Assert.Single(parameter.RefCustomModifiers);
            Assert.Equal(optional, mod.IsOptional);
            Assert.Equal(qualifiedName, mod.Modifier.ToTestDisplayString());
        }
    }

    [Fact]
    public void Method()
    {
        var source = """
            class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void ManuallyDefinedAttribute()
    {
        var source = $$"""
            class C
            {
                public void M(ref readonly int p) { }
            }

            namespace {{RequiresLocationAttributeNamespace}}
            {
                class {{RequiresLocationAttributeName}} : System.Attribute
                {
                }
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            var attribute = m.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName);
            Assert.NotNull(attribute);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void ManuallyAppliedAttribute()
    {
        var source = """
            using System.Runtime.CompilerServices;
            [RequiresLocation] class C
            {
                void M1([RequiresLocation] ref readonly int p) { }
                void M2([RequiresLocation] in int p) { }
                void M3([RequiresLocation] ref int p) { }
                void M4([RequiresLocation] int p) { }
                [return: RequiresLocation] int M5() => 5;
                [return: RequiresLocation] ref int M6() => throw null;
                [return: RequiresLocation] ref readonly int M7() => throw null;
                [RequiresLocation] void M8() { }
                [RequiresLocation] public int field;
                [RequiresLocation] int Property { get => @field; set => @field = value; }
            }
            """;

        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // 0.cs(2,2): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            // [RequiresLocation] class C
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(2, 2),
            // 0.cs(4,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M1([RequiresLocation] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(4, 14),
            // 0.cs(4,36): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M1([RequiresLocation] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 36),
            // 0.cs(5,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M2([RequiresLocation] in int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 14),
            // 0.cs(6,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M3([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(6, 14),
            // 0.cs(7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M4([RequiresLocation] int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 14),
            // 0.cs(8,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] int M5() => 5;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(8, 14),
            // 0.cs(9,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] ref int M6() => throw null;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(9, 14),
            // 0.cs(10,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] ref readonly int M7() => throw null;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(10, 14),
            // 0.cs(11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] void M8() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(11, 6),
            // 0.cs(12,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] public int field;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(12, 6),
            // 0.cs(13,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] int Property { get => @field; set => @field = value; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(13, 6));

        var expectedDiagnostics = new[]
        {
            // 0.cs(2,2): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            // [RequiresLocation] class C
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(2, 2),
            // 0.cs(4,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M1([RequiresLocation] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(4, 14),
            // 0.cs(5,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M2([RequiresLocation] in int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 14),
            // 0.cs(6,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M3([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(6, 14),
            // 0.cs(7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M4([RequiresLocation] int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 14),
            // 0.cs(8,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] int M5() => 5;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(8, 14),
            // 0.cs(9,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] ref int M6() => throw null;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(9, 14),
            // 0.cs(10,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [return: RequiresLocation] ref readonly int M7() => throw null;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(10, 14),
            // 0.cs(11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] void M8() { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(11, 6),
            // 0.cs(12,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] public int field;
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(12, 6),
            // 0.cs(13,6): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     [RequiresLocation] int Property { get => @field; set => @field = value; }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(13, 6)
        };

        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ManuallyAppliedAttribute_NotDefined()
    {
        var source = """
            using System.Runtime.CompilerServices;
            class C
            {
                void M([RequiresLocation] ref int p) { }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using System.Runtime.CompilerServices;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Runtime.CompilerServices;").WithLocation(1, 1),
            // (4,13): error CS0246: The type or namespace name 'RequiresLocationAttribute' could not be found (are you missing a using directive or an assembly reference?)
            //     void M([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RequiresLocation").WithArguments("RequiresLocationAttribute").WithLocation(4, 13),
            // (4,13): error CS0246: The type or namespace name 'RequiresLocation' could not be found (are you missing a using directive or an assembly reference?)
            //     void M([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RequiresLocation").WithArguments("RequiresLocation").WithLocation(4, 13));

        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }).VerifyDiagnostics(
            // 0.cs(4,13): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(4, 13));
    }

    [Fact]
    public void ManuallyAppliedAttributes_RequiresLocationIn()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            class C
            {
                void M1([RequiresLocation, In] ref int p) { }
                void M2([RequiresLocation, In] in int p) { }
                void M3([RequiresLocation, In] int p) { }
            }
            """;
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }).VerifyDiagnostics(
            // 0.cs(5,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M1([RequiresLocation, In] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 14),
            // 0.cs(6,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M2([RequiresLocation, In] in int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(6, 14),
            // 0.cs(7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M3([RequiresLocation, In] int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 14));
    }

    [Fact]
    public void ManuallyAppliedAttributes_In()
    {
        var source = """
            using System.Runtime.InteropServices;
            class C
            {
                public void M([In] ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, attributes: m is not SourceModuleSymbol);
            if (m is SourceModuleSymbol)
            {
                var attribute = Assert.Single(p.GetAttributes());
                Assert.Equal("System.Runtime.InteropServices.InAttribute", attribute.AttributeClass.ToTestDisplayString());
                Assert.Empty(attribute.ConstructorArguments);
                Assert.Empty(attribute.NamedArguments);
            }
        }
    }

    [Fact]
    public void ManuallyAppliedAttributes_InOut()
    {
        var source = """
            using System.Runtime.InteropServices;
            class C
            {
                void M1([Out] ref readonly int p) { }
                void M2([In, Out] ref readonly int p) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,36): error CS9199: A ref readonly parameter cannot have the Out attribute.
            //     void M1([Out] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_OutAttrOnRefReadonlyParam, "p").WithLocation(4, 36),
            // (5,40): error CS9199: A ref readonly parameter cannot have the Out attribute.
            //     void M2([In, Out] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_OutAttrOnRefReadonlyParam, "p").WithLocation(5, 40));
    }

    [Fact]
    public void ManuallyAppliedAttributes_IsReadOnly()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            class C
            {
                void M1([IsReadOnly] ref readonly int p) { }
                void M2([In, IsReadOnly] ref readonly int p) { }
                void M3([In, RequiresLocation, IsReadOnly] ref int p) { }
            }

            namespace System.Runtime.CompilerServices
            {
                public class IsReadOnlyAttribute : System.Attribute { }
            }
            """;
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }).VerifyDiagnostics(
            // 0.cs(5,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
            //     void M1([IsReadOnly] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(5, 14),
            // 0.cs(6,18): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
            //     void M2([In, IsReadOnly] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 18),
            // 0.cs(7,18): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M3([In, RequiresLocation, IsReadOnly] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 18),
            // 0.cs(7,36): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
            //     void M3([In, RequiresLocation, IsReadOnly] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(7, 36));
    }

    [Fact]
    public void BothAttributes()
    {
        // public class C
        // {
        //     public void M([IsReadOnly] ref readonly int p) { }
        // }
        var ilSource = """
            .class public auto ansi abstract sealed beforefieldinit C extends System.Object
            {
                .method public hidebysig instance void M([in] int32& p) cil managed
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
                            01 00 00 00
                        )
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var comp = CreateCompilationWithIL("", ilSource).VerifyDiagnostics();

        var p = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
        VerifyRefReadonlyParameter(p, attributes: false);
        var attribute = Assert.Single(p.GetAttributes());
        Assert.Equal("System.Runtime.CompilerServices.IsReadOnlyAttribute", attribute.AttributeClass.ToTestDisplayString());
        Assert.Empty(attribute.ConstructorArguments);
        Assert.Empty(attribute.NamedArguments);
    }

    [Fact]
    public void ReturnParameter()
    {
        // public class C
        // {
        //     [return: RequiresLocation]
        //     public ref int M() { }
        // }
        var ilSource = """
            .class public auto ansi abstract sealed beforefieldinit C extends System.Object
            {
                .method public hidebysig instance int32& M() cil managed
                {
                    .param [0]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var comp = CreateCompilationWithIL("", ilSource).VerifyDiagnostics();

        var m = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");
        Assert.Equal(RefKind.Ref, m.RefKind);
    }

    [Fact]
    public void Modreq_NonVirtual()
    {
        // public class C
        // {
        //     public void M(modreq(In) ref readonly int p) { }
        // }
        var ilSource = """
            .class public auto ansi abstract sealed beforefieldinit C extends System.Object
            {
                .method public hidebysig instance void M(
                    [in] int32& modreq(System.Runtime.InteropServices.InAttribute) p
                    ) cil managed
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.InAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var comp = CreateCompilationWithIL("", ilSource).VerifyDiagnostics();

        var p = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
        VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In, useSiteError: true);
    }

    [Fact]
    public void Method_Virtual()
    {
        var source = """
            class C
            {
                public virtual void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Fact]
    public void Method_Abstract()
    {
        var source = """
            abstract class C
            {
                public abstract void M(ref readonly int p);
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Fact]
    public void Constructor()
    {
        var source = """
            class C
            {
                public C(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C..ctor").Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void AttributeConstructor()
    {
        var source = """
            [A(1)]
            class A : System.Attribute
            {
                A(ref readonly int x) { }
            }
            
            [B()]
            class B : System.Attribute
            {
                B(ref readonly int x = 2) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (1,2): error CS8358: Cannot use attribute constructor 'A.A(ref readonly int)' because it has 'in' or 'ref readonly' parameters.
            // [A(1)]
            Diagnostic(ErrorCode.ERR_AttributeCtorInParameter, "A(1)").WithArguments("A.A(ref readonly int)").WithLocation(1, 2),
            // (1,4): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            // [A(1)]
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "1").WithArguments("1").WithLocation(1, 4),
            // (7,2): error CS8358: Cannot use attribute constructor 'B.B(ref readonly int)' because it has 'in' or 'ref readonly' parameters.
            // [B()]
            Diagnostic(ErrorCode.ERR_AttributeCtorInParameter, "B()").WithArguments("B.B(ref readonly int)").WithLocation(7, 2),
            // (10,28): warning CS9521: A default value is specified for 'ref readonly' parameter 'x', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     B(ref readonly int x = 2) { }
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "2").WithArguments("x").WithLocation(10, 28));
    }

    [Fact]
    public void PrimaryConstructor_Class()
    {
        var source = """
            class C(ref readonly int p);
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (1,26): warning CS9113: Parameter 'p' is unread.
            // class C(ref readonly int p);
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p").WithArguments("p").WithLocation(1, 26));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C..ctor").Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void PrimaryConstructor_Struct()
    {
        var source = """
            struct C(ref readonly int p);
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (1,27): warning CS9113: Parameter 'p' is unread.
            // struct C(ref readonly int p);
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p").WithArguments("p").WithLocation(1, 27));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var c = m.GlobalNamespace.GetTypeMember("C");
            var ctor = c.InstanceConstructors.Single(s => s.Parameters is [{ Name: "p" }]);
            var p = ctor.Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void PrimaryConstructor_Record()
    {
        var source = """
            record C(ref readonly int p);
            """;
        var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition },
            sourceSymbolValidator: verify, symbolValidator: verify,
            verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var c = m.GlobalNamespace.GetTypeMember("C");
            var ctor = c.InstanceConstructors.Single(s => s.Parameters is [{ Name: "p" }]);
            var p = ctor.Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void PrimaryConstructor_RecordStruct()
    {
        var source = """
            record struct C(ref readonly int p);
            """;
        var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition },
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var c = m.GlobalNamespace.GetTypeMember("C");
            var ctor = c.InstanceConstructors.Single(s => s.Parameters is [{ Name: "p" }]);
            var p = ctor.Parameters.Single();
            VerifyRefReadonlyParameter(p);
        }
    }

    [Fact]
    public void Operators()
    {
        var source = """
            class C
            {
                public static C operator+(ref readonly C x, C y) => x;
                public static C operator--(ref readonly C x) => x;
                public static implicit operator C(ref readonly int x) => null;
                public static explicit operator C(ref readonly short x) => null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,29): error CS0631: ref and out are not valid in this context
            //     public static C operator+(ref readonly C x, C y) => x;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "+").WithLocation(3, 29),
            // (4,29): error CS0631: ref and out are not valid in this context
            //     public static C operator--(ref readonly C x) => x;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "--").WithLocation(4, 29),
            // (5,37): error CS0631: ref and out are not valid in this context
            //     public static implicit operator C(ref readonly int x) => null;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "C").WithLocation(5, 37),
            // (6,37): error CS0631: ref and out are not valid in this context
            //     public static explicit operator C(ref readonly short x) => null;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "C").WithLocation(6, 37));
    }

    [Fact]
    public void ExpressionTrees_Invalid()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            Expression<D> e1 = (ref readonly int p) => C.M(in p);
            Expression<D> e2 = (ref readonly int p) => C.M(ref p);
            Expression<D> e3 = (ref readonly int p) => C.M(p);
            Expression<D> e4 = (int p) => C.M(in p);
            Expression<Action<int>> e5 = (int p) => C.M(out p);

            delegate void D(ref readonly int p);

            static class C
            {
                public static void M(ref readonly int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,38): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
            // Expression<D> e1 = (ref readonly int p) => C.M(in p);
            Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "p").WithLocation(4, 38),
            // (5,52): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            // Expression<D> e2 = (ref readonly int p) => C.M(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(5, 52),
            // (6,38): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
            // Expression<D> e3 = (ref readonly int p) => C.M(p);
            Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "p").WithLocation(6, 38),
            // (6,48): warning CS9195: Argument 1 should be passed with the 'in' keyword
            // Expression<D> e3 = (ref readonly int p) => C.M(p);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "p").WithArguments("1").WithLocation(6, 48),
            // (7,25): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // Expression<D> e4 = (int p) => C.M(in p);
            Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "ref readonly").WithLocation(7, 25),
            // (7,28): error CS1661: Cannot convert lambda expression to type 'Expression<D>' because the parameter types do not match the delegate parameter types
            // Expression<D> e4 = (int p) => C.M(in p);
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression<D>").WithLocation(7, 28),
            // (8,49): error CS1615: Argument 1 may not be passed with the 'out' keyword
            // Expression<Action<int>> e5 = (int p) => C.M(out p);
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "p").WithArguments("1", "out").WithLocation(8, 49));
    }

    [Fact]
    public void ExpressionTrees_Valid()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.E((int p) => C.M(in p));
            C.E((int p) => C.M(ref p));
            C.E((int p) => C.M(p));
            C.E((int p) => C.M(5));

            static class C
            {
                public static void M(ref readonly int x) => Console.Write(x);
                public static void E(Expression<Action<int>> e) => e.Compile()(4);
            }
            """;
        CompileAndVerify(source, expectedOutput: "4445").VerifyDiagnostics(
            // (6,20): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            // C.E((int p) => C.M(p));
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "p").WithArguments("1").WithLocation(6, 20),
            // (7,20): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            // C.E((int p) => C.M(5));
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("1").WithLocation(7, 20));
    }

    [Fact]
    public void Delegate()
    {
        var source = """
            delegate void D(ref readonly int p);
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("D.Invoke").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Fact]
    public void Lambda()
    {
        var source = """
            var lam = (ref readonly int p) => { };
            System.Console.WriteLine(lam.GetType());
            """;
        var verifier = CompileAndVerify(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
            sourceSymbolValidator: verify, symbolValidator: verify,
            expectedOutput: "<>A{00000004}`1[System.Int32]");
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            if (m is not SourceModuleSymbol)
            {
                var p = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<>c.<<Main>$>b__0_0").Parameters.Single();
                VerifyRefReadonlyParameter(p);
            }
        }
    }

    [Fact]
    public void LocalFunction()
    {
        var source = """
            void local(ref readonly int p) { }
            System.Console.WriteLine(((object)local).GetType());
            """;
        var verifier = CompileAndVerify(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
            sourceSymbolValidator: verify, symbolValidator: verify,
            expectedOutput: "<>A{00000004}`1[System.Int32]");
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            if (m is not SourceModuleSymbol)
            {
                var p = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<<Main>$>g__local|0_0").Parameters.Single();
                VerifyRefReadonlyParameter(p);
            }
        }
    }

    [Theory]
    [InlineData("var x = (ref readonly int p) => { };")]
    [InlineData("var x = local; void local(ref readonly int p) { }")]
    public void AnonymousDelegate(string def)
    {
        var source = $"""
            {def}
            System.Console.WriteLine(((object)x).GetType());
            """;
        var verifier = CompileAndVerify(source, sourceSymbolValidator: verify, symbolValidator: verify,
            expectedOutput: "<>A{00000004}`1[System.Int32]");
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            if (m is not SourceModuleSymbol)
            {
                var p = m.GlobalNamespace.GetMember<MethodSymbol>("<>A{00000004}.Invoke").Parameters.Single();
                VerifyRefReadonlyParameter(p,
                    // Invoke method is virtual but no modreq is emitted. https://github.com/dotnet/roslyn/issues/69079
                    useSiteError: true);
            }
        }
    }

    [Fact]
    public void FunctionPointer()
    {
        var source = """
            class C
            {
                public unsafe void M(delegate*<ref readonly int, void> p) { }
            }
            """;
        var verifier = CompileAndVerify(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeReleaseDll,
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            Assert.NotNull(m.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName));

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            var ptr = (FunctionPointerTypeSymbol)p.Type;
            var p2 = ptr.Signature.Parameters.Single();
            VerifyRefReadonlyParameter(p2, customModifiers: VerifyModifiers.RequiresLocation);
        }
    }

    [Fact]
    public void FunctionPointer_Modopt_CustomAttribute_In()
    {
        // public class C
        // {
        //     public unsafe delegate*<in int modopt(MyAttribute), void> D;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .field public method void *(int32& modreq(System.Runtime.InteropServices.InAttribute) modopt(MyAttribute)) D
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.InAttribute extends System.Object
            {
            }

            .class public auto ansi sealed beforefieldinit MyAttribute extends System.Object
            {
            }
            """;

        var source = """
            class D
            {
                unsafe void M(C c)
                {
                    int x = 6;
                    c.D(x);
                    c.D(ref x);
                    c.D(in x);
                }
            }
            """;

        CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (7,17): error CS9194: Argument 1 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version 12.0 or greater.
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "x").WithArguments("1", "11.0", "12.0").WithLocation(7, 17));

        var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (7,17): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         c.D(ref x);
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(7, 17));

        var ptr = (FunctionPointerTypeSymbol)comp.GlobalNamespace.GetMember<FieldSymbol>("C.D").Type;
        var p = ptr.Signature.Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false, attributes: false, customModifiers: VerifyModifiers.DoNotVerify);
        Assert.Equal(RefKind.In, p.RefKind);
        Assert.Empty(p.GetAttributes());
        AssertEx.SetEqual(new[]
        {
            (false, InAttributeQualifiedName),
            (true, "MyAttribute")
        }, p.RefCustomModifiers.Select(m => (m.IsOptional, m.Modifier.ToTestDisplayString())));
    }

    /// <summary>
    /// Demonstrates that modopt encoding of 'ref readonly' parameters in function pointers
    /// won't break older compilers (they will see the parameter as 'ref').
    /// </summary>
    [Fact]
    public void FunctionPointer_Modopt_CustomAttribute_Ref()
    {
        // public class C
        // {
        //     public unsafe delegate*<ref int modopt(MyAttribute), void> D;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .field public method void *(int32& modopt(MyAttribute)) D
            }

            .class public auto ansi sealed beforefieldinit MyAttribute extends System.Object
            {
            }
            """;

        var source = """
            class D
            {
                unsafe void M(C c)
                {
                    int x = 6;
                    c.D(x);
                    c.D(ref x);
                    c.D(in x);

                    delegate*<int, void> v = c.D;
                    delegate*<ref int, void> r = c.D;
                    delegate*<in int, void> i = c.D;
                    delegate*<ref readonly int, void> rr = c.D;
                    delegate*<out int, void> o = c.D;
                }
            }
            """;

        CreateCompilationWithIL(new[] { source, RequiresLocationAttributeDefinition }, ilSource, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // 0.cs(6,13): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         c.D(x);
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(6, 13),
            // 0.cs(8,16): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         c.D(in x);
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(8, 16),
            // 0.cs(10,34): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<int, void> v = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<int, void>").WithLocation(10, 34),
            // 0.cs(12,37): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<in int, void> i = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<in int, void>").WithLocation(12, 37),
            // 0.cs(13,23): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //         delegate*<ref readonly int, void> rr = c.D;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(13, 23),
            // 0.cs(13,48): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<ref readonly int, void> rr = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<ref readonly int, void>").WithLocation(13, 48),
            // 0.cs(14,38): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<out int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<out int, void> o = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<out int, void>").WithLocation(14, 38));

        var comp = CreateCompilationWithIL(new[] { source, RequiresLocationAttributeDefinition }, ilSource, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // 0.cs(6,13): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         c.D(x);
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(6, 13),
            // 0.cs(8,16): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         c.D(in x);
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(8, 16),
            // 0.cs(10,34): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<int, void> v = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<int, void>").WithLocation(10, 34),
            // 0.cs(12,37): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<in int, void> i = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<in int, void>").WithLocation(12, 37),
            // 0.cs(13,48): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<ref readonly int, void> rr = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<ref readonly int, void>").WithLocation(13, 48),
            // 0.cs(14,38): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<out int, void>'. An explicit conversion exists (are you missing a cast?)
            //         delegate*<out int, void> o = c.D;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c.D").WithArguments("delegate*<ref int, void>", "delegate*<out int, void>").WithLocation(14, 38));

        var ptr = (FunctionPointerTypeSymbol)comp.GlobalNamespace.GetMember<FieldSymbol>("C.D").Type;
        var p = ptr.Signature.Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false, metadataIn: false, attributes: false, customModifiers: VerifyModifiers.DoNotVerify);
        Assert.Equal(RefKind.Ref, p.RefKind);
        Assert.Empty(p.GetAttributes());
        var m = p.RefCustomModifiers.Single();
        Assert.True(m.IsOptional);
        Assert.Equal("MyAttribute", m.Modifier.ToTestDisplayString());
    }

    [Fact]
    public void FunctionPointer_ModreqIn_ModoptRequiresLocation()
    {
        // public class C
        // {
        //     public unsafe delegate*<in int modopt(RequiresLocation), void> D;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .field public method void *(int32& modreq(System.Runtime.InteropServices.InAttribute) modopt(System.Runtime.CompilerServices.RequiresLocationAttribute)) D
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.InAttribute extends System.Object
            {
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
            }
            """;

        var source = """
            class D
            {
                void M(C c)
                {
                    int x = 6;
                    c.D(ref x);
                }
            }
            """;

        var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (6,9): error CS0570: 'delegate*<in int, void>' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "c.D(ref x)").WithArguments("delegate*<in int, void>").WithLocation(6, 9),
            // (6,11): error CS0570: 'C.D' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "D").WithArguments("C.D").WithLocation(6, 11));

        var ptr = (FunctionPointerTypeSymbol)comp.GlobalNamespace.GetMember<FieldSymbol>("C.D").Type;
        var p = ptr.Signature.Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false, attributes: false, customModifiers: VerifyModifiers.DoNotVerify, useSiteError: true);
        Assert.Equal(RefKind.In, p.RefKind);
        Assert.Empty(p.GetAttributes());
        AssertEx.SetEqual(new[]
        {
            (false, InAttributeQualifiedName),
            (true, RequiresLocationAttributeQualifiedName)
        }, p.RefCustomModifiers.Select(m => (m.IsOptional, m.Modifier.ToTestDisplayString())));
    }

    [Fact]
    public void FunctionPointer_ModoptRequiresLocation_ModreqCustom()
    {
        // public class C
        // {
        //     public unsafe delegate*<ref readonly int modreq(MyAttribute), void> D;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .field public method void *(int32& modopt(System.Runtime.CompilerServices.RequiresLocationAttribute) modreq(MyAttribute)) D
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
            }

            .class public auto ansi sealed beforefieldinit MyAttribute extends System.Object
            {
            }
            """;

        var source = """
            class X
            {
                void M(C c)
                {
                    int x = 111;
                    c.D(ref x);
                }
            }
            """;

        var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,9): error CS0570: 'delegate*<ref readonly int, void>' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "c.D(ref x)").WithArguments("delegate*<ref readonly int, void>").WithLocation(6, 9),
            // (6,11): error CS0570: 'C.D' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "D").WithArguments("C.D").WithLocation(6, 11));

        var ptr = (FunctionPointerTypeSymbol)comp.GlobalNamespace.GetMember<FieldSymbol>("C.D").Type;
        var p = ptr.Signature.Parameters.Single();
        VerifyRefReadonlyParameter(p, attributes: false, customModifiers: VerifyModifiers.DoNotVerify, useSiteError: true);
        Assert.Empty(p.GetAttributes());
        AssertEx.SetEqual(new[]
        {
            (false, "MyAttribute"),
            (true, RequiresLocationAttributeQualifiedName)
        }, p.RefCustomModifiers.Select(m => (m.IsOptional, m.Modifier.ToTestDisplayString())));
    }

    [Fact]
    public void FunctionPointer_ModreqRequiresLocation()
    {
        // public class C
        // {
        //     public unsafe delegate*<ref int modreq(RequiresLocation), void> D;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .field public method void *(int32& modreq(System.Runtime.CompilerServices.RequiresLocationAttribute)) D
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
            }
            """;

        var source = """
            class X
            {
                void M(C c)
                {
                    int x = 111;
                    c.D(ref x);
                }
            }
            """;

        var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (6,9): error CS0570: 'delegate*<ref int, void>' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "c.D(ref x)").WithArguments("delegate*<ref int, void>").WithLocation(6, 9),
            // (6,11): error CS0570: 'C.D' is not supported by the language
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BindToBogus, "D").WithArguments("C.D").WithLocation(6, 11));

        var ptr = (FunctionPointerTypeSymbol)comp.GlobalNamespace.GetMember<FieldSymbol>("C.D").Type;
        var p = ptr.Signature.Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false, metadataIn: false, attributes: false, customModifiers: VerifyModifiers.DoNotVerify, useSiteError: true);
        Assert.Equal(RefKind.Ref, p.RefKind);
        Assert.Empty(p.GetAttributes());
        var mod = Assert.Single(p.RefCustomModifiers);
        Assert.False(mod.IsOptional);
        Assert.Equal(RequiresLocationAttributeQualifiedName, mod.Modifier.ToTestDisplayString());
    }

    [Fact]
    public void FunctionPointer_MissingInAttribute()
    {
        var source = """
            class C
            {
                public unsafe void M(delegate*<ref readonly int, void> p) { }
            }
            """;
        var comp = CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
        CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify).VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            Assert.NotNull(m.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName));

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            var ptr = (FunctionPointerTypeSymbol)p.Type;
            var p2 = ptr.Signature.Parameters.Single();
            VerifyRefReadonlyParameter(p2, customModifiers: VerifyModifiers.RequiresLocation);
        }
    }

    [Fact]
    public void FunctionPointer_MissingRequiresLocationAttribute()
    {
        var source = """
            class C
            {
                public unsafe void M(delegate*<ref readonly int, void> p) { }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (3,36): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //     public unsafe void M(delegate*<ref readonly int, void> p) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(3, 36));
    }

    [Fact]
    public void FunctionPointer_NoAttribute_PlusNormalMethod()
    {
        var source = """
            class C
            {
                public unsafe void M1(delegate*<ref readonly int, void> p) { }
                public void M2(ref readonly int p) { }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (3,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //     public unsafe void M1(delegate*<ref readonly int, void> p) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(3, 37));
    }

    [Fact]
    public void FunctionPointer_InternalAttribute()
    {
        // Attribute is synthesized for Assembly1, but it's not visible to Assembly2.
        var source1 = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly2")]
            internal class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var comp1 = CreateCompilation(source1, assemblyName: "Assembly1").VerifyDiagnostics();
        var comp1Ref = comp1.EmitToImageReference();
        var source2 = """
            class D
            {
                public unsafe object M(delegate*<ref readonly int, void> p)
                {
                    var c = new C();
                    int x = 5;
                    c.M(in x);
                    var attr = new System.Runtime.CompilerServices.RequiresLocationAttribute();
                    return attr;
                }
            }
            """;
        var comp2 = CreateCompilation(source2, new[] { comp1Ref }, assemblyName: "Assembly2", options: TestOptions.UnsafeDebugDll);
        comp2.VerifyDiagnostics(
            // (3,38): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //     public unsafe object M(delegate*<ref readonly int, void> p)
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(3, 38),
            // (8,56): error CS0234: The type or namespace name 'RequiresLocationAttribute' does not exist in the namespace 'System.Runtime.CompilerServices' (are you missing an assembly reference?)
            //         var attr = new System.Runtime.CompilerServices.RequiresLocationAttribute();
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "RequiresLocationAttribute").WithArguments("RequiresLocationAttribute", "System.Runtime.CompilerServices").WithLocation(8, 56));

        // Assembly1 defines the attribute in source and has IVT to Assembly2, so the attribute is visible to Assembly2.
        var comp1b = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, assemblyName: "Assembly1").VerifyDiagnostics();
        var comp1bRef = comp1b.EmitToImageReference();
        var comp2b = CreateCompilation(source2, new[] { comp1bRef }, assemblyName: "Assembly2", options: TestOptions.UnsafeDebugDll);
        comp2b.VerifyDiagnostics();

        // Assembly1 defines the attribute in source but doesn't have IVT to Assembly3, so the attribute isn't visible to Assembly3.
        var source3 = """
            class D
            {
                public unsafe object M(delegate*<ref readonly int, void> p)
                {
                    var attr = new System.Runtime.CompilerServices.RequiresLocationAttribute();
                    return attr;
                }
            }
            """;
        var comp3 = CreateCompilation(source3, new[] { comp1bRef }, assemblyName: "Assembly3", options: TestOptions.UnsafeDebugDll);
        comp3.VerifyDiagnostics(
            // (3,38): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //     public unsafe object M(delegate*<ref readonly int, void> p)
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(3, 38),
            // (5,56): error CS0122: 'RequiresLocationAttribute' is inaccessible due to its protection level
            //         var attr = new System.Runtime.CompilerServices.RequiresLocationAttribute();
            Diagnostic(ErrorCode.ERR_BadAccess, "RequiresLocationAttribute").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 56));
    }

    [Fact]
    public void FunctionPointer_DefineManually_LaterDefinedInRuntime()
    {
        // Library defines an API with function pointer, manually declaring the attribute.
        var source1 = """
            public class C
            {
                public unsafe void M(delegate*<ref readonly int, void> f)
                {
                    int x = 123;
                    f(in x);
                }
            }
            """;
        var comp1v1 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, assemblyName: "Assembly1", options: TestOptions.UnsafeReleaseDll);
        comp1v1.VerifyDiagnostics();
        verifyModoptFromAssembly(comp1v1, "Assembly1");
        var comp1v1Ref = comp1v1.EmitToImageReference();

        // Consumer can use the API.
        var source2 = """
            public class D
            {
                public unsafe void M()
                {
                    new C().M(&F);
                }
                static void F(ref readonly int x) => System.Console.Write("F" + x);
            }
            """;
        var comp2 = CreateCompilation(source2, new[] { comp1v1Ref }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        verifyModoptFromAssembly(comp2, "Assembly1");
        var comp2Ref = comp2.EmitToImageReference();

        var source3 = """
            try
            {
                new D().M();
            }
            catch (System.MissingMethodException e)
            {
                System.Console.Write(e.Message.Contains("Void C.M(Void (Int32 ByRef))"));
            }
            """;
        var verifier3v1 = CompileAndVerify(source3, new[] { comp1v1Ref, comp2Ref }, expectedOutput: "F123").VerifyDiagnostics();
        verifyModoptFromAssembly(verifier3v1.Compilation, "Assembly1");

        // .NET runtime declares the attribute.
        var source4 = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
                public sealed class RequiresLocationAttribute : Attribute
                {
                }
            }
            """;
        var comp4 = CreateCompilation(source4, assemblyName: "Assembly4").VerifyDiagnostics();
        var comp4Ref = comp4.EmitToImageReference();

        // Library is recompiled against the newest runtime.
        var comp1v2 = CreateCompilation(source1, new[] { comp4Ref }, assemblyName: "Assembly1", options: TestOptions.UnsafeReleaseDll);
        comp1v2.VerifyDiagnostics();
        verifyModoptFromAssembly(comp1v2, "Assembly4");
        var comp1v2Ref = comp1v2.EmitToImageReference();

        // That breaks the consumer.
        var verifier3v2 = CompileAndVerify(source3, new[] { comp1v2Ref, comp2Ref, comp4Ref }, expectedOutput: "True").VerifyDiagnostics();
        verifyModoptFromAssembly(verifier3v2.Compilation, "Assembly4");

        // Unless the library adds type forwarding.
        var source5 = """
            using System.Runtime.CompilerServices;
            [assembly: TypeForwardedToAttribute(typeof(RequiresLocationAttribute))]
            """;
        var comp1v3 = CreateCompilation(new[] { source1, source5 }, new[] { comp4Ref }, assemblyName: "Assembly1", options: TestOptions.UnsafeReleaseDll);
        comp1v3.VerifyDiagnostics();
        verifyModoptFromAssembly(comp1v3, "Assembly4");
        var comp1v3Ref = comp1v3.EmitToImageReference();
        CompileAndVerify(source3, new[] { comp1v3Ref, comp2Ref, comp4Ref }, expectedOutput: "F123").VerifyDiagnostics();

        // Or keeps the manual attribute definition.
        var comp1v4 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, new[] { comp4Ref }, assemblyName: "Assembly1", options: TestOptions.UnsafeReleaseDll);
        comp1v4.VerifyDiagnostics();
        verifyModoptFromAssembly(comp1v4, "Assembly1");
        var comp1v4Ref = comp1v4.EmitToImageReference();
        CompileAndVerify(source3, new[] { comp1v4Ref, comp2Ref, comp4Ref }, expectedOutput: "F123").VerifyDiagnostics();

        static void verifyModoptFromAssembly(Compilation comp, string assemblyName)
        {
            var f = ((CSharpCompilation)comp).GetMember<MethodSymbol>("C.M").Parameters.Single();
            var p = ((FunctionPointerTypeSymbol)f.Type).Signature.Parameters.Single();
            var mod = p.RefCustomModifiers.Single();
            Assert.True(mod.IsOptional);
            Assert.Equal(RequiresLocationAttributeQualifiedName, mod.Modifier.ToTestDisplayString());
            Assert.Equal(assemblyName, mod.Modifier.ContainingAssembly.Name);
        }
    }

    [Fact]
    public void FunctionPointer_DefineManually_AndInReference()
    {
        var source1 = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
                public sealed class RequiresLocationAttribute : Attribute
                {
                }
            }
            """;
        var comp1 = CreateCompilation(source1, assemblyName: "Assembly1").VerifyDiagnostics();
        var comp1Ref = comp1.EmitToImageReference();

        // Attribute declared both in the same assembly and in a reference.
        var source2 = """
            public class C
            {
                public unsafe void M(delegate*<ref readonly int, void> f) { }
            }
            """;
        var comp2 = CreateCompilation(new[] { source2, RequiresLocationAttributeDefinition }, new[] { comp1Ref },
            assemblyName: "Assembly2", options: TestOptions.UnsafeReleaseDll);
        CompileAndVerify(comp2, sourceSymbolValidator: verify2, symbolValidator: verify2).VerifyDiagnostics();

        static void verify2(ModuleSymbol m)
        {
            Assert.NotNull(m.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName));

            var modifier = verify(m);
            Assert.Equal("Assembly2", modifier.ContainingAssembly.Name);
        }

        // Attribute declared only in a reference.
        var comp3 = CreateCompilation(source2, new[] { comp1Ref }, assemblyName: "Assembly3", options: TestOptions.UnsafeReleaseDll);
        CompileAndVerify(comp3, sourceSymbolValidator: verify3, symbolValidator: verify3).VerifyDiagnostics();

        static void verify3(ModuleSymbol m)
        {
            Assert.Null(m.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName));

            var modifier = verify(m);
            Assert.Equal("Assembly1", modifier.ContainingAssembly.Name);
        }

        static INamedTypeSymbol verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            var ptr = (FunctionPointerTypeSymbol)p.Type;
            var p2 = ptr.Signature.Parameters.Single();
            VerifyRefReadonlyParameter(p2, customModifiers: VerifyModifiers.RequiresLocation);
            return p2.RefCustomModifiers.Single().Modifier;
        }
    }

    [Fact]
    public void FunctionPointer_Local()
    {
        var source = """
            class C
            {
                unsafe void M()
                {
                    delegate*<ref readonly int, void> p = null;
                }
            }
            """;
        var comp = CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var local = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(local).GetSymbol<LocalSymbol>()!.Type as FunctionPointerTypeSymbol;
        VerifyRefReadonlyParameter(symbol!.Signature.Parameters.Single(), customModifiers: VerifyModifiers.RequiresLocation);
    }

    [Fact]
    public void FunctionPointer_Local_MissingRequiresLocationAttribute()
    {
        var source = """
            class C
            {
                unsafe void M()
                {
                    delegate*<ref readonly int, void> p = null;
                }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (5,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //         delegate*<ref readonly int, void> p = null;
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 19));
    }

    [Fact]
    public void FunctionPointer_CrossAssembly()
    {
        var source1 = """
            public class C
            {
                public unsafe delegate*<ref readonly int, void> D;
            }
            """;
        var comp1 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            class D
            {
                unsafe void M(C c)
                {
                    int x = 6;
                    c.D(x);
                    c.D(ref x);
                    c.D(in x);
                }
            }
            """;
        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //         c.D(x);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x").WithArguments("ref readonly parameters", "12.0").WithLocation(6, 13),
            // (8,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //         c.D(in x);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x").WithArguments("ref readonly parameters", "12.0").WithLocation(8, 16));

        var expectedDiagnostics = new[]
        {
            // (6,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.D(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(6, 13)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source2, new[] { comp1Ref }, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void AttributeIL()
    {
        var source = """
            class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL(RequiresLocationAttributeName, """
            .class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute
                extends [netstandard]System.Attribute
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2050
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Attribute::.ctor()
                	IL_0006: ret
                } // end of method RequiresLocationAttribute::.ctor
            } // end of class System.Runtime.CompilerServices.RequiresLocationAttribute
            """);
    }

    [Fact]
    public void Modifier()
    {
        var source = """
            class C
            {
                void M(ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16));

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        var comp = CreateCompilation(source).VerifyDiagnostics();

        var p = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
        VerifyRefReadonlyParameter(p);
    }

    [Fact]
    public void Modifier_Invalid_01()
    {
        var source = """
            class C
            {
                void M(ref params readonly int[] p) => throw null;
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (3,16): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref params readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 16),
            // (3,23): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(ref params readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 23));

        var p = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false, metadataIn: false);
        Assert.Equal(RefKind.Ref, p.RefKind);
    }

    [Fact]
    public void Modifier_Invalid_02()
    {
        var source = """
            class C
            {
                void M(in readonly int p) => throw null;
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (3,15): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(in readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 15));

        var p = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
        VerifyRefReadonlyParameter(p, refKind: false);
        Assert.Equal(RefKind.In, p.RefKind);
    }

    [Fact]
    public void DuplicateModifier_01()
    {
        var source = """
            class C
            {
                void M(ref readonly readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_02()
    {
        var source = """
            class C
            {
                void M(readonly readonly int p) { }
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_03()
    {
        var source = """
            class C
            {
                void M(readonly ref readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,25): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_04()
    {
        var source = """
            class C
            {
                void M(readonly readonly ref int p) { }
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_05()
    {
        var source = """
            class C
            {
                void M(ref readonly ref readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
            // (3,25): error CS1107: A parameter can only have one 'ref' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(3, 25),
            // (3,29): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 29));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS1107: A parameter can only have one 'ref' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(3, 25),
            // (3,29): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 29)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithoutRef()
    {
        var source = """
            class C
            {
                void M(readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithParams()
    {
        var source = """
            class C
            {
                void M(readonly params int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithParams_01()
    {
        var source = """
            class C
            {
                void M(params ref readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,19): error CS1611: The params parameter cannot be declared as ref
            //     void M(params ref readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "ref").WithArguments("ref").WithLocation(3, 19)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithParams_02()
    {
        var source = """
            class C
            {
                void M(ref readonly params int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithIn()
    {
        var source = """
            class C
            {
                void M(in readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,15): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(in readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 15)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithIn()
    {
        var source = """
            class C
            {
                void M(ref readonly in int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithOut()
    {
        var source = """
            class C
            {
                void M(out readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,16): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     void M(out readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 16)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithOut()
    {
        var source = """
            class C
            {
                void M(ref readonly out int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithThis()
    {
        var source = """
            static class C
            {
                public static void M(this readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,31): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 31)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithThis_01()
    {
        var source = """
            static class C
            {
                public static void M(this ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,35): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(this ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 35));

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithThis_02()
    {
        var source = """
            static class C
            {
                public static void M(ref this readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,35): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 35)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithThis_03()
    {
        var source = """
            static class C
            {
                public static void M(ref readonly this int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,30): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly this int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithScoped_01()
    {
        var source = """
            static class C
            {
                public static void M(scoped ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,37): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(scoped ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 37));

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithScoped_02()
    {
        var source = """
            static class C
            {
                public static void M(ref scoped readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,30): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 30),
            // (3,37): error CS1001: Identifier expected
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(3, 37),
            // (3,37): error CS1003: Syntax error, ',' expected
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(3, 37),
            // (3,37): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 37)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithScoped_03()
    {
        var source = """
            static class C
            {
                public static void M(readonly scoped ref int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,26): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 26),
            // (3,35): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 35),
            // (3,42): error CS1001: Identifier expected
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(3, 42),
            // (3,42): error CS1003: Syntax error, ',' expected
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(3, 42)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithScoped()
    {
        var source = """
            static class C
            {
                public static void M(scoped readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,26): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 26),
            // (3,33): error CS1001: Identifier expected
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(3, 33),
            // (3,33): error CS1003: Syntax error, ',' expected
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(3, 33),
            // (3,33): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 33)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonly_ScopedParameterName()
    {
        var source = """
            static class C
            {
                public static void M(ref readonly int scoped) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,30): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly int scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonly_ScopedTypeName()
    {
        var source = """
            struct scoped { }
            static class C
            {
                public static void M(ref readonly scoped p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
            // (4,30): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8773: Feature 'ref readonly parameters' is not available in C# 9.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 30));
    }

    [Fact]
    public void RefReadonly_ScopedBothNames()
    {
        var source = """
            struct scoped { }
            static class C
            {
                public static void M(ref readonly scoped scoped) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
            // (4,30): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8773: Feature 'ref readonly parameters' is not available in C# 9.0. Please use language version 12.0 or greater.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 30));
    }

    [Fact]
    public void RefReadonlyParameter_Assignable_PlainArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M(x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics(
            // (7,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 11));
        verifier.VerifyIL("C.Main", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.5
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void RefReadonlyParameter_Assignable_RefOrInArgument([CombinatorialValues("ref", "in")] string modifier)
    {
        var source = $$"""
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M({{modifier}} x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.5
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void RefReadonlyParameter_ReadonlyRef_PlainArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static readonly int x = 5;
                static void Main()
                {
                    M(x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5", verify: Verification.Fails);
        verifier.VerifyDiagnostics(
            // (7,11): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("1").WithLocation(7, 11));
        verifier.VerifyIL("C.Main", """
            {
              // Code size       11 (0xb)
              .maxstack  1
              IL_0000:  ldsflda    "int C.x"
              IL_0005:  call       "void C.M(ref readonly int)"
              IL_000a:  ret
            }
            """);
    }

    [Fact]
    public void RefReadonlyParameter_ReadonlyRef_RefArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static readonly int x = 5;
                static void Main()
                {
                    M(ref x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,15): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
            //         M(ref x);
            Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "x").WithLocation(7, 15));
    }

    [Fact]
    public void RefReadonlyParameter_ReadonlyRef_InArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static readonly int x = 5;
                static void Main()
                {
                    M(in x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5", verify: Verification.Fails);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       11 (0xb)
              .maxstack  1
              IL_0000:  ldsflda    "int C.x"
              IL_0005:  call       "void C.M(ref readonly int)"
              IL_000a:  ret
            }
            """);
    }

    [Fact]
    public void RefReadonlyParameter_RValue_PlainArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    M(5);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics(
            // (6,11): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            //         M(5);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("1").WithLocation(6, 11));
        verifier.VerifyIL("C.Main", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldc.i4.5
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void RefReadonlyParameter_RValue_RefOrInArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    M(ref 6);
                    M(in 7);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,15): error CS1510: A ref or out value must be an assignable variable
            //         M(ref 6);
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "6").WithLocation(6, 15),
            // (7,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
            //         M(in 7);
            Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "7").WithLocation(7, 14));
    }

    [Fact]
    public void RefReadonlyParameter_OutArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.WriteLine(p);
                static readonly int x = 5;
                static void Main()
                {
                    M(out x);
                    int y;
                    M(out y);
                    M(out 6);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,15): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
            //         M(out x);
            Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "x").WithLocation(7, 15),
            // (9,15): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         M(out y);
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "out").WithLocation(9, 15),
            // (10,15): error CS1510: A ref or out value must be an assignable variable
            //         M(out 6);
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "6").WithLocation(10, 15));
    }

    [Fact]
    public void PassingParameters_In_RefReadonly_PlainArgument()
    {
        var source = """
            class C
            {
                static void M1(in int p)
                {
                    M2(p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main() => M1(5);
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics(
            // (5,12): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M2(p);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "p").WithArguments("1").WithLocation(5, 12));
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(ref readonly int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_In_RefReadonly_RefArgument()
    {
        var source = """
            class C
            {
                static void M1(in int p)
                {
                    M2(ref p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main() => M1(5);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,16): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         M2(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(5, 16));
    }

    [Fact]
    public void PassingParameters_In_RefReadonly_InArgument()
    {
        var source = """
            class C
            {
                static void M1(in int p)
                {
                    M2(in p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main() => M1(5);
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(ref readonly int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_RefReadonly_In_PlainArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(p);
                }
                static void M2(in int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(in int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_RefReadonly_In_RefArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(ref p);
                }
                static void M2(in int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,16): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         M2(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(5, 16));
    }

    [Fact]
    public void PassingParameters_RefReadonly_In_InArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(in p);
                }
                static void M2(in int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(in int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_RefReadonly_RefReadonly_PlainArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics(
            // (5,12): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M2(p);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "p").WithArguments("1").WithLocation(5, 12));
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(ref readonly int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_RefReadonly_RefReadonly_RefArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(ref p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,16): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         M2(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(5, 16));
    }

    [Fact]
    public void PassingParameters_RefReadonly_RefReadonly_InArgument()
    {
        var source = """
            class C
            {
                static void M1(ref readonly int p)
                {
                    M2(in p);
                }
                static void M2(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    M1(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "void C.M2(ref readonly int)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void PassingParameters_RefReadonly_RefOrOut()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p)
                {
                    Ref(p);
                    Ref(ref p);
                    Ref(in p);
                    Ref(out p);

                    Out(p);
                    Out(ref p);
                    Out(in p);
                    Out(out p);
                }
                static void Ref(ref int p) => throw null;
                static void Out(out int p) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,13): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         Ref(p);
            Diagnostic(ErrorCode.ERR_BadArgRef, "p").WithArguments("1", "ref").WithLocation(5, 13),
            // (6,17): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         Ref(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(6, 17),
            // (7,16): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         Ref(in p);
            Diagnostic(ErrorCode.ERR_BadArgRef, "p").WithArguments("1", "ref").WithLocation(7, 16),
            // (8,17): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         Ref(out p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(8, 17),
            // (10,13): error CS1620: Argument 1 must be passed with the 'out' keyword
            //         Out(p);
            Diagnostic(ErrorCode.ERR_BadArgRef, "p").WithArguments("1", "out").WithLocation(10, 13),
            // (11,17): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         Out(ref p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(11, 17),
            // (12,16): error CS1620: Argument 1 must be passed with the 'out' keyword
            //         Out(in p);
            Diagnostic(ErrorCode.ERR_BadArgRef, "p").WithArguments("1", "out").WithLocation(12, 16),
            // (13,17): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         Out(out p);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(13, 17));
    }

    [Fact]
    public void RefReturn_ReadonlyToMutable()
    {
        var source = """
            class C
            {
                ref int M(ref readonly int x)
                {
                    return ref x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS8333: Cannot return variable 'x' by writable reference because it is a readonly variable
            //         return ref x;
            Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "x").WithArguments("variable", "x").WithLocation(5, 20));
    }

    [Fact]
    public void RefReturn_ReadonlyToReadonly()
    {
        var source = """
            class C
            {
                ref readonly int M(ref readonly int x)
                {
                    return ref x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReturn_ReadonlyFromMutable()
    {
        var source = """
            class C
            {
                ref int M1() => throw null;
                void M2(ref readonly int x) { }
                void M3()
                {
                    M2(M1());
                    M2(in M1());
                    M2(ref M1());
                    M2(out M1());
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,12): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M2(M1());
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "M1()").WithArguments("1").WithLocation(7, 12),
            // (10,16): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         M2(out M1());
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "M1()").WithArguments("1", "out").WithLocation(10, 16));
    }

    [Fact]
    public void RefReturn_ReadonlyFromReadonly()
    {
        var source = """
            class C
            {
                ref readonly int M1() => throw null;
                void M2(ref readonly int x) { }
                void M3()
                {
                    M2(M1());
                    M2(in M1());
                    M2(ref M1());
                    M2(out M1());
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,12): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M2(M1());
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "M1()").WithArguments("1").WithLocation(7, 12),
            // (9,16): error CS8329: Cannot use method 'M1' as a ref or out value because it is a readonly variable
            //         M2(ref M1());
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M1()").WithArguments("method", "M1").WithLocation(9, 16),
            // (10,16): error CS8329: Cannot use method 'M1' as a ref or out value because it is a readonly variable
            //         M2(out M1());
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M1()").WithArguments("method", "M1").WithLocation(10, 16));
    }

    [Fact]
    public void RefAssignment()
    {
        var source = """
            ref struct C
            {
                public C(ref readonly int p)
                {
                    r = ref p; // 1
                    rro = ref p;
                    ror = ref p; // 2
                    rorro = ref p;
                }

                ref int r;
                ref readonly int rro;
                readonly ref int ror;
                readonly ref readonly int rorro;

                void M(ref readonly int p)
                {
                    ref int a = ref p; // 3
                    ref readonly int b = ref p;
                    r = ref p; // 4
                    rro = ref p; // 5
                    ror = ref p; // 6
                    rorro = ref p; // 7
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
            // (5,17): error CS8331: Cannot assign to variable 'p' or use it as the right hand side of a ref assignment because it is a readonly variable
            //         r = ref p; // 1
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(5, 17),
            // (7,19): error CS8331: Cannot assign to variable 'p' or use it as the right hand side of a ref assignment because it is a readonly variable
            //         ror = ref p; // 2
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(7, 19),
            // (18,25): error CS8329: Cannot use variable 'p' as a ref or out value because it is a readonly variable
            //         ref int a = ref p; // 3
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(18, 25),
            // (20,17): error CS8331: Cannot assign to variable 'p' or use it as the right hand side of a ref assignment because it is a readonly variable
            //         r = ref p; // 4
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "p").WithArguments("variable", "p").WithLocation(20, 17),
            // (21,9): error CS9079: Cannot ref-assign 'p' to 'rro' because 'p' can only escape the current method through a return statement.
            //         rro = ref p; // 5
            Diagnostic(ErrorCode.ERR_RefAssignReturnOnly, "rro = ref p").WithArguments("rro", "p").WithLocation(21, 9),
            // (22,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            //         ror = ref p; // 6
            Diagnostic(ErrorCode.ERR_AssgReadonly, "ror").WithLocation(22, 9),
            // (23,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            //         rorro = ref p; // 7
            Diagnostic(ErrorCode.ERR_AssgReadonly, "rorro").WithLocation(23, 9));
    }

    [Theory, CombinatorialData]
    public void RefAssignment_BetweenParameters(
        [CombinatorialValues("in", "ref readonly", "ref")] string modifier1,
        [CombinatorialValues("in", "ref readonly", "ref")] string modifier2)
    {
        var source = $$"""
            class C
            {
                static void M1({{modifier1}} int x, {{modifier2}} int y)
                {
                    x = ref y;
                }
                static void M2({{modifier1}} int x, {{modifier2}} int y)
                {
                    System.Console.WriteLine(x + " " + y);
                    x = ref y;
                    System.Console.WriteLine(x + " " + y);
                }
                static void Main()
                {
                    int x = 5;
                    int y = 6;
                    M2({{getArgumentModifier(modifier1)}} x, {{getArgumentModifier(modifier2)}} y);
                }
            }
            """;

        if (modifier1 == "ref" && modifier2 != "ref")
        {
            CreateCompilation(source).VerifyDiagnostics(
                // (5,17): error CS8331: Cannot assign to variable 'y' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(5, 17),
                // (10,17): error CS8331: Cannot assign to variable 'y' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(10, 17));
        }
        else
        {
            var verifier = CompileAndVerify(source, expectedOutput: """
                5 6
                6 6
                """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1", """
                {
                  // Code size        4 (0x4)
                  .maxstack  1
                  IL_0000:  ldarg.1
                  IL_0001:  starg.s    V_0
                  IL_0003:  ret
                }
                """);
        }

        static string getArgumentModifier(string parameterModifier)
        {
            return parameterModifier switch
            {
                "ref" => "ref",
                _ => "in",
            };
        }
    }

    [Theory, CombinatorialData]
    public void RefAssignment_BetweenParameters_Struct(
        [CombinatorialValues("in", "ref readonly", "ref")] string modifier1,
        [CombinatorialValues("in", "ref readonly", "ref")] string modifier2)
    {
        var source = $$"""
            struct S(int v)
            {
                public int V = v;
            }
            class C
            {
                static int M1({{modifier1}} S x, {{modifier2}} S y)
                {
                    return (x = ref y).V;
                }
                static void M2({{modifier1}} S x, {{modifier2}} S y)
                {
                    System.Console.WriteLine(x.V + " " + y.V);
                    System.Console.WriteLine((x = ref y).V);
                    System.Console.WriteLine(x.V + " " + y.V);
                }
                static void Main()
                {
                    S x = new S(5);
                    S y = new S(6);
                    M2({{getArgumentModifier(modifier1)}} x, {{getArgumentModifier(modifier2)}} y);
                }
            }
            """;

        if (modifier1 == "ref" && modifier2 != "ref")
        {
            CreateCompilation(source).VerifyDiagnostics(
                // (9,25): error CS8331: Cannot assign to variable 'y' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         return (x = ref y).V;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(9, 25),
                // (14,43): error CS8331: Cannot assign to variable 'y' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         System.Console.WriteLine((x = ref y).V);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(14, 43));
        }
        else
        {
            var verifier = CompileAndVerify(source, expectedOutput: """
                5 6
                6
                6 6
                """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1", """
                {
                  // Code size       10 (0xa)
                  .maxstack  2
                  IL_0000:  ldarg.1
                  IL_0001:  dup
                  IL_0002:  starg.s    V_0
                  IL_0004:  ldfld      "int S.V"
                  IL_0009:  ret
                }
                """);
        }

        static string getArgumentModifier(string parameterModifier)
        {
            return parameterModifier switch
            {
                "ref" => "ref",
                _ => "in",
            };
        }
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
    public void RefReadonlyParameter_Arglist()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p, __arglist) => System.Console.WriteLine(p);
                static void Main()
                {
                    int x = 111;
                    M(x, __arglist(x));
                    M(ref x, __arglist(x));
                    M(in x, __arglist(x));
                }
            }
            """;
        var verifier = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: """
            111
            111
            111
            """);
        verifier.VerifyDiagnostics(
            // (7,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(x, __arglist(x));
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 11));
    }

    [Fact]
    public void RefReadonlyParameter_Arglist_OutArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p, __arglist) => System.Console.WriteLine(p);
                static void Main()
                {
                    int x = 111;
                    M(out x, __arglist(x));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,15): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         M(out x, __arglist(x));
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out").WithLocation(7, 15));
    }

    [Fact]
    public void RefReadonlyParameter_CrossAssembly()
    {
        var source1 = """
            public class C
            {
                public void M(ref readonly int p) => System.Console.Write(p);
                void M2()
                {
                    int x = 5;
                    M(x);
                    M(ref x);
                    M(in x);
                }
            }
            """;
        var comp1 = CreateCompilation(source1).VerifyDiagnostics(
            // (7,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 11));
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            class D
            {
                void M(C c)
                {
                    int x = 6;
                    c.M(x);
                    c.M(ref x);
                    c.M(in x);
                }
            }
            """;
        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (6,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //         c.M(x);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x").WithArguments("ref readonly parameters", "12.0").WithLocation(6, 13),
            // (8,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //         c.M(in x);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x").WithArguments("ref readonly parameters", "12.0").WithLocation(8, 16));

        var expectedDiagnostics = new[]
        {
            // (6,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(6, 13)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source2, new[] { comp1Ref }).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyParameter_Ctor()
    {
        var source = """
            class C
            {
                private C(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    int x = 5;
                    new C(x);
                    new C(ref x);
                    new C(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "555").VerifyDiagnostics(
            // (7,15): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         new C(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 15));
    }

    [Fact]
    public void RefReadonlyParameter_Ctor_OutArgument()
    {
        var source = """
            class C
            {
                private C(ref readonly int p) => throw null;
                static void Main()
                {
                    int x = 5;
                    new C(out x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,19): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         new C(out x);
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out").WithLocation(7, 19));
    }

    [Fact]
    public void RefReadonlyParameter_Indexer()
    {
        var source = """
            class C
            {
                int this[ref readonly int p]
                {
                    get
                    {
                        System.Console.Write(p);
                        return p;
                    }
                }

                static void Main()
                {
                    var c = new C();
                    int x = 5;
                    _ = c[x];
                    _ = c[ref x];
                    _ = c[in x];
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "555").VerifyDiagnostics(
            // (16,15): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         _ = c[x];
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(16, 15));
    }

    [Fact]
    public void RefReadonlyParameter_Indexer_OutArgument()
    {
        var source = """
            class C
            {
                int this[ref readonly int p] => throw null;
                static void Main()
                {
                    var c = new C();
                    int x = 5;
                    _ = c[out x];
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,19): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         _ = c[out x];
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out").WithLocation(8, 19));
    }

    [Fact]
    public void RefReadonlyParameter_FunctionPointer()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.Write(p);
                static unsafe void Main()
                {
                    delegate*<ref readonly int, void> f = &M;
                    int x = 5;
                    f(x);
                    f(ref x);
                    f(in x);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { source, RequiresLocationAttributeDefinition },
            expectedOutput: "555", options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
        verifier.VerifyDiagnostics(
            // (8,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         f(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(8, 11));
    }

    [Fact]
    public void RefReadonlyParameter_FunctionPointer_OutArgument()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => throw null;
                static unsafe void Main()
                {
                    delegate*<ref readonly int, void> f = &M;
                    int x = 5;
                    f(out x);
                }
            }
            """;
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (8,15): error CS1615: Argument 1 may not be passed with the 'out' keyword
            //         f(out x);
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out").WithLocation(8, 15));
    }

    [Fact]
    public void RefReadonlyParameter_NamedArguments()
    {
        var source = """
            class C
            {
                static void M(in int a, ref readonly int b)
                {
                    System.Console.Write(a);
                    System.Console.Write(b);
                }
                static void Main()
                {
                    int x = 5;
                    int y = 6;
                    M(b: x, a: y); // 1
                    M(b: x, a: in y); // 2
                    M(a: x, y); // 3
                    M(a: x, in y); // 4
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "65655656").VerifyDiagnostics(
            // (12,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(b: x, a: y); // 1
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(12, 14),
            // (13,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(b: x, a: in y); // 2
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(13, 14),
            // (14,17): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            //         M(a: x, y); // 3
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "y").WithArguments("2").WithLocation(14, 17));
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_01()
    {
        var source = """
            class C
            {
                static string M1(string s, ref int i) => "string" + i;
                static string M1(object o, in int i) => "object" + i;
                static string M1(C c, ref readonly int i) => "c" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.WriteLine(M1(null, ref i));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,34): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(string, ref int)' and 'C.M1(C, ref readonly int)'
            //         System.Console.WriteLine(M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(string, ref int)", "C.M1(C, ref readonly int)").WithLocation(9, 34));
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_01_Ctor()
    {
        var source = """
            class C
            {
                private C(string s, ref int i) => System.Console.WriteLine("string" + i);
                private C(object o, in int i) => System.Console.WriteLine("object" + i);
                private C(C c, ref readonly int i) => System.Console.WriteLine("c" + i);
                static void Main()
                {
                    int i = 5;
                    new C(null, ref i);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,13): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(string, ref int)' and 'C.C(C, ref readonly int)'
            //         new C(null, ref i);
            Diagnostic(ErrorCode.ERR_AmbigCall, "C").WithArguments("C.C(string, ref int)", "C.C(C, ref readonly int)").WithLocation(9, 13));
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_02()
    {
        var source = """
            class C
            {
                static string M1(string s, ref int i) => "string" + i;
                static string M1(object o, in int i) => "object" + i;
                static string M1(C c, ref readonly int i) => "c" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.WriteLine(M1(default(string), ref i));
                    System.Console.WriteLine(M1(default(object), ref i));
                    System.Console.WriteLine(M1(default(C), ref i));
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            string5
            object5
            c5
            """).VerifyDiagnostics(
            // (10,58): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.WriteLine(M1(default(object), ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(10, 58));
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_02_Ctor()
    {
        var source = """
            class C
            {
                private C(string s, ref int i) => System.Console.WriteLine("string" + i);
                private C(object o, in int i) => System.Console.WriteLine("object" + i);
                static void Main()
                {
                    int i = 5;
                    new C(default(object), ref i);
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (8,15): error CS1503: Argument 1: cannot convert from 'object' to 'string'
            //         new C(default(object), ref i);
            Diagnostic(ErrorCode.ERR_BadArgType, "default(object)").WithArguments("1", "object", "string").WithLocation(8, 15));

        var expectedDiagnostics = new[]
        {
            // (8,36): warning CS9190: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         new C(default(object), ref i);
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(8, 36)
        };

        CompileAndVerify(source, expectedOutput: "object5", parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source, expectedOutput: "object5").VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_03()
    {
        var source = """
            class C
            {
                static string M1(object o, in int i) => "object" + i;
                static string M1(C c, ref readonly int i) => "c" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.WriteLine(M1(null, ref i));
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "c5").VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyParameter_RefArgument_OverloadResolution_03_Ctor()
    {
        var source = """
            class C
            {
                private C(object o, in int i) => System.Console.WriteLine("object" + i);
                private C(C c, ref readonly int i) => System.Console.WriteLine("c" + i);
                static void Main()
                {
                    int i = 5;
                    new C(null, ref i);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "c5").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_01()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                static string M1(I1 o, in int i) => " 1" + i;
                static string M1(I2 o, ref readonly int i) => " 2" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(M1(null, ref i));
                    System.Console.Write(M1(null, in i));
                    System.Console.Write(M1(null, i));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I2, ref readonly int)'
            //         System.Console.Write(M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I2, ref readonly int)").WithLocation(10, 30),
            // (11,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I2, ref readonly int)'
            //         System.Console.Write(M1(null, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I2, ref readonly int)").WithLocation(11, 30),
            // (12,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I2, ref readonly int)'
            //         System.Console.Write(M1(null, i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I2, ref readonly int)").WithLocation(12, 30));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_02()
    {
        var source1 = """
            interface I1 { }
            interface I2 { }
            class C
            {
                public static string M1(I1 o, ref int i) => " 1" + i;
                public static string M1(I2 o, ref readonly int i) => " 2" + i;
            }
            """;

        var source2 = """
            int i = 5;
            System.Console.Write(C.M1(null, ref i));
            """;

        CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(
            // 1.cs(2,24): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, ref int)' and 'C.M1(I2, ref readonly int)'
            // System.Console.Write(C.M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, ref int)", "C.M1(I2, ref readonly int)").WithLocation(2, 24));

        var source3 = """
            int i = 5;
            System.Console.Write(C.M1(null, in i));
            System.Console.Write(C.M1(null, i));
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "25 25").VerifyDiagnostics(
            // 1.cs(3,33): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            // System.Console.Write(C.M1(null, i));
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "i").WithArguments("2").WithLocation(3, 33));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_03()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                static string M1(I1 o, int i) => " 1" + i;
                static string M1(I2 o, ref readonly int i) => " 2" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(M1(null, ref i));
                    System.Console.Write(M1(null, in i));
                    System.Console.Write(M1(null, i));
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "25 25 15").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_04()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            interface I3 { }
            class C
            {
                static string M1(I1 o, in int i) => " 1" + i;
                static string M1(I2 o, ref int i) => " 2" + i;
                static string M1(I3 o, ref readonly int i) => " 3" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(M1(null, ref i));
                    System.Console.Write(M1(null, in i));
                    System.Console.Write(M1(null, i));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (12,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I2, ref int)'
            //         System.Console.Write(M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I2, ref int)").WithLocation(12, 30),
            // (13,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I3, ref readonly int)'
            //         System.Console.Write(M1(null, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I3, ref readonly int)").WithLocation(13, 30),
            // (14,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I3, ref readonly int)'
            //         System.Console.Write(M1(null, i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I3, ref readonly int)").WithLocation(14, 30));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_05()
    {
        var source1 = """
            interface I1 { }
            interface I2 { }
            interface I3 { }
            interface I4 { }
            class C
            {
                public static string M1(I1 o, int i) => " 1" + i;
                public static string M1(I2 o, in int i) => " 2" + i;
                public static string M1(I3 o, ref int i) => " 3" + i;
                public static string M1(I4 o, ref readonly int i) => " 4" + i;
            }
            """;

        var source2 = """
            int i = 5;
            System.Console.Write(C.M1(null, ref i));
            System.Console.Write(C.M1(null, in i));
            """;

        CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(
            // 1.cs(2,24): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I2, in int)' and 'C.M1(I3, ref int)'
            // System.Console.Write(C.M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I2, in int)", "C.M1(I3, ref int)").WithLocation(2, 24),
            // 1.cs(3,24): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I2, in int)' and 'C.M1(I4, ref readonly int)'
            // System.Console.Write(C.M1(null, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I2, in int)", "C.M1(I4, ref readonly int)").WithLocation(3, 24));

        var source3 = """
            int i = 5;
            System.Console.Write(C.M1(null, i));
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "15").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_06()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                static string M1(I1 o, in int i) => " 1" + i;
                static string M1(I2 o, ref int i) => " 2" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(M1(null, ref i));
                    System.Console.Write(M1(null, in i));
                    System.Console.Write(M1(null, i));
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "25 15 15", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (10,30): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int)' and 'C.M1(I2, ref int)'
            //         System.Console.Write(M1(null, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int)", "C.M1(I2, ref int)").WithLocation(10, 30)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_07([CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        var source1 = """
            interface I1 { }
            interface I2 { }
            class C
            {
                public static string M1(I1 o, in int i, ref int j) => " 1" + i + j;
                public static string M1(I2 o, ref int i, in int j) => " 2" + i + j;
            }
            """;

        var source2 = """
            int i = 5;
            int j = 6;
            System.Console.Write(C.M1(null, ref i, ref j));
            System.Console.Write(C.M1(null, in i, in j));
            System.Console.Write(C.M1(null, in i, j));
            System.Console.Write(C.M1(null, i, in j));
            System.Console.Write(C.M1(null, i, j));
            """;

        CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            languageVersion == LanguageVersion.CSharp11
                // 1.cs(3,37): error CS9194: Argument 2 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version 12.0 or greater.
                // System.Console.Write(C.M1(null, ref i, ref j));
                ? Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "i").WithArguments("2", "11.0", "12.0").WithLocation(3, 37)
                // 1.cs(3,24): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref int)' and 'C.M1(I2, ref int, in int)'
                // System.Console.Write(C.M1(null, ref i, ref j));
                : Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref int)", "C.M1(I2, ref int, in int)").WithLocation(3, 24),
            // 1.cs(4,42): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // System.Console.Write(C.M1(null, in i, in j));
            Diagnostic(ErrorCode.ERR_BadArgRef, "j").WithArguments("3", "ref").WithLocation(4, 42),
            // 1.cs(5,39): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // System.Console.Write(C.M1(null, in i, j));
            Diagnostic(ErrorCode.ERR_BadArgRef, "j").WithArguments("3", "ref").WithLocation(5, 39),
            // 1.cs(6,39): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // System.Console.Write(C.M1(null, i, in j));
            Diagnostic(ErrorCode.ERR_BadArgRef, "j").WithArguments("3", "ref").WithLocation(6, 39),
            // 1.cs(7,36): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // System.Console.Write(C.M1(null, i, j));
            Diagnostic(ErrorCode.ERR_BadArgRef, "j").WithArguments("3", "ref").WithLocation(7, 36));

        var source3 = """
            int i = 5;
            int j = 6;
            System.Console.Write(C.M1(null, ref i, in j));
            System.Console.Write(C.M1(null, ref i, j));
            System.Console.Write(C.M1(null, in i, ref j));
            System.Console.Write(C.M1(null, i, ref j));
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "256 256 156 156", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_08()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                public static string M1(I1 o, in int i, ref readonly int j) => " 1" + i + j;
                public static string M1(I2 o, ref readonly int i, in int j) => " 2" + i + j;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(C.M1(null, ref i, ref i));
                    System.Console.Write(C.M1(null, ref i, in i));
                    System.Console.Write(C.M1(null, ref i, i));
                    System.Console.Write(C.M1(null, in i, ref i));
                    System.Console.Write(C.M1(null, in i, in i));
                    System.Console.Write(C.M1(null, in i, i));
                    System.Console.Write(C.M1(null, i, ref i));
                    System.Console.Write(C.M1(null, i, in i));
                    System.Console.Write(C.M1(null, i, i));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, ref i, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(10, 32),
            // (11,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, ref i, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(11, 32),
            // (12,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, ref i, i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(12, 32),
            // (13,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, in i, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(13, 32),
            // (14,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, in i, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(14, 32),
            // (15,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, in i, i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(15, 32),
            // (16,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, i, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(16, 32),
            // (17,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, i, in i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(17, 32),
            // (18,32): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, ref readonly int)' and 'C.M1(I2, ref readonly int, in int)'
            //         System.Console.Write(C.M1(null, i, i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, ref readonly int)", "C.M1(I2, ref readonly int, in int)").WithLocation(18, 32));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_09()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                string M1(I1 o, in int i, in int j, in int k) => "1";
                string M1(I2 o, in int i, in int j, ref int k) => "2";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(null, in i, ref i, ref i));
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (10,57): error CS9194: Argument 3 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version 12.0 or greater.
            //         System.Console.Write(new C().M1(null, in i, ref i, ref i));
            Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "i").WithArguments("3", "11.0", "12.0").WithLocation(10, 57),
            // (10,64): error CS9194: Argument 4 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version 12.0 or greater.
            //         System.Console.Write(new C().M1(null, in i, ref i, ref i));
            Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "i").WithArguments("4", "11.0", "12.0").WithLocation(10, 64));

        var expectedDiagnostics = new[]
        {
            // (10,38): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, in int, in int)' and 'C.M1(I2, in int, in int, ref int)'
            //         System.Console.Write(new C().M1(null, in i, ref i, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, in int, in int)", "C.M1(I2, in int, in int, ref int)").WithLocation(10, 38)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_10()
    {
        var source = """
            interface I1 { }
            interface I2 { }
            class C
            {
                string M1(I1 o, in int i, in int j, in int k) => "1";
                string M1(I2 o, in int i, in int j, in int k) => "2";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(null, in i, ref i, ref i));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,38): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(I1, in int, in int, in int)' and 'C.M1(I2, in int, in int, in int)'
            //         System.Console.Write(new C().M1(null, in i, ref i, ref i));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(I1, in int, in int, in int)", "C.M1(I2, in int, in int, in int)").WithLocation(10, 38));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_11()
    {
        var source1 = """
            using System;
            using System.Runtime.InteropServices;

            interface I1 { }
            interface I2 { }

            [ComImport, Guid("96A2DE64-6D44-4DA5-BBA4-25F5F07E0E6B")]
            interface I
            {
                void M(I1 o, ref int i);
                void M(I2 o, in int i);
            }

            class C : I
            {
                void I.M(I1 o, ref int i) => System.Console.Write("1");
                void I.M(I2 o, in int i) => System.Console.Write("2");
            }
            """;

        var source2 = """
            I i = new C();
            int x = 42;
            i.M(null, 43);
            i.M(null, x);
            i.M(null, in x);
            """;

        var expectedOutput = "222";
        CompileAndVerify(new[] { source1, source2 }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        CompileAndVerify(new[] { source1, source2 }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CompileAndVerify(new[] { source1, source2 }, expectedOutput: expectedOutput).VerifyDiagnostics();

        var source3 = """
            I i = new C();
            int x = 42;
            i.M(null, ref x);
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "1", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // 1.cs(3,3): error CS0121: The call is ambiguous between the following methods or properties: 'I.M(I1, ref int)' and 'I.M(I2, in int)'
            // i.M(null, ref x);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("I.M(I1, ref int)", "I.M(I2, in int)").WithLocation(3, 3)
        };

        CreateCompilation(new[] { source1, source3 }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(new[] { source1, source3 }).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_12()
    {
        var source1 = """
            using System;
            using System.Runtime.InteropServices;

            interface I1 { }
            interface I2 { }

            [ComImport, Guid("96A2DE64-6D44-4DA5-BBA4-25F5F07E0E6B")]
            interface I
            {
                void M(I1 o, ref int i);
                void M(I2 o, ref readonly int i);
            }

            class C : I
            {
                void I.M(I1 o, ref int i) => System.Console.Write("1");
                void I.M(I2 o, ref readonly int i) => System.Console.Write("2");
            }
            """;

        var source2 = """
            I i = new C();
            int x = 42;
            i.M(null, 43);
            i.M(null, x);
            i.M(null, in x);
            """;
        CompileAndVerify(new[] { source1, source2 }, expectedOutput: "222").VerifyDiagnostics(
            // 1.cs(3,11): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
            // i.M(null, 43);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "43").WithArguments("2").WithLocation(3, 11),
            // 1.cs(4,11): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            // i.M(null, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("2").WithLocation(4, 11));

        var source3 = """
            I i = new C();
            int x = 42;
            i.M(null, ref x);
            """;
        CreateCompilation(new[] { source1, source3 }).VerifyDiagnostics(
            // 1.cs(3,3): error CS0121: The call is ambiguous between the following methods or properties: 'I.M(I1, ref int)' and 'I.M(I2, ref readonly int)'
            // i.M(null, ref x);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("I.M(I1, ref int)", "I.M(I2, ref readonly int)").WithLocation(3, 3));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_01()
    {
        var source = """
            class C
            {
                string M1(in int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(ref i));
                    System.Console.Write(new C().M1(in i));
                    System.Console.Write(new C().M1(i));
                }
            }
            static class E
            {
                public static string M1(this C c, ref int i) => "E";
            }
            """;
        CompileAndVerify(source, expectedOutput: "ECC", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (7,45): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("1").WithLocation(7, 45)
        };

        CompileAndVerify(source, expectedOutput: "CCC", parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source, expectedOutput: "CCC").VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_02()
    {
        var source = """
            using N1;
            class C
            {
                string M1(in int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(ref i));
                    System.Console.Write(new C().M1(in i));
                    System.Console.Write(new C().M1(i));
                }
            }
            static class X
            {
                public static string M1(this C c, in int i) => "X";
            }
            namespace N1
            {
                static class Y
                {
                    public static string M1(this C c, ref int i) => "Y";
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "YCC", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1),
            // (8,45): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("1").WithLocation(8, 45)
        };

        CompileAndVerify(source, expectedOutput: "CCC", parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source, expectedOutput: "CCC").VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_03()
    {
        var source = """
            using N1;
            class C
            {
                string M1(in int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(ref i));
                    System.Console.Write(new C().M1(in i));
                    System.Console.Write(new C().M1(i));
                }
            }
            static class X
            {
                public static string M1(this C c, ref readonly int i) => "X";
            }
            namespace N1
            {
                static class Y
                {
                    public static string M1(this C c, ref int i) => "Y";
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "CCC").VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1),
            // (8,45): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("1").WithLocation(8, 45));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_04()
    {
        var source1 = """
            class C
            {
                public string M1(in int i, ref int j) => "C";
            }
            static class E
            {
                public static string M1(this C c, ref int i, in int j) => "E";
            }
            """;

        var source2 = """
            int i = 5;
            System.Console.Write(new C().M1(in i, in i));
            System.Console.Write(new C().M1(in i, i));
            System.Console.Write(new C().M1(i, in i));
            System.Console.Write(new C().M1(i, i));
            """;

        var expectedDiagnostics2 = new[]
        {
            // 1.cs(2,42): error CS1620: Argument 2 must be passed with the 'ref' keyword
            // System.Console.Write(new C().M1(in i, in i));
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("2", "ref").WithLocation(2, 42),
            // 1.cs(3,39): error CS1620: Argument 2 must be passed with the 'ref' keyword
            // System.Console.Write(new C().M1(in i, i));
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("2", "ref").WithLocation(3, 39),
            // 1.cs(4,39): error CS1620: Argument 2 must be passed with the 'ref' keyword
            // System.Console.Write(new C().M1(i, in i));
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("2", "ref").WithLocation(4, 39),
            // 1.cs(5,36): error CS1620: Argument 2 must be passed with the 'ref' keyword
            // System.Console.Write(new C().M1(i, i));
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("2", "ref").WithLocation(5, 36)
        };

        CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics2);
        CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics2);
        CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(expectedDiagnostics2);

        var source3 = """
            int i = 5;
            System.Console.Write(new C().M1(ref i, ref i));
            """;

        CreateCompilation(new[] { source1, source3 }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // 1.cs(2,37): error CS9194: Argument 1 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version 12.0 or greater.
            // System.Console.Write(new C().M1(ref i, ref i));
            Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "i").WithArguments("1", "11.0", "12.0").WithLocation(2, 37));

        var expectedDiagnostics3 = new[]
        {
            // 1.cs(2,37): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            // System.Console.Write(new C().M1(ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("1").WithLocation(2, 37)
        };

        var expectedOutput3 = "C";
        CompileAndVerify(new[] { source1, source3 }, expectedOutput: expectedOutput3, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics3);
        CompileAndVerify(new[] { source1, source3 }, expectedOutput: expectedOutput3).VerifyDiagnostics(expectedDiagnostics3);

        var source4 = """
            int i = 5;
            System.Console.Write(new C().M1(ref i, i));
            System.Console.Write(new C().M1(in i, ref i));
            System.Console.Write(new C().M1(i, ref i));
            """;

        var expectedOutput4 = "ECC";
        CompileAndVerify(new[] { source1, source4 }, expectedOutput: expectedOutput4, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        CompileAndVerify(new[] { source1, source4 }, expectedOutput: expectedOutput4, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CompileAndVerify(new[] { source1, source4 }, expectedOutput: expectedOutput4).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_05()
    {
        var source = """
            class C
            {
                string M1(in int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i));
                    System.Console.Write(new C().M1(i));
                }
            }
            static class E
            {
                public static string M1(this C c, int i) => "E";
            }
            """;
        var expectedOutput = "CC";
        CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_06()
    {
        var source = """
            class C
            {
                string M1(int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i));
                    System.Console.Write(new C().M1(i));
                }
            }
            static class E
            {
                public static string M1(this C c, in int i) => "E";
            }
            """;
        var expectedOutput = "EC";
        CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_07()
    {
        var source = """
            class C
            {
                string M1(ref readonly int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(i));
                }
            }
            static class E
            {
                public static string M1(this C c, ref readonly int i) => "E";
            }
            """;
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(
            // (7,41): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         System.Console.Write(new C().M1(i));
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "i").WithArguments("1").WithLocation(7, 41));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_08()
    {
        var source = """
            class C
            {
                string M1(in int i) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i));
                }
            }
            static class E
            {
                public static string M1(this C c, ref readonly int i) => "E";
            }
            """;
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_09()
    {
        var source = """
            namespace N1
            {
                namespace N2
                {
                    class C
                    {
                        string M1(in int i) => "C";
                        static void Main()
                        {
                            int i = 5;
                            System.Console.Write(new C().M1(ref i));
                            System.Console.Write(new C().M1(in i));
                            System.Console.Write(new C().M1(i));
                        }
                    }
                    static class X
                    {
                        public static string M1(this C c, ref readonly int i) => "X";
                    }
                }
                static class Y
                {
                    public static string M1(this N2.C c, int i) => "Y";
                }
            }
            static class Z
            {
                public static string M1(this N1.N2.C c, ref int i) => "Z";
            }
            """;
        CompileAndVerify(source, expectedOutput: "CCC").VerifyDiagnostics(
            // (11,53): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //                 System.Console.Write(new C().M1(ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("1").WithLocation(11, 53));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_10()
    {
        var source = """
            class C
            {
                string M1(in int i, in int j, in int k) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i, ref i, ref i));
                }
            }
            static class E
            {
                public static string M1(this C c, in int i, in int j, ref int k) => "E";
            }
            """;
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(
            // (7,51): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(7, 51),
            // (7,58): warning CS9191: The 'ref' modifier for argument 3 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("3").WithLocation(7, 58));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_11()
    {
        var source = """
            using N1;
            class C
            {
                string M1(in int i, in int j, in int k) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i, ref i, ref i));
                }
            }
            static class X
            {
                public static string M1(this C c, in int i, in int j, ref int k) => "X";
            }
            namespace N1
            {
                static class Y
                {
                    public static string M1(this C c, in int i, in int j, in int k) => "Y";
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1),
            // (8,51): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(8, 51),
            // (8,58): warning CS9191: The 'ref' modifier for argument 3 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("3").WithLocation(8, 58));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_12()
    {
        var source = """
            using N1;
            class C
            {
                string M1(in int i, in int j, in int k) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i, ref i, ref i));
                }
            }
            static class X
            {
                public static string M1(this C c, in int i, in int j, in int k) => "X";
            }
            namespace N1
            {
                static class Y
                {
                    public static string M1(this C c, in int i, in int j, ref int k) => "Y";
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1),
            // (8,51): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(8, 51),
            // (8,58): warning CS9191: The 'ref' modifier for argument 3 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("3").WithLocation(8, 58));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_13()
    {
        var source = """
            class C
            {
                string M1(in int i, in int j, in int k) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i, in i, ref i));
                }
            }
            static class E
            {
                public static string M1<T>(this T t, in int i, in int j, ref int k) => "E";
            }
            """;
        CompileAndVerify(source, expectedOutput: "E", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (7,57): warning CS9191: The 'ref' modifier for argument 3 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, in i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("3").WithLocation(7, 57)
        };

        CompileAndVerify(source, expectedOutput: "C", parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void OverloadResolution_ExtensionMethod_14()
    {
        var source = """
            class C
            {
                string M1(in int i, in int j, in int k) => "C";
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(new C().M1(in i, ref i, ref i));
                }
            }
            static class E
            {
                public static string M1<T>(this T t, in int i, in int j, in int k) => "E";
            }
            """;
        // Neither method is better than the other, so the first scope wins.
        CompileAndVerify(source, expectedOutput: "C").VerifyDiagnostics(
            // (7,51): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(7, 51),
            // (7,58): warning CS9191: The 'ref' modifier for argument 3 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         System.Console.Write(new C().M1(in i, ref i, ref i));
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("3").WithLocation(7, 58));
    }

    [Fact]
    public void RefReadonlyParameter_PlainArgument_OverloadResolution()
    {
        var source = """
            class C
            {
                static string M1(ref readonly int i) => "ref readonly" + i;
                static string M1(int i) => "plain" + i;
                static void Main()
                {
                    int i = 5;
                    System.Console.Write(M1(i));
                    System.Console.Write(M1(6));
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "plain5plain6").VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyParameter_WrongType()
    {
        var source = """
            class C
            {
                static void M(ref readonly int i) => throw null;
                static void Main()
                {
                    string x = null;
                    M(x);
                    M(ref x);
                    M(in x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,11): error CS1503: Argument 1: cannot convert from 'string' to 'ref readonly int'
            //         M(x);
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "string", "ref readonly int").WithLocation(7, 11),
            // (8,15): error CS1503: Argument 1: cannot convert from 'ref string' to 'ref readonly int'
            //         M(ref x);
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "ref string", "ref readonly int").WithLocation(8, 15),
            // (9,14): error CS1503: Argument 1: cannot convert from 'in string' to 'ref readonly int'
            //         M(in x);
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "in string", "ref readonly int").WithLocation(9, 14));
    }

    [Theory, CombinatorialData]
    public void DefaultParameterValue_EqualsValue(bool fromMetadata)
    {
        var source1 = """
            public class C
            {
                public static void M(ref readonly int i = 1) => System.Console.Write(i);
            }
            """;
        var warning1 =
            // (3,47): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M(ref readonly int i = 1) => System.Console.Write(i);
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1").WithArguments("i").WithLocation(3, 47);
        var source2 = """
            class D
            {
                static void Main()
                {
                    int x = 2;
                    C.M();
                    C.M(x);
                    C.M(ref x);
                    C.M(in x);
                }
                static void M2() => C.M();
            }
            """;
        var warning2 =
            // (7,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         C.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 13);
        var comp = fromMetadata
            ? CreateCompilation(source2, new[] { CreateCompilation(source1).VerifyDiagnostics(warning1).EmitToImageReference() }, options: TestOptions.ReleaseExe)
            : CreateCompilation(new[] { source1, source2 }, options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "1222");
        verifier.VerifyDiagnostics(fromMetadata ? new[] { warning2 } : new[] { warning1, warning2 });
        verifier.VerifyIL("D.M2", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldc.i4.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void DefaultParameterValue_Attribute(bool fromMetadata)
    {
        var source1 = """
            using System.Runtime.InteropServices;
            public class C
            {
                public static void M([Optional, DefaultParameterValue(1)] ref readonly int i) => System.Console.Write(i);
            }
            """;
        var warning1 =
            // (4,37): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M([Optional, DefaultParameterValue(1)] ref readonly int i) => System.Console.Write(i);
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "DefaultParameterValue(1)").WithArguments("i").WithLocation(4, 37);
        var source2 = """
            class D
            {
                static void Main()
                {
                    int x = 2;
                    C.M();
                    C.M(x);
                    C.M(ref x);
                    C.M(in x);
                }
                static void M2() => C.M();
            }
            """;
        var warning2 =
            // (7,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         C.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 13);
        var comp = fromMetadata
            ? CreateCompilation(source2, new[] { CreateCompilation(source1).VerifyDiagnostics(warning1).EmitToImageReference() }, options: TestOptions.ReleaseExe)
            : CreateCompilation(new[] { source1, source2 }, options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "1222");
        verifier.VerifyDiagnostics(fromMetadata ? new[] { warning2 } : new[] { warning1, warning2 });
        verifier.VerifyIL("D.M2", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldc.i4.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void DefaultParameterValue_AttributeAndEqualsValue()
    {
        var source = """
            using System.Runtime.InteropServices;
            class C
            {
                static void M([DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,20): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     static void M([DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(4, 20),
            // (4,67): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     static void M([DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1").WithArguments("i").WithLocation(4, 67),
            // (4,67): error CS8017: The parameter has multiple distinct default values.
            //     static void M([DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "1").WithLocation(4, 67));
    }

    [Fact]
    public void DefaultParameterValue_OptionalAndEqualsValue()
    {
        var source = """
            using System.Runtime.InteropServices;
            class C
            {
                static void M([Optional] ref readonly int i = 1) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,20): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     static void M([Optional] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "Optional").WithLocation(4, 20),
            // (4,51): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     static void M([Optional] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1").WithArguments("i").WithLocation(4, 51));
    }

    [Fact]
    public void DefaultParameterValue_All()
    {
        var source = """
            using System.Runtime.InteropServices;
            class C
            {
                static void M([Optional, DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,20): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     static void M([Optional, DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "Optional").WithLocation(4, 20),
            // (4,30): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     static void M([Optional, DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(4, 30),
            // (4,77): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     static void M([Optional, DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1").WithArguments("i").WithLocation(4, 77),
            // (4,77): error CS8017: The parameter has multiple distinct default values.
            //     static void M([Optional, DefaultParameterValue(1)] ref readonly int i = 1) => throw null;
            Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "1").WithLocation(4, 77));
    }

    [Theory, CombinatorialData]
    public void DefaultParameterValue_DecimalConstant_Valid(bool fromMetadata)
    {
        var source1 = """
            using System;
            using System.Globalization;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            public class C
            {
                public static void M1([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d) => Console.WriteLine("M1 " + d.ToString(CultureInfo.InvariantCulture));
                public static void M2(ref readonly decimal d = 1.1m) => Console.WriteLine("M2 " + d.ToString(CultureInfo.InvariantCulture));
            }
            """;
        var warnings1 = new[]
        {
            // (7,38): warning CS9200: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M1([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d) => Console.WriteLine("M1 " + d.ToString(CultureInfo.InvariantCulture));
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "DecimalConstant(1, 0, 0u, 0u, 11u)").WithArguments("d").WithLocation(7, 38),
            // (8,52): warning CS9200: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M2(ref readonly decimal d = 1.1m) => Console.WriteLine("M2 " + d.ToString(CultureInfo.InvariantCulture));
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1.1m").WithArguments("d").WithLocation(8, 52),
        };
        var source2 = """
            class D
            {
                static void Main()
                {
                    decimal x = 2.2m;
                    C.M1();
                    C.M1(x);
                    C.M1(ref x);
                    C.M1(in x);

                    decimal y = 3.3m;
                    C.M2();
                    C.M2(y);
                    C.M2(ref y);
                    C.M2(in y);
                }
                static void M3() => C.M1();
                static void M4() => C.M2();
            }
            """;
        var warnings2 = new[]
        {
            // (7,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         C.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 14),
            // (13,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         C.M2(y);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "y").WithArguments("1").WithLocation(13, 14)
        };
        var comp = fromMetadata
            ? CreateCompilation(source2, new[] { CreateCompilation(source1).VerifyDiagnostics(warnings1).EmitToImageReference() }, options: TestOptions.ReleaseExe)
            : CreateCompilation(new[] { source1, source2 }, options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(comp, expectedOutput: """
            M1 1.1
            M1 2.2
            M1 2.2
            M1 2.2
            M2 1.1
            M2 3.3
            M2 3.3
            M2 3.3
            """);
        verifier.VerifyDiagnostics(fromMetadata ? warnings2 : warnings1.Concat(warnings2).ToArray());
        verifier.VerifyIL("D.M3", """
            {
              // Code size       20 (0x14)
              .maxstack  5
              .locals init (decimal V_0)
              IL_0000:  ldc.i4.s   11
              IL_0002:  ldc.i4.0
              IL_0003:  ldc.i4.0
              IL_0004:  ldc.i4.0
              IL_0005:  ldc.i4.1
              IL_0006:  newobj     "decimal..ctor(int, int, int, bool, byte)"
              IL_000b:  stloc.0
              IL_000c:  ldloca.s   V_0
              IL_000e:  call       "void C.M1(ref readonly decimal)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("D.M4", """
            {
              // Code size       20 (0x14)
              .maxstack  5
              .locals init (decimal V_0)
              IL_0000:  ldc.i4.s   11
              IL_0002:  ldc.i4.0
              IL_0003:  ldc.i4.0
              IL_0004:  ldc.i4.0
              IL_0005:  ldc.i4.1
              IL_0006:  newobj     "decimal..ctor(int, int, int, bool, byte)"
              IL_000b:  stloc.0
              IL_000c:  ldloca.s   V_0
              IL_000e:  call       "void C.M2(ref readonly decimal)"
              IL_0013:  ret
            }
            """);
    }

    [Fact]
    public void DefaultParameterValue_DecimalConstant_Invalid()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            class C
            {
                static void M1([DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d) => throw null;
                static void M2([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d = 1.1m) => throw null;
                static void Main()
                {
                    M1();
                    M2();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,21): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     static void M2([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d = 1.1m) => throw null;
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "Optional").WithLocation(6, 21),
            // (6,92): warning CS9200: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     static void M2([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d = 1.1m) => throw null;
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "1.1m").WithArguments("d").WithLocation(6, 92),
            // (9,9): error CS7036: There is no argument given that corresponds to the required parameter 'd' of 'C.M1(ref readonly decimal)'
            //         M1();
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M1").WithArguments("d", "C.M1(ref readonly decimal)").WithLocation(9, 9));
    }

    [Theory, CombinatorialData]
    public void DefaultParameterValue_DateTimeConstant_Valid(bool fromMetadata)
    {
        var source1 = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            public class C
            {
                public static void M([Optional, DateTimeConstant(100L)] ref readonly DateTime d) => Console.Write(d.Ticks);
            }
            """;
        var warning1 =
            // (6,37): warning CS9200: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M([Optional, DateTimeConstant(100L)] ref readonly DateTime d) => Console.Write(d.Ticks);
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "DateTimeConstant(100L)").WithArguments("d").WithLocation(6, 37);
        var source2 = """
            class D
            {
                static void Main() => C.M();
            }
            """;
        var comp = fromMetadata
            ? CreateCompilation(source2, new[] { CreateCompilation(source1).VerifyDiagnostics(warning1).EmitToImageReference() }, options: TestOptions.ReleaseExe)
            : CreateCompilation(new[] { source1, source2 }, options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "100");
        verifier.VerifyDiagnostics(fromMetadata ? Array.Empty<DiagnosticDescription>() : new[] { warning1 });
        verifier.VerifyIL("D.Main", """
            {
              // Code size       17 (0x11)
              .maxstack  1
              .locals init (System.DateTime V_0)
              IL_0000:  ldc.i4.s   100
              IL_0002:  conv.i8
              IL_0003:  newobj     "System.DateTime..ctor(long)"
              IL_0008:  stloc.0
              IL_0009:  ldloca.s   V_0
              IL_000b:  call       "void C.M(ref readonly System.DateTime)"
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void DefaultParameterValue_DateTimeConstant_Invalid()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            class C
            {
                static void M([DateTimeConstant(100L)] ref readonly DateTime d) => throw null;
                static void Main()
                {
                    M();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,9): error CS7036: There is no argument given that corresponds to the required parameter 'd' of 'C.M(ref readonly DateTime)'
            //         M();
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("d", "C.M(ref readonly System.DateTime)").WithLocation(8, 9));
    }

    [Theory, CombinatorialData]
    public void OperationTree([CombinatorialValues("ref ", "in ", "")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M(ref readonly int p) { }
                void M2(int x)
                /*<bind>*/{
                    M({{modifier}}x);
                }/*</bind>*/
            }
            """;
        var comp = CreateCompilation(source);

        VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, $$"""
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M({{modifier}}x);')
                Expression:
                  IInvocationOperation ( void C.M(ref readonly System.Int32 p)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M({{modifier}}x)')
                    Instance Receiver:
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null) (Syntax: '{{modifier}}x')
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            """,
            modifier == ""
                ? new[]
                {
                    // (6,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
                    //         M(x);
                    Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(6, 11)
                }
                : DiagnosticDescription.None);

        VerifyFlowGraphForTest<BlockSyntax>(comp, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M({{modifier}}x);')
                      Expression:
                        IInvocationOperation ( void C.M(ref readonly System.Int32 p)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M({{modifier}}x)')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null) (Syntax: '{{modifier}}x')
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B2]
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void Invocation_VirtualMethod()
    {
        var source = """
            class C
            {
                protected virtual void M(ref readonly int p) => System.Console.WriteLine(p);
                static void Main()
                {
                    int x = 111;
                    new C().M(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "111");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       16 (0x10)
              .maxstack  2
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   111
              IL_0002:  stloc.0
              IL_0003:  newobj     "C..ctor()"
              IL_0008:  ldloca.s   V_0
              IL_000a:  callvirt   "void C.M(ref readonly int)"
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_OverriddenMethod()
    {
        var source = """
            class B
            {
                protected virtual void M(ref readonly int p) => System.Console.WriteLine("B" + p);
            }
            class C : B
            {
                protected override void M(ref readonly int p) => System.Console.WriteLine("C" + p);
                static void Main()
                {
                    int x = 111;
                    new C().M(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "C111");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       16 (0x10)
              .maxstack  2
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   111
              IL_0002:  stloc.0
              IL_0003:  newobj     "C..ctor()"
              IL_0008:  ldloca.s   V_0
              IL_000a:  callvirt   "void B.M(ref readonly int)"
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Constructor()
    {
        var source = """
            class C
            {
                C(ref readonly int p) => System.Console.WriteLine(p);
                static void Main()
                {
                    int x = 111;
                    new C(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "111");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   111
              IL_0002:  stloc.0
              IL_0003:  ldloca.s   V_0
              IL_0005:  newobj     "C..ctor(ref readonly int)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Indexer()
    {
        var source = """
            class C
            {
                int this[ref readonly int p]
                {
                    get
                    {
                        System.Console.WriteLine(p);
                        return 0;
                    }
                }
                static void Main()
                {
                    int x = 111;
                    _ = new C()[ref x];
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "111");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       17 (0x11)
              .maxstack  2
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   111
              IL_0002:  stloc.0
              IL_0003:  newobj     "C..ctor()"
              IL_0008:  ldloca.s   V_0
              IL_000a:  call       "int C.this[ref readonly int].get"
              IL_000f:  pop
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_FunctionPointer()
    {
        var source = """
            class C
            {
                static void M(ref readonly int p) => System.Console.WriteLine(p);
                static unsafe void Main()
                {
                    delegate*<ref readonly int, void> f = &M;
                    int x = 111;
                    f(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { source, RequiresLocationAttributeDefinition },
            expectedOutput: "111", options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       19 (0x13)
              .maxstack  2
              .locals init (int V_0, //x
                            delegate*<ref readonly int, void> V_1)
              IL_0000:  ldftn      "void C.M(ref readonly int)"
              IL_0006:  ldc.i4.s   111
              IL_0008:  stloc.0
              IL_0009:  stloc.1
              IL_000a:  ldloca.s   V_0
              IL_000c:  ldloc.1
              IL_000d:  calli      "delegate*<ref readonly int, void>"
              IL_0012:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Operator_Metadata()
    {
        // public class C
        // {
        //     public static C operator+(ref readonly C x, C y) => x;
        //     public static C operator--(ref readonly C x) => x;
        //     public static implicit operator C(ref readonly int x) => null;
        //     public static explicit operator C(ref readonly short x) => null;
        // }
        var ilSource = """
            .class public auto ansi beforefieldinit C extends System.Object
            {
                .method public hidebysig specialname static 
                    class C op_Addition (
                        [in] class C& x,
                        class C y
                    ) cil managed 
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }

                .method public hidebysig specialname static 
                    class C op_Decrement (
                        [in] class C& x
                    ) cil managed 
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }

                .method public hidebysig specialname static 
                    class C op_Implicit (
                        [in] int32& x
                    ) cil managed 
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }

                .method public hidebysig specialname static 
                    class C op_Explicit (
                        [in] int16& x
                    ) cil managed 
                {
                    .param [1]
                        .custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                            01 00 00 00
                        )
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var source = """
            int i = 4;
            short s = 5;
            C c = null;
            _ = c + c;
            c--;
            c = i;
            c = (C)s;
            """;
        CreateCompilationWithIL(source, ilSource).VerifyDiagnostics(
            // (4,5): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'C'
            // _ = c + c;
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + c").WithArguments("+", "C", "C").WithLocation(4, 5),
            // (5,1): error CS0023: Operator '--' cannot be applied to operand of type 'C'
            // c--;
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "c--").WithArguments("--", "C").WithLocation(5, 1),
            // (6,5): error CS0029: Cannot implicitly convert type 'int' to 'C'
            // c = i;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "i").WithArguments("int", "C").WithLocation(6, 5),
            // (7,5): error CS0030: Cannot convert type 'short' to 'C'
            // c = (C)s;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C)s").WithArguments("short", "C").WithLocation(7, 5));
    }

    [Theory, CombinatorialData]
    public void Invocation_ExtensionMethod([CombinatorialValues("ref readonly", "ref", "in")] string modifier)
    {
        var source = $$"""
            static class C
            {
                static void M(this {{modifier}} int x) => System.Console.Write(x);
                static void Main()
                {
                    var x = 1;
                    x.M();
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", $$"""
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M({{modifier}} int)"
              IL_0009:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Invocation_ExtensionMethod_Metadata([CombinatorialValues("ref readonly", "ref", "in")] string modifier)
    {
        var source1 = $$"""
            public static class E
            {
                public static void M(this {{modifier}} int x) => System.Console.Write(x);
            }
            """;
        var comp1 = CreateCompilation(source1).VerifyDiagnostics();
        var comp1Ref = comp1.EmitToImageReference();

        var source2 = """
            static class Program
            {
                static void Main()
                {
                    var x = 1;
                    x.M();
                }
            }
            """;
        var verifier = CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.Main", $$"""
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void E.M({{modifier}} int)"
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_ExtensionMethod_SecondParameter()
    {
        var source = """
            static class C
            {
                static void M1(this int x, ref readonly int y) { }
                static void M2(this ref readonly int x, ref readonly int y) { }
                static void M3()
                {
                    var x = 1;
                    x.M1(in x);
                    x.M1(x);
                    M1(x, in x);
                    M1(x, x);
                    x.M2(in x);
                    x.M2(x);
                    M2(x, in x);
                    M2(x, x);
                    M2(in x, in x);
                    M2(in x, x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         x.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(9, 14),
            // (11,15): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            //         M1(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("2").WithLocation(11, 15),
            // (13,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         x.M2(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(13, 14),
            // (14,12): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M2(x, in x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 12),
            // (15,12): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M2(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(15, 12),
            // (15,15): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            //         M2(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("2").WithLocation(15, 15),
            // (17,18): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
            //         M2(in x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("2").WithLocation(17, 18));
    }

    [Fact]
    public void Invocation_ExtensionMethod_SecondParameter_ReadOnly()
    {
        var source = """
            static class C
            {
                static void M1(this int x, ref readonly int y) { }
                static void M2(this ref readonly int x, ref readonly int y) { }
                static void M3(ref readonly int x)
                {
                    x.M1(in x);
                    x.M1(x);
                    M1(x, in x);
                    M1(x, x);
                    x.M2(in x);
                    x.M2(x);
                    M2(x, in x);
                    M2(x, x);
                    M2(in x, in x);
                    M2(in x, x);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,14): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         x.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("1").WithLocation(8, 14),
            // (10,15): warning CS9195: Argument 2 should be passed with the 'in' keyword
            //         M1(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("2").WithLocation(10, 15),
            // (12,14): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         x.M2(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("1").WithLocation(12, 14),
            // (13,12): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M2(x, in x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("1").WithLocation(13, 12),
            // (14,12): warning CS9195: Argument 1 should be passed with the 'in' keyword
            //         M2(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("1").WithLocation(14, 12),
            // (14,15): warning CS9195: Argument 2 should be passed with the 'in' keyword
            //         M2(x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("2").WithLocation(14, 15),
            // (16,18): warning CS9195: Argument 2 should be passed with the 'in' keyword
            //         M2(in x, x);
            Diagnostic(ErrorCode.WRN_ArgExpectedIn, "x").WithArguments("2").WithLocation(16, 18));
    }

    [Fact]
    public void Invocation_ExtensionMethod_RValue()
    {
        var source = """
            static class C
            {
                static void M(this ref readonly int x) => System.Console.Write(x);
                static void Main()
                {
                    5.M();
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "5");
        verifier.VerifyDiagnostics(
            // (6,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
            //         5.M();
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("0").WithLocation(6, 9));
        verifier.VerifyIL("C.Main", """
            {
              // Code size       10 (0xa)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldc.i4.5
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "void C.M(ref readonly int)"
              IL_0009:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_ExtensionMethod_RValue_SecondParameter()
    {
        var source = """
            static class C
            {
                static void M1(this int x, ref readonly int y) { }
                static void M2(this ref readonly int x, ref readonly int y) => System.Console.Write(x + y);
                static void M3()
                {
                    5.M1(111);
                    M1(5, 111);
                    5.M2(111);
                    M2(5, 111);
                    5.M2(in 111);
                    M2(in 5, 111);
                    M2(in 5, in 111);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,14): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            //         5.M1(111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "111").WithArguments("1").WithLocation(7, 14),
            // (8,15): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
            //         M1(5, 111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "111").WithArguments("2").WithLocation(8, 15),
            // (9,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
            //         5.M2(111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("0").WithLocation(9, 9),
            // (9,14): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            //         5.M2(111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "111").WithArguments("1").WithLocation(9, 14),
            // (10,12): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            //         M2(5, 111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("1").WithLocation(10, 12),
            // (10,15): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
            //         M2(5, 111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "111").WithArguments("2").WithLocation(10, 15),
            // (11,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
            //         5.M2(in 111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "5").WithArguments("0").WithLocation(11, 9),
            // (11,17): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
            //         5.M2(in 111);
            Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "111").WithLocation(11, 17),
            // (12,15): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
            //         M2(in 5, 111);
            Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "5").WithLocation(12, 15),
            // (12,18): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
            //         M2(in 5, 111);
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "111").WithArguments("2").WithLocation(12, 18),
            // (13,15): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
            //         M2(in 5, in 111);
            Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "5").WithLocation(13, 15),
            // (13,21): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
            //         M2(in 5, in 111);
            Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "111").WithLocation(13, 21));
    }

    [Theory, CombinatorialData]
    public void Invocation_ExtensionMethod_Pointer([CombinatorialValues("ref readonly", "ref", "in")] string modifier)
    {
        var source = $$"""
            static class C
            {
                static void M(this {{modifier}} int x) => System.Console.Write(x);
                static unsafe void Main()
                {
                    var x = 1;
                    (&x)->M();
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "1",
            options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", $$"""
            {
              // Code size       11 (0xb)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  conv.u
              IL_0005:  call       "void C.M({{modifier}} int)"
              IL_000a:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_CollectionInitializer()
    {
        var source = """
            struct S : System.Collections.IEnumerable
            {
                public int i;
                public System.Collections.IEnumerator GetEnumerator() => throw null;
            }

            static class MyStructExtension
            {
                public static void Add(ref this S s, ref readonly S other)
                {
                    s.i += other.i;
                }
            }

            static class Program
            {
                static readonly S ro = new S { i = 3 };
                static void Main()
                {
                    var rw = new S { i = 2 };
                    var s = new S
                    {
                        rw, // 1
                        ro  // 2
                    };
                    System.Console.Write(s.i);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "5", verify: Verification.Fails).VerifyDiagnostics();
    }

    [Fact]
    public void Invocation_CollectionInitializer_MoreArguments()
    {
        var source = """
            struct S : System.Collections.IEnumerable
            {
                public int i;
                public System.Collections.IEnumerator GetEnumerator() => throw null;
            }

            static class MyStructExtension
            {
                public static void Add(ref this S s, ref readonly S x, ref readonly S y)
                {
                    s.i += x.i + y.i;
                }
            }

            static class Program
            {
                static readonly S ro = new S { i = 3 };
                static void Main()
                {
                    var rw = new S { i = 2 };
                    var s = new S
                    {
                        { rw, ro }, // 1
                        { ro, rw }  // 2
                    };
                    System.Console.Write(s.i);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "10", verify: Verification.Fails).VerifyDiagnostics();
    }

    [Fact]
    public void Invocation_Delegate()
    {
        var source = """
            delegate void D(ref readonly int p);
            class C
            {
                static void M(ref readonly int p) => System.Console.WriteLine(p);
                static void Main()
                {
                    D d = M;
                    int x = 111;
                    d(ref x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "111");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.Main", """
            {
              // Code size       38 (0x26)
              .maxstack  2
              .locals init (int V_0) //x
              IL_0000:  ldsfld     "D C.<>O.<0>__M"
              IL_0005:  dup
              IL_0006:  brtrue.s   IL_001b
              IL_0008:  pop
              IL_0009:  ldnull
              IL_000a:  ldftn      "void C.M(ref readonly int)"
              IL_0010:  newobj     "D..ctor(object, System.IntPtr)"
              IL_0015:  dup
              IL_0016:  stsfld     "D C.<>O.<0>__M"
              IL_001b:  ldc.i4.s   111
              IL_001d:  stloc.0
              IL_001e:  ldloca.s   V_0
              IL_0020:  callvirt   "void D.Invoke(ref readonly int)"
              IL_0025:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Dynamic_01()
    {
        var source = """
            class C
            {
                void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    dynamic d = 1;
                    var c = new C();
                    try
                    {
                        c.M(d);
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                    {
                        System.Console.Write("exception");
                    }

                    int i = 2;
                    dynamic cd = new C();
                    cd.M(ref i);
                }
                void M2(dynamic p) => M(p);

                void M(ref readonly long p) => System.Console.Write(p);
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: "exception2");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M2", """
            {
              // Code size       92 (0x5c)
              .maxstack  9
              IL_0000:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0005:  brtrue.s   IL_0045
              IL_0007:  ldc.i4     0x102
              IL_000c:  ldstr      "M"
              IL_0011:  ldnull
              IL_0012:  ldtoken    "C"
              IL_0017:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_001c:  ldc.i4.2
              IL_001d:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
              IL_0022:  dup
              IL_0023:  ldc.i4.0
              IL_0024:  ldc.i4.1
              IL_0025:  ldnull
              IL_0026:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
              IL_002b:  stelem.ref
              IL_002c:  dup
              IL_002d:  ldc.i4.1
              IL_002e:  ldc.i4.0
              IL_002f:  ldnull
              IL_0030:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
              IL_0035:  stelem.ref
              IL_0036:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
              IL_003b:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
              IL_0040:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0045:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_004a:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>>.Target"
              IL_004f:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0054:  ldarg.0
              IL_0055:  ldarg.1
              IL_0056:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, C, dynamic)"
              IL_005b:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Dynamic_02()
    {
        var source = """
            class C
            {
                void M(ref readonly int p) => System.Console.Write(p);
                static void Main()
                {
                    dynamic d = 1;
                    var c = new C();
                    try
                    {
                        c.M(d);
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                    {
                        System.Console.Write("exception");
                    }

                    int i = 2;
                    dynamic cd = new C();
                    cd.M(ref i);
                }
                void M2(dynamic p) => M(p);
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: "exception2");

        verifier.VerifyDiagnostics();

        verifier.VerifyIL("C.M2", """
            {
              // Code size       92 (0x5c)
              .maxstack  9
              IL_0000:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0005:  brtrue.s   IL_0045
              IL_0007:  ldc.i4     0x102
              IL_000c:  ldstr      "M"
              IL_0011:  ldnull
              IL_0012:  ldtoken    "C"
              IL_0017:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_001c:  ldc.i4.2
              IL_001d:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
              IL_0022:  dup
              IL_0023:  ldc.i4.0
              IL_0024:  ldc.i4.1
              IL_0025:  ldnull
              IL_0026:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
              IL_002b:  stelem.ref
              IL_002c:  dup
              IL_002d:  ldc.i4.1
              IL_002e:  ldc.i4.0
              IL_002f:  ldnull
              IL_0030:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
              IL_0035:  stelem.ref
              IL_0036:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
              IL_003b:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
              IL_0040:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0045:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_004a:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>>.Target"
              IL_004f:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>> C.<>o__2.<>p__0"
              IL_0054:  ldarg.0
              IL_0055:  ldarg.1
              IL_0056:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, C, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, C, dynamic)"
              IL_005b:  ret
            }
            """);
    }

    [Fact]
    public void Invocation_Dynamic_In()
    {
        var source = """
            class C
            {
                public void M(ref readonly int p) => System.Console.WriteLine(p);
                static void Main()
                {
                    int x = 1;
                    dynamic d = new C();
                    d.M(in x);

                    dynamic y = 2;
                    C c = new C();
                    c.M(in y);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,16): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
            //         d.M(in x);
            Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "x").WithLocation(8, 16),
            // (12,16): error CS1503: Argument 1: cannot convert from 'in dynamic' to 'ref readonly int'
            //         c.M(in y);
            Diagnostic(ErrorCode.ERR_BadArgType, "y").WithArguments("1", "in dynamic", "ref readonly int").WithLocation(12, 16));
    }

    [Theory, CombinatorialData]
    public void VisualBasic_Invocation([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
    {
        var source1 = $$"""
            public class C
            {
                public void M({{modifier}} int p) => System.Console.WriteLine(p);
            }
            """;
        var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Mscorlib40).VerifyDiagnostics();

        var source2 = """
            Public Module Program
                Public Sub Main()
                    Dim i = 123
                    Dim c = New C()
                    c.M(i)
                End Sub
            End Module
            """;
        var comp2 = CreateVisualBasicCompilation("Program", source2,
            compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
            referencedCompilations: new[] { comp1 });
        CompileAndVerify(comp2, expectedOutput: "123").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void VisualBasic_Invocation_Virtual([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        var source1 = $$"""
            public class C
            {
                public virtual void M({{modifier}} int p) => System.Console.WriteLine(p);
            }
            """;
        var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Mscorlib40).VerifyDiagnostics();

        var source2 = """
            Public Module Program
                Public Sub Main()
                    Dim i = 123
                    Dim c = New C()
                    c.M(i)
                End Sub
            End Module
            """;
        CreateVisualBasicCompilation("Program", source2, referencedCompilations: new[] { comp1 }).VerifyDiagnostics(
            // (5) : error BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
            Diagnostic(30657, "M").WithArguments("M").WithLocation(5, 11));
    }

    [Theory, CombinatorialData]
    public void VisualBasic_Override([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        var source1 = $$"""
            public class C
            {
                public virtual void M({{modifier}} int p) => System.Console.WriteLine(p);
            }
            """;
        var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Mscorlib40).VerifyDiagnostics();

        var source2 = """
            Public Class D
                Inherits C
                Public Overrides Sub M(ByRef p As Integer)
                End Sub
            End Class
            """;
        CreateVisualBasicCompilation("Program", source2, referencedCompilations: new[] { comp1 }).VerifyDiagnostics(
            // (3) : error BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
            Diagnostic(30657, "M").WithArguments("M").WithLocation(3, 26));
    }

    [Fact]
    public void Overridden_RefReadonly_RefReadonly()
    {
        var source = """
            class B
            {
                protected virtual void M(ref readonly int x) => System.Console.WriteLine("B.M" + x);
            }
            class C : B
            {
                protected override void M(ref readonly int x) => System.Console.WriteLine("C.M" + x);
                static void Main()
                {
                    var x = 123;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: """
            C.M123
            C.M123
            C.M123
            """, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Fact]
    public void Overridden_In_RefReadonly()
    {
        var source = """
            class B
            {
                protected virtual void M(in int x) => System.Console.WriteLine("B.M" + x);
            }
            class C : B
            {
                protected override void M(ref readonly int x) => System.Console.WriteLine("C.M" + x);
                static void Main()
                {
                    var x = 123;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: """
            C.M123
            C.M123
            C.M123
            """, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (7,29): warning CS9196: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in overridden or implemented member.
            //     protected override void M(ref readonly int x) => System.Console.WriteLine("C.M" + x);
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M").WithArguments("ref readonly int x", "in int x").WithLocation(7, 29),
            // (12,17): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         c.M(ref x);
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(12, 17));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Fact]
    public void Overridden_In_RefReadonly_Indexer()
    {
        var source = """
            class B
            {
                protected virtual int this[in int x]
                {
                    get
                    {
                        System.Console.WriteLine("B" + x);
                        return 0;
                    }
                    set { }
                }
            }
            class C : B
            {
                protected override int this[ref readonly int x]
                {
                    get
                    {
                        System.Console.WriteLine("C" + x);
                        return 0;
                    }
                    set { }
                }
                static void Main()
                {
                    var x = 123;
                    var c = new C();
                    _ = c[ref x];
                    _ = c[in x];
                    _ = c[x];
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: """
            C123
            C123
            C123
            """, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (22,9): warning CS9196: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in overridden or implemented member.
            //         set { }
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "set").WithArguments("ref readonly int x", "in int x").WithLocation(22, 9),
            // (28,19): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         _ = c[ref x];
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(28, 19));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<PropertySymbol>("C.this[]").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In, isProperty: true);
        }
    }

    [Fact]
    public void Overridden_RefReadonly_In()
    {
        var source = """
            class B
            {
                protected virtual void M(ref readonly int x) => System.Console.WriteLine("B.M" + x);
            }
            class C : B
            {
                protected override void M(in int x) => System.Console.WriteLine("C.M" + x);
                static void Main()
                {
                    var x = 123;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: """
            C.M123
            C.M123
            C.M123
            """, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics(
            // (7,29): warning CS9196: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in overridden or implemented member.
            //     protected override void M(in int x) => System.Console.WriteLine("C.M" + x);
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M").WithArguments("in int x", "ref readonly int x").WithLocation(7, 29),
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            var p = m.GlobalNamespace.GetMember<MethodSymbol>("B.M").Parameters.Single();
            VerifyRefReadonlyParameter(p, customModifiers: VerifyModifiers.In);
        }
    }

    [Theory, CombinatorialData]
    public void Overridden_NotIn_RefReadonly([CombinatorialValues("ref", "out")] string modifier)
    {
        var source = $$"""
            class B
            {
                protected virtual void M({{modifier}} int x) => throw null!;
            }
            class C : B
            {
                protected override void M(ref readonly int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,29): error CS0115: 'C.M(ref readonly int)': no suitable method found to override
            //     protected override void M(ref readonly int x) { }
            Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M").WithArguments("C.M(ref readonly int)").WithLocation(7, 29));
    }

    [Theory, CombinatorialData]
    public void Overridden_RefReadonly_NotIn([CombinatorialValues("ref", "out")] string modifier)
    {
        var source = $$"""
            class B
            {
                protected virtual void M(ref readonly int x) { }
            }
            class C : B
            {
                protected override void M({{modifier}} int x) => throw null!;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,29): error CS0115: 'C.M(ref int)': no suitable method found to override
            //     protected override void M(ref int x) { }
            Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M").WithArguments($"C.M({modifier} int)").WithLocation(7, 29));
    }

    [Theory, CombinatorialData]
    public void Overridden_GenericBase([CombinatorialValues("ref readonly", "in")] string modifier)
    {
        var source = $$"""
            class B<T>
            {
                protected virtual void M(in T x) => throw null;
                protected virtual void M(ref readonly int x) => throw null;
            }
            class C : B<int>
            {
                protected override void M({{modifier}} int x) => throw null;
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net50).VerifyDiagnostics(
            // (8,29): error CS0462: The inherited members 'B<T>.M(in T)' and 'B<T>.M(ref readonly int)' have the same signature in type 'C', so they cannot be overridden
            //     protected override void M(ref readonly int x)
            Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("B<T>.M(in T)", "B<T>.M(ref readonly int)", "C").WithLocation(8, 29));
    }

    [Fact]
    public void Hiding_RefReadonly_RefReadonly()
    {
        var source = """
            class B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (7,17): warning CS0108: 'C.M(ref readonly int)' hides inherited member 'B.M(ref readonly int)'. Use the new keyword if hiding was intended.
            //     public void M(ref readonly int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("C.M(ref readonly int)", "B.M(ref readonly int)").WithLocation(7, 17),
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));
    }

    [Fact]
    public void Hiding_RefReadonly_RefReadonly_New()
    {
        var source = """
            class B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public new void M(ref readonly int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));
    }

    [Fact]
    public void Hiding_In_RefReadonly()
    {
        var source = """
            class B
            {
                public void M(in int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (7,17): warning CS0108: 'C.M(ref readonly int)' hides inherited member 'B.M(in int)'. Use the new keyword if hiding was intended.
            //     public void M(ref readonly int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("C.M(ref readonly int)", "B.M(in int)").WithLocation(7, 17),
            // (7,17): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //     public void M(ref readonly int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "M").WithArguments("ref readonly int x", "in int x").WithLocation(7, 17),
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));
    }

    [Fact]
    public void Hiding_In_RefReadonly_New()
    {
        var source = """
            class B
            {
                public void M(in int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public new void M(ref readonly int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (7,21): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //     public new void M(ref readonly int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "M").WithArguments("ref readonly int x", "in int x").WithLocation(7, 21),
            // (14,13): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(14, 13));
    }

    [Fact]
    public void Hiding_In_RefReadonly_Indexer()
    {
        var source = """
            class B
            {
                public int this[in int x]
                {
                    get
                    {
                        System.Console.WriteLine("B" + x);
                        return 0;
                    }
                    set { }
                }
            }
            class C : B
            {
                public int this[ref readonly int x]
                {
                    get
                    {
                        System.Console.WriteLine("C" + x);
                        return 0;
                    }
                    set { }
                }
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    _ = c[ref x];
                    _ = c[in x];
                    _ = c[x];
                    _ = ((B)c)[in x];
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (15,16): warning CS0108: 'C.this[ref readonly int]' hides inherited member 'B.this[in int]'. Use the new keyword if hiding was intended.
            //     public int this[ref readonly int x]
            Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("C.this[ref readonly int]", "B.this[in int]").WithLocation(15, 16),
            // (17,9): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //         get
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "get").WithArguments("ref readonly int x", "in int x").WithLocation(17, 9),
            // (22,9): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //         set { }
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "set").WithArguments("ref readonly int x", "in int x").WithLocation(22, 9),
            // (30,15): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         _ = c[x];
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(30, 15));
    }

    [Fact]
    public void Hiding_In_RefReadonly_Indexer_New()
    {
        var source = """
            class B
            {
                public int this[in int x]
                {
                    get
                    {
                        System.Console.WriteLine("B" + x);
                        return 0;
                    }
                    set { }
                }
            }
            class C : B
            {
                public new int this[ref readonly int x]
                {
                    get
                    {
                        System.Console.WriteLine("C" + x);
                        return 0;
                    }
                    set { }
                }
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    _ = c[ref x];
                    _ = c[in x];
                    _ = c[x];
                    _ = ((B)c)[in x];
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (17,9): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //         get
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "get").WithArguments("ref readonly int x", "in int x").WithLocation(17, 9),
            // (22,9): warning CS9197: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in hidden member.
            //         set { }
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "set").WithArguments("ref readonly int x", "in int x").WithLocation(22, 9),
            // (30,15): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         _ = c[x];
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(30, 15));
    }

    [Fact]
    public void Hiding_RefReadonly_In()
    {
        var source = """
            class B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public void M(in int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (7,17): warning CS0108: 'C.M(in int)' hides inherited member 'B.M(ref readonly int)'. Use the new keyword if hiding was intended.
            //     public void M(in int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("C.M(in int)", "B.M(ref readonly int)").WithLocation(7, 17),
            // (7,17): warning CS9197: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in hidden member.
            //     public void M(in int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "M").WithArguments("in int x", "ref readonly int x").WithLocation(7, 17),
            // (12,17): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         c.M(ref x);
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(12, 17));
    }

    [Fact]
    public void Hiding_RefReadonly_In_New()
    {
        var source = """
            class B
            {
                public void M(ref readonly int x) => System.Console.WriteLine("B" + x);
            }
            class C : B
            {
                public new void M(in int x) => System.Console.WriteLine("C" + x);
                static void Main()
                {
                    var x = 111;
                    var c = new C();
                    c.M(ref x);
                    c.M(in x);
                    c.M(x);
                    ((B)c).M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C111
            C111
            C111
            B111
            """).VerifyDiagnostics(
            // (7,21): warning CS9197: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in hidden member.
            //     public new void M(in int x) => System.Console.WriteLine("C" + x);
            Diagnostic(ErrorCode.WRN_HidingDifferentRefness, "M").WithArguments("in int x", "ref readonly int x").WithLocation(7, 21),
            // (12,17): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         c.M(ref x);
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(12, 17));
    }

    [Fact]
    public void Hiding_RefReadonly_Ref()
    {
        var source = """
            class B
            {
                public void M1(ref int x) => System.Console.WriteLine("B.M1");
                public void M2(ref readonly int x) => System.Console.WriteLine("B.M2");
            }
            class C : B
            {
                public void M1(ref readonly int x) => System.Console.WriteLine("C.M1");
                public void M2(ref int x) => System.Console.WriteLine("C.M2");
                static void Main()
                {
                    var x = 1;
                    var c = new C();
                    c.M1(ref x);
                    c.M1(in x);
                    c.M1(x);
                    ((B)c).M1(ref x);
                    c.M2(ref x);
                    c.M2(in x);
                    c.M2(x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C.M1
            C.M1
            C.M1
            B.M1
            C.M2
            B.M2
            B.M2
            """).VerifyDiagnostics(
            // (16,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(16, 14),
            // (20,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M2(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(20, 14));
    }

    [Fact]
    public void Hiding_RefReadonly_Out()
    {
        var source = """
            class B
            {
                public void M1(out int x)
                {
                    x = 5;
                    System.Console.WriteLine("B.M1");
                }
                public void M2(ref readonly int x) => System.Console.WriteLine("B.M2");
            }
            class C : B
            {
                public void M1(ref readonly int x) => System.Console.WriteLine("C.M1");
                public void M2(out int x)
                {
                    x = 5;
                    System.Console.WriteLine("C.M2");
                }
                static void Main()
                {
                    var x = 1;
                    var c = new C();
                    c.M1(ref x);
                    c.M1(in x);
                    c.M1(x);
                    c.M1(out x);
                    c.M2(ref x);
                    c.M2(in x);
                    c.M2(x);
                    c.M2(out x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C.M1
            C.M1
            C.M1
            B.M1
            B.M2
            B.M2
            B.M2
            C.M2
            """).VerifyDiagnostics(
            // (24,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(24, 14),
            // (28,14): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M2(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(28, 14));
    }

    [Theory, CombinatorialData]
    public void Hiding_RefReadonly_NotIn_New([CombinatorialValues("ref", "out")] string modifier)
    {
        var source = $$"""
            class B
            {
                public void M1({{modifier}} int x) => throw null!;
                public void M2(ref readonly int x) { }
            }
            class C : B
            {
                public new void M1(ref readonly int x) { }
                public new void M2({{modifier}} int x) => throw null!;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,21): warning CS0109: The member 'C.M1(ref readonly int)' does not hide an accessible member. The new keyword is not required.
            //     public new void M1(ref readonly int x) { }
            Diagnostic(ErrorCode.WRN_NewNotRequired, "M1").WithArguments("C.M1(ref readonly int)").WithLocation(8, 21),
            // (9,21): warning CS0109: The member 'C.M2(ref int)' does not hide an accessible member. The new keyword is not required.
            //     public new void M2(ref int x) => throw null!;
            Diagnostic(ErrorCode.WRN_NewNotRequired, "M2").WithArguments($"C.M2({modifier} int)").WithLocation(9, 21));
    }

    [Fact]
    public void Implementation_RefReadonly_RefReadonly()
    {
        var source = """
            interface I
            {
                void M(ref readonly int x);
            }
            class C : I
            {
                public void M(ref readonly int x) => System.Console.WriteLine("C.M" + x);
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    c.M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "C.M1").VerifyDiagnostics();
    }

    [Fact]
    public void Implementation_RefReadonly_RefReadonly_Explicit()
    {
        var source = """
            interface I
            {
                void M(ref readonly int x);
            }
            class C : I
            {
                void I.M(ref readonly int x) => System.Console.WriteLine("C.M" + x);
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    c.M(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "C.M1").VerifyDiagnostics();
    }

    [Fact]
    public void Implementation_RefReadonly_In()
    {
        var source = """
            interface I
            {
                void M1(in int x);
                void M2(ref readonly int x);
            }
            class C : I
            {
                public void M1(ref readonly int x) => System.Console.WriteLine("C.M1" + x);
                public void M2(in int x) => System.Console.WriteLine("C.M2" + x);
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    c.M1(in x);
                    c.M2(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C.M11
            C.M21
            """).VerifyDiagnostics(
            // (8,17): warning CS9196: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in overridden or implemented member.
            //     public void M1(ref readonly int x) { }
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M1").WithArguments("ref readonly int x", "in int x").WithLocation(8, 17),
            // (9,17): warning CS9196: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in overridden or implemented member.
            //     public void M2(in int x) { }
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M2").WithArguments("in int x", "ref readonly int x").WithLocation(9, 17));
    }

    [Fact]
    public void Implementation_RefReadonly_In_Explicit()
    {
        var source = """
            interface I
            {
                void M1(in int x);
                void M2(ref readonly int x);
            }
            class C : I
            {
                void I.M1(ref readonly int x) => System.Console.WriteLine("C.M1" + x);
                void I.M2(in int x) => System.Console.WriteLine("C.M2" + x);
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    c.M1(in x);
                    c.M2(in x);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            C.M11
            C.M21
            """).VerifyDiagnostics(
            // (8,12): warning CS9196: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in overridden or implemented member.
            //     void I.M1(ref readonly int x) { }
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M1").WithArguments("ref readonly int x", "in int x").WithLocation(8, 12),
            // (9,12): warning CS9196: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in overridden or implemented member.
            //     void I.M2(in int x) { }
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "M2").WithArguments("in int x", "ref readonly int x").WithLocation(9, 12));
    }

    [Theory, CombinatorialData]
    public void Implementation_RefReadonly_In_Explicit_Retargeting(bool emit)
    {
        var source1v1 = """
            public interface I
            {
                void M(in int x);
            }
            """;
        var comp1v1 = CreateCompilation(source1v1, assemblyName: "Assembly1");
        var comp1v1Ref = emit ? comp1v1.EmitToImageReference() : comp1v1.ToMetadataReference();

        var source1v2 = """
            public interface I
            {
                void M(ref readonly int x);
            }
            """;
        var comp1v2 = CreateCompilation(source1v2, assemblyName: "Assembly1");
        var comp1v2Ref = emit ? comp1v2.EmitToImageReference() : comp1v2.ToMetadataReference();

        var source2 = """
            public class C : I
            {
                void I.M(in int x) { }
            }
            """;
        var comp2 = CreateCompilation(source2, new[] { comp1v1Ref }, assemblyName: "Assembly2");
        var comp2Ref = emit ? comp2.EmitToImageReference() : comp2.ToMetadataReference();

        var comp3v1 = CreateCompilation("", new[] { comp2Ref, comp1v1Ref }, assemblyName: "Assembly3");

        var c1 = comp3v1.GetMember<NamedTypeSymbol>("C");
        var m1 = c1.GetMember<MethodSymbol>("I.M");
        Assert.True(m1 is not RetargetingMethodSymbol);
        Assert.Equal("I.M(in int)", m1.ExplicitInterfaceImplementations.Single().ToDisplayString());

        var comp3v2 = CreateCompilation("", new[] { comp2Ref, comp1v2Ref }, assemblyName: "Assembly3");

        var c2 = comp3v2.GetMember<NamedTypeSymbol>("C");
        var m2 = c2.GetMember<MethodSymbol>("I.M");
        Assert.Equal(!emit, m2 is RetargetingMethodSymbol);
        Assert.Equal("I.M(ref readonly int)", m2.ExplicitInterfaceImplementations.Single().ToDisplayString());
    }

    [Fact]
    public void Implementation_RefReadonly_In_Close()
    {
        var source = """
            interface I
            {
                void M1(in int x);
                void M2(ref readonly int x);
            }
            class C : I
            {
                void M1(ref readonly int x) { }
                void M2(in int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,11): error CS0737: 'C' does not implement interface member 'I.M1(in int)'. 'C.M1(ref readonly int)' cannot implement an interface member because it is not public.
            // class C : I
            Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I").WithArguments("C", "I.M1(in int)", "C.M1(ref readonly int)").WithLocation(6, 11),
            // (6,11): error CS0737: 'C' does not implement interface member 'I.M2(ref readonly int)'. 'C.M2(in int)' cannot implement an interface member because it is not public.
            // class C : I
            Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I").WithArguments("C", "I.M2(ref readonly int)", "C.M2(in int)").WithLocation(6, 11));
    }

    [Fact]
    public void Implementation_RefReadonly_In_Indexer()
    {
        var source = """
            interface I
            {
                int this[ref readonly int x] { get; set; }
            }
            class C : I
            {
                public int this[in int x]
                {
                    get
                    {
                        System.Console.WriteLine("get" + x);
                        return 0;
                    }
                    set
                    {
                        System.Console.WriteLine("set" + x);
                    }
                }
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    _ = c[in x];
                    c[in x] = 0;
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            get1
            set1
            """).VerifyDiagnostics(
            // (14,9): warning CS9196: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in overridden or implemented member.
            //         set
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "set").WithArguments("in int x", "ref readonly int x").WithLocation(14, 9));
    }

    [Fact]
    public void Implementation_RefReadonly_In_Indexer_Explicit()
    {
        var source = """
            interface I
            {
                int this[ref readonly int x] { get; set; }
            }
            class C : I
            {
                int I.this[in int x]
                {
                    get
                    {
                        System.Console.WriteLine("get" + x);
                        return 0;
                    }
                    set
                    {
                        System.Console.WriteLine("set" + x);
                    }
                }
                static void Main()
                {
                    var x = 1;
                    I c = new C();
                    _ = c[in x];
                    c[in x] = 0;
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: """
            get1
            set1
            """).VerifyDiagnostics(
            // (14,9): warning CS9196: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in overridden or implemented member.
            //         set
            Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "set").WithArguments("in int x", "ref readonly int x").WithLocation(14, 9));
    }

    [Theory, CombinatorialData]
    public void Implementation_RefReadonly_NotIn([CombinatorialValues("ref", "out")] string modifier)
    {
        var source = $$"""
            interface I
            {
                void M1({{modifier}} int x);
                void M2(ref readonly int x);
            }
            class C : I
            {
                public void M1(ref readonly int x) { }
                public void M2({{modifier}} int x) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,11): error CS0535: 'C' does not implement interface member 'I.M1(ref int)'
            // class C : I
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", $"I.M1({modifier} int)").WithLocation(6, 11),
            // (6,11): error CS0535: 'C' does not implement interface member 'I.M2(ref readonly int)'
            // class C : I
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", "I.M2(ref readonly int)").WithLocation(6, 11));
    }

    [Theory, CombinatorialData]
    public void Implementation_RefReadonly_NotIn_Explicit([CombinatorialValues("ref", "out")] string modifier)
    {
        var source = $$"""
            interface I
            {
                void M1({{modifier}} int x);
                void M2(ref readonly int x);
            }
            class C : I
            {
                void I.M1(ref readonly int x) { }
                void I.M2({{modifier}} int x) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,11): error CS0535: 'C' does not implement interface member 'I.M1(ref int)'
            // class C : I
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", $"I.M1({modifier} int)").WithLocation(6, 11),
            // (6,11): error CS0535: 'C' does not implement interface member 'I.M2(ref readonly int)'
            // class C : I
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", "I.M2(ref readonly int)").WithLocation(6, 11),
            // (8,12): error CS0539: 'C.M1(ref readonly int)' in explicit interface declaration is not found among members of the interface that can be implemented
            //     void I.M1(ref readonly int x) { }
            Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("C.M1(ref readonly int)").WithLocation(8, 12),
            // (9,12): error CS0539: 'C.M2(ref int)' in explicit interface declaration is not found among members of the interface that can be implemented
            //     void I.M2(ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments($"C.M2({modifier} int)").WithLocation(9, 12));
    }

    [Theory, CombinatorialData]
    public void DuplicateMembers([CombinatorialValues("in", "ref", "out")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M(ref readonly int x) { }
                void M({{modifier}} int x) => throw null;
                void M(int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,10): error CS0663: 'C' cannot define an overloaded method that differs only on parameter modifiers 'in' and 'ref readonly'
            //     void M(in int x) => throw null;
            Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("C", "method", modifier, "ref readonly").WithLocation(4, 10));
    }

    [Fact]
    public void PartialMembers_RefReadonly()
    {
        var source = """
            partial class C
            {
                public partial void M(ref readonly int x);
            }
            partial class C
            {
                public partial void M(ref readonly int x) => throw null;
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void PartialMembers_RefReadonly_Inverse()
    {
        var source = """
            partial class C
            {
                public partial void M(ref readonly int x) => throw null;
            }
            partial class C
            {
                public partial void M(ref readonly int x);
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Theory, CombinatorialData]
    public void PartialMembers([CombinatorialValues("ref ", "out ", "in ", "")] string modifier)
    {
        var source = $$"""
            partial class C
            {
                public partial void M(ref readonly int x);
            }
            partial class C
            {
                public partial void M({{modifier}} int x) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,25): error CS8795: Partial method 'C.M(ref readonly int)' must have an implementation part because it has accessibility modifiers.
            //     public partial void M(ref readonly int x);
            Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("C.M(ref readonly int)").WithLocation(3, 25),
            // (7,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(ref int)'
            //     public partial void M(ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments($"C.M({modifier}int)").WithLocation(7, 25));
    }

    [Theory, CombinatorialData]
    public void PartialMembers_Inverse([CombinatorialValues("ref ", "out ", "in ", "")] string modifier)
    {
        var source = $$"""
            partial class C
            {
                public partial void M(ref readonly int x) => throw null;
            }
            partial class C
            {
                public partial void M({{modifier}} int x);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(ref readonly int)'
            //     public partial void M(ref readonly int x) => throw null;
            Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(ref readonly int)").WithLocation(3, 25),
            // (7,25): error CS8795: Partial method 'C.M(ref int)' must have an implementation part because it has accessibility modifiers.
            //     public partial void M(ref int x);
            Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments($"C.M({modifier}int)").WithLocation(7, 25));
    }

    [Theory, CombinatorialData]
    public void PartialMembers_DifferentReturnType([CombinatorialValues("ref ", "out ", "in ", "")] string modifier)
    {
        var source = $$"""
            #nullable enable
            partial class C
            {
                public partial string M(ref readonly int x);
            }
            partial class C
            {
                public partial string? M({{modifier}} int x) => throw null!;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,27): error CS8795: Partial method 'C.M(ref readonly int)' must have an implementation part because it has accessibility modifiers.
            //     public partial string M(ref readonly int x);
            Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("C.M(ref readonly int)").WithLocation(4, 27),
            // (8,28): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(out int)'
            //     public partial string? M(out int x) => throw null;
            Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments($"C.M({modifier}int)").WithLocation(8, 28));
    }

    [Theory, CombinatorialData]
    public void MethodGroupComparer([CombinatorialValues("ref", "in", "")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M(ref readonly int x) { }
                void M2()
                {
                    var m = this.M;
                }
            }
            static class E1
            {
                public static void M(this C c, {{modifier}} int x) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (6,17): error CS8917: The delegate type could not be inferred.
            //         var m = this.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "this.M").WithLocation(6, 17));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void MethodGroupComparer_Inverse([CombinatorialValues("ref", "in", "")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M({{modifier}} int x) { }
                void M2()
                {
                    var m = this.M;
                }
            }
            static class E1
            {
                public static void M(this C c, ref readonly int x) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (6,17): error CS8917: The delegate type could not be inferred.
            //         var m = this.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "this.M").WithLocation(6, 17));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void MethodGroupComparer_TwoExtensionMethods([CombinatorialValues("ref", "in", "")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M2()
                {
                    var m = this.M;
                }
            }
            static class E1
            {
                public static void M(this C c, ref readonly int x) { }
            }
            static class E2
            {
                public static void M(this C c, {{modifier}} int x) { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var m = this.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "this.M").WithLocation(5, 17));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var m = this.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "this.M").WithLocation(5, 17));
    }

    /// <summary>
    /// If <see cref="MemberSignatureComparer.CrefComparer"/> allowed 'ref readonly'/'in' mismatch,
    /// this would report ambiguous cref warning for 'in'.
    /// </summary>
    [Theory, CombinatorialData]
    public void CrefComparer([CombinatorialValues("ref", "in")] string modifier)
    {
        var refKind = modifier switch
        {
            "ref" => RefKind.Ref,
            "in" => RefKind.In,
            _ => throw ExceptionUtilities.UnexpectedValue(modifier)
        };

        var source = $$"""
            /// <summary>
            /// <see cref="M({{modifier}} int)"/>
            /// </summary>
            public class C
            {
                void M(ref readonly int x) { }
                void M({{modifier}} int x) { }
            }
            """;
        var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(
            // (7,10): error CS0663: 'C' cannot define an overloaded method that differs only on parameter modifiers 'ref' and 'ref readonly'
            //     void M(ref int x) { }
            Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("C", "method", modifier, "ref readonly").WithLocation(7, 10));
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var docComment = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().Single();
        var cref = docComment.DescendantNodes().OfType<XmlCrefAttributeSyntax>().Select(attr => attr.Cref).Single();
        var info = model.GetSymbolInfo(cref);
        var methodFromCref = info.Symbol as IMethodSymbol;
        Assert.Equal(refKind, methodFromCref!.Parameters.Single().RefKind);
        var methodFromClass = comp.GetMembers("C.M").Cast<MethodSymbol>().Single(m => m.Parameters.Single().RefKind == refKind);
        Assert.Same(methodFromCref, methodFromClass.GetPublicSymbol());
    }

    [Theory, CombinatorialData]
    public void CrefComparer_RefReadonly([CombinatorialValues("ref", "in")] string modifier)
    {
        var source = $$"""
            /// <summary>
            /// <see cref="M(ref readonly int)"/>
            /// </summary>
            public class C
            {
                void M(ref readonly int x) { }
                void M({{modifier}} int x) { }
            }
            """;
        var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(
            // (7,10): error CS0663: 'C' cannot define an overloaded method that differs only on parameter modifiers 'ref' and 'ref readonly'
            //     void M(ref int x) { }
            Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("C", "method", modifier, "ref readonly").WithLocation(7, 10));
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var docComment = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().Single();
        var cref = docComment.DescendantNodes().OfType<XmlCrefAttributeSyntax>().Select(attr => attr.Cref).Single();
        var info = model.GetSymbolInfo(cref);
        var methodFromCref = info.Symbol as IMethodSymbol;
        Assert.Equal(RefKind.RefReadOnlyParameter, methodFromCref!.Parameters.Single().RefKind);
        var methodFromClass = comp.GetMembers("C.M").Cast<MethodSymbol>().Single(m => m.Parameters.Single().RefKind == RefKind.RefReadOnlyParameter);
        Assert.Same(methodFromCref, methodFromClass.GetPublicSymbol());
    }

    [Fact]
    public void NoPia()
    {
        var source1 = """
            using System;
            using System.Runtime.InteropServices;
            [assembly: ImportedFromTypeLib("test.dll")]
            [assembly: Guid("EB40B9A2-B368-4001-93E4-8571A8AB3215")]
            [ComImport, Guid("EB40B9A2-B368-4001-93E4-8571A8AB3215")]
            public interface Test
            {
                void Method(ref readonly int x);
            }
            """;
        var comp1 = CreateCompilationWithMscorlib40(source1).VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference(embedInteropTypes: true);

        var source2 = """
            class Program
            {
                public void M(Test p)
                {
                    var x = 1;
                    p.Method(in x);
                }
            }
            """;
        var comp2 = CreateCompilationWithMscorlib40(source2, new[] { comp1Ref });
        CompileAndVerify(comp2, symbolValidator: verify).VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>(RequiresLocationAttributeQualifiedName));

            var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.Method");
            var parameter = method.Parameters.Single();
            // Because no attribute is embedded with the parameter, it's decoded as `ref`, not `ref readonly`,
            // and combined with `modreq(In)` that results in a use site error. Same thing happens for `in` parameters.
            VerifyRefReadonlyParameter(parameter, refKind: false, customModifiers: VerifyModifiers.In, useSiteError: true);
            Assert.Equal(RefKind.Ref, parameter.RefKind);
        }
    }

    [Theory]
    [InlineData("", "", true)]
    [InlineData("", "ref", false)]
    [InlineData("", "in", false)]
    [InlineData("", "out", false)]
    [InlineData("", "ref readonly", null, false)]
    [InlineData("ref", "in", false, false)]
    [InlineData("ref", "out", false)]
    [InlineData("ref", "ref readonly", null, false)]
    [InlineData("in", "out", false)]
    [InlineData("in", "ref readonly", null, true)]
    [InlineData("out", "ref readonly", null, false)]
    public void Conversions(string x, string y, bool? validInCSharp11, bool? validInCSharp12 = null)
    {
        var source = $$"""
            X x = C.X;
            Y y = C.Y;

            var i = 1;
            x({{getArgumentModifier(x)}} i);
            y({{getArgumentModifier(y)}} i);

            x = C.Y;
            y = C.X;

            x({{getArgumentModifier(x)}} i);
            y({{getArgumentModifier(y)}} i);

            class C
            {
                public static void X({{x}} int p) {{getCode(x, "X")}}
                public static void Y({{y}} int p) {{getCode(y, "Y")}}
            }

            delegate void X({{x}} int p);
            delegate void Y({{y}} int p);
            """;

        var expectedOutput = "XYYX";

        var expectedDiagnostics = new[]
        {
            // (8,7): error CS0123: No overload for 'Y' matches delegate 'X'
            // x = C.Y;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y").WithArguments("Y", "X").WithLocation(8, 7),
            // (9,7): error CS0123: No overload for 'X' matches delegate 'Y'
            // y = C.X;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "X").WithArguments("X", "Y").WithLocation(9, 7)
        };

        if (validInCSharp11 == true)
        {
            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }
        else if (validInCSharp11 == false)
        {
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        }
        else
        {
            Assert.NotNull(validInCSharp12);
        }

        if (validInCSharp12 ?? validInCSharp11 == true)
        {
            var expectedWarnings = (x, y) is ("in", "ref readonly") or ("ref", "in") or ("ref", "ref readonly")
                ? new[]
                {
                    // (8,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
                    // x = C.Y;
                    Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments($"{y} int p", $"{x} int p").WithLocation(8, 5),
                    // (9,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                    // y = C.X;
                    Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.X").WithArguments($"{x} int p", $"{y} int p").WithLocation(9, 5)
                }
                : Array.Empty<DiagnosticDescription>();

            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedWarnings);
            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expectedWarnings);
        }
        else
        {
            if (x is "ref" && y is "in" or "ref readonly")
            {
                expectedDiagnostics = new[]
                {
                    // (8,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref int p' in target.
                    // x = C.Y;
                    Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments($"{y} int p", "ref int p").WithLocation(8, 5),
                    // (9,7): error CS0123: No overload for 'X' matches delegate 'Y'
                    // y = C.X;
                    Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "X").WithArguments("X", "Y").WithLocation(9, 7)
                };
            }

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expectedDiagnostics);
        }

        static string getCode(string modifier, string output)
        {
            return modifier == "out"
                ? $$"""{ p = 0; System.Console.Write("{{output}}"); }"""
                : $$"""=> System.Console.Write("{{output}}");""";
        }

        static string getArgumentModifier(string modifier)
        {
            return modifier switch
            {
                "ref readonly" => "in",
                _ => modifier
            };
        }
    }

    [Theory, CombinatorialData]
    public void Conversion_ExtensionMethod([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            class C
            {
                public void X1(ref readonly int p) => System.Console.Write("X1 ");
                public void Y1({{modifier}} int p) => System.Console.Write("Y1 ");
            }

            static class E
            {
                public static void X2(this C c, ref readonly int p) => System.Console.Write("X2 ");
                public static void Y2(this C c, {{modifier}} int p) => System.Console.Write("Y2 ");
            }

            delegate void X(ref readonly int p);
            delegate void Y({{modifier}} int p);
            """;

        var source2 = """
            var c = new C();
            X x = c.X1;
            var i = 1;
            x(in i);
            x = c.Y1;
            x(in i);
            x = c.Y2;
            x(in i);
            """;

        if (modifier == "ref")
        {
            CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(
                // 1.cs(5,7): error CS0123: No overload for 'Y1' matches delegate 'X'
                // x = c.Y1;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y1").WithArguments("Y1", "X").WithLocation(5, 7),
                // 1.cs(7,7): error CS0123: No overload for 'Y2' matches delegate 'X'
                // x = c.Y2;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y2").WithArguments("Y2", "X").WithLocation(7, 7));
        }
        else
        {
            CompileAndVerify(new[] { source1, source2 }, expectedOutput: "X1 Y1 Y2").VerifyDiagnostics(
                // 1.cs(5,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = c.Y1;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "c.Y1").WithArguments("in int p", "ref readonly int p").WithLocation(5, 5),
                // 1.cs(7,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = c.Y2;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "c.Y2").WithArguments("in int p", "ref readonly int p").WithLocation(7, 5));
        }

        var source3 = $"""
            var c = new C();
            Y y = c.Y1;
            var i = 1;
            y({modifier} i);
            y = c.X1;
            y({modifier} i);
            y = c.X2;
            y({modifier} i);
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "Y1 X1 X2").VerifyDiagnostics(
            // 1.cs(5,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = c.X1;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "c.X1").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(5, 5),
            // 1.cs(7,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = c.X2;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "c.X2").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(7, 5));
    }

    [Theory, CombinatorialData]
    public void Conversion_Tuple([CombinatorialValues("in", "ref")] string modifier)
    {
        var source = $$"""
            (X x, Y y) = (C.X, C.Y);
            
            var i = 1;
            x(in i);
            y({{modifier}} i);
            
            (x, y) = (C.Y, C.X);
            
            x(in i);
            y({{modifier}} i);
            
            class C
            {
                public static void X(ref readonly int p) => System.Console.Write("X");
                public static void Y({{modifier}} int p) => System.Console.Write("Y");
            }
            
            delegate void X(ref readonly int p);
            delegate void Y({{modifier}} int p);
            """;

        if (modifier == "ref")
        {
            CreateCompilation(source).VerifyDiagnostics(
                // (7,13): error CS0123: No overload for 'Y' matches delegate 'X'
                // (x, y) = (C.Y, C.X);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y").WithArguments("Y", "X").WithLocation(7, 13));
        }
        else
        {
            CompileAndVerify(source, expectedOutput: "XYYX").VerifyDiagnostics(
                // (7,11): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // (x, y) = (C.Y, C.X);
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments($"{modifier} int p", "ref readonly int p").WithLocation(7, 11),
                // (7,16): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
                // (x, y) = (C.Y, C.X);
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.X").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(7, 16));
        }
    }

    [Theory, CombinatorialData]
    public void Conversion_Cast([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            class C
            {
                public static void X(ref readonly int p) => System.Console.Write("X");
                public static void Y({{modifier}} int p) => System.Console.Write("Y");
            }

            delegate void X(ref readonly int p);
            delegate void Y({{modifier}} int p);
            """;

        var source2 = """
            X x = C.X;
            var i = 1;
            x(in i);
            x = (X)C.Y;
            x(in i);
            """;

        if (modifier == "ref")
        {
            CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(
                // 1.cs(4,5): error CS0123: No overload for 'Y' matches delegate 'X'
                // x = (X)C.Y;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(X)C.Y").WithArguments("Y", "X").WithLocation(4, 5));
        }
        else
        {
            CompileAndVerify(new[] { source1, source2 }, expectedOutput: "XY").VerifyDiagnostics(
                // 1.cs(4,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = (X)C.Y;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(X)C.Y").WithArguments("in int p", "ref readonly int p").WithLocation(4, 5));
        }

        var source3 = $"""
            Y y = C.Y;
            var i = 1;
            y({modifier} i);
            y = (Y)C.X;
            y({modifier} i);
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "YX").VerifyDiagnostics(
            // 1.cs(4,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = (Y)C.X;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(Y)C.X").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(4, 5));
    }

    [Theory, CombinatorialData]
    public void Conversion_DelegateConstruction([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            class C
            {
                public static void X(ref readonly int p) => System.Console.Write("X");
                public static void Y({{modifier}} int p) => System.Console.Write("Y");
            }

            delegate void X(ref readonly int p);
            delegate void Y({{modifier}} int p);
            """;

        var source2 = """
            X x = C.X;
            var i = 1;
            x(in i);
            x = new X(C.Y);
            x(in i);
            """;

        if (modifier == "ref")
        {
            CreateCompilation(new[] { source1, source2 }).VerifyDiagnostics(
                // 1.cs(4,5): error CS0123: No overload for 'Y' matches delegate 'X'
                // x = new X(C.Y);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new X(C.Y)").WithArguments("Y", "X").WithLocation(4, 5));
        }
        else
        {
            CompileAndVerify(new[] { source1, source2 }, expectedOutput: "XY").VerifyDiagnostics(
                // 1.cs(4,11): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = new X(C.Y);
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments("in int p", "ref readonly int p").WithLocation(4, 11));
        }

        var source3 = $"""
            Y y = C.Y;
            var i = 1;
            y({modifier} i);
            y = new Y(C.X);
            y({modifier} i);
            """;

        CompileAndVerify(new[] { source1, source3 }, expectedOutput: "YX").VerifyDiagnostics(
            // 1.cs(4,11): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = new Y(C.X);
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.X").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(4, 11));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void Conversion_OverloadResolution_01([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M(ref readonly int x) => System.Console.Write("C");
                void Run()
                {
                    D1 m1 = this.M;
                    D2 m2 = this.M;

                    var i = 1;
                    m1(in i);
                    m2({{(modifier == "ref readonly" ? "in" : modifier)}} i);
                }
                static void Main() => new C().Run();
            }
            static class E
            {
                public static void M(this C c, {{modifier}} int x) => System.Console.Write("E");
            }
            delegate void D1(ref readonly int x);
            delegate void D2({{modifier}} int x);
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "CC");
        if (modifier != "ref readonly")
        {
            verifier.VerifyDiagnostics(
                // (7,17): warning CS9198: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int x' in target.
                //         D2 m2 = this.M;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "this.M").WithArguments("ref readonly int x", $"{modifier} int x").WithLocation(7, 17));
        }
        else
        {
            verifier.VerifyDiagnostics();
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void Conversion_OverloadResolution_02([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
    {
        var source = $$"""
            class C
            {
                void M({{modifier}} int x) => System.Console.Write("C");
                void Run()
                {
                    D1 m1 = this.M;
                    D2 m2 = this.M;

                    var i = 1;
                    m1({{(modifier == "ref readonly" ? "in" : modifier)}} i);
                    m2(in i);
                }
                static void Main() => new C().Run();
            }
            static class E
            {
                public static void M(this C c, ref readonly int x) => System.Console.Write("E");
            }
            delegate void D1({{modifier}} int x);
            delegate void D2(ref readonly int x);
            """;
        var verifier = CompileAndVerify(source,
            expectedOutput: modifier == "ref" ? "CE" : "CC");
        if (modifier == "in")
        {
            verifier.VerifyDiagnostics(
                // (7,17): warning CS9198: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int x' in target.
                //         D2 m2 = this.M;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "this.M").WithArguments("in int x", "ref readonly int x").WithLocation(7, 17));
        }
        else
        {
            verifier.VerifyDiagnostics();
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void Conversion_OverloadResolution_03()
    {
        static string getSource(string body) => $$"""
            class C
            {
                void M(I1 o, ref readonly int x) => System.Console.Write("1");
                void M(I2 o, ref int x) => System.Console.Write("2");
                void Run()
                {
                    var i = 1;
                    {{body}}
                }
                static void Main() => new C().Run();
            }
            interface I1 { }
            interface I2 { }
            class X : I1, I2 { }
            delegate void D1(X s, ref readonly int x);
            delegate void D2(X s, ref int x);
            """;

        var source1 = getSource("""
            D1 m1 = this.M;
            m1(null, in i);
            """);

        CompileAndVerify(source1, expectedOutput: "1").VerifyDiagnostics();

        var source2 = getSource("""
            D2 m2 = this.M;
            m2(null, ref i);
            """);

        CreateCompilation(source2).VerifyDiagnostics(
            // (8,17): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1, ref readonly int)' and 'C.M(I2, ref int)'
            //         D2 m2 = this.M;
            Diagnostic(ErrorCode.ERR_AmbigCall, "this.M").WithArguments("C.M(I1, ref readonly int)", "C.M(I2, ref int)").WithLocation(8, 17));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69229")]
    public void Conversion_OverloadResolution_04()
    {
        static string getSource(string body) => $$"""
            class C
            {
                void M(I1 o, in int x) => System.Console.Write("1");
                void M(I2 o, ref int x) => System.Console.Write("2");
                void Run()
                {
                    var i = 1;
                    {{body}}
                }
                static void Main() => new C().Run();
            }
            interface I1 { }
            interface I2 { }
            class X : I1, I2 { }
            delegate void D1(X s, in int x);
            delegate void D2(X s, ref int x);
            """;

        var source1 = getSource("""
            D1 m1 = this.M;
            D2 m2 = this.M;
            m1(null, in i);
            m2(null, ref i);
            """);

        CompileAndVerify(source1, expectedOutput: "12", parseOptions: TestOptions.Regular11).VerifyDiagnostics();

        var source2 = getSource("""
            D1 m1 = this.M;
            m1(null, in i);
            """);

        var expectedOutput = "1";

        CompileAndVerify(source2, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        CompileAndVerify(source2, expectedOutput: expectedOutput).VerifyDiagnostics();

        var source3 = getSource("""
            D2 m2 = this.M;
            m2(null, ref i);
            """);

        var expectedDiagnostics = new[]
        {
            // (8,17): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1, in int)' and 'C.M(I2, ref int)'
            //         D2 m2 = this.M;
            Diagnostic(ErrorCode.ERR_AmbigCall, "this.M").WithArguments("C.M(I1, in int)", "C.M(I2, ref int)").WithLocation(8, 17)
        };

        CreateCompilation(source3, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source3).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void Conversion_LangVersion([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            public class C
            {
                public static void X(ref readonly int p) => System.Console.Write("X");
                public static void Y({{modifier}} int p) => System.Console.Write("Y");
            }

            public delegate void X(ref readonly int p);
            public delegate void Y({{modifier}} int p);
            """;
        var comp1 = CreateCompilation(source1).VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        DiagnosticDescription[] expectedDiagnostics;
        string expectedOutput;

        var source2 = """
            X x = C.X;
            var i = 1;
            x(in i);
            x = C.Y;
            x(in i);
            """;

        if (modifier == "ref")
        {
            CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (3,6): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                // x(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 6),
                // (4,7): error CS0123: No overload for 'Y' matches delegate 'X'
                // x = C.Y;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y").WithArguments("Y", "X").WithLocation(4, 7),
                // (5,6): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                // x(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(5, 6));

            expectedDiagnostics = new[]
            {
                // (4,7): error CS0123: No overload for 'Y' matches delegate 'X'
                // x = C.Y;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y").WithArguments("Y", "X").WithLocation(4, 7)
            };

            CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source2, new[] { comp1Ref }).VerifyDiagnostics(expectedDiagnostics);
        }
        else
        {
            CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (3,6): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                // x(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 6),
                // (4,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = C.Y;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments("in int p", "ref readonly int p").WithLocation(4, 5),
                // (5,6): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                // x(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(5, 6));

            expectedDiagnostics = new[]
            {
                // (4,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = C.Y;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments("in int p", "ref readonly int p").WithLocation(4, 5)
            };

            expectedOutput = "XY";

            CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
        }

        var source3 = $"""
            Y y = C.Y;
            var i = 1;
            y({modifier} i);
            y = C.X;
            y({modifier} i);
            """;

        CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (4,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = C.X;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.X").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(4, 5));

        expectedDiagnostics = new[]
        {
            // (4,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = C.X;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.X").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(4, 5)
        };

        expectedOutput = "YX";

        CompileAndVerify(source3, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source3, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_LangVersion_RefIn()
    {
        var source1 = """
            public class C
            {
                public static void X(ref int p) => System.Console.Write("X");
                public static void Y(in int p) => System.Console.Write("Y");
            }

            public delegate void X(ref int p);
            public delegate void Y(in int p);
            """;
        var comp1 = CreateCompilation(source1).VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            X x = C.X;
            var i = 1;
            x(ref i);
            x = C.Y;
            x(ref i);
            """;

        var source3 = """
            Y y = C.Y;
            var j = 1;
            y(in j);
            y = C.X;
            y(in j);
            """;

        CreateCompilation(source2 + Environment.NewLine + source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (4,7): error CS0123: No overload for 'Y' matches delegate 'X'
            // x = C.Y;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Y").WithArguments("Y", "X").WithLocation(4, 7),
            // (9,7): error CS0123: No overload for 'X' matches delegate 'Y'
            // y = C.X;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "X").WithArguments("X", "Y").WithLocation(9, 7));

        var expectedDiagnostics = new[]
        {
            // (4,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref int p' in target.
            // x = C.Y;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "C.Y").WithArguments("in int p", "ref int p").WithLocation(4, 5)
        };

        var expectedOutput = "XY";

        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        expectedDiagnostics = new[]
        {
            // (4,7): error CS0123: No overload for 'X' matches delegate 'Y'
            // y = C.X;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "X").WithArguments("X", "Y").WithLocation(4, 7)
        };

        CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source3, new[] { comp1Ref }).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_FunctionPointer_Assignment()
    {
        var source = """
            unsafe
            {
                delegate*<int, void> v = null;
                delegate*<ref readonly int, void> rr = null;
                delegate*<in int, void> i = null;
                delegate*<ref int, void> r = null;
                delegate*<out int, void> o = null;

                v = rr;
                i = rr;
                r = rr;
                o = rr;
                r = i;
                i = r;
                rr = r;
                rr = i;
                rr = o;
                rr = v;
                delegate*<ref readonly int, void> rr2 = rr;
            }
            """;
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // 0.cs(9,9): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<int, void>'. An explicit conversion exists (are you missing a cast?)
            //     v = rr;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "rr").WithArguments("delegate*<ref readonly int, void>", "delegate*<int, void>").WithLocation(9, 9),
            // 0.cs(10,9): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //     i = rr;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "rr").WithArguments("delegate*<ref readonly int, void>", "delegate*<in int, void>").WithLocation(10, 9),
            // 0.cs(11,9): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<ref int, void>'. An explicit conversion exists (are you missing a cast?)
            //     r = rr;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "rr").WithArguments("delegate*<ref readonly int, void>", "delegate*<ref int, void>").WithLocation(11, 9),
            // 0.cs(12,9): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<out int, void>'. An explicit conversion exists (are you missing a cast?)
            //     o = rr;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "rr").WithArguments("delegate*<ref readonly int, void>", "delegate*<out int, void>").WithLocation(12, 9),
            // 0.cs(13,9): error CS0266: Cannot implicitly convert type 'delegate*<in int, void>' to 'delegate*<ref int, void>'. An explicit conversion exists (are you missing a cast?)
            //     r = i;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("delegate*<in int, void>", "delegate*<ref int, void>").WithLocation(13, 9),
            // 0.cs(14,9): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //     i = r;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "r").WithArguments("delegate*<ref int, void>", "delegate*<in int, void>").WithLocation(14, 9),
            // 0.cs(15,10): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     rr = r;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "r").WithArguments("delegate*<ref int, void>", "delegate*<ref readonly int, void>").WithLocation(15, 10),
            // 0.cs(16,10): error CS0266: Cannot implicitly convert type 'delegate*<in int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     rr = i;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("delegate*<in int, void>", "delegate*<ref readonly int, void>").WithLocation(16, 10),
            // 0.cs(17,10): error CS0266: Cannot implicitly convert type 'delegate*<out int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     rr = o;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "o").WithArguments("delegate*<out int, void>", "delegate*<ref readonly int, void>").WithLocation(17, 10),
            // 0.cs(18,10): error CS0266: Cannot implicitly convert type 'delegate*<int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     rr = v;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v").WithArguments("delegate*<int, void>", "delegate*<ref readonly int, void>").WithLocation(18, 10));
    }

    [Fact]
    public void Conversion_FunctionPointer_Assignment_Cast()
    {
        var source = """
            unsafe
            {
                delegate*<int, void> v = &V;
                delegate*<ref readonly int, void> rr = &RR;
                delegate*<in int, void> i = &I;
                delegate*<ref int, void> r = &R;
                delegate*<out int, void> o = &O;

                var x = 1;

                rr = (delegate*<ref readonly int, void>)r; rr(in x);
                rr = (delegate*<ref readonly int, void>)i; rr(in x);
                rr = (delegate*<ref readonly int, void>)o; rr(in x);
                rr = (delegate*<ref readonly int, void>)v; rr(in x);
                r = (delegate*<ref int, void>)i; r(ref x);
                i = (delegate*<in int, void>)r; i(in x);
                v = (delegate*<int, void>)rr; v(x);
                i = (delegate*<in int, void>)rr; i(in x);
                r = (delegate*<ref int, void>)rr; r(ref x);
                o = (delegate*<out int, void>)rr; o(out x);
                delegate*<ref readonly int, void> rr2 = (delegate*<ref readonly int, void>)rr; rr2(in x);
            }

            static void V(int p) => System.Console.Write("v ");
            static void RR(ref readonly int p) => System.Console.Write("rr ");
            static void I(in int p) => System.Console.Write("i ");
            static void R(ref int p) => System.Console.Write("r ");
            static void O(out int p) { p = 0; System.Console.Write("o "); }
            """;
        CompileAndVerify(new[] { source, RequiresLocationAttributeDefinition }, verify: Verification.Fails,
            expectedOutput: "r i o v i i v v v v v", options: TestOptions.UnsafeDebugExe).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Conversion_FunctionPointer_Assignment_LangVersion([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            public unsafe class C
            {
                public static delegate*<ref readonly int, void> X1 = &x1;
                public static delegate*<{{modifier}} int, void> Y1 = &y1;

                public static delegate*<ref readonly int, void> X2 = &x2;
                public static delegate*<{{modifier}} int, void> Y2 = &y2;

                static void x1(ref readonly int p) => System.Console.Write("X1 ");
                static void y1({{modifier}} int p) => System.Console.Write("Y1 ");

                static void x2(ref readonly int p) => System.Console.Write("X2 ");
                static void y2({{modifier}} int p) => System.Console.Write("Y2 ");
            }
            """;
        var comp1 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = $$"""
            unsafe
            {
                var i = 1;
                C.X2(in i);
                C.Y2({{modifier}} i);

                C.X2 = C.X1;
                C.Y2 = C.Y1;

                C.X2(in i);
                C.Y2({{modifier}} i);

                C.Y2 = C.X1;
                C.X2 = C.Y1;

                C.X2(in i);
                C.Y2({{modifier}} i);
            }
            """;

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // (4,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     C.X2(in i);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(4, 13),
            // (10,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     C.X2(in i);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(10, 13),
            // (13,12): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<ref int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.Y2 = C.X1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.X1").WithArguments("delegate*<ref readonly int, void>", $"delegate*<{modifier} int, void>").WithLocation(13, 12),
            // (14,12): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.X2 = C.Y1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.Y1").WithArguments($"delegate*<{modifier} int, void>", "delegate*<ref readonly int, void>").WithLocation(14, 12),
            // (16,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            //     C.X2(in i);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(16, 13));

        var expectedDiagnostics = new[]
        {
            // (13,12): error CS0266: Cannot implicitly convert type 'delegate*<ref readonly int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.Y2 = C.X1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.X1").WithArguments("delegate*<ref readonly int, void>", $"delegate*<{modifier} int, void>").WithLocation(13, 12),
            // (14,12): error CS0266: Cannot implicitly convert type 'delegate*<in int, void>' to 'delegate*<ref readonly int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.X2 = C.Y1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.Y1").WithArguments($"delegate*<{modifier} int, void>", "delegate*<ref readonly int, void>").WithLocation(14, 12)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source2, new[] { comp1Ref }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_FunctionPointer_Assignment_LangVersion_RefIn()
    {
        var source1 = """
            public unsafe class C
            {
                public static delegate*<ref int, void> X1 = &x1;
                public static delegate*<in int, void> Y1 = &y1;

                public static delegate*<ref int, void> X2 = &x2;
                public static delegate*<in int, void> Y2 = &y2;

                static void x1(ref int p) => System.Console.Write("X1 ");
                static void y1(in int p) => System.Console.Write("Y1 ");

                static void x2(ref int p) => System.Console.Write("X2 ");
                static void y2(in int p) => System.Console.Write("Y2 ");
            }
            """;
        var comp1 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            unsafe
            {
                var i = 1;
                C.X2(ref i);
                C.Y2(in i);

                C.X2 = C.X1;
                C.Y2 = C.Y1;

                C.X2(ref i);
                C.Y2(in i);

                C.Y2 = C.X1;
                C.X2 = C.Y1;

                C.X2(ref i);
                C.Y2(in i);
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (13,12): error CS0266: Cannot implicitly convert type 'delegate*<ref int, void>' to 'delegate*<in int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.Y2 = C.X1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.X1").WithArguments("delegate*<ref int, void>", "delegate*<in int, void>").WithLocation(13, 12),
            // (14,12): error CS0266: Cannot implicitly convert type 'delegate*<in int, void>' to 'delegate*<ref int, void>'. An explicit conversion exists (are you missing a cast?)
            //     C.X2 = C.Y1;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.Y1").WithArguments("delegate*<in int, void>", "delegate*<ref int, void>").WithLocation(14, 12)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics); CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source2, new[] { comp1Ref }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_FunctionPointer_MethodGroup()
    {
        var source = """
            unsafe
            {
                delegate*<int, void> v = &V;
                delegate*<ref readonly int, void> rr = &RR;
                delegate*<in int, void> i = &I;
                delegate*<ref int, void> r = &R;
                delegate*<out int, void> o = &O;

                rr = &V;
                rr = &R;
                rr = &I;
                rr = &O;
                v = &RR;
                i = &RR;
                r = &RR;
                o = &RR;
                r = &I;
                i = &R;

                static void V(int p) => throw null;
                static void RR(ref readonly int p) => throw null;
                static void I(in int p) => throw null;
                static void R(ref int p) => throw null;
                static void O(out int p) => throw null;
            }
            """;
        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // 0.cs(9,10): error CS8757: No overload for 'V' matches function pointer 'delegate*<ref readonly int, void>'
            //     rr = &V;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&V").WithArguments("V", "delegate*<ref readonly int, void>").WithLocation(9, 10),
            // 0.cs(10,10): error CS8757: No overload for 'R' matches function pointer 'delegate*<ref readonly int, void>'
            //     rr = &R;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&R").WithArguments("R", "delegate*<ref readonly int, void>").WithLocation(10, 10),
            // 0.cs(11,10): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int' in target.
            //     rr = &I;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&I").WithArguments("in int p", "ref readonly int").WithLocation(11, 10),
            // 0.cs(12,10): error CS8757: No overload for 'O' matches function pointer 'delegate*<ref readonly int, void>'
            //     rr = &O;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&O").WithArguments("O", "delegate*<ref readonly int, void>").WithLocation(12, 10),
            // 0.cs(13,9): error CS8757: No overload for 'RR' matches function pointer 'delegate*<int, void>'
            //     v = &RR;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&RR").WithArguments("RR", "delegate*<int, void>").WithLocation(13, 9),
            // 0.cs(14,9): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int' in target.
            //     i = &RR;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&RR").WithArguments("ref readonly int p", "in int").WithLocation(14, 9),
            // 0.cs(15,9): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'ref int' in target.
            //     r = &RR;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&RR").WithArguments("ref readonly int p", "ref int").WithLocation(15, 9),
            // 0.cs(16,9): error CS8757: No overload for 'RR' matches function pointer 'delegate*<out int, void>'
            //     o = &RR;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&RR").WithArguments("RR", "delegate*<out int, void>").WithLocation(16, 9),
            // 0.cs(17,9): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref int' in target.
            //     r = &I;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&I").WithArguments("in int p", "ref int").WithLocation(17, 9),
            // 0.cs(18,9): error CS8757: No overload for 'R' matches function pointer 'delegate*<in int, void>'
            //     i = &R;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&R").WithArguments("R", "delegate*<in int, void>").WithLocation(18, 9));
    }

    [Theory, CombinatorialData]
    public void Conversion_FunctionPointer_MethodGroup_Cast([CombinatorialValues("in", "ref")] string modifier)
    {
        string getSource(string body) => $$"""
            unsafe
            {
                delegate*<ref readonly int, void> x = &X;
                delegate*<{{modifier}} int, void> y = &Y;

                var i = 1;
                x(in i);
                y({{modifier}} i);

                {{body}}

                x(in i);
                y({{modifier}} i);

                static void X(ref readonly int p) => System.Console.Write("X");
                static void Y({{modifier}} int p) => System.Console.Write("Y");
            }
            """;

        var source1 = getSource("x = (delegate*<ref readonly int, void>)&Y;");

        if (modifier == "ref")
        {
            CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
                // 0.cs(10,9): error CS8757: No overload for 'Y' matches function pointer 'delegate*<ref readonly int, void>'
                //     x = (delegate*<ref readonly int, void>)&Y;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "(delegate*<ref readonly int, void>)&Y").WithArguments("Y", "delegate*<ref readonly int, void>").WithLocation(10, 9));
        }
        else
        {
            CompileAndVerify(new[] { source1, RequiresLocationAttributeDefinition }, verify: Verification.Fails,
                expectedOutput: "XYYY", options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
                // 0.cs(10,9): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int' in target.
                //     x = (delegate*<ref readonly int, void>)&Y;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(delegate*<ref readonly int, void>)&Y").WithArguments("in int p", "ref readonly int").WithLocation(10, 9));
        }

        var source2 = getSource($"y = (delegate*<{modifier} int, void>)&X;");

        CompileAndVerify(new[] { source2, RequiresLocationAttributeDefinition }, verify: Verification.Fails,
            expectedOutput: "XYXX", options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // 0.cs(10,9): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int' in target.
            //     y = (delegate*<in int, void>)&X;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, $"(delegate*<{modifier} int, void>)&X").WithArguments("ref readonly int p", $"{modifier} int").WithLocation(10, 9));
    }

    [Theory, CombinatorialData]
    public void Conversion_FunctionPointer_MethodGroup_LangVersion([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            public unsafe class C
            {
                public static void X1(ref readonly int p) => System.Console.Write("X");
                public static void Y1({{modifier}} int p) => System.Console.Write("Y");

                public static delegate*<ref readonly int, void> X2;
                public static delegate*<{{modifier}} int, void> Y2;
            }
            """;
        var comp1 = CreateCompilation(new[] { source1, RequiresLocationAttributeDefinition }, options: TestOptions.UnsafeDebugDll);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        DiagnosticDescription[] expectedDiagnostics;
        string expectedOutput;

        var source2 = """
            unsafe
            {
                C.X2 = &C.X1;
                var i = 1;
                C.X2(in i);
                C.X2 = &C.Y1;
                C.X2(in i);
            }
            """;

        var comp = CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe);
        if (modifier == "ref")
        {
            comp.VerifyDiagnostics(
                // (5,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     C.X2(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(5, 13),
                // (6,12): error CS8757: No overload for 'Y1' matches function pointer 'delegate*<ref readonly int, void>'
                //     C.X2 = &C.Y1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.Y1").WithArguments("Y1", "delegate*<ref readonly int, void>").WithLocation(6, 12),
                // (7,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     C.X2(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(7, 13));

            expectedDiagnostics = new[]
            {
                // (6,12): error CS8757: No overload for 'Y1' matches function pointer 'delegate*<ref readonly int, void>'
                //     C.X2 = &C.Y1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.Y1").WithArguments("Y1", "delegate*<ref readonly int, void>").WithLocation(6, 12)
            };

            CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source2, new[] { comp1Ref }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
        }
        else
        {
            comp.VerifyDiagnostics(
                // (5,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     C.X2(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(5, 13),
                // (6,12): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int' in target.
                //     C.X2 = &C.Y1;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&C.Y1").WithArguments("in int p", "ref readonly int").WithLocation(6, 12),
                // (7,13): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     C.X2(in i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "i").WithArguments("ref readonly parameters", "12.0").WithLocation(7, 13));

            expectedDiagnostics = new[]
            {
                // (6,12): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int' in target.
                //     C.X2 = &C.Y1;
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&C.Y1").WithArguments("in int p", "ref readonly int").WithLocation(6, 12)
            };

            expectedOutput = "XY";

            CompileAndVerify(source2, new[] { comp1Ref }, verify: Verification.Fails, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe,
                expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
            CompileAndVerify(source2, new[] { comp1Ref }, verify: Verification.Fails, options: TestOptions.UnsafeDebugExe,
                expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
        }

        var source3 = $$"""
            unsafe
            {
                C.Y2 = &C.Y1;
                var i = 1;
                C.Y2({{modifier}} i);
                C.Y2 = &C.X1;
                C.Y2({{modifier}} i);
            }
            """;

        CompileAndVerify(source3, new[] { comp1Ref }, verify: Verification.Fails, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe,
            expectedOutput: "YX").VerifyDiagnostics(
            // (6,12): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int' in target.
            //     C.Y2 = &C.X1;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&C.X1").WithArguments("ref readonly int p", $"{modifier} int").WithLocation(6, 12));

        expectedDiagnostics = new[]
        {
            // (6,12): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int' in target.
            //     C.Y2 = &C.X1;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&C.X1").WithArguments("ref readonly int p", $"{modifier} int").WithLocation(6, 12)
        };

        expectedOutput = "YX";

        CompileAndVerify(source3, new[] { comp1Ref }, verify: Verification.Fails, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe,
            expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source3, new[] { comp1Ref }, verify: Verification.Fails, options: TestOptions.UnsafeDebugExe,
            expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_FunctionPointer_MethodGroup_LangVersion_RefIn()
    {
        var source1 = """
            public unsafe class C
            {
                public static void X1(ref int p) => System.Console.Write("X");
                public static void Y1(in int p) => System.Console.Write("Y");

                public static delegate*<ref int, void> X2;
                public static delegate*<in int, void> Y2;
            }
            """;
        var comp1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            unsafe
            {
                C.X2 = &C.X1;
                var i = 1;
                C.X2(ref i);
                C.X2 = &C.Y1;
                C.X2(ref i);
            }
            """;

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // (6,12): error CS8757: No overload for 'Y1' matches function pointer 'delegate*<ref int, void>'
            //     C.X2 = &C.Y1;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.Y1").WithArguments("Y1", "delegate*<ref int, void>").WithLocation(6, 12));

        var expectedDiagnostics = new[]
        {
            // (6,12): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref int' in target.
            //     C.X2 = &C.Y1;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "&C.Y1").WithArguments("in int p", "ref int").WithLocation(6, 12)
        };

        var expectedOutput = "XY";

        CompileAndVerify(source2, new[] { comp1Ref }, verify: Verification.Fails, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe,
            expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source2, new[] { comp1Ref }, verify: Verification.Fails, options: TestOptions.UnsafeDebugExe,
            expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        var source3 = """
            unsafe
            {
                C.Y2 = &C.Y1;
                var i = 1;
                C.Y2(in i);
                C.Y2 = &C.X1;
                C.Y2(in i);
            }
            """;

        CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular11, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
            // (6,12): error CS8757: No overload for 'X1' matches function pointer 'delegate*<in int, void>'
            //     C.Y2 = &C.X1;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.X1").WithArguments("X1", "delegate*<in int, void>").WithLocation(6, 12));

        expectedDiagnostics = new[]
        {
            // (6,12): error CS8757: No overload for 'X1' matches function pointer 'delegate*<in int, void>'
            //     C.Y2 = &C.X1;
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.X1").WithArguments("X1", "delegate*<in int, void>").WithLocation(6, 12)
        };

        CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source3, new[] { comp1Ref }, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Lambda()
    {
        var source = """
            V v = (int x) => throw null;
            RR rr = (ref readonly int x) => throw null;
            I i = (in int x) => throw null;
            R r = (ref int x) => throw null;
            O o = (out int x) => throw null;

            rr = (int x) => throw null;
            rr = (ref int x) => throw null;
            rr = (in int x) => throw null;
            rr = (out int x) => throw null;
            v = (ref readonly int x) => throw null;
            i = (ref readonly int x) => throw null;
            r = (ref readonly int x) => throw null;
            o = (ref readonly int x) => throw null;
            r = (in int x) => throw null;
            i = (ref int x) => throw null;

            rr = (int x) => throw null;
            rr = x => throw null;

            delegate void V(int p);
            delegate void RR(ref readonly int p);
            delegate void I(in int p);
            delegate void R(ref int p);
            delegate void O(out int p);
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,11): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // rr = (int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(7, 11),
            // (7,14): error CS1661: Cannot convert lambda expression to type 'RR' because the parameter types do not match the delegate parameter types
            // rr = (int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "RR").WithLocation(7, 14),
            // (8,15): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // rr = (ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(8, 15),
            // (8,18): error CS1661: Cannot convert lambda expression to type 'RR' because the parameter types do not match the delegate parameter types
            // rr = (ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "RR").WithLocation(8, 18),
            // (9,6): warning CS9198: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref readonly int p' in target.
            // rr = (in int x) => throw null;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(in int x) => throw null").WithArguments("in int x", "ref readonly int p").WithLocation(9, 6),
            // (10,15): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // rr = (out int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(10, 15),
            // (10,18): error CS1661: Cannot convert lambda expression to type 'RR' because the parameter types do not match the delegate parameter types
            // rr = (out int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "RR").WithLocation(10, 18),
            // (11,23): error CS1677: Parameter 1 should not be declared with the 'ref readonly' keyword
            // v = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamExtraRef, "x").WithArguments("1", "ref readonly").WithLocation(11, 23),
            // (11,26): error CS1661: Cannot convert lambda expression to type 'V' because the parameter types do not match the delegate parameter types
            // v = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "V").WithLocation(11, 26),
            // (12,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'in int p' in target.
            // i = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(ref readonly int x) => throw null").WithArguments("ref readonly int x", "in int p").WithLocation(12, 5),
            // (13,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'ref int p' in target.
            // r = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(ref readonly int x) => throw null").WithArguments("ref readonly int x", "ref int p").WithLocation(13, 5),
            // (14,23): error CS1676: Parameter 1 must be declared with the 'out' keyword
            // o = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "out").WithLocation(14, 23),
            // (14,26): error CS1661: Cannot convert lambda expression to type 'O' because the parameter types do not match the delegate parameter types
            // o = (ref readonly int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "O").WithLocation(14, 26),
            // (15,5): warning CS9198: Reference kind modifier of parameter 'in int x' doesn't match the corresponding parameter 'ref int p' in target.
            // r = (in int x) => throw null;
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(in int x) => throw null").WithArguments("in int x", "ref int p").WithLocation(15, 5),
            // (16,14): error CS1676: Parameter 1 must be declared with the 'in' keyword
            // i = (ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "in").WithLocation(16, 14),
            // (16,17): error CS1661: Cannot convert lambda expression to type 'I' because the parameter types do not match the delegate parameter types
            // i = (ref int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "I").WithLocation(16, 17),
            // (18,11): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // rr = (int x) => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(18, 11),
            // (18,14): error CS1661: Cannot convert lambda expression to type 'RR' because the parameter types do not match the delegate parameter types
            // rr = (int x) => throw null;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "RR").WithLocation(18, 14),
            // (19,6): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
            // rr = x => throw null;
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(19, 6));
    }

    [Theory, CombinatorialData]
    public void Conversion_Lambda_Cast([CombinatorialValues("in", "ref")] string modifier)
    {
        string getSource(string body) => $$"""
            X x = (X)((ref readonly int p) => System.Console.Write("1"));
            Y y = (Y)(({{modifier}} int p) => System.Console.Write("2"));

            var i = 1;
            x(in i);
            y({{modifier}} i);

            {{body}}

            x(in i);
            y({{modifier}} i);

            delegate void X(ref readonly int p);
            delegate void Y({{modifier}} int p);
            """;

        var source1 = getSource($"""x = (X)(({modifier} int p) => System.Console.Write("3"));""");

        if (modifier == "ref")
        {
            CreateCompilation(source1).VerifyDiagnostics(
                // (8,18): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
                // x = (X)((ref int p) => System.Console.Write("3"));
                Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "ref readonly").WithLocation(8, 18),
                // (8,21): error CS1661: Cannot convert lambda expression to type 'X' because the parameter types do not match the delegate parameter types
                // x = (X)((ref int p) => System.Console.Write("3"));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "X").WithLocation(8, 21));
        }
        else
        {
            CompileAndVerify(source1, expectedOutput: "1232").VerifyDiagnostics(
                // (8,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // x = (X)((in int p) => System.Console.Write("3"));
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(X)((in int p) => System.Console.Write(""3""))").WithArguments("in int p", "ref readonly int p").WithLocation(8, 5));
        }

        var source2 = getSource("""y = (Y)((ref readonly int p) => System.Console.Write("4"));""");

        CompileAndVerify(source2, expectedOutput: "1214").VerifyDiagnostics(
            // (8,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // y = (Y)((ref readonly int p) => System.Console.Write("4"));
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(Y)((ref readonly int p) => System.Console.Write(""4""))").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(8, 5));
    }

    [Theory, CombinatorialData]
    public void Conversion_Lambda_LangVersion([CombinatorialValues("in", "ref")] string modifier)
    {
        var source1 = $$"""
            public class C
            {
                public static void X(X x) { System.Console.Write("X"); int i = 1; x(in i); }
                public static void Y(Y y) { System.Console.Write("Y"); int i = 1; y({{modifier}} i); }
            }

            public delegate void X(ref readonly int p);
            public delegate void Y({{modifier}} int p);
            """;
        var comp1 = CreateCompilation(source1);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = $$"""
            C.X((ref readonly int p) => System.Console.Write("1"));
            C.Y(({{modifier}} int p) => System.Console.Write("2"));
            C.Y((ref readonly int p) => System.Console.Write("3"));
            """;

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,10): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            // C.X((ref readonly int p) => System.Console.Write("1"));
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(1, 10),
            // (3,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // C.Y((ref readonly int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(ref readonly int p) => System.Console.Write(""3"")").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(3, 5),
            // (3,10): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
            // C.Y((ref readonly int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 10));

        var expectedDiagnostics = new[]
        {
            // (3,5): warning CS9198: Reference kind modifier of parameter 'ref readonly int p' doesn't match the corresponding parameter 'in int p' in target.
            // C.Y((ref readonly int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(ref readonly int p) => System.Console.Write(""3"")").WithArguments("ref readonly int p", $"{modifier} int p").WithLocation(3, 5)
        };

        var expectedOutput = "X1Y2Y3";

        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        var source3 = $$"""
            C.X(({{modifier}} int p) => System.Console.Write("4"));
            """;

        if (modifier == "ref")
        {
            CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (1,14): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
                // C.X((ref int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "ref readonly").WithLocation(1, 14),
                // (1,17): error CS1661: Cannot convert lambda expression to type 'X' because the parameter types do not match the delegate parameter types
                // C.X((ref int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "X").WithLocation(1, 17));

            expectedDiagnostics = new[]
            {
                // (1,14): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
                // C.X((ref int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "ref readonly").WithLocation(1, 14),
                // (1,17): error CS1661: Cannot convert lambda expression to type 'X' because the parameter types do not match the delegate parameter types
                // C.X((ref int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "X").WithLocation(1, 17)
            };

            CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source3, new[] { comp1Ref }).VerifyDiagnostics(expectedDiagnostics);
        }
        else
        {
            expectedOutput = "X4";

            CompileAndVerify(source3, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (1,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // C.X((in int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(in int p) => System.Console.Write(""4"")").WithArguments("in int p", "ref readonly int p").WithLocation(1, 5));

            expectedDiagnostics = new[]
            {
                // (1,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref readonly int p' in target.
                // C.X((in int p) => System.Console.Write("4"));
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(in int p) => System.Console.Write(""4"")").WithArguments("in int p", "ref readonly int p").WithLocation(1, 5)
            };

            CompileAndVerify(source3, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CompileAndVerify(source3, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
        }
    }

    [Fact]
    public void Conversion_Lambda_LangVersion_RefIn()
    {
        var source1 = """
            public class C
            {
                public static void X(X x) { System.Console.Write("X"); int i = 1; x(ref i); }
                public static void Y(Y y) { System.Console.Write("Y"); int i = 1; y(in i); }
            }

            public delegate void X(ref int p);
            public delegate void Y(in int p);
            """;
        var comp1 = CreateCompilation(source1);
        comp1.VerifyDiagnostics();
        var comp1Ref = comp1.ToMetadataReference();

        var source2 = """
            C.X((ref int p) => System.Console.Write("1"));
            C.Y((in int p) => System.Console.Write("2"));

            C.X((in int p) => System.Console.Write("3"));
            """;

        var source3 = """
            C.Y((ref int p) => System.Console.Write("4"));
            """;

        CreateCompilation(source2 + Environment.NewLine + source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (4,13): error CS1676: Parameter 1 must be declared with the 'ref' keyword
            // C.X((in int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "ref").WithLocation(4, 13),
            // (4,16): error CS1661: Cannot convert lambda expression to type 'X' because the parameter types do not match the delegate parameter types
            // C.X((in int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "X").WithLocation(4, 16),
            // (5,14): error CS1676: Parameter 1 must be declared with the 'in' keyword
            // C.Y((ref int p) => System.Console.Write("4"));
            Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "in").WithLocation(5, 14),
            // (5,17): error CS1661: Cannot convert lambda expression to type 'Y' because the parameter types do not match the delegate parameter types
            // C.Y((ref int p) => System.Console.Write("4"));
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "Y").WithLocation(5, 17));

        var expectedDiagnostics = new[]
        {
            // (4,5): warning CS9198: Reference kind modifier of parameter 'in int p' doesn't match the corresponding parameter 'ref int p' in target.
            // C.X((in int p) => System.Console.Write("3"));
            Diagnostic(ErrorCode.WRN_TargetDifferentRefness, @"(in int p) => System.Console.Write(""3"")").WithArguments("in int p", "ref int p").WithLocation(4, 5)
        };

        var expectedOutput = "X1Y2X3";

        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CompileAndVerify(source2, new[] { comp1Ref }, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        expectedDiagnostics = new[]
        {
            // (1,14): error CS1676: Parameter 1 must be declared with the 'in' keyword
            // C.Y((ref int p) => System.Console.Write("4"));
            Diagnostic(ErrorCode.ERR_BadParamRef, "p").WithArguments("1", "in").WithLocation(1, 14),
            // (1,17): error CS1661: Cannot convert lambda expression to type 'Y' because the parameter types do not match the delegate parameter types
            // C.Y((ref int p) => System.Console.Write("4"));
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "Y").WithLocation(1, 17)
        };

        CreateCompilation(source3, new[] { comp1Ref }, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source3, new[] { comp1Ref }).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71383")]
    public void Conversion_ReadonlySemantics()
    {
        var source = """
            class Program
            {
                static readonly int f = 123;
                static void Main()
                {
                    var d = (in int x) => { };
                    d = (ref int x) => { x = 42; }; // should be an error
                    d(f);
                    System.Console.WriteLine(f);
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,22): error CS1676: Parameter 1 must be declared with the 'in' keyword
            //         d = (ref int x) => { x = 42; }; // should be an error
            Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "in").WithLocation(7, 22),
            // (7,25): error CS1661: Cannot convert lambda expression to type '<anonymous delegate>' because the parameter types do not match the delegate parameter types
            //         d = (ref int x) => { x = 42; }; // should be an error
            Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "<anonymous delegate>").WithLocation(7, 25));
    }
}
