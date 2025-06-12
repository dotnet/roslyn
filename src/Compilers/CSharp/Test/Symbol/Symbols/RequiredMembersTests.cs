// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

[CompilerTrait(CompilerFeature.RequiredMembers)]
public class RequiredMembersTests : CSharpTestBase
{
    private static CSharpCompilation CreateCompilationWithRequiredMembers(CSharpTestSource source, IEnumerable<MetadataReference>? references = null, CSharpParseOptions? parseOptions = null, CSharpCompilationOptions? options = null, string? assemblyName = null, TargetFramework targetFramework = TargetFramework.Standard)
        => CreateCompilation(new[] { source, RequiredMemberAttribute, SetsRequiredMembersAttribute, CompilerFeatureRequiredAttribute }, references, options: options, parseOptions: parseOptions, assemblyName: assemblyName, targetFramework: targetFramework);

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
                Assert.All(member.GetAttributes(), attr => AssertEx.NotEqual("System.Runtime.CompilerServices.RequiredMemberAttribute", attr.AttributeClass.ToTestDisplayString()));

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
            // (5,27): error CS8936: Feature 'required members' is not available in C# 10.0. Please use language version 11.0 or greater.
            //     internal required int Field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "Field").WithArguments("required members", "11.0").WithLocation(5, 27),
            // (6,27): error CS8936: Feature 'required members' is not available in C# 10.0. Please use language version 11.0 or greater.
            //     internal required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "Prop").WithArguments("required members", "11.0").WithLocation(6, 27)
        );

        comp = CreateCompilationWithRequiredMembers(code, parseOptions: TestOptions.Regular11);
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
        var comp = CreateCompilationWithRequiredMembers(code, parseOptions: use10 ? TestOptions.Regular10 : TestOptions.Regular11);

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
                    // (4,12): error CS9029: Types and aliases cannot be named 'required'.
                    //     struct required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(4, 12),
                    // (8,11): error CS9029: Types and aliases cannot be named 'required'.
                    //     class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(8, 11),
                    // (12,15): error CS9029: Types and aliases cannot be named 'required'.
                    //     interface required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(12, 15),
                    // (16,19): error CS9029: Types and aliases cannot be named 'required'.
                    //     delegate void required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(16, 19),
                    // (20,12): error CS9029: Types and aliases cannot be named 'required'.
                    //     record required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(20, 12),
                    // (24,19): error CS9029: Types and aliases cannot be named 'required'.
                    //     record struct required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(24, 19),
                    // (30,15): error CS9029: Types and aliases cannot be named 'required'.
                    //         class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(30, 15),
                    // (35,11): error CS9029: Types and aliases cannot be named 'required'.
                    //     class required<T> {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(35, 11)
                }
        );

        code = code.Replace("required", "@required");
        comp = CreateCompilationWithRequiredMembers(code, parseOptions: use10 ? TestOptions.Regular10 : TestOptions.Regular11);
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
            // (8,25): error CS9030: 'Derived.Prop' must be required because it overrides required member 'Base.Prop'
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("Derived.Prop", "Base.Prop").WithLocation(8, 25)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();

        comp = CreateCompilation(derived, references: new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (4,25): error CS9030: 'Derived.Prop' must be required because it overrides required member 'Base.Prop'
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
            // (12,25): error CS9030: 'DerivedDerived.Prop' must be required because it overrides required member 'Derived.Prop'
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
            // (4,25): error CS9030: 'DerivedDerived.Prop' must be required because it overrides required member 'Derived.Prop'
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
            // (10,20): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived1.Field'.
            //     public new int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived1.Field").WithLocation(10, 20),
            // (11,20): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived1.Prop'.
            //     public new int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived1.Prop").WithLocation(11, 20),
            // (15,20): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived2.Prop'.
            //     public new int Prop; // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived2.Prop").WithLocation(15, 20),
            // (16,20): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived2.Field'.
            //     public new int Field { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived2.Field").WithLocation(16, 20),
            // (20,16): warning CS0108: 'Derived3.Field' hides inherited member 'Base.Field'. Use the new keyword if hiding was intended.
            //     public int Field; // 1
            Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Derived3.Field", "Base.Field").WithLocation(20, 16),
            // (20,16): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived3.Field'.
            //     public int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived3.Field").WithLocation(20, 16),
            // (21,16): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived3.Prop'.
            //     public int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived3.Prop").WithLocation(21, 16)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        baseComp.VerifyDiagnostics();

        comp = CreateCompilation("#pragma warning disable CS0649 // Never assigned" + derived, new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (4,20): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived1.Field'.
            //     public new int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived1.Field").WithLocation(4, 20),
            // (5,20): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived1.Prop'.
            //     public new int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived1.Prop").WithLocation(5, 20),
            // (9,20): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived2.Prop'.
            //     public new int Prop; // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived2.Prop").WithLocation(9, 20),
            // (10,20): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived2.Field'.
            //     public new int Field { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived2.Field").WithLocation(10, 20),
            // (14,16): warning CS0108: 'Derived3.Field' hides inherited member 'Base.Field'. Use the new keyword if hiding was intended.
            //     public int Field; // 1
            Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Derived3.Field", "Base.Field").WithLocation(14, 16),
            // (14,16): error CS9031: Required member 'Base.Field' cannot be hidden by 'Derived3.Field'.
            //     public int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeHidden, "Field").WithArguments("Base.Field", "Derived3.Field").WithLocation(14, 16),
            // (15,16): error CS9031: Required member 'Base.Prop' cannot be hidden by 'Derived3.Prop'.
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
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; } // 17
        private protected required int PrivateProtectedProperty { get; set; } // 18
        private required int PrivateProperty { get; set; } // 19
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField; // 20
        private protected required int PrivateProtectedField; // 21
        private required int PrivateField; // 22
    }
    protected class ProtectedClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; } // 23
        private protected required int PrivateProtectedProperty { get; set; } // 24
        private required int PrivateProperty { get; set; } // 25
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField; // 26
        private protected required int PrivateProtectedField; // 27
        private required int PrivateField; // 28
    }
    private protected class PrivateProtectedClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; } // 29
        private protected required int PrivateProtectedProperty { get; set; } // 30
        private required int PrivateProperty { get; set; } // 31
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField; // 32
        private protected required int PrivateProtectedField; // 33
        private required int PrivateField; // 34
    }
    private class PrivateClass
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; }
        internal required int InternalProperty { get; set; }
        protected required int ProtectedProperty { get; set; } // 35
        private protected required int PrivateProtectedProperty { get; set; } // 36
        private required int PrivateProperty { get; set; } // 37
        public required int PublicField;
        internal protected required int InternalProtectedField;
        internal required int InternalField;
        protected required int ProtectedField; // 38
        private protected required int PrivateProtectedField; // 39
        private required int PrivateField; // 40
    }
}
public class Outer2
{
    protected internal class ProtectedInternalClass2
    {
        public required int PublicProperty { get; set; }
        internal protected required int InternalProtectedProperty { get; set; } // 41
        internal required int InternalProperty { get; set; } // 42
        protected required int ProtectedProperty { get; set; } // 43
        private protected required int PrivateProtectedProperty { get; set; } // 44
        private required int PrivateProperty { get; set; } // 45
        public required int PublicField;
        internal protected required int InternalProtectedField; // 46
        internal required int InternalField; // 47
        protected required int ProtectedField; // 48
        private protected required int PrivateProtectedField; // 49
        private required int PrivateField; // 50
    }
}
");

        comp.VerifyDiagnostics(
            // (7,37): error CS9032: Required member 'PublicClass.InternalProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedProperty { get; set; } // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedProperty").WithArguments("PublicClass.InternalProtectedProperty", "PublicClass").WithLocation(7, 37),
            // (8,27): error CS9032: Required member 'PublicClass.InternalProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal required int InternalProperty { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("PublicClass.InternalProperty", "PublicClass").WithLocation(8, 27),
            // (9,28): error CS9032: Required member 'PublicClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedProperty { get; set; } // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("PublicClass.ProtectedProperty", "PublicClass").WithLocation(9, 28),
            // (10,36): error CS9032: Required member 'PublicClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedProperty { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("PublicClass.PrivateProtectedProperty", "PublicClass").WithLocation(10, 36),
            // (11,26): error CS9032: Required member 'PublicClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private required int PrivateProperty { get; set; } // 5
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("PublicClass.PrivateProperty", "PublicClass").WithLocation(11, 26),
            // (13,37): error CS9032: Required member 'PublicClass.InternalProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedField; // 6
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedField").WithArguments("PublicClass.InternalProtectedField", "PublicClass").WithLocation(13, 37),
            // (14,27): error CS9032: Required member 'PublicClass.InternalField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     internal required int InternalField; // 7
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("PublicClass.InternalField", "PublicClass").WithLocation(14, 27),
            // (15,28): error CS9032: Required member 'PublicClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedField; // 8
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("PublicClass.ProtectedField", "PublicClass").WithLocation(15, 28),
            // (16,36): error CS9032: Required member 'PublicClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedField; // 9
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("PublicClass.PrivateProtectedField", "PublicClass").WithLocation(16, 36),
            // (17,26): error CS9032: Required member 'PublicClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     private required int PrivateField; // 10
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("PublicClass.PrivateField", "PublicClass").WithLocation(17, 26),
            // (24,28): error CS9032: Required member 'InternalClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedProperty { get; set; } // 11
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("InternalClass.ProtectedProperty", "InternalClass").WithLocation(24, 28),
            // (25,36): error CS9032: Required member 'InternalClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedProperty { get; set; } // 12
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("InternalClass.PrivateProtectedProperty", "InternalClass").WithLocation(25, 36),
            // (26,26): error CS9032: Required member 'InternalClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private required int PrivateProperty { get; set; } // 13
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("InternalClass.PrivateProperty", "InternalClass").WithLocation(26, 26),
            // (30,28): error CS9032: Required member 'InternalClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedField; // 14
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("InternalClass.ProtectedField", "InternalClass").WithLocation(30, 28),
            // (31,36): error CS9032: Required member 'InternalClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedField; // 15
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("InternalClass.PrivateProtectedField", "InternalClass").WithLocation(31, 36),
            // (32,26): error CS9032: Required member 'InternalClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     private required int PrivateField; // 16
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("InternalClass.PrivateField", "InternalClass").WithLocation(32, 26),
            // (41,32): error CS9032: Required member 'Outer.ProtectedInternalClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedProperty { get; set; } // 17
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.ProtectedInternalClass.ProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(41, 32),
            // (42,40): error CS9032: Required member 'Outer.ProtectedInternalClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 18
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(42, 40),
            // (43,30): error CS9032: Required member 'Outer.ProtectedInternalClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateProperty { get; set; } // 19
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProperty", "Outer.ProtectedInternalClass").WithLocation(43, 30),
            // (47,32): error CS9032: Required member 'Outer.ProtectedInternalClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedField; // 20
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.ProtectedInternalClass.ProtectedField", "Outer.ProtectedInternalClass").WithLocation(47, 32),
            // (48,40): error CS9032: Required member 'Outer.ProtectedInternalClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedField; // 21
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedField", "Outer.ProtectedInternalClass").WithLocation(48, 40),
            // (49,30): error CS9032: Required member 'Outer.ProtectedInternalClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateField; // 22
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedInternalClass.PrivateField", "Outer.ProtectedInternalClass").WithLocation(49, 30),
            // (56,32): error CS9032: Required member 'Outer.ProtectedClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         protected required int ProtectedProperty { get; set; } // 23
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.ProtectedClass.ProtectedProperty", "Outer.ProtectedClass").WithLocation(56, 32),
            // (57,40): error CS9032: Required member 'Outer.ProtectedClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 24
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedClass.PrivateProtectedProperty", "Outer.ProtectedClass").WithLocation(57, 40),
            // (58,30): error CS9032: Required member 'Outer.ProtectedClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateProperty { get; set; } // 25
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedClass.PrivateProperty", "Outer.ProtectedClass").WithLocation(58, 30),
            // (62,32): error CS9032: Required member 'Outer.ProtectedClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         protected required int ProtectedField; // 26
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.ProtectedClass.ProtectedField", "Outer.ProtectedClass").WithLocation(62, 32),
            // (63,40): error CS9032: Required member 'Outer.ProtectedClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedField; // 27
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedClass.PrivateProtectedField", "Outer.ProtectedClass").WithLocation(63, 40),
            // (64,30): error CS9032: Required member 'Outer.ProtectedClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateField; // 28
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedClass.PrivateField", "Outer.ProtectedClass").WithLocation(64, 30),
            // (71,32): error CS9032: Required member 'Outer.PrivateProtectedClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         protected required int ProtectedProperty { get; set; } // 29
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.PrivateProtectedClass.ProtectedProperty", "Outer.PrivateProtectedClass").WithLocation(71, 32),
            // (72,40): error CS9032: Required member 'Outer.PrivateProtectedClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 30
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.PrivateProtectedClass.PrivateProtectedProperty", "Outer.PrivateProtectedClass").WithLocation(72, 40),
            // (73,30): error CS9032: Required member 'Outer.PrivateProtectedClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateProperty { get; set; } // 31
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.PrivateProtectedClass.PrivateProperty", "Outer.PrivateProtectedClass").WithLocation(73, 30),
            // (77,32): error CS9032: Required member 'Outer.PrivateProtectedClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         protected required int ProtectedField; // 32
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.PrivateProtectedClass.ProtectedField", "Outer.PrivateProtectedClass").WithLocation(77, 32),
            // (78,40): error CS9032: Required member 'Outer.PrivateProtectedClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private protected required int PrivateProtectedField; // 33
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.PrivateProtectedClass.PrivateProtectedField", "Outer.PrivateProtectedClass").WithLocation(78, 40),
            // (79,30): error CS9032: Required member 'Outer.PrivateProtectedClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateField; // 34
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.PrivateProtectedClass.PrivateField", "Outer.PrivateProtectedClass").WithLocation(79, 30),
            // (86,32): error CS9032: Required member 'Outer.PrivateClass.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         protected required int ProtectedProperty { get; set; } // 35
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.PrivateClass.ProtectedProperty", "Outer.PrivateClass").WithLocation(86, 32),
            // (87,40): error CS9032: Required member 'Outer.PrivateClass.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 36
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.PrivateClass.PrivateProtectedProperty", "Outer.PrivateClass").WithLocation(87, 40),
            // (88,30): error CS9032: Required member 'Outer.PrivateClass.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         private required int PrivateProperty { get; set; } // 37
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.PrivateClass.PrivateProperty", "Outer.PrivateClass").WithLocation(88, 30),
            // (92,32): error CS9032: Required member 'Outer.PrivateClass.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         protected required int ProtectedField; // 38
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.PrivateClass.ProtectedField", "Outer.PrivateClass").WithLocation(92, 32),
            // (93,40): error CS9032: Required member 'Outer.PrivateClass.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         private protected required int PrivateProtectedField; // 39
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.PrivateClass.PrivateProtectedField", "Outer.PrivateClass").WithLocation(93, 40),
            // (94,30): error CS9032: Required member 'Outer.PrivateClass.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         private required int PrivateField; // 40
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.PrivateClass.PrivateField", "Outer.PrivateClass").WithLocation(94, 30),
            // (102,41): error CS9032: Required member 'Outer2.ProtectedInternalClass2.InternalProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         internal protected required int InternalProtectedProperty { get; set; } // 41
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedProperty").WithArguments("Outer2.ProtectedInternalClass2.InternalProtectedProperty", "Outer2.ProtectedInternalClass2").WithLocation(102, 41),
            // (103,31): error CS9032: Required member 'Outer2.ProtectedInternalClass2.InternalProperty' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         internal required int InternalProperty { get; set; } // 42
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("Outer2.ProtectedInternalClass2.InternalProperty", "Outer2.ProtectedInternalClass2").WithLocation(103, 31),
            // (104,32): error CS9032: Required member 'Outer2.ProtectedInternalClass2.ProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         protected required int ProtectedProperty { get; set; } // 43
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer2.ProtectedInternalClass2.ProtectedProperty", "Outer2.ProtectedInternalClass2").WithLocation(104, 32),
            // (105,40): error CS9032: Required member 'Outer2.ProtectedInternalClass2.PrivateProtectedProperty' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         private protected required int PrivateProtectedProperty { get; set; } // 44
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer2.ProtectedInternalClass2.PrivateProtectedProperty", "Outer2.ProtectedInternalClass2").WithLocation(105, 40),
            // (106,30): error CS9032: Required member 'Outer2.ProtectedInternalClass2.PrivateProperty' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         private required int PrivateProperty { get; set; } // 45
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer2.ProtectedInternalClass2.PrivateProperty", "Outer2.ProtectedInternalClass2").WithLocation(106, 30),
            // (108,41): error CS9032: Required member 'Outer2.ProtectedInternalClass2.InternalProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         internal protected required int InternalProtectedField; // 46
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtectedField").WithArguments("Outer2.ProtectedInternalClass2.InternalProtectedField", "Outer2.ProtectedInternalClass2").WithLocation(108, 41),
            // (109,31): error CS9032: Required member 'Outer2.ProtectedInternalClass2.InternalField' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         internal required int InternalField; // 47
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("Outer2.ProtectedInternalClass2.InternalField", "Outer2.ProtectedInternalClass2").WithLocation(109, 31),
            // (110,32): error CS9032: Required member 'Outer2.ProtectedInternalClass2.ProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         protected required int ProtectedField; // 48
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer2.ProtectedInternalClass2.ProtectedField", "Outer2.ProtectedInternalClass2").WithLocation(110, 32),
            // (111,40): error CS9032: Required member 'Outer2.ProtectedInternalClass2.PrivateProtectedField' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         private protected required int PrivateProtectedField; // 49
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer2.ProtectedInternalClass2.PrivateProtectedField", "Outer2.ProtectedInternalClass2").WithLocation(111, 40),
            // (112,30): error CS9032: Required member 'Outer2.ProtectedInternalClass2.PrivateField' cannot be less visible or have a setter less visible than the containing type 'Outer2.ProtectedInternalClass2'.
            //         private required int PrivateField; // 50
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer2.ProtectedInternalClass2.PrivateField", "Outer2.ProtectedInternalClass2").WithLocation(112, 30)
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
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; } // 9
        public required int PrivateProtected { get; private protected set; } // 10
        public required int Private { get; private set; } // 11
    }
    protected class ProtectedClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; } // 12
        public required int PrivateProtected { get; private protected set; } // 13
        public required int Private { get; private set; } // 14
    }
    private protected class PrivateProtectedClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; } // 15
        public required int PrivateProtected { get; private protected set; } // 16
        public required int Private { get; private set; } // 17
    }
    private class PrivateClass
    {
        public required int InternalProtected { get; internal protected set; }
        public required int Internal { get; internal set; }
        public required int Protected { get; protected set; } // 18
        public required int PrivateProtected { get; private protected set; } // 19
        public required int Private { get; private set; } // 20
    }
}
public class Outer2
{
    protected internal class InternalProtectedClass2
    {
        public required int InternalProtected { get; internal protected set; } // 21
        public required int Internal { get; internal set; } // 22
        public required int Protected { get; protected set; } // 23
        public required int PrivateProtected { get; private protected set; } // 24
        public required int Private { get; private set; } // 25
    }
}
");

        comp.VerifyDiagnostics(
            // (4,25): error CS9032: Required member 'PublicClass.InternalProtected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int InternalProtected { get; internal protected set; } // 1
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtected").WithArguments("PublicClass.InternalProtected", "PublicClass").WithLocation(4, 25),
            // (5,25): error CS9032: Required member 'PublicClass.Internal' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Internal { get; internal set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Internal").WithArguments("PublicClass.Internal", "PublicClass").WithLocation(5, 25),
            // (6,25): error CS9032: Required member 'PublicClass.Protected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Protected { get; protected set; } // 3
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("PublicClass.Protected", "PublicClass").WithLocation(6, 25),
            // (7,25): error CS9032: Required member 'PublicClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int PrivateProtected { get; private protected set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("PublicClass.PrivateProtected", "PublicClass").WithLocation(7, 25),
            // (8,25): error CS9032: Required member 'PublicClass.Private' cannot be less visible or have a setter less visible than the containing type 'PublicClass'.
            //     public required int Private { get; private set; } // 5
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("PublicClass.Private", "PublicClass").WithLocation(8, 25),
            // (14,25): error CS9032: Required member 'InternalClass.Protected' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int Protected { get; protected set; } // 6
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("InternalClass.Protected", "InternalClass").WithLocation(14, 25),
            // (15,25): error CS9032: Required member 'InternalClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int PrivateProtected { get; private protected set; } // 7
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("InternalClass.PrivateProtected", "InternalClass").WithLocation(15, 25),
            // (16,25): error CS9032: Required member 'InternalClass.Private' cannot be less visible or have a setter less visible than the containing type 'InternalClass'.
            //     public required int Private { get; private set; } // 8
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("InternalClass.Private", "InternalClass").WithLocation(16, 25),
            // (24,29): error CS9032: Required member 'Outer.InternalProtectedClass.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int Protected { get; protected set; } // 9
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer.InternalProtectedClass.Protected", "Outer.InternalProtectedClass").WithLocation(24, 29),
            // (25,29): error CS9032: Required member 'Outer.InternalProtectedClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int PrivateProtected { get; private protected set; } // 10
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.InternalProtectedClass.PrivateProtected", "Outer.InternalProtectedClass").WithLocation(25, 29),
            // (26,29): error CS9032: Required member 'Outer.InternalProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.InternalProtectedClass'.
            //         public required int Private { get; private set; } // 11
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.InternalProtectedClass.Private", "Outer.InternalProtectedClass").WithLocation(26, 29),
            // (32,29): error CS9032: Required member 'Outer.ProtectedClass.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         public required int Protected { get; protected set; } // 12
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer.ProtectedClass.Protected", "Outer.ProtectedClass").WithLocation(32, 29),
            // (33,29): error CS9032: Required member 'Outer.ProtectedClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         public required int PrivateProtected { get; private protected set; } // 13
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.ProtectedClass.PrivateProtected", "Outer.ProtectedClass").WithLocation(33, 29),
            // (34,29): error CS9032: Required member 'Outer.ProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.ProtectedClass'.
            //         public required int Private { get; private set; } // 14
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.ProtectedClass.Private", "Outer.ProtectedClass").WithLocation(34, 29),
            // (40,29): error CS9032: Required member 'Outer.PrivateProtectedClass.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         public required int Protected { get; protected set; } // 15
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer.PrivateProtectedClass.Protected", "Outer.PrivateProtectedClass").WithLocation(40, 29),
            // (41,29): error CS9032: Required member 'Outer.PrivateProtectedClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         public required int PrivateProtected { get; private protected set; } // 16
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.PrivateProtectedClass.PrivateProtected", "Outer.PrivateProtectedClass").WithLocation(41, 29),
            // (42,29): error CS9032: Required member 'Outer.PrivateProtectedClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         public required int Private { get; private set; } // 17
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.PrivateProtectedClass.Private", "Outer.PrivateProtectedClass").WithLocation(42, 29),
            // (48,29): error CS9032: Required member 'Outer.PrivateClass.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         public required int Protected { get; protected set; } // 18
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer.PrivateClass.Protected", "Outer.PrivateClass").WithLocation(48, 29),
            // (49,29): error CS9032: Required member 'Outer.PrivateClass.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         public required int PrivateProtected { get; private protected set; } // 19
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer.PrivateClass.PrivateProtected", "Outer.PrivateClass").WithLocation(49, 29),
            // (50,29): error CS9032: Required member 'Outer.PrivateClass.Private' cannot be less visible or have a setter less visible than the containing type 'Outer.PrivateClass'.
            //         public required int Private { get; private set; } // 20
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer.PrivateClass.Private", "Outer.PrivateClass").WithLocation(50, 29),
            // (57,29): error CS9032: Required member 'Outer2.InternalProtectedClass2.InternalProtected' cannot be less visible or have a setter less visible than the containing type 'Outer2.InternalProtectedClass2'.
            //         public required int InternalProtected { get; internal protected set; } // 21
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "InternalProtected").WithArguments("Outer2.InternalProtectedClass2.InternalProtected", "Outer2.InternalProtectedClass2").WithLocation(57, 29),
            // (58,29): error CS9032: Required member 'Outer2.InternalProtectedClass2.Internal' cannot be less visible or have a setter less visible than the containing type 'Outer2.InternalProtectedClass2'.
            //         public required int Internal { get; internal set; } // 22
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Internal").WithArguments("Outer2.InternalProtectedClass2.Internal", "Outer2.InternalProtectedClass2").WithLocation(58, 29),
            // (59,29): error CS9032: Required member 'Outer2.InternalProtectedClass2.Protected' cannot be less visible or have a setter less visible than the containing type 'Outer2.InternalProtectedClass2'.
            //         public required int Protected { get; protected set; } // 23
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Protected").WithArguments("Outer2.InternalProtectedClass2.Protected", "Outer2.InternalProtectedClass2").WithLocation(59, 29),
            // (60,29): error CS9032: Required member 'Outer2.InternalProtectedClass2.PrivateProtected' cannot be less visible or have a setter less visible than the containing type 'Outer2.InternalProtectedClass2'.
            //         public required int PrivateProtected { get; private protected set; } // 24
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "PrivateProtected").WithArguments("Outer2.InternalProtectedClass2.PrivateProtected", "Outer2.InternalProtectedClass2").WithLocation(60, 29),
            // (61,29): error CS9032: Required member 'Outer2.InternalProtectedClass2.Private' cannot be less visible or have a setter less visible than the containing type 'Outer2.InternalProtectedClass2'.
            //         public required int Private { get; private set; } // 25
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Private").WithArguments("Outer2.InternalProtectedClass2.Private", "Outer2.InternalProtectedClass2").WithLocation(61, 29)

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
            // (3,2): error CS9033: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(3, 2),
            // (6,6): error CS9033: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMember, "RequiredMember").WithLocation(6, 6),
            // (8,6): error CS9033: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
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
    public required ref int Prop1 => ref i;
    public required ref readonly int Prop2 => ref i;
}
");

        comp.VerifyDiagnostics(
                // (5,29): error CS9034: Required member 'C.Prop1' must be settable.
                //     public required ref int Prop1 => ref i;
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(5, 29),
                // (5,29): error CS9043: Ref returning properties cannot be required.
                //     public required ref int Prop1 => ref i;
                Diagnostic(ErrorCode.ERR_RefReturningPropertiesCannotBeRequired, "Prop1").WithLocation(5, 29),
                // (6,38): error CS9034: Required member 'C.Prop2' must be settable.
                //     public required ref readonly int Prop2 => ref i;
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop2").WithArguments("C.Prop2").WithLocation(6, 38),
                // (6,38): error CS9043: Ref returning properties cannot be required.
                //     public required ref readonly int Prop2 => ref i;
                Diagnostic(ErrorCode.ERR_RefReturningPropertiesCannotBeRequired, "Prop2").WithLocation(6, 38)
        );
    }

    [Fact]
    public void RefFields()
    {
        var source = """
            #pragma warning disable 9265
            internal ref struct R1<T>
            {
                internal required ref T F1;
                public R1() { }
            }
            public ref struct R2<U>
            {
                public required ref readonly U F2;
                public R2() { }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        var expectedRequiredMembers = new[] { "R1.F1", "R2.F2" };
        var expectedAttributeLayout = """
            [RequiredMember] R1<T>
                    [RequiredMember] ref T R1<T>.F1
                [RequiredMember] R2<U>
                    [RequiredMember] ref readonly U R2<U>.F2
            """;
        var symbolValidator = ValidateRequiredMembersInModule(expectedRequiredMembers, expectedAttributeLayout);
        var verifier = CompileAndVerify(comp, verify: Verification.Skipped, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();
    }

    [Theory]
    [InlineData("internal")]
    [InlineData("internal protected")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("private")]
    public void UnsettableMembers(string setterAccessibility)
    {
        var comp = CreateCompilationWithRequiredMembers($$"""
#pragma warning disable CS0649 // Unassigned field
public class C
{
    public required readonly int Field;
    public required int Prop1 { get; }
    public required int Prop2 { get; {{setterAccessibility}} set; }
}
""");

        comp.VerifyDiagnostics(
            // (4,34): error CS9034: Required member 'C.Field' must be settable.
            //     public required readonly int Field;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Field").WithArguments("C.Field").WithLocation(4, 34),
            // (5,25): error CS9034: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(5, 25),
            // (6,25): error CS9032: Required member 'C.Prop2' cannot be less visible or have a setter less visible than the containing type 'C'.
            //     public required int Prop2 { get; private set; }
            Diagnostic(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, "Prop2").WithArguments("C.Prop2", "C").WithLocation(6, 25)
        );
    }

    [Fact]
    public void ObsoleteMember_NoObsoleteContext()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            using System;
            #pragma warning disable CS0649 // Unassigned field
            class C
            {
                [Obsolete]
                public required int Field;
                [Obsolete]
                public required int Prop1 { get; set; }
            }
            """);

        comp.VerifyDiagnostics(
            // (6,25): warning CS9042: Required member 'C.Field' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Field;
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Field").WithArguments("C.Field").WithLocation(6, 25),
            // (8,25): warning CS9042: Required member 'C.Prop1' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Prop1 { get; set; }
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Prop1").WithArguments("C.Prop1").WithLocation(8, 25)
        );
    }

    [Fact]
    public void ObsoleteMember_NoObsoleteContext_Struct()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            using System;
            #pragma warning disable CS0649 // Unassigned field
            struct S
            {
                [Obsolete]
                public required int Field;
                [Obsolete]
                public required int Prop1 { get; set; }
            }
            """);

        comp.VerifyDiagnostics(
            // (6,25): warning CS9042: Required member 'S.Field' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Field;
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Field").WithArguments("S.Field").WithLocation(6, 25),
            // (8,25): warning CS9042: Required member 'S.Prop1' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Prop1 { get; set; }
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Prop1").WithArguments("S.Prop1").WithLocation(8, 25)
        );
    }

    [Fact]
    public void ObsoleteMember_ObsoleteContext()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            using System;
            #pragma warning disable CS0649 // Unassigned field
            [Obsolete]
            class C
            {
                [Obsolete]
                public required int Field;
                [Obsolete]
                public required int Prop1 { get; set; }
            }
            """);

        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ObsoleteMember_ObsoleteOrSetsRequiredMembersConstructors_01()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            using System;
            using System.Diagnostics.CodeAnalysis;
            #pragma warning disable CS0649 // Unassigned field
            class C
            {
                [Obsolete]
                public C() { }
                [SetsRequiredMembers]
                public C(int i) { }

                [Obsolete]
                public required int Field;
                [Obsolete]
                public required int Prop1 { get; set; }
            }

            """);

        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ObsoleteMember_ObsoleteOrSetsRequiredMembersConstructors_02()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            using System;
            using System.Diagnostics.CodeAnalysis;
            #pragma warning disable CS0649 // Unassigned field
            class C
            {
                [Obsolete]
                public C() { }
                [SetsRequiredMembers]
                public C(int i) { }

                public C(bool b) { }

                [Obsolete]
                public required int Field;
                [Obsolete]
                public required int Prop1 { get; set; }
            }

            """);

        comp.VerifyDiagnostics(
            // (14,25): warning CS9042: Required member 'C.Field' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Field;
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Field").WithArguments("C.Field").WithLocation(14, 25),
            // (16,25): warning CS9042: Required member 'C.Prop1' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
            //     public required int Prop1 { get; set; }
            Diagnostic(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, "Prop1").WithArguments("C.Prop1").WithLocation(16, 25)
        );
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
            // (1,11): error CS9035: Required member 'C.Prop' must be set in the object initializer or attribute constructor.
            // C c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop").WithLocation(1, 11),
            // (1,11): error CS9035: Required member 'C.Field' must be set in the object initializer or attribute constructor.
            // C c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field").WithLocation(1, 11)
        }
        : new[] {
            // (1,7): error CS9035: Required member 'C.Prop' must be set in the object initializer or attribute constructor.
            // C c = new();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Prop").WithLocation(1, 7),
            // (1,7): error CS9035: Required member 'C.Field' must be set in the object initializer or attribute constructor.
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
            // (1,11): error CS9035: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // C c = new C() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(1, 11),
            // (1,11): error CS9035: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // C c = new C() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field1").WithLocation(1, 11)
        }
        : new[] {
            // (1,7): error CS9035: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // C c = new() { Prop2 = 1, Field2 = 1 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "new").WithArguments("C.Prop1").WithLocation(1, 7),
            // (1,7): error CS9035: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
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
            // (2,13): error CS9035: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(2, 13),
            // (2,13): error CS9035: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Field1").WithLocation(2, 13),
            // (3,15): error CS0200: Property or indexer 'C.Prop1' cannot be assigned to -- it is read only
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Prop1").WithArguments("C.Prop1").WithLocation(3, 15),
            // (3,26): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            // c = new C() { Prop1 = 1, Field1 = 1 };
            Diagnostic(ErrorCode.ERR_AssgReadonly, "Field1").WithLocation(3, 26),
            // (7,25): error CS9034: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(7, 25),
            // (8,34): error CS9034: Required member 'C.Field1' must be settable.
            //     public required readonly int Field1;
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Field1").WithArguments("C.Field1").WithLocation(8, 34)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_NoInheritance_Unsettable_FromMetadata()
    {
        // Equivalent to:
        // public class C
        // {
        //     public required readonly int Field1;
        //     public required int Prop1 { get; }
        // }
        var il = @"
.class public auto ansi C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _Prop1
    .field public initonly int32 Field1
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_Prop1 () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 C::_Prop1
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .property instance int32 Prop1()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 C::get_Prop1()
    }
}";

        var ilRef = CompileIL(il);

        var c = @"
var c = new C();
c = new C() { Prop1 = 1, Field1 = 1 };
";
        var comp = CreateCompilation(new[] { c }, references: new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,13): error CS9035: Required member 'C.Prop1' must be set in the object initializer or attribute constructor.
            // var c = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Prop1").WithLocation(2, 13),
            // (2,13): error CS9035: Required member 'C.Field1' must be set in the object initializer or attribute constructor.
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
            // (8,25): error CS9034: Required member 'C.Prop1' must be settable.
            //     public required int Prop1 { get; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Prop1").WithArguments("C.Prop1").WithLocation(8, 25),
            // (9,34): error CS9034: Required member 'C.Field1' must be settable.
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
            // (2,24): error CS9036: Required member 'C.D1' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { D1 = { NestedProp = 1 }, D2 = { NestedProp = 2 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ NestedProp = 1 }").WithArguments("C.D1").WithLocation(2, 24),
            // (2,49): error CS9036: Required member 'C.D2' must be assigned a value, it cannot use a nested member or collection initializer.
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
            // (3,24): error CS9036: Required member 'C.L1' must be assigned a value, it cannot use a nested member or collection initializer.
            // var c = new C() { L1 = { 1, 2, 3 }, L2 = { 4, 5, 6 } };
            Diagnostic(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, "{ 1, 2, 3 }").WithArguments("C.L1").WithLocation(3, 24),
            // (3,42): error CS9036: Required member 'C.L2' must be assigned a value, it cannot use a nested member or collection initializer.
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
                // (2,9): error CS9035: Required member 'Base.Prop1' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop1").WithLocation(2, 9),
                // (2,9): error CS9035: Required member 'Base.Field1' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field1").WithLocation(2, 9),
                // (2,9): error CS9035: Required member 'Derived.Prop2' must be set in the object initializer or attribute constructor.
                // _ = new Derived();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop2").WithLocation(2, 9),
                // (2,9): error CS9035: Required member 'Derived.Field2' must be set in the object initializer or attribute constructor.
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
            // (2,9): error CS9035: Required member 'Base.Prop2' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop2").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Base.Field2' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field2").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Derived.Prop4' must be set in the object initializer or attribute constructor.
            // _ = new Derived() { Prop1 = 1, Field1 = 1, Prop3 = 3, Field3 = 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop4").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Derived.Field4' must be set in the object initializer or attribute constructor.
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
            // (2,9): error CS9035: Required member 'Base.Field1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Field1").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Base.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Base.Prop1").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Derived.Prop2' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop2").WithLocation(2, 9),
            // (2,9): error CS9035: Required member 'Derived.Field2' must be set in the object initializer or attribute constructor.
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
            // (2,9): error CS9035: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
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
            // (2,9): error CS9035: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
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
    public void EnforcedRequiredMembers_Override_DiffersByModreq_NoneSet()
    {
        // Equivalent to 
        // class Base
        // {
        //     public virtual required modopt(object) int Prop1 { get; set; }
        // }
        var @base = @"
.class public auto ansi beforefieldinit Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 '<Prop1>k__BackingField'

    .method public hidebysig specialname newslot virtual 
        instance int32 get_Prop1 () cil managed 
    {
        ldarg.0
        ldfld int32 Base::'<Prop1>k__BackingField'
        ret
    }

    .method public hidebysig specialname newslot virtual 
        instance void set_Prop1 (
            int32 'value'
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::'<Prop1>k__BackingField'
        ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 5f 43 6f 6e 73 74 72 75 63 74 6f 72 73 20
            6f 66 20 74 79 70 65 73 20 77 69 74 68 20 72 65
            71 75 69 72 65 64 20 6d 65 6d 62 65 72 73 20 61
            72 65 20 6e 6f 74 20 73 75 70 70 6f 72 74 65 64
            20 69 6e 20 74 68 69 73 20 76 65 72 73 69 6f 6e
            20 6f 66 20 79 6f 75 72 20 63 6f 6d 70 69 6c 65
            72 2e 01 00 00
        )
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0f 52 65 71 75 69 72 65 64 4d 65 6d 62 65
            72 73 00 00
        )
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .property instance int32 modopt([mscorlib]System.Object) Prop1()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_Prop1()
        .set instance void Base::set_Prop1(int32)
    }
}";

        var code = @"
_ = new Derived() { Prop1 = 1 };

public class Derived : Base
{
    public override required int Prop1 { get; set; }
}
";

        var baseRef = CompileIL(@base);

        var comp = CreateCompilation(code, new[] { baseRef }, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
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
            // (2,9): error CS9035: Required member 'Derived.Prop1' must be set in the object initializer or attribute constructor.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Derived").WithArguments("Derived.Prop1").WithLocation(2, 9)
        );

        Assert.IsType<RetargetingNamedTypeSymbol>(comp.GetTypeByMetadataName("Base"));
    }

    /// <summary>
    /// Equivalent to
    /// <code>
    /// public class Base
    /// {
    ///     public required int P { get; set; }
    /// }
    /// public class Derived : Base
    /// {
    ///     public new required int P { get; set; }
    /// }
    /// </code>
    /// </summary>
    private const string ShadowingBaseAndDerivedIL = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_01()
    {
        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
    public Derived2(int x) {}
}
class Derived3 : Derived { }";

        var ilRef = CompileIL(ShadowingBaseAndDerivedIL);

        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (8,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2(int x) {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(8, 12),
            // (10,7): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // class Derived3 : Derived { }
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived3").WithArguments("Derived").WithLocation(10, 7)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_01_HasSetsRequiredMembers()
    {
        var ilRef = CompileIL(ShadowingBaseAndDerivedIL);

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

        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_02()
    {
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // [RequiredMember]public class Derived : Base
        // {
        //     public new int P { get; set; }
        // }
        // </code>
        string il = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

        var ilRef = CompileIL(il);

        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
}
class Derived3 : Derived { }";

        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (9,7): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // class Derived3 : Derived { }
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived3").WithArguments("Derived").WithLocation(9, 7)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedInSource_03()
    {
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // public class Derived : Base
        // {
        //     public new int P { get; set; }
        // }
        // </code>
        string il = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

        var c = @"
_ = new Derived2();
_ = new Derived3();

class Derived2 : Derived
{
    public Derived2() {}
}
class Derived3 : Derived { }";

        var ilRef = CompileIL(il);

        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public Derived2() {}
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "Derived2").WithArguments("Derived").WithLocation(7, 12),
            // (9,7): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
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
            // (8,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public DerivedDerived1()
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived1").WithArguments("Derived").WithLocation(8, 12),
            // (12,8): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived2 : Derived
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived2").WithArguments("Derived").WithLocation(12, 8),
            // (15,8): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
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
            // (18,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
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
            // (8,12): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            //     public DerivedDerived1()
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived1").WithArguments("Derived").WithLocation(8, 12),
            // (12,8): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
            // record DerivedDerived2 : Derived
            Diagnostic(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, "DerivedDerived2").WithArguments("Derived").WithLocation(12, 8),
            // (15,8): error CS9038: The required members list for the base type 'Derived' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
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
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // public class Derived : Base
        // {
        //     public new required int P { get; set; }
        //     public Derived() {}
        //     [SetsRequiredMembers] public Derived(int unused) {}
        // }
        var il = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

        var ilRef = CompileIL(il);

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS9037: The required members list for 'Derived' is malformed and cannot be interpreted.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "Derived").WithArguments("Derived").WithLocation(1, 9)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedFromMetadata_02()
    {
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // [RequiredMember] public class Derived : Base
        // {
        //     public new int P { get; set; }
        //     public Derived() {}
        //     [SetsRequiredMembers] public Derived(int unused) {}
        // }
        var il = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

        var ilRef = CompileIL(il);

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS9037: The required members list for 'Derived' is malformed and cannot be interpreted.
            // _ = new Derived();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "Derived").WithArguments("Derived").WithLocation(1, 9)
        );
    }

    [Fact]
    public void EnforcedRequiredMembers_ShadowedFromMetadata_03()
    {
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // public class Derived : Base
        // {
        //     public new int P { get; set; }
        //     public Derived() {}
        //     [SetsRequiredMembers] public Derived(int unused) {}
        // }
        var il = @"
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}";

        var ilRef = CompileIL(il);

        var c = """
            _ = new Derived();
            _ = new Derived(1);
            """;
        var comp = CreateCompilation(c, new[] { ilRef }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS9037: The required members list for 'Derived' is malformed and cannot be interpreted.
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
            // (2,25): error CS9035: Required member 'CustomHandler.Field' must be set in the object initializer or attribute constructor.
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
            // (5,9): error CS9035: Required member 'C.P' must be set in the object initializer or attribute constructor.
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
            // (5,9): error CS9035: Required member 'C.P' must be set in the object initializer or attribute constructor.
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
            // (4,2): error CS9035: Required member 'AttrAttribute.P' must be set in the object initializer or attribute constructor.
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
            // (4,2): error CS9035: Required member 'RequiredMemberAttribute.P' must be set in the object initializer or attribute constructor.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "RequiredMember").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute.P").WithLocation(4, 2),
            // (4,2): error CS9033: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
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
            // (6,6): error CS9035: Required member 'RequiredMemberAttribute.P' must be set in the object initializer or attribute constructor.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "RequiredMember").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute.P").WithLocation(6, 6),
            // (6,6): error CS9033: Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
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
            // (8,19): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public string P2 { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P2").WithArguments("property", "P2").WithLocation(8, 19),
            // (9,19): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
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
            // (11,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(11, 12),
            // (11,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
            //     public C()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "F2").WithLocation(11, 12),
            // (15,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C(int _) {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "P2").WithLocation(15, 12),
            // (15,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
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
            // (11,12): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public S() {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "P2").WithLocation(11, 12),
            // (11,12): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
            //     public S() {}
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "F2").WithLocation(11, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_01()
    {
        var code = @"
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    public required string Property { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    public C() { }
}";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_02()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; set => _field = value; }

    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_03()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field1;
    private string _field2;
    [MemberNotNull(nameof(_field1))]
    public required string Property
    { 
        get => _field1 ??= "";
        [MemberNotNull(nameof(_field2))]
        set
        {
            _field1 = value; 
            _field2 = value; 
        }
    }

    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_04()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    public required string Property1 { get => Property2; [MemberNotNull(nameof(Property2))] set => Property2 = value; }
    public string Property2 { get; set; }

    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_05()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    [MemberNotNull(nameof(Property2))]
    public required string Property1 { get => Property2 ??= ""; set => Property2 = value; }
    public string Property2 { get; set; }

    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_06()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; set => _field = value; }

    [SetsRequiredMembers]
    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (9,12): warning CS8618: Non-nullable property 'Property' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Property").WithLocation(9, 12),
            // (9,12): warning CS8618: Non-nullable field '_field' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_field").WithLocation(9, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_07()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    public required string Property { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    [SetsRequiredMembers]
    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (9,12): warning CS8618: Non-nullable property 'Property' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Property").WithLocation(9, 12),
            // (9,12): warning CS8618: Non-nullable field '_field' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("field", "_field").WithLocation(9, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_08()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; }

    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (6,60): error CS9034: Required member 'C.Property' must be settable.
            //     [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; }
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSettable, "Property").WithArguments("C.Property").WithLocation(6, 60)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_NoChainedConstructor_09()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class C
{
    public required string Prop1 { get => Prop2; [MemberNotNull(nameof(Prop2))] set => Prop2 = value; }
    public string Prop2 { get => Prop3; [MemberNotNull(nameof(Prop3))] set => Prop3 = value; }
    public string Prop3 { get; set; }
    public C() { }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (8,12): warning CS8618: Non-nullable property 'Prop3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Prop3").WithLocation(8, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor_01()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    public required string Property { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    public C() { }
    public C(bool unused) : this()
    { 
        _field.ToString();
        Property.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (11,9): warning CS8602: Dereference of a possibly null reference.
            //         _field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(11, 9),
            // (12,9): warning CS8602: Dereference of a possibly null reference.
            //         Property.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property").WithLocation(12, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor_02()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    private string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; set => _field = value; }

    public C() { }
    public C(bool unused) : this()
    { 
        _field.ToString();
        Property.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (11,9): warning CS8602: Dereference of a possibly null reference.
            //         _field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(11, 9),
            // (12,9): warning CS8602: Dereference of a possibly null reference.
            //         Property.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property").WithLocation(12, 9)
        );
    }

    [Theory, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    [InlineData("public string Property2 { get; set; }")]
    [InlineData("public string Property2 { get => field; set => field = value; }")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor_03(string property2Definition)
    {
        var code = $$"""
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    public required string Property1 { get => Property2; [MemberNotNull(nameof(Property2))] set => Property2 = value; }
    {{property2Definition}}

    public C() { }
    public C(bool unused) : this()
    { 
        Property1.ToString();
        Property2.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (11,9): warning CS8602: Dereference of a possibly null reference.
            //         Property1.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property1").WithLocation(11, 9),
            // (12,9): warning CS8602: Dereference of a possibly null reference.
            //         Property2.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property2").WithLocation(12, 9)
        );
    }

    [Theory, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    [InlineData("public string Property2 { get; set; }")]
    [InlineData("public string Property2 { get => field; set => field = value; }")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor_04(string property2Definition)
    {
        var code = $$"""
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    public required string Property1 { get => Property2; [MemberNotNull(nameof(Property2))] set => Property2 = value; }
    {{property2Definition}}

    public C() { }
    [SetsRequiredMembers]
    public C(bool unused) : this()
    { 
        Property1.ToString();
        Property2.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (12,9): warning CS8602: Dereference of a possibly null reference.
            //         Property1.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property1").WithLocation(12, 9),
            // (13,9): warning CS8602: Dereference of a possibly null reference.
            //         Property2.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property2").WithLocation(13, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedConstructor_05()
    {
        var code = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class C
{
    public required string Property1 { get => Property2; [MemberNotNull(nameof(Property2))] set => Property2 = value; }
    public string Property2 { get; set; }

    [SetsRequiredMembers]
    public C() { }
    [SetsRequiredMembers]
    public C(bool unused) : this()
    { 
        Property1.ToString();
        Property2.ToString();
    }
}
""";

        var comp = CreateCompilationWithRequiredMembers(new[] { code, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(
            // (9,12): warning CS8618: Non-nullable property 'Property2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Property2").WithLocation(9, 12),
            // (9,12): warning CS8618: Non-nullable property 'Property1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Property1").WithLocation(9, 12)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedBaseConstructor_01()
    {
        var @base = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class Base 
{
    protected string _field;
    public required string Property { get => _field; [MemberNotNull(nameof(_field))] set => _field = value; }

    public Base() { }
}
""";

        var derived = """
#nullable enable
class Derived : Base
{
    public Derived()
    { 
        _field.ToString();
        Property.ToString();
    }
}
""";

        var expectedDiagnostics = new[] {
            // (6,9): warning CS8602: Dereference of a possibly null reference.
            //         _field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(6, 9),
            // (7,9): warning CS8602: Dereference of a possibly null reference.
            //         Property.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property").WithLocation(7, 9)
        };

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(new[] { @base, MemberNotNullAttributeDefinition });
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedBaseConstructor_02()
    {
        var @base = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class Base 
{
    protected string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; set => _field = value; }

    public Base() { }
}
""";

        var derived = """
#nullable enable
class Derived : Base
{
    public Derived()
    { 
        _field.ToString();
        Property.ToString();
    }
}
""";

        var expectedDiagnostics = new[] {
            // (6,9): warning CS8602: Dereference of a possibly null reference.
            //         _field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(6, 9),
            // (7,9): warning CS8602: Dereference of a possibly null reference.
            //         Property.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property").WithLocation(7, 9)
        };

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(new[] { @base, MemberNotNullAttributeDefinition });
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedBaseConstructor_03()
    {
        var @base = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class Base 
{
    private string _field;
    public required string Property { get => _field ??= ""; [MemberNotNull(nameof(_field))] set => _field = value; }

    public Base() { }
}
""";

        var derived = """
#nullable enable
class Derived : Base
{
    private string _field;
    private Derived() { _field = ""; }
    public Derived(bool unused) : this()
    { 
        // No warning, as the _field in the MemberNotNull isn't visible in this type, and the one that is visible was set by the chained ctor
        _field.ToString();
        Property.ToString();
    }
}
""";

        var expectedDiagnostics = new[] {
            // (10,9): warning CS8602: Dereference of a possibly null reference.
            //         Property.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Property").WithLocation(10, 9)
        };

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(new[] { @base, MemberNotNullAttributeDefinition });
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedBaseConstructor_04()
    {
        var @base = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class Base 
{
    private string _field;
    public required string Property { get => _field ??= ""; [MemberNotNull(nameof(_field))] set => _field = value; }

    public Base() { }
}
""";

        var derived = """
#nullable enable
class Derived : Base
{
    private string _field;
    private Derived()
    {
    }
}
""";

        var expectedDiagnostics = new[] {
            // (4,20): warning CS0169: The field 'Derived._field' is never used
            //     private string _field;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "_field").WithArguments("Derived._field").WithLocation(4, 20),
            // (5,13): warning CS8618: Non-nullable field '_field' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
            //     private Derived()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("field", "_field").WithLocation(5, 13)
        };

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(new[] { @base, MemberNotNullAttributeDefinition });
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(6754, "https://github.com/dotnet/csharplang/issues/6754")]
    public void RequiredMemberSuppressesNullabilityWarnings_MemberNotNull_ChainedBaseConstructor_05()
    {
        var @base = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
public class Base 
{
    protected string _field;
    [MemberNotNull(nameof(_field))] public required string Property { get => _field ??= ""; set => _field = value; }

    public Base() { }
}
""";

        var derived = """
using System.Diagnostics.CodeAnalysis;
#nullable enable
class Derived : Base
{
    [SetsRequiredMembers]
    public Derived()
    {
        _field.ToString();
    }
}
""";

        var expectedDiagnostics = new[] {
            // (6,12): warning CS8618: Non-nullable property 'Property' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Property").WithLocation(6, 12),
            // (8,9): warning CS8602: Dereference of a possibly null reference.
            //         _field.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(8, 9)
        };

        var comp = CreateCompilationWithRequiredMembers(new[] { @base, derived, MemberNotNullAttributeDefinition });
        comp.VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilationWithRequiredMembers(new[] { @base, MemberNotNullAttributeDefinition });
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(expectedDiagnostics);
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
                protected Base() {} // 1
            }
            
            public class Derived : Base
            {
                public required string Prop3 { get; set; }
                public string Prop4 { get; set; }
            
                public Derived() : base() // 2
                {
                    Prop1.ToString();
                    Prop2.ToString();
                    Prop3.ToString(); // 3
                    Prop4.ToString(); // 4
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (9,15): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     protected Base() {} // 1
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Base").WithArguments("property", "Prop1").WithLocation(9, 15),
            // (17,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : base() // 2
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "base").WithLocation(17, 24),
            // (21,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop3.ToString(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop3").WithLocation(21, 9),
            // (22,9): warning CS8602: Dereference of a possibly null reference.
            //         Prop4.ToString(); // 4
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Prop4").WithLocation(22, 9)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(61718, "https://github.com/dotnet/roslyn/issues/61718")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_09A()
    {
        var @base = $$"""
            #nullable enable
            public class Base
            {
                public required string Prop1 { get; set; }
                public string Prop2 { get; set; } = null!;

                protected Base() {}
            }
            """;

        var derived = $$"""
            using System.Diagnostics.CodeAnalysis;
            #nullable enable

            public class Derived : Base
            {
                public required string Prop3 { get; set; }
                public string Prop4 { get; set; }

                [SetsRequiredMembers]
                public Derived(int unused) : base()
                {
                    Prop4 = null!;
                }

                [SetsRequiredMembers]
                public Derived(bool unused)
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

        var comp = CreateCompilationWithRequiredMembers(new[] { derived, @base });

        comp.VerifyDiagnostics(
            // (10,12): warning CS8618: Non-nullable property 'Prop3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop3").WithLocation(10, 12),
            // (10,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(10, 12),
            // (16,12): warning CS8618: Non-nullable property 'Prop3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop3").WithLocation(16, 12),
            // (16,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(16, 12),
            // (21,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : this(0)
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(21, 24)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (10,12): warning CS8618: Non-nullable property 'Prop3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop3").WithLocation(10, 12),
            // (10,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(10, 12),
            // (16,12): warning CS8618: Non-nullable property 'Prop3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop3").WithLocation(16, 12),
            // (16,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(16, 12),
            // (21,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : this(0)
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(21, 24)
        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(61718, "https://github.com/dotnet/roslyn/issues/61718")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_09B()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            public class Base
            {
                public required string Field1;
                public string Field2 = null!;

                protected Base() {}
            }

            public class Derived : Base
            {
                public required string Field3;
                public string Field4;

                [SetsRequiredMembers]
                public Derived(int unused) : base()
                {
                    Field4 = null!;
                }

                [SetsRequiredMembers]
                public Derived(bool unused)
                {
                    Field4 = null!;
                }

                public Derived() : this(0)
                {
                    Field1.ToString();
                    Field2.ToString();
                    Field3.ToString();
                    Field4.ToString();
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
                // (17,12): warning CS8618: Non-nullable field 'Field3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     public Derived(int unused) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("field", "Field3").WithLocation(17, 12),
                // (17,12): warning CS8618: Non-nullable field 'Field1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     public Derived(int unused) : base()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("field", "Field1").WithLocation(17, 12),
                // (23,12): warning CS8618: Non-nullable field 'Field3' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     public Derived(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("field", "Field3").WithLocation(23, 12),
                // (23,12): warning CS8618: Non-nullable field 'Field1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     public Derived(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("field", "Field1").WithLocation(23, 12),
                // (28,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
                //     public Derived() : this(0)
                Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(28, 24)

        );
    }

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    [WorkItem(61718, "https://github.com/dotnet/roslyn/issues/61718")]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_09C()
    {
        var @base = $$"""
            #nullable enable
            public class Base
            {
                private string _field1 = null!;
                public required string Prop1 { get => _field1; set => _field1 = value; }

                protected Base() {}
            }
            """;

        var derived = $$"""
            using System.Diagnostics.CodeAnalysis;
            #nullable enable

            public class Derived : Base
            {
                private string _field2 = null!;
                public required string Prop2 { get => _field2; set => _field2 = value; }

                [SetsRequiredMembers]
                public Derived(int unused) : base()
                {
                }

                [SetsRequiredMembers]
                public Derived(bool unused)
                {
                }

                public Derived() : this(0)
                {
                    Prop1.ToString();
                    Prop2.ToString();
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(new[] { derived, @base });

        comp.VerifyDiagnostics(
            // (10,12): warning CS8618: Non-nullable property 'Prop2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop2").WithLocation(10, 12),
            // (10,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(10, 12),
            // (15,12): warning CS8618: Non-nullable property 'Prop2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop2").WithLocation(15, 12),
            // (15,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(15, 12),
            // (19,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : this(0)
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(19, 24)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        comp = CreateCompilation(derived, new[] { baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (10,12): warning CS8618: Non-nullable property 'Prop2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop2").WithLocation(10, 12),
            // (10,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(int unused) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(10, 12),
            // (15,12): warning CS8618: Non-nullable property 'Prop2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop2").WithLocation(15, 12),
            // (15,12): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(bool unused)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Prop1").WithLocation(15, 12),
            // (19,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : this(0)
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "this").WithLocation(19, 24)
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
            // (2,42): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            // public record Derived(bool unused) : Base(unused);
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "(unused)").WithLocation(2, 42)
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

    [Fact, CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public void RequiredMemberSuppressesNullabilityWarnings_ChainedConstructor_12()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            public record Base
            {
                public required string Prop1 { get; set; }
                public string Prop2 { get; set; } = null!;

                [SetsRequiredMembers]
                protected Base() {} // 1
            }
            
            public record Derived() : Base;
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (9,15): warning CS8618: Non-nullable property 'Prop1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     protected Base() {} // 1
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Base").WithArguments("property", "Prop1").WithLocation(9, 15),
            // (12,15): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            // public record Derived() : Base;
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "Derived").WithLocation(12, 15)
        );
    }

    [Fact]
    public void SetsRequiredMembersRequiredForChaining_ImplicitConstructor()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;

            class Base
            {
                [SetsRequiredMembers]
                public Base() { }
            }

            class Derived : Base { }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (9,7): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            // class Derived : Base { }
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "Derived").WithLocation(9, 7)
        );
    }

    [Fact]
    public void SetsRequiredMembersRequiredForChaining_ImplicitBaseCall()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;

            class Base
            {
                [SetsRequiredMembers]
                public Base() { }
            }

            class Derived : Base
            {
                public Derived() { }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (11,12): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() { }
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "Derived").WithLocation(11, 12)
        );
    }

    [Fact]
    public void SetsRequiredMembersRequiredForChaining_Explicit()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;

            class Base
            {
                [SetsRequiredMembers]
                public Base() { }
            }

            class Derived : Base
            {
                public Derived() : base() { } 
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (11,24): error CS9039: This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
            //     public Derived() : base() { } 
            Diagnostic(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, "base").WithLocation(11, 24)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_01()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_02()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                }

                public required virtual string Str { get; set; } = "";
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (17,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str) : base(str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(17, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_03()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (7,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Base(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Base").WithArguments("property", "Str").WithLocation(7, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_04()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public Base(string str)
                {
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (16,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str) : base(str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(16, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_05()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public Base()
                {
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base()
                {
                    this.Str = str;
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_06()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public Base()
                {
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                public Derived() : base()
                {
                }

                public override required string Str { get; set; }
            }

            public class DerivedDerived : Derived
            {
                [SetsRequiredMembers]
                public DerivedDerived(string str) : base()
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (25,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public DerivedDerived(string str) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "DerivedDerived").WithArguments("property", "Str").WithLocation(25, 12),
            // (25,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public DerivedDerived(string str) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "DerivedDerived").WithArguments("property", "Str").WithLocation(25, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_07()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public Base()
                {
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                public Derived() : base()
                {
                }

                public override required string Str { get; set; }
            }

            public class DerivedDerived : Derived
            {
                [SetsRequiredMembers]
                public DerivedDerived(string str) : base()
                {
                }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (25,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public DerivedDerived(string str) : base()
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "DerivedDerived").WithArguments("property", "Str").WithLocation(25, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_08()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string? Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, NotNullAttributeDefinition, DisallowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (18,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str) : base(str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(18, 12),
            // (22,48): warning CS8765: Nullability of type of parameter 'value' doesn't match overridden member (possibly because of nullability attributes).
            //     public override required string Str { get; set; }
            Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "set").WithArguments("value").WithLocation(22, 48)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_09()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string? Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, MaybeNullAttributeDefinition, AllowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (22,44): warning CS8764: Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
            //     public override required string Str { get; set; }
            Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride, "get").WithLocation(22, 44)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_10()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string? Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                [NotNull, DisallowNull]
                public override required string? Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, NotNullAttributeDefinition, DisallowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (18,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str) : base(str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(18, 12),
            // (23,49): warning CS8765: Nullability of type of parameter 'value' doesn't match overridden member (possibly because of nullability attributes).
            //     public override required string? Str { get; set; }
            Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "set").WithArguments("value").WithLocation(23, 49)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_11()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                [MaybeNull, AllowNull]
                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, MaybeNullAttributeDefinition, AllowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (23,43): warning CS8764: Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
            //     public override required string Str { get; set; }
            Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride, "get").WithLocation(23, 43)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_12()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public required abstract string Str { get; set; }
            }

            public abstract class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str)
                {
                    Str = str;
                }

                public abstract override required string Str { get; set; }
            }

            public class DerivedDerived : Derived
            {
                [SetsRequiredMembers]
                public DerivedDerived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, MaybeNullAttributeDefinition, AllowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_13()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public required virtual string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (12,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(12, 12),
            // (12,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(12, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_13A()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public required virtual string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str)
                {
                    Str = str;
                    base.Str = str;
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_14()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public virtual string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (6,27): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public virtual string Str { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Str").WithArguments("property", "Str").WithLocation(6, 27),
            // (12,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(12, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_15()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                public abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (12,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(12, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_16()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                }

                public virtual string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (7,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Base(string str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Base").WithArguments("property", "Str").WithLocation(7, 12),
            // (17,12): warning CS8618: Non-nullable property 'Str' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public Derived(string str) : base(str)
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Derived").WithArguments("property", "Str").WithLocation(17, 12)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_17()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                }

                public abstract string Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74423")]
    public void SetsRequiredMembersHonoredForPropertyOverride_18()
    {
        var code = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            public abstract class Base
            {
                [SetsRequiredMembers]
                public Base(string str)
                {
                    this.Str = str;
                }
    
                public required abstract string? Str { get; set; }
            }

            public class Derived : Base
            {
                [SetsRequiredMembers]
                public Derived(string str) : base(str)
                {
                }

                [AllowNull]
                public override required string Str { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers([code, AllowNullAttributeDefinition]);
        comp.VerifyDiagnostics(
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
            // (1,1): error CS9040: 'C' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'C' has required members.
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
            // (1,1): error CS9040: 'Derived' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'Derived' has required members.
            // M<Derived>();
            Diagnostic(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, "M<Derived>").WithArguments("M<T>()", "T", "Derived").WithLocation(1, 1)
        );

        var baseComp = CreateCompilationWithRequiredMembers(@base);
        CompileAndVerify(baseComp).VerifyDiagnostics();

        comp = CreateCompilation(code, references: new[] { useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error CS9040: 'Derived' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'Derived' has required members.
            // M<Derived>();
            Diagnostic(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, "M<Derived>").WithArguments("M<T>()", "T", "Derived").WithLocation(1, 1)
        );
    }

    [Fact]
    public void ForbidRequiredAsNew_MalformedMembersList()
    {
        // Equivalent to
        // public class Base
        // {
        //     public required int P { get; set; }
        // }
        // public class Derived : Base
        // {
        //     public new required int P { get; set; }
        //     public Derived() {}
        // }
        var badIl = """
            .class public auto ansi Base
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .field private int32 _P
            
                .method public specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                }
            
                .method public specialname 
                    instance int32 get_P () cil managed 
                {
                    IL_0000: ldarg.0
                    IL_0001: ldfld int32 Base::_P
                    IL_0006: br.s IL_0008
            
                    IL_0008: ret
                }
            
                .method public specialname 
                    instance void set_P (
                        int32 AutoPropertyValue
                    ) cil managed 
                {
                    ldarg.0
                    ldarg.1
                    stfld int32 Base::_P
                    ret
                }
            
                .property instance int32 P()
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 Base::get_P()
                    .set instance void Base::set_P(int32)
                }
            }
            
            .class public auto ansi Derived
                extends Base
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .field private int32 _P
            
                .method public specialname 
                    instance int32 get_P () cil managed 
                {
                    IL_0000: ldarg.0
                    IL_0001: ldfld int32 Derived::_P
                    IL_0006: br.s IL_0008
            
                    IL_0008: ret
                }
            
                .method public specialname 
                    instance void set_P (
                        int32 AutoPropertyValue
                    ) cil managed 
                {
                    ldarg.0
                    ldarg.1
                    stfld int32 Derived::_P
                    ret
                }
            
                .method public specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    nop
                    ldarg.0
                    call instance void Base::.ctor()
                    ret
                }
            
                .property instance int32 P()
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 Derived::get_P()
                    .set instance void Derived::set_P(int32)
                }
            }
            """;

        var code = """
            M<Derived>();

            void M<T>() where T : new()
            {
            }
            """;

        var comp = CreateCompilationWithIL(code, badIl, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): error CS9040: 'Derived' cannot satisfy the 'new()' constraint on parameter 'T' in the generic type or or method 'M<T>()' because 'Derived' has required members.
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

    [Theory]
    [CombinatorialData]
    public void PublicAPITests(bool isRequired)
    {
        var requiredText = isRequired ? "required" : "";
        var comp = CreateCompilationWithRequiredMembers($$"""
            public class C
            {
                public {{requiredText}} int Field;
                public {{requiredText}} int Property { get; set; }
            }
            """);

        var c = comp.GlobalNamespace.GetTypeMember("C");

        var field = c.GetField("Field").GetPublicSymbol();
        Assert.Equal(isRequired, field.IsRequired);

        var property = c.GetProperty("Property").GetPublicSymbol();
        Assert.Equal(isRequired, property.IsRequired);
    }

    [Fact, WorkItem(61822, "https://github.com/dotnet/roslyn/issues/61822")]
    public void RequiredMembersNotAllowedInSubmission()
    {
        var reference = CreateCompilationWithRequiredMembers("").ToMetadataReference();
        var submission = CreateSubmission("""
            public required int Field;
            public required int Prop { get; set; }
            """, new[] { reference }, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.Preview));

        submission.VerifyDiagnostics(
            // (1,21): error CS9045: Required members are not allowed on the top level of a script or submission.
            // public required int Field;
            Diagnostic(ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers, "Field").WithLocation(1, 21),
            // (2,21): error CS9045: Required members are not allowed on the top level of a script or submission.
            // public required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers, "Prop").WithLocation(2, 21)
        );
    }

    [Fact, WorkItem(61822, "https://github.com/dotnet/roslyn/issues/61822")]
    public void RequiredMembersNotAllowedInScript()
    {
        var reference = CreateCompilationWithRequiredMembers("").ToMetadataReference();
        var script = CreateCompilation("""
            public required int Field;
            public required int Prop { get; set; }
            """, new[] { reference }, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.Preview));

        script.VerifyDiagnostics(
            // (1,21): error CS9045: Required members are not allowed on the top level of a script or submission.
            // public required int Field;
            Diagnostic(ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers, "Field").WithLocation(1, 21),
            // (2,21): error CS9045: Required members are not allowed on the top level of a script or submission.
            // public required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers, "Prop").WithLocation(2, 21)
        );
    }

    [Fact, WorkItem(62062, "https://github.com/dotnet/roslyn/issues/62062")]
    public void DuplicateRequiredMembers_Fields()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            public class C
            {
                public required int Test;
                public required int Test;
                public required int Test;
                public required int Test;
                public required int Test;
                public required int Test;
            
                public void M()
                {
                    C c = new C { T = 42 };
                }
            }
            """);

        comp.VerifyDiagnostics(
            // (4,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(4, 25),
            // (5,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(5, 25),
            // (6,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(6, 25),
            // (7,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(7, 25),
            // (8,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(8, 25),
            // (12,19): error CS9035: Required member 'C.Test' must be set in the object initializer or attribute constructor.
            //         C c = new C { T = 42 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Test").WithLocation(12, 19),
            // (12,23): error CS0117: 'C' does not contain a definition for 'T'
            //         C c = new C { T = 42 };
            Diagnostic(ErrorCode.ERR_NoSuchMember, "T").WithArguments("C", "T").WithLocation(12, 23)
        );
    }

    [Fact, WorkItem(62062, "https://github.com/dotnet/roslyn/issues/62062")]
    public void DuplicateRequiredMembers_Properties()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            public class C
            {
                public required int Test { get; set; }
                public required int Test { get; set; }
                public required int Test { get; set; }
                public required int Test { get; set; }
                public required int Test { get; set; }
                public required int Test { get; set; }
            
                public void M()
                {
                    C c = new C { T = 42 };
                }
            }
            """);

        comp.VerifyDiagnostics(
            // (4,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(4, 25),
            // (5,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(5, 25),
            // (6,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(6, 25),
            // (7,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(7, 25),
            // (8,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(8, 25),
            // (12,19): error CS9035: Required member 'C.Test' must be set in the object initializer or attribute constructor.
            //         C c = new C { T = 42 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.Test").WithLocation(12, 19),
            // (12,23): error CS0117: 'C' does not contain a definition for 'T'
            //         C c = new C { T = 42 };
            Diagnostic(ErrorCode.ERR_NoSuchMember, "T").WithArguments("C", "T").WithLocation(12, 23)
        );
    }

    [Fact, WorkItem(62062, "https://github.com/dotnet/roslyn/issues/62062")]
    public void DuplicateRequiredMembers_Mixed01()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            class C
            {
                public required int Test { get; set; }
                public required int Test;
            }
            """);

        comp.VerifyDiagnostics(
            // (4,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(4, 25),
            // (4,25): warning CS0649: Field 'C.Test' is never assigned to, and will always have its default value 0
            //     public required int Test;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Test").WithArguments("C.Test", "0").WithLocation(4, 25)
        );
    }

    [Fact, WorkItem(62062, "https://github.com/dotnet/roslyn/issues/62062")]
    public void DuplicateRequiredMembers_Mixed02()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            class C
            {
                public required int Test;
                public required int Test { get; set; }
            }
            """);

        comp.VerifyDiagnostics(
            // (3,25): warning CS0649: Field 'C.Test' is never assigned to, and will always have its default value 0
            //     public required int Test;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Test").WithArguments("C.Test", "0").WithLocation(3, 25),
            // (4,25): error CS0102: The type 'C' already contains a definition for 'Test'
            //     public required int Test { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Test").WithArguments("C", "Test").WithLocation(4, 25)
        );
    }

    [Theory, CombinatorialData]
    public void FirstAccessOfIsRequiredDoesNotMatter(bool accessAttributesFirst)
    {
        // Accessing attributes will populate IsRequired if it's not already populated, so we want to test both codepaths explicitly.
        var comp = CreateCompilationWithRequiredMembers("""
            public class C
            {
                public required int Field1;
                public required int Property1 { get; set; }
            }

            public class D
            {
                public int Field2;
                public int Property2 { get; set; }
            }
            """);

        CompileAndVerify(comp, symbolValidator: module =>
        {
            var c = module.ContainingAssembly.GetTypeByMetadataName("C");
            AssertEx.NotNull(c);
            FieldSymbol field1 = c.GetMember<FieldSymbol>("Field1");
            PropertySymbol property1 = c.GetMember<PropertySymbol>("Property1");
            var d = module.ContainingAssembly.GetTypeByMetadataName("D");
            AssertEx.NotNull(d);
            FieldSymbol field2 = d.GetMember<FieldSymbol>("Field2");
            PropertySymbol property2 = d.GetMember<PropertySymbol>("Property2");

            if (accessAttributesFirst)
            {
                assertAttributesEmpty();
                assertIsRequired();
            }
            else
            {
                assertIsRequired();
                assertAttributesEmpty();
            }

            void assertIsRequired()
            {
                Assert.True(c.HasDeclaredRequiredMembers);
                Assert.True(field1.IsRequired);
                Assert.True(property1.IsRequired);
                Assert.False(d.HasDeclaredRequiredMembers);
                Assert.False(field2.IsRequired);
                Assert.False(property2.IsRequired);
            }

            void assertAttributesEmpty()
            {
                Assert.Empty(c.GetAttributes());
                Assert.Empty(field1.GetAttributes());
                Assert.Empty(property1.GetAttributes());
                Assert.Empty(d.GetAttributes());
                Assert.Empty(field2.GetAttributes());
                Assert.Empty(property2.GetAttributes());
            }
        });
    }

    [Fact]
    public void GenericSubstitution_NoneSet()
    {
        var code = """
            _ = new C<int>();
            
            public class C<T>
            {
                public required T Prop { get; set; }
                public required T Field;
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (1,9): error CS9035: Required member 'C<int>.Prop' must be set in the object initializer or attribute constructor.
            // _ = new C<int>();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C<int>").WithArguments("C<int>.Prop").WithLocation(1, 9),
            // (1,9): error CS9035: Required member 'C<int>.Field' must be set in the object initializer or attribute constructor.
            // _ = new C<int>();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C<int>").WithArguments("C<int>.Field").WithLocation(1, 9)
        );
    }

    [Fact]
    public void GenericSubstitution_AllSet()
    {
        var code = """
            _ = new C<int>() { Prop = 1, Field = 2 };
            
            public class C<T>
            {
                public required T Prop { get; set; }
                public required T Field;
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void GenericSubstitution_Unbound()
    {
        var code = """
            _ = new C<>();
            
            public class C<T>
            {
                public required T Prop { get; set; }
                public required T Field;
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (1,9): error CS7003: Unexpected use of an unbound generic name
            // _ = new C<>();
            Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<>").WithLocation(1, 9),
            // (1,9): error CS9035: Required member 'C<T>.Prop' must be set in the object initializer or attribute constructor.
            // _ = new C<>();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C<>").WithArguments("C<T>.Prop").WithLocation(1, 9),
            // (1,9): error CS9035: Required member 'C<T>.Field' must be set in the object initializer or attribute constructor.
            // _ = new C<>();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C<>").WithArguments("C<T>.Field").WithLocation(1, 9)
        );

        var c = comp.GetTypeByMetadataName("C`1");
        var u_c = c!.ConstructUnboundGenericType();
        Assert.False(u_c.HasDeclaredRequiredMembers);
        AssertEx.Empty(u_c.AllRequiredMembers);
    }

    [Fact]
    public void GenericSubstitution_Inheritance_NoneSet()
    {
        var code = """
            _ = new D();
            
            public class C<T>
            {
                public required T Prop { get; set; }
                public required T Field;
            }

            class D : C<int> { }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (1,9): error CS9035: Required member 'C<int>.Prop' must be set in the object initializer or attribute constructor.
            // _ = new D();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "D").WithArguments("C<int>.Prop").WithLocation(1, 9),
            // (1,9): error CS9035: Required member 'C<int>.Field' must be set in the object initializer or attribute constructor.
            // _ = new D();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "D").WithArguments("C<int>.Field").WithLocation(1, 9)
        );
    }

    [Fact]
    public void GenericSubstitution_Inheritance_AllSet()
    {
        var code = """
            _ = new D() { Prop = 1, Field = 2 };

            public class C<T>
            {
                public required T Prop { get; set; }
                public required T Field;
            }

            class D : C<int> { }
           """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void GenericSubstitution_InheritanceAndOverride_NoneSet()
    {
        var code = """
            _ = new D();
            
            public class C<T>
            {
                public virtual required T Prop { get; set; }
            }

            class D : C<int>
            {
                public override required int Prop { get; set; }
            }
            """;

        var comp = CreateCompilationWithRequiredMembers(code);
        comp.VerifyDiagnostics(
            // (1,9): error CS9035: Required member 'D.Prop' must be set in the object initializer or attribute constructor.
            // _ = new D();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "D").WithArguments("D.Prop").WithLocation(1, 9)
        );
    }

    [Fact]
    public void ProtectedParameterlessConstructorInStruct()
    {

        // Equivalent to
        // public struct S
        // {
        //     protected S() {}
        //     public required int Prop { get; set; }
        // }
        var il = """
            .class public sequential ansi sealed beforefieldinit S
                extends[mscorlib] System.ValueType
                {
                .custom instance void [mscorlib] System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .field private int32 f
            
                .method family hidebysig specialname rtspecialname
                    instance void .ctor () cil managed
                {
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
                        01 00 5f 43 6f 6e 73 74 72 75 63 74 6f 72 73 20
                        6f 66 20 74 79 70 65 73 20 77 69 74 68 20 72 65
                        71 75 69 72 65 64 20 6d 65 6d 62 65 72 73 20 61
                        72 65 20 6e 6f 74 20 73 75 70 70 6f 72 74 65 64
                        20 69 6e 20 74 68 69 73 20 76 65 72 73 69 6f 6e
                        20 6f 66 20 79 6f 75 72 20 63 6f 6d 70 69 6c 65
                        72 2e 01 00 00
                    )
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0f 52 65 71 75 69 72 65 64 4d 65 6d 62 65
                        72 73 00 00
                    )
                    ret
                }
            
                .method public hidebysig specialname
                    instance int32 get_Prop() cil managed
                {
                    ldarg.0
                    ldfld int32 S::f
                    ret
                }
            
                .method public hidebysig specialname
                    instance void set_Prop(
                        int32 'value'
                    ) cil managed
                {
                    ldarg.0
                    ldarg.1
                    stfld int32 S::f
                    ret
                }
            
                .property instance int32 Prop()
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 S::get_Prop()
                    .set instance void S::set_Prop(int32)
                }
            }
            """;

        var comp = CreateCompilationWithIL("_ = new S();", il, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS0122: 'S.S()' is inaccessible due to its protection level
            // _ = new S();
            Diagnostic(ErrorCode.ERR_BadAccess, "S").WithArguments("S.S()").WithLocation(1, 9)
        );
    }

    private static string TupleWithRequiredMemberDefinition(bool setsRequiredMembers) => $$"""
        namespace System
        {
            public struct ValueTuple<T1, T2>
            {
                public required T1 Item1;
                public required T2 Item2;
                public required int AnotherField;
                public required int Property { get; set; }
        
                {{(setsRequiredMembers ? "[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : "")}}
                public ValueTuple(T1 item1, T2 item2)
                {
                    this.Item1 = item1;
                    this.Item2 = item2;
                }
        
                public static bool operator ==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
                    => throw null;
                public static bool operator !=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
                    => throw null;
        
                public override bool Equals(object o)
                    => throw null;
                public override int GetHashCode()
                    => throw null;
            }
        
            public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct
            {
                public T1 Item1;
                public T2 Item2;
                public T3 Item3;
                public T4 Item4;
                public T5 Item5;
                public T6 Item6;
                public T7 Item7;
                public required TRest Rest;
        
                {{(setsRequiredMembers ? "[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : "")}}
                public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
                {
                    this.Item1 = item1;
                    this.Item2 = item2;
                    this.Item3 = item3;
                    this.Item4 = item4;
                    this.Item5 = item5;
                    this.Item6 = item6;
                    this.Item7 = item7;
                    this.Rest = rest;
                }
        
                public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t1, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t2)
                    => throw null;
                public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t1, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t2)
                    => throw null;
        
                public override bool Equals(object o)
                    => throw null;
                public override int GetHashCode()
                    => throw null;
            }
        
            namespace Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
                public sealed class RequiredMemberAttribute : Attribute
                {
                    public RequiredMemberAttribute()
                    {
                    }
                }
        
                [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
                public sealed class CompilerFeatureRequiredAttribute : Attribute
                {
                    public CompilerFeatureRequiredAttribute(string featureName)
                    {
                        FeatureName = featureName;
                    }
                    public string FeatureName { get; }
                    public bool IsOptional { get; set; }
                }
            }
            namespace Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
                public sealed class SetsRequiredMembersAttribute : Attribute
                {
                    public SetsRequiredMembersAttribute()
                    {
                    }
                }
            }
        }
        """;

    [Theory]
    [CombinatorialData]
    public void TupleWithRequiredFields(bool setsRequiredMembers)
    {
        var comp = CreateCompilation(new[] { """
            #pragma warning disable CS0219 // Unused local
            var t1 = new (int, int)(1, 2);
            var t2 = new System.ValueTuple<int, int>(3, 4);
            var t3 = new System.ValueTuple<int, int>();
            (int, int) t4 = default;
            System.ValueTuple<int, int> t5 = default;
            var t6 = new System.ValueTuple<int, int>() {
                Item1 = 1,
                Item2 = 2,
                Property = 3,
                AnotherField = 4
            };
            """, TupleWithRequiredMemberDefinition(setsRequiredMembers) }, targetFramework: TargetFramework.Mscorlib461 /* Using 461 to get a framework without ValueTuple */);

        if (setsRequiredMembers)
        {
            comp.VerifyDiagnostics(
                // 0.cs(2,14): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                // var t1 = new (int, int)(1, 2);
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "(int, int)").WithLocation(2, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item2").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item1").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).AnotherField").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Property").WithLocation(4, 14)
            );
        }
        else
        {
            comp.VerifyDiagnostics(
                // 0.cs(2,14): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                // var t1 = new (int, int)(1, 2);
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "(int, int)").WithLocation(2, 14),
                // 0.cs(3,14): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // var t2 = new System.ValueTuple<int, int>(3, 4);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item2").WithLocation(3, 14),
                // 0.cs(3,14): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // var t2 = new System.ValueTuple<int, int>(3, 4);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item1").WithLocation(3, 14),
                // 0.cs(3,14): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // var t2 = new System.ValueTuple<int, int>(3, 4);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).AnotherField").WithLocation(3, 14),
                // 0.cs(3,14): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // var t2 = new System.ValueTuple<int, int>(3, 4);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Property").WithLocation(3, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item2").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Item1").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).AnotherField").WithLocation(4, 14),
                // 0.cs(4,14): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // var t3 = new System.ValueTuple<int, int>();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "System.ValueTuple<int, int>").WithArguments("(int, int).Property").WithLocation(4, 14)
            );
        }
    }

    [Theory]
    [CombinatorialData]
    public void TupleWithRequiredFields_TupleExpressionSyntax(bool setsRequiredMembers)
    {
        var comp = CreateCompilation(new[] { """
            #pragma warning disable CS0219 // Unused local
            var t1 = (1, 2);
            (int, int) t2 = (1, default);
            var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
            """, TupleWithRequiredMemberDefinition(setsRequiredMembers) }, targetFramework: TargetFramework.Mscorlib461 /* Using 461 to get a framework without ValueTuple */);

        if (setsRequiredMembers)
        {
            comp.VerifyEmitDiagnostics();
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // 0.cs(2,10): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // var t1 = (1, 2);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2)").WithArguments("(int, int).Item2").WithLocation(2, 10),
                // 0.cs(2,10): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // var t1 = (1, 2);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2)").WithArguments("(int, int).Item1").WithLocation(2, 10),
                // 0.cs(2,10): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // var t1 = (1, 2);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2)").WithArguments("(int, int).AnotherField").WithLocation(2, 10),
                // 0.cs(2,10): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // var t1 = (1, 2);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2)").WithArguments("(int, int).Property").WithLocation(2, 10),
                // 0.cs(3,17): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // (int, int) t2 = (1, default);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, default)").WithArguments("(int, int).Item2").WithLocation(3, 17),
                // 0.cs(3,17): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // (int, int) t2 = (1, default);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, default)").WithArguments("(int, int).Item1").WithLocation(3, 17),
                // 0.cs(3,17): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // (int, int) t2 = (1, default);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, default)").WithArguments("(int, int).AnotherField").WithLocation(3, 17),
                // 0.cs(3,17): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // (int, int) t2 = (1, default);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, default)").WithArguments("(int, int).Property").WithLocation(3, 17),
                // 0.cs(4,10): error CS9035: Required member '(int, int).Item2' must be set in the object initializer or attribute constructor.
                // var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int).Item2").WithLocation(4, 10),
                // 0.cs(4,10): error CS9035: Required member '(int, int).Item1' must be set in the object initializer or attribute constructor.
                // var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int).Item1").WithLocation(4, 10),
                // 0.cs(4,10): error CS9035: Required member '(int, int).AnotherField' must be set in the object initializer or attribute constructor.
                // var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int).AnotherField").WithLocation(4, 10),
                // 0.cs(4,10): error CS9035: Required member '(int, int).Property' must be set in the object initializer or attribute constructor.
                // var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int).Property").WithLocation(4, 10),
                // 0.cs(4,10): error CS9035: Required member 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Rest' must be set in the object initializer or attribute constructor.
                // var t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Rest").WithLocation(4, 10)
            );
        }

        var tree = comp.SyntaxTrees[0];
        var tuple = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
        var model = comp.GetSemanticModel(tree);
        var tupleType = model.GetTypeInfo(tuple).Type.GetSymbol<NamedTypeSymbol>()!;

        Assert.True(tupleType.HasDeclaredRequiredMembers);
        AssertEx.Equal(new[] { "AnotherField", "Item1", "Item2", "Property" }, tupleType.AllRequiredMembers
                                                                                        .OrderBy(m => m.Key, StringComparer.InvariantCulture)
                                                                                        .Select(m => m.Key));
        Assert.All(tupleType.TupleElements, field => Assert.True(field.IsRequired));
        Assert.True(tupleType.GetMember<PropertySymbol>("Property").IsRequired);
    }

    [Fact]
    public void IndexedPropertyCannotBeRequired()
    {

        // Equivalent to
        // <RequiredMember>
        // Public Class C
        //     <RequiredMember>
        //     Public Property P1(x As Integer) As Integer
        //         Get
        //             Return 0
        //         End Get
        //         Set
        //         End Set
        //     End Property
        // End Class
        var il = """
            .class public auto ansi C
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .method public specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                }
            
                .method public specialname 
                    instance int32 get_P1 (
                        int32 x
                    ) cil managed 
                {
                    ldc.i4.0
                    ret
                }
            
                .method public specialname 
                    instance void set_P1 (
                        int32 x,
                        int32 Value
                    ) cil managed 
                {
                    ret
                }
            
                .property instance int32 P1(
                    int32 x
                )
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 C::get_P1(int32)
                    .set instance void C::set_P1(int32, int32)
                }
            
            }
            """;

        var comp = CreateCompilationWithIL("_ = new C();", il, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics(
            // (1,9): error CS9037: The required members list for 'C' is malformed and cannot be interpreted.
            // _ = new C();
            Diagnostic(ErrorCode.ERR_RequiredMembersInvalid, "C").WithArguments("C").WithLocation(1, 9)
        );
    }

    /// <summary>
    /// Equivalent to
    /// {RequiredMember}
    /// Public Class C
    ///     {RequiredMember}
    ///     Public Property P1(x As Integer) As Integer
    ///         Get
    ///             Return 0
    ///         End Get
    ///         Set
    ///         End Set
    ///     End Property
    /// End Class
    /// </summary>
    private const string IndexedPropertyOverloadWithRequiredMemberIL = """
            .class public auto ansi C
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 00
                )
                .field private int32 _P1
            
                .method public specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                }
            
                .method public hidebysig specialname 
                    instance int32 get_P1 (
                        int32 x
                    ) cil managed 
                {
                    ldc.i4.0
                    ret
                }
            
                .method public hidebysig specialname 
                    instance void set_P1 (
                        int32 x,
                        int32 Value
                    ) cil managed 
                {
                    ret
                }
            
                .method public hidebysig specialname 
                    instance int32 get_P1 () cil managed 
                {
                    ldarg.0
                    ldfld int32 C::_P1
                    ret
                }
            
                .method public hidebysig specialname 
                    instance void set_P1 (
                        int32 AutoPropertyValue
                    ) cil managed 
                {
                    ldarg.0
                    ldarg.1
                    stfld int32 C::_P1
                    ret
                }
            
                .property instance int32 P1(
                    int32 x
                )
                {
                    .get instance int32 C::get_P1(int32)
                    .set instance void C::set_P1(int32, int32)
                }
                .property instance int32 P1()
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 C::get_P1()
                    .set instance void C::set_P1(int32)
                }
            }
            """;

    [Fact]
    public void IndexedPropertyOverload_NoneSet()
    {

        var il = IndexedPropertyOverloadWithRequiredMemberIL;

        var comp = CreateCompilationWithIL("_ = new C();", il, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics(
            // (1,9): error CS9035: Required member 'C.P1' must be set in the object initializer or attribute constructor.
            // _ = new C();
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.P1").WithLocation(1, 9)
        );
    }

    [Fact]
    public void IndexedPropertyOverload_AllSet()
    {

        var il = IndexedPropertyOverloadWithRequiredMemberIL;

        var comp = CreateCompilationWithIL("_ = new C() { P1 = 1 };", il, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics(
        );
    }

    [Fact]
    public void IndexedPropertyOverload_InDerivedType()
    {
        var comp = CreateCompilationWithRequiredMembers("""
            _ = new C2() { };
            _ = new C2() { P1 = 1 };

            public class C1
            {
                public required int P1 {get;set;}
            }
            
            public class C2 : C1
            {
                [System.Runtime.CompilerServices.IndexerNameAttribute(nameof(P1))]
                public int this[int x] => x;
            }
            """);

        comp.VerifyDiagnostics(
            // (1,9): error CS9035: Required member 'C1.P1' must be set in the object initializer or attribute constructor.
            // _ = new C2() { };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C2").WithArguments("C1.P1").WithLocation(1, 9)
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74108")]
    public void CycleOnConstructorAppliedToSelf()
    {
        var source = """
            namespace System.Diagnostics;

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true)]
            public sealed class ConditionalAttribute : Attribute
            {
                [Conditional("blah")]
                public ConditionalAttribute(string condition)
                {
                    Condition = condition;
                }

                public string Condition { get;}
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
            // (6,6): warning CS0436: The type 'ConditionalAttribute' in '' conflicts with the imported type 'ConditionalAttribute' in 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
            //     [Conditional("blah")]
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Conditional").WithArguments("", "System.Diagnostics.ConditionalAttribute", "netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Diagnostics.ConditionalAttribute").WithLocation(6, 6),
            // (6,6): error CS0592: Attribute 'Conditional' is not valid on this declaration type. It is only valid on 'class, method' declarations.
            //     [Conditional("blah")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Conditional").WithArguments("Conditional", "class, method").WithLocation(6, 6)
        );
    }

    [Fact]
    public void UnknownCompilerFeatureRequired()
    {
        // Equivalent to
        // public class C
        // {
        //    public required int Prop { get; set; }
        //    [CompilerFeatureRequired("Unknown")]
        //    public C() {}
        // }
        var il = """
            .class public auto ansi C
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                    01 00 00 0
                )
                .method public specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    // CompilerFeatureRequiredAttribute("Unknown")
                    .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 07 55 6e 6b 6e 6f 77 6e 00 00
                    )
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
                        01 00 5f 43 6f 6e 73 74 72 75 63 74 6f 72 73 20
                        6f 66 20 74 79 70 65 73 20 77 69 74 68 20 72 65
                        71 75 69 72 65 64 20 6d 65 6d 62 65 72 73 20 61
                        72 65 20 6e 6f 74 20 73 75 70 70 6f 72 74 65 64
                        20 69 6e 20 74 68 69 73 20 76 65 72 73 69 6f 6e
                        20 6f 66 20 79 6f 75 72 20 63 6f 6d 70 69 6c 65
                        72 2e 01 00 00
                    )
                    .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 0f 52 65 71 75 69 72 65 64 4d 65 6d 62 65
                        72 73 00 00
                    )
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                }

                .method public hidebysig specialname 
                    instance int32 get_Prop () cil managed 
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldarg.0
                    ldfld int32 C::'<Prop>k__BackingField'
                    ret
                } // end of method C::get_Prop

                .method public hidebysig specialname 
                    instance void set_Prop (
                        int32 'value'
                    ) cil managed 
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    ldarg.0
                    ldarg.1
                    stfld int32 C::'<Prop>k__BackingField'
                    ret
                } // end of method C::set_Prop
                    .property instance int32 Prop()
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .get instance int32 C::get_Prop()
                    .set instance void C::set_Prop(int32)
                }
            }
            """ + CompilerFeatureRequiredAttributeIL;

        var comp = CreateCompilationWithIL(source: "", ilSource: il, targetFramework: TargetFramework.Net70);
        var c = comp.GetTypeByMetadataName("C");

        MethodSymbol constructor = c!.Constructors.Single();
        AssertEx.Equal(["System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute(\"Unknown\")",
                        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute(\"RequiredMembers\")"],
                       constructor.GetAttributes().Select(a => $"{a.AttributeClass.ToTestDisplayString()}({string.Join(", ", a.CommonConstructorArguments.Select(arg => arg.ToCSharpString()))})"));

        Assert.True(constructor.ShouldCheckRequiredMembers());
    }
}
