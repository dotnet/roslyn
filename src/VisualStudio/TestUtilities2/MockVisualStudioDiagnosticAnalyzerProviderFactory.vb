' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <PartNotDiscoverable>
    <Export(GetType(IVisualStudioDiagnosticAnalyzerProviderFactory)), [Shared]>
    Friend Class MockVisualStudioDiagnosticAnalyzerProviderFactory
        Implements IVisualStudioDiagnosticAnalyzerProviderFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetOrCreateProviderAsync() As Task(Of VisualStudioDiagnosticAnalyzerProvider) Implements IVisualStudioDiagnosticAnalyzerProviderFactory.GetOrCreateProviderAsync
            Return Task.FromResult(GetOrCreateProviderOnMainThread())
        End Function

        Public Function GetOrCreateProviderOnMainThread() As VisualStudioDiagnosticAnalyzerProvider Implements IVisualStudioDiagnosticAnalyzerProviderFactory.GetOrCreateProviderOnMainThread
            Return New VisualStudioDiagnosticAnalyzerProvider(
                    New MockExtensionManager(Array.Empty(Of String)()),
                    GetType(MockExtensionManager.MockContent))
        End Function
    End Class
End Namespace
