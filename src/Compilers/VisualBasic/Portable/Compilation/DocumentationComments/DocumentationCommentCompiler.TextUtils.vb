' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports System.Resources
Imports System.Runtime.InteropServices
Imports System.Xml
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Private Sub Indent()
                Me._writer.Indent()
            End Sub

            Private Sub Unindent()
                Me._writer.Unindent()
            End Sub

            Private Sub WriteLine(message As String)
                Me._writer.WriteLine(message)
            End Sub

            Private Sub Write(message As String)
                Me._writer.Write(message)
            End Sub

            ''' <summary>
            ''' Given the full text of a documentation comment, strip off the comment 
            ''' punctuation (''') and add appropriate indentations.
            ''' </summary>
            Private Function FormatComment(substitutedText As String) As String
                Me._writer.BeginTemporaryString()
                WriteFormattedComment(substitutedText)
                Return Me._writer.GetAndEndTemporaryString()
            End Function

            ''' <summary>
            ''' Find the first non-whitespace character in a given substring.
            ''' </summary>
            ''' <param name="str">The string to search</param>
            ''' <param name="start">The start index</param>
            ''' <param name="end">The last index (non-inclusive)</param>
            ''' <returns>The index of the first non-whitespace char after index 
            ''' start in the string up to, but not including the end index</returns>
            Private Shared Function GetIndexOfFirstNonWhitespaceChar(str As String, start As Integer, [end] As Integer) As Integer
                Debug.Assert(start >= 0)
                Debug.Assert(start <= str.Length)
                Debug.Assert([end] >= 0)
                Debug.Assert([end] <= str.Length)
                Debug.Assert([end] >= start)

                While start < [end] And Char.IsWhiteSpace(str(start))
                    start += 1
                End While

                Return start
            End Function

            ''' <summary>
            ''' Given a string which may contain newline sequences, get the index of the first newline
            ''' sequence beginning at the given starting index.
            ''' </summary>
            ''' <param name="str">The string to split.</param>
            ''' <param name="start">The starting index within the string.</param>
            ''' <param name="newLineLength">The length of the newline sequence discovered. 0 if the end of the string was reached, otherwise either 1 or 2 chars</param>
            ''' <returns>The index of the start of the first newline sequence following the start index</returns>
            Private Shared Function IndexOfNewLine(str As String, start As Integer, <Out> ByRef newLineLength As Integer) As Integer
                Dim len As Integer = str.Length
                While start < len
                    Select Case str(start)
                        Case ChrW(13)
                            If (start + 1) < str.Length AndAlso str(start + 1) = ChrW(10) Then
                                newLineLength = 2
                            Else
                                newLineLength = 1
                            End If
                            Return start

                        Case ChrW(10)
                            newLineLength = 1
                            Return start
                    End Select
                    start += 1
                End While

                newLineLength = 0
                Return start
            End Function

            ''' <summary>
            ''' Given the full text of a single-line style documentation comment, for each line, strip off
            ''' the comment punctuation (''') and flatten the text.
            ''' </summary>
            Private Sub WriteFormattedComment(text As String)
                ' PERF: Avoid allocating intermediate strings e.g. via Split, Trim or Substring
                Dim substringStart As Integer = 3
                Dim start As Integer = 0
                Dim len As Integer = text.Length
                While start < len
                    Dim newLineLength As Integer = 0
                    Dim [end] As Integer = IndexOfNewLine(text, start, newLineLength)

                    Dim trimStart As Integer = GetIndexOfFirstNonWhitespaceChar(text, start, [end])
                    If trimStart < [end] AndAlso text(trimStart) = "'"c Then
                        trimStart += substringStart
                    End If

                    Me._writer.WriteSubString(text, trimStart, [end] - trimStart, appendNewLine:=True)
                    start = [end] + newLineLength
                End While
            End Sub

            ''' <remarks>
            ''' WORKAROUND:
            ''' We're taking a dependency on the location and structure of a framework assembly resource.  This is not a robust solution.
            ''' 
            ''' Possible alternatives:
            ''' 1) Polish our XML parser until it matches MSXML.  We don't want to reinvent the wheel.
            ''' 2) Build a map that lets us go from XML string positions back to source positions.  
            ''' This is what the native compiler did, and it was a lot of work.  We'd also still need to modify the message.
            ''' 3) Do not report a diagnostic.  This is very unhelpful.
            ''' 4) Report a vague diagnostic (i.e. there's a problem somewhere in this doc comment).  This is relatively unhelpful.
            ''' 5) Always report the message in English, so that we can pull it apart without needing to mess with resource files.
            ''' This engenders a lot of ill will.
            ''' 6) Report the exception message without modification and (optionally) include the text with respect to which the
            ''' position is specified.  This looks amateurish.            
            ''' </remarks>
            Private Shared Function GetDescription(e As XmlException) As String
                Dim message As String = e.Message
                Try
                    Dim manager As New ResourceManager("System.Xml", GetType(XmlException).GetTypeInfo().Assembly)
                    Dim locationTemplate As String = manager.GetString("Xml_MessageWithErrorPosition")
                    Dim locationString As String =
                        String.Format(locationTemplate, "", e.LineNumber, e.LinePosition) ' first arg is where the problem description goes

                    Dim position As Integer = message.IndexOf(locationString, StringComparison.Ordinal) ' Expect exact match
                    Return If(position < 0, message, message.Remove(position, locationString.Length))
                Catch ex As Exception
                    Debug.Assert(False, "If we hit this, then we might need to think about a different workaround " +
                                        "for stripping the location out of the message.")

                    ' If anything at all goes wrong, just return the message verbatim.  It probably
                    ' contains an invalid position, but it's better than nothing.
                    Return message
                End Try
            End Function

        End Class

    End Class
End Namespace
