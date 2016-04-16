' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.Host
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
            Dim root = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), SyntaxNode)
            Dim rewriter = New Rewriter(placeSystemNamespaceFirst)
            Dim newRoot = rewriter.Visit(root)
            Return document.WithSyntaxRoot(newRoot)
        End Function

        Public ReadOnly Property OrganizeImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.OrganizeImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.OrganizeImportsWithAccelerator
            End Get
        End Property

        Public ReadOnly Property SortImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.SortImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.SortImportsWithAccelerator
            End Get
        End Property

        Public ReadOnly Property RemoveUnusedImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.RemoveUnusedImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.RemoveUnnecessaryImportsWithAccelerator
            End Get
        End Property

        Public ReadOnly Property SortAndRemoveUnusedImportsDisplayStringWithAccelerator As String Implements IOrganizeImportsService.SortAndRemoveUnusedImportsDisplayStringWithAccelerator
            Get
                Return VBFeaturesResources.RemoveAndSortImportsWithAccelerator
            End Get
        End Property
    End Class
End Namespace
