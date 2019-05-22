' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.CaseCorrection, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class CaseCorrectionCodeCleanupProvider
        Implements ICodeCleanupProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public ReadOnly Property Name As String Implements ICodeCleanupProvider.Name
            Get
                Return PredefinedCodeCleanupProviderNames.CaseCorrection
            End Get
        End Property

        Public Function CleanupAsync(document As Document, spans As ImmutableArray(Of TextSpan), Optional cancellationToken As CancellationToken = Nothing) As Task(Of Document) Implements ICodeCleanupProvider.CleanupAsync
            Return CaseCorrector.CaseCorrectAsync(document, spans, cancellationToken)
        End Function

        Public Function CleanupAsync(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), workspace As Workspace, Optional cancellationToken As CancellationToken = Nothing) As Task(Of SyntaxNode) Implements ICodeCleanupProvider.CleanupAsync
            Return Task.FromResult(CaseCorrector.CaseCorrect(root, spans, workspace, cancellationToken))
        End Function
    End Class
End Namespace
