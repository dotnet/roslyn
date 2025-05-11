' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.OrganizeImports
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportNewDocumentFormattingProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicOrganizeUsingsNewDocumentFormattingProvider
        Implements INewDocumentFormattingProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function FormatNewDocumentAsync(document As Document, hintDocument As Document, options As CodeCleanupOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements INewDocumentFormattingProvider.FormatNewDocumentAsync
            Dim service = document.GetRequiredLanguageService(Of IOrganizeImportsService)
            Dim organizeOptions = Await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(False)
            Return Await service.OrganizeImportsAsync(document, organizeOptions, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
