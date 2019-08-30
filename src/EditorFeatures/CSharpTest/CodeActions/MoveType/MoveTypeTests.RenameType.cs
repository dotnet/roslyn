// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
