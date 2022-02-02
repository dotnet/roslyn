' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic), [Shared]>
    Friend Class MethodDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim methodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock Is Nothing OrElse Not TypeOf methodBlock.BlockStatement Is MethodStatementSyntax Then
                Return
            End If

            With methodBlock
                Dim isAsync = False
                Dim isIterator = False

                With DirectCast(.BlockStatement, MethodStatementSyntax)
                    isAsync = .Modifiers.Any(SyntaxKind.AsyncKeyword)
                    isIterator = .Modifiers.Any(SyntaxKind.IteratorKeyword)

                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                    If .HandlesClause IsNot Nothing Then
                        highlights.Add(.HandlesClause.HandlesKeyword.Span)
                    End If

                    If .ImplementsClause IsNot Nothing Then
                        highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                    End If
                End With

                highlights.AddRange(
                    .GetRelatedStatementHighlights(
                        blockKind:= .BlockStatement.DeclarationKeyword.Kind,
                        checkReturns:=True))

                If isIterator Then
                    highlights.AddRange(.GetRelatedYieldStatementHighlights())
                End If

                If isAsync Then
                    HighlightRelatedAwaits(methodBlock, highlights, cancellationToken)
                End If

                highlights.Add(.EndBlockStatement.Span)
            End With
        End Sub
    End Class
End Namespace
