// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

[CompilerTrait(CompilerFeature.RequiredMembers)]
public class RequiredMembersTests : CSharpTestBase
{
    [Fact]
    public void InvalidModifierLocations()
    {
        var comp = CreateCompilation(@"
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
        var comp = CreateCompilation(@"
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
        var comp = CreateCompilation(@"
class C
{
    required int Field;
    required int Prop { get; set; }
}
", parseOptions: TestOptions.Regular10);

        comp.VerifyDiagnostics(
                // (4,18): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     required int Field;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "Field").WithArguments("required members").WithLocation(4, 18),
                // (4,18): warning CS0169: The field 'C.Field' is never used
                //     required int Field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Field").WithArguments("C.Field").WithLocation(4, 18),
                // (5,18): error CS8652: The feature 'required members' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     required int Prop { get; set; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop").WithArguments("required members").WithLocation(5, 18)
        );
    }

    [Fact]
    public void DuplicateKeyword()
    {
        var comp = CreateCompilation(@"
class C
{
    required required int Field;
    required required int Prop { get; set; }
}
");

        comp.VerifyDiagnostics(
            // (4,14): error CS1004: Duplicate 'required' modifier
            //     required required int Field;
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "required").WithArguments("required").WithLocation(4, 14),
            // (4,27): warning CS0169: The field 'C.Field' is never used
            //     required required int Field;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "Field").WithArguments("C.Field").WithLocation(4, 27),
            // (5,14): error CS1004: Duplicate 'required' modifier
            //     required required int Prop { get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "required").WithArguments("required").WithLocation(5, 14)
        );
    }

    [Theory]
    [CombinatorialData]
    public void InvalidNames(bool use10)
    {
        var comp = CreateCompilation(@"
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
}
