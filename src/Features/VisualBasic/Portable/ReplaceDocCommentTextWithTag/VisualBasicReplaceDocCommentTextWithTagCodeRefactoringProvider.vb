' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag

Namespace Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag), [Shared]>
    Friend Class VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider
        Inherits AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
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
            Return SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(text)) OrElse SyntaxFacts.IsContextualKeyword(SyntaxFacts.GetContextualKeywordKind(text))
        End Function

        Protected Overrides Function ParseExpression(text As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(text)
        End Function
    End Class
End Namespace
