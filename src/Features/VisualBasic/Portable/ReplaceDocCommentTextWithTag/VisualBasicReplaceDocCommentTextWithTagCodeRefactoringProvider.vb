' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag

Namespace Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag), [Shared]>
    Friend Class VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider
        Inherits AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider

        Protected Overrides Function IsXmlTextToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.XmlTextLiteralToken OrElse
                   token.Kind() = SyntaxKind.DocumentationCommentLineBreakToken
        End Function

        Protected Overrides Function IsAnyKeyword(text As String) As Boolean
            Return SyntaxFacts.GetKeywordKind(text) <> SyntaxKind.None OrElse
                   SyntaxFacts.GetContextualKeywordKind(text) <> SyntaxKind.None
        End Function

        Protected Overrides Function ParseExpression(text As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(text)
        End Function
    End Class
End Namespace
