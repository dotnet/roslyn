﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class XmlDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlDeclarationSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub addHighlights(xmlDocumentPrologue As XmlDeclarationSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            With xmlDocumentPrologue
                If Not .ContainsDiagnostics AndAlso
                   Not .HasAncestor(Of DocumentationCommentTriviaSyntax)() Then
                    highlights.Add(.LessThanQuestionToken.Span)
                    highlights.Add(.QuestionGreaterThanToken.Span)
                End If
            End With
        End Sub
    End Class
End Namespace
