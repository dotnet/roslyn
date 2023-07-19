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
    Friend Class PropertyDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of PropertyStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AddHighlights(propertyDeclaration As PropertyStatementSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            ' If the ancestor is not a property block, treat this as an auto-property.
            ' Otherwise, let the PropertyBlockHighlighter take over.
            Dim propertyBlock = propertyDeclaration.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock IsNot Nothing Then
                Return
            End If

            With propertyDeclaration
                Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                If .ImplementsClause IsNot Nothing Then
                    highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                End If
            End With
        End Sub
    End Class
End Namespace
