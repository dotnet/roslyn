' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
    Friend Module NavigationPointHelpers

        Public Function GetNavigationPoint(text As SourceText, indentSize As Integer, eventBlock As EventBlockSyntax) As VirtualTreePoint
            Dim line As Integer
            If eventBlock.EndEventStatement Is Nothing OrElse eventBlock.EndEventStatement.IsMissing Then
                line = text.Lines.GetLineFromPosition(GetHeaderStartPosition(eventBlock)).LineNumber
            Else
                line = text.Lines.GetLineFromPosition(eventBlock.EventStatement.Span.End).LineNumber + 1
            End If

            Return GetNavigationPoint(text, indentSize, eventBlock.EventStatement, line)
        End Function

        Public Function GetNavigationPoint(text As SourceText, indentSize As Integer, methodBlock As MethodBlockBaseSyntax) As VirtualTreePoint
            Dim line As Integer
            If methodBlock.EndBlockStatement Is Nothing OrElse methodBlock.EndBlockStatement.IsMissing Then
                line = text.Lines.GetLineFromPosition(GetHeaderStartPosition(methodBlock)).LineNumber
            Else
                line = text.Lines.GetLineFromPosition(methodBlock.BlockStatement.Span.End).LineNumber + 1
            End If

            Return GetNavigationPoint(text, indentSize, methodBlock.BlockStatement, line)
        End Function

        Public Function GetHeaderStartPosition(eventBlock As EventBlockSyntax) As Integer
            If eventBlock.EventStatement.AttributeLists.Count > 0 Then
                Return eventBlock.EventStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
            Else
                Return eventBlock.EventStatement.SpanStart
            End If
        End Function

        Public Function GetHeaderStartPosition(methodBlock As MethodBlockBaseSyntax) As Integer
            If methodBlock.BlockStatement.Modifiers.Count > 0 Then
                Return methodBlock.BlockStatement.Modifiers.First().SpanStart
            Else
                Return methodBlock.BlockStatement.DeclarationKeyword.SpanStart
            End If
        End Function

        ' TODO: this function conflates tab size and indent size.
        Public Function GetNavigationPoint(text As SourceText, indentSize As Integer, beginStatement As StatementSyntax, lineNumber As Integer) As VirtualTreePoint
            Dim line = text.Lines(lineNumber)
            Dim nonWhitespaceOffset = line.GetFirstNonWhitespacePosition()

            If nonWhitespaceOffset.HasValue Then
                ' Simply go to the start of the line
                Return New VirtualTreePoint(beginStatement.SyntaxTree, text, nonWhitespaceOffset.Value)
            Else
                ' We have whitespace only. Compute the indent. We subtract 1 since the CompilationUnitSyntax doesn't count.
                Dim indents = beginStatement.Ancestors().Count() - 1

                ' Compute the total column size of the current line
                Dim totalLineSize = line.GetColumnFromLineOffset(line.Span.Length, indentSize)
                Dim targetColumn As Integer = indents * indentSize

                ' If we need to go past the end, then we'll be in virtual space
                If totalLineSize < targetColumn Then
                    Return New VirtualTreePoint(beginStatement.SyntaxTree, text, line.End, targetColumn - totalLineSize)
                Else
                    Return New VirtualTreePoint(beginStatement.SyntaxTree, text, line.GetLineOffsetFromColumn(targetColumn, indentSize) + line.Start)
                End If
            End If
        End Function
    End Module
End Namespace
