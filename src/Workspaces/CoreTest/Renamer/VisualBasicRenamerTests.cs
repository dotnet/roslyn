// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer
{
    public class VisualBasicRenamerTests : RenamerTests
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        [Fact]
        public Task VisualBasic_TestEmptyDocument()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(""),
            MakeSingleDocumentWithInfoArray("", "NewDocumentName"));

        [Fact]
        public Task VisualBasic_RenameDocument_NoRenameType()
        => TestEmptyActionSet(
@"Class C
End Class",
        newDocumentName: "C.cs");

        [Fact]
        public Task VisualBasic_RenameDocument_RenameType()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(
@"Class OriginalName
End Class",
                name: "OriginalName.vb"),
            MakeSingleDocumentWithInfoArray(
@"Class NewDocumentName
End Class",
                name: "NewDocumentName.vb"),
            RenameHelpers.MakeSymbolPairs("OriginalName", "NewDocumentName"));

        [Fact]
        public Task VisualBasic_RenameDocument_RenameInterface()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(
@"Interface IInterface
End Interface",
                name: "IInterface.vb"),
            MakeSingleDocumentWithInfoArray(
@"Interface IInterface2
End Interface",
                name: "IInterface2.vb"),
            RenameHelpers.MakeSymbolPairs("IInterface", "IInterface2"));

        [Fact]
        public Task VisualBasic_RenameDocument_RenameEnum()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(
@"Enum MyEnum
End Enum",
                name: "MyEnum.vb"),
            MakeSingleDocumentWithInfoArray(
@"Enum MyEnum2
End Enum",
                name: "MyEnum2.vb"),
            RenameHelpers.MakeSymbolPairs("MyEnum", "MyEnum2"));

        [Fact]
        public Task VisualBasic_RenameDocument_RenamePartialClass()
        {
            var originalDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = @"
Namespace Test
    Partial Class C
    End Class
End Namespace",
                    DocumentFilePath = @"Test\Folder\Path\C.vb",
                    DocumentName = "C.vb"
                },
                new DocumentWithInfo()
                {
                    Text = @"
Namespace Test
    Partial Class C
        Class Other
        End Class
    End Class
End Namespace",
                    DocumentFilePath = @"Test\Folder\Path\C.Other.vb",
                    DocumentName = "C.Other.vb"
                }
            };

            var expectedDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = @"
Namespace Test
    Partial Class C2
    End Class
End Namespace",
                    DocumentFilePath = @"Test\Folder\Path\C2.vb",
                    DocumentName = "C2.vb"
                },
                new DocumentWithInfo()
                {
                    Text = @"
Namespace Test
    Partial Class C2
        Class Other
        End Class
    End Class
End Namespace",
                    DocumentFilePath = @"Test\Folder\Path\C.Other.vb",
                    DocumentName = "C.Other.vb"
                }
            };

            return TestRenameDocument(
                originalDocuments,
                expectedDocuments,
                RenameHelpers.MakeSymbolPairs("Test.C", "Test.C2"));
        }

        [Fact]
        public Task VisualBasic_RenameDocument_NoRenameNamespace()
        => TestEmptyActionSet(
@"Namespace Test.Path
    Class C
    End Class
End Namespace",
        documentPath: @"Test\Path\Document.vb",
        documentName: @"Document.vb");

        [Fact]
        // https://github.com/dotnet/roslyn/issues/41841 tracks VB support
        public Task VisualBasic_RenameDocument_NamespaceNotSupported()
        => TestEmptyActionSet(
@"Namespace Test.Path
    Class C
    End Class
End Namespace",
        documentPath: @"Test\Path\Document.vb",
        newDocumentPath: @"Test\New\Path\Document.vb",
        documentName: @"Document.vb");
    }
}
