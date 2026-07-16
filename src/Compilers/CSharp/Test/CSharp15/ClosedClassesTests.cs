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
using System.Threading;
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // closed class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14));

        comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsClosedTypeAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute", ".ctor").WithLocation(1, 21));

        var comp2 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsClosedTypeAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute", ".ctor").WithLocation(1, 21),
            // (1,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
            // public closed class C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(1, 21));

        var comp2 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var verifier = CompileAndVerify([source, IsClosedTypeAttributeDefinition], symbolValidator: verifySymbols, sourceSymbolValidator: verifySymbols, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("C", """
            .class private auto ansi abstract beforefieldinit C
                extends [System.Runtime]System.Object
            {
                .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
                    01 00 01 00 54 1d 50 0c 44 65 72 69 76 65 64 54
                    79 70 65 73 00 00 00 00
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
            // IsClosedTypeAttribute is filtered out of source and metadata symbols.
            Assert.Empty(classC.GetAttributes());

            var ctor = classC.Constructors.Single();
            // CompilerFeatureRequiredAttribute is filtered out
            Assert.Empty(ctor.GetAttributes());

            if (module is PEModuleSymbol peModule)
            {
                var peType = (PENamedTypeSymbol)classC;
                // Get attributes from metadata without doing any filtering
                AssertEx.SetEqual([
                        "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {})"
                    ],
                    GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
            }
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

        var verifier = CompileAndVerify([source, IsClosedTypeAttributeDefinition], symbolValidator: verifySymbols, sourceSymbolValidator: verifySymbols, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("C", """
            .class private auto ansi abstract beforefieldinit C
                extends [System.Runtime]System.Object
            {
                .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
                    01 00 01 00 54 1d 50 0c 44 65 72 69 76 65 64 54
                    79 70 65 73 00 00 00 00
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

    private static readonly string s_reportHelper = """
        using System.Runtime.CompilerServices;
        using System.Linq;
        using System;

        public partial class Program
        {
            public static void Report(Type type)
            {
                var attr = (IsClosedTypeAttribute)type.GetCustomAttributes(typeof(IsClosedTypeAttribute), inherit: false).FirstOrDefault();
                if (attr is null)
                {
                    Console.Write("<null> ");
                    return;
                }

                Console.Write(attr.DerivedTypes.Length);
                Console.Write(" ");
                foreach (var derivedType in attr.DerivedTypes)
                {
                    Console.Write(derivedType.FullName);
                    if (derivedType.IsConstructedGenericType)
                        throw new Exception(); // unexpected

                    if (derivedType.GetGenericArguments() is { Length: > 0 } args)
                    {
                        if (!derivedType.IsGenericTypeDefinition)
                            throw new Exception(); // unexpected

                        Console.Write("[");
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (i > 0)
                            {
                                Console.Write(",");
                            }

                            Console.Write(args[i].FullName ?? args[i].Name);
                        }

                        Console.Write("]");
                    }
                    Console.Write(" ");
                }
            }
        }
        """;

    [Fact]
    public void DerivedTypesMetadata_01()
    {
        // simple case
        var source = """
            Report(typeof(C));
            Report(typeof(D1));

            closed class C;

            class D1 : C;
            class D2 : C;

            class D3 : D1;
            """;

        var verifier = CompileAndVerify(
            [source, IsClosedTypeAttributeDefinition, s_reportHelper, CompilerFeatureRequiredAttribute],
            symbolValidator: verifyMetadata,
            expectedOutput: "2 D1 D2 <null>");
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.IsClosed);
            // attribute is filtered out of source and metadata symbols.
            Assert.Empty(classC.GetAttributes());

            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)classC;
            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));

            peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("D1");
            AssertEx.Empty(GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_02()
    {
        // nested hierarchy
        var source = """
            Report(typeof(C));
            Report(typeof(D1));
            Report(typeof(D5));

            closed class C;

            closed class D1 : C;
            class D2 : C;

            class D3 : D1;
            class D4 : D1;

            class D5 : D4;
            """;

        var verifier = CompileAndVerify(
            [source, IsClosedTypeAttributeDefinition, s_reportHelper, CompilerFeatureRequiredAttribute],
            symbolValidator: verifyMetadata,
            expectedOutput: "2 D1 D2 2 D3 D4 <null>");
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));

            peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("D1");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D3), typeof(D4)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_03()
    {
        // various generic subtypes
        var source = """
            Report(typeof(C<>));

            closed class C<T>;

            class D1 : C<string>;
            class D2 : C<int>;
            class D3<T> : C<T>;
            class D4<T> : C<T*[]> where T : unmanaged;
            class D5<T, U> : C<(T, U)>;
            """;

        var verifier = CompileAndVerify(
            [source, IsClosedTypeAttributeDefinition, s_reportHelper, CompilerFeatureRequiredAttribute],
            symbolValidator: verifyMetadata,
            expectedOutput: "5 D1 D2 D3`1[T] D4`1[T] D5`2[T,U]");
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2), typeof(D3<>), typeof(D4<>), typeof(D5<,>)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_04()
    {
        // nested generic subtype
        var source = """
            Report(typeof(C<,>));
            Report(typeof(C<int, string>));

            closed class C<T, U>;

            class Container<T>
            {
                internal class D1<U> : C<T, U>;
                internal class D2 : C<T, string>;
            }
            """;

        var verifier = CompileAndVerify(
            [source, IsClosedTypeAttributeDefinition, s_reportHelper, CompilerFeatureRequiredAttribute],
            symbolValidator: verifyMetadata,
            expectedOutput: "2 Container`1+D1`1[T,U] Container`1+D2[T] 2 Container`1+D1`1[T,U] Container`1+D2[T]");
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(Container<>.D1<>), typeof(Container<>.D2)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_05()
    {
        // System.Type is missing
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.MakeTypeMissing(WellKnownType.System_Type);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_06()
    {
        // DerivedTypes property is missing
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute;
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute, CompilerFeatureRequiredAttribute]);

        var verifier = CompileAndVerify(comp, symbolValidator: verifyMetadata);
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_07()
    {
        // DerivedTypes only has getter
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes { get; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: The property 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_08()
    {
        // DerivedTypes only has setter
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes { set { } }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: The property 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_09()
    {
        // DerivedTypes getter is internal.
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes { internal get; set; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS9395: The property 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_10()
    {
        // DerivedTypes inaccessible setter
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes { get; private set; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: The property 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_11()
    {
        // DerivedTypes wrong type
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public int[] DerivedTypes { get; set; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14)
            );
    }

    [Fact]
    public void DerivedTypesMetadata_12()
    {
        // DerivedTypes property has parameters
        var source1 = """
            Namespace System.Runtime.CompilerServices
                <System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
                Public NotInheritable Class IsClosedTypeAttribute
                    Inherits System.Attribute

                    Private _derivedTypes As System.Type()

                    Public Property DerivedTypes(Optional x As Integer = 0) As System.Type()
                        Get
                            Return _derivedTypes
                        End Get
                        Set(value As System.Type())
                            _derivedTypes = value
                        End Set
                    End Property
                End Class
            End Namespace
            """;

        var source2 = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var comp = CreateCompilation([source2], references: [CreateVisualBasicCompilation(source1).EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_13()
    {
        // DerivedTypes argument from IL is missing a type
        var ilSource = """
      .class private auto ansi abstract beforefieldinit C
          extends [mscorlib]System.Object
      {
          .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
              01 00 01 00 54 1d 50 0c 44 65 72 69 76 65 64 54 // ...DerivedT
              79 70 65 73 01 00 00 00 02 44 31                // ypes...D1
          )
          // Methods
          .method family hidebysig specialname rtspecialname 
              instance void .ctor () cil managed 
          {
              .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                  01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
                  00 00
              )
              // Method begins at RVA 0x2050
              // Code size 7 (0x7)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: call instance void [mscorlib]System.Object::.ctor()
              IL_0006: ret
          } // end of method C::.ctor
      } // end of class C

      .class private auto ansi beforefieldinit D1
          extends C
      {
          // Methods
          .method public hidebysig specialname rtspecialname 
              instance void .ctor () cil managed 
          {
              // Method begins at RVA 0x2058
              // Code size 7 (0x7)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: call instance void C::.ctor()
              IL_0006: ret
          } // end of method D1::.ctor
      } // end of class D1

      .class private auto ansi beforefieldinit D2
          extends C
      {
          // Methods
          .method public hidebysig specialname rtspecialname 
              instance void .ctor () cil managed 
          {
              // Method begins at RVA 0x2058
              // Code size 7 (0x7)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: call instance void C::.ctor()
              IL_0006: ret
          } // end of method D2::.ctor
      } // end of class D2

      .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsClosedTypeAttribute
          extends [mscorlib]System.Attribute
      {
          .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
              01 00 04 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
              4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
              72 69 74 65 64 00
          )
          // Fields
          .field private class [mscorlib]System.Type[] '<DerivedTypes>k__BackingField'
          .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
              01 00 00 00
          )
          // Methods
          .method public hidebysig specialname 
              instance class [mscorlib]System.Type[] get_DerivedTypes () cil managed 
          {
              .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                  01 00 00 00
              )
              // Method begins at RVA 0x2060
              // Code size 7 (0x7)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: ldfld class [mscorlib]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::'<DerivedTypes>k__BackingField'
              IL_0006: ret
          } // end of method IsClosedTypeAttribute::get_DerivedTypes
          .method public hidebysig specialname 
              instance void set_DerivedTypes (
                  class [mscorlib]System.Type[] 'value'
              ) cil managed 
          {
              .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                  01 00 00 00
              )
              // Method begins at RVA 0x2068
              // Code size 8 (0x8)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: ldarg.1
              IL_0002: stfld class [mscorlib]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::'<DerivedTypes>k__BackingField'
              IL_0007: ret
          } // end of method IsClosedTypeAttribute::set_DerivedTypes
          .method public hidebysig specialname rtspecialname 
              instance void .ctor () cil managed 
          {
              // Method begins at RVA 0x2071
              // Code size 7 (0x7)
              .maxstack 8
              IL_0000: ldarg.0
              IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
              IL_0006: ret
          } // end of method IsClosedTypeAttribute::.ctor
          // Properties
          .property instance class [mscorlib]System.Type[] DerivedTypes()
          {
              .get instance class [mscorlib]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::get_DerivedTypes()
              .set instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::set_DerivedTypes(class [mscorlib]System.Type[])
          }
      } // end of class System.Runtime.CompilerServices.IsClosedTypeAttribute
      """;

        var comp = CreateCompilationWithIL("", ilSource, TargetFramework.Net100);

        var peType = (PENamedTypeSymbol)comp.GetMember("C");
        var peModule = peType.ContainingPEModule;
        AssertEx.SetEqual([
                "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1)})"
            ],
            GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));

        AssertEx.SetEqual(["D1", "D2"], peType.CandidateClosedSubtypeDefinitions.ToTestDisplayStrings());
        Assert.True(peType.TryGetClosedSubtypes(out var subtypes));
        AssertEx.SetEqual(["D1", "D2"], subtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void DerivedTypesMetadata_14()
    {
        // DerivedTypes is a field, not a property
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes;
                }
            }
            """;

        var verifier = CompileAndVerify([source, isClosedTypeAttribute, CompilerFeatureRequiredAttribute], symbolValidator: verifyMetadata);
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_15()
    {
        // DerivedTypes is a method, not a property
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public Type[] DerivedTypes() => throw null;
                    public Type[] DerivedTypes(bool ignored) => throw null;
                }
            }
            """;

        var verifier = CompileAndVerify([source, isClosedTypeAttribute, CompilerFeatureRequiredAttribute], symbolValidator: verifyMetadata);
        verifier.VerifyDiagnostics();

        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void DerivedTypesMetadata_16()
    {
        // DerivedTypes is static
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    public static Type[] DerivedTypes { get; set; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14)
            );
    }

    [Fact]
    public void DerivedTypesMetadata_17()
    {
        // DerivedTypes is ref-returning
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    private Type[] _derivedTypes = null;
                    public ref Type[] DerivedTypes => ref _derivedTypes;
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,14): error CS9395: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_18()
    {
        // DerivedTypes has a 'CompilerFeatureRequiredAttribute' for an unsupported feature, resulting in a use-site error.
        var il = """
            .assembly extern System.Runtime { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsClosedTypeAttribute
                extends [System.Runtime]System.Attribute
            {
                .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
                    01 00 04 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
                    4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
                    72 69 74 65 64 00
                )
                // Fields
                .field private class [System.Runtime]System.Type[] '<DerivedTypes>k__BackingField'
                // Methods
                .method public hidebysig specialname 
                    instance class [System.Runtime]System.Type[] get_DerivedTypes () cil managed 
                {
                    IL_0000: ldarg.0
                    IL_0001: ldfld class [System.Runtime]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::'<DerivedTypes>k__BackingField'
                    IL_0006: ret
                } // end of method IsClosedTypeAttribute::get_DerivedTypes
                .method public hidebysig specialname 
                    instance void set_DerivedTypes (
                        class [System.Runtime]System.Type[] 'value'
                    ) cil managed 
                {
                    IL_0000: ldarg.0
                    IL_0001: ldarg.1
                    IL_0002: stfld class [System.Runtime]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::'<DerivedTypes>k__BackingField'
                    IL_0007: ret
                } // end of method IsClosedTypeAttribute::set_DerivedTypes
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    IL_0000: ldarg.0
                    IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
                    IL_0006: ret
                } // end of method IsClosedTypeAttribute::.ctor
                // Properties
                .property instance class [System.Runtime]System.Type[] DerivedTypes()
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0b 4e 6f 6e 65 78 69 73 74 65 6e 74 00 00 // ...Nonexistent
                    )
                    .get instance class [System.Runtime]System.Type[] System.Runtime.CompilerServices.IsClosedTypeAttribute::get_DerivedTypes()
                    .set instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::set_DerivedTypes(class [System.Runtime]System.Type[])
                }
            } // end of class System.Runtime.CompilerServices.IsClosedTypeAttribute
            """;

        var ilComp = CompileIL(il);
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var comp = CreateCompilation(source, references: [ilComp], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS9041: 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' requires compiler feature 'Nonexistent', which is not supported by this version of the C# compiler.
            // closed class C;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "C").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes", "Nonexistent").WithLocation(1, 14));
    }

    [Fact]
    public void DerivedTypesMetadata_19()
    {
        // DerivedTypes property is internal.
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var isClosedTypeAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                public sealed class IsClosedTypeAttribute : Attribute
                {
                    internal Type[] DerivedTypes { get; set; }
                }
            }
            """;

        var comp = CreateCompilation([source, isClosedTypeAttribute], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS9395: The property 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'.
            // closed class C;
            Diagnostic(ErrorCode.ERR_ClosedBadDerivedTypesProperty, "C").WithLocation(1, 14));
    }

    [Fact]
    public void PublicAPI_01()
    {
        var source = """
            closed class C;

            class D1 : C;
            class D2 : C;
            """;

        var verifier = CompileAndVerify([source, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute], symbolValidator: verifySymbols, sourceSymbolValidator: verifySymbols);
        verifier.VerifyDiagnostics();

        void verifySymbols(ModuleSymbol module)
        {
            ITypeSymbol classC = module.GlobalNamespace.GetMember<TypeSymbol>("C").GetPublicSymbol();
            Assert.True(classC.IsClosed);
            // attribute is filtered out of source and metadata symbols.
            Assert.Empty(classC.GetAttributes());

            var derivedTypeInfo = classC.GetClosedDerivedTypeInfo(CancellationToken.None);
            Assert.Equal(["D1", "D2"], derivedTypeInfo.ClosedDerivedTypes.ToTestDisplayStrings());
            Assert.True(derivedTypeInfo.IsComplete);

            var d1 = derivedTypeInfo.ClosedDerivedTypes[0];
            Assert.False(d1.IsClosed);
            Assert.Throws<InvalidOperationException>(() => d1.GetClosedDerivedTypeInfo(CancellationToken.None));

            var source = new CancellationTokenSource();
            source.Cancel();
            Assert.Throws<OperationCanceledException>(() => classC.GetClosedDerivedTypeInfo(source.Token));
        }
    }

    [Fact]
    public void PublicAPI_02()
    {
        var source = """
            closed class C<T>;

            class D1<U1> : C<U1>;
            class D2<U2> : C<U2[]>;
            class D3 : C<string>;
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C").GetPublicSymbol();
            Assert.Equal("C<T>", classC.ToTestDisplayString());

            var derivedTypeInfo = classC.GetClosedDerivedTypeInfo(CancellationToken.None);
            Assert.False(derivedTypeInfo.IsComplete);
            Assert.Equal(["D1<T>", "D3"], derivedTypeInfo.ClosedDerivedTypes.ToTestDisplayStrings());

            var cOfIntArray = classC.Construct(comp.CreateArrayTypeSymbol(comp.GetSpecialType(SpecialType.System_Int32)));
            Assert.Equal("C<System.Int32[]>", cOfIntArray.ToTestDisplayString());

            derivedTypeInfo = cOfIntArray.GetClosedDerivedTypeInfo(CancellationToken.None);
            Assert.True(derivedTypeInfo.IsComplete);
            Assert.Equal(["D1<System.Int32[]>", "D2<System.Int32>"], derivedTypeInfo.ClosedDerivedTypes.ToTestDisplayStrings());

            var cOfString = classC.Construct(comp.GetSpecialType(SpecialType.System_String));
            Assert.Equal("C<System.String>", cOfString.ToTestDisplayString());
            derivedTypeInfo = cOfString.GetClosedDerivedTypeInfo(CancellationToken.None);
            Assert.True(derivedTypeInfo.IsComplete);
            Assert.Equal(["D1<System.String>", "D3"], derivedTypeInfo.ClosedDerivedTypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Sealed_01()
    {
        var source = """
            sealed closed class C { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9381: 'C': a closed type cannot be sealed or static
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9381: 'C': a closed type cannot be sealed or static
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,23): error CS9384: 'C': a closed type cannot be marked abstract because it is always implicitly abstract.
            // abstract closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedExplicitlyAbstract, "C").WithArguments("C").WithLocation(1, 23));

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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0144: Cannot create an instance of the abstract type or interface 'C'
            // new C(); // 1
            Diagnostic(ErrorCode.ERR_NoNewAbstract, "new C()").WithArguments("C").WithLocation(1, 1));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
        Assert.True(classC.IsClosed);

        var referenceComp = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (1,14): error CS9382: 'D': cannot use a closed type 'C' from another assembly as a base type.
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], options: TestOptions.DebugModule, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();

        var source2 = """
            public class D : C { }
            """;
        var comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (1,14): error CS9382: 'D': cannot use a closed type 'C' from another assembly as a base type.
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (1,14): error CS9382: 'E': cannot use a closed type 'D' from another assembly as a base type.
                // public class E : D { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "D").WithLocation(1, 14));
        }
    }

    [Fact]
    public void BaseTypeFromMetadata_06()
    {
        // Attempt to inherit a closed class which is accessible due to an IVT
        var source1 = """
            using System.Runtime.CompilerServices;
            [assembly: InternalsVisibleTo("Consumer")]

            internal closed class C { }
            """;
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics();
        verifyReference(comp1.ToMetadataReference());
        verifyReference(comp1.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var source2 = """
                internal class E : C { }
                """;
            var comp2 = CreateCompilation(source2, references: [reference], targetFramework: TargetFramework.Net100, assemblyName: "Consumer");
            comp2.VerifyEmitDiagnostics(
                // (1,16): error CS9382: 'E': cannot use a closed type 'C' from another assembly as a base type.
                // internal class E : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "C").WithLocation(1, 16));
        }
    }

    [Fact]
    public void CompilerFeatureRequired_NonClosedContainingType()
    {
        // Constructor has CompilerFeatureRequired("ClosedClasses") yet containing type lacks IsClosedTypeAttribute
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute], options: TestOptions.UnsafeDebugDll);

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.False(classC.TryGetClosedSubtypes(out _));

        CompileAndVerify(comp1, symbolValidator: verifyMetadata).VerifyDiagnostics();
        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1<>), typeof(D2<>), typeof(D3<>)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
    }

    [Fact]
    public void GenericSubtype_02()
    {
        // Type parameter is not used in base
        var source1 = """
            public closed class C { }
            public class D<T> : C { }
            """;
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (2,14): error CS9383: 'D<T>': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            // public class D<T> : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("D<T>", "T", "C").WithLocation(2, 14));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.False(classC.TryGetClosedSubtypes(out _));
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute]);

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["Outer<T>.D"], subtypes.ToTestDisplayStrings());

        CompileAndVerify(comp1, symbolValidator: verifyMetadata).VerifyDiagnostics();
        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(Outer<>.D)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (5,18): error CS9383: 'Outer<T>.D': The type parameter 'T' must be referenced in the base type 'C' because the base type is closed.
            //     public class D : C { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<T>.D", "T", "C").WithLocation(5, 18));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.False(classC.TryGetClosedSubtypes(out _));
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute]);

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D"], subtypes.ToTestDisplayStrings());

        CompileAndVerify(comp1, symbolValidator: verifyMetadata).VerifyDiagnostics();
        void verifyMetadata(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var peType = (PENamedTypeSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D)})"
                ],
                GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
        }
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,11): error CS9383: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U5' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U5", "C<U1, U2, U4, U6>").WithLocation(4, 11),
            // (4,11): error CS9383: 'Outer<U1, U2, U3>.D<U4, U5, U6>': The type parameter 'U3' must be referenced in the base type 'C<U1, U2, U4, U6>' because the base type is closed.
            //     class D<U4, U5, U6> : C<U1, U2, U4, U6> { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "D").WithArguments("Outer<U1, U2, U3>.D<U4, U5, U6>", "U3", "C<U1, U2, U4, U6>").WithLocation(4, 11));

        var classC = comp1.GetMember<NamedTypeSymbol>("C");
        Assert.False(classC.TryGetClosedSubtypes(out _));
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (5,14): error CS9383: 'F<T>': The type parameter 'T' must be referenced in the base type 'E' because the base type is closed.
            // closed class F<T> : E { }
            Diagnostic(ErrorCode.ERR_UnderspecifiedClosedSubtype, "F").WithArguments("F<T>", "T", "E").WithLocation(5, 14));

        Assert.False(comp1.GetMember<NamedTypeSymbol>("C").TryGetClosedSubtypes(out _));
        Assert.True(comp1.GetMember<NamedTypeSymbol>("D").TryGetClosedSubtypes(out var subtypes));
        Assert.Empty(subtypes);

        var classE = comp1.GetMember<NamedTypeSymbol>("E");
        Assert.False(classE.TryGetClosedSubtypes(out _));

        Assert.True(comp1.GetMember<NamedTypeSymbol>("F").TryGetClosedSubtypes(out subtypes));
        Assert.Empty(subtypes);
    }

    [Fact]
    public void ConsumeFromVB_01()
    {
        var source1 = """
            public closed class C { }
            """;
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();

        var source2 = """
            Public Class D
                Inherits C
            End Class
            """;
        CreateVisualBasicCompilation("Program", source2, referencedCompilations: [comp1], referencedAssemblies: comp1.References).VerifyEmitDiagnostics(
            Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, "D").WithArguments("Protected Overloads Sub New()", "ClosedClasses").WithLocation(1, 14));
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
        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();

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
    public void IsClosedTypeAttributeExplicitUsage()
    {
        var source1 = """
            #pragma warning disable CS0067 // The event is never used
            using System.Runtime.CompilerServices;

            [assembly: IsClosedType] // 1
            [module: IsClosedType] // 2

            [IsClosedType] public class C // 3
            {
                [IsClosedType] public C() { } // 4
                [IsClosedType] public void M() { } // 5
                [IsClosedType] public string P { get; set; } // 6
                [IsClosedType] public string F; // 7
                [IsClosedType] public event System.Action E; // 8

                public void M1([IsClosedType] int param) { } // 9
                [return: IsClosedType] public int M2() => 0; // 10
                public void M3<[IsClosedType] T>() { } // 11
            }
            [IsClosedType] public struct S { } // 12
            [IsClosedType] public enum E { } // 13
            [IsClosedType] public interface I { } // 14
            [IsClosedType] public delegate void D(); // 15
            """;

        var isClosedTypeAttributeAllowingAllTargets = """
            namespace System.Runtime.CompilerServices
            {
                public sealed class IsClosedTypeAttribute : Attribute { }
            }
            """;

        var comp1 = CreateCompilation([source1, isClosedTypeAttributeAllowingAllTargets], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,12): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [assembly: IsClosedType] // 1
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(4, 12),
            // (5,10): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [module: IsClosedType] // 2
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(5, 10),
            // (7,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public class C // 3
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(7, 2),
            // (9,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [IsClosedType] public C() { } // 4
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(9, 6),
            // (10,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [IsClosedType] public void M() { } // 5
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(10, 6),
            // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [IsClosedType] public string P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(11, 6),
            // (12,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [IsClosedType] public string F; // 7
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(12, 6),
            // (13,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [IsClosedType] public event System.Action E; // 8
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(13, 6),
            // (15,21): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     public void M1([IsClosedType] int param) { } // 9
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(15, 21),
            // (16,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     [return: IsClosedType] public int M2() => 0; // 10
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(16, 14),
            // (17,21): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            //     public void M3<[IsClosedType] T>() { } // 11
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(17, 21),
            // (19,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public struct S { } // 12
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(19, 2),
            // (20,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public enum E { } // 13
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(20, 2),
            // (21,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public interface I { } // 14
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(21, 2),
            // (22,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public delegate void D(); // 15
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(22, 2));

        // Note: ERR_AttributeOnBadSymbolType causes well-known attribute decoding to be skipped.
        // So, ERR_ExplicitReservedAttr is only reported for the class attribute in this case.
        comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (4,12): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [assembly: IsClosedType] // 1
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(4, 12),
            // (5,10): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [module: IsClosedType] // 2
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(5, 10),
            // (7,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsClosedTypeAttribute'. This is reserved for compiler usage.
            // [IsClosedType] public class C // 3
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsClosedType").WithArguments("System.Runtime.CompilerServices.IsClosedTypeAttribute").WithLocation(7, 2),
            // (9,6): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [IsClosedType] public C() { } // 4
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(9, 6),
            // (10,6): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [IsClosedType] public void M() { } // 5
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(10, 6),
            // (11,6): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [IsClosedType] public string P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(11, 6),
            // (12,6): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [IsClosedType] public string F; // 7
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(12, 6),
            // (13,6): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [IsClosedType] public event System.Action E; // 8
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(13, 6),
            // (15,21): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     public void M1([IsClosedType] int param) { } // 9
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(15, 21),
            // (16,14): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     [return: IsClosedType] public int M2() => 0; // 10
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(16, 14),
            // (17,21): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            //     public void M3<[IsClosedType] T>() { } // 11
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(17, 21),
            // (19,2): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [IsClosedType] public struct S { } // 12
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(19, 2),
            // (20,2): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [IsClosedType] public enum E { } // 13
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(20, 2),
            // (21,2): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [IsClosedType] public interface I { } // 14
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(21, 2),
            // (22,2): error CS0592: Attribute 'IsClosedType' is not valid on this declaration type. It is only valid on 'class' declarations.
            // [IsClosedType] public delegate void D(); // 15
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IsClosedType").WithArguments("IsClosedType", "class").WithLocation(22, 2));
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
        var verifier = CompileAndVerify([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100, symbolValidator: verifyMetadataSymbols, verify: Verification.FailsPEVerify);
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
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {})"
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
                // (3,7): error CS9382: 'D': cannot use a closed type 'C' from another assembly as a base type.
                // class D : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "D").WithArguments("D", "C").WithLocation(3, 7),
                // (4,7): error CS9382: 'E': cannot use a closed type 'C' from another assembly as a base type.
                // class E() : C { }
                Diagnostic(ErrorCode.ERR_ClosedBaseTypeBaseFromOtherAssembly, "E").WithArguments("E", "C").WithLocation(4, 7),
                // (5,7): error CS9382: 'F': cannot use a closed type 'C' from another assembly as a base type.
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

        var verifier = CompileAndVerify([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100, symbolValidator: verifyMetadataSymbols, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        void verifyMetadataSymbols(ModuleSymbol module)
        {
            var peModule = (PEModuleSymbol)module;
            var classC = peModule.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

            // Get attributes from metadata without doing any filtering
            AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.RequiredMemberAttribute",
                    "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {})"
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

        var verifier = CompileAndVerify([source, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute], sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C", classC.ToTestDisplayString());
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());

            if (module is PEModuleSymbol peModule)
            {
                var peType = (PENamedTypeSymbol)classC;
                // Get attributes from metadata without doing any filtering
                AssertEx.SetEqual([
                        "System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2)})"
                    ],
                    GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
            }
        }
    }

    [Fact]
    public void Subtypes_Retargeting_01()
    {
        var retargetedDependencySource = """
            public class R { }
            """;

        var dependencyV1 = CreateCompilation(
            new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true),
            retargetedDependencySource,
            TargetFrameworkUtil.StandardReferences);

        var source = """
            public closed class C
            {
                public R P { get; } = new R();
            }

            public class D1 : C { }
            public class D2 : C { }
            """;

        var sourceComp = CreateCompilation(
            [source, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute],
            references: [dependencyV1.ToMetadataReference()],
            targetFramework: TargetFramework.Standard);
        sourceComp.VerifyEmitDiagnostics();

        var dependencyV2 = CreateCompilation(
            new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true),
            retargetedDependencySource,
            TargetFrameworkUtil.StandardReferences);

        var comp = CreateCompilation(
            "",
            references: [sourceComp.ToMetadataReference(), dependencyV2.ToMetadataReference()],
            targetFramework: TargetFramework.Standard);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetTypeByMetadataName("C");
        Assert.IsType<RetargetingNamedTypeSymbol>(classC);

        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>", "D2"], subtypes.ToTestDisplayStrings());

            var cOfInt = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.Int32>", cOfInt.ToTestDisplayString());
            Assert.True(cOfInt.TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<System.Int32>", "D2"], subtypes.ToTestDisplayStrings());

            var cOfString = classC.Construct(comp.GetSpecialType(SpecialType.System_String));
            Assert.Equal("C<System.String>", cOfString.ToTestDisplayString());
            Assert.True(cOfString.TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<System.String>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C<T>", classC.ToTestDisplayString());
            Assert.False(classC.TryGetClosedSubtypes(out _));

            var immutableArrayOfInt = comp
                .GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T)
                .Construct(comp.GetSpecialType(SpecialType.System_Int32));

            var cOfImmutableArray = classC.Construct(immutableArrayOfInt);
            Assert.Equal("C<System.Collections.Immutable.ImmutableArray<System.Int32>>", cOfImmutableArray.ToTestDisplayString());
            Assert.True(cOfImmutableArray.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<System.Collections.Immutable.ImmutableArray<System.Int32>>", "D2<System.Int32>"], subtypes.ToTestDisplayStrings());

            var cOfInt = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.Int32>", cOfInt.ToTestDisplayString());
            Assert.True(cOfInt.TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<System.Int32>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Subtypes_04()
    {
        // Verify that TryGetClosedSubtypes API behaves reasonably in base type cycle scenario.
        var source = """
            using System.Collections.Immutable;

            closed class C<T> : D<T>
            {
            }

            closed class D<T> : C<T>
            {
            }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (3,14): error CS0146: Circular base type dependency involving 'D<T>' and 'C<T>'
            // closed class C<T> : D<T>
            Diagnostic(ErrorCode.ERR_CircularBase, "C").WithArguments("D<T>", "C<T>").WithLocation(3, 14),
            // (7,14): error CS0146: Circular base type dependency involving 'C<T>' and 'D<T>'
            // closed class D<T> : C<T>
            Diagnostic(ErrorCode.ERR_CircularBase, "D").WithArguments("C<T>", "D<T>").WithLocation(7, 14));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("C<T>", classC.ToTestDisplayString());
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Empty(subtypes);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        verify(comp);
        verify(CreateCompilation([], references: [comp.ToMetadataReference()], targetFramework: TargetFramework.Net100));
        verify(CreateCompilation([], references: [comp.EmitToImageReference()], targetFramework: TargetFramework.Net100));

        void verify(CSharpCompilation comp)
        {
            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("C<T1, T2>", classC.ToTestDisplayString());
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T1>"], subtypes.ToTestDisplayStrings());

            var cOfStringInt = classC.Construct(comp.GetSpecialType(SpecialType.System_String), comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C<System.String, System.Int32>", cOfStringInt.ToTestDisplayString());
            Assert.True(cOfStringInt.TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<System.String>"], subtypes.ToTestDisplayStrings());

            var cOfIntString = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32), comp.GetSpecialType(SpecialType.System_String));
            Assert.Equal("C<System.Int32, System.String>", cOfIntString.ToTestDisplayString());
            Assert.True(cOfIntString.TryGetClosedSubtypes(out subtypes));
            Assert.Empty(subtypes);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_07()
    {
        // nested hierarchy in nested type declarations
        var source = """
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
                        Container.E1 => 1,
                        Container.F1 => 2,
                        Container.E2 => 3,
                        Container.F2 => 4,
                    };
                }
            }

            closed class C
            {
            }

            class Container
            {
                public closed class D1 : C { }
                public class E1 : D1 { }
                public class F1 : D1 { }

                public closed class D2 : C { }
                public class E2 : D2 { }
                public class F2 : D2 { }
            }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Container.E1' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Container.E1").WithLocation(5, 18));
    }

    [Fact]
    public void Exhaustiveness_CSharp14_01()
    {
        // Old C# versions treat a closed class, the same as a non-closed class, for pattern matching purposes.
        // Note: since subtypes do not subsume the base type under old LangVersion, it means certain patterns could go from valid to erroneous on upgrade.
        var source1 = """
            public closed class C
            {
            }

            public class D1 : C { }
            public class D2 : C { }
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
            #line 100
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                    };
                }

                int M2(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        C => 3,
                    };
                }

                int M3(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        C => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // public closed class C
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 21),
            // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(100, 18));

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp0.VerifyEmitDiagnostics();

        var comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.ToMetadataReference()], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(100, 18));

        comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.EmitToImageReference()], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(100, 18));

        comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.ToMetadataReference()], parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (113,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             C => 3,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "C").WithLocation(113, 13));

        comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.EmitToImageReference()], parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.Net100);
        comp1.VerifyEmitDiagnostics(
            // (113,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             C => 3,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "C").WithLocation(113, 13));
    }

    [Fact]
    public void Exhaustiveness_CSharp14_02()
    {
        // Pattern matching a union of closed classes in old LangVersion
        var source1 = """
            public union U(C1, C2);

            public closed class C1;
            public class D1 : C1 { }

            public closed class C2;
            public class D2 : C2 { }
            """;

        var source2 = """
            class Program
            {
                int M1(U u)
                {
                    return u switch
                    {
                        D1 => 1,
                        D2 => 2,
                    };
                }

                int M2(U u)
                {
                    return u switch
                    {
                        C1 => 1,
                        C2 => 2,
                    };
                }
            }
            """;

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp0.VerifyDiagnostics();

        var comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.ToMetadataReference()], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp1.VerifyDiagnostics(
            // (7,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             D1 => 1,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "D1").WithArguments("unions").WithLocation(7, 13),
            // (8,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "D2").WithArguments("unions").WithLocation(8, 13),
            // (16,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             C1 => 1,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C1").WithArguments("unions").WithLocation(16, 13),
            // (17,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             C2 => 2,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C2").WithArguments("unions").WithLocation(17, 13));

        comp1 = CreateCompilation([source2, IsClosedTypeAttributeDefinition], references: [comp0.EmitToImageReference()], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net100);
        comp1.VerifyDiagnostics(
            // (7,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             D1 => 1,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "D1").WithArguments("unions").WithLocation(7, 13),
            // (8,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "D2").WithArguments("unions").WithLocation(8, 13),
            // (16,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             C1 => 1,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C1").WithArguments("unions").WithLocation(16, 13),
            // (17,13): error CS8652: The feature 'unions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //             C2 => 2,
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C2").WithArguments("unions").WithLocation(17, 13));
    }

    [Fact]
    public void Exhaustiveness_UnionOfClosedClasses_01()
    {
        // Union with closed classes as case types
        var source = """
            class Program
            {
                int M1(U u)
                {
                    return u switch
                    {
                        E1 => 1,
                        F1 => 2,
                        E2 => 3,
                        F2 => 4,
                    };
                }

                int M2(U u)
                {
                    return u switch
                    {
                        E1 => 1,
                        F1 => 2,
                        E2 => 3,
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

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (16,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'F2' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("F2").WithLocation(16, 18));
    }

    [Fact]
    public void Exhaustiveness_UnionOfClosedClasses_02()
    {
        // Union including both base and derived of a hierarchy
        var source = """
            class Program
            {
                int M1(U u)
                {
            #line 100
                    return u switch
                    {
                        E1 => 1,
                    };
                }

                int M2(U u)
                {
                    return u switch
                    {
                        E1 => 1,
                        F1 => 2,
                    };
                }
            }

            union U(D1, C);

            closed class C { }

            closed class D1 : C { }
            class E1 : D1 { }
            class F1 : D1 { }
            """;

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'F1' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("F1").WithLocation(100, 18));
    }

    [Fact]
    public void Exhaustiveness_UnionOfClosedClasses_03()
    {
        // Union of closed classes with no subtypes
        var source = """
            class Program
            {
                int M1(U u)
                {
            #line 100
                    return u switch
                    {
                    };
                }

                int M2(U u)
                {
            #line 200
                    return u switch
                    {
                        C1 => 1,
                    };
                }
            }

            union U(C1, C2);

            closed class C1;
            closed class C2;
            """;

        var comp = CreateCompilation([source, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(100, 18),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C2' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C2").WithLocation(200, 18));
    }

    [Fact]
    public void Exhaustiveness_ClosedClassCustomUnion_01()
    {
        // Test a class which is both closed and a union.
        // The "union-ness" wins over the "closed-ness" in terms of pattern matching behavior.
        var source1 = """
            #nullable enable
            using System.Runtime.CompilerServices;

            [Union]
            public closed class MyUnion
            {
                public MyUnion(string value) => Value = value;
                public MyUnion(int value) => Value = value;

                public object? Value { get; }
            }

            public sealed class D1() : MyUnion(0);
            public sealed class D2() : MyUnion("a");
            """;

        var source2 = """
            class Program
            {
                public int Match1(MyUnion u)
                    => u switch
                    {
                        string => 1,
                        int => 2,
                    };

                public int Match2(MyUnion u)
            #line 100
                    => u switch
                    {
                        int => 2,
                    };

                public int Match3(MyUnion u)
                    => u switch
                    {
            #line 200
                        D1 => 1,
                        D2 => 2,
                    };

                public int Match4(MyUnion u)
                    => u switch
                    {
            #line 300
                        D2 => 2,
                    };
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
            //         => u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(100, 14),
            // (200,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D1'.
            //             D1 => 1,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D1").WithArguments("MyUnion", "D1").WithLocation(200, 13),
            // (201,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D2'.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D2").WithArguments("MyUnion", "D2").WithLocation(201, 13),
            // (300,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D2'.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D2").WithArguments("MyUnion", "D2").WithLocation(300, 13));
    }

    [Fact]
    public void Exhaustiveness_ClosedClassCustomUnion_02()
    {
        // Test a class which is both closed and a union, and has no union case types.

        // #nullable enable
        // using System.Runtime.CompilerServices;
        //   
        // [Union]
        // public closed class MyUnion
        // {
        //     public object? Value { get; }
        // }
        //   
        // public sealed class D1 : MyUnion;
        // public sealed class D2 : MyUnion;
        var ilSource = """
.class public auto ansi abstract beforefieldinit MyUnion
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 02 00 00
    )
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )
    .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void System.Runtime.CompilerServices.UnionAttribute::.ctor() = (
        01 00 00 00
    )

    .field private initonly object '<Value>k__BackingField'

    .method public hidebysig specialname 
        instance object get_Value () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldfld object MyUnion::'<Value>k__BackingField'
        IL_0006: ret
    }

    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
            00 00
        )
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }

    .property instance object Value()
    {
        .get instance object MyUnion::get_Value()
    }
}

.class public auto ansi sealed beforefieldinit D1
    extends MyUnion
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void MyUnion::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi sealed beforefieldinit D2
    extends MyUnion
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void MyUnion::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsClosedTypeAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 04 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.UnionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
""";

        var source2 = """
            class Program
            {
                public int Match1(MyUnion u)
                    => u switch
                    {
            #line 100
                        MyUnion => 1
                    };

                public int Match2(MyUnion u)
                    => u switch
                    {
            #line 200
                        object => 1
                    };

                public int Match3(MyUnion u)
                    => u switch
                    {
                        not null => 1
                    };

                public int Match4(MyUnion u)
                    => u switch
                    {
            #line 300
                        D1 => 1,
                        D2 => 2,
                    };

                public int Match5(MyUnion u)
                    => u switch
                    {
            #line 400
                        D2 => 2,
                    };
            }
            """;

        var comp = CreateCompilationWithIL(source2, ilSource, targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'MyUnion'.
            //             MyUnion => 1
            Diagnostic(ErrorCode.ERR_PatternWrongType, "MyUnion").WithArguments("MyUnion", "MyUnion").WithLocation(100, 13),
            // (200,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'object'.
            //             object => 1
            Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("MyUnion", "object").WithLocation(200, 13),
            // (300,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D1'.
            //             D1 => 1,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D1").WithArguments("MyUnion", "D1").WithLocation(300, 13),
            // (301,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D2'.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D2").WithArguments("MyUnion", "D2").WithLocation(301, 13),
            // (400,13): error CS8121: An expression of type 'MyUnion' cannot be handled by a pattern of type 'D2'.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D2").WithArguments("MyUnion", "D2").WithLocation(400, 13));
    }

    [Fact]
    public void Exhaustiveness_ClosedClassCustomUnion_03()
    {
        // Test scenario where a subclass of closed class is a custom union.
        // Demonstrate that the "expanded set" of closed subtypes does not include union case types.
        var source1 = """
            #nullable enable
            using System.Runtime.CompilerServices;

            public closed class C;

            public sealed class D1 : C;

            [Union]
            public sealed class D2 : C
            {
                public D2(string value) => Value = value;
                public D2(int value) => Value = value;

                public object? Value { get; }
            }
            """;

        var source2 = """
            class Program
            {
                public int Match1(C c)
                    => c switch
                    {
                        D1 => 1,
                        D2 => 2,
                    };

                public int Match2(C c)
                    => c switch
                    {
                        D1 => 1,
            #line 100
                        string => 2,
                        int => 3,
                        D2 => 2,
                    };

                public int Match3(C c)
                    => c switch
                    {
                        D1 => 1,
                        D2 and string => 2,
                        D2 and int => 3,
                    };

                public int Match4(C c)
                    => c switch
                    {
                        D1 => 1,
                        D2 and string => 2,
            #line 200
                        int => 3,
                    };

                public int Match5(C c)
            #line 300
                    => c switch
                    {
                        D1 => 1,
                        D2 and string => 2,
                    };
            }
            """;

        // https://github.com/dotnet/roslyn/issues/83617: The pattern `int` suggested for line 300 is invalid. A pattern like `D2` or `D2 and int` should be suggested instead.
        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,13): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'string'.
            //             string => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("C", "string").WithLocation(100, 13),
            // (101,13): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'int'.
            //             int => 3,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("C", "int").WithLocation(101, 13),
            // (200,13): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'int'.
            //             int => 3,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("C", "int").WithLocation(200, 13),
            // (300,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'int' is not covered.
            //         => c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("int").WithLocation(300, 14));
    }

    [Fact]
    public void Exhaustiveness_Generic_01()
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();
    }

    /// <summary>Tests an exhaustiveness scenario similar to <see cref="Subtypes_03"/>.</summary>
    [Fact]
    public void Exhaustiveness_Generic_02()
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

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (7,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<T>' is not covered.
            //         return item switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<T>").WithLocation(7, 21));
    }

    [Fact]
    public void Exhaustiveness_Generic_03()
    {
        // Indirect subtype is unspeakable
        var source = """
            using System.Collections.Immutable;

            closed class C<T>
            {
                public static int Use1(C<T> item)
                {
                    return item switch
                    {
                        D1<T> => 1,
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

                public static int Use3(C<T> item)
                {
                    return item switch
                    {
                        D1<T> => 1,
                        D2<T> => 2
                    };
                }
            }

            class D1<U> : C<U> { }

            closed class D2<U> : C<U> { }
            class E2<V> : D2<ImmutableArray<V>> { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (7,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2<T>' is not covered.
            //         return item switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2<T>").WithLocation(7, 21));
    }

    [Fact]
    public void Exhaustiveness_Generic_04()
    {
        // Unspeakable subtypes along two indirections
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

            closed class D2<U> : C<ImmutableArray<U>> { }
            class E2<V> : D2<ImmutableArray<V>> { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (7,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<T>' is not covered.
            //         return item switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<T>").WithLocation(7, 21));
    }

    [Fact]
    public void Exhaustiveness_Generic_NonGenericSubtypes_01()
    {
        // Base type is generic and subtypes are non-generic
        var source = """
            closed class C<T>;
            class D1 : C<string>;
            class D2 : C<int>;

            class Program
            {
                int Match1(C<int> c) =>
            #line 100
                    c switch
                    {
                    };

                int Match2(C<int> c) =>
                    c switch
                    {
                        D2 => 2,
                    };

                int Match3(C<int> c) =>
                    c switch
                    {
            #line 200
                        D1 => 2,
                    };

                int Match4(C<string> c) =>
                    c switch
                    {
                        D1 => 2,
                    };

                int Match5(C<string> c) =>
                    c switch
                    {
            #line 300
                        D2 => 2,
                    };

                int Match6(C<object> c) =>
            #line 400
                    c switch
                    {
                    };

                int Match7(C<object> c) =>
                    c switch
                    {
                        C<object> => 1,
                    };
            }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (100,11): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(100, 11),
            // (200,13): error CS8121: An expression of type 'C<int>' cannot be handled by a pattern of type 'D1'.
            //             D1 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D1").WithArguments("C<int>", "D1").WithLocation(200, 13),
            // (300,13): error CS8121: An expression of type 'C<string>' cannot be handled by a pattern of type 'D2'.
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_PatternWrongType, "D2").WithArguments("C<string>", "D2").WithLocation(300, 13),
            // (400,11): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<object>' is not covered.
            //         c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<object>").WithLocation(400, 11));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());

        var cOfInt = classC.Construct(comp.GetSpecialType(SpecialType.System_Int32));
        Assert.True(cOfInt.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D2"], subtypes.ToTestDisplayStrings());

        var cOfString = classC.Construct(comp.GetSpecialType(SpecialType.System_String));
        Assert.True(cOfString.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D1"], subtypes.ToTestDisplayStrings());

        var cOfObject = classC.Construct(comp.GetSpecialType(SpecialType.System_Object));
        Assert.True(cOfObject.TryGetClosedSubtypes(out subtypes));
        Assert.Empty(subtypes);
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Empty(subtypes);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));
        classC = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Empty(subtypes);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (5,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(5, 18));
        classC = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Empty(subtypes);
    }

    [Fact]
    public void Exhaustiveness_BaseTypeSubsumedBySubtypes_01()
    {
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        C => 3,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (9,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             C => 3,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "C").WithLocation(9, 13));

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp, """
            [0]: t0 is D1 ? [1] : [2]
            [1]: leaf <arm> `D1 => 1`
            [2]: t0 is D2 ? [3] : [4]
            [3]: leaf <arm> `D2 => 2`
            [4]: t0 != null ? [5] : [6]
            [5]: leaf <arm> `C => 3`
            [6]: leaf <default> `c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        C => 3,
                    }`

            """,
            forLowering: true);
    }

    [Fact]
    public void Exhaustiveness_BaseTypeSubsumedBySubtypes_02()
    {
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        _ => 3,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute]);
        comp.VerifyDiagnostics();

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp, """
            [0]: t0 is D1 ? [1] : [2]
            [1]: leaf <arm> `D1 => 1`
            [2]: t0 is D2 ? [3] : [4]
            [3]: leaf <arm> `D2 => 2`
            [4]: leaf <arm> `_ => 3`

            """,
            forLowering: true);

        var verifier = CompileAndVerify(comp);
        verifier.VerifyIL("Program.M", """
            {
              // Code size       30 (0x1e)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldarg.1
              IL_0001:  isinst     "D1"
              IL_0006:  brtrue.s   IL_0012
              IL_0008:  ldarg.1
              IL_0009:  isinst     "D2"
              IL_000e:  brtrue.s   IL_0016
              IL_0010:  br.s       IL_001a
              IL_0012:  ldc.i4.1
              IL_0013:  stloc.0
              IL_0014:  br.s       IL_001c
              IL_0016:  ldc.i4.2
              IL_0017:  stloc.0
              IL_0018:  br.s       IL_001c
              IL_001a:  ldc.i4.3
              IL_001b:  stloc.0
              IL_001c:  ldloc.0
              IL_001d:  ret
            }
            """);
    }

    [Fact]
    public void Exhaustiveness_BaseTypeSubsumedBySubtypes_03()
    {
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        null => 3,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }

            namespace System.Runtime.CompilerServices
            {
                public class SwitchExpressionException : InvalidOperationException
                {
                    public SwitchExpressionException() {}
                    public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
                    public object UnmatchedValue { get; }
                }
            }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition, CompilerFeatureRequiredAttribute]);
        comp.VerifyDiagnostics();

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp, """
            [0]: t0 is D1 ? [1] : [2]
            [1]: leaf <arm> `D1 => 1`
            [2]: t0 is D2 ? [3] : [4]
            [3]: leaf <arm> `D2 => 2`
            [4]: t0 == null ? [5] : [6]
            [5]: leaf <arm> `null => 3`
            [6]: leaf <default> `c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        null => 3,
                    }`

            """,
            forLowering: true);

        var verifier = CompileAndVerify(comp);
        verifier.VerifyIL("Program.M", """
            {
              // Code size       41 (0x29)
              .maxstack  1
              .locals init (int V_0)
              IL_0000:  ldarg.1
              IL_0001:  isinst     "D1"
              IL_0006:  brtrue.s   IL_0015
              IL_0008:  ldarg.1
              IL_0009:  isinst     "D2"
              IL_000e:  brtrue.s   IL_0019
              IL_0010:  ldarg.1
              IL_0011:  brfalse.s  IL_001d
              IL_0013:  br.s       IL_0021
              IL_0015:  ldc.i4.1
              IL_0016:  stloc.0
              IL_0017:  br.s       IL_0027
              IL_0019:  ldc.i4.2
              IL_001a:  stloc.0
              IL_001b:  br.s       IL_0027
              IL_001d:  ldc.i4.3
              IL_001e:  stloc.0
              IL_001f:  br.s       IL_0027
              IL_0021:  ldarg.1
              IL_0022:  call       "void <PrivateImplementationDetails>.ThrowSwitchExpressionException(object)"
              IL_0027:  ldloc.0
              IL_0028:  ret
            }
            """);
    }

    [Fact]
    public void Exhaustiveness_BaseTypeSubsumedBySubtypes_04()
    {
        var source = """
            class Program
            {
                int M(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        null => 3,
                        _ => 4,
                    };
                }
            }

            closed class C
            {
            }

            class D1 : C { }
            class D2 : C { }
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (10,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             _ => 4,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(10, 13));

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp, """
            [0]: t0 is D1 ? [1] : [2]
            [1]: leaf <arm> `D1 => 1`
            [2]: t0 is D2 ? [3] : [4]
            [3]: leaf <arm> `D2 => 2`
            [4]: t0 == null ? [5] : [6]
            [5]: leaf <arm> `null => 3`
            [6]: leaf <arm> `_ => 4`

            """,
            forLowering: true);
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'E' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("E").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'E' is not covered.
                //         return d switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("E").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D", "F"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1", "Container.D2"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(200, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,13): error CS0122: 'D2' is inaccessible due to its protection level
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_BadAccess, "D2").WithArguments("D2").WithLocation(100, 13),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(200, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,13): error CS0122: 'D2' is inaccessible due to its protection level
            //             D2 => 2,
            Diagnostic(ErrorCode.ERR_BadAccess, "D2").WithArguments("D2").WithLocation(100, 13),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(200, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_03()
    {
        // Inaccessible subtype should still be imported from metadata to allow checking exhaustiveness
        var source1 = """
            public class Container
            {
                public closed class C;
                private class D : C;
            }
            """;

        var source2 = """
            class Program
            {
                int M1(Container.C c)
                {
                    return c switch
                    {
            #line 100
                        Container.D => 1,
                    };
                }

                int M2(Container.C c)
                {
            #line 200
                    return c switch
                    {
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        var imageReference = comp0.EmitToImageReference();
        foreach (var importOptions in new[] { MetadataImportOptions.Public, MetadataImportOptions.Internal, MetadataImportOptions.All })
        {
            comp = CreateCompilation([source2], references: [imageReference], options: TestOptions.DebugDll.WithMetadataImportOptions(importOptions), targetFramework: TargetFramework.Net100);
            verify(comp);
        }

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,23): error CS0122: 'Container.D' is inaccessible due to its protection level
                //             Container.D => 1,
                Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("Container.D").WithLocation(100, 23),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Container.C' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Container.C").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("Container.C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["Container.D"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_04()
    {
        // Less accessible indirect subtype
        var source1 = """
            public closed class C;
            public class D1 : C;
            public closed class D2 : C;

            public class Container
            {
                protected class E2 : D2;
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
                        Container.E2 => 2,
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

                int M3(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,23): error CS0122: 'Container.E2' is inaccessible due to its protection level
                //             Container.E2 => 2,
                Diagnostic(ErrorCode.ERR_BadAccess, "E2").WithArguments("Container.E2").WithLocation(100, 23),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D2' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D2").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1", "D2"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_05()
    {
        // Less accessible subtypes across two indirections
        var source1 = """
            public closed class C;
            public class D1 : C;

            public class Container
            {
                protected closed class D2 : C;
                protected class E2 : D2;
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
                        Container.E2 => 2,
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,23): error CS0122: 'Container.E2' is inaccessible due to its protection level
                //             Container.E2 => 2,
                Diagnostic(ErrorCode.ERR_BadAccess, "E2").WithArguments("Container.E2").WithLocation(100, 23),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1", "Container.D2"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_LessAccessibleSubtype_06()
    {
        // Entire hierarchy is inaccessible
        var source1 = """
            public class Container
            {
                protected closed class C;
                protected class D1 : C;

                protected closed class D2 : C;
                protected class E2 : D2;
            }
            """;

        var source2 = """
            class Program
            {
            #line 100
                int M1(Container.C c)
                {
            #line 200
                    return c switch
                    {
                    };
                }

            #line 300
                int M2(Container.C c)
                {
                    return c switch
                    {
            #line 400
                        Container.D1 => 1
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,22): error CS0122: 'Container.C' is inaccessible due to its protection level
                //     int M1(Container.C c)
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("Container.C").WithLocation(100, 22),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
                // (300,22): error CS0122: 'Container.C' is inaccessible due to its protection level
                //     int M2(Container.C c)
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("Container.C").WithLocation(300, 22),
                // (400,23): error CS0122: 'Container.D1' is inaccessible due to its protection level
                //             Container.D1 => 1
                Diagnostic(ErrorCode.ERR_BadAccess, "D1").WithArguments("Container.D1").WithLocation(400, 23));

            var classC = comp.GetMember<NamedTypeSymbol>("Container.C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["Container.D1", "Container.D2"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_MatchInterface_01()
    {
        // Exhaust an inaccessible subtype by matching an interface that it implements
        var source1 = """
            public closed class C;
            public class D1 : C;

            public interface I2;

            public class Container
            {
                protected class D2 : C, I2;
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
                        I2 => 2,
                    };
                }

                int M2(C c)
                {
            #line 200
                    return c switch
                    {
                        I2 => 2,
                    };
                }

                int M3(C c)
                {
            #line 300
                    return c switch
                    {
                        D1 => 2,
                    };
                }

                int M4(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        I2 => 2,
            #line 400
                        C => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1").WithLocation(200, 18),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C").WithLocation(300, 18),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             C => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "C").WithLocation(400, 13));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1", "Container.D2"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_MatchInterface_02()
    {
        // Exhaust an inaccessible subtype by matching an interface that it implements
        var source1 = """
            public closed class B { }
            public interface I1 { }
            public interface I2 { }

            public class Container
            {
                private class D1 : B, I1 { }
                private class D2 : B, I2 { }
            }
            """;

        var source2 = """
            class Program
            {
                int M1(B b)
                {
                    return b switch
                    {
                        I1 i1 => 1,
                        I2 i2 => 2
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_MatchInterface_03()
    {
        // Matching an interface implemented by all subtypes exhausts the hierarchy.
        // Despite this, the base type is not convertible to the interface type.
        var source1 = """
            public closed class C;
            public class D1 : C, I;
            public class D2 : C, I;

            public closed class D3 : C;
            public class E1 : D3, I;
            public class E2 : D3, I;

            public interface I;
            """;

        var source2 = """
            class Program
            {
                int M1(C c)
                {
                    return c switch
                    {
                        D1 => 1,
                        D2 => 2,
                        E1 => 3,
                        E2 => 4,
                    };
                }

                int M2(C c)
                {
                    return c switch
                    {
                        I => 1,
                    };
                }

                int M3(C c)
                {
                    return c switch
                    {
                        I => 1,
            #line 100
                        C => 2,
                    };
                }

            #line 200
                I M4(C c) => c;
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp1 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation([source2], references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        comp2 = CreateCompilation([source2], references: [comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp2);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             C => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "C").WithLocation(100, 13),
                // (200,18): error CS0266: Cannot implicitly convert type 'C' to 'I'. An explicit conversion exists (are you missing a cast?)
                //     I M4(C c) => c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c").WithArguments("C", "I").WithLocation(200, 18));
        }
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>", "D2<T>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_02()
    {
        // Subtype definition constraints which do not "overlap" with constructed closed type using type parameters
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>", "D2<T>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<string>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<string>").WithLocation(200, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>", "D2<T>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,14): error CS0452: The type 'U2' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
            // public class D2<U2> : C<U2> where U2 : struct;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "D2").WithArguments("C<T>", "T", "U2").WithLocation(100, 14),
            // (200,16): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
            //             D2<string> => 2,
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("D2<U2>", "U2", "string").WithLocation(200, 16),
            // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<string>' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<string>").WithLocation(300, 18));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D1<T>", "D2<T>"], subtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (200,16): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
            //             D2<string> => 2,
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("D2<U2>", "U2", "string").WithLocation(200, 16),
            // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<string>' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<string>").WithLocation(300, 18));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D1<T>", "D2<T>"], subtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_Constraints_05()
    {
        // Pattern input type itself violates constraints. Do not suggest a more base type than the input type.
        var source1 = """
            public closed class C<T>;
            public closed class D1<U1> : C<U1> where U1 : class;
            public class D2<U2> : D1<U2> where U2 : class;
            public class D3<U3> : D1<U3> where U3 : class;
            """;

        var source2 = """
            class Program
            {
            #line 100
                public D1<int> d1 = null!;

                int M1()
                {
            #line 200
                    return d1 switch
                    {
                    };
                }

                int M2()
                {
            #line 300
                    return d1 switch
                    {
            #line 400
                        D2<int> => 1,
                    };
                }

                int M3()
                {
                    return d1 switch
                    {
            #line 500
                        D1<int> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,20): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1>'
                //     public D1<int> d1 = null!;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "d1").WithArguments("D1<U1>", "U1", "int").WithLocation(100, 20),
                // (200,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int>' is not covered.
                //         return d1 switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int>").WithLocation(200, 19),
                // (300,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int>' is not covered.
                //         return d1 switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int>").WithLocation(300, 19),
                // (400,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
                //             D2<int> => 1,
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<U2>", "U2", "int").WithLocation(400, 16),
                // (500,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1>'
                //             D1<int> => 1,
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<U1>", "U1", "int").WithLocation(500, 16));
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_06()
    {
        // A nested pattern input type itself violates constraints. Do not suggest a more base type than the input type.
        var source1 = """
            public closed class C<T>;
            public closed class D1<U1> : C<U1> where U1 : class;
            public class D2<U2> : D1<U2> where U2 : class;
            public class D3<U3> : D1<U3> where U3 : class;

            public class Container<X1> where X1 : class
            {
                public D1<X1> Value;
            }
            """;

        var source2 = """
            class Program
            {
            #line 100
                public Container<int> c = null!;

                int M1()
                {
            #line 200
                    return c switch
                    {
                    };
                }

                int M2()
                {
            #line 300
                    return c switch
                    {
            #line 400
                        { Value: D2<int> } => 1,
                    };
                }

                int M3()
                {
                    return c switch
                    {
            #line 500
                        { Value: D1<int> } => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,27): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'X1' in the generic type or method 'Container<X1>'
                //     public Container<int> c = null!;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "c").WithArguments("Container<X1>", "X1", "int").WithLocation(100, 27),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Value: D1<int> }' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Value: D1<int> }").WithLocation(300, 18),
                // (400,25): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U2' in the generic type or method 'D2<U2>'
                //             { Value: D2<int> } => 1,
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<U2>", "U2", "int").WithLocation(400, 25),
                // (500,25): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1>'
                //             { Value: D1<int> } => 1,
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<U1>", "U1", "int").WithLocation(500, 25)

            );
        }
    }

    [Fact]
    public void Exhaustiveness_Constraints_07()
    {
        // A nested pattern input type violates constraints.
        // The subtype has a different construction of the base type than the pattern input type.
        var source1 = """
            public closed class C<T1, T2>;
            public closed class D1<U1, U2> : C<U1, U2> where U1 : class where U2 : class;

            #line 100
            public class D2<V1> : D1<int, V1> where V1 : class;

            public class D3<V2, V3> : D1<V2, V3> where V2 : class where V3 : class;
            """;

        var source2 = """
            class Program<X1, X2>
            {
            #line 200
                public D1<X1, X2> d1 = null!;

                int M1()
                {
            #line 300
                    return d1 switch
                    {
                    };
                }

                int M2()
                {
            #line 400
                    return d1 switch
                    {
            #line 500
                        D2<X2> => 1,
                    };
                }

                int M3()
                {
                    return d1 switch
                    {
            #line 600
                        D1<X1, X2> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,14): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1, U2>'
            // public class D2<V1> : D1<int, V1> where V1 : class;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "D2").WithArguments("D1<U1, U2>", "U1", "int").WithLocation(100, 14),
            // (200,23): error CS0452: The type 'X1' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1, U2>'
            //     public D1<X1, X2> d1 = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "d1").WithArguments("D1<U1, U2>", "U1", "X1").WithLocation(200, 23),
            // (200,23): error CS0452: The type 'X2' must be a reference type in order to use it as parameter 'U2' in the generic type or method 'D1<U1, U2>'
            //     public D1<X1, X2> d1 = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "d1").WithArguments("D1<U1, U2>", "U2", "X2").WithLocation(200, 23),
            // (300,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int, X2>' is not covered.
            //         return d1 switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int, X2>").WithLocation(300, 19),
            // (400,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X1, X2>' is not covered.
            //         return d1 switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X1, X2>").WithLocation(400, 19),
            // (500,16): error CS0452: The type 'X2' must be a reference type in order to use it as parameter 'V1' in the generic type or method 'D2<V1>'
            //             D2<X2> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "X2").WithArguments("D2<V1>", "V1", "X2").WithLocation(500, 16),
            // (600,16): error CS0452: The type 'X1' must be a reference type in order to use it as parameter 'U1' in the generic type or method 'D1<U1, U2>'
            //             D1<X1, X2> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "X1").WithArguments("D1<U1, U2>", "U1", "X1").WithLocation(600, 16),
            // (600,20): error CS0452: The type 'X2' must be a reference type in order to use it as parameter 'U2' in the generic type or method 'D1<U1, U2>'
            //             D1<X1, X2> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "X2").WithArguments("D1<U1, U2>", "U2", "X2").WithLocation(600, 20));
    }

    [Fact]
    public void Exhaustiveness_Constraints_08()
    {
        // A union case type is a subtype of a closed type which violates constraints.
        var source1 = """
            public closed class C<U1>;
            public closed class D1<V1> : C<V1> where V1 : class;
            public class D2<W1> : D1<W1> where W1 : class;
            public class D3<X1> : D1<X1> where X1 : class;

            public union U<T1>(D1<T1>) where T1 : class;
            """;

        var source2 = """
            class Program
            {
            #line 100
                public U<int> u = null!;

                int M1()
                {
            #line 200
                    return u switch
                    {
                    };
                }

                int M2()
                {
            #line 400
                    return u switch
                    {
            #line 500
                        D2<int> => 1,
                    };
                }

                int M3()
                {
                    return u switch
                    {
            #line 600
                        D1<int> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'U<T1>'
            //     public U<int> u = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "u").WithArguments("U<T1>", "T1", "int").WithLocation(100, 19),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
            // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int>' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int>").WithLocation(400, 18),
            // (500,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'W1' in the generic type or method 'D2<W1>'
            //             D2<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<W1>", "W1", "int").WithLocation(500, 16),
            // (600,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'V1' in the generic type or method 'D1<V1>'
            //             D1<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<V1>", "V1", "int").WithLocation(600, 16));
    }

    [Theory]
    [InlineData("C<T1>, D1<T1>")]
    [InlineData("C<T1>")]
    public void Exhaustiveness_Constraints_09(string caseTypes)
    {
        // A union case type is a subtype of a closed type which violates constraints.
        var source1 = $$"""
            public closed class C<U1>;
            public closed class D1<V1> : C<V1> where V1 : class;
            public class D2<W1> : D1<W1> where W1 : class;
            public class D3<X1> : D1<X1> where X1 : class;

            public union U<T1>({{caseTypes}}) where T1 : class;
            """;

        var source2 = """
            class Program
            {
            #line 100
                public U<int> u = null!;

                int M1()
                {
            #line 200
                    return u switch
                    {
                    };
                }

                int M2()
                {
            #line 400
                    return u switch
                    {
            #line 500
                        D2<int> => 1,
                    };
                }

                int M3()
                {
                    return u switch
                    {
            #line 600
                        D1<int> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'U<T1>'
            //     public U<int> u = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "u").WithArguments("U<T1>", "T1", "int").WithLocation(100, 19),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
            // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<int>' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<int>").WithLocation(400, 18),
            // (500,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'W1' in the generic type or method 'D2<W1>'
            //             D2<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<W1>", "W1", "int").WithLocation(500, 16),
            // (600,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'V1' in the generic type or method 'D1<V1>'
            //             D1<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<V1>", "V1", "int").WithLocation(600, 16));
    }

    [Fact]
    public void Exhaustiveness_Constraints_09_SubtypeFirst()
    {
        // A union case type is a subtype of a closed type which violates constraints.
        var source1 = $$"""
            public closed class C<U1>;
            public closed class D1<V1> : C<V1> where V1 : class;
            public class D2<W1> : D1<W1> where W1 : class;
            public class D3<X1> : D1<X1> where X1 : class;

            public union U<T1>(D1<T1>, C<T1>) where T1 : class;
            """;

        var source2 = """
            class Program
            {
            #line 100
                public U<int> u = null!;

                int M1()
                {
            #line 200
                    return u switch
                    {
                    };
                }

                int M2()
                {
            #line 400
                    return u switch
                    {
            #line 500
                        D2<int> => 1,
                    };
                }

                int M3()
                {
                    return u switch
                    {
            #line 600
                        D1<int> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'U<T1>'
            //     public U<int> u = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "u").WithArguments("U<T1>", "T1", "int").WithLocation(100, 19),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
            // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<int>' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<int>").WithLocation(400, 18),
            // (500,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'W1' in the generic type or method 'D2<W1>'
            //             D2<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<W1>", "W1", "int").WithLocation(500, 16),
            // (600,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'V1' in the generic type or method 'D1<V1>'
            //             D1<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<V1>", "V1", "int").WithLocation(600, 16));
    }

    [Fact]
    public void Exhaustiveness_Constraints_10()
    {
        // Union of leaf types of a closed hierarchy which violate constraints
        var source1 = """
            public closed class C<U1>;
            public closed class D1<V1> : C<V1> where V1 : class;
            public class D2<W1> : D1<W1> where W1 : class;
            public class D3<X1> : D1<X1> where X1 : class;

            public union U<T1>(D2<T1>, D3<T1>) where T1 : class;
            """;

        var source2 = """
            class Program
            {
            #line 100
                public U<int> u = null!;

                int M1()
                {
            #line 200
                    return u switch
                    {
                    };
                }

                int M2()
                {
            #line 400
                    return u switch
                    {
            #line 500
                        D2<int> => 1,
                    };
                }

                int M3()
                {
                    return u switch
                    {
            #line 600
                        D1<int> => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'U<T1>'
            //     public U<int> u = null!;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "u").WithArguments("U<T1>", "T1", "int").WithLocation(100, 19),
            // (100,23): error CS0037: Cannot convert null to 'U<int>' because it is a non-nullable value type
            //     public U<int> u = null!;
            Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("U<int>").WithLocation(100, 23),
            // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(200, 18),
            // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D3<int>' is not covered.
            //         return u switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D3<int>").WithLocation(400, 18),
            // (500,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'W1' in the generic type or method 'D2<W1>'
            //             D2<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D2<W1>", "W1", "int").WithLocation(500, 16),
            // (600,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'V1' in the generic type or method 'D1<V1>'
            //             D1<int> => 1,
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("D1<V1>", "V1", "int").WithLocation(600, 16));
    }

    [Fact]
    public void Exhaustiveness_InterfaceConstraints_01()
    {
        // Subtype has interface constraint
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1> where U1 : I1;

            public interface I1;
            public interface I2;

            public class E1 : I1;
            public class E2 : E1, I2;
            """;

        var source2 = $$"""
            class Program
            {
                int Match1<X>(C<X> c)
            #line 100
                    => c switch
                    {
                    };

                int Match2<X>(C<X> c)
                    => c switch
                    {
            #line 200
                        D1<X> => 1
                    };

                int Match3<X>(C<X> c)
                    => c switch
                    {
                        C<X> => 1
                    };

                void Use()
                {
            #line 300
                    Match1(new D1<E1>());
                    Match1(new D1<E2>());
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         => c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(100, 14),
                // (200,16): error CS0314: The type 'X' cannot be used as type parameter 'U1' in the generic type or method 'D1<U1>'. There is no boxing conversion or type parameter conversion from 'X' to 'I1'.
                //             D1<X> => 1
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "X").WithArguments("D1<U1>", "I1", "U1", "X").WithLocation(200, 16));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>"], subtypes.ToTestDisplayStrings());
            Assert.True(classC.Construct(comp.GetMember<NamedTypeSymbol>("E1")).TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<E1>"], subtypes.ToTestDisplayStrings());
            Assert.True(classC.Construct(comp.GetMember<NamedTypeSymbol>("E2")).TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<E2>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_InterfaceConstraints_02()
    {
        // Subtype interface constraints "overlap" with use site interface constraints
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1> where U1 : I1;

            public interface I1;
            public interface I2;

            public class E1 : I1;
            public class E2 : E1, I2;
            """;

        var source2 = $$"""
            class Program
            {
                int Match1<X>(C<X> c) where X : I2
            #line 100
                    => c switch
                    {
                    };

                int Match2<X>(C<X> c) where X : I2
                    => c switch
                    {
            #line 200
                        D1<X> => 1
                    };

                int Match3<X>(C<X> c) where X : I2
                    => c switch
                    {
                        C<X> => 1
                    };

                void Use()
                {
            #line 300
                    Match1(new D1<E1>());
                    Match1(new D1<E2>());
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         => c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(100, 14),
                // (200,16): error CS0314: The type 'X' cannot be used as type parameter 'U1' in the generic type or method 'D1<U1>'. There is no boxing conversion or type parameter conversion from 'X' to 'I1'.
                //             D1<X> => 1
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "X").WithArguments("D1<U1>", "I1", "U1", "X").WithLocation(200, 16),
                // (300,9): error CS0311: The type 'E1' cannot be used as type parameter 'X' in the generic type or method 'Program.Match1<X>(C<X>)'. There is no implicit reference conversion from 'E1' to 'I2'.
                //         Match1(new D1<E1>());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Match1").WithArguments("Program.Match1<X>(C<X>)", "I2", "X", "E1").WithLocation(300, 9));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>"], subtypes.ToTestDisplayStrings());
            Assert.True(classC.Construct(comp.GetMember<NamedTypeSymbol>("E1")).TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<E1>"], subtypes.ToTestDisplayStrings());
            Assert.True(classC.Construct(comp.GetMember<NamedTypeSymbol>("E2")).TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<E2>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_InterfaceConstraints_03()
    {
        // Subtype interface constraints match use site interface constraints
        var source1 = """
            public closed class C<T>;
            public class D1<U1> : C<U1> where U1 : I1;

            public interface I1;

            public class E1 : I1;
            """;

        var source2 = $$"""
            class Program
            {
                int Match1<X>(C<X> c) where X : I1
            #line 100
                    => c switch
                    {
                    };

                int Match2<X>(C<X> c) where X : I1
                    => c switch
                    {
                        D1<X> => 1
                    };

                int Match3<X>(C<X> c) where X : I1
                    => c switch
                    {
                        C<X> => 1
                    };
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
            //         => c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(100, 14));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["D1<T>"], subtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'D1<X>' is not covered.
            //         => c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("D1<X>").WithLocation(100, 14));

        classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D1<T>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["Container<T>.D1", "D2<T>", "D3", "D4"], subtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["Container<T>.D1", "D2<T>", "D3", "D4"], subtypes.ToTestDisplayStrings());

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D2<T>", "D3", "D4", "Container<T>.D1"], subtypes.ToTestDisplayStrings());
    }

    [Fact]
    public void Exhaustiveness_GenericNestedType_01()
    {
        var source1 = """
            public class Container
            {
                public closed class C<T>;
                public class D1<T> : C<T>;
            }

            public class D2<U> : Container.C<U>;
            public class D3 : Container.C<string>;
            public class D4 : Container.C<int>;
            """;

        var source2 = """
            class Program
            {
                int M1(Container.C<string> c)
                {
                    return c switch
                    {
                        Container.D1<string> => 1,
                        D2<string> => 2,
                        D3 => 3,
                    };
                }

                int M2(Container.C<int> c)
                {
                    return c switch
                    {
                        Container.D1<int> => 1,
                        D2<int> => 2,
                        D4 => 3,
                    };
                }

                int M3<X>(Container.C<X> c)
                {
                    return c switch
                    {
                        Container.D1<X> => 1,
                        D2<X> => 2,
                        D3 => 3,
                        D4 => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
        Assert.Equal(["Container.D1<T>", "D2<T>", "D3", "D4"], subtypes.ToTestDisplayStrings());

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["Container.D1<T>", "D2<T>", "D3", "D4"], subtypes.ToTestDisplayStrings());

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        classC = comp.GetMember<NamedTypeSymbol>("Container.C");
        Assert.True(classC.TryGetClosedSubtypes(out subtypes));
        Assert.Equal(["D2<T>", "D3", "D4", "Container.D1<T>"], subtypes.ToTestDisplayStrings());
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
            public sealed class F1 : E;
            public sealed class F2 : E;
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
            Assert.True(classC.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<T>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_02()
    {
        // Attempt to exhaust a type parameter constrained to closed type.
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<X>(X x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
                    };
                }

                int M2<X>(X x) where X : E
                {
            #line 100
                    return x switch
                    {
                        F1 => 1,
                    };
                }

                int M3<X>(X x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X>(X x) where X : E
                {
                    return x switch
                    {
                        X => 1,
                    };
                }

                int M5<X>(X x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }

                int M6<X>(X x) where X : E
                {
                    return x switch
                    {
                        X => 2,
            #line 300
                        F1 => 1,
                    };
                }

                int M7<X>(X x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
            #line 400
                        X => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'X' is not covered.
                //         return x switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("X").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13),
                // (300,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 1,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(300, 13),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13)
                );

            var classE = comp.GetMember<NamedTypeSymbol>("E");
            Assert.True(classE.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["F1", "F2"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_03()
    {
        // A union case type is a type parameter constrained to closed class type
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;

            public union U<T>(T, int);
            """;

        var source2 = """
            class Program
            {
                int M1<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
                        int => 3,
                    };
                }

                int M2<X>(U<X> x) where X : E
                {
            #line 100
                    return x switch
                    {
                        F1 => 1,
                        int => 2,
                    };
                }

                int M3<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        int => 4,
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        X => 1,
                        int => 2,
                    };
                }

                int M5<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        X => 2,
                        int => 3,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'X' is not covered.
                //         return x switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("X").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_04()
    {
        // A union case type is a type parameter constrained to closed class type and only has one case type.
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;

            public union U<T>(T);
            """;

        var source2 = """
            class Program
            {
                int M1<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
                    };
                }

                int M2<X>(U<X> x) where X : E
                {
            #line 100
                    return x switch
                    {
                        F1 => 1,
                    };
                }

                int M3<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        X => 1,
                    };
                }

                int M5<X>(U<X> x) where X : E
                {
                    return x switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'X' is not covered.
                //         return x switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("X").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_05()
    {
        // Type parameter is constrained indirectly to closed type
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                    };
                }

                int M2<X, Y>(Y y) where X : E where Y : X
                {
            #line 100
                    return y switch
                    {
                        F1 => 1,
                    };
                }

                int M3<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }

                int M6<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                    };
                }

                int M7<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        Y => 2,
                    };
                }

                int M8<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 300
                        Y => 2,
                    };
                }

                int M9<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 400
                        X => 2,
                    };
                }

                int M10<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        X => 1,
                #line 500
                        Y => 2,
                    };
                }

                int M11<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M12<X, Y>(object obj) where X : E where Y : X
                {
                #line 600
                    return obj switch
                    {
                        X => 1,
                #line 610
                        Y => 2,
                    };
                }

                int M13<X, Y>(object obj) where X : E where Y : X
                {
                #line 700
                    return obj switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M14<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 800
                        F1 => 2,
                    };
                }

                int M15<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 900
                        F1 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13),
                // (300,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(300, 13),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13),
                // (500,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(500, 13),
                // (600,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(600, 20),
                // (610,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(610, 13),
                // (700,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(700, 20),
                // (800,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(800, 13),
                // (900,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(900, 13)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_06()
    {
        // Type parameter is constrained indirectly to closed type, and closed type has no derived types
        var source1 = """
            public closed class E;
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        E => 1,
                    };
                }

                int M4<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        E => 1,
                #line 100
                        X => 2,
                    };
                }

                int M6<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                    };
                }

                int M7<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        E => 1,
                #line 200
                        Y => 2,
                    };
                }

                int M8<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 300
                        Y => 2,
                    };
                }

                int M9<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 400
                        X => 2,
                    };
                }

                int M10<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        X => 1,
                #line 500
                        Y => 2,
                    };
                }

                int M11<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M12<X, Y>(object obj) where X : E where Y : X
                {
                #line 600
                    return obj switch
                    {
                        X => 1,
                #line 610
                        Y => 2,
                    };
                }

                int M13<X, Y>(object obj) where X : E where Y : X
                {
                #line 700
                    return obj switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M14<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 800
                        E => 2,
                    };
                }

                int M15<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 900
                        E => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(100, 13),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(200, 13),
                // (300,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(300, 13),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13),
                // (500,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(500, 13),
                // (600,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(600, 20),
                // (610,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(610, 13),
                // (700,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(700, 20),
                // (800,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(800, 13),
                // (900,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(900, 13));
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_07()
    {
        // Type parameter is constrained indirectly to closed type, and closed hierarchy is nested
        var source1 = """
            public closed class E;

            public class F1 : E;
            public class F2 : E;
            public closed class F3 : E;

            public class G1 : F3;
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                        F3 => 3,
                    };
                }

                int M1_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                        G1 => 3,
                    };
                }

                int M2<X, Y>(Y y) where X : E where Y : X
                {
            #line 100
                    return y switch
                    {
                        F1 => 1,
                    };
                }

                int M2_2<X, Y>(Y y) where X : E where Y : X
                {
            #line 150
                    return y switch
                    {
                        G1 => 1,
                    };
                }

                int M3<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        G1 => 0,
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }

                int M5_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        G1 => 1,
                        X => 2,
                    };
                }

                int M6<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                    };
                }

                int M7<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        Y => 2,
                    };
                }

                int M7_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        G1 => 1,
                        Y => 2,
                    };
                }

                int M8<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 300
                        Y => 2,
                    };
                }

                int M9<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 400
                        X => 2,
                    };
                }

                int M10<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        X => 1,
                #line 500
                        Y => 2,
                    };
                }

                int M11<X, Y>(X x) where X : E where Y : X
                {
                    return x switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M12<X, Y>(object obj) where X : E where Y : X
                {
                #line 600
                    return obj switch
                    {
                        X => 1,
                #line 610
                        Y => 2,
                    };
                }

                int M13<X, Y>(object obj) where X : E where Y : X
                {
                #line 700
                    return obj switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M14<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 800
                        F1 => 2,
                    };
                }

                int M14_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 850
                        G1 => 2,
                    };
                }

                int M15<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 900
                        F1 => 2,
                    };
                }

                int M15_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 950
                        G1 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (150,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(150, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13),
                // (300,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(300, 13),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13),
                // (500,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(500, 13),
                // (600,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(600, 20),
                // (610,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(610, 13),
                // (700,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(700, 20),
                // (800,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(800, 13),
                // (850,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             G1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "G1").WithLocation(850, 13),
                // (900,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(900, 13),
                // (950,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             G1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "G1").WithLocation(950, 13)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_08()
    {
        // Type parameter is constrained to closed type, and matching is also performed against unrelated type parameter
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                    };
                }

                int M2<X, Y>(Y y) where Y : E
                {
                #line 100
                    return y switch
                    {
                        F1 => 1,
                    };
                }

                int M3<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                #line 200
                        E => 3,
                    };
                }

                int M4<X, Y>(Y y) where Y : E
                {
                #line 300
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(Y y) where Y : E
                {
                #line 310
                    return y switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }

                int M6<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        Y => 1,
                    };
                }

                int M7<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        F1 => 1,
                        Y => 2,
                    };
                }

                int M8<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        X => 1,
                        Y => 2,
                    };
                }

                int M9<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        Y => 1,
                #line 400
                        X => 2,
                    };
                }

                int M10<X, Y>(X x) where Y : E
                {
                    return x switch
                    {
                        X => 1,
                #line 500
                        Y => 2,
                    };
                }

                int M11<X, Y>(X x) where Y : E
                {
                    return x switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M12<X, Y>(object obj) where Y : E
                {
                #line 600
                    return obj switch
                    {
                        X => 1,
                        Y => 2,
                    };
                }

                int M13<X, Y>(object obj) where Y : E
                {
                #line 700
                    return obj switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M14<X, Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        Y => 1,
                #line 800
                        F1 => 2,
                    };
                }

                int M15<X, Y>(Y y) where Y : E
                {
                #line 900
                    return y switch
                    {
                        X => 1,
                        F1 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(300, 18),
                // (310,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(310, 18),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13),
                // (500,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(500, 13),
                // (600,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(600, 20),
                // (700,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(700, 20),
                // (800,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(800, 13),
                // (900,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(900, 18)

                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_09()
    {
        // Type parameter is constrained indirectly to closed type. Test a few 'not'/'and'/'or' cases
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 or F2 => 1,
                    };
                }

                int M2<X, Y>(Y y) where X : E where Y : X
                {
            #line 100
                    return y switch
                    {
                        not F1 => 1,
                    };
                }

                int M2_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        not F1 => 1,
                        F1 => 2,
                    };
                }

                int M3<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
            #line 200
                        F2 or Y => 3,
                    };
                }

                int M3_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        not (F1 or F2) => 1,
                        X => 2,
                    };
                }

                int M4<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(Y y) where X : E where Y : X
                {
            #line 300
                    return y switch
                    {
                        F1 and X => 1,
                    };
                }

                int M5_2<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
            #line 400
                        F1 and X => 1,
                        F2 => 2,
                    };
                }

                int M6<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        not Y => 1,
                        X => 2,
                    };
                }

                int M7<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        not F1 => 1,
                        Y => 2,
                    };
                }

                int M14<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                        not F1 => 2,
                    };
                }

                int M15<X, Y>(Y y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                        not F2 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (200,19): hidden CS9335: The pattern is redundant.
                //             F2 or Y => 3,
                Diagnostic(ErrorCode.HDN_RedundantPattern, "Y").WithLocation(200, 19),
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(300, 18),
                // (400,20): hidden CS9335: The pattern is redundant.
                //             F1 and X => 1,
                Diagnostic(ErrorCode.HDN_RedundantPattern, "X").WithLocation(400, 20)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_10()
    {
        // Test when a derived type of case type is absent, but case type itself is present
        var source1 = """
            public closed class E;
            public class F1 : E;
            public class G1 : F1;
            """;

        var source2 = """
            class Program
            {
                int M1<Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        G1 => 1,
                        F1 => 2,
                    };
                }

                int M2(E e)
                {
                    return e switch
                    {
                        G1 => 1,
                        F1 => 2,
                    };
                }
                int M3<Y>(Y y) where Y : E
                {
            #line 100
                    return y switch
                    {
                        G1 => 1,
                    };
                }

                int M4(E e)
                {
            #line 200
                    return e switch
                    {
                        G1 => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'F1' is not covered.
                //         return e switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("F1").WithLocation(200, 18)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_11()
    {
        // Exercise 'trueTestImpliesTrueOther' detection
        var source1 = """
            public closed class E;
            public class F1 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        F1 => 1,
                    };
                }

                int M2(E e)
                {
                    return e switch
                    {
                        F1 => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics();
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedToClosedType_12()
    {
        // Exercise 'falseTestImpliesTrueOther' detection
        var source1 = """
            public closed class E;
            public class F1 : E;
            """;

        var source2 = """
            class Program
            {
                int M1<Y>(Y y) where Y : E
                {
                    return y switch
                    {
                        null => 1,
                        F1 => 1,
                    };
                }

                int M2(E e)
                {
                    return e switch
                    {
                        null => 1,
                        F1 => 1,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics();
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedUnionCaseType_01()
    {
        // Union case type is a type parameter constrained indirectly to closed type
        var source1 = """
            public closed class E;
            public sealed class F1 : E;
            public sealed class F2 : E;

            public union U<T>(T);
            """;

        var source2 = """
            class Program
            {
                int M1<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
                    };
                }

                int M2<X, Y>(U<Y> y) where X : E where Y : X
                {
            #line 100
                    return y switch
                    {
                        F1 => 1,
                    };
                }

                int M3<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        F2 => 2,
            #line 200
                        E => 3,
                    };
                }

                int M4<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                    };
                }

                int M5<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        X => 2,
                    };
                }

                int M6<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                    };
                }

                int M7<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        F1 => 1,
                        Y => 2,
                    };
                }

                int M8<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        X => 1,
                #line 300
                        Y => 2,
                    };
                }

                int M9<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 400
                        X => 2,
                    };
                }

                int M10<X, Y>(U<X> x) where X : E where Y : X
                {
                    return x switch
                    {
                        X => 1,
                #line 500
                        Y => 2,
                    };
                }

                int M11<X, Y>(U<X> x) where X : E where Y : X
                {
                    return x switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M12<X, Y>(U<object> obj) where X : E where Y : X
                {
                #line 600
                    return obj switch
                    {
                        X => 1,
                #line 610
                        Y => 2,
                    };
                }

                int M13<X, Y>(U<object> obj) where X : E where Y : X
                {
                #line 700
                    return obj switch
                    {
                        Y => 1,
                        X => 2,
                    };
                }

                int M14<X, Y>(U<Y> y) where X : E where Y : X
                {
                    return y switch
                    {
                        Y => 1,
                #line 800
                        F1 => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            comp.VerifyEmitDiagnostics(
                // (100,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Y' is not covered.
                //         return y switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Y").WithLocation(100, 18),
                // (200,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             E => 3,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E").WithLocation(200, 13),
                // (300,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(300, 13),
                // (400,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             X => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "X").WithLocation(400, 13),
                // (500,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(500, 13),
                // (600,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'object' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("object").WithLocation(600, 20),
                // (610,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             Y => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Y").WithLocation(610, 13),
                // (700,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'object' is not covered.
                //         return obj switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("object").WithLocation(700, 20),
                // (800,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             F1 => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "F1").WithLocation(800, 13)
                );
        }
    }

    [Fact]
    public void Exhaustiveness_ConstrainedUnionCaseType_02()
    {
        // A union case type is a type parameter constrained to non-closed class type
        // This is tested as a point of comparison with Exhaustiveness_ConstrainedToClosedType_04
        var source1 = """
            public union U<T>(T);

            public class C;
            public class D : C;
            """;

        var source2 = """
            class Program
            {
                int M1<X>(U<X> x) where X : C
                {
                    return x switch
                    {
                        X => 1,
                    };
                }

                int M2<X>(U<X> x) where X : C
                {
                    return x switch
                    {
                        C => 1,
                    };
                }

                int M3<X>(U<X> x) where X : C
                {
                    return x switch
                    {
                        D => 1,
                        X => 2,
                    };
                }

                int M4<X>(U<X> x) where X : C
                {
                    return x switch
                    {
                        D => 1,
                        C => 2,
                    };
                }
            }
            """;

        var comp = CreateCompilation([source1, source2, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var comp0 = CreateCompilation([source1, UnionAttributeSource, IUnionSource, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp = CreateCompilation([source2], references: [comp0.ToMetadataReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation([source2], references: [comp0.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
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
                // (300,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(300, 18),
                // (400,13): error CS8121: An expression of type 'C<string[]>' cannot be handled by a pattern of type 'D1<object>'.
                //             D1<object> => 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "D1<object>").WithArguments("C<string[]>", "D1<object>").WithLocation(400, 13));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.False(classC.TryGetClosedSubtypes(out _));

            var cOfStringArray = classC.Construct(
                comp.CreateArrayTypeSymbol(comp.GetSpecialType(SpecialType.System_String)));
            Assert.True(cOfStringArray.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<System.String>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
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
            Assert.False(classC.TryGetClosedSubtypes(out _));

            var cOfStringArray = classC.Construct(
                comp.CreateArrayTypeSymbol(
                    comp.CreatePointerTypeSymbol(
                        comp.GetSpecialType(SpecialType.System_Int32))));
            Assert.True(cOfStringArray.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<System.Int32>"], subtypes.ToTestDisplayStrings());
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

        var comp = CreateCompilation([source1, source2, IsClosedTypeAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        verify(comp);

        var comp0 = CreateCompilation([source1, IsClosedTypeAttributeDefinition], options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
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
                // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(400, 18),
                // (500,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<X>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<X>").WithLocation(500, 18),
                // (600,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C<string>' is not covered.
                //         return c switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C<string>").WithLocation(600, 18));

            var classC = comp.GetMember<NamedTypeSymbol>("C");
            Assert.False(classC.TryGetClosedSubtypes(out _));

            var tupleOfStringInt = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(
                    comp.GetSpecialType(SpecialType.System_String),
                    comp.GetSpecialType(SpecialType.System_Int32));

            var cOfStringArray = classC.Construct(tupleOfStringInt);
            Assert.True(cOfStringArray.TryGetClosedSubtypes(out var subtypes));
            Assert.Equal(["D1<System.String>"], subtypes.ToTestDisplayStrings());

            tupleOfStringInt = NamedTypeSymbol.CreateTuple(tupleOfStringInt);
            cOfStringArray = classC.Construct(tupleOfStringInt);
            Assert.True(cOfStringArray.TryGetClosedSubtypes(out subtypes));
            Assert.Equal(["D1<System.String>"], subtypes.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void Partial_01()
    {
        // Multiple partial declarations specify 'closed'
        var source = """
            closed partial class C1;
            closed partial class C1;
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();

        var c1 = comp.GetMember<NamedTypeSymbol>("C1");
        Assert.True(c1.IsClosed);
    }

    [Fact]
    public void Partial_02()
    {
        // Not all partial declarations specify 'closed'
        var source = """
            partial class C1;
            closed partial class C1;

            closed partial class C2;
            partial class C2;
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics();

        var c1 = comp.GetMember<NamedTypeSymbol>("C1");
        Assert.True(c1.IsClosed);

        var c2 = comp.GetMember<NamedTypeSymbol>("C2");
        Assert.True(c2.IsClosed);
    }

    [Fact]
    public void Partial_03()
    {
        // Incompatible modifiers are spread across partial declarations
        var source = """
            abstract partial class C1;
            closed partial class C1;

            closed partial class C2;
            sealed partial class C2;

            static partial class C3;
            closed partial class C3;
            """;

        var comp = CreateCompilation([source, IsClosedTypeAttributeDefinition], targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,24): error CS9384: 'C1': a closed type cannot be marked abstract because it is always implicitly abstract.
            // abstract partial class C1;
            Diagnostic(ErrorCode.ERR_ClosedExplicitlyAbstract, "C1").WithArguments("C1").WithLocation(1, 24),
            // (4,22): error CS9381: 'C2': a closed type cannot be sealed or static
            // closed partial class C2;
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C2").WithArguments("C2").WithLocation(4, 22),
            // (7,22): error CS9381: 'C3': a closed type cannot be sealed or static
            // static partial class C3;
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C3").WithArguments("C3").WithLocation(7, 22));

        var c1 = comp.GetMember<NamedTypeSymbol>("C1");
        Assert.True(c1.IsClosed);

        var c2 = comp.GetMember<NamedTypeSymbol>("C2");
        Assert.True(c2.IsClosed);
        Assert.True(c2.IsSealed);

        var c3 = comp.GetMember<NamedTypeSymbol>("C3");
        Assert.True(c3.IsClosed);
        Assert.True(c3.IsStatic);
    }
}
