﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task SingleClassInFile_RenameType()
        {
            var code =
@"[||]class Class1 { }";

            var codeWithTypeRenamedToMatchFileName =
@"class [|test1|] { }";

            await TestRenameTypeToMatchFileAsync(code, codeWithTypeRenamedToMatchFileName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoreThanOneTypeInFile_RenameType()
        {
            var code =
@"[||]class Class1
{ 
    class Inner { }
}";

            var codeWithTypeRenamedToMatchFileName =
@"class [|test1|]
{ 
    class Inner { }
}";

            await TestRenameTypeToMatchFileAsync(code, codeWithTypeRenamedToMatchFileName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_TypeNameMatchesFileName_RenameType()
        {
            // testworkspace creates files like test1.cs, test2.cs and so on.. 
            // so type name matches filename here and rename file action should not be offered.
            var code =
@"[||]class test1 { }";

            await TestRenameTypeToMatchFileAsync(code, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameType()
        {
            var code =
@"[||]class Class1 { }
class test1 { }";

            await TestRenameTypeToMatchFileAsync(code, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName1_RenameType()
        {
            var code =
@"[||]class Class1 { }
class Class2 { }";

            var codeWithTypeRenamedToMatchFileName =
@"class [|test1|] { }
class Class2 { }";

            await TestRenameTypeToMatchFileAsync(code, codeWithTypeRenamedToMatchFileName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameType()
        {
            var code =
@"class Class1 { }
[||]class Class2 { }";

            var codeWithTypeRenamedToMatchFileName =
@"class Class1 { }
class [|test1|] { }";

            await TestRenameTypeToMatchFileAsync(code, codeWithTypeRenamedToMatchFileName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(40043, "https://github.com/dotnet/roslyn/issues/40043")]
        public async Task NothingOfferedWhenTypeHasNoNameYet1()
        {
            var code = @"class[||]";
            await TestMissingAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(40043, "https://github.com/dotnet/roslyn/issues/40043")]
        public async Task NothingOfferedWhenTypeHasNoNameYet2()
        {
            var code = @"class [||]";
            await TestMissingAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(40043, "https://github.com/dotnet/roslyn/issues/40043")]
        public async Task NothingOfferedWhenTypeHasNoNameYet3()
        {
            var code = @"class [||] { }";
            await TestMissingAsync(code);
        }
    }
}
