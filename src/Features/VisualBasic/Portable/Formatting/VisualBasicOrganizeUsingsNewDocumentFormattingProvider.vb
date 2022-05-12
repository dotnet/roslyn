' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
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

        Public Function FormatNewDocumentAsync(document As Document, hintDocument As Document, options As CodeCleanupOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements INewDocumentFormattingProvider.FormatNewDocumentAsync
            Return Formatter.OrganizeImportsAsync(document, cancellationToken)
        End Function
    End Class
End Namespace
