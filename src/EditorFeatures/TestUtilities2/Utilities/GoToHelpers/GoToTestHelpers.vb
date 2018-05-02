' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CSharp.GeneratedCodeRecognition
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.GeneratedCodeRecognition
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Module GoToTestHelpers
        Public ReadOnly Catalog As ComposableCatalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                        GetType(MockDocumentNavigationServiceFactory),
                        GetType(MockSymbolNavigationServiceFactory),
                        GetType(DefaultSymbolNavigationServiceFactory),
                        GetType(CSharpGoToDefinitionSymbolService),
                        GetType(VisualBasicGoToDefinitionSymbolService),
                        GetType(CSharpGeneratedCodeRecognitionService),
                        GetType(VisualBasicGeneratedCodeRecognitionService))

        Public ReadOnly ExportProviderFactory As IExportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(Catalog)
    End Module

    Friend Structure FilePathAndSpan
        Implements IComparable(Of FilePathAndSpan)

        Public ReadOnly Property FilePath As String
        Public ReadOnly Property Span As TextSpan

        Public Sub New(filePath As String, span As TextSpan)
            Me.FilePath = filePath
            Me.Span = span
        End Sub

        Public Function CompareTo(other As FilePathAndSpan) As Integer Implements IComparable(Of FilePathAndSpan).CompareTo
            Dim result = String.CompareOrdinal(FilePath, other.FilePath)

            If result <> 0 Then
                Return result
            End If

            Return Span.CompareTo(other.Span)
        End Function

        Public Overrides Function ToString() As String
            Return $"{FilePath}, {Span}"
        End Function
    End Structure

End Namespace
