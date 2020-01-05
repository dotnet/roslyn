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
        public async Task SingleClassInFile_RenameFile()
        {
            var code =
@"[||]class Class1 { }";

            var expectedDocumentName = "Class1.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoreThanOneTypeInFile_RenameFile()
        {
            var code =
@"[||]class Class1
{ 
    class Inner { }
}";

            var expectedDocumentName = "Class1.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WorkItem(16284, "https://github.com/dotnet/roslyn/issues/16284")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoreThanOneTypeInFile_RenameFile_InnerType()
        {
            var code =
@"class Class1
{ 
    [||]class Inner { }
}";

            var expectedDocumentName = "Class1.Inner.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestRenameFileWithFolders()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[||]class Class1
{ 
    class Inner { }
}
        </Document>
    </Project>
</Workspace>";

            var expectedDocumentName = "Class1.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName,
                destinationDocumentContainers: new[] { "A", "B" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_TypeNameMatchesFileName_RenameFile()
        {
            // testworkspace creates files like test1.cs, test2.cs and so on.. 
            // so type name matches filename here and rename file action should not be offered.
            var code =
@"[||]class test1 { }";

            await TestRenameFileToMatchTypeAsync(code, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameFile()
        {
            var code =
@"[||]class Class1 { }
class test1 { }";

            await TestRenameFileToMatchTypeAsync(code, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName_RenameFile()
        {
            var code =
@"[||]class Class1 { }
class Class2 { }";

            var expectedDocumentName = "Class1.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameFile()
        {
            var code =
@"class Class1 { }
[||]class Class2 { }";

            var expectedDocumentName = "Class2.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task NestedFile_Simple_RenameFile()
        {
            var code =
@"class OuterType
{
    [||]class InnerType { }
}";

            var expectedDocumentName = "InnerType.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task NestedFile_DottedName_RenameFile()
        {
            var code =
@"class OuterType
{
    [||]class InnerType { }
}";

            var expectedDocumentName = "OuterType.InnerType.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }
    }
}
