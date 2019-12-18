' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DocumentRefactoring
Imports Microsoft.VisualStudio.Composition
Imports System.IO
Imports System.Xml

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DocumentRefactoring
    <UseExportProvider>
    Public Class DocumentRefactoringServiceTests
        Private ReadOnly _catalog As ComposableCatalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(DocumentRefactoringService))
        Private ReadOnly _factory As IExportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(_catalog)

        Private Function TestDocumentRefactoring(markup As String, Optional expectMatchingName As Boolean = True) As Task
            Dim workspace = TestWorkspace.CreateCSharp(markup, exportProvider:=_factory.CreateExportProvider())
            Return TestDocumentRefactoring(workspace, expectMatchingName:=expectMatchingName)
        End Function

        Private Function TestDocumentRefactoring(files() As String, Optional ExpectMatchingName As Boolean = True) As Task
            Dim workspace = TestWorkspace.CreateCSharp(files, exportProvider:=_factory.CreateExportProvider())
            Return TestDocumentRefactoring(workspace, expectMatchingName:=ExpectMatchingName)
        End Function

        Private Async Function TestDocumentRefactoring(workspace As TestWorkspace, Optional expectMatchingName As Boolean = True) As Task
            Dim project = workspace.CurrentSolution.Projects.Single()
            Dim document = project.Documents.First()
            Dim newDocument = document.WithName("NewName.cs")

            Dim refactorService = workspace.GetService(Of DocumentRefactoringService)

            Dim solution = Await refactorService.UpdateAfterInfoChangeAsync(current:=newDocument, previous:=document)

            Dim afterUpdateDocument = solution.GetDocument(document.Id)

            Dim syntaxRoot = Await afterUpdateDocument.GetSyntaxRootAsync().ConfigureAwait(False)

            Dim syntaxFactsService = afterUpdateDocument.GetLanguageService(Of ISyntaxFactsService)
            Dim typeDeclarationPairs = syntaxRoot _
                                .DescendantNodes() _
                                .Where(Function(n) syntaxFactsService.IsTypeDeclaration(n)) _
                                .Select(Function(n) Tuple.Create(n, syntaxFactsService.GetDisplayName(n, DisplayNameOptions.None)))

            Assert.NotEmpty(typeDeclarationPairs)
            Dim matchingTypeDeclarationPair = typeDeclarationPairs.Where(Function(p) String.Equals(p.Item2, "NewName"))

            If (expectMatchingName) Then
                Assert.NotEmpty(matchingTypeDeclarationPair)
            Else
                Assert.Empty(matchingTypeDeclarationPair)
            End If
        End Function

        <Fact>
        Public Function TestRefactorSingleClass_RenamesClass() As Task
            Return TestDocumentRefactoring("
class Test1 { }
            ")
        End Function

        <Fact>
        Public Function TestRefactorMultipleClasses_RenamesClass() As Task
            Return TestDocumentRefactoring("
class Test1 { }
class OtherClassName { }
            ")
        End Function

        <Fact>
        Public Function TestRefactorSingleClass_CaseInsensitive() As Task
            Return TestDocumentRefactoring("
class test1 { }
            ")
        End Function

        <Fact>
        Public Function TestRefactorSingleClass_NoRename() As Task
            Return TestDocumentRefactoring("
class DifferentNamedClass { }
            ", expectMatchingName:=False)
        End Function


        <Fact>
        Public Function TestRefactorPartialClass_SingleFile() As Task
            Return TestDocumentRefactoring("
partial class Test1 { }
partial class Test1 { }
        ")
        End Function

        <Fact>
        Public Function TestRefactorPartialClass_MultipleFiles() As Task
            Dim files = New String() {
                "partial class Test1 { }",
                "partial class Test1 { }"
                }
            Return TestDocumentRefactoring(files)
        End Function
    End Class
End Namespace
