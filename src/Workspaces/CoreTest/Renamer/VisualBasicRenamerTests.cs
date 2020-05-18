// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer
{
    public class VisualBasicRenamerTests : RenamerTests
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        [Fact]
        public Task VisualBasic_TestEmptyDocument()
        => TestRenameDocumentAsync(
            "",
            "",
            newDocumentName: "NewDocumentName");

        [Fact]
        public Task VisualBasic_RenameDocument_NoRenameType()
        => TestEmptyActionSetAsync(
@"Class C
End Class",
        newDocumentName: "C.cs");

        [Fact]
        public Task VisualBasic_RenameDocument_RenameType()
        => TestRenameDocumentAsync(
@"Class OriginalName
End Class",
@"Class NewDocumentName
End Class",
            documentName: "OriginalName.vb",
            newDocumentName: "NewDocumentName.vb");

        [Fact]
        public Task VisualBasic_RenameDocument_RenameInterface()
        => TestRenameDocumentAsync(
@"Interface IInterface
End Interface",
@"Interface IInterface2
End Interface",
            documentName: "IInterface.vb",
            newDocumentName: "IInterface2.vb");

        [Fact]
        public Task VisualBasic_RenameDocument_RenameEnum()
        => TestRenameDocumentAsync(
            @"enum MyEnum {}",
            @"enum MyEnum2 {}",
            documentName: "MyEnum.vb",
            newDocumentName: "MyEnum2.vb");

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

            return TestRenameDocumentAsync(originalDocuments, expectedDocuments);
        }

        [Fact]
        public Task VisualBasic_RenameDocument_NoRenameNamespace()
        => TestEmptyActionSetAsync(
@"Namespace Test.Path
    Class C
    End Class
End Namespace",
        documentPath: @"Test\Path\Document.vb",
        documentName: @"Document.vb");

        [Fact]
        // https://github.com/dotnet/roslyn/issues/41841 tracks VB support
        public Task VisualBasic_RenameDocument_NamespaceNotSupported()
        => TestEmptyActionSetAsync(
@"Namespace Test.Path
    Class C
    End Class
End Namespace",
        documentPath: @"Test\Path\Document.vb",
        newDocumentPath: @"Test\New\Path\Document.vb",
        documentName: @"Document.vb");
    }
}
