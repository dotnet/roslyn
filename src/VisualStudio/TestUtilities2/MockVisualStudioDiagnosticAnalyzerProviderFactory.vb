' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <PartNotDiscoverable>
    <Export(GetType(IVisualStudioDiagnosticAnalyzerProviderFactory)), [Shared]>
    Friend Class MockVisualStudioDiagnosticAnalyzerProviderFactory
        Implements IVisualStudioDiagnosticAnalyzerProviderFactory

        Public Property Extensions As (Paths As String(), Id As String)() = Array.Empty(Of (Paths As String(), Id As String))()

        Public Property ContentTypeName As String = "Microsoft.VisualStudio.Analyzer"

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetOrCreateProviderAsync(cancellationToken As CancellationToken) As Task(Of VisualStudioDiagnosticAnalyzerProvider) Implements IVisualStudioDiagnosticAnalyzerProviderFactory.GetOrCreateProviderAsync
            Return Task.FromResult(New VisualStudioDiagnosticAnalyzerProvider(New MockExtensionManager(Extensions, ContentTypeName), GetType(MockExtensionManager.MockContent)))
        End Function
    End Class
End Namespace
