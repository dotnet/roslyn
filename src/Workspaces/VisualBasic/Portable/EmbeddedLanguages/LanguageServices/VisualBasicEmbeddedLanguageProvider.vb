' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
    <ExportLanguageService(GetType(IEmbeddedLanguageProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageProvider
        Inherits AbstractEmbeddedLanguageProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Private Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken,
                       VisualBasicSyntaxFacts.Instance,
                       VisualBasicSemanticFactsService.Instance,
                       VisualBasicVirtualCharService.Instance)
        End Sub

        Friend Overrides Sub AddComment(editor As SyntaxEditor, stringLiteral As SyntaxToken, commentContents As String)
            Dim trivia = SyntaxFactory.TriviaList(
                SyntaxFactory.CommentTrivia($"' {commentContents}"),
                SyntaxFactory.ElasticCarriageReturnLineFeed)

            Dim containingStatement = stringLiteral.Parent.GetAncestor(Of StatementSyntax)

            Dim leadingBlankLines = containingStatement.GetLeadingBlankLines()

            Dim newStatement = containingStatement.GetNodeWithoutLeadingBlankLines().
                                                   WithPrependedLeadingTrivia(leadingBlankLines.AddRange(trivia))

            editor.ReplaceNode(containingStatement, newStatement)
        End Sub
    End Class
End Namespace
