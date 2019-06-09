' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MultiLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of MultiLineIfBlockSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(ifBlock As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim highlights As New List(Of TextSpan)

            With ifBlock.IfStatement
                ' ElseIf case
                highlights.Add(.IfKeyword.Span)

                If .ThenKeyword.Kind <> SyntaxKind.None Then
                    highlights.Add(.ThenKeyword.Span)
                End If
            End With

            Dim highlightElseIfPart = Sub(elseIfBlock As ElseIfBlockSyntax)
                                          With elseIfBlock.ElseIfStatement
                                              ' ElseIf case
                                              highlights.Add(.ElseIfKeyword.Span)

                                              If .ThenKeyword.Kind <> SyntaxKind.None Then
                                                  highlights.Add(.ThenKeyword.Span)
                                              End If
                                          End With
                                      End Sub

            For Each elseIfBlock In ifBlock.ElseIfBlocks
                highlightElseIfPart(elseIfBlock)
            Next

            If ifBlock.ElseBlock IsNot Nothing Then
                highlights.Add(ifBlock.ElseBlock.ElseStatement.ElseKeyword.Span)
            End If

            highlights.Add(ifBlock.EndIfStatement.Span)

            Return highlights
        End Function
    End Class
End Namespace
