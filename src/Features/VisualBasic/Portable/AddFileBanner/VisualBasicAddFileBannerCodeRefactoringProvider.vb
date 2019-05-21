' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddFileBanner
Imports Microsoft.CodeAnalysis.CodeRefactorings

Namespace Microsoft.CodeAnalysis.VisualBasic.AddFileBanner
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.AddFileBanner), [Shared]>
    Friend Class VisualBasicAddFileBannerCodeRefactoringProvider
        Inherits AbstractAddFileBannerCodeRefactoringProvider

        Protected Overrides Function IsCommentStartCharacter(ch As Char) As Boolean
            Return ch = "'"c
        End Function

        Protected Overrides Function CreateTrivia(trivia As SyntaxTrivia, text As String) As SyntaxTrivia
            Return If(trivia.Kind() = SyntaxKind.CommentTrivia OrElse trivia.Kind() = SyntaxKind.DocumentationCommentTrivia,
                      SyntaxFactory.CommentTrivia(text),
                      trivia)
        End Function
    End Class
End Namespace
