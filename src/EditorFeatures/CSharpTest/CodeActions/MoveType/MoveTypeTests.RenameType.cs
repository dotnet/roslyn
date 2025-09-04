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
    public Task SingleClassInFile_RenameType()
        => TestRenameTypeToMatchFileAsync(@"[||]class Class1 { }", @"class [|test1|] { }");

    [Fact]
    public Task MoreThanOneTypeInFile_RenameType()
        => TestRenameTypeToMatchFileAsync("""
            [||]class Class1
            { 
                class Inner { }
            }
            """, """
            class [|test1|]
            { 
                class Inner { }
            }
            """);

    [Fact]
    public Task TestMissing_TypeNameMatchesFileName_RenameType()
        => TestRenameTypeToMatchFileAsync(@"[||]class test1 { }", expectedCodeAction: false);

    [Fact]
    public Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameType()
        => TestRenameTypeToMatchFileAsync("""
            [||]class Class1 { }
            class test1 { }
            """, expectedCodeAction: false);

    [Fact]
    public Task MultipleTopLevelTypesInFileAndNoneMatchFileName1_RenameType()
        => TestRenameTypeToMatchFileAsync("""
            [||]class Class1 { }
            class Class2 { }
            """, """
            class [|test1|] { }
            class Class2 { }
            """);

    [Fact]
    public Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameType()
        => TestRenameTypeToMatchFileAsync("""
            class Class1 { }
            [||]class Class2 { }
            """, """
            class Class1 { }
            class [|test1|] { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public Task NothingOfferedWhenTypeHasNoNameYet1()
        => TestMissingAsync(@"class[||]");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public Task NothingOfferedWhenTypeHasNoNameYet2()
        => TestMissingAsync(@"class [||]");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")]
    public Task NothingOfferedWhenTypeHasNoNameYet3()
        => TestMissingAsync(@"class [||] { }");
}
