' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class FxCopAnalyzersSuggestedActionCallbackTests
        <Fact>
        <WorkItem(39092, "https://github.com/dotnet/roslyn/issues/39092")>
        Public Sub TestIsNuGetInstalled()
            ' Verify with FxCop analyzer reference
            Dim fxcopAnalyzerReference = New CustomAnalyzerReference(fullPath:="c:\Microsoft.CodeQuality.Analyzers.dll",
                                                                     display:="Microsoft.CodeQuality.Analyzers")
            Dim analyzerReferences As IEnumerable(Of AnalyzerReference) = {fxcopAnalyzerReference}
            Dim hasUnresolvedAnalyzerReference = False
            Assert.True(FxCopAnalyzersSuggestedActionCallback.IsNuGetInstalled(analyzerReferences, hasUnresolvedAnalyzerReference))
            Assert.False(hasUnresolvedAnalyzerReference)

            ' Verify with analyzer reference with null Display
            Dim analyzerReferenceWithNullDisplay = New CustomAnalyzerReference(fullPath:="c:\temp.dll",
                                                                               display:=Nothing)
            analyzerReferences = {analyzerReferenceWithNullDisplay}
            hasUnresolvedAnalyzerReference = False
            Assert.False(FxCopAnalyzersSuggestedActionCallback.IsNuGetInstalled(analyzerReferences, hasUnresolvedAnalyzerReference))
            Assert.False(hasUnresolvedAnalyzerReference)

            ' Verify with unresolved analyzer reference
            Dim unresolvedAnalyzerReference = New UnresolvedAnalyzerReference(unresolvedPath:="c:\temp.dll")
            analyzerReferences = {unresolvedAnalyzerReference}
            hasUnresolvedAnalyzerReference = False
            Assert.False(FxCopAnalyzersSuggestedActionCallback.IsNuGetInstalled(analyzerReferences, hasUnresolvedAnalyzerReference))
            Assert.True(hasUnresolvedAnalyzerReference)

            ' Verify with a mix of all the above analyzer reference kinds.
            analyzerReferences = {fxcopAnalyzerReference, analyzerReferenceWithNullDisplay, unresolvedAnalyzerReference}
            hasUnresolvedAnalyzerReference = False
            Assert.True(FxCopAnalyzersSuggestedActionCallback.IsNuGetInstalled(analyzerReferences, hasUnresolvedAnalyzerReference))
            Assert.True(hasUnresolvedAnalyzerReference)
        End Sub

        Private NotInheritable Class CustomAnalyzerReference
            Inherits AnalyzerReference

            Public Sub New(fullPath As String, display As String)
                Me.FullPath = fullPath
                Me.Display = display
            End Sub

            Public Overrides ReadOnly Property FullPath As String
            Public Overrides ReadOnly Property Display As String

            Public Overrides ReadOnly Property Id As Object
                Get
                    Return FullPath
                End Get
            End Property

            Public Overrides Function GetAnalyzersForAllLanguages() As ImmutableArray(Of DiagnosticAnalyzer)
                Return ImmutableArray(Of DiagnosticAnalyzer).Empty
            End Function

            Public Overrides Function GetAnalyzers(language As String) As ImmutableArray(Of DiagnosticAnalyzer)
                Return ImmutableArray(Of DiagnosticAnalyzer).Empty
            End Function
        End Class
    End Class
End Namespace
