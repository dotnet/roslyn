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
    Friend Class EventDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of EventStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AddHighlights(eventDeclaration As EventStatementSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            ' If the ancestor is not a event block, treat this as a single line event.
            ' Otherwise, let the EventBlockHighlighter take over.
            Dim eventBlock = eventDeclaration.GetAncestor(Of EventBlockSyntax)()
            If eventBlock IsNot Nothing Then
                Return
            End If

            With eventDeclaration
                Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                If .ImplementsClause IsNot Nothing Then
                    highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                End If
            End With
        End Sub
    End Class
End Namespace
