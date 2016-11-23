' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Public Overrides Sub VisitNamespace(symbol As Symbols.NamespaceSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then

                    If Not Me._isForSingleSymbol Then
                        If symbol.IsGlobalNamespace Then
                            Debug.Assert(Me._assemblyName IsNot Nothing)

                            WriteLine("<?xml version=""1.0""?>")
                            WriteLine("<doc>")
                            Indent()

                            If Not Me._compilation.Options.OutputKind.IsNetModule() Then
                                WriteLine("<assembly>")
                                Indent()
                                WriteLine("<name>")
                                WriteLine(Me._assemblyName)
                                WriteLine("</name>")
                                Unindent()
                                WriteLine("</assembly>")
                            End If

                            WriteLine("<members>")
                            Indent()
                        End If
                    End If

                    WriteDocumentationCommentForNamedType(symbol)
                    
                    If Not Me._isForSingleSymbol Then
                        For Each member In symbol.GetMembers()
                            Me.Visit(member)
                        Next

                        If symbol.IsGlobalNamespace Then
                            Unindent()
                            WriteLine("</members>")
                            Unindent()
                            WriteLine("</doc>")
                        End If
                    End If

                End If
            End Sub
            
            Private Sub WriteDocumentationCommentForNamedType(symbol As Symbols.NamespaceSymbol)

                Dim multipleDocComments = ArrayBuilder(Of DocumentationCommentTriviaSyntax).GetInstance
                Dim maxDocumentationMode As DocumentationMode = DocumentationMode.None

                Dim symbolName As String = GetSymbolName(symbol)

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

                Dim docCommentXml As String = GetDocumentationCommentForSymbol(symbol, theOnlyDocumentationCommentTrivia, wellKnownElementNodes)

                ' No further processing
                If docCommentXml Is Nothing Then
                    FreeWellKnownElementNodes(wellKnownElementNodes)
                    Return
                End If

                If theOnlyDocumentationCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics OrElse _writer.IsSpecified Then

                    ' Duplicate top-level well known tags
                    ReportWarningsForDuplicatedTags(wellKnownElementNodes)

                    ' <exception>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Exception, wellKnownElementNodes, symbolName)

                    ' <returns>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Returns, wellKnownElementNodes, symbolName)

                    ' <param> & <paramref>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Param, wellKnownElementNodes, symbolName)
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.ParamRef, wellKnownElementNodes, symbolName)

                    ' <value>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.Value, wellKnownElementNodes, symbolName)

                    ' <typeparam>
                    ReportIllegalWellKnownTagIfAny(WellKnownTag.TypeParam, wellKnownElementNodes, symbolName)

                    ' <typeparamref>
                    ReportWarningsForTypeParamRefTags(wellKnownElementNodes, symbolName, symbol, theOnlyDocumentationCommentTrivia.SyntaxTree)
                End If

                FreeWellKnownElementNodes(wellKnownElementNodes)

                WriteDocumentationCommentForSymbol(docCommentXml)
            End Sub

        End Class

    End Class
End Namespace
