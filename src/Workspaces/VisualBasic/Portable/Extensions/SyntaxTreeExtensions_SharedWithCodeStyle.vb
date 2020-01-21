' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTreeExtensions
        ''' <summary>
        ''' check whether given token is the last token of a statement that ends with end of line trivia or an elastic trivia
        ''' </summary>
        <Extension()>
        Public Function IsLastTokenOfStatementWithEndOfLine(token As SyntaxToken) As Boolean
            If Not token.HasTrailingTrivia Then
                Return False
            End If

            ' easy case
            Dim trailing = token.TrailingTrivia
            If trailing.Count = 1 Then
                Dim trivia = trailing.First()

                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    Return token.IsLastTokenOfStatement()
                End If

                Return False
            End If

            ' little bit more expansive case
            For Each trivia In trailing
                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    Return token.IsLastTokenOfStatement()
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' check whether given token is the last token of a statement by walking up the spine
        ''' </summary>
        <Extension()>
        Public Function IsLastTokenOfStatement(token As SyntaxToken, Optional checkColonTrivia As Boolean = False) As Boolean
            Dim current = token.Parent
            While current IsNot Nothing
                If current.FullSpan.End <> token.FullSpan.End Then
                    Return False
                End If

                If TypeOf current Is StatementSyntax Then
                    Dim colonTrivia = GetTrailingColonTrivia(DirectCast(current, StatementSyntax))
                    If Not PartOfSingleLineLambda(current) AndAlso Not PartOfMultilineLambdaFooter(current) Then
                        If checkColonTrivia Then
                            If colonTrivia Is Nothing Then
                                Return current.GetLastToken(includeZeroWidth:=True) = token
                            End If
                        Else
                            Return current.GetLastToken(includeZeroWidth:=True) = token
                        End If
                    End If
                End If

                current = current.Parent
            End While

            Return False
        End Function

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowImplicitBoxing:=False)>
        Private Function GetTrailingColonTrivia(statement As StatementSyntax) As SyntaxTrivia?
            If Not statement.HasTrailingTrivia Then
                Return Nothing
            End If

            Return statement _
                    .GetTrailingTrivia() _
                    .FirstOrNull(Function(t) t.Kind = SyntaxKind.ColonTrivia)
        End Function

        Private Function PartOfSingleLineLambda(node As SyntaxNode) As Boolean
            While node IsNot Nothing
                If TypeOf node Is MultiLineLambdaExpressionSyntax Then Return False
                If TypeOf node Is SingleLineLambdaExpressionSyntax Then Return True
                node = node.Parent
            End While
            Return False
        End Function

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowCaptures:=False)>
        Private Function PartOfMultilineLambdaFooter(node As SyntaxNode) As Boolean
            For Each n In node.AncestorsAndSelf
                Dim multiLine = TryCast(n, MultiLineLambdaExpressionSyntax)
                If multiLine Is Nothing Then
                    Continue For
                End If

                If (multiLine.EndSubOrFunctionStatement Is node) Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Module
End Namespace
