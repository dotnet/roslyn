' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.CaseCorrection, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class CaseCorrectionCodeCleanupProvider
        Implements ICodeCleanupProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="https://github.com/dotnet/roslyn/issues/42820")>
        Public Sub New()
        End Sub

        Public ReadOnly Property Name As String Implements ICodeCleanupProvider.Name
            Get
                Return PredefinedCodeCleanupProviderNames.CaseCorrection
            End Get
        End Property

        Public Function CleanupAsync(document As Document, spans As ImmutableArray(Of TextSpan), options As CodeCleanupOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements ICodeCleanupProvider.CleanupAsync
            Return CaseCorrector.CaseCorrectAsync(document, spans, cancellationToken)
        End Function

        Public Function CleanupAsync(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), options As SyntaxFormattingOptions, services As SolutionServices, cancellationToken As CancellationToken) As Task(Of SyntaxNode) Implements ICodeCleanupProvider.CleanupAsync
            Return Task.FromResult(CaseCorrector.CaseCorrect(root, spans, services, cancellationToken))
        End Function
    End Class
End Namespace
