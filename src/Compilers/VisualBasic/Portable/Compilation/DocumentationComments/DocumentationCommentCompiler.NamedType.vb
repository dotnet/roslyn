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

            ''' <summary>
            ''' Generates the documentation comment for the type, writes it into 
            ''' the writer
            ''' </summary>
            Public Overrides Sub VisitNamedType(symbol As NamedTypeSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then
                    Dim sourceNamedType = TryCast(symbol, SourceNamedTypeSymbol)
                    If sourceNamedType IsNot Nothing Then
                        WriteDocumentationCommentForNamedType(sourceNamedType)
                    End If

                    If Not Me._isForSingleSymbol Then
                        For Each member In symbol.GetMembers()
                            Me.Visit(member)
                        Next
                    End If
                End If
            End Sub

            Private Sub WriteDocumentationCommentForNamedType(namedType As SourceNamedTypeSymbol)

                Dim multipleDocComments = ArrayBuilder(Of DocumentationCommentTriviaSyntax).GetInstance
                Dim maxDocumentationMode As DocumentationMode = DocumentationMode.None

                For Each reference In namedType.SyntaxReferences
                    Dim trivia As DocumentationCommentTriviaSyntax =
                        TryGetDocCommentTriviaAndGenerateDiagnostics(reference.GetVisualBasicSyntax(Me._cancellationToken))

                    If trivia IsNot Nothing Then
                        multipleDocComments.Add(trivia)

                        Dim documentationMode As DocumentationMode = trivia.SyntaxTree.Options.DocumentationMode
                        If maxDocumentationMode < documentationMode Then
                            maxDocumentationMode = documentationMode
                        End If
                    End If
                Next

                Dim symbolName As String = GetSymbolName(namedType)

                If multipleDocComments.Count > 1 Then
                    ' In case we have multiple documentation comments we should discard 
                    ' all of them with (optionally) reporting all diagnostics 
                    If maxDocumentationMode = DocumentationMode.Diagnose Then
                        For Each trivia In multipleDocComments
                            Me._diagnostics.Add(ERRID.WRN_XMLDocOnAPartialType, trivia.GetLocation(), symbolName)
                        Next
                    End If
                    multipleDocComments.Free()

                    ' No further processing of any of the found documentation comments
                    Return

                ElseIf multipleDocComments.Count = 0 Then
                    multipleDocComments.Free()
                    Return
                End If

                Dim wellKnownElementNodes As New Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))
                Dim theOnlyDocumentationCommentTrivia As DocumentationCommentTriviaSyntax = multipleDocComments(0)
                multipleDocComments.Free()

                Dim docCommentXml As String = GetDocumentationCommentForSymbol(namedType, theOnlyDocumentationCommentTrivia, wellKnownElementNodes)

                ' No further processing
                If docCommentXml Is Nothing Then
                    FreeWellKnownElementNodes(wellKnownElementNodes)
                    Return
                End If

                If theOnlyDocumentationCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics OrElse _writer.IsSpecified Then
                    Dim delegateInvoke As MethodSymbol = namedType.DelegateInvokeMethod

                    ' Duplicate top-level well known tags
                    ReportWarningsForDuplicatedTags(wellKnownElementNodes)

                    ' <exception>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Exception, wellKnownElementNodes, symbolName)

                    ' <returns>
                    If namedType.TypeKind = TypeKind.Delegate Then
                        If delegateInvoke IsNot Nothing AndAlso delegateInvoke.IsSub Then
                            ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, wellKnownElementNodes, "delegate sub")
                        End If
                    Else
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, wellKnownElementNodes, symbolName)
                    End If

                    ' <param> & <paramref>
                    If namedType.TypeKind = TypeKind.Delegate Then
                        ReportWarningsForParamAndParamRefTags(wellKnownElementNodes, GetSymbolName(delegateInvoke), delegateInvoke.Parameters)
                    Else
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.Param, wellKnownElementNodes, symbolName)
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.ParamRef, wellKnownElementNodes, symbolName)
                    End If

                    ' <value>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Value, wellKnownElementNodes, symbolName)

                    ' <typeparam> & <typeparamref>
                    If namedType.TypeKind = TypeKind.Enum Then
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)
                        ' <typeparamref>
                        ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, namedType, theOnlyDocumentationCommentTrivia.SyntaxTree)

                    ElseIf namedType.TypeKind = TypeKind.Enum OrElse namedType.TypeKind = TypeKind.Module Then
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)
                        ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParamRef, wellKnownElementNodes, symbolName)

                    Else
                        ReportWarningsForTypeParamTags(wellKnownElementNodes, symbolName, namedType.TypeParameters)
                        ' <typeparamref>
                        ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, namedType, theOnlyDocumentationCommentTrivia.SyntaxTree)
                    End If
                End If

                FreeWellKnownElementNodes(wellKnownElementNodes)

                WriteDocumentationCommentForSymbol(docCommentXml)
            End Sub

        End Class

    End Class
End Namespace
