' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class CommandLineDiagnosticFormatter
        Inherits VisualBasicDiagnosticFormatter

        Private ReadOnly _baseDirectory As String
        Private ReadOnly _getAdditionalTextFiles As Func(Of ImmutableArray(Of AdditionalTextFile))

        Friend Sub New(baseDirectory As String, getAdditionalTextFiles As Func(Of ImmutableArray(Of AdditionalTextFile)))
            _baseDirectory = baseDirectory
            _getAdditionalTextFiles = getAdditionalTextFiles
        End Sub

        ' Returns a diagnostic message in string.
        ' VB has a special implementation that prints out a squiggle under the error span as well as a diagnostic message.
        ' e.g.,
        '   c:\Roslyn\Temp\a.vb(5) : warning BC42024: Unused local variable: 'x'.
        '
        '       Dim x As Integer
        '           ~
        '
        Public Overrides Function Format(diagnostic As Diagnostic, Optional formatter As IFormatProvider = Nothing) As String
            ' Builds a diagnostic message
            ' Dev12 vbc prints raw paths -- relative and not normalized, so we don't need to customize the base implementation.

            Dim text As SourceText = Nothing
            Dim diagnosticSpanOpt = GetDiagnosticSpanAndFileText(diagnostic, text)

            If Not diagnosticSpanOpt.HasValue OrElse
                text Is Nothing OrElse
                text.Length < diagnosticSpanOpt.Value.End Then
                ' Diagnostic either has Location.None OR an invalid location OR invalid file contents.
                ' For all these cases, format the diagnostic as a no-location project level diagnostic.

                ' Strip location, if required.
                If diagnostic.Location <> Location.None Then
                    diagnostic = diagnostic.WithLocation(Location.None)
                End If

                ' Add "vbc : " command line prefix to the start of the command line diagnostics which do not have a location to match the 
                ' behavior of native compiler.    This allows MSBuild output to be consistent whether Roslyn is installed or not.      
                Return VisualBasicCompiler.VbcCommandLinePrefix &
                    MyBase.Format(diagnostic, formatter)
            End If

            Dim baseMessage = MyBase.Format(diagnostic, formatter)

            Dim sb As New StringBuilder()
            sb.AppendLine(baseMessage)

            ' the squiggles are displayed for the original (unmapped) location
            Dim sourceSpan = diagnosticSpanOpt.Value
            Dim sourceSpanStart = sourceSpan.Start
            Dim sourceSpanEnd = sourceSpan.End
            Dim linenumber = text.Lines.IndexOf(sourceSpanStart)
            Dim line = text.Lines(linenumber)

            If sourceSpan.IsEmpty AndAlso line.Start = sourceSpanEnd AndAlso linenumber > 0 Then
                ' Sometimes there is something missing at the end of the line, then the error is reported with an empty span
                ' beyond the end of the line, which makes it appear that the span starts at the beginning of the next line.
                ' Let's go back to the previous line in this case.
                linenumber -= 1
                line = text.Lines(linenumber)
            End If

            While (line.Start < sourceSpanEnd)
                ' Builds the original text line
                sb.AppendLine()
                sb.AppendLine(line.ToString().Replace(vbTab, "    ")) ' normalize tabs with 4 spaces

                ' Builds leading spaces up to the error span
                For position = Math.Min(sourceSpanStart, line.Start) To Math.Min(line.End, sourceSpanStart) - 1
                    If (text(position) = vbTab) Then
                        ' normalize tabs with 4 spaces
                        sb.Append(" "c, 4)
                    Else
                        sb.Append(" ")
                    End If
                Next

                ' Builds squiggles
                If sourceSpan.IsEmpty Then
                    sb.Append("~")
                Else
                    For position = Math.Max(sourceSpanStart, line.Start) To Math.Min(If(sourceSpanEnd = sourceSpanStart, sourceSpanEnd, sourceSpanEnd - 1), line.End - 1)
                        If (text(position) = vbTab) Then
                            ' normalize tabs with 4 spaces
                            sb.Append("~"c, 4)
                        Else
                            sb.Append("~")
                        End If
                    Next
                End If

                ' Builds trailing spaces up to the end of this line
                For position = Math.Min(sourceSpanEnd, line.End) To line.End - 1
                    If (text(position) = vbTab) Then
                        ' normalize tabs with 4 spaces
                        sb.Append(" "c, 4)
                    Else
                        sb.Append(" ")
                    End If
                Next

                ' The error span can continue over multiple lines
                linenumber = linenumber + 1
                If linenumber >= text.Lines.Count Then
                    ' Exit the loop when we reach the end line (0-based)
                    Exit While
                End If

                line = text.Lines(linenumber)
            End While

            Return sb.ToString()
        End Function

        Friend Overrides Function FormatSourcePath(path As String, basePath As String, formatter As IFormatProvider) As String
            Return If(FileUtilities.NormalizeRelativePath(path, basePath, _baseDirectory), path)
        End Function

        Private Function GetDiagnosticSpanAndFileText(diagnostic As Diagnostic, <Out> ByRef text As SourceText) As TextSpan?
            If diagnostic.Location.IsInSource Then
                text = diagnostic.Location.SourceTree.GetText()
                Return diagnostic.Location.SourceSpan
            End If

            If diagnostic.Location.Kind = LocationKind.ExternalFile Then
                Dim path = diagnostic.Location.GetLineSpan().Path
                If path IsNot Nothing Then
                    For Each additionalTextFile In _getAdditionalTextFiles()
                        If path.Equals(additionalTextFile.Path) Then
                            Try
                                text = additionalTextFile.GetText()
                            Catch
                                text = Nothing
                            End Try

                            Return diagnostic.Location.SourceSpan
                        End If
                    Next
                End If
            End If

            text = Nothing
            Return Nothing
        End Function
    End Class
End Namespace
