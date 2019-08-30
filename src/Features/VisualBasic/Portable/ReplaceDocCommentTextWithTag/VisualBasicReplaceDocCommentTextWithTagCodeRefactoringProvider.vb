' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag

Namespace Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag), [Shared]>
    Friend Class VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider
        Inherits AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider

        Private Shared ReadOnly s_keywords As New HashSet(Of String) From
            {
            SyntaxFacts.GetText(SyntaxKind.NothingKeyword),
            SyntaxFacts.GetText(SyntaxKind.SharedKeyword),
            SyntaxFacts.GetText(SyntaxKind.OverridableKeyword),
            SyntaxFacts.GetText(SyntaxKind.TrueKeyword),
            SyntaxFacts.GetText(SyntaxKind.FalseKeyword),
            SyntaxFacts.GetText(SyntaxKind.MustInheritKeyword),
            SyntaxFacts.GetText(SyntaxKind.NotOverridableKeyword),
            SyntaxFacts.GetText(SyntaxKind.AsyncKeyword),
            SyntaxFacts.GetText(SyntaxKind.AwaitKeyword)
            }

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function IsXmlTextToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.XmlTextLiteralToken OrElse
                   token.Kind() = SyntaxKind.DocumentationCommentLineBreakToken
        End Function

        Protected Overrides Function IsInXMLAttribute(token As SyntaxToken) As Boolean
            Return token.Parent.IsKind(SyntaxKind.XmlAttribute) Or token.Parent.IsKind(SyntaxKind.XmlString)
        End Function

        Protected Overrides Function IsKeyword(text As String) As Boolean
            Return s_keywords.Contains(text)
        End Function

        Protected Overrides Function ParseExpression(text As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(text)
        End Function

    End Class
End Namespace
