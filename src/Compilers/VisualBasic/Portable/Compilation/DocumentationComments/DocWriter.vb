' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation
        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Private Structure DocWriter

                Private ReadOnly _writer As TextWriter
                Private _indentDepth As Integer

                Private _temporaryStringBuilders As Stack(Of TemporaryStringBuilder)

                Public Sub New(writer As TextWriter)
                    Me._writer = writer
                    Me._indentDepth = 0
                    Me._temporaryStringBuilders = Nothing
                End Sub

                Public ReadOnly Property IsSpecified As Boolean
                    Get
                        Return Me._writer IsNot Nothing
                    End Get
                End Property

                Public ReadOnly Property IndentDepth As Integer
                    Get
                        Return Me._indentDepth
                    End Get
                End Property

                Public Sub Indent()
                    ' NOTE: Dev11 does not seem to try pretty-indenting of the document tags
                    '       which is reasonable because we don't want to add extra indents in XML
                    'Me._indentDepth += 1
                End Sub

                Public Sub Unindent()
                    ' NOTE: Dev11 does not seem to try pretty-indenting of the document tags
                    '       which is reasonable because we don't want to add extra indents in XML
                    'Me._indentDepth -= 1
                    Debug.Assert(Me._indentDepth >= 0)
                End Sub

                Public Sub WriteLine(message As String)
                    If IsSpecified Then
                        If Me._temporaryStringBuilders IsNot Nothing AndAlso Me._temporaryStringBuilders.Count > 0 Then
                            Dim builder As StringBuilder = Me._temporaryStringBuilders.Peek().Pooled.Builder
                            builder.Append(MakeIndent(Me._indentDepth))
                            builder.AppendLine(message)

                        ElseIf Me._writer IsNot Nothing Then
                            Me._writer.Write(MakeIndent(Me._indentDepth))
                            Me._writer.WriteLine(message)
                        End If
                    End If
                End Sub

                Public Sub Write(message As String)
                    If IsSpecified Then
                        If Me._temporaryStringBuilders IsNot Nothing AndAlso Me._temporaryStringBuilders.Count > 0 Then
                            Dim builder As StringBuilder = Me._temporaryStringBuilders.Peek().Pooled.Builder
                            builder.Append(MakeIndent(Me._indentDepth))
                            builder.Append(message)

                        ElseIf Me._writer IsNot Nothing Then
                            Me._writer.Write(MakeIndent(Me._indentDepth))
                            Me._writer.Write(message)
                        End If
                    End If
                End Sub

                Public Sub WriteSubString(message As String, start As Integer, length As Integer, Optional appendNewLine As Boolean = True)
                    If Me._temporaryStringBuilders IsNot Nothing AndAlso Me._temporaryStringBuilders.Count > 0 Then
                        Dim builder As StringBuilder = Me._temporaryStringBuilders.Peek().Pooled.Builder
                        builder.Append(MakeIndent(IndentDepth))
                        builder.Append(message, start, length)
                        If appendNewLine Then
                            builder.AppendLine()
                        End If

                    ElseIf Me._writer IsNot Nothing Then
                        Me._writer.Write(MakeIndent(IndentDepth))
                        For i = 0 To length - 1
                            Me._writer.Write(message(start + i))
                        Next
                        If appendNewLine Then
                            Me._writer.WriteLine()
                        End If
                    End If
                End Sub

                Public Function GetAndEndTemporaryString() As String
                    Dim t As TemporaryStringBuilder = Me._temporaryStringBuilders.Pop()
                    Debug.Assert(Me._indentDepth = t.InitialIndentDepth,
                                 String.Format("Temporary strings should be indent-neutral (was {0}, is {1})",
                                               t.InitialIndentDepth,
                                               Me._indentDepth))

                    Me._indentDepth = t.InitialIndentDepth
                    Return t.Pooled.ToStringAndFree()
                End Function

                Private Shared Function MakeIndent(depth As Integer) As String
                    Debug.Assert(depth >= 0)

                    ' Since we know a lot about the structure of the output, we should 
                    ' be able to do this without constructing any new string objects.
                    Select Case depth
                        Case 0 : Return ""
                        Case 1 : Return "    "
                        Case 2 : Return "        "
                        Case 3 : Return "            "

                        Case Else
                            Debug.Assert(False, "Didn't expect nesting to reach depth " & depth)
                            Return New String(" "c, depth * 4)
                    End Select
                End Function

                Public Sub BeginTemporaryString()
                    If Me._temporaryStringBuilders Is Nothing Then
                        Me._temporaryStringBuilders = New Stack(Of TemporaryStringBuilder)()
                    End If

                    Me._temporaryStringBuilders.Push(New TemporaryStringBuilder(Me._indentDepth))
                End Sub

                Private Structure TemporaryStringBuilder
                    Public ReadOnly Pooled As PooledStringBuilder
                    Public ReadOnly InitialIndentDepth As Integer

                    Public Sub New(indentDepth As Integer)
                        Me.InitialIndentDepth = indentDepth
                        Me.Pooled = PooledStringBuilder.GetInstance()
                    End Sub
                End Structure

            End Structure

        End Class
    End Class
End Namespace
