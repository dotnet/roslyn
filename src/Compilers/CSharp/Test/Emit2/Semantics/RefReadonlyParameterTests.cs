// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
        bool useSiteError = false)
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

        var method = (MethodSymbol)parameter.ContainingSymbol;

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
        var source = """
            class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(new[] { source, RequiresLocationAttributeDefinition },
            sourceSymbolValidator: verify, symbolValidator: verify);
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
            }
            """;

        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // 0.cs(4,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M1([RequiresLocation] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(4, 14),
            // 0.cs(4,36): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M1([RequiresLocation] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 36),
            // 0.cs(5,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M2([RequiresLocation] in int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(5, 14),
            // 0.cs(6,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M3([RequiresLocation] ref int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(6, 14),
            // 0.cs(7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.RequiresLocationAttribute'. This is reserved for compiler usage.
            //     void M4([RequiresLocation] int p) { }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 14));

        var expectedDiagnostics = new[]
        {
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
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RequiresLocation").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(7, 14)
        };

        CreateCompilation(new[] { source, RequiresLocationAttributeDefinition }, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (4,36): error CS9520: A ref readonly parameter cannot have the Out attribute.
            //     void M1([Out] ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_OutAttrOnRefReadonlyParam, "p").WithLocation(4, 36),
            // (5,40): error CS9520: A ref readonly parameter cannot have the Out attribute.
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
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
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
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
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
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol m)
        {
            VerifyRequiresLocationAttributeSynthesized(m);

            if (m is not SourceModuleSymbol)
            {
                var p = m.GlobalNamespace.GetMember<MethodSymbol>("<>f__AnonymousDelegate0.Invoke").Parameters.Single();
                VerifyRefReadonlyParameter(p,
                    // PROTOTYPE: Invoke method is virtual but no modreq is emitted. This happens for `in` parameters, as well.
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

    /// <summary>
    /// Demonstrates that modopt encoding of 'ref readonly' parameters in function pointers
    /// won't break older compilers (they will see the parameter as 'in').
    /// </summary>
    [Fact]
    public void FunctionPointer_Modopt_CustomAttribute()
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
            // (7,17): error CS9505: Argument 1 may not be passed with the 'ref' keyword in language version 11.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version preview or greater.
            //         c.D(ref x);
            Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "x").WithArguments("1", "11.0", "preview").WithLocation(7, 17));

        var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (7,17): warning CS9502: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
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
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_RequiresLocationAttribute);
        comp.VerifyDiagnostics(
            // (3,36): error CS0518: Predefined type 'System.Runtime.CompilerServices.RequiresLocationAttribute' is not defined or imported
            //     public unsafe void M(delegate*<ref readonly int, void> p) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.CompilerServices.RequiresLocationAttribute").WithLocation(3, 36));
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
            // (6,13): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         c.D(x);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("ref readonly parameters").WithLocation(6, 13),
            // (8,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         c.D(in x);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("ref readonly parameters").WithLocation(8, 16));

        var expectedDiagnostics = new[]
        {
            // (6,13): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.D(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(6, 13)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.RegularNext, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly int p);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (3,23): error CS9501: 'readonly' modifier must be specified after 'ref'.
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
            // (3,15): error CS9501: 'readonly' modifier must be specified after 'ref'.
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,25): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
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

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,15): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(in readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 15)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(out readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 16)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,31): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 31)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,35): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(this ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 35));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (3,35): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 35)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly this int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (3,37): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(scoped ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 37));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (3,37): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 37)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,26): error CS9501: 'readonly' modifier must be specified after 'ref'.
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
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,33): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 33)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly int scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));
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
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));
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
            // (7,11): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (7,11): warning CS9506: Argument 1 should be passed with the 'in' keyword
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
            // (6,11): warning CS9504: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
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
            // (5,12): warning CS9506: Argument 1 should be passed with the 'in' keyword
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
            // (5,12): warning CS9506: Argument 1 should be passed with the 'in' keyword
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
            // (7,12): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (7,12): warning CS9506: Argument 1 should be passed with the 'in' keyword
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
            // (7,11): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (7,11): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (6,13): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         c.M(x);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("ref readonly parameters").WithLocation(6, 13),
            // (8,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         c.M(in x);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("ref readonly parameters").WithLocation(8, 16));

        var expectedDiagnostics = new[]
        {
            // (6,13): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
            //         c.M(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(6, 13)
        };

        CreateCompilation(source2, new[] { comp1Ref }, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (7,15): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (16,15): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (8,11): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (12,14): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(b: x, a: y); // 1
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(12, 14),
            // (13,14): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
            //         M(b: x, a: in y); // 2
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(13, 14),
            // (14,17): warning CS9503: Argument 2 should be passed with 'ref' or 'in' keyword
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
            // (10,58): warning CS9502: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
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
            // (8,36): warning CS9501: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            //         new C(default(object), ref i);
            Diagnostic(ErrorCode.WRN_BadArgRef, "i").WithArguments("2").WithLocation(8, 36)
        };

        CompileAndVerify(source, expectedOutput: "object5", parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,47): warning CS9521: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (7,13): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (4,37): warning CS9521: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (7,13): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (4,67): warning CS9521: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (4,51): warning CS9521: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (4,77): warning CS9521: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (7,38): warning CS9521: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     public static void M1([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] ref readonly decimal d) => Console.WriteLine("M1 " + d.ToString(CultureInfo.InvariantCulture));
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "DecimalConstant(1, 0, 0u, 0u, 11u)").WithArguments("d").WithLocation(7, 38),
            // (8,52): warning CS9521: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (7,14): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
            //         C.M1(x);
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(7, 14),
            // (13,14): warning CS9503: Argument 1 should be passed with 'ref' or 'in' keyword
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
            // (6,92): warning CS9521: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
            // (6,37): warning CS9521: A default value is specified for 'ref readonly' parameter 'd', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
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
}
