' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Public Overrides Sub VisitProperty(symbol As PropertySymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then
                    Dim sourceProperty = TryCast(symbol, SourcePropertySymbol)
                    If sourceProperty IsNot Nothing Then
                        WriteDocumentationCommentForProperty(sourceProperty)
                    End If
                End If
            End Sub

            Private Sub WriteDocumentationCommentForProperty([property] As SourcePropertySymbol)
                If [property].IsWithEventsProperty Then
                    Return
                End If

                Dim docCommentTrivia As DocumentationCommentTriviaSyntax =
                    TryGetDocCommentTriviaAndGenerateDiagnostics(If([property].BlockSyntaxReference, [property].SyntaxReference).GetVisualBasicSyntax(Me._cancellationToken))

                If docCommentTrivia Is Nothing Then
                    Return
                End If

                Dim wellKnownElementNodes As New Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))

                Dim docCommentXml As String =
                    GetDocumentationCommentForSymbol([property], docCommentTrivia, wellKnownElementNodes)

                ' No further processing
                If docCommentXml Is Nothing Then
                    FreeWellKnownElementNodes(wellKnownElementNodes)
                    Return
                End If

                If docCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics OrElse _writer.IsSpecified Then
                    Dim symbolName As String = GetSymbolName([property])

                    ' Duplicate top-level well known tags
                    ReportWarningsForDuplicatedTags(wellKnownElementNodes)

                    ' <exception>
                    ReportWarningsForExceptionTags(wellKnownElementNodes)

                    ' <returns>
                    If [property].IsWriteOnly Then
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, ERRID.WRN_XMLDocReturnsOnWriteOnlyProperty, wellKnownElementNodes)
                    End If

                    ' <param>
                    ReportWarningsForParamAndParamRefTags(wellKnownElementNodes, symbolName, [property].Parameters)

                    ' <typeparam>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)

                    ' <typeparamref>
                    ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, [property], docCommentTrivia.SyntaxTree)
                End If

                FreeWellKnownElementNodes(wellKnownElementNodes)

                WriteDocumentationCommentForSymbol(docCommentXml)
            End Sub

        End Class

    End Class
End Namespace
