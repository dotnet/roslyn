﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

[CompilerTrait(CompilerFeature.RequiredMembers)]
public class RequiredMembersTests : CSharpTestBase
{
    private const string RequiredMemberAttributeVB = @"
Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Field Or AttributeTargets.Property, Inherited := false, AllowMultiple := false)>
    Public Class RequiredMemberAttribute
        Inherits Attribute
    End Class
End Namespace
Namespace System.Diagnostics.CodeAnalysis
    <AttributeUsage(AttributeTargets.Constructor, Inherited := false, AllowMultiple := false)>
    Public Class SetsRequiredMembersAttribute
        Inherits Attribute
    End Class
End Namespace";

    private static CSharpCompilation CreateCompilationWithRequiredMembers(CSharpTestSource source, IEnumerable<MetadataReference>? references = null, CSharpParseOptions? parseOptions = null, CSharpCompilationOptions? options = null, string? assemblyName = null, TargetFramework targetFramework = TargetFramework.Standard)
        => CreateCompilation(new[] { source, RequiredMemberAttribute, SetsRequiredMembersAttribute, CompilerFeatureRequiredAttribute }, references, options: options, parseOptions: parseOptions, assemblyName: assemblyName, targetFramework: targetFramework);

    private Compilation CreateVisualBasicCompilationWithRequiredMembers(string source)
        => CreateVisualBasicCompilation(new[] { source, RequiredMemberAttributeVB });

    private static Action<ModuleSymbol> ValidateRequiredMembersInModule(string[] memberPaths, string expectedAttributeLayout)
    {
        return module =>
        {
            if (module is PEModuleSymbol peModule)
            {
                var actualAttributes = RequiredMemberAttributesVisitor.GetString(peModule);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedAttributeLayout, actualAttributes);
            }

            var requiredTypes = new HashSet<NamedTypeSymbol>();

            foreach (var memberPath in memberPaths)
            {
                var member = module.GlobalNamespace.GetMember(memberPath);
                AssertEx.NotNull(member, $"Member {memberPath} was not found");
                Assert.True(member is PropertySymbol or FieldSymbol, $"Unexpected member symbol type {member.Kind}");
                Assert.True(member.IsRequired());
                if (module is SourceModuleSymbol)
                {
                    Assert.All(member.GetAttributes(), attr => AssertEx.NotEqual("System.Runtime.CompilerServices.RequiredMemberAttribute", attr.AttributeClass.ToTestDisplayString()));
                }
                else
                {
                    AssertEx.Any(member.GetAttributes(), attr => attr.AttributeClass.ToTestDisplayString() == "System.Runtime.CompilerServices.RequiredMemberAttribute");
                }

                requiredTypes.Add((NamedTypeSymbol)member.ContainingType);
            }

            foreach (var type in requiredTypes)
            {
                AssertTypeRequiredMembersInvariants(module, type);
            }
        };
    }

    private static Action<ModuleSymbol> GetTypeRequiredMembersInvariantsValidator(string expectedType)
    {
        return module =>
        {
            var type = module.GlobalNamespace.GetTypeMember(expectedType);
            Assert.NotNull(type);
            AssertTypeRequiredMembersInvariants(module, type);
        };
    }

    private static void AssertTypeRequiredMembersInvariants(ModuleSymbol module, NamedTypeSymbol type)
    {
        Assert.True(type.HasAnyRequiredMembers);

        var peModule = module as PEModuleSymbol;
        foreach (var ctor in type.GetMembers().Where(m => m is MethodSymbol { MethodKind: MethodKind.Constructor }))
        {
            var ctorAttributes = ctor.GetAttributes();

            // Attributes should be filtered out when loaded from metadata, and are only added during emit in source
            Assert.DoesNotContain(ctorAttributes, attr => attr.AttributeClass.ToTestDisplayString() is "System.ObsoleteAttribute" or "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute");

            if (peModule is not null)
            {
                var peMethod = (PEMethodSymbol)ctor;
                var decoder = new MetadataDecoder(peModule, peMethod);
                var obsoleteAttribute = peModule.Module.TryGetDeprecatedOrExperimentalOrObsoleteAttribute(peMethod.Handle, decoder, ignoreByRefLikeMarker: false, ignoreRequiredMemberMarker: false);
                string? unsupportedCompilerFeatureToken = peModule.Module.GetFirstUnsupportedCompilerFeatureFromToken(peMethod.Handle, decoder, CompilerFeatureRequiredFeatures.None);

                if (ctorAttributes.Any(attr => attr.AttributeClass.ToTestDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"))
                {
                    Assert.Null(obsoleteAttribute);
                    Assert.Null(unsupportedCompilerFeatureToken);
                }
                else
                {
                    Assert.NotNull(obsoleteAttribute);
                    Assert.Equal(PEModule.RequiredMembersMarker, obsoleteAttribute.Message);
                    Assert.True(obsoleteAttribute.IsError);

                    Assert.Equal(nameof(CompilerFeatureRequiredFeatures.RequiredMembers), unsupportedCompilerFeatureToken);
                    Assert.Null(peModule.Module.GetFirstUnsupportedCompilerFeatureFromToken(peMethod.Handle, decoder, CompilerFeatureRequiredFeatures.RequiredMembers));
                }
            }
        }
    }

    [Fact]
    public void InvalidModifierLocations()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
required class C1
{
    required void M(required int i)
    {
    }

    required C1() { }
    required ~C1() { }

    required int this[int i] { get => 0; set { } }

    int Prop1 { required get; }
    int Prop2 { required set { } }
}
required struct S {}
required delegate void D();
required interface I1
{
    required int Prop3 { get; set; }
    required int Field;
}
interface I2
{
    public int Prop4 { get; }
}
class C2 : I2
{
    required int I2.Prop4 => 0;
}
");

        comp.VerifyDiagnostics(
            // (2,16): error CS0106: The modifier 'required' is not valid for this item
            // required class C1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C1").WithArguments("required").WithLocation(2, 16),
            // (4,19): error CS0106: The modifier 'required' is not valid for this item
            //     required void M(required int i)
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("required").WithLocation(4, 19),
            // (4,21): error CS0246: The type or namespace name 'required' could not be found (are you missing a using directive or an assembly reference?)
            //     required void M(required int i)
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "required").WithArguments("required").WithLocation(4, 21),
            // (4,30): error CS1001: Identifier expected
            //     required void M(required int i)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(4, 30),
            // (4,30): error CS1003: Syntax error, ',' expected
            //     required void M(required int i)
            Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(4, 30),
            // (8,14): error CS0106: The modifier 'required' is not valid for this item
            //     required C1() { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C1").WithArguments("required").WithLocation(8, 14),
            // (9,15): error CS0106: The modifier 'required' is not valid for this item
            //     required ~C1() { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C1").WithArguments("required").WithLocation(9, 15),
            // (11,18): error CS0106: The modifier 'required' is not valid for this item
            //     required int this[int i] { get => 0; set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("required").WithLocation(11, 18),
            // (13,26): error CS0106: The modifier 'required' is not valid for this item
            //     int Prop1 { required get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("required").WithLocation(13, 26),
            // (14,26): error CS0106: The modifier 'required' is not valid for this item
            //     int Prop2 { required set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("required").WithLocation(14, 26),
            // (16,17): error CS0106: The modifier 'required' is not valid for this item
            // required struct S {}
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("required").WithLocation(16, 17),
            // (17,24): error CS0106: The modifier 'required' is not valid for this item
            // required delegate void D();
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("required").WithLocation(17, 24),
            // (18,20): error CS0106: The modifier 'required' is not valid for this item
            // required interface I1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("required").WithLocation(18, 20),
            // (20,18): error CS0106: The modifier 'required' is not valid for this item
            //     required int Prop3 { get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Prop3").WithArguments("required").WithLocation(20, 18),
            // (21,18): error CS0525: Interfaces cannot contain instance fields
            //     required int Field;
            Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "Field").WithLocation(21, 18),
            // (21,18): warning CS0649: Field 'I1.Field' is never assigned to, and will always have its default value 0
            //     required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("I1.Field", "0").WithLocation(21, 18),
            // (29,21): error CS0106: The modifier 'required' is not valid for this item
            //     required int I2.Prop4 => 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Prop4").WithArguments("required").WithLocation(29, 21)
        );
    }

    [Fact]
    public void InvalidModifierCombinations()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
unsafe struct C
{
    required const int F1 = 1;
    required static int F2 = 2;
    required static int P1 { get; set; }
    required fixed int F3[10];
}
", options: TestOptions.UnsafeReleaseDll);

        comp.VerifyDiagnostics(
            // (4,24): error CS0106: The modifier 'required' is not valid for this item
            //     required const int F1 = 1;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "F1").WithArguments("required").WithLocation(4, 24),
            // (5,25): error CS0106: The modifier 'required' is not valid for this item
            //     required static int F2 = 2;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "F2").WithArguments("required").WithLocation(5, 25),
            // (5,25): warning CS0414: The field 'C.F2' is assigned but its value is never used
            //     required static int F2 = 2;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F2").WithArguments("C.F2").WithLocation(5, 25),
            // (6,25): error CS0106: The modifier 'required' is not valid for this item
            //     required static int P1 { get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("required").WithLocation(6, 25),
            // (7,24): error CS0106: The modifier 'required' is not valid for this item
            //     required fixed int F3[10];
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "F3").WithArguments("required").WithLocation(7, 24)
        );
    }

    [Fact]
    public void LangVersion()
    {
        string code = @"
#pragma warning disable CS0649 // Field is never assigned
class C
{
    internal required int Field;
    internal required int Prop { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(code, parseOptions: TestOptions.Regular10);

        comp.VerifyDiagnostics(
            // (5,27): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     internal required int Field;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Field").WithArguments("required members").WithLocation(5, 27),
            // (6,27): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     internal required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop").WithArguments("required members").WithLocation(6, 27)
        );

        comp = CreateCompilationWithRequiredMembers(code, parseOptions: TestOptions.RegularNext);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void DuplicateKeyword()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    internal required required int Field;
    internal required required int Prop { get; set; }
}
");

        comp.VerifyDiagnostics(
            // (4,23): error CS1004: Duplicate 'required' modifier
            //     internal required required int Field;
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "required").WithArguments("required").WithLocation(4, 23),
            // (4,36): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     internal required required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(4, 36),
            // (5,23): error CS1004: Duplicate 'required' modifier
            //     internal required required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "required").WithArguments("required").WithLocation(5, 23)
        );
    }

    [Theory]
    [CombinatorialData]
    public void InvalidNames(bool use10)
    {
        string code = @"
namespace N1
{
    struct required {}
}
namespace N2
{
    class required {}
}
namespace N3
{
    interface required {}
}
namespace N4
{
    delegate void required();
}
namespace N5
{
    record required();
}
namespace N6
{
    record struct required();
}
namespace N7
{
    class C
    {
        class required {}
    }
}
namespace N8
{
    class required<T> {}
}
";
        var comp = CreateCompilationWithRequiredMembers(code, parseOptions: use10 ? TestOptions.Regular10 : TestOptions.RegularNext);

        comp.VerifyDiagnostics(
            use10 ?
                new[]
                {
                    // (4,12): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     struct required {}
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(4, 12),
                    // (8,11): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     class required {}
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(8, 11),
                    // (12,15): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     interface required {}
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(12, 15),
                    // (16,19): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     delegate void required();
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(16, 19),
                    // (20,12): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     record required();
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(20, 12),
                    // (24,19): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     record struct required();
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(24, 19),
                    // (30,15): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //         class required {}
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(30, 15),
                    // (35,11): warning CS8981: The type name 'required' only contains lower-cased ascii characters. Such names may become reserved for the language.
                    //     class required<T> {}
                    Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "required").WithArguments("required").WithLocation(35, 11)
                } :
                new[]
                {
                    // (4,12): error CS9500: Types and aliases cannot be named 'required'.
                    //     struct required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(4, 12),
                    // (8,11): error CS9500: Types and aliases cannot be named 'required'.
                    //     class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(8, 11),
                    // (12,15): error CS9500: Types and aliases cannot be named 'required'.
                    //     interface required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(12, 15),
                    // (16,19): error CS9500: Types and aliases cannot be named 'required'.
                    //     delegate void required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(16, 19),
                    // (20,12): error CS9500: Types and aliases cannot be named 'required'.
                    //     record required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(20, 12),
                    // (24,19): error CS9500: Types and aliases cannot be named 'required'.
                    //     record struct required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(24, 19),
                    // (30,15): error CS9500: Types and aliases cannot be named 'required'.
                    //         class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(30, 15),
                    // (35,11): error CS9500: Types and aliases cannot be named 'required'.
                    //     class required<T> {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(35, 11)
                }
        );

        code = code.Replace("required", "@required");
        comp = CreateCompilationWithRequiredMembers(code, parseOptions: use10 ? TestOptions.Regular10 : TestOptions.RegularNext);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void MissingRequiredMemberAttribute()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int I { get; set; }
}");

        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_RequiredMemberAttribute);

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RequiredMemberAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute", ".ctor").WithLocation(2, 7);
        comp.VerifyDiagnostics(expected);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void MissingRequiredMemberAttributeCtor()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int I { get; set; }
}
");

        comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RequiredMemberAttribute__ctor);

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RequiredMemberAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute", ".ctor").WithLocation(2, 7);
        comp.VerifyDiagnostics(expected);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void MissingCompilerFeatureRequiredAttribute()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int I { get; set; }
}");

        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute);

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(2, 7);

        comp.VerifyDiagnostics(expected);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void MissingCompilerFeatureRequiredAttributeCtor()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int I { get; set; }
}
");

        comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor);

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(2, 7);
        comp.VerifyDiagnostics(expected);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void RequiredMemberAttributeEmitted()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int Prop { get; set; }
    public required int Field;
}
");

        var expectedRequiredMembers = new[] { "C.Prop", "C.Field" };

        var expectedAttributeLayout = @"
[RequiredMember] C
    [RequiredMember] System.Int32 C.Field
    [RequiredMember] System.Int32 C.Prop { get; set; }
";

        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics(
            // (5,25): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     public required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(5, 25)
        );
    }

    [Theory]
    [CombinatorialData]
    public void RequiredMemberAttributeEmitted_OverrideRequiredProperty_MissingRequiredOnOverride01(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop { get; set; }
}";

        var derived = @"
class Derived : Base
{
    public override int Prop { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(@base + derived);

        comp.VerifyDiagnostics(
            // (8,25): error CS9501: 'Derived.Prop' must be required because it overrides required member 'Base.Prop'
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("Derived.Prop", "Base.Prop").WithLocation(8, 25)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();

        comp = CreateCompilation(derived, references: new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (4,25): error CS9501: 'Derived.Prop' must be required because it overrides required member 'Base.Prop'
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("Derived.Prop", "Base.Prop").WithLocation(4, 25)
        );
    }

    [Theory]
    [CombinatorialData]
    public void RequiredMemberAttributeEmitted_OverrideRequiredProperty_MissingRequiredOnOverride02(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual int Prop { get; set; }
}";

        var derived = @"
public class Derived : Base
{
    public override required int Prop { get; set; }
}";

        var derivedDerived = @"
class DerivedDerived : Derived
{
    public override int Prop { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(@base + derived + derivedDerived);

        comp.VerifyDiagnostics(
            // (12,25): error CS9501: 'DerivedDerived.Prop' must be required because it overrides required member 'Derived.Prop'
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("DerivedDerived.Prop", "Derived.Prop").WithLocation(12, 25)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();

        MetadataReference baseReference = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();
        var derivedComp = CreateCompilation(derived, references: new[] { baseReference });
        derivedComp.VerifyDiagnostics();

        comp = CreateCompilation(derivedDerived, new[] { baseReference, useMetadataReference ? derivedComp.ToMetadataReference() : derivedComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (4,25): error CS9501: 'DerivedDerived.Prop' must be required because it overrides required member 'Derived.Prop'
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("DerivedDerived.Prop", "Derived.Prop").WithLocation(4, 25)
        );
    }

    [Theory]
    [CombinatorialData]
    public void RequiredMemberAttributeEmitted_OverrideRequiredProperty(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop { get; set; }
}
";

        string derived = @"
class Derived : Base
{
    public override required int Prop { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(@base + derived);

        var expectedRequiredMembers = new[] { "Base.Prop", "Derived.Prop" };

        var expectedAttributeLayout = @"
[RequiredMember] Base
    [RequiredMember] System.Int32 Base.Prop { get; set; }
[RequiredMember] Derived
    [RequiredMember] System.Int32 Derived.Prop { get; set; }";

        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base, assemblyName: "test");
        baseComp.VerifyDiagnostics();

        comp = CreateCompilation(derived, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        expectedAttributeLayout = @"
[RequiredMember] Derived
    [RequiredMember] System.Int32 Derived.Prop { get; set; }
";
        symbolValidator = ValidateRequiredMembersInModule(new[] { "Derived.Prop" }, expectedAttributeLayout);
        verifier = CompileAndVerify(comp, symbolValidator: symbolValidator, sourceSymbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void RequiredMemberAttributeEmitted_AddRequiredOnOverride(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual int Prop { get; set; }
}
";

        var derived = @"
public class Derived : Base
{
    public override required int Prop { get; set; }
}
";

        var derivedDerived = @"
class DerivedDerived : Derived
{
    public override required int Prop { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(@base + derived + derivedDerived);

        var expectedRequiredMembers = new[] { "Derived.Prop", "DerivedDerived.Prop" };

        var expectedAttributeLayout = @"
[RequiredMember] Derived
    [RequiredMember] System.Int32 Derived.Prop { get; set; }
[RequiredMember] DerivedDerived
    [RequiredMember] System.Int32 DerivedDerived.Prop { get; set; }
";

        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();
        var baseReference = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();

        var derivedComp = CreateCompilation(derived, new[] { baseReference });
        derivedComp.VerifyDiagnostics();

        comp = CreateCompilation(derivedDerived, new[] { baseReference, useMetadataReference ? derivedComp.ToMetadataReference() : derivedComp.EmitToImageReference() });
        expectedAttributeLayout = @"
[RequiredMember] DerivedDerived
    [RequiredMember] System.Int32 DerivedDerived.Prop { get; set; }
";
        symbolValidator = ValidateRequiredMembersInModule(new[] { "DerivedDerived.Prop" }, expectedAttributeLayout);
        verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void RequiredMemberAttributeEmitted_NestedTypeHasRequired()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class Outer
{
    class Inner
    {
        public required int Prop { get; set; }
        public required int Field;
    }
}
");

        var expectedRequiredMembers = new[] { "Outer.Inner.Prop", "Outer.Inner.Field" };

        var expectedAttributeLayout = @"
Outer
    [RequiredMember] Outer.Inner
        [RequiredMember] System.Int32 Outer.Inner.Field
        [RequiredMember] System.Int32 Outer.Inner.Prop { get; set; }";

        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics(
            // (7,29): warning CS0649: Field 'Outer.Inner.Field' is never assigned to, and will always have its default value 0
            //         public required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("Outer.Inner.Field", "0").WithLocation(7, 29)
        );
    }

    [Theory]
    [CombinatorialData]
    public void RequiredMemberAttributeEmitted_AbstractProperty(bool useMetadataReference)
    {
        var @base = @"
public abstract class Base
{
    public required abstract int Prop { get; set; }
}";

        var derived = @"
class Derived : Base
{
    public override required int Prop { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(@base + derived);

        var expectedRequiredMembers = new[] { "Base.Prop", "Derived.Prop" };

        var expectedAttributeLayout = @"
[RequiredMember] Base
    [RequiredMember] System.Int32 Base.Prop { get; set; }
[RequiredMember] Derived
    [RequiredMember] System.Int32 Derived.Prop { get; set; }
";

        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base, assemblyName: "base");
        baseComp.VerifyDiagnostics();
        var baseReference = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();

        comp = CreateCompilation(derived, new[] { baseReference }, assemblyName: "derived");

        expectedAttributeLayout = @"
[RequiredMember] Derived
    [RequiredMember] System.Int32 Derived.Prop { get; set; }
";

        symbolValidator = ValidateRequiredMembersInModule(new[] { "Derived.Prop" }, expectedAttributeLayout);
        verifier = CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void HidingRequiredMembers(bool useMetadataReference)
    {
        var @base =
@"
#pragma warning disable CS0649 // Never assigned
public class Base
{
    public required int Field;
    public required int Prop { get; set; }
}";

        var derived = @"
class Derived1 : Base
{
    public new int Field; // 1
    public new int Prop { get; set; } // 2
}
class Derived2 : Base
{
    public new int Prop; // 3
    public new int Field { get; set; } // 4
}
class Derived3 : Base
{
    public int Field; // 1
    public int Prop { get; set; } // 2
}
";
        var comp = CreateCompilationWithRequiredMembers(@base + derived);

        comp.VerifyDiagnostics(
            // (10,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived1.Field'.
            //     public new int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived1.Field").WithLocation(10, 20),
            // (11,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived1.Prop'.
            //     public new int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived1.Prop").WithLocation(11, 20),
            // (15,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived2.Prop'.
            //     public new int Prop; // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived2.Prop").WithLocation(15, 20),
            // (16,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived2.Field'.
            //     public new int Field { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived2.Field").WithLocation(16, 20),
            // (20,16): warning CS0108: 'Derived3.Field' hides inherited member 'Base.Field'. Use the new keyword if hiding was intended.
            //     public int Field; // 1
            Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Derived3.Field", "Base.Field").WithLocation(20, 16),
            // (20,16): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived3.Field'.
            //     public int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived3.Field").WithLocation(20, 16),
            // (21,16): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived3.Prop'.
            //     public int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived3.Prop").WithLocation(21, 16)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();

        comp = CreateCompilation("#pragma warning disable CS0649 // Never assigned" + derived, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (4,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived1.Field'.
            //     public new int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived1.Field").WithLocation(4, 20),
            // (5,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived1.Prop'.
            //     public new int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived1.Prop").WithLocation(5, 20),
            // (9,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived2.Prop'.
            //     public new int Prop; // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived2.Prop").WithLocation(9, 20),
            // (10,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived2.Field'.
            //     public new int Field { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived2.Field").WithLocation(10, 20),
            // (14,16): warning CS0108: 'Derived3.Field' hides inherited member 'Base.Field'. Use the new keyword if hiding was intended.
            //     public int Field; // 1
            Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Derived3.Field", "Base.Field").WithLocation(14, 16),
            // (14,16): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived3.Field'.
            //     public int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived3.Field").WithLocation(14, 16),
            // (15,16): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived3.Prop'.
            //     public int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived3.Prop").WithLocation(15, 16)
        );
    }

    [Fact]
    public void RequiredMembersMustBeAsVisibleAsContainingType()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
#pragma warning disable CS0649 // Never assigned
#pragma warning disable CS0169 // Never used
public class PublicClass
{
    public required int PublicProperty { get; set; }
    internal protected required int InternalProtectedProperty { get; set; } // 1
    internal required int InternalProperty { get; set; } // 2
    protected required int ProtectedProperty { get; set; } // 3
    private protected required int PrivateProtectedProperty { get; set; } // 4
    private required int PrivateProperty { get; set; } // 5
    public required int PublicField;
    internal protected required int InternalProtectedField; // 6
    internal required int InternalField; // 7
    protected required int ProtectedField; // 8
    private protected required int PrivateProtectedField; // 9
    private required int PrivateField; // 10
}
internal class InternalClass
{
    public required int PublicProperty { get; set; }
    internal protected required int InternalProtectedProperty { get; set; }
    internal required int InternalProperty { get; set; }
    protected required int ProtectedProperty { get; set; } // 11
    private protected required int PrivateProtectedProperty { get; set; } // 12
    private required int PrivateProperty { get; set; } // 13
    public required int PublicField;
    internal protected required int InternalProtectedField;
    internal required int InternalField;
    protected required int ProtectedField; // 14
    private protected required int PrivateProtectedField; // 15
    private required int PrivateField; // 16
}
internal class Outer
{
    protected internal class ProtectedInternalClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; } // 17
        protected required int ProtectedProperty { get; set; } // 18
        private protected required int PrivateProtectedProperty { get; set; } // 19
        private required int PrivateProperty { get; set; } // 20
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField; // 21
        protected required int ProtectedField; // 22
        private protected required int PrivateProtectedField; // 23
        private required int PrivateField; // 24
    }
    protected class ProtectedClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; }
        private protected required int PrivateProtectedProperty { get; set; } // 25
        private required int PrivateProperty { get; set; } // 26
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField;
        private protected required int PrivateProtectedField; // 27
        private required int PrivateField; // 28
    }
    private protected class PrivateProtectedClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; }
        private protected required int PrivateProtectedProperty { get; set; }
        private required int PrivateProperty { get; set; } // 29
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField;
        private protected required int PrivateProtectedField;
        private required int PrivateField; // 30
    }
    private class PrivateClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; }
        private protected required int PrivateProtectedProperty { get; set; }
        private required int PrivateProperty { get; set; }
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField;
        private protected required int PrivateProtectedField;
        private required int PrivateField;
    }
}
");

        comp.VerifyDiagnostics(
            // (7,37): error CS9503: Required member 'PublicClass.InternalProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedProperty { get; set; } // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedProperty").WithArguments("PublicClass.InternalProtectedProperty", "PublicClass").WithLocation(7, 37),
            // (8,27): error CS9503: Required member 'PublicClass.InternalProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal required int InternalProperty { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("PublicClass.InternalProperty", "PublicClass").WithLocation(8, 27),
            // (9,28): error CS9503: Required member 'PublicClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedProperty { get; set; } // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("PublicClass.ProtectedProperty", "PublicClass").WithLocation(9, 28),
            // (10,36): error CS9503: Required member 'PublicClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedProperty { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("PublicClass.PrivateProtectedProperty", "PublicClass").WithLocation(10, 36),
            // (11,26): error CS9503: Required member 'PublicClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private required int PrivateProperty { get; set; } // 5
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("PublicClass.PrivateProperty", "PublicClass").WithLocation(11, 26),
            // (13,37): error CS9503: Required member 'PublicClass.InternalProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedField; // 6
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedField").WithArguments("PublicClass.InternalProtectedField", "PublicClass").WithLocation(13, 37),
            // (14,27): error CS9503: Required member 'PublicClass.InternalField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal required int InternalField; // 7
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("PublicClass.InternalField", "PublicClass").WithLocation(14, 27),
            // (15,28): error CS9503: Required member 'PublicClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedField; // 8
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("PublicClass.ProtectedField", "PublicClass").WithLocation(15, 28),
            // (16,36): error CS9503: Required member 'PublicClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedField; // 9
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("PublicClass.PrivateProtectedField", "PublicClass").WithLocation(16, 36),
            // (17,26): error CS9503: Required member 'PublicClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private required int PrivateField; // 10
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("PublicClass.PrivateField", "PublicClass").WithLocation(17, 26),
            // (24,28): error CS9503: Required member 'InternalClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedProperty { get; set; } // 11
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("InternalClass.ProtectedProperty", "InternalClass").WithLocation(24, 28),
            // (25,36): error CS9503: Required member 'InternalClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedProperty { get; set; } // 12
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("InternalClass.PrivateProtectedProperty", "InternalClass").WithLocation(25, 36),
            // (26,26): error CS9503: Required member 'InternalClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private required int PrivateProperty { get; set; } // 13
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("InternalClass.PrivateProperty", "InternalClass").WithLocation(26, 26),
            // (30,28): error CS9503: Required member 'InternalClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedField; // 14
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("InternalClass.ProtectedField", "InternalClass").WithLocation(30, 28),
            // (31,36): error CS9503: Required member 'InternalClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedField; // 15
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("InternalClass.PrivateProtectedField", "InternalClass").WithLocation(31, 36),
            // (32,26): error CS9503: Required member 'InternalClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private required int PrivateField; // 16
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("InternalClass.PrivateField", "InternalClass").WithLocation(32, 26),
            // (40,31): error CS9503: Required member 'Outer.ProtectedInternalClass.InternalProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         internal required int InternalProperty { get; set; } // 17
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("Outer.ProtectedInternalClass.InternalProperty", "Outer.ProtectedInternalClass").WithLocation(40, 31),
            // (41,32): error CS9503: Required member 'Outer.ProtectedInternalClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedProperty { get; set; } // 18
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.ProtectedInternalClass.ProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(41, 32),
            // (42,40): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 19
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(42, 40),
            // (43,30): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateProperty { get; set; } // 20
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProperty", "Outer.ProtectedInternalClass").WithLocation(43, 30),
            // (46,31): error CS9503: Required member 'Outer.ProtectedInternalClass.InternalField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         internal required int InternalField; // 21
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("Outer.ProtectedInternalClass.InternalField", "Outer.ProtectedInternalClass").WithLocation(46, 31),
            // (47,32): error CS9503: Required member 'Outer.ProtectedInternalClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedField; // 22
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.ProtectedInternalClass.ProtectedField", "Outer.ProtectedInternalClass").WithLocation(47, 32),
            // (48,40): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedField; // 23
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedField", "Outer.ProtectedInternalClass").WithLocation(48, 40),
            // (49,30): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateField; // 24
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedInternalClass.PrivateField", "Outer.ProtectedInternalClass").WithLocation(49, 30),
            // (57,40): error CS9503: Required member 'Outer.ProtectedClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 25
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedClass.PrivateProtectedProperty", "Outer.ProtectedClass").WithLocation(57, 40),
            // (58,30): error CS9503: Required member 'Outer.ProtectedClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateProperty { get; set; } // 26
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedClass.PrivateProperty", "Outer.ProtectedClass").WithLocation(58, 30),
            // (63,40): error CS9503: Required member 'Outer.ProtectedClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedField; // 27
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedClass.PrivateProtectedField", "Outer.ProtectedClass").WithLocation(63, 40),
            // (64,30): error CS9503: Required member 'Outer.ProtectedClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateField; // 28
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedClass.PrivateField", "Outer.ProtectedClass").WithLocation(64, 30),
            // (73,30): error CS9503: Required member 'Outer.PrivateProtectedClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateProperty { get; set; } // 29
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.PrivateProtectedClass.PrivateProperty", "Outer.PrivateProtectedClass").WithLocation(73, 30),
            // (79,30): error CS9503: Required member 'Outer.PrivateProtectedClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateField; // 30
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.PrivateProtectedClass.PrivateField", "Outer.PrivateProtectedClass").WithLocation(79, 30)
        );
    }

    [Fact]
    public void RequiredMembersMustBeAsVisibleAsContainingType_InaccessibleSetters()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
public class PublicClass
{
    public required int InternalProtected { get; internal protected set; } // 1
    public required int Internal { get; internal set; } // 2
    public required int Protected { get; protected set; } // 3
    public required int PrivateProtected { get; private protected set; } // 4
    public required int Private { get; private set; } // 5
}
internal class InternalClass
{
    public required int InternalProtected { get; internal protected set; }
    public required int Internal { get; internal set; }
    public required int Protected { get; protected set; } // 6
    public required int PrivateProtected { get; private protected set; } // 7
    public required int Private { get; private set; } // 8
}
internal class Outer
{
    protected internal class InternalProtectedClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; } // 9
        public required int Protected { get; protected set; } // 10
        public required int PrivateProtected { get; private protected set; } // 11
        public required int Private { get; private set; } // 12
    }
    protected class ProtectedClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; }
        public required int PrivateProtected { get; private protected set; } // 13
        public required int Private { get; private set; } // 14
    }
    private protected class PrivateProtectedClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; }
        public required int PrivateProtected { get; private protected set; }
        public required int Private { get; private set; } // 15
    }
    private class PrivateClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; }
        public required int PrivateProtected { get; private protected set; }
        public required int Private { get; private set; }
    }
}
");

        comp.VerifyDiagnostics(
            // (4,25): error CS9503: Required member 'PublicClass.InternalProtected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int InternalProtected { get; internal protected set; } // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtected").WithArguments("PublicClass.InternalProtected", "PublicClass").WithLocation(4, 25),
            // (5,25): error CS9503: Required member 'PublicClass.Internal' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Internal { get; internal set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Internal").WithArguments("PublicClass.Internal", "PublicClass").WithLocation(5, 25),
            // (6,25): error CS9503: Required member 'PublicClass.Protected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Protected { get; protected set; } // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("PublicClass.Protected", "PublicClass").WithLocation(6, 25),
            // (7,25): error CS9503: Required member 'PublicClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int PrivateProtected { get; private protected set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("PublicClass.PrivateProtected", "PublicClass").WithLocation(7, 25),
            // (8,25): error CS9503: Required member 'PublicClass.Private' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Private { get; private set; } // 5
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("PublicClass.Private", "PublicClass").WithLocation(8, 25),
            // (14,25): error CS9503: Required member 'InternalClass.Protected' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int Protected { get; protected set; } // 6
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("InternalClass.Protected", "InternalClass").WithLocation(14, 25),
            // (15,25): error CS9503: Required member 'InternalClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int PrivateProtected { get; private protected set; } // 7
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("InternalClass.PrivateProtected", "InternalClass").WithLocation(15, 25),
            // (16,25): error CS9503: Required member 'InternalClass.Private' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int Private { get; private set; } // 8
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("InternalClass.Private", "InternalClass").WithLocation(16, 25),
            // (23,29): error CS9503: Required member 'Outer.InternalProtectedClass.Internal' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int Internal { get; internal set; } // 9
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Internal").WithArguments("Outer.InternalProtectedClass.Internal", "Outer.InternalProtectedClass").WithLocation(23, 29),
            // (24,29): error CS9503: Required member 'Outer.InternalProtectedClass.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int Protected { get; protected set; } // 10
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer.InternalProtectedClass.Protected", "Outer.InternalProtectedClass").WithLocation(24, 29),
            // (25,29): error CS9503: Required member 'Outer.InternalProtectedClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int PrivateProtected { get; private protected set; } // 11
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.InternalProtectedClass.PrivateProtected", "Outer.InternalProtectedClass").WithLocation(25, 29),
            // (26,29): error CS9503: Required member 'Outer.InternalProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int Private { get; private set; } // 12
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.InternalProtectedClass.Private", "Outer.InternalProtectedClass").WithLocation(26, 29),
            // (33,29): error CS9503: Required member 'Outer.ProtectedClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         public required int PrivateProtected { get; private protected set; } // 13
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.ProtectedClass.PrivateProtected", "Outer.ProtectedClass").WithLocation(33, 29),
            // (34,29): error CS9503: Required member 'Outer.ProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         public required int Private { get; private set; } // 14
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.ProtectedClass.Private", "Outer.ProtectedClass").WithLocation(34, 29),
            // (42,29): error CS9503: Required member 'Outer.PrivateProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         public required int Private { get; private set; } // 15
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.PrivateProtectedClass.Private", "Outer.PrivateProtectedClass").WithLocation(42, 29)
         );
    }

    [Fact]
    public void UsingRequiredMemberAttributeExplicitly()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
using System.Runtime.CompilerServices;
[RequiredMember]
class C
{
    [RequiredMember]
    public int Prop { get; set; }
    [RequiredMember]
    public int Field;
}
");

        comp.VerifyDiagnostics(
            // (3,2): error CS9504: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(3, 2),
            // (6,6): error CS9504: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(6, 6),
            // (8,6): error CS9504: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(8, 6),
            // (9,16): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     public int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(9, 16)
        );

        var prop = comp.SourceModule.GlobalNamespace.GetMember<PropertySymbol>("C.Prop");
        Assert.False(prop.IsRequired);
    }

    [Fact]
    public void UsingRequiredMemberAttributeExplicitly_WrongLocations()
    {
        var comp = CreateCompilation(@"
using System;
using System.Runtime.CompilerServices;
class C
{
    [RequiredMember]
    void M() {}
    [RequiredMember]
    event Action E;
    [RequiredMember]
    C() {}
    [RequiredMember]
    ~C() {}
    [return: RequiredMember]
    void M<[RequiredMember] T>([RequiredMember] int i) {}
}

namespace System.Runtime.CompilerServices
{
    public class RequiredMemberAttribute : Attribute
    {
        public RequiredMemberAttribute()
        {
        }
    }
}
");

        comp.VerifyDiagnostics(
            // (9,18): warning CS0067: The event 'C.E' is never used
            //     event Action E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(9, 18)
        );
    }

    [Fact]
    public void RequiredWithInitializer()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    public required int Field = 1;
    public required int Prop { get; set; } = 1;
}
");

        comp.VerifyDiagnostics();
    }

    [Fact]
    public void RefReturningProperties()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    private int i;
    public required ref int Prop => ref i;
}
");

        comp.VerifyDiagnostics(
            // (5,29): error CS9505: Required member 'C.Prop' must be settable.
            //     public required ref int Prop => ref i;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop").WithArguments("C.Prop").WithLocation(5, 29)
        );
    }

    [Fact]
    public void UnsettableMembers()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
#pragma warning disable CS0649 // Unassigned field
class C
{
    public required readonly int Field;
    public required int Prop1 { get; }
    public required int Prop2 { get; protected set; }
}
");

        comp.VerifyDiagnostics(
            // (5,34): error CS9505: Required member 'C.Field' must be settable.
            //     public required readonly int Field;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Field").WithArguments("C.Field").WithLocation(5, 34),
            // (6,25): error CS9505: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(6, 25),
            // PROTOTYPE(req): Better error message?
            // (7,25): error CS9503: Required member 'C.Prop2' cannot be less visible or have a setter less visible than the containing type 'C'.
            //     public required int Prop2 { get; protected set; }
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Prop2").WithArguments("C.Prop2", "C").WithLocation(7, 25)
        );
    }

    [Fact]
    public void ObsoleteMember()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
using System;
#pragma warning disable CS0649 // Unassigned field
class C
{
    [Obsolete]
    public required int Field;
    [Obsolete]
    public required int Prop1 { get; set; }
}
");

        // PROTOTYPE(req): Confirm with LDM whether we want a warning here.
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ReadonlyPropertiesAndStructs()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
readonly struct S1
{
    public required readonly int Prop1 { get => 1; set {} }
}
struct S2
{
    public readonly int Prop2 { get => 1; set {} }
}
struct S3
{
    public int Prop2 { get => 1; readonly set {} }
}
");

        comp.VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_NoInheritance_NoneSet(bool useMetadataReference, [CombinatorialValues("", " C")] string constructor)
    {
        var c = @"
public class C
{
    public required int Prop { get; set; }
    public required int Field;
}
";

        var creation = $@"C c = new{constructor}();";
        var comp = CreateCompilationWithRequiredMembers(new[] { c, creation });

        var expectedDiagnostics = constructor == " C" ? new[]
        {
            // (1,11): error CS9506: Required member 'C.Prop' must be set in the object initializer or attribute constructor.
            // C c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop").WithLocation(1, 11),
            // (1,11): error CS9506: Required member 'C.Field' must be set in the object initializer or attribute constructor.
            // C c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field").WithLocation(1, 11)
        }
        : new[] {
            // (1,7): error CS9506: Required member 'C.Prop' must be set in the object initializer or attribute constructor.
            // C c = new();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Prop").WithLocation(1, 7),
            // (1,7): error CS9506: Required member 'C.Field' must be set in the object initializer or attribute constructor.
            // C c = new();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Field").WithLocation(1, 7)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var cComp = CreateCompilationWithRequiredMembers(c);
        comp = CreateCompilation(creation, references: new[] { useMetadataReference ? cComp.ToMetadataReference() : cComp.EmitToImageReference() });

        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_NoInheritance_NoneSet_HasSetsRequiredMembers(bool useMetadataReference, [CombinatorialValues("", " C")] string constructor, [CombinatorialValues("", "method: ")] string target)
    {
        var c = $$"""
            using System.Diagnostics.CodeAnalysis;
            public class C
            {
                public required int Prop { get; set; }
                public required int Field;

                [{{target}}SetsRequiredMembers]
                public C() {}
            }
            """;

        var creation = $@"C c = new{constructor}();";
        var comp = CreateCompilationWithRequiredMembers(new[] { c, creation });

        comp.VerifyDiagnostics();

        var cComp = CreateCompilationWithRequiredMembers(c);
        comp = CreateCompilation(creation, references: new[] { useMetadataReference ? cComp.ToMetadataReference() : cComp.EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_NoInheritance_PartialSet(bool useMetadataReference, [CombinatorialValues("", " C")] string constructor)
    {
        var c = @"
public class C
{
    public required int Prop1 { get; set; }
    public required int Prop2 { get; set; }
    public required int Field1;
    public required int Field2;
}
";

        var creation = $@"C c = new{constructor}() {{ Prop2 = 1, Field2 = 1 }};";
        var comp = CreateCompilationWithRequiredMembers(new[] { c, creation });

        var expectedDiagnostics = constructor == " C" ? new[]
        {
            // (1,11): error CS9506: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // C c = new C() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(1, 11),
            // (1,11): error CS9506: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // C c = new C() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field1").WithLocation(1, 11)
        }
        : new[] {
            // (1,7): error CS9506: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // C c = new() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Prop1").WithLocation(1, 7),
            // (1,7): error CS9506: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // C c = new() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Field1").WithLocation(1, 7)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var cComp = CreateCompilationWithRequiredMembers(c);
        comp = CreateCompilation(creation, references: new[] { useMetadataReference ? cComp.ToMetadataReference() : cComp.EmitToImageReference() });

        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_NoInheritance_AllSet(bool useMetadataReference, [CombinatorialValues("", " C")] string constructor)
    {
        var c = @"
public class C
{
    public required int Prop1 { get; set; }
    public required int Field1;
}
";

        var creation = @"
C c = new" + constructor + @"() { Prop1 = 1, Field1 = 1 };
System.Console.WriteLine($""{c.Prop1}, {c.Field1}"");
";
        var comp = CreateCompilationWithRequiredMembers(new[] { c, creation });
        CompileAndVerify(comp, expectedOutput: "1, 1");

        var cComp = CreateCompilationWithRequiredMembers(c);
        comp = CreateCompilation(creation, references: new[] { useMetadataReference ? cComp.ToMetadataReference() : cComp.EmitToImageReference() });
        CompileAndVerify(comp, expectedOutput: "1, 1");
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_Unsettable()
    {
        var c = @"
var c = new C();
c = new C() { Prop1 = 1, Field1 = 1 };

public class C
{
    public required int Prop1 { get; }
    public required readonly int Field1;
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics(
            // (2,13): error CS9506: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(2, 13),
            // (2,13): error CS9506: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field1").WithLocation(2, 13),
            // (3,15): error CS0200: Property or indexer 'C.Prop1' cannot be assigned to -- it is read only
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Prop1").WithArguments("C.Prop1").WithLocation(3, 15),
            // (3,26): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonly, "Field1").WithLocation(3, 26),
            // (7,25): error CS9505: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(7, 25),
            // (8,34): error CS9505: Required member 'C.Field1' must be settable.
            //     public required readonly int Field1;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Field1").WithArguments("C.Field1").WithLocation(8, 34)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_Unsettable_FromMetadata()
    {
        var vb = @"
Imports System.Runtime.CompilerServices

<RequiredMember>
Public Class C
    <RequiredMember>
    Public Readonly Property Prop1 As Integer
    <RequiredMember>
    Public Readonly Field1 As Integer
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        vbComp.VerifyEmitDiagnostics();

        var c = @"
var c = new C();
c = new C() { Prop1 = 1, Field1 = 1 };
";
        var comp = CreateCompilation(new[] { c }, references: new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (2,13): error CS9506: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(2, 13),
            // (2,13): error CS9506: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field1").WithLocation(2, 13),
            // (3,15): error CS0200: Property or indexer 'C.Prop1' cannot be assigned to -- it is read only
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Prop1").WithArguments("C.Prop1").WithLocation(3, 15),
            // (3,26): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonly, "Field1").WithLocation(3, 26)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_Unsettable_HasSetsRequiredMembers()
    {
        var c = @"
using System.Diagnostics.CodeAnalysis;
var c = new C();
c = new C() { Prop1 = 1, Field1 = 1 };

public class C
{
    public required int Prop1 { get; }
    public required readonly int Field1;

    [SetsRequiredMembers]
    public C() {}
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics(
            // (4,15): error CS0200: Property or indexer 'C.Prop1' cannot be assigned to -- it is read only
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Prop1").WithArguments("C.Prop1").WithLocation(4, 15),
            // (4,26): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonly, "Field1").WithLocation(4, 26),
            // (8,25): error CS9505: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(8, 25),
            // (9,34): error CS9505: Required member 'C.Field1' must be settable.
            //     public required readonly int Field1;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Field1").WithArguments("C.Field1").WithLocation(9, 34)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_DisallowedNestedObjectInitializer()
    {
        var c = @"
var c = new C() { D1 = { NestedProp = 1 }, D2 = { NestedProp = 2 } };

public class C
{
    public required D D1 { get; set; }
    public required D D2;
}
public class D
{
    public int NestedProp { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics(
            // (2,24): error CS9507: Required member 'C.D1' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { D1 = { NestedProp = 1 }, D2 = { NestedProp = 2 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ NestedProp = 1 }").WithArguments("C.D1").WithLocation(2, 24),
            // (2,49): error CS9507: Required member 'C.D2' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { D1 = { NestedProp = 1 }, D2 = { NestedProp = 2 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ NestedProp = 2 }").WithArguments("C.D2").WithLocation(2, 49)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_DisallowedNestedObjectInitializer_HasSetsRequiredMembers()
    {
        var c = @"
using System.Diagnostics.CodeAnalysis;
var c = new C() { D1 = { NestedProp = 1 }, D2 = { NestedProp = 2 } };

public class C
{
    public required D D1 { get; set; }
    public required D D2;

    [SetsRequiredMembers]
    public C() {}
}
public class D
{
    public int NestedProp { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_NestedObjectCreationAllowed()
    {
        var c = @"
var c = new C() { D1 = new() { NestedProp = 1 }, D2 = new() { NestedProp = 2 } };

public class C
{
    public required D D1 { get; set; }
    public required D D2;
}
public class D
{
    public int NestedProp { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_DisallowedNestedCollectionInitializer()
    {
        var c = @"
using System.Collections.Generic;
var c = new C() { L1 = { 1, 2, 3 }, L2 = { 4, 5, 6 } };

public class C
{
    public required List<int> L1 { get; set; }
    public required List<int> L2;
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics(
            // (3,24): error CS9507: Required member 'C.L1' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { L1 = { 1, 2, 3 }, L2 = { 4, 5, 6 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ 1, 2, 3 }").WithArguments("C.L1").WithLocation(3, 24),
            // (3,42): error CS9507: Required member 'C.L2' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { L1 = { 1, 2, 3 }, L2 = { 4, 5, 6 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ 4, 5, 6 }").WithArguments("C.L2").WithLocation(3, 42)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_DisallowedNestedCollectionInitializer_HasSetsRequiredMember()
    {
        var c = @"
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
var c = new C() { L1 = { 1, 2, 3 }, L2 = { 4, 5, 6 } };

public class C
{
    public required List<int> L1 { get; set; }
    public required List<int> L2;

    [SetsRequiredMembers]
    public C() {}
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_NestedObjectCreationWithCollectionInitializerAllowed()
    {
        var c = @"
using System.Collections.Generic;
var c = new C() { L1 = new() { 1, 2, 3 }, L2 = new() { 4, 5, 6 } };

public class C
{
    public required List<int> L1 { get; set; }
    public required List<int> L2;
}
";
        var comp = CreateCompilationWithRequiredMembers(c);
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Inheritance_NoneSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public required int Prop1 { get; set; }
    public required int Field1;
}
";

        var code = @"
_ = new Derived();

public class Derived : Base
{
    public required int Prop2 { get; set; }
    public required int Field2;
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });

        var expectedDiagnostics = new[] {
                // (2,9): error CS9506: Required member 'Base.Prop1' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop1").WithLocation(2, 9),
                // (2,9): error CS9506: Required member 'Base.Field1' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field1").WithLocation(2, 9),
                // (2,9): error CS9506: Required member 'Derived.Prop2' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop2").WithLocation(2, 9),
                // (2,9): error CS9506: Required member 'Derived.Field2' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Field2").WithLocation(2, 9)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Inheritance_NoneSet_HasSetsRequiredMembers(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public required int Prop1 { get; set; }
    public required int Field1;
}
";

        var derived = @"
using System.Diagnostics.CodeAnalysis;

public class Derived : Base
{
    public required int Prop2 { get; set; }
    public required int Field2;

    [SetsRequiredMembers]
    public Derived() {}
}
";

        var code = @"_ = new Derived();";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, code });
        var validator = GetTypeRequiredMembersInvariantsValidator("Derived");
        CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();
        var baseRef = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();

        var derivedComp = CreateCompilation(derived, new[] { baseRef });
        CompileAndVerify(derivedComp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

        comp = CreateCompilation(code, new[] { baseRef, useMetadataReference ? derivedComp.ToMetadataReference() : derivedComp.EmitToImageReference() });
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Inheritance_PartialSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public required int Prop1 { get; set; }
    public required int Field1;
    public required int Prop2 { get; set; }
    public required int Field2;
}
";

        var code = @"
_ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };

public class Derived : Base
{
    public required int Prop3 { get; set; }
    public required int Field3;
    public required int Prop4 { get; set; }
    public required int Field4;
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });

        var expectedDiagnostics = new[] {
            // (2,9): error CS9506: Required member 'Base.Prop2' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop2").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Base.Field2' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field2").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Derived.Prop4' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop4").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Derived.Field4' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Field4").WithLocation(2, 9)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Inheritance_AllSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public required int Prop1 { get; set; }
    public required int Field1;
}
";

        var code = @"
_ = new Derived() { Prop1 = 1, Field1 = 1, Prop2 = 2, Field2 = 2 };

public class Derived : Base
{
    public required int Prop2 { get; set; }
    public required int Field2;
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });
        var validator = GetTypeRequiredMembersInvariantsValidator("Derived");
        CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Inheritance_NoMembersOnDerivedType(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public required int Prop1 { get; set; }
    public required int Field1;
}
";

        var code = @"
_ = new Derived() { Prop1 = 1, Field1 = 1 };

public class Derived : Base
{
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });
        var validator = GetTypeRequiredMembersInvariantsValidator("Derived");
        CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_ThroughRetargeting_NoneSet()
    {
        var retargetedCode = @"public class C {}";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = @"
public class Base
{
    public required C Prop1 { get; set; }
    public required C Field1;
}
";

        var baseComp = CreateCompilationWithRequiredMembers(@base, new[] { originalC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);

        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var code = @"
_ = new Derived();

public class Derived : Base
{
    public required C Prop2 { get; set; }
    public required C Field2;
}
";

        var comp = CreateCompilation(code, new[] { baseComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics(
            // (2,9): error CS9506: Required member 'Base.Field1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field1").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Base.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop1").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Derived.Prop2' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop2").WithLocation(2, 9),
            // (2,9): error CS9506: Required member 'Derived.Field2' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Field2").WithLocation(2, 9)
        );

        var baseSymbol = comp.GetTypeByMetadataName("Base");
        Assert.IsType<RetargetingNamedTypeSymbol>(baseSymbol);
    }

    [Fact]
    public void EnforcedRequiredMembers_ThroughRetargeting_NoneSet_HasSetsRequiredMembers()
    {
        var retargetedCode = @"public class C {}";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = @"
public class Base
{
    public required C Prop1 { get; set; }
    public required C Field1;
}
";

        var originalCRef = originalC.ToMetadataReference();
        var baseComp = CreateCompilationWithRequiredMembers(@base, new[] { originalCRef }, targetFramework: TargetFramework.Standard);

        var derived = @"
using System.Diagnostics.CodeAnalysis;
public class Derived : Base
{
    public required C Prop2 { get; set; }
    public required C Field2;

    [SetsRequiredMembers]
    public Derived() {}
}
";

        var baseRef = baseComp.ToMetadataReference();
        var derivedComp = CreateCompilation(derived, new[] { baseRef, originalCRef }, targetFramework: TargetFramework.Standard);

        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var code = @"
_ = new Derived();
";

        var comp = CreateCompilation(code, new[] { baseRef, derivedComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics();

        var baseSymbol = comp.GetTypeByMetadataName("Derived");
        Assert.IsType<RetargetingNamedTypeSymbol>(baseSymbol);
    }

    [Fact]
    public void EnforcedRequiredMembers_ThroughRetargeting_AllSet()
    {
        var retargetedCode = @"public class C {}";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = @"
public class Base
{
    public required C Prop1 { get; set; }
    public required C Field1;
}
";

        var baseComp = CreateCompilationWithRequiredMembers(@base, new[] { originalC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);

        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var code = @"
_ = new Derived() { Prop1 = new(), Field1 = new(), Prop2 = new(), Field2 = new() };

public class Derived : Base
{
    public required C Prop2 { get; set; }
    public required C Field2;
}
";

        var comp = CreateCompilation(code, new[] { baseComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics();

        var baseSymbol = comp.GetTypeByMetadataName("Base");
        Assert.IsType<RetargetingNamedTypeSymbol>(baseSymbol);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Override_NoneSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop1 { get; set; }
}
";

        var code = @"
_ = new Derived();

public class Derived : Base
{
    public override required int Prop1 { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });

        var expectedDiagnostics = new[] {
            // (2,9): error CS9506: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop1").WithLocation(2, 9)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Override_NoneSet_HasSetsRequiredMembers(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop1 { get; set; }
}
";

        var code = @"
using System.Diagnostics.CodeAnalysis;
_ = new Derived();

public class Derived : Base
{
    public override required int Prop1 { get; set; }

    [SetsRequiredMembers]
    public Derived() {}
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });

        comp.VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Override_PartialSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop1 { get; set; }
    public virtual required int Prop2 { get; set; }
}
";

        var code = @"
_ = new Derived() { Prop2 = 1 };

public class Derived : Base
{
    public override required int Prop1 { get; set; }
    public override required int Prop2 { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });

        var expectedDiagnostics = new[] {
            // (2,9): error CS9506: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop1").WithLocation(2, 9)
        };

        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void EnforcedRequiredMembers_Override_AllSet(bool useMetadataReference)
    {
        var @base = @"
public class Base
{
    public virtual required int Prop1 { get; set; }
    public virtual required int Prop2 { get; set; }
}
";

        var code = @"
_ = new Derived() { Prop1 = 1, Prop2 = 1 };

public class Derived : Base
{
    public override required int Prop1 { get; set; }
    public override required int Prop2 { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });
        CompileAndVerify(comp).VerifyDiagnostics();

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyEmitDiagnostics();

        comp = CreateCompilation(code, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_OverrideRetargeted_AllSet()
    {
        var retargetedCode = @"public class C {}";
        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = @"
public class Base
{
    public required virtual C Prop1 { get; set; }
}
";

        var baseComp = CreateCompilationWithRequiredMembers(@base, new[] { originalC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var code = @"
_ = new Derived() { Prop1 = new() };

public class Derived : Base
{
    public override required C Prop1 { get; set; }
}
";

        var comp = CreateCompilation(code, new[] { baseComp.ToMetadataReference(), retargetedC.ToMetadataReference() });
        comp.VerifyDiagnostics();

        Assert.IsType<RetargetingNamedTypeSymbol>(comp.GetTypeByMetadataName("Base"));
    }

    [Fact]
    public void EnforcedRequiredMembers_OverrideRetargeted_NoneSet()
    {
        var retargetedCode = @"public class C {}";
        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = @"
public class Base
{
    public required virtual C Prop1 { get; set; }
}
";

        var baseComp = CreateCompilationWithRequiredMembers(@base, new[] { originalC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var code = @"
_ = new Derived();

public class Derived : Base
{
    public override required C Prop1 { get; set; }
}
";

        var comp = CreateCompilation(code, new[] { baseComp.ToMetadataReference(), retargetedC.ToMetadataReference() });
        comp.VerifyDiagnostics(
            // (2,9): error CS9506: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop1").WithLocation(2, 9)
        );

        Assert.IsType<RetargetingNamedTypeSymbol>(comp.GetTypeByMetadataName("Base"));
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_01()
    {
        var vb = @"
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

<RequiredMember>
Public Class Derived
    Inherits Base
    <RequiredMember>
    Public Shadows Property P As Integer
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
    public Derived2(int x) {}
}
class Derived3 : Derived { }";

        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (7,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (8,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2(int x) {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(8, 12),
            // (10,7): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // class Derived3 : Derived { }
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived3").WithArguments("Derived").WithLocation(10, 7)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_01_HasSetsRequiredMembers()
    {
        var vb = @"
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

<RequiredMember>
Public Class Derived
    Inherits Base
    <RequiredMember>
    Public Shadows Property P As Integer
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = @"
using System.Diagnostics.CodeAnalysis;
_ = new Derived2();

class Derived2 : Derived
{
    [SetsRequiredMembers]
    public Derived2() {}
    [SetsRequiredMembers]
    public Derived2(int x) {}
}";

        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_02()
    {
        var vb = @"
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

<RequiredMember>
Public Class Derived
    Inherits Base
    Public Shadows Property P As Integer
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
}
class Derived3 : Derived { }";

        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (7,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (9,7): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // class Derived3 : Derived { }
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived3").WithArguments("Derived").WithLocation(9, 7)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_03()
    {
        var vb = @"
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

Public Class Derived
    Inherits Base
    Public Shadows Property P As Integer
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
}
class Derived3 : Derived { }";

        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (7,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (9,7): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // class Derived3 : Derived { }
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived3").WithArguments("Derived").WithLocation(9, 7)
        );
    }


    /// <summary>
    /// This IL is the equivalent of:
    /// public record Derived : Base
    /// {
    ///    public {propertyIsRequired ? required : ""} new int P { get; init; }
    /// }
    /// </summary>
    private static string GetShadowingRecordIl(bool propertyIsRequired)
    {
        var propertyAttribute = propertyIsRequired ?
            """
            .custom instance void [original]RequiredMemberAttribute::.ctor() = (
                01 00 00 00
            )
            """ : "";
        return $$"""
                 .assembly extern original {}
                 
                 .class public auto ansi beforefieldinit Derived
                     extends [original]Base
                     implements class [mscorlib]System.IEquatable`1<class Derived>
                 {
                     .custom instance void [original]RequiredMemberAttribute::.ctor() = (
                         01 00 00 00
                     )
                     // Fields
                     .field private initonly int32 '<P>k__BackingField'
                     .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                         01 00 00 00
                     )
                     .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = (
                         01 00 00 00 00 00 00 00
                     )
                 
                     // Methods
                     .method family hidebysig specialname virtual 
                         instance class [mscorlib]System.Type get_EqualityContract () cil managed 
                     {
                         .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                             01 00 00 00
                         )
                         ldnull
                         throw
                     } // end of method Derived::get_EqualityContract
                 
                     .method public hidebysig specialname 
                         instance int32 get_P () cil managed 
                     {
                         .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                             01 00 00 00
                         )
                         ldnull
                         throw
                     } // end of method Derived::get_P
                 
                     .method public hidebysig specialname 
                         instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) set_P (
                             int32 'value'
                         ) cil managed 
                     {
                         .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                             01 00 00 00
                         )
                         ldnull
                         throw
                     } // end of method Derived::set_P
                 
                     .method public hidebysig virtual 
                         instance string ToString () cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::ToString
                 
                     .method family hidebysig virtual 
                         instance bool PrintMembers (
                             class [mscorlib]System.Text.StringBuilder builder
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::PrintMembers
                 
                     .method public hidebysig specialname static 
                         bool op_Inequality (
                             class Derived left,
                             class Derived right
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::op_Inequality
                 
                     .method public hidebysig specialname static 
                         bool op_Equality (
                             class Derived left,
                             class Derived right
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::op_Equality
                 
                     .method public hidebysig virtual 
                         instance int32 GetHashCode () cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::GetHashCode
                 
                     .method public hidebysig virtual 
                         instance bool Equals (
                             object obj
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::Equals
                 
                     .method public final hidebysig virtual 
                         instance bool Equals (
                             class [original]Base other
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::Equals
                 
                     .method public hidebysig newslot virtual 
                         instance bool Equals (
                             class Derived other
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::Equals
                 
                     .method public hidebysig newslot virtual 
                         instance class Derived '<Clone>$' () cil managed 
                     {
                         .custom instance void [mscorlib]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (
                             01 00 00 00
                         )
                         .override method instance class [original]Base [original]Base::'<Clone>$'()
                         ldnull
                         throw
                     } // end of method Derived::'<Clone>$'
                 
                     .method family hidebysig specialname rtspecialname 
                         instance void .ctor (
                             class Derived original
                         ) cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::.ctor
                 
                     .method public hidebysig specialname rtspecialname 
                         instance void .ctor () cil managed 
                     {
                         ldnull
                         throw
                     } // end of method Derived::.ctor
                 
                     // Properties
                     .property instance class [mscorlib]System.Type EqualityContract()
                     {
                         .get instance class [mscorlib]System.Type Derived::get_EqualityContract()
                     }
                     .property instance int32 P()
                     {
                         {{propertyAttribute}}
                         .get instance int32 Derived::get_P()
                         .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Derived::set_P(int32)
                     }
                 
                 } // end of class Derived
                 """;
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_04()
    {
        var original = @"
public record Base
{
    public required int P { get; init; }
}
";

        var originalComp = CreateCompilationWithRequiredMembers(new[] { original, IsExternalInitTypeDefinition }, assemblyName: "original");
        CompileAndVerify(originalComp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

        var ilSource = GetShadowingRecordIl(propertyIsRequired: false);

        var il = CompileIL(ilSource);

        var c = @"
_ = new DerivedDerived1();
_ = new DerivedDerived2();
_ = new DerivedDerived3();

record DerivedDerived1 : Derived
{
    public DerivedDerived1()
    {
    }
}
record DerivedDerived2 : Derived
{
}
record DerivedDerived3() : Derived;
";

        var comp = CreateCompilation(c, new[] { il, originalComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (8,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public DerivedDerived1()
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived1").WithArguments("Derived").WithLocation(8, 12),
            // (12,8): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived2 : Derived
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived2").WithArguments("Derived").WithLocation(12, 8),
            // (15,8): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived3() : Derived;
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived3").WithArguments("Derived").WithLocation(15, 8)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_04_HasSetsRequiredMembers()
    {
        var original = @"
public record Base
{
    public required int P { get; init; }
}
";

        var originalComp = CreateCompilationWithRequiredMembers(new[] { original, IsExternalInitTypeDefinition }, assemblyName: "original");
        CompileAndVerify(originalComp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

        var ilSource = GetShadowingRecordIl(propertyIsRequired: false);

        var il = CompileIL(ilSource);

        var c = @"
using System.Diagnostics.CodeAnalysis;

_ = new DerivedDerived1();

record DerivedDerived1 : Derived
{
    [SetsRequiredMembers]
    public DerivedDerived1()
    {
    }
}
";

        var comp = CreateCompilation(c, new[] { il, originalComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_04_HasSetsRequiredMembers_ManualBaseCall()
    {
        var original = @"
public record Base
{
    public required int P { get; init; }
}
";

        var originalComp = CreateCompilationWithRequiredMembers(new[] { original, IsExternalInitTypeDefinition }, assemblyName: "original");
        CompileAndVerify(originalComp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

        var ilSource = GetShadowingRecordIl(propertyIsRequired: false);

        var il = CompileIL(ilSource);

        var c = @"
using System.Diagnostics.CodeAnalysis;

_ = new DerivedDerived1();

record DerivedDerived1 : Derived
{
    [SetsRequiredMembers]
    public DerivedDerived1()
    {
    }

    [SetsRequiredMembers]
    public DerivedDerived1(int unused) : base(null)
    {
    }

    public DerivedDerived1(bool unused) : base(null)
    {
    }
}
";

        var comp = CreateCompilation(c, new[] { il, originalComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (18,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public DerivedDerived1(bool unused) : base(null)
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived1").WithArguments("Derived").WithLocation(18, 12)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_06()
    {
        var original = @"
public record Base
{
    public required int P { get; init; }
}
";

        var originalComp = CreateCompilationWithRequiredMembers(new[] { original, IsExternalInitTypeDefinition }, assemblyName: "original");
        CompileAndVerify(originalComp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

        var ilSource = GetShadowingRecordIl(propertyIsRequired: true);
        var il = CompileIL(ilSource);

        var c = @"
_ = new DerivedDerived1();
_ = new DerivedDerived2();
_ = new DerivedDerived3();

record DerivedDerived1 : Derived
{
    public DerivedDerived1()
    {
    }
}
record DerivedDerived2 : Derived
{
}
record DerivedDerived3() : Derived;
";

        var comp = CreateCompilation(c, new[] { il, originalComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (8,12): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public DerivedDerived1()
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived1").WithArguments("Derived").WithLocation(8, 12),
            // (12,8): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived2 : Derived
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived2").WithArguments("Derived").WithLocation(12, 8),
            // (15,8): error CS9509: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived3() : Derived;
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived3").WithArguments("Derived").WithLocation(15, 8)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_06_HasSetsRequiredMembers()
    {
        var original = @"
public record Base
{
    public required int P { get; init; }
}
";

        var originalComp = CreateCompilationWithRequiredMembers(new[] { original, IsExternalInitTypeDefinition }, assemblyName: "original");
        CompileAndVerify(originalComp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

        var ilSource = GetShadowingRecordIl(propertyIsRequired: true);
        var il = CompileIL(ilSource);

        var c = @"
using System.Diagnostics.CodeAnalysis;

_ = new DerivedDerived1();

record DerivedDerived1 : Derived
{
    [SetsRequiredMembers]
    public DerivedDerived1()
    {
    }
}
";

        var comp = CreateCompilation(c, new[] { il, originalComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedFromMetadata_01()
    {
        var vb = @"
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

<RequiredMember>
Public Class Derived
    Inherits Base
    <RequiredMember>
    Public Shadows Property P As Integer

    Public Sub New()
    End Sub

    <SetsRequiredMembers>
    Public Sub New(unused As Integer)
    End Sub
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,9): error CS9508: The required members list for 'Derived' is malformed and cannot be interpreted.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "Derived").WithArguments("Derived").WithLocation(1, 9)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedFromMetadata_02()
    {
        var vb = @"
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

<RequiredMember>
Public Class Derived
    Inherits Base
    Public Shadows Property P As Integer

    Public Sub New()
    End Sub

    <SetsRequiredMembers>
    Public Sub New(unused As Integer)
    End Sub
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,9): error CS9508: The required members list for 'Derived' is malformed and cannot be interpreted.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "Derived").WithArguments("Derived").WithLocation(1, 9)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedFromMetadata_03()
    {
        var vb = @"
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.CompilerServices
<RequiredMember>
Public Class Base
    <RequiredMember>
    Public Property P As Integer
End Class

Public Class Derived
    Inherits Base
    Public Shadows Property P As Integer

    Public Sub New()
    End Sub

    <SetsRequiredMembers>
    Public Sub New(unused As Integer)
    End Sub
End Class
";

        var vbComp = CreateVisualBasicCompilationWithRequiredMembers(vb);
        CompileAndVerify(vbComp).VerifyDiagnostics();

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { vbComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,9): error CS9508: The required members list for 'Derived' is malformed and cannot be interpreted.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "Derived").WithArguments("Derived").WithLocation(1, 9)
        );
    }

    [Fact]
    public void InterpolatedStringHandlerWithRequiredMembers()
    {
        var code = @"
CustomHandler handler = $"""";

partial class CustomHandler
{
    public required int Field;
}";

        var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial class", useBoolReturns: false);

        var comp = CreateCompilationWithRequiredMembers(new[] { code, handler });
        comp.VerifyDiagnostics(
            // (2,25): error CS9506: Required member 'CustomHandler.Field' must be set in the object initializer or attribute constructor.
            // CustomHandler handler = $"";
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, @"$""""").WithArguments("CustomHandler.Field").WithLocation(2, 25)
        );
    }

    [Fact]
    public void CoClassWithRequiredMembers_NoneSet()
    {
        string code = @"
using System;
using System.Runtime.InteropServices;

_ = new I();

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(C))]
interface I
{
}

class C : I
{
    public required int P { get; set; }
}

";
        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (5,9): error CS9506: Required member 'C.P' must be set in the object initializer or attribute constructor.
            // _ = new I();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "I").WithArguments("C.P").WithLocation(5, 9)
        );
    }

    [Fact]
    public void CoClassWithRequiredMembers_AllSet()
    {
        string code = @"
using System;
using System.Runtime.InteropServices;

_ = new I() { P = 1 };

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(C))]
interface I
{
    public int P { get; set; }
}

class C : I
{
    public required int P { get; set; }
}
";
        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (5,9): error CS9506: Required member 'C.P' must be set in the object initializer or attribute constructor.
            // _ = new I() { P = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "I").WithArguments("C.P").WithLocation(5, 9)
        );
    }

    [Fact]
    public void RequiredMemberInAttribute_NotSet()
    {
        var code = @"
using System;

[Attr]
class C
{
}

class AttrAttribute : Attribute
{
    public required int P { get; set; }
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (4,2): error CS9506: Required member 'AttrAttribute.P' must be set in the object initializer or attribute constructor.
            // [Attr]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Attr").WithArguments("AttrAttribute.P").WithLocation(4, 2)
        );
    }

    [Fact]
    public void RequiredMemberInAttribute_AllSet()
    {
        var code = @"
using System;

[Attr(P = 1, F = 2)]
class C
{
}

class AttrAttribute : Attribute
{
    public required int P { get; set; }
    public required int F;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        var verifier = CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var attr = c.GetAttributes().Single();
            AssertEx.Equal("AttrAttribute", attr.AttributeClass.ToTestDisplayString());
            Assert.Equal(2, attr.CommonNamedArguments.Length);
            Assert.Equal(1, (int)attr.CommonNamedArguments[0].Value.ValueInternal!);
            Assert.Equal(2, (int)attr.CommonNamedArguments[1].Value.ValueInternal!);
        }
    }

    [Fact]
    public void RequiredMemberInAttribute_Recursive01()
    {
        var code = @"
using System;

[Attr(P = 1, F = 2)]
class AttrAttribute : Attribute
{
    public required int P { get; set; }
    public required int F;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        var verifier = CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var attrAttribute = module.GlobalNamespace.GetMember<NamedTypeSymbol>("AttrAttribute");
            var attr = attrAttribute.GetAttributes().Where(a => a.AttributeClass!.Name == "AttrAttribute").Single();
            AssertEx.Equal("AttrAttribute", attr.AttributeClass.ToTestDisplayString());
            Assert.Equal(2, attr.CommonNamedArguments.Length);
            Assert.Equal(1, (int)attr.CommonNamedArguments[0].Value.ValueInternal!);
            Assert.Equal(2, (int)attr.CommonNamedArguments[1].Value.ValueInternal!);
        }
    }

    [Fact]
    public void RequiredMemberInAttribute_Recursive02()
    {
        var code = @"
using System;

class AttrAttribute : Attribute
{
    [Attr(P = 1, F = 2)]
    public required int P { get; set; }
    public required int F;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        var verifier = CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var attrAttribute = module.GlobalNamespace.GetMember<NamedTypeSymbol>("AttrAttribute");
            var p = attrAttribute.GetMember<PropertySymbol>("P");
            var attr = p.GetAttributes().Where(a => a.AttributeClass!.Name == "AttrAttribute").Single();
            AssertEx.Equal("AttrAttribute", attr.AttributeClass.ToTestDisplayString());
            Assert.Equal(2, attr.CommonNamedArguments.Length);
            Assert.Equal(1, (int)attr.CommonNamedArguments[0].Value.ValueInternal!);
            Assert.Equal(2, (int)attr.CommonNamedArguments[1].Value.ValueInternal!);
        }
    }

    [Fact]
    public void RequiredMemberInAttribute_Recursive03()
    {
        var code = @"
using System;

class AttrAttribute : Attribute
{
    public required int P { get; set; }
    [Attr(P = 1, F = 2)]
    public required int F;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        var verifier = CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify);
        verifier.VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var attrAttribute = module.GlobalNamespace.GetMember<NamedTypeSymbol>("AttrAttribute");
            var f = attrAttribute.GetMember<FieldSymbol>("F");
            var attr = f.GetAttributes().Where(a => a.AttributeClass!.Name == "AttrAttribute").Single();
            AssertEx.Equal("AttrAttribute", attr.AttributeClass.ToTestDisplayString());
            Assert.Equal(2, attr.CommonNamedArguments.Length);
            Assert.Equal(1, (int)attr.CommonNamedArguments[0].Value.ValueInternal!);
            Assert.Equal(2, (int)attr.CommonNamedArguments[1].Value.ValueInternal!);
        }
    }

    [Fact]
    public void RequiredMemberInAttribute_Recursive04()
    {
        var code = @"
namespace System.Runtime.CompilerServices;

[RequiredMember]
public class RequiredMemberAttribute : Attribute
{
    public required int P { get; set; }
}
";

        var comp = CreateCompilation(new[] { code, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics(
            // (4,2): error CS9506: Required member 'RequiredMemberAttribute.P' must be set in the object initializer or attribute constructor.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "RequiredMember").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute.P").WithLocation(4, 2),
            // (4,2): error CS9504: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(4, 2)
        );
    }

    [Fact]
    public void RequiredMemberInAttribute_Recursive05()
    {
        var code = @"
namespace System.Runtime.CompilerServices;

public class RequiredMemberAttribute : Attribute
{
    [RequiredMember]
    public required int P { get; set; }
}
";

        var comp = CreateCompilation(new[] { code, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics(
            // (6,6): error CS9506: Required member 'RequiredMemberAttribute.P' must be set in the object initializer or attribute constructor.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "RequiredMember").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute.P").WithLocation(6, 6),
            // (6,6): error CS9504: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(6, 6)
        );
    }

    [Fact]
    public void SetsRequiredMemberInAttribute_Recursive()
    {
        var code = @"
namespace System.Diagnostics.CodeAnalysis;

public class SetsRequiredMembersAttribute : Attribute
{
    public required int P { get; set; }

    [SetsRequiredMembers]
    public SetsRequiredMembersAttribute()
    {
    }
}

public class C
{
    public required int Prop { get; set; }

    [SetsRequiredMembers]
    public C()
    {
    }

    static void M()
    {
        _ = new C();
    }
}
";

        var comp = CreateCompilation(new[] { code, RequiredMemberAttribute, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_01()
    {
        var code = @"
#pragma warning disable CS0649 // Field is never assigned to
#nullable enable
class C
{
    public required string P1 { get; set; }
    public required string F1;
    public string P2 { get; set; }
    public string F2;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (8,19): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public string P2 { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P2").WithArguments("property", "P2").WithLocation(8, 19),
            // (9,19): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public string F2;
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "F2").WithArguments("field", "F2").WithLocation(9, 19)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_02()
    {
        var code = @"
#pragma warning disable CS0649 // Field is never assigned to
#nullable enable
class C
{
    public required string P1 { get; set; }
    public required string F1;
    public string P2 { get; set; }
    public string F2;

    public C()
    {
    }

    public C(int _) {}
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (11,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public C()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(11, 12),
            // (11,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public C()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(11, 12),
            // (15,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public C(int _) {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(15, 12),
            // (15,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public C(int _) {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(15, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_03()
    {
        var code = @"
#pragma warning disable CS0649 // Field is never assigned to
#nullable enable
struct S
{
    public required string P1 { get; set; }
    public required string F1;
    public string P2 { get; set; }
    public string F2;
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_04()
    {
        var code = @"
#pragma warning disable CS0649 // Field is never assigned to
#nullable enable
struct S
{
    public required string P1 { get; set; }
    public required string F1;
    public string P2 { get; set; }
    public string F2;

    public S() {}
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (11,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public S() {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "P2").WithLocation(11, 12),
            // (11,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public S() {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "F2").WithLocation(11, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor()
    {
        var code = @"
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    public required string Field { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    public C() { }
}";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (9,12): warning CS8618: Non-nullable field '_field' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_field").WithLocation(9, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor()
    {
        var code = @"
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    public required string Field { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    public C() { }
    public C(bool unused) : this()
    { 
        _field.ToString();
        Field.ToString();
    }
}";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (9,12): warning CS8618: Non-nullable field '_field' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_field").WithLocation(9, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_01()
    {
        var code = @"
#nullable enable
public class C
{
    public required string Prop { get; set; }
    public required string Field;

    public C(bool unused) { }

    public C() : this(true)
    {
        Prop.ToString();
        Field.ToString();
    }
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (12,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop").WithLocation(12, 9),
            // (13,9): warning CS8602: Dereference of a possibly null reference.
            //         Field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field").WithLocation(13, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_02()
    {
        var code = @"
#nullable enable
public struct C
{
    public required string Prop { get; set; }
    public required string Field;

    public C(bool unused) { }

    public C() : this(true)
    {
        Prop.ToString();
        Field.ToString();
    }
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (11,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop").WithLocation(12, 9),
            // (13,9): warning CS8602: Dereference of a possibly null reference.
            //         Field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field").WithLocation(13, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_03()
    {
        var code = @"
#nullable enable
public struct C
{
    public required string Prop { get; set; }

    public C(bool unused) : this()
    {
        Prop.ToString();
    }
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (9,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop").WithLocation(9, 9)
        );
    }

    [Theory, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [InlineData(": base()")]
    [InlineData("")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_04(string baseSyntax)
    {
        var code = $$"""
#nullable enable
public class Base
{
    public required string Prop1 { get; set; }
    public string Prop2 { get; set; } = null!;
    public required string Field1;
    public string Field2 = null!;
}

public class Derived : Base
{
    public Derived() {{baseSyntax}}
    {
        Prop1.ToString();
        Prop2.ToString();
        Field1.ToString();
        Field2.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (14,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop1.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop1").WithLocation(14, 9),
            // (16,9): warning CS8602: Dereference of a possibly null reference.
            //         Field1.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field1").WithLocation(16, 9)
        );
    }

    [Theory, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [InlineData(": base()")]
    [InlineData("")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_05(string baseSyntax)
    {
        var code = $$"""
#nullable enable
public class Base
{
    public required string Prop1 { get; set; }
    public string Prop2 { get; set; } = null!;
    public required string Field1;
    public string Field2 = null!;
}

public class Derived : Base
{
    public required string Prop3 { get; set; }
    public string Prop4 { get; set; } = null!;
    public required string Field3;
    public string Field4 = null!;

    public Derived() {{baseSyntax}}
    {
        Prop1.ToString(); // 1
        Prop2.ToString();
        Prop3.ToString(); // 2
        Prop4.ToString();
        Field1.ToString(); // 1
        Field2.ToString();
        Field3.ToString(); // 2
        Field4.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (19,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop1.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop1").WithLocation(19, 9),
            // (21,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop3.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop3").WithLocation(21, 9),
            // (23,9): warning CS8602: Dereference of a possibly null reference.
            //         Field1.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field1").WithLocation(23, 9),
            // (25,9): warning CS8602: Dereference of a possibly null reference.
            //         Field3.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Field3").WithLocation(25, 9)
        );
    }

    [Theory, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [InlineData(": base()")]
    [InlineData("")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_06(string baseSyntax)
    {
        var code = @$"
#nullable enable
public class Base
{{
    public required string Prop1 {{ get; set; }}
    public string Prop2 {{ get; set; }} = null!;
}}

public class Derived : Base
{{
    public required string Prop3 {{ get; set; }}
    public string Prop4 {{ get; set; }}

    public Derived() {baseSyntax}
    {{
        Prop1.ToString(); // 1
        Prop2.ToString();
        Prop3.ToString(); // 2
        Prop4.ToString(); // 3
    }}
}}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (16,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop1.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop1").WithLocation(16, 9),
            // (18,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop3.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop3").WithLocation(18, 9),
            // (19,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop4.ToString(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop4").WithLocation(19, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_07()
    {
        var code = @"
#nullable enable
public class Base
{
    public required string Prop1 { get; set; }
    public string Prop2 { get; set; } = null!;
}

public class Derived : Base
{
    public required string Prop3 { get; set; }
    public string Prop4 { get; set; }

    public Derived(bool unused) { Prop4 = null!; }

    public Derived() : this(true)
    {
        Prop1.ToString(); // 1
        Prop2.ToString();
        Prop3.ToString(); // 2
        Prop4.ToString();
    }
}
";

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (18,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop1.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop1").WithLocation(18, 9),
            // (20,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop3.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop3").WithLocation(20, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_08()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            public class Base
            {
                public required string Prop1 { get; set; }
                public string Prop2 { get; set; } = null!;

                [SetsRequiredMembers]
                protected Base() {}
            }
            
            public class Derived : Base
            {
                public required string Prop3 { get; set; }
                public string Prop4 { get; set; }
            
                public Derived() : base()
                {
                    Prop1.ToString();
                    Prop2.ToString();
                    Prop3.ToString(); // 1
                    Prop4.ToString(); // 2
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (17,24): error CS9510: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : base()
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "base").WithLocation(17, 24),
            // (21,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop3.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop3").WithLocation(21, 9),
            // (22,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop4.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop4").WithLocation(22, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_09()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            public class Base
            {
                public required string Prop1 { get; set; }
                public string Prop2 { get; set; } = null!;

                protected Base() {}
            }
            
            public class Derived : Base
            {
                public required string Prop3 { get; set; }
                public string Prop4 { get; set; }

                [SetsRequiredMembers]
                public Derived(int unused) : base()
                {
                    Prop4 = null!;
                }
            
                public Derived() : this(0)
                {
                    Prop1.ToString();
                    Prop2.ToString();
                    Prop3.ToString();
                    Prop4.ToString();
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (22,24): error CS9510: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : this(0)
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(22, 24)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_10()
    {
        // This IL is equivalent to:
        // #nullable enable
        // [constructor: SetsRequiredMembers]
        // public record Base(bool unused) { public required string Prop { get; init; } }
        var il = """
            .assembly extern attr {}

            .class public auto ansi beforefieldinit Base
                extends [mscorlib]System.Object
                implements class [mscorlib]System.IEquatable`1<class Base>
            {
                .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
                    01 00 01 00 00
                )
                .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 00 00 00
                )
                // Fields
                .field public string Field1
                .custom instance void [attr]RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .field public string Field2
                .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 02 00 00
                )
                .custom instance void [attr]RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
            
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        bool 'unused'
                    ) cil managed 
                {
                    .custom instance void [attr]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::.ctor
            
                .method family hidebysig specialname newslot virtual 
                    instance class [mscorlib]System.Type get_EqualityContract () cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::get_EqualityContract
            
                .method public hidebysig specialname 
                    instance bool get_unused () cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::get_unused
            
                .method public hidebysig specialname 
                    instance void modreq([mscorlib]mscorlib.CompilerServices.IsExternalInit) set_unused (
                        bool 'value'
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::set_unused
            
                .method public hidebysig virtual 
                    instance string ToString () cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::ToString
            
                .method family hidebysig newslot virtual 
                    instance bool PrintMembers (
                        class [mscorlib]System.Text.StringBuilder builder
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::PrintMembers
            
                .method public hidebysig specialname static 
                    bool op_Inequality (
                        class Base left,
                        class Base right
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::op_Inequality
            
                .method public hidebysig specialname static 
                    bool op_Equality (
                        class Base left,
                        class Base right
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::op_Equality
            
                .method public hidebysig virtual 
                    instance int32 GetHashCode () cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::GetHashCode
            
                .method public hidebysig virtual 
                    instance bool Equals (
                        object obj
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::Equals
            
                .method public hidebysig newslot virtual 
                    instance bool Equals (
                        class Base other
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::Equals
            
                .method public hidebysig newslot virtual 
                    instance class Base '<Clone>$' () cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::'<Clone>$'
            
                .method family hidebysig specialname rtspecialname 
                    instance void .ctor (
                        class Base original
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::.ctor
            
                .method public hidebysig 
                    instance void Deconstruct (
                        [out] bool& 'unused'
                    ) cil managed 
                {
                    .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Base::Deconstruct
            
                // Properties
                .property instance class [mscorlib]System.Type EqualityContract()
                {
                    .get instance class [mscorlib]System.Type Base::get_EqualityContract()
                }
                .property instance bool 'unused'()
                {
                    .get instance bool Base::get_unused()
                    .set instance void modreq([mscorlib]mscorlib.CompilerServices.IsExternalInit) Base::set_unused(bool)
                }
            } // end of class Base

            .class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableAttribute
                extends [mscorlib]System.Attribute
            {
                .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
                    01 00 84 6b 00 00 02 00 54 02 0d 41 6c 6c 6f 77
                    4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
                    72 69 74 65 64 00
                )
                // Fields
                .field public initonly uint8[] NullableFlags
            
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        uint8 ''
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method NullableAttribute::.ctor
            
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        uint8[] ''
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method NullableAttribute::.ctor
            } // end of class mscorlib.CompilerServices.NullableAttribute
            
            .class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableContextAttribute
                extends [mscorlib]System.Attribute
            {
                .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
                    01 00 4c 14 00 00 02 00 54 02 0d 41 6c 6c 6f 77
                    4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
                    72 69 74 65 64 00
                )
                // Fields
                .field public initonly uint8 Flag
            
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        uint8 ''
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method NullableContextAttribute::.ctor
            }
            .class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
                extends [mscorlib]System.Attribute
            {
                .custom instance void [mscorlib]mscorlib.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldnull
                    throw
                } // end of method EmbeddedAttribute::.ctor
            } // end of class Microsoft.CodeAnalysis.EmbeddedAttribute
            """;

        var attrComp = CreateCompilationWithRequiredMembers("", assemblyName: "attr");

        var code = """
            #nullable enable
            public record Derived(bool unused) : Base(unused);
            """;

        var comp = CreateCompilationWithIL(code, ilSource: il, references: new[] { attrComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (2,42): error CS9510: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            // public record Derived(bool unused) : Base(unused);
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "(unused)").WithLocation(2, 42)
        );
    }

    [Fact]
    public void SetsRequiredMembersAppliedToRecordCopyConstructor_DeclaredInType()
    {
        var code = """
            public record C
            {
                public required string Prop1 { get; set; }
                public required int Field1;
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

        void validate(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetTypeMember("C");
            var copyCtor = c.GetMembers(".ctor").Cast<MethodSymbol>().Single(m => m.ParameterCount == 1);

            if (copyCtor is SynthesizedRecordCopyCtor)
            {
                Assert.Empty(copyCtor.GetAttributes());
            }
            else
            {
                AssertEx.Equal("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute..ctor()",
                               copyCtor.GetAttributes().Single(a => a.AttributeClass!.IsWellKnownSetsRequiredMembersAttribute()).AttributeConstructor.ToTestDisplayString());
            }
        }
    }

    [Fact]
    public void SetsRequiredMembersAppliedToRecordCopyConstructor_DeclaredInType_SetsRequiredMembersMissing()
    {
        var code = """
            public record C
            {
                public required string Prop1 { get; set; }
                public required int Field1;
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor);
        comp.VerifyDiagnostics(
            // (1,15): error CS0656: Missing compiler required member 'System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute..ctor'
            // public record C
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute", ".ctor").WithLocation(1, 15)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_11()
    {
        var code = """
            #nullable enable
            public class Base
            {
                public required string Prop1 { get; set; }
                public string Prop2 { get; set; } = null!;
            }
            
            public class Derived : Base
            {
                public required string Prop3 { get; set; } = Prop1.ToString();
                public string Prop4 { get; set; } = Prop2.ToString();
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (10,50): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Base.Prop1'
            //     public required string Prop3 { get; set; } = Prop1.ToString();
            Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Prop1").WithArguments("Base.Prop1").WithLocation(10, 50),
            // (11,41): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Base.Prop2'
            //     public string Prop4 { get; set; } = Prop2.ToString();
            Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Prop2").WithArguments("Base.Prop2").WithLocation(11, 41)
        );
    }

    [Theory]
    [CombinatorialData]
    public void SetsRequiredMembersAppliedToRecordCopyConstructor_DeclaredInBase(bool useMetadataReference)
    {
        var @base = """
            public record Base 
            {
                public required string Prop1 { get; set; }
                public required int Field1;
            }
            """;

        var code = """
            public record Derived : Base;
            """;

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        CompileAndVerify(baseComp).VerifyDiagnostics();

        comp = CreateCompilation(code, references: new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

        void validate(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetTypeMember("Derived");
            var copyCtor = c.GetMembers(".ctor").Cast<MethodSymbol>().Single(m => m.ParameterCount == 1);

            if (copyCtor is SynthesizedRecordCopyCtor)
            {
                Assert.Empty(copyCtor.GetAttributes());
            }
            else
            {
                AssertEx.Equal("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute..ctor()",
                               copyCtor.GetAttributes().Single(a => a.AttributeClass!.IsWellKnownSetsRequiredMembersAttribute()).AttributeConstructor.ToTestDisplayString());
            }
        }
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("class")]
    public void ForbidRequiredAsNew_NoInheritance(string typeKind)
    {
        var code = $$"""
            M<C>();

            void M<T>() where T : new()
            {
            }

            {{typeKind}} C
            {
                public required int Prop1 { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (1,1): error CS9511: 'C' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'C' has required members.
            // M<C>();
            Diagnostic(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, "M<C>").WithArguments("M<T>()", "T", "C").WithLocation(1, 1)
        );
    }

    [Theory]
    [CombinatorialData]
    public void ForbidRequiredAsNew_Inheritance(bool useMetadataReference)
    {
        var @base = """
            public class Base
            {
                public required int Prop1 { get; set; }
            }
            """;

        var code = """
            M<Derived>();

            void M<T>() where T : new()
            {
            }

            class Derived : Base
            {
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, code });
        comp.VerifyDiagnostics(
            // (1,1): error CS9511: 'Derived' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'Derived' has required members.
            // M<Derived>();
            Diagnostic(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, "M<Derived>").WithArguments("M<T>()", "T", "Derived").WithLocation(1, 1)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        CompileAndVerify(baseComp).VerifyDiagnostics();

        comp = CreateCompilation(code, references: new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error CS9511: 'Derived' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'Derived' has required members.
            // M<Derived>();
            Diagnostic(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, "M<Derived>").WithArguments("M<T>()", "T", "Derived").WithLocation(1, 1)
        );
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void AllowRequiredAsNew_SetsRequiredMembersOnConstructor(string typeKind)
    {
        var code = $$"""
            using System.Diagnostics.CodeAnalysis;
            M<C>();

            void M<T>() where T : new()
            {
            }

            {{typeKind}} C
            {
                public required int Prop1 { get; set; }

                [SetsRequiredMembers]
                public C()
                {
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Fact]
    public void AllowRequiredAsNew_IndirectionViaStruct()
    {
        var code = """
            M1<C>();

            void M1<T>() where T : struct
            {
                M2<T>();
            }

            void M2<T>() where T : new()
            {
            }

            struct C
            {
                public required int Prop1 { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        CompileAndVerify(comp).VerifyDiagnostics();
    }

    [Fact]
    public void HasExistingObsoleteAttribute_RequiredMembersOnSelf()
    {
        var code = """
            using System;
            class C
            {
                public required int Prop { get; set; }

                [Obsolete("Reason 1", false)]
                public C() { }


                [Obsolete("Reason 2", true)]
                public C(int unused) { }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol m)
        {
            var c = m.GlobalNamespace.GetTypeMember("C");
            var ctors = c.GetMembers(".ctor").Cast<MethodSymbol>().ToArray();

            Assert.Equal(2, ctors.Length);

            assertAttributeData(ctors[0], "Reason 1", expectedError: false);
            assertAttributeData(ctors[1], "Reason 2", expectedError: true);

            static void assertAttributeData(MethodSymbol ctor, string expectedReason, bool expectedError)
            {
                var attrData = ctor.GetAttributes().Single();
                AssertEx.Equal("System.ObsoleteAttribute", attrData.AttributeClass.ToTestDisplayString());
                var attrArgs = attrData.ConstructorArguments.ToArray();
                Assert.Equal(2, attrArgs.Length);
                AssertEx.Equal(expectedReason, (string)attrArgs[0].ValueInternal!);
                Assert.Equal(expectedError, (bool)attrArgs[1].ValueInternal!);
            }
        }
    }

    [Fact]
    public void HasExistingObsoleteAttribute_RequiredMembersOnBase()
    {
        var code = """
            using System;
            class Base
            {
                public required int Prop { get; set; }
            }

            class Derived : Base
            {
                [Obsolete("Reason 1", false)]
                public Derived() { }


                [Obsolete("Reason 2", true)]
                public Derived(int unused) { }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol m)
        {
            var c = m.GlobalNamespace.GetTypeMember("Derived");
            var ctors = c.GetMembers(".ctor").Cast<MethodSymbol>().ToArray();

            Assert.Equal(2, ctors.Length);

            assertAttributeData(ctors[0], "Reason 1", expectedError: false);
            assertAttributeData(ctors[1], "Reason 2", expectedError: true);

            static void assertAttributeData(MethodSymbol ctor, string expectedReason, bool expectedError)
            {
                var attrData = ctor.GetAttributes().Single();
                AssertEx.Equal("System.ObsoleteAttribute", attrData.AttributeClass.ToTestDisplayString());
                var attrArgs = attrData.ConstructorArguments.ToArray();
                Assert.Equal(2, attrArgs.Length);
                AssertEx.Equal(expectedReason, (string)attrArgs[0].ValueInternal!);
                Assert.Equal(expectedError, (bool)attrArgs[1].ValueInternal!);
            }
        }
    }
}
