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
    Friend Class MultiLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of MultiLineIfBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub addHighlights(ifBlock As MultiLineIfBlockSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
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
        End Sub
    End Class
End Namespace
