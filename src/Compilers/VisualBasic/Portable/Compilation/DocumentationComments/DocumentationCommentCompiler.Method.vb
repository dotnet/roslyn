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

            Public Overrides Sub VisitMethod(symbol As MethodSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then
                    Dim sourceMethod As SourceMethodSymbol =
                        If(TryCast(symbol, SourceMemberMethodSymbol),
                           DirectCast(TryCast(symbol, SourceDeclareMethodSymbol), SourceMethodSymbol))

                    If sourceMethod IsNot Nothing Then
                        WriteDocumentationCommentForMethod(sourceMethod)
                    End If
                End If
            End Sub

            ''' <returns> True, if the comment was written </returns>
            Private Function WriteDocumentationCommentForMethod(method As SourceMethodSymbol) As Boolean

                ' NOTE: In UI scenarios, for partial methods Dev11 always uses implementation part's 
                ' NOTE: comment if it exists ignoring errors, if any. 
                ' NOTE: 
                ' NOTE: In compilation scenarios Dev11 writes into resulting file *both* documentation
                ' NOTE: comments from declaration and implementation parts into separate <member></member> sections, 
                ' NOTE: which seems to be wrong. If any part has error, it does not get into the final XML, thus
                ' NOTE: in case implementation part has errors and declaration part does not, the latest will 
                ' NOTE: finally land in the resulting documentation, meaning that the user sees implementation 
                ' NOTE: part's comment in UI and finds declaration part in the resulting XML.
                ' NOTE: 
                ' NOTE: Roslyn for UI scenarios always (even in presence of errors) returns implementation part's 
                ' NOTE: doc comment to be consistent with Dev11 and in compilation scenario writes implementation 
                ' NOTE: part's comment if it exists and does not have errors, otherwise uses doc comment from 
                ' NOTE: declaration part.

                Dim implementationPart = TryCast(method.PartialImplementationPart, SourceMethodSymbol)
                If implementationPart IsNot Nothing AndAlso WriteDocumentationCommentForMethod(implementationPart) Then
                    ' We used comment from implementation part, that's all
                    Return True
                End If

                Dim docCommentTrivia As DocumentationCommentTriviaSyntax =
                    TryGetDocCommentTriviaAndGenerateDiagnostics(method.Syntax)

                If docCommentTrivia Is Nothing Then
                    Return False
                End If

                Dim wellKnownElementNodes As New Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))

                Dim docCommentXml As String =
                    GetDocumentationCommentForSymbol(method, docCommentTrivia, wellKnownElementNodes)

                ' No further processing
                If docCommentXml Is Nothing Then
                    FreeWellKnownElementNodes(wellKnownElementNodes)
                    Return False
                End If

                If docCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics OrElse _writer.IsSpecified Then
                    Dim symbolName As String = GetSymbolName(method)

                    ' Duplicate top-level well known tags
                    ReportWarningsForDuplicatedTags(wellKnownElementNodes)

                    ' <exception>
                    ReportWarningsForExceptionTags(wellKnownElementNodes)

                    ' <returns>
                    If method.IsSub Then
                        If method.MethodKind = MethodKind.DeclareMethod Then
                            ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, ERRID.WRN_XMLDocReturnsOnADeclareSub, wellKnownElementNodes)
                        Else
                            ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, wellKnownElementNodes, symbolName)
                        End If
                    End If

                    ' <param>
                    ReportWarningsForParamAndParamRefTags(wellKnownElementNodes, symbolName, method.Parameters)

                    ' <value>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Value, wellKnownElementNodes, symbolName)

                    ' <typeparam>
                    If method.MethodKind = MethodKind.UserDefinedOperator OrElse method.MethodKind = MethodKind.DeclareMethod Then
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)
                    Else
                        ReportWarningsForTypeParamTags(wellKnownElementNodes, symbolName, method.TypeParameters)
                    End If

                    ' <typeparamref>
                    ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, method, docCommentTrivia.SyntaxTree)
                End If

                FreeWellKnownElementNodes(wellKnownElementNodes)

                WriteDocumentationCommentForSymbol(docCommentXml)
                Return True
            End Function

        End Class

    End Class
End Namespace
