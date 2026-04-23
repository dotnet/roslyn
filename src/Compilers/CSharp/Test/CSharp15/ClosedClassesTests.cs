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
            // (1,21): error CS9601: 'C': a closed type cannot be sealed or static
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
            // (1,21): error CS9601: 'C': a closed type cannot be sealed or static
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
                // (1,14): error CS9602: 'D': cannot use a closed type 'C' from another assembly as a base type.
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
            // (1,14): error CS9602: 'D': cannot use a closed type 'C' from another assembly as a base type.
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
                // (1,14): error CS9602: 'E': cannot use a closed type 'D' from another assembly as a base type.
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

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1<T>", "D2<T>", "D3<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
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
            // (2,14): error CS9603: 'D<T>': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            // public class D<T> : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("D<T>", "T", "C").WithLocation(2, 14));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void GenericSubtype_03()
    {
        // Type parameter from a containing type is used in base
        var source1 = """
            public closed class C<T> { }

            public class Outer<U>
            {
                public class D : C<U> { }
            }
            """;
        var comp1 = CreateCompilation([source1, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["Outer<T>.D"], classC.ClosedSubtypes.ToTestDisplayStrings());
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
            // (5,18): error CS9603: 'Outer<T>.D': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            //     public class D : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<T>.D", "T", "C").WithLocation(5, 18));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["Outer<T>.D"], classC.ClosedSubtypes.ToTestDisplayStrings());
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

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D"], classC.ClosedSubtypes.ToTestDisplayStrings());
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
            // (4,11): error CS9603: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U5' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U5", "C<U1, U2, U4, U6>").WithLocation(4, 11),
            // (4,11): error CS9603: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U3' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U3", "C<U1, U2, U4, U6>").WithLocation(4, 11));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["Outer<T1, T2, U3>.D<T3, U5, T4>"], classC.ClosedSubtypes.ToTestDisplayStrings());
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
            // (5,14): error CS9603: 'F<T>': The type parameter 'T' must be referenced in the base type 'E' because the base type is closed.
            // closed class F<T> : E { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "F").WithArguments("F<T>", "T", "E").WithLocation(5, 14));

        Assert.Empty(comp1.GetMember<NamedTypeSymbol>("C").ClosedSubtypes.ToTestDisplayStrings());
        Assert.Empty(comp1.GetMember<NamedTypeSymbol>("D").ClosedSubtypes.ToTestDisplayStrings());

        var classE = comp1.GetMember<NamedTypeSymbol>("E");
        Assert.Equal(["F<T>"], classE.ClosedSubtypes.ToTestDisplayStrings());

        Assert.Empty(comp1.GetMember<NamedTypeSymbol>("F").ClosedSubtypes.ToTestDisplayStrings());
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
                // (3,7): error CS9602: 'D': cannot use a closed type 'C' from another assembly as a base type.
                // class D : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "D").WithArguments("D", "C").WithLocation(3, 7),
                // (4,7): error CS9602: 'E': cannot use a closed type 'C' from another assembly as a base type.
                // class E() : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "C").WithLocation(4, 7),
                // (5,7): error CS9602: 'F': cannot use a closed type 'C' from another assembly as a base type.
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

    [Fact]
    public void Subtypes_01()
    {
        var source = """
            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var verifier = CompileAndVerify([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100, sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C", classC.ToTestDisplayString());
            Assert.Equal(["D1", "D2"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Subtypes_02()
    {
        var source = """
            closed class C<T>
            {
            }

            class D1<U> : C<U> { }
            class D2 : C<int> { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C<T>", classC.ToTestDisplayString());
            // Note: 'D2' is included in the set, because its base type 'C<int>' can unify with 'C<T>'.
            // For example, if we encounter a value of type 'C<U>' where U is some unconstrained generic,
            // then it's possible the value is also a 'D2'. i.e. 'U' could be substituted with 'int' at runtime.
            Assert.Equal(["D1<T>", "D2"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var cOfInt = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.Int32>", cOfInt.ToTestDisplayString());
            Assert.Equal(["D1<System.Int32>", "D2"], cOfInt.ClosedSubtypes.ToTestDisplayStrings());

            var cOfString = classC.Construct(comp.GetSpecialType(SpecialType.System_String));
            Assert.Equal("C<System.String>", cOfString.ToTestDisplayString());
            Assert.Equal(["D1<System.String>"], cOfString.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Subtypes_03()
    {
        // Test subtype using non-trivial type arguments to base type.
        var source = """
            using System.Collections.Immutable;

            closed class C<T>
            {
            }

            class D1<U> : C<U> { }
            class D2<U> : C<ImmutableArray<U>> { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C<T>", classC.ToTestDisplayString());
            // PROTOTYPE(cc): This 'ClosedSubtypes' result reflects a unification which is possible, but not in terms of types available at the use site.
            // It's unclear what representation of such types we want this API to use.
            // For example, perhaps we only want to identify the fact that the situation is occurring, and introduce some 'bool IsExhaustibleViaSubtypes' API
            // to check when creating a TypeUnionValueSet, so that we can advise user to match the base type.
            Assert.Equal(["D1<T>", "D2<U>"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var immutableArrayOfInt = comp
                .GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T)
                .Construct(comp.GetSpecialType(SpecialType.System_Int32));

            var cOfImmutableArray = classC.Construct(immutableArrayOfInt);
            Assert.Equal("C<System.Collections.Immutable.ImmutableArray<System.Int32>>", cOfImmutableArray.ToTestDisplayString());
            Assert.Equal(["D1<System.Collections.Immutable.ImmutableArray<System.Int32>>", "D2<System.Int32>"], cOfImmutableArray.ClosedSubtypes.ToTestDisplayStrings());

            var cOfInt = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.Int32>", cOfInt.ToTestDisplayString());
            Assert.Equal(["D1<System.Int32>"], cOfInt.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Subtypes_04()
    {
        // Verify that ClosedSubtypes API behaves reasonably in base type cycle scenario.
        var source = """
            using System.Collections.Immutable;

            closed class C<T> : D<T>
            {
            }

            closed class D<T> : C<T>
            {
            }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (3,14): error CS0146: Circular base type dependency involving 'D<T>' and 'C<T>'
            // closed class C<T> : D<T>
            Diagnostic(ErrorCode.ERR_CircularBase, "C").WithArguments("D<T>", "C<T>").WithLocation(3, 14),
            // (7,14): error CS0146: Circular base type dependency involving 'C<T>' and 'D<T>'
            // closed class D<T> : C<T>
            Diagnostic(ErrorCode.ERR_CircularBase, "D").WithArguments("C<T>", "D<T>").WithLocation(7, 14));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("C<T>", classC.ToTestDisplayString());
        Assert.Equal([], classC.ClosedSubtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Subtypes_05()
    {
        var source = """
            closed class C<T1, T2>
            {
            }

            class D1<U1> : C<U1, int> { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C<T1, T2>", classC.ToTestDisplayString());
            Assert.Equal(["D1<T1>"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var cOfStringInt = classC.Construct(comp.GetSpecialType(SpecialType.System_String), comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.String, System.Int32>", cOfStringInt.ToTestDisplayString());
            Assert.Equal(["D1<System.String>"], cOfStringInt.ClosedSubtypes.ToTestDisplayStrings());

            var cOfIntString = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32), comp.GetSpecialType(SpecialType.System_String));
            Assert.Equal("C<System.Int32, System.String>", cOfIntString.ToTestDisplayString());
            Assert.Empty(cOfIntString.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_01()
    {
        // Simple case
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_02()
    {
        // Non-exhaustive inner property pattern
        var source = """
            #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 { Value: 1 } => 2,
                        D2 { Value: > 1 } => 3,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { public int Value; }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (6,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2{ Value: 0 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2{ Value: 0 }").WithLocation(6, 18));
    }

    [Fact]
    public void Exhaustiveness_03()
    {
        // Exhaustive inner property pattern
        var source = """
            #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 { Value: 1 } => 2,
                        D2 { Value: > 1 } => 3,
                        D2 { Value: < 1 } => 4,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { public int Value; }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (11,25): hidden CS9335: The pattern is redundant.
            //             D2 { Value: < 1 } => 4,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "< 1").WithLocation(11, 25));
    }

    [Fact]
    public void Exhaustiveness_04()
    {
        // Non-exhaustive type match
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(5, 18));
    }

    [Fact]
    public void Exhaustiveness_05()
    {
        // Non-exhaustive type match of nested hierarchy
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        E1 => 1,
                        F1 => 2,
                        E2 => 3,
                    };
                }
            }

            closed class C
            {
            }

            closed class D1 : C { }
            class E1 : D1 { }
            class F1 : D1 { }

            closed class D2 : C { }
            class E2 : D2 { }
            class F2 : D2 { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'F2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("F2").WithLocation(5, 18));
    }

    [Fact]
    public void Exhaustiveness_06()
    {
        // Exhaustive type match of nested hierarchy
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        E1 => 1,
                        F1 => 2,
                        E2 => 3,
                        F2 => 4,
                    };
                }
            }

            closed class C
            {
            }

            closed class D1 : C { }
            class E1 : D1 { }
            class F1 : D1 { }

            closed class D2 : C { }
            class E2 : D2 { }
            class F2 : D2 { }
            """;

        var comp = CreateCompilation([source, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_07()
    {
        // Union with closed classes as case types
        var source = """
            class Program
            {
                int M(U u)
                {
                    return u switch
                    {
                        E1 => 1,
                        F1 => 2,
                        E2 => 3,
                        F2 => 4,
                    };
                }
            }

            union U(D1, D2);

            closed class D1 { }
            class E1 : D1 { }
            class F1 : D1 { }

            closed class D2 { }
            class E2 : D2 { }
            class F2 : D2 { }
            """;

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_08()
    {
        // Simple generic closed hierarchy
        var source = """
            class Program
            {
                int M<X>(C<X> c)
                {
                    return c switch
                    {
                        D1<X> => 1,
                        D2<X> => 2,
                    };
                }
            }

            closed class C<T>
            {
                int M()
                {
                    return this switch
                    {
                        D1<T> => 1,
                        D2<T> => 2,
                    };
                }
            }

            class D1<U> : C<U>;
            class D2<V> : C<V>;
            """;

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    /// <summary>Tests an exhaustiveness scenario similar to <see cref="Subtypes_03"/>.</summary>
    [Fact]
    public void Exhaustiveness_09()
    {
        var source = """
            using System.Collections.Immutable;

            closed class C<T>
            {
                public static int Use1(C<T> item)
                {
                    return item switch
                    {
                        D1<T> => 1,
                        // We know some 'D2<...>' may be possible here (i.e. 'C<T>' allows 'C<ImmutableArray<...>>' by substitution.)
                        // But, we have no way of speaking that D2 in this context.
                    };
                }

                public static int Use2(C<T> item)
                {
                    return item switch
                    {
                        D1<T> => 1,
                        C<T> => 2
                    };
                }
            }

            class D1<U> : C<U> { }
            class D2<U> : C<ImmutableArray<U>> { }
            """;

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        // PROTOTYPE(cc): We should not propose invalid subtype patterns (using type parameters not in scope, or which violate accessibility, constraints, etc.)
        comp.VerifyDiagnostics(
            // (7,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<U>' is not covered.
            //         return item switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<U>").WithLocation(7, 21));
    }

    [Fact]
    public void Exhaustiveness_NoSubtypes()
    {
        // Closed with no subtypes
        var source1 = """
            public closed class C;
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
                    return c switch
                    {
                    };
                }

                int M2(C c)
                {
                    return c switch
                    {
                        C => 1
                    };
                }

                int M3(C c)
                {
                    return c switch
                    {
                        _ => 1
                    };
                }
            }
            """;

        // PROTOTYPE(cc): unexpected warnings in all below scenarios
        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Empty(classC.ClosedSubtypes);

        var comp1 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));
        classC = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.Empty(classC.ClosedSubtypes);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));
        classC = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.Empty(classC.ClosedSubtypes);
    }

    [Fact]
    public void Exhaustiveness_OnlyClosedSubtypes()
    {
        // Closed with only closed subtypes
        var source1 = """
            public closed class C;
            public closed class D : C;
            public closed class E : D;
            public closed class F : C;
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
            #line 100
                    return c switch
                    {
                    };
                }

                int M2(C c)
                {
                    return c switch
                    {
                        E => 1,
                        F => 1
                    };
                }

                int M3(C c)
                {
                    return c switch
                    {
                        _ => 1
                    };
                }

                int M4(D d)
                {
            #line 200
                    return d switch
                    {
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'F' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("F").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'E' is not covered.
                //         return d switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("E").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D", "F"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_01()
    {
        // Less accessible subtype
        var source1 = """
            public closed class C;
            public class D1 : C;

            public class Container
            {
                protected class D2 : C;
            }
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
                    return c switch
                    {
                        D1 => 1,
            #line 100
                        Container.D2 => 2,
                    };
                }

                int M2(C c)
                {
            #line 200
                    return c switch
                    {
                        D1 => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,23): error CS0122: 'Container.D2' is inaccessible due to its protection level
                //             Container.D2 => 2,
                Diagnostic(ErrorCode.ERR_BadAccess, "D2").WithArguments("Container.D2").WithLocation(100, 23),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Container.D2' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Container.D2").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1", "Container.D2"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_02()
    {
        // Subtype less accessible only when used from other assembly
        var source1 = """
            public closed class C;
            public class D1 : C;
            class D2 : C;
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
                    return c switch
                    {
                        D1 => 1,
            #line 100
                        D2 => 2,
                    };
                }

                int M2(C c)
                {
            #line 200
                    return c switch
                    {
                        D1 => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(200, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1", "D2"], classC.ClosedSubtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,13): error CS0122: 'D2' is inaccessible due to its protection level
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_BadAccess, "D2").WithArguments("D2").WithLocation(100, 13),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(200, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1", "D2"], classC.ClosedSubtypes.ToTestDisplayStrings());

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,13): error CS0122: 'D2' is inaccessible due to its protection level
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_BadAccess, "D2").WithArguments("D2").WithLocation(100, 13),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(200, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1", "D2"], classC.ClosedSubtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_Constraints_01()
    {
        // Subtype definition constraints which can "overlap" with constructed closed type
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1>;
            public class D2<U2> : C<U2> where U2 : struct;
            """;

        var source2 = """
            class Program
            {
                int M1<X>(C<X> c)
                {
                    return c switch
                    {
                        D1<X> => 1,
            #line 100
                        D2<X> => 2,
                    };
                }

                int M2<X>(C<X> c)
                {
            #line 200
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,16): error CS0453: The type 'X' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
                //             D2<X> => 2,
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "X").WithArguments("D2<U2>", "U2", "X").WithLocation(100, 16),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<X>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<T>", "D2<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_02()
    {
        // Subtype definition constraints which do not "overlap" with constructed closed type using type parameters
        // PROTOTYPE(cc): Should we detect when the subtype cannot unify with the closed type due to subtype constraints?
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1>;
            public class D2<U2> : C<U2> where U2 : struct;
            """;

        var source2 = """
            class Program
            {
                int M1<X>(C<X> c) where X : class
                {
                    return c switch
                    {
                        D1<X> => 1,
            #line 100
                        D2<X> => 2,
                    };
                }

                int M2<X>(C<X> c) where X : class
                {
            #line 200
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,16): error CS0453: The type 'X' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
                //             D2<X> => 2,
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "X").WithArguments("D2<U2>", "U2", "X").WithLocation(100, 16),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<X>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<T>", "D2<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_03()
    {
        // Subtype definition constraints which do not "overlap" with constructed closed type using concrete types
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1>;
            public class D2<U2> : C<U2> where U2 : struct;
            """;

        var source2 = """
            class Program
            {
                int M1(C<string> c)
                {
                    return c switch
                    {
                        D1<string> => 1,
            #line 100
                        D2<string> => 2,
                    };
                }

                int M2(C<string> c)
                {
            #line 200
                    return c switch
                    {
                        D1<string> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,16): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
                //             D2<string> => 2,
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("D2<U2>", "U2", "string").WithLocation(100, 16),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<string>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<string>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<T>", "D2<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_04()
    {
        // Subtype violates the constraints of the base type
        var source1 = """
            public closed class C<T> where T : class;
            public class D1<U1> : C<U1> where U1 : class;
            #line 100
            public class D2<U2> : C<U2> where U2 : struct;
            """;

        var source2 = """
            class Program
            {
                int M1(C<string> c)
                {
                    return c switch
                    {
                        D1<string> => 1,
            #line 200
                        D2<string> => 2,
                    };
                }

                int M2(C<string> c)
                {
            #line 300
                    return c switch
                    {
                        D1<string> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,14): error CS0452: The type 'U2' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
            // public class D2<U2> : C<U2> where U2 : struct;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "D2").WithArguments("C<T>", "T", "U2").WithLocation(100, 14),
            // (200,16): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
            //             D2<string> => 2,
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("D2<U2>", "U2", "string").WithLocation(200, 16),
            // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<string>' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<string>").WithLocation(300, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1<T>", "D2<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (200,16): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
            //             D2<string> => 2,
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("D2<U2>", "U2", "string").WithLocation(200, 16),
            // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<string>' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<string>").WithLocation(300, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(["D1<T>", "D2<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_GenericContainingType_01()
    {
        var source1 = """
            public class Container<T>
            {
                public closed class C;
                public class D1 : C;
            }

            public class D2<U> : Container<U>.C;
            public class D3 : Container<string>.C;
            public class D4 : Container<int>.C;
            """;

        var source2 = """
            class Program
            {
                int M1(Container<string>.C c)
                {
                    return c switch
                    {
                        Container<string>.D1 => 1,
                        D2<string> => 2,
                        D3 => 3,
                    };
                }

                int M2(Container<int>.C c)
                {
                    return c switch
                    {
                        Container<int>.D1 => 1,
                        D2<int> => 2,
                        D4 => 3,
                    };
                }

                int M3<X>(Container<X>.C c)
                {
                    return c switch
                    {
                        Container<X>.D1 => 1,
                        D2<X> => 2,
                        D3 => 3,
                        D4 => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.Equal(["Container<T>.D1", "D2<T>", "D3", "D4"], classC.ClosedSubtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.Equal(["Container<T>.D1", "D2<T>", "D3", "D4"], classC.ClosedSubtypes.ToTestDisplayStrings());

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.Equal(["D2<T>", "D3", "D4", "Container<T>.D1"], classC.ClosedSubtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_01()
    {
        // Attempt to exhaust a type parameter constrained to closed type.
        // This scenario isn't supported by the exhaustiveness check.
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1>;

            public closed class E;
            public sealed class F1;
            public sealed class F2;
            """;

        var source2 = """
            class Program
            {
                int M1<X>(C<X> c) where X : E
                {
            #line 100
                    return c switch
                    {
                        D1<F1> => 1,
                        D1<F2> => 2,
                        D1<E> => 3,
                    };
                }

                int M2<X>(C<X> c) where X : E
                {
                    return c switch
                    {
                        D1<X> => 3,
                    };
                }

                int M3<X>(D1<X> c) where X : E
                {
            #line 200
                    return c switch
                    {
                        D1<F1> => 1,
                        D1<F2> => 2,
                        D1<E> => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<T>"], classC.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_BaseTypeArguments_Array_01()
    {
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1[]>;
            """;

        var source2 = """
            class Program
            {
                int M1(C<string[]> c)
                {
                    return c switch
                    {
                        D1<string> => 1,
                    };
                }

                int M2(C<string[]> c)
                {
            #line 100
                    return c switch
                    {
                    };
                }

                int M3<X>(C<X[]> c)
                {
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }

                int M4<X>(C<X[]> c)
                {
            #line 200
                    return c switch
                    {
                    };
                }

                int M5<X>(C<X> c)
                {
            #line 300
                    return c switch
                    {
                    };
                }

                int M6(C<string[]> c)
                {
                    return c switch
                    {
            #line 400
                        D1<object> => 1
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<string>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<string>").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(200, 18),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<U1>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<U1>").WithLocation(300, 18),
                // (400,13): error CS8121: An expression of type 'C<string[]>' cannot be handled by a pattern of type 'D1<object>'.
                //             D1<object> => 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "D1<object>").WithArguments("C<string[]>", "D1<object>").WithLocation(400, 13));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<U1>"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var cOfStringArray = classC.Construct(
                comp.CreateArrayTypeSymbol(comp.GetSpecialType(SpecialType.System_String)));
            Assert.Equal(["D1<System.String>"], cOfStringArray.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_BaseTypeArguments_Array_02()
    {
        // Base type uses array of pointers
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1*[]> where U1 : unmanaged;
            """;

        var source2 = """
            class Program
            {
                unsafe int M1(C<int*[]> c)
                {
                    return c switch
                    {
                        D1<int> => 1,
                    };
                }

                unsafe int M2(C<int*[]> c)
                {
            #line 100
                    return c switch
                    {
                    };
                }

                unsafe int M3<X>(C<X*[]> c) where X : unmanaged
                {
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }

                unsafe int M4<X>(C<X*[]> c) where X : unmanaged
                {
            #line 200
                    return c switch
                    {
                    };
                }

                unsafe int M5<X>(C<X[]> c) where X : unmanaged
                {
            #line 300
                    return c switch
                    {
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int>").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(200, 18),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X[]>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X[]>").WithLocation(300, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<U1>"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var cOfStringArray = classC.Construct(
                comp.CreateArrayTypeSymbol(
                    comp.CreatePointerTypeSymbol(
                        comp.GetSpecialType(SpecialType.System_Int32))));
            Assert.Equal(["D1<System.Int32>"], cOfStringArray.ClosedSubtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_BaseTypeArguments_Tuple()
    {
        // Base type uses tuple
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<(U1, int)>;
            """;

        var source2 = """
            using System;

            class Program
            {
                int M1<X>(C<(X, int)> c)
                {
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }

                int M2<X>(C<(X, int)> c)
                {
            #line 100
                    return c switch
                    {
                    };
                }

                int M3<X>(C<ValueTuple<X, int>> c)
                {
                    return c switch
                    {
                        D1<X> => 1,
                    };
                }

                int M4<X>(C<ValueTuple<X, int>> c)
                {
            #line 200
                    return c switch
                    {
                    };
                }

                int M5<X1, X2>(C<(X1, X2)> c)
                {
                    return c switch
                    {
                        D1<X1> => 1,
                    };
                }

                int M6<X1, X2>(C<(X1, X2)> c)
                {
            #line 300
                    return c switch
                    {
                    };
                }

                int M7<X>(C<X> c)
                {
            #line 400
                    return c switch
                    {
                    };
                }

                int M8<X>(C<X> c) where X : class
                {
            #line 500
                    return c switch
                    {
                    };
                }

                int M9(C<string> c)
                {
            #line 600
                    return c switch
                    {
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, ClosedAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(200, 18),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X1>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X1>").WithLocation(300, 18),
                // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<U1>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<U1>").WithLocation(400, 18),
                // (500,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<U1>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<U1>").WithLocation(500, 18),
                // (600,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<string>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<string>").WithLocation(600, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(["D1<U1>"], classC.ClosedSubtypes.ToTestDisplayStrings());

            var tupleOfStringInt = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(
                    comp.GetSpecialType(SpecialType.System_String),
                    comp.GetSpecialType(SpecialType.System_Int32));

            var cOfStringArray = classC.Construct(tupleOfStringInt);
            Assert.Equal(["D1<System.String>"], cOfStringArray.ClosedSubtypes.ToTestDisplayStrings());

            tupleOfStringInt = NamedTypeSymbol.CreateTuple(tupleOfStringInt);
            cOfStringArray = classC.Construct(tupleOfStringInt);
            Assert.Equal(["D1<System.String>"], cOfStringArray.ClosedSubtypes.ToTestDisplayStrings());
        }
    }
}
