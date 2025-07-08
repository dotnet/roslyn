// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
public partial class MoveTypeTests : CSharpMoveTypeTestsBase
{
    [Fact]
    public async Task SingleClassInFile_RenameType()
    {
        await TestRenameTypeToMatchFileAsync(@"[||]class Class1 { }", @"class [|test1|] { }");
    }

    [Fact]
    public async Task MoreThanOneTypeInFile_RenameType()
    {
        await TestRenameTypeToMatchFileAsync(@"[||]class Class1
{ 
    class Inner { }
}", @"class [|test1|]
{ 
    class Inner { }
}");
    }

    [Fact]
    public async Task TestMissing_TypeNameMatchesFileName_RenameType()
    {
        // testworkspace creates files like test1.cs, test2.cs and so on.. 
        // so type name matches filename here and rename file action should not be offered.

        await TestRenameTypeToMatchFileAsync(@"[||]class test1 { }", expectedCodeAction: false);
    }

    [Fact]
    public async Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameType()
    {
        await TestRenameTypeToMatchFileAsync(@"[||]class Class1 { }
class test1 { }", expectedCodeAction: false);
    }

    [Fact]
    public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName1_RenameType()
    {
        await TestRenameTypeToMatchFileAsync(@"[||]class Class1 { }
class Class2 { }", @"class [|test1|] { }
class Class2 { }");
    }

    [Fact]
    public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameType()
    {
        await TestRenameTypeToMatchFileAsync(@"class Class1 { }
[||]class Class2 { }", @"class Class1 { }
class [|test1|] { }");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public async Task NothingOfferedWhenTypeHasNoNameYet1()
    {
        await TestMissingAsync(@"class[||]");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public async Task NothingOfferedWhenTypeHasNoNameYet2()
    {
        await TestMissingAsync(@"class [||]");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public async Task NothingOfferedWhenTypeHasNoNameYet3()
    {
        await TestMissingAsync(@"class [||] { }");
    }
}
