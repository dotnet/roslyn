' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.OrganizeImports

Namespace Microsoft.CodeAnalysis.VisualBasic.OrganizeImports
    <ExportLanguageService(GetType(IOrganizeImportsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicOrganizeImportsService
        Implements IOrganizeImportsService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Async Function OrganizeImportsAsync(document As Document,
                                        cancellationToken As CancellationToken) As Task(Of Document) Implements IOrganizeImportsService.OrganizeImportsAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim options = Await document.GetOptionsAsync(cancellationToken).ConfigureAwait(False)

            Dim placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst)
            Dim separateGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups)

            Dim rewriter = New Rewriter(placeSystemNamespaceFirst, separateGroups)
            Dim newRoot = rewriter.Visit(root)
            Return document.WithSyntaxRoot(newRoot)
        End Function

        Public ReadOnly Property SortImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.SortImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.Sort_Imports
            End Get
        End Property

        Public ReadOnly Property SortAndRemoveUnusedImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.SortAndRemoveUnusedImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.Remove_and_Sort_Imports
            End Get
        End Property
    End Class
End Namespace
