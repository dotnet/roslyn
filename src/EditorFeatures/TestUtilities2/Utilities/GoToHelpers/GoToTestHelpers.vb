' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Module GoToTestHelpers
        Public ReadOnly Composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(
                        GetType(MockDocumentNavigationServiceFactory),
                        GetType(MockSymbolNavigationServiceFactory))
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
