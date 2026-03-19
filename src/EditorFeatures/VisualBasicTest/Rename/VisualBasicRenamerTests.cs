' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.UnitTests.Renamer
    Public Class VisualBasicRenamerTests
        Inherits RenamerTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        <Fact>
        Public Function VisualBasic_TestEmptyDocument() As Task
            Return TestRenameDocument(
                "",
                "",
                newDocumentName:="NewDocumentName")
        End Function

        <Fact>
        Public Function VisualBasic_RenameDocument_NoRenameType() As Task
            Return TestEmptyActionSet(
"Class C
End Class",
                newDocumentName:="C.cs")
        End Function

        <Fact>
        Public Function VisualBasic_RenameDocument_RenameType() As Task
            Return TestRenameDocument(
"Class OriginalName
End Class",
"Class NewDocumentName
End Class",
            documentName:="OriginalName.vb",
            newDocumentName:="NewDocumentName.vb")
        End Function

        <Fact>
        Public Function VisualBasic_RenameDocument_RenameInterface() As Task
            Return TestRenameDocument(
"Interface IInterface
End Interface",
"Interface IInterface2
End Interface",
                documentName:="IInterface.vb",
                newDocumentName:="IInterface2.vb")
        End Function

        <Fact>
        Public Function VisualBasic_RenameDocument_RenameEnum() As Task
            Return TestRenameDocument(
"enum MyEnum {}",
"enum MyEnum2 {}",
                documentName:="MyEnum.vb",
                newDocumentName:="MyEnum2.vb")
        End Function

        <Fact>
        public Function VisualBasic_RenameDocument_RenamePartialClass() As Task
            Dim originalDocuments = {
                New DocumentWithInfo() With
                {
                    .Text = "
Namespace Test
    Partial Class C
    End Class
End Namespace",
                    .DocumentFilePath = "Test\Folder\Path\C.vb",
                    .DocumentName = "C.vb"
                },
                new DocumentWithInfo() With
                {
                    .Text = "
Namespace Test
    Partial Class C
        Class Other
        End Class
    End Class
End Namespace",
                    .DocumentFilePath = "Test\Folder\Path\C.Other.vb",
                    .DocumentName = "C.Other.vb"
                }
            }

            Dim expectedDocuments =
            {
                new DocumentWithInfo() with
                {
                    .Text = "
Namespace Test
    Partial Class C2
    End Class
End Namespace",
                    .DocumentFilePath = "Test\Folder\Path\C2.vb",
                    .DocumentName = "C2.vb"
                },
                new DocumentWithInfo() with
                {
                    .Text = "
Namespace Test
    Partial Class C2
        Class Other
        End Class
    End Class
End Namespace",
                    .DocumentFilePath = "Test\Folder\Path\C.Other.vb",
                    .DocumentName = "C.Other.vb"
                }
            }

            return TestRenameDocument(originalDocuments, expectedDocuments)
        End Function

        <Fact>
        Public Function VisualBasic_RenameDocument_NoRenameNamespace() As Task
            Return TestEmptyActionSet(
"Namespace Test.Path
    Class C
    End Class
End Namespace",
            documentPath:="Test\Path\Document.vb",
            documentName:="Document.vb")
        End Function

        ' https://github.com/dotnet/roslyn/issues/41841 tracks VB support
        <Fact>
        Public Function VisualBasic_RenameDocument_NamespaceNotSupported() As Task
            Return TestEmptyActionSet(
"Namespace Test.Path
    Class C
    End Class
End Namespace",
            documentPath:="Test\Path\Document.vb",
            newDocumentPath:="Test\New\Path\Document.vb",
            documentName:="Document.vb")
        End Function
    End Class
End namespace
