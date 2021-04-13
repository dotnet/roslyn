' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
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

            Public Overrides Sub VisitEvent(symbol As EventSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then
                    Dim sourceEvent = TryCast(symbol, SourceEventSymbol)
                    If sourceEvent IsNot Nothing Then
                        WriteDocumentationCommentForEvent(sourceEvent)
                    End If
                End If
            End Sub

            Private Sub WriteDocumentationCommentForEvent([event] As SourceEventSymbol)

                Dim docCommentTrivia As DocumentationCommentTriviaSyntax =
                    TryGetDocCommentTriviaAndGenerateDiagnostics(
                        [event].SyntaxReference.GetVisualBasicSyntax(Me._cancellationToken))

                If docCommentTrivia Is Nothing Then
                    Return
                End If

                Dim wellKnownElementNodes As New Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))

                Dim docCommentXml As String =
                    GetDocumentationCommentForSymbol([event], docCommentTrivia, wellKnownElementNodes)

                ' No further processing
                If docCommentXml Is Nothing Then
                    FreeWellKnownElementNodes(wellKnownElementNodes)
                    Return
                End If

                If docCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics OrElse _writer.IsSpecified Then
                    Dim symbolName As String = GetSymbolName([event])

                    ' Duplicate top-level well known tags
                    ReportWarningsForDuplicatedTags(wellKnownElementNodes, isEvent:=True)

                    ' <exception>
                    ReportWarningsForExceptionTags(wellKnownElementNodes)

                    ' <returns>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, wellKnownElementNodes, symbolName)

                    ' <param> & <paramref>
                    Dim parameters As ImmutableArray(Of ParameterSymbol) = ImmutableArray(Of ParameterSymbol).Empty
                    Dim delegateType = TryCast([event].Type, NamedTypeSymbol)
                    If delegateType IsNot Nothing Then
                        Dim invokeMethod As MethodSymbol = delegateType.DelegateInvokeMethod
                        If invokeMethod IsNot Nothing Then
                            parameters = invokeMethod.Parameters
                        End If
                    End If

                    ReportWarningsForParamAndParamRefTags(wellKnownElementNodes, symbolName, parameters)

                    ' <value>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Value, wellKnownElementNodes, symbolName)

                    ' <typeparam>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)

                    ' <typeparamref>
                    ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, [event], docCommentTrivia.SyntaxTree)
                End If

                FreeWellKnownElementNodes(wellKnownElementNodes)

                WriteDocumentationCommentForSymbol(docCommentXml)
            End Sub

        End Class

    End Class
End Namespace
