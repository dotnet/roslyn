' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
    Friend Module BreakpointGetter
        Friend Async Function GetBreakpointAsync(document As Document, position As Integer, length As Integer, cancellationToken As CancellationToken) As Task(Of BreakpointResolutionResult)
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)

            ' Non-zero length means that the span is passed by the debugger and we may need validate it.
            ' In a rare VB case, this span may contain multiple methods, e.g., 
            '
            '    [Sub Foo() Handles A
            '
            '     End Sub
            '
            '     Sub Bar() Handles B]
            '
            '     End Sub
            '
            ' It happens when IDE services (e.g., NavBar or CodeFix) inserts a new method at the beginning of the existing one
            ' which happens to have a breakpoint on its head. In this situation, we should attempt to validate the span of the existing method,
            ' not that of a newly-prepended method.

            Dim descendIntoChildren As Func(Of SyntaxNode, Boolean) =
                Function(n)
                    Return (Not n.IsKind(SyntaxKind.ConstructorBlock)) _
                        AndAlso (Not n.IsKind(SyntaxKind.SubBlock))
                End Function

            If length > 0 Then
                Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                Dim item = root.DescendantNodes(New TextSpan(position, length), descendIntoChildren:=descendIntoChildren).OfType(Of MethodBlockSyntax).Skip(1).LastOrDefault()
                If item IsNot Nothing Then
                    position = item.SpanStart
                End If
            End If

            Dim span As TextSpan
            If Not TryGetBreakpointSpan(tree, position, cancellationToken, span) Then
                Return Nothing
            End If

            If span.Length = 0 Then
                Return BreakpointResolutionResult.CreateLineResult(document)
            End If

            Return BreakpointResolutionResult.CreateSpanResult(document, span)
        End Function

        Friend Function TryGetBreakpointSpan(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken, <Out> ByRef breakpointSpan As TextSpan) As Boolean
            Dim source = tree.GetText(cancellationToken)

            ' If the line is entirely whitespace, then don't set any breakpoint there.
            Dim line = source.Lines.GetLineFromPosition(position)
            If IsBlank(line) Then
                breakpointSpan = Nothing
                Return False
            End If

            ' If the user is asking for breakpoint in an inactive region, then just create a line
            ' breakpoint there.
            If tree.IsInInactiveRegion(position, cancellationToken) Then
                breakpointSpan = Nothing
                Return True
            End If

            Dim root = tree.GetRoot(cancellationToken)
            Return root.TryGetEnclosingBreakpointSpan(position, breakpointSpan)
        End Function

        Private Function IsBlank(line As TextLine) As Boolean
            Dim text = line.ToString()

            For i = 0 To text.Length - 1
                If Not SyntaxFacts.IsWhitespace(text(i)) Then
                    Return False
                End If
            Next

            Return True
        End Function
    End Module
End Namespace
