// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class ClosedClassesTests : CSharpTestBase
{
    [Fact]
    public void LangVersion()
    {
        var source = """
            closed class C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // closed class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14));

        comp = CreateCompilation([source, ClosedAttributeDefinition], parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void DoesNotSynthesizeAttribute_01()
    {
        var source1 = """
            public closed class C { }
            """;

        var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.ClosedAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.ClosedAttribute", ".ctor").WithLocation(1, 21));

        var comp2 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();
    }

    [Fact]
    public void DoesNotSynthesizeAttribute_02()
    {
        var source1 = """
            public closed class C { }
            """;

        var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Net100);
        comp1.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute);
        comp1.VerifyEmitDiagnostics(
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.ClosedAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.ClosedAttribute", ".ctor").WithLocation(1, 21),
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(1, 21));

        var comp2 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp2.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor);
        comp2.VerifyEmitDiagnostics(
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(1, 21));
    }

    [Fact]
    public void Symbols_01()
    {
        var source = """
            closed class C { }
            """;

        var verifier = CompileAndVerify([source, ClosedAttributeDefinition], symbolValidator: verifySymbols, sourceSymbolValidator: verifySymbols, targetFramework: TargetFramework.Net100, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("C", """
            .class private auto ansi abstract beforefieldinit C
                extends [System.Runtime]System.Object
            {
                .custom instance void System.Runtime.CompilerServices.ClosedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Methods
                .method family hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
                        00 00
                    )
                    // Method begins at RVA 0x2050
                    // Code size 7 (0x7)
                    .maxstack 8
                    IL_0000: ldarg.0
                    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
                    IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        void verifySymbols(ModuleSymbol module)
        {
            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.IsClosed);
            // ClosedAttribute is filtered out of source and metadata symbols.
            Assert.Empty(classC.GetAttributes());

            var ctor = classC.Constructors.Single();
            // CompilerFeatureRequiredAttribute is filtered out
            Assert.Empty(ctor.GetAttributes());
        }
    }

    [Fact]
    public void Symbols_02()
    {
        // All constructors get 'CompilerFeatureRequiredAttribute'
        var source = """
            closed class C
            {
                public C() { }
                public C(int value) { }
            }
            """;

        var verifier = CompileAndVerify([source, ClosedAttributeDefinition], symbolValidator: verifySymbols, sourceSymbolValidator: verifySymbols, targetFramework: TargetFramework.Net100, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("C", """
            .class private auto ansi abstract beforefieldinit C
                extends [System.Runtime]System.Object
            {
                .custom instance void System.Runtime.CompilerServices.ClosedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
                        00 00
                    )
                    // Method begins at RVA 0x2050
                    // Code size 7 (0x7)
                    .maxstack 8
                    IL_0000: ldarg.0
                    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
                    IL_0006: ret
                } // end of method C::.ctor
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        int32 'value'
                    ) cil managed 
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
                        00 00
                    )
                    // Method begins at RVA 0x2050
                    // Code size 7 (0x7)
                    .maxstack 8
                    IL_0000: ldarg.0
                    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
                    IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        void verifySymbols(ModuleSymbol module)
        {
            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.IsClosed);
            // attribute is filtered out of source and metadata symbols.
            Assert.Empty(classC.GetAttributes());
        }
    }

    [Fact]
    public void Sealed_01()
    {
        var source = """
            sealed closed class C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // sealed closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsSealed);
        Assert.True(classC.IsAbstract);
        Assert.True(classC.IsClosed);
    }

    [Fact]
    public void Static_01()
    {
        var source = """
            static closed class C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // static closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsStatic);
        Assert.True(classC.IsAbstract);
        Assert.True(classC.IsClosed);
    }

    [Fact]
    public void Abstract_01()
    {
        var source = """
            abstract closed class C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
        Assert.True(classC.IsClosed);
    }

    [Fact]
    public void ImplicitlyAbstract_01()
    {
        var source1 = """
            public closed class C { }
            """;

        var source2 = """
            new C(); // 1
            """;

        var comp = CreateCompilation([source1, source2, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0144: Cannot create an instance of the abstract type or interface 'C'
            // new C(); // 1
            Diagnostic(ErrorCode.ERR_NoNewAbstract, "new C()").WithArguments("C").WithLocation(1, 1));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
        Assert.True(classC.IsClosed);

        var referenceComp = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verifyReference(referenceComp.ToMetadataReference());
        verifyReference(referenceComp.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var comp2 = CreateCompilation("""
                new C(); // 1
                """, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics(
                // (1,1): error CS0144: Cannot create an instance of the abstract type or interface 'C'
                // new C(); // 1
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new C()").WithArguments("C").WithLocation(1, 1));

            var classC2 = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC2.IsAbstract);
            Assert.True(classC.IsClosed);
        }
    }

    [Fact]
    public void ImplicitlyAbstract_02()
    {
        var source = """
            abstract class Base
            {
                public abstract void M();
            }

            closed class C : Base { }

            class D : C { }
            class E : C
            {
                public override void M() { }
            }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (8,7): error CS0534: 'D' does not implement inherited abstract member 'Base.M()'
            // class D : C { }
            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "Base.M()").WithLocation(8, 7));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void ImplicitlyAbstract_03()
    {
        var source = """
            closed class C
            {
                public abstract void M();
            }
            class D : C { }

            class E : C
            {
                public override void M() { }
            }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,7): error CS0534: 'D' does not implement inherited abstract member 'C.M()'
            // class D : C { }
            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "C.M()").WithLocation(5, 7));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void BadTypeKind_01()
    {
        var source = """
            closed interface I { } // 1
            closed enum E { } // 2
            closed delegate void D(); // 3
            closed struct S { } // 4

            class C
            {
                closed void M() { } // 5
                closed int P { get; set; } // 6
                closed event System.Action E; // 7
                closed string F; // 8
            }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS0106: The modifier 'closed' is not valid for this item
            // closed interface I { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("closed").WithLocation(1, 18),
            // (2,13): error CS0106: The modifier 'closed' is not valid for this item
            // closed enum E { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("closed").WithLocation(2, 13),
            // (3,22): error CS0106: The modifier 'closed' is not valid for this item
            // closed delegate void D(); // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("closed").WithLocation(3, 22),
            // (4,15): error CS0106: The modifier 'closed' is not valid for this item
            // closed struct S { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("closed").WithLocation(4, 15),
            // (8,17): error CS0106: The modifier 'closed' is not valid for this item
            //     closed void M() { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("closed").WithLocation(8, 17),
            // (9,16): error CS0106: The modifier 'closed' is not valid for this item
            //     closed int P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("closed").WithLocation(9, 16),
            // (10,32): error CS0106: The modifier 'closed' is not valid for this item
            //     closed event System.Action E; // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("closed").WithLocation(10, 32),
            // (10,32): warning CS0067: The event 'C.E' is never used
            //     closed event System.Action E; // 7
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(10, 32),
            // (11,19): error CS0106: The modifier 'closed' is not valid for this item
            //     closed string F; // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "F").WithArguments("closed").WithLocation(11, 19),
            // (11,19): warning CS0169: The field 'C.F' is never used
            //     closed string F; // 8
            Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(11, 19));
    }

    [Fact]
    public void BaseTypeFromMetadata_01()
    {
        // Direct inheritance
        var source1 = """
            public closed class C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
        verifyReference(comp1.ToMetadataReference());
        verifyReference(comp1.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var source2 = """
                public class D : C { }
                """;
            var comp2 = CreateCompilation(source2, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics(
                // (1,14): error CS9367: 'D': cannot use a closed type 'C' from another assembly as a base type.
                // public class D : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "D").WithArguments("D", "C").WithLocation(1, 14));
        }
    }

    [Fact]
    public void BaseTypeFromMetadata_02()
    {
        // Direct inheritance from netmodule
        var source1 = """
            public closed class C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], options: TestOptions.DebugModule, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();

        var source2 = """
            public class D : C { }
            """;
        var comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (1,14): error CS9367: 'D': cannot use a closed type 'C' from another assembly as a base type.
            // public class D : C { }
            Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "D").WithArguments("D", "C").WithLocation(1, 14));
    }

    [Fact]
    public void BaseTypeFromMetadata_03()
    {
        // Used in type argument to base type
        var source1 = """
            public closed class C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
        verifyReference(comp1.ToMetadataReference());
        verifyReference(comp1.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var source2 = """
                public class C1<T> { }
                public class D : C1<C> { }
                """;
            var comp2 = CreateCompilation(source2, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics();
        }
    }

    [Fact]
    public void BaseTypeFromMetadata_04()
    {
        // Indirect inheritance through non-closed type
        var source1 = """
            public closed class C { }
            public class D : C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
        verifyReference(comp1.ToMetadataReference());
        verifyReference(comp1.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var source2 = """
                public class E : D { }
                """;
            var comp2 = CreateCompilation(source2, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics();
        }
    }

    [Fact]
    public void BaseTypeFromMetadata_05()
    {
        // Indirect inheritance through closed type
        var source1 = """
            public closed class C { }
            public closed class D : C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
        verifyReference(comp1.ToMetadataReference());
        verifyReference(comp1.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var source2 = """
                public class E : D { }
                """;
            var comp2 = CreateCompilation(source2, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics(
                // (1,14): error CS9367: 'E': cannot use a closed type 'D' from another assembly as a base type.
                // public class E : D { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "D").WithLocation(1, 14));
        }
    }

    [Fact]
    public void CompilerFeatureRequired_NonClosedContainingType()
    {
        // Constructor has CompilerFeatureRequired("ClosedClasses") yet containing type lacks ClosedAttribute
        var il = """
            .assembly extern System.Runtime { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }

            .class public auto ansi abstract beforefieldinit C
                extends [System.Runtime]System.Object
            {
                // Methods
                .method family hidebysig specialname rtspecialname
                    instance void .ctor () cil managed
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
                        00 00
                    )
                    // Method begins at RVA 0x2050
                    // Code size 7 (0x7)
                    .maxstack 8
                    IL_0000: ldarg.0
                    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
                    IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """;

        var ilComp = CompileIL(il);
        var source1 = """
            public class D : C { }
            """;
        var comp1 = CreateCompilation(source1, references: [ilComp], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (1,14): error CS9041: 'C.C()' requires compiler feature 'ClosedClasses', which is not supported by this version of the C# compiler.
            // public class D : C { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "D").WithArguments("C.C()", "ClosedClasses").WithLocation(1, 14));
    }

    [Fact]
    public void GenericSubtype_01()
    {
        // Type parameter is used in base
        var source1 = """
            public closed class C<T> { }

            public class D1<T> : C<T> { }
            public class D2<T> : C<T[]> { }
            public unsafe class D3<T> : C<T*[]> where T : unmanaged { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GenericSubtype_02()
    {
        // Type parameter is not used in base
        var source1 = """
            public closed class C { }
            public class D<T> : C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (2,14): error CS9368: 'D<T>': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            // public class D<T> : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("D<T>", "T", "C").WithLocation(2, 14));
    }

    [Fact]
    public void GenericSubtype_03()
    {
        // Type parameter from a containing type is used in base
        var source1 = """
            public closed class C<T> { }

            public class Outer<T>
            {
                public class D : C<T> { }
            }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GenericSubtype_04()
    {
        // Type parameter from a containing type is not used in base
        var source1 = """
            public closed class C { }

            public class Outer<T>
            {
                public class D : C { }
            }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (5,18): error CS9368: 'Outer<T>.D': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            //     public class D : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<T>.D", "T", "C").WithLocation(5, 18));
    }

    [Fact]
    public void GenericSubtype_05()
    {
        // Indirect generic subtype
        var source1 = """
            public closed class C { }
            class D : C { }
            class E<T> : D { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GenericSubtype_06()
    {
        // Mix of multiple used and unused type parameters
        var source1 = """
            public closed class C<T1, T2, T3, T4> { }
            class Outer<U1, U2, U3>
            {
                class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,11): error CS9368: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U5' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U5", "C<U1, U2, U4, U6>").WithLocation(4, 11),
            // (4,11): error CS9368: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U3' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U3", "C<U1, U2, U4, U6>").WithLocation(4, 11));
    }

    [Fact]
    public void GenericSubtype_07()
    {
        // Closed subtype does not use its type parameters in closed or non-closed base type
        var source1 = """
            public class C { }
            closed class D<T> : C { }

            public closed class E { }
            closed class F<T> : E { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (5,14): error CS9368: 'F<T>': The type parameter 'T' must be referenced in the base type 'E' because the base type is closed.
            // closed class F<T> : E { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "F").WithArguments("F<T>", "T", "E").WithLocation(5, 14));
    }

    [Fact]
    public void ConsumeFromVB_01()
    {
        var source1 = """
            public closed class C { }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();

        // PROTOTYPE(cc): VB should report an error due to presence of 'CompilerFeatureRequired' on the base constructor
        var source2 = """
            Public Class D
                Inherits C
            End Class
            """;
        CreateVisualBasicCompilation("Program", source2, referencedCompilations: [comp1], referencedAssemblies: comp1.References).VerifyEmitDiagnostics(
            );
    }

    [Fact]
    public void ConsumeFromVB_02()
    {
        var source1 = """
            public closed class C
            {
                public C() { }
                public C(int i) { }
            }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();

        var source2 = """
            Public Class D
                Inherits C

            #ExternalSource("file.vb", 100)
                Public Sub New()
                    MyBase.New()
                End Sub
            #End ExternalSource

            #ExternalSource("file.vb", 200)
                Public Sub New(i As Integer)
                    MyBase.New(i)
                End Sub
            #End ExternalSource
            End Class
            """;
        var vbComp = CreateVisualBasicCompilation("Program", source2, referencedCompilations: [comp1], referencedAssemblies: comp1.References);

        // note: the multi-line strings in the diagnostic arguments not behaving well when pasting in the VerifyDiagnostics baseline.
        // Just verifying the things we are interested in directly instead.
        var diagnostics = vbComp.GetDiagnostics();
        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(36954 /*ERRID.ERR_BadOverloadCandidates2*/, diagnostics[0].Code);
        Assert.Equal(2, diagnostics[0].Arguments.Count);
        AssertEx.AssertEqualToleratingWhitespaceDifferences("New", diagnostics[0].Arguments[0].ToString());
        //error BC0000:
        //        'Public Overloads Sub New()': 'Public Overloads Sub New()' requires compiler feature 'ClosedClasses', which is not supported by this version of the Visual Basic compiler.
        //        'Public Overloads Sub New(i As Integer)': 'Public Overloads Sub New(i As Integer)' requires compiler feature 'ClosedClasses', which is not supported by this version of the Visual Basic compiler.
        Assert.Contains(expectedSubstring: "ClosedClasses", actualString: diagnostics[0].Arguments[1].ToString());
        Assert.Equal("file.vb: (100,15)-(100,18)", diagnostics[0].Location.GetMappedLineSpan().ToString());

        Assert.Equal(36954 /*ERRID.ERR_BadOverloadCandidates2*/, diagnostics[1].Code);
        Assert.Equal(2, diagnostics[1].Arguments.Count);
        AssertEx.AssertEqualToleratingWhitespaceDifferences("New", diagnostics[1].Arguments[0].ToString());
        //error BC0000:
        //        'Public Overloads Sub New()': 'Public Overloads Sub New()' requires compiler feature 'ClosedClasses', which is not supported by this version of the Visual Basic compiler.
        //        'Public Overloads Sub New(i As Integer)': 'Public Overloads Sub New(i As Integer)' requires compiler feature 'ClosedClasses', which is not supported by this version of the Visual Basic compiler.
        Assert.Contains(expectedSubstring: "ClosedClasses", actualString: diagnostics[1].Arguments[1].ToString());
        Assert.Equal("file.vb: (200,15)-(200,18)", diagnostics[1].Location.GetMappedLineSpan().ToString());
    }

    [Fact]
    public void ClosedAttributeExplicitUsage()
    {
        var source1 = """
            #pragma warning disable CS0067 // The event is never used
            using System.Runtime.CompilerServices;

            [assembly: Closed] // 1
            [module: Closed] // 2

            [Closed] public class C // 3
            {
                [Closed] public C() { } // 4
                [Closed] public void M() { } // 5
                [Closed] public string P { get; set; } // 6
                [Closed] public string F; // 7
                [Closed] public event System.Action E; // 8

                public void M1([Closed] int param) { } // 9
                [return: Closed] public int M2() => 0; // 10
                public void M3<[Closed] T>() { } // 11
            }
            [Closed] public struct S { } // 12
            [Closed] public enum E { } // 13
            [Closed] public interface I { } // 14
            [Closed] public delegate void D(); // 15
            """;

        var closedAttributeAllowingAllTargets = """
            namespace System.Runtime.CompilerServices
            {
                public sealed class ClosedAttribute : Attribute { }
            }
            """;

        var comp1 = CreateCompilation([source1, closedAttributeAllowingAllTargets], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,12): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [assembly: Closed] // 1
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(4, 12),
            // (5,10): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [module: Closed] // 2
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(5, 10),
            // (7,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public class C // 3
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(7, 2),
            // (9,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [Closed] public C() { } // 4
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(9, 6),
            // (10,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [Closed] public void M() { } // 5
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(10, 6),
            // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [Closed] public string P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(11, 6),
            // (12,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [Closed] public string F; // 7
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(12, 6),
            // (13,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [Closed] public event System.Action E; // 8
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(13, 6),
            // (15,21): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     public void M1([Closed] int param) { } // 9
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(15, 21),
            // (16,14): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     [return: Closed] public int M2() => 0; // 10
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(16, 14),
            // (17,21): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            //     public void M3<[Closed] T>() { } // 11
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(17, 21),
            // (19,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public struct S { } // 12
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(19, 2),
            // (20,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public enum E { } // 13
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(20, 2),
            // (21,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public interface I { } // 14
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(21, 2),
            // (22,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public delegate void D(); // 15
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(22, 2));

        // Note: ERR_AttributeOnBadSymbolType causes well-known attribute decoding to be skipped.
        // So, ERR_ExplicitReservedAttr is only reported for the class attribute in this case.
        comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,12): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [assembly: Closed] // 1
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(4, 12),
            // (5,10): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [module: Closed] // 2
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(5, 10),
            // (7,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ClosedAttribute'. This is reserved for compiler usage.
            // [Closed] public class C // 3
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "Closed").WithArguments("System.Runtime.CompilerServices.ClosedAttribute").WithLocation(7, 2),
            // (9,6): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [Closed] public C() { } // 4
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(9, 6),
            // (10,6): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [Closed] public void M() { } // 5
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(10, 6),
            // (11,6): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [Closed] public string P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(11, 6),
            // (12,6): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [Closed] public string F; // 7
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(12, 6),
            // (13,6): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [Closed] public event System.Action E; // 8
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(13, 6),
            // (15,21): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     public void M1([Closed] int param) { } // 9
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(15, 21),
            // (16,14): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [return: Closed] public int M2() => 0; // 10
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(16, 14),
            // (17,21): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     public void M3<[Closed] T>() { } // 11
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(17, 21),
            // (19,2): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [Closed] public struct S { } // 12
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(19, 2),
            // (20,2): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [Closed] public enum E { } // 13
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(20, 2),
            // (21,2): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [Closed] public interface I { } // 14
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(21, 2),
            // (22,2): error CS0592: Attribute 'Closed' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [Closed] public delegate void D(); // 15
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Closed").WithArguments("Closed", "class").WithLocation(22, 2));
    }

    [Fact]
    public void RequiredMembers_01()
    {
        // Verify what attributes are emitted when both required members and closed classes are used
        var source1 = """
            public closed class C
            {
                public required string P { get; set; }
            }
            """;
        var verifier = CompileAndVerify([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100, symbolValidator: verifyMetadataSymbols, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifyUse(verifier.Compilation.ToMetadataReference());
        verifyUse(verifier.GetImageReference());

        void verifyMetadataSymbols(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var classC = peModule.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
            var ctor = (PEMethodSymbol)classC.Constructors.Single();

            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.RequiredMemberAttribute",
                    "System.Runtime.CompilerServices.ClosedAttribute"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(classC.Handle)));
            AssertEx.SetEqual([
                    """System.ObsoleteAttribute("Constructors of types with required members are not supported in this version of your compiler.", true)""",
                    """System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("RequiredMembers")""",
                    """System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("ClosedClasses")"""
                ], GetAttributeStrings(peModule.GetCustomAttributesForToken(ctor.Handle)));
        }

        void verifyUse(MetadataReference reference)
        {
            var comp2 = CreateCompilation("""
                using System;

                class D : C { }
                class E() : C { }
                class F : C
                {
                    public F() { }
                }

                class Program
                {
                    public void M(C c)
                    {
                        Console.Write(c.P);

                        _ = new D();
                        _ = new D() { P = "a" };
                    }
                }
                """, references: [reference], targetFramework: TargetFramework.Net100);
            comp2.VerifyEmitDiagnostics(
                // (3,7): error CS9367: 'D': cannot use a closed type 'C' from another assembly as a base type.
                // class D : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "D").WithArguments("D", "C").WithLocation(3, 7),
                // (4,7): error CS9367: 'E': cannot use a closed type 'C' from another assembly as a base type.
                // class E() : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "C").WithLocation(4, 7),
                // (5,7): error CS9367: 'F': cannot use a closed type 'C' from another assembly as a base type.
                // class F : C
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "F").WithArguments("F", "C").WithLocation(5, 7),
                // (16,17): error CS9035: Required member 'C.P' must be set in the object initializer or attribute constructor.
                //         _ = new D();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "D").WithArguments("C.P").WithLocation(16, 17));
        }
    }

    [Fact]
    public void RequiredMembers_02()
    {
        // Verify what attributes are emitted when both required members and closed classes with explicit constructors are used
        var source1 = """
            public closed class C
            {
                public C() { }
                public C(int value) { }

                public required int F;
            }
            """;

        var verifier = CompileAndVerify([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100, symbolValidator: verifyMetadataSymbols, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();

        void verifyMetadataSymbols(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var classC = peModule.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.RequiredMemberAttribute",
                    "System.Runtime.CompilerServices.ClosedAttribute"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(classC.Handle)));

            AssertEx.SetEqual(["C..ctor()", "C..ctor(System.Int32 value)"], classC.Constructors.ToTestDisplayStrings());
            foreach (PEMethodSymbol ctor in classC.Constructors)
            {
                AssertEx.SetEqual([
                        """System.ObsoleteAttribute("Constructors of types with required members are not supported in this version of your compiler.", true)""",
                        """System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("RequiredMembers")""",
                        """System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("ClosedClasses")"""
                    ], GetAttributeStrings(peModule.GetCustomAttributesForToken(ctor.Handle)));
            }
        }
    }
}
