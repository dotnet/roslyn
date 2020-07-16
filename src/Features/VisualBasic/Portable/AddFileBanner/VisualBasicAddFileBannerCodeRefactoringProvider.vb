' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddFileBanner
Imports Microsoft.CodeAnalysis.CodeRefactorings

Namespace Microsoft.CodeAnalysis.VisualBasic.AddFileBanner
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.AddFileBanner), [Shared]>
    Friend Class VisualBasicAddFileBannerCodeRefactoringProvider
        Inherits AbstractAddFileBannerCodeRefactoringProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

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
