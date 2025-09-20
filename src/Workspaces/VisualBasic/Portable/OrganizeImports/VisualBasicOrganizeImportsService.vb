' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.OrganizeImports

Namespace Microsoft.CodeAnalysis.VisualBasic.OrganizeImports
    <ExportLanguageService(GetType(IOrganizeImportsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend NotInheritable Class VisualBasicOrganizeImportsService
        Implements IOrganizeImportsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function OrganizeImportsAsync(document As Document, options As OrganizeImportsOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements IOrganizeImportsService.OrganizeImportsAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim rewriter = New Rewriter(options)
            Dim newRoot = rewriter.Visit(root)
            Return document.WithSyntaxRoot(newRoot)
        End Function

        Public ReadOnly Property SortImportsDisplayStringWithAccelerator As String = VBWorkspaceResources.Sort_Imports_with_accelerator Implements IOrganizeImportsService.SortImportsDisplayStringWithAccelerator
        Public ReadOnly Property SortImportsDisplayStringWithoutAccelerator As String = VBWorkspaceResources.Sort_Imports Implements IOrganizeImportsService.SortImportsDisplayStringWithoutAccelerator

        Public ReadOnly Property SortAndRemoveUnusedImportsDisplayStringWithAccelerator As String = VBWorkspaceResources.Remove_and_Sort_Imports Implements IOrganizeImportsService.SortAndRemoveUnusedImportsDisplayStringWithAccelerator
    End Class
End Namespace
