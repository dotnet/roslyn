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

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DocumentRefactoring
    <UseExportProvider>
    Public Class DocumentRefactoringServiceTests
        Private ReadOnly _catalog As ComposableCatalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(DocumentRefactoringService))
        Private ReadOnly _factory As IExportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(_catalog)

        Private Async Function TestDocumentRefactoring(markup As String, Optional expectSuccess As Boolean = True) As Task
            Dim workspace = TestWorkspace.CreateCSharp(markup, exportProvider:=_factory.CreateExportProvider())
            Dim project = workspace.CurrentSolution.Projects.Single()


            Dim document = project.Documents.Single()
            Dim refactorService = workspace.GetService(Of DocumentRefactoringService)

            Await refactorService.UpdateAfterInfoChangeAsync(document, document)

            If (expectSuccess) Then
                document = workspace.CurrentSolution.GetDocument(document.Id)
                Dim syntaxRoot = Await document.GetSyntaxRootAsync().ConfigureAwait(False)

                Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)
                Dim typeDeclarationPairs = syntaxRoot _
                                .DescendantNodes() _
                                .Where(Function(n) syntaxFactsService.IsTypeDeclaration(n)) _
                                .Select(Function(n) Tuple.Create(n, syntaxFactsService.GetDisplayName(n, DisplayNameOptions.None)))

                Assert.True(typeDeclarationPairs.Any())

                Dim matchingTypeDeclarationPair = typeDeclarationPairs.FirstOrDefault(Function(p) String.Equals(p.Item2, Path.GetFileNameWithoutExtension(document.FilePath), StringComparison.OrdinalIgnoreCase))
                Assert.NotEqual(Nothing, matchingTypeDeclarationPair)
            End If
        End Function

        <Fact>
        Public Function TestRefactorSingleClass_RenamesClass() As Task
            Return TestDocumentRefactoring("class Test1
            {
            }")
        End Function

        <Fact>
        Public Function TestRefactorMultipleClasses_RenamesClass() As Task
            Return TestDocumentRefactoring("class Test1
            {
            }

            class OtherClassName
            {
            }")
        End Function
    End Class
End Namespace
