' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.OrganizeImports
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.OrganizeImports
    <ExportLanguageService(GetType(IOrganizeImportsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicOrganizeImportsService
        Implements IOrganizeImportsService

        Public Async Function OrganizeImportsAsync(document As Document,
                                        placeSystemNamespaceFirst As Boolean,
                                        cancellationToken As CancellationToken) As Task(Of Document) Implements IOrganizeImportsService.OrganizeImportsAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim rewriter = New Rewriter(placeSystemNamespaceFirst)
            Dim newRoot = rewriter.Visit(root)
            Return document.WithSyntaxRoot(newRoot)
        End Function

        Public ReadOnly Property SortAndRemoveUnusedImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.SortAndRemoveUnusedImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.Remove_and_Sort_Imports
            End Get
        End Property
    End Class
End Namespace