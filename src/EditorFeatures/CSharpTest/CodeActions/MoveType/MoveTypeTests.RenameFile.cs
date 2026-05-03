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
    public Task SingleClassInFile_RenameFile()
        => TestRenameFileToMatchTypeAsync(@"[||]class Class1 { }", "Class1.cs");

    [Fact]
    public Task MoreThanOneTypeInFile_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            [||]class Class1
            { 
                class Inner { }
            }
            """, "Class1.cs");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16284")]
    public Task MoreThanOneTypeInFile_RenameFile_InnerType()
        => TestRenameFileToMatchTypeAsync("""
            class Class1
            { 
                [||]class Inner { }
            }
            """, "Class1.Inner.cs");

    [Fact]
    public Task TestRenameFileWithFolders()
        => TestRenameFileToMatchTypeAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document Folders="A\B"> 
            [||]class Class1
            { 
                class Inner { }
            }
                    </Document>
                </Project>
            </Workspace>
            """, "Class1.cs",
            destinationDocumentContainers: ["A", "B"]);

    [Fact]
    public Task TestMissing_TypeNameMatchesFileName_RenameFile()
        => TestRenameFileToMatchTypeAsync(@"[||]class test1 { }", expectedCodeAction: false);

    [Fact]
    public Task TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            [||]class Class1 { }
            class test1 { }
            """, expectedCodeAction: false);

    [Fact]
    public Task MultipleTopLevelTypesInFileAndNoneMatchFileName_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            [||]class Class1 { }
            class Class2 { }
            """, "Class1.cs");

    [Fact]
    public Task MultipleTopLevelTypesInFileAndNoneMatchFileName2_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            class Class1 { }
            [||]class Class2 { }
            """, "Class2.cs");

    [Fact]
    public Task NestedFile_Simple_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            class OuterType
            {
                [||]class InnerType { }
            }
            """, "InnerType.cs");

    [Fact]
    public Task NestedFile_DottedName_RenameFile()
        => TestRenameFileToMatchTypeAsync("""
            class OuterType
            {
                [||]class InnerType { }
            }
            """, "OuterType.InnerType.cs");
}
