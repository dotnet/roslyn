// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

[CompilerTrait(CompilerFeature.RequiredMembers)]
public class RequiredMembersTests : CSharpTestBase
{
    private const string RequiredMemberAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class RequiredMemberAttribute : Attribute
    {
        public RequiredMemberAttribute()
        {
        }
    }
}
";

    private CSharpCompilation CreateCompilationWithRequiredMembers(CSharpTestSource source, CSharpParseOptions? parseOptions = null, CSharpCompilationOptions? options = null)
        => CreateCompilation(new[] { source, RequiredMemberAttribute }, options: options, parseOptions: parseOptions);

    private Action<ModuleSymbol> ValidateRequiredMembersInModule(string[] memberPaths)
    {
        return module =>
        {
            foreach (var memberPath in memberPaths)
            {
                var member = module.GlobalNamespace.GetMember(memberPath);
                Assert.True(member is PropertySymbol or FieldSymbol, $"Unexpected member symbol type {member.Kind}");
                Assert.True(member.IsRequired());
            }
        };
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
            Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(4, 30),
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
        var comp = CreateCompilationWithRequiredMembers(@"
class C
{
    internal required int Field;
    internal required int Prop { get; set; }
}
", parseOptions: TestOptions.Regular10);

        comp.VerifyDiagnostics(
            // (4,27): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     internal required int Field;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Field").WithArguments("required members").WithLocation(4, 27),
            // (4,27): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     internal required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(4, 27),
            // (5,27): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     internal required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop").WithArguments("required members").WithLocation(5, 27)
        );
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
        var comp = CreateCompilationWithRequiredMembers(@"
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
", parseOptions: use10 ? TestOptions.Regular10 : null);

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
                    // (4,12): error CS9500: Types and aliases cannot not be named 'required'.
                    //     struct required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(4, 12),
                    // (8,11): error CS9500: Types and aliases cannot not be named 'required'.
                    //     class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(8, 11),
                    // (12,15): error CS9500: Types and aliases cannot not be named 'required'.
                    //     interface required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(12, 15),
                    // (16,19): error CS9500: Types and aliases cannot not be named 'required'.
                    //     delegate void required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(16, 19),
                    // (20,12): error CS9500: Types and aliases cannot not be named 'required'.
                    //     record required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(20, 12),
                    // (24,19): error CS9500: Types and aliases cannot not be named 'required'.
                    //     record struct required();
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(24, 19),
                    // (30,15): error CS9500: Types and aliases cannot not be named 'required'.
                    //         class required {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(30, 15),
                    // (35,11): error CS9500: Types and aliases cannot not be named 'required'.
                    //     class required<T> {}
                    Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(35, 11)
                }
        );
    }

    [Fact]
    public void MissingRequiredMemberAttribute()
    {
        var comp = CreateCompilation(@"
class C
{
    public required int I { get; }
}");

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RequiredMemberAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute", ".ctor").WithLocation(2, 7);
        comp.VerifyDiagnostics(expected);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void MissingRequiredMemberAttributeCtor()
    {
        var comp = CreateCompilation(@"
class C
{
    public required int I { get; }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequiredMemberAttribute : Attribute
    {
        public RequiredMemberAttribute(int i) {}
    }
}
");

        // (2,7): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RequiredMemberAttribute..ctor'
        // class C
        var expected = Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute", ".ctor").WithLocation(2, 7);
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

        var verifier = CompileAndVerify(comp, sourceSymbolValidator: ValidateRequiredMembersInModule(expectedRequiredMembers));
        verifier.VerifyDiagnostics(
            // (5,25): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     public required int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(5, 25)
        );

        verifier.VerifyTypeIL("C", @"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .field public int32 Field
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 C::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method C::get_Prop
    .method public hidebysig specialname 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2058
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 C::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method C::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2061
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [netstandard]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 C::get_Prop()
        .set instance void C::set_Prop(int32)
    }
} // end of class C
");
    }

    [Fact]
    public void RequiredMemberAttributeEmitted_OverrideRequiredProperty_MissingRequiredOnOverride()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class Base
{
    public virtual required int Prop { get; set; }
}
class Dervied : Base
{
    public override int Prop { get; set; }
}
");

        comp.VerifyDiagnostics(
            // (8,25): error CS9501: 'Dervied.Prop': cannot remove 'required' from 'Base.Prop' when overriding
            //     public override int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_OverrideMustHaveRequired, "Prop").WithArguments("Dervied.Prop", "Base.Prop").WithLocation(8, 25)
        );
    }

    [Fact]
    public void RequiredMemberAttributeEmitted_OverrideRequiredProperty()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class Base
{
    public virtual required int Prop { get; set; }
}
class Derived : Base
{
    public override required int Prop { get; set; }
}
");

        var expectedRequiredMembers = new[] { "Base.Prop", "Derived.Prop" };

        var verifier = CompileAndVerify(comp, sourceSymbolValidator: ValidateRequiredMembersInModule(expectedRequiredMembers));
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("Base", @"
.class private auto ansi beforefieldinit Base
    extends [netstandard]System.Object
{
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method Base::get_Prop
    .method public hidebysig specialname newslot virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2058
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Base::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method Base::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2061
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [netstandard]System.Object::.ctor()
        IL_0006: ret
    } // end of method Base::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_Prop()
        .set instance void Base::set_Prop(int32)
    }
} // end of class Base
");

        verifier.VerifyTypeIL("Derived", @"
.class private auto ansi beforefieldinit Derived
    extends Base
{
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname virtual 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2069
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method Derived::get_Prop
    .method public hidebysig specialname virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2071
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Derived::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method Derived::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x207a
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void Base::.ctor()
        IL_0006: ret
    } // end of method Derived::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_Prop()
        .set instance void Derived::set_Prop(int32)
    }
} // end of class Derived
");
    }

    [Fact]
    public void RequiredMemberAttributeEmitted_AddRequiredOnOverride()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
class Base
{
    public virtual int Prop { get; set; }
}
class Derived : Base
{
    public override required int Prop { get; set; }
}
class DerivedDerived : Derived
{
    public override required int Prop { get; set; }
}
");

        var expectedRequiredMembers = new[] { "Derived.Prop", "DerivedDerived.Prop" };

        var verifier = CompileAndVerify(comp, sourceSymbolValidator: ValidateRequiredMembersInModule(expectedRequiredMembers));
        verifier.VerifyDiagnostics();

        verifier.VerifyTypeIL("Base", @"
.class private auto ansi beforefieldinit Base
    extends [netstandard]System.Object
{
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method Base::get_Prop
    .method public hidebysig specialname newslot virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2058
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Base::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method Base::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2061
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [netstandard]System.Object::.ctor()
        IL_0006: ret
    } // end of method Base::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .get instance int32 Base::get_Prop()
        .set instance void Base::set_Prop(int32)
    }
} // end of class Base
");

        verifier.VerifyTypeIL("Derived", @"
.class private auto ansi beforefieldinit Derived
    extends Base
{
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname virtual 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2069
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method Derived::get_Prop
    .method public hidebysig specialname virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2071
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Derived::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method Derived::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x207a
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void Base::.ctor()
        IL_0006: ret
    } // end of method Derived::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_Prop()
        .set instance void Derived::set_Prop(int32)
    }
} // end of class Derived
");

        verifier.VerifyTypeIL("DerivedDerived", @"
.class private auto ansi beforefieldinit DerivedDerived
    extends Derived
{
    .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field private int32 '<Prop>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname virtual 
        instance int32 get_Prop () cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2082
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld int32 DerivedDerived::'<Prop>k__BackingField'
        IL_0006: ret
    } // end of method DerivedDerived::get_Prop
    .method public hidebysig specialname virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x208a
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 DerivedDerived::'<Prop>k__BackingField'
        IL_0007: ret
    } // end of method DerivedDerived::set_Prop
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2093
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void Derived::.ctor()
        IL_0006: ret
    } // end of method DerivedDerived::.ctor
    // Properties
    .property instance int32 Prop()
    {
        .custom instance void System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 DerivedDerived::get_Prop()
        .set instance void DerivedDerived::set_Prop(int32)
    }
} // end of class DerivedDerived
");
    }

    [Fact]
    public void HidingRequiredMembers()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
#pragma warning disable CS0649 // Never assigned
class Base
{
    public required int Field;
    public required int Prop { get; set; }
}
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
");

        comp.VerifyDiagnostics(
            // (10,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived1.Field'.
            //     public new int Field; // 1
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeHidden, "Field").WithArguments("Base.Field", "Derived1.Field").WithLocation(10, 20),
            // (11,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived1.Prop'.
            //     public new int Prop { get; set; } // 2
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived1.Prop").WithLocation(11, 20),
            // (15,20): error CS9502: Required member 'Base.Prop' cannot be hidden by 'Derived2.Prop'.
            //     public new int Prop; // 3
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeHidden, "Prop").WithArguments("Base.Prop", "Derived2.Prop").WithLocation(15, 20),
            // (16,20): error CS9502: Required member 'Base.Field' cannot be hidden by 'Derived2.Field'.
            //     public new int Field { get; set; } // 4
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeHidden, "Field").WithArguments("Base.Field", "Derived2.Field").WithLocation(16, 20)
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
internal class InternalClass
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
internal class Outer
{
    protected internal class ProtectedInternalClass
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
    protected class ProtectedClass
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
    private protected class PrivateProtectedClass
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
            // (7,37): error CS9503: Required member 'PublicClass.InternalProtectedProperty' cannot be less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalProtectedProperty").WithArguments("PublicClass.InternalProtectedProperty", "PublicClass").WithLocation(7, 37),
            // (8,27): error CS9503: Required member 'PublicClass.InternalProperty' cannot be less visible than the containing type 'PublicClass'.
            //     internal required int InternalProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("PublicClass.InternalProperty", "PublicClass").WithLocation(8, 27),
            // (9,28): error CS9503: Required member 'PublicClass.ProtectedProperty' cannot be less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("PublicClass.ProtectedProperty", "PublicClass").WithLocation(9, 28),
            // (10,36): error CS9503: Required member 'PublicClass.PrivateProtectedProperty' cannot be less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("PublicClass.PrivateProtectedProperty", "PublicClass").WithLocation(10, 36),
            // (11,26): error CS9503: Required member 'PublicClass.PrivateProperty' cannot be less visible than the containing type 'PublicClass'.
            //     private required int PrivateProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("PublicClass.PrivateProperty", "PublicClass").WithLocation(11, 26),
            // (13,37): error CS9503: Required member 'PublicClass.InternalProtectedField' cannot be less visible than the containing type 'PublicClass'.
            //     internal protected required int InternalProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalProtectedField").WithArguments("PublicClass.InternalProtectedField", "PublicClass").WithLocation(13, 37),
            // (14,27): error CS9503: Required member 'PublicClass.InternalField' cannot be less visible than the containing type 'PublicClass'.
            //     internal required int InternalField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("PublicClass.InternalField", "PublicClass").WithLocation(14, 27),
            // (15,28): error CS9503: Required member 'PublicClass.ProtectedField' cannot be less visible than the containing type 'PublicClass'.
            //     protected required int ProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("PublicClass.ProtectedField", "PublicClass").WithLocation(15, 28),
            // (16,36): error CS9503: Required member 'PublicClass.PrivateProtectedField' cannot be less visible than the containing type 'PublicClass'.
            //     private protected required int PrivateProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("PublicClass.PrivateProtectedField", "PublicClass").WithLocation(16, 36),
            // (17,26): error CS9503: Required member 'PublicClass.PrivateField' cannot be less visible than the containing type 'PublicClass'.
            //     private required int PrivateField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("PublicClass.PrivateField", "PublicClass").WithLocation(17, 26),
            // (24,28): error CS9503: Required member 'InternalClass.ProtectedProperty' cannot be less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("InternalClass.ProtectedProperty", "InternalClass").WithLocation(24, 28),
            // (25,36): error CS9503: Required member 'InternalClass.PrivateProtectedProperty' cannot be less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("InternalClass.PrivateProtectedProperty", "InternalClass").WithLocation(25, 36),
            // (26,26): error CS9503: Required member 'InternalClass.PrivateProperty' cannot be less visible than the containing type 'InternalClass'.
            //     private required int PrivateProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("InternalClass.PrivateProperty", "InternalClass").WithLocation(26, 26),
            // (30,28): error CS9503: Required member 'InternalClass.ProtectedField' cannot be less visible than the containing type 'InternalClass'.
            //     protected required int ProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("InternalClass.ProtectedField", "InternalClass").WithLocation(30, 28),
            // (31,36): error CS9503: Required member 'InternalClass.PrivateProtectedField' cannot be less visible than the containing type 'InternalClass'.
            //     private protected required int PrivateProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("InternalClass.PrivateProtectedField", "InternalClass").WithLocation(31, 36),
            // (32,26): error CS9503: Required member 'InternalClass.PrivateField' cannot be less visible than the containing type 'InternalClass'.
            //     private required int PrivateField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("InternalClass.PrivateField", "InternalClass").WithLocation(32, 26),
            // (40,31): error CS9503: Required member 'Outer.ProtectedInternalClass.InternalProperty' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         internal required int InternalProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalProperty").WithArguments("Outer.ProtectedInternalClass.InternalProperty", "Outer.ProtectedInternalClass").WithLocation(40, 31),
            // (41,32): error CS9503: Required member 'Outer.ProtectedInternalClass.ProtectedProperty' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedProperty").WithArguments("Outer.ProtectedInternalClass.ProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(41, 32),
            // (42,40): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProtectedProperty' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedProperty", "Outer.ProtectedInternalClass").WithLocation(42, 40),
            // (43,30): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProperty' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedInternalClass.PrivateProperty", "Outer.ProtectedInternalClass").WithLocation(43, 30),
            // (46,31): error CS9503: Required member 'Outer.ProtectedInternalClass.InternalField' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         internal required int InternalField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "InternalField").WithArguments("Outer.ProtectedInternalClass.InternalField", "Outer.ProtectedInternalClass").WithLocation(46, 31),
            // (47,32): error CS9503: Required member 'Outer.ProtectedInternalClass.ProtectedField' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         protected required int ProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "ProtectedField").WithArguments("Outer.ProtectedInternalClass.ProtectedField", "Outer.ProtectedInternalClass").WithLocation(47, 32),
            // (48,40): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateProtectedField' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private protected required int PrivateProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedInternalClass.PrivateProtectedField", "Outer.ProtectedInternalClass").WithLocation(48, 40),
            // (49,30): error CS9503: Required member 'Outer.ProtectedInternalClass.PrivateField' cannot be less visible than the containing type 'Outer.ProtectedInternalClass'.
            //         private required int PrivateField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedInternalClass.PrivateField", "Outer.ProtectedInternalClass").WithLocation(49, 30),
            // (57,40): error CS9503: Required member 'Outer.ProtectedClass.PrivateProtectedProperty' cannot be less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedProperty").WithArguments("Outer.ProtectedClass.PrivateProtectedProperty", "Outer.ProtectedClass").WithLocation(57, 40),
            // (58,30): error CS9503: Required member 'Outer.ProtectedClass.PrivateProperty' cannot be less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.ProtectedClass.PrivateProperty", "Outer.ProtectedClass").WithLocation(58, 30),
            // (63,40): error CS9503: Required member 'Outer.ProtectedClass.PrivateProtectedField' cannot be less visible than the containing type 'Outer.ProtectedClass'.
            //         private protected required int PrivateProtectedField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProtectedField").WithArguments("Outer.ProtectedClass.PrivateProtectedField", "Outer.ProtectedClass").WithLocation(63, 40),
            // (64,30): error CS9503: Required member 'Outer.ProtectedClass.PrivateField' cannot be less visible than the containing type 'Outer.ProtectedClass'.
            //         private required int PrivateField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.ProtectedClass.PrivateField", "Outer.ProtectedClass").WithLocation(64, 30),
            // (73,30): error CS9503: Required member 'Outer.PrivateProtectedClass.PrivateProperty' cannot be less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateProperty { get; set; }
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateProperty").WithArguments("Outer.PrivateProtectedClass.PrivateProperty", "Outer.PrivateProtectedClass").WithLocation(73, 30),
            // (79,30): error CS9503: Required member 'Outer.PrivateProtectedClass.PrivateField' cannot be less visible than the containing type 'Outer.PrivateProtectedClass'.
            //         private required int PrivateField;
            Diagnostic(ErrorCode.ERR_RequiredMembersCannotBeLessVisibleThanContainingType, "PrivateField").WithArguments("Outer.PrivateProtectedClass.PrivateField", "Outer.PrivateProtectedClass").WithLocation(79, 30)
        );
    }

    [Fact]
    public void RequiredMembersCannotBeExplicitInterfaceImplementations()
    {
        var comp = CreateCompilationWithRequiredMembers(@"
interface I
{
    int Prop { get; set; }
}
class C : I
{
    required int I.Prop { get; set; }
}
");

        comp.VerifyDiagnostics(
            // (8,20): error CS0106: The modifier 'required' is not valid for this item
            //     required int I.Prop { get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Prop").WithArguments("required").WithLocation(8, 20)
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
            // (3,2): error CS9504: Do not use 'System.Runtime.CompilerSerives.RequiredMembersAttribute'. Use the 'required' keyword on required fields and properties instead.
            // [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMembers, "RequiredMember").WithLocation(3, 2),
            // (6,6): error CS9504: Do not use 'System.Runtime.CompilerSerives.RequiredMembersAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMembers, "RequiredMember").WithLocation(6, 6),
            // (8,6): error CS9504: Do not use 'System.Runtime.CompilerSerives.RequiredMembersAttribute'. Use the 'required' keyword on required fields and properties instead.
            //     [RequiredMember]
            Diagnostic(ErrorCode.ERR_ExplicitRequiredMembers, "RequiredMember").WithLocation(8, 6),
            // (9,16): warning CS0649: Field 'C.Field' is never assigned to, and will always have its default value 0
            //     public int Field;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("C.Field", "0").WithLocation(9, 16)
        );

        var prop = comp.SourceModule.GlobalNamespace.GetMember<PropertySymbol>("C.Prop");
        Assert.False(prop.IsRequired);
    }
}
