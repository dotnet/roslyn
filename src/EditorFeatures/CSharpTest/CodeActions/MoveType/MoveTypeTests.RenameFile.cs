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
    public async Task SingleClassInFile_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"[||]class Class1 { }", "Class1.cs");
    }

    [Fact]
    public async Task MoreThanOneTypeInFile_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"[||]class Class1
{ 
    class Inner { }
}", "Class1.cs");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16284")]
    public async Task MoreThanOneTypeInFile_RenameFile_InnerType()
    {
        await TestRenameFileToMatchTypeAsync(@"class Class1
{ 
    [||]class Inner { }
}", "Class1.Inner.cs");
    }

    [Fact]
    public async Task TestRenameFileWithFolders()
    {
        await TestRenameFileToMatchTypeAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[||]class Class1
{ 
    class Inner { }
}
        </Document>
    </Project>
</Workspace>", "Class1.cs",
            destinationDocumentContainers: ["A", "B"]);
    }

    [Fact]
    public async Task TestMissing_TypeNameMatchesFileName_RenameFile()
    {
        // testworkspace creates files like test1.cs, test2.cs and so on.. 
        // so type name matches filename here and rename file action should not be offered.

        await TestRenameFileToMatchTypeAsync(@"[||]class test1 { }", expectedCodeAction: false);
    }

    [Fact]
    public async Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"[||]class Class1 { }
class test1 { }", expectedCodeAction: false);
    }

    [Fact]
    public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"[||]class Class1 { }
class Class2 { }", "Class1.cs");
    }

    [Fact]
    public async Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"class Class1 { }
[||]class Class2 { }", "Class2.cs");
    }

    [Fact]
    public async Task NestedFile_Simple_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"class OuterType
{
    [||]class InnerType { }
}", "InnerType.cs");
    }

    [Fact]
    public async Task NestedFile_DottedName_RenameFile()
    {
        await TestRenameFileToMatchTypeAsync(@"class OuterType
{
    [||]class InnerType { }
}", "OuterType.InnerType.cs");
    }
}
