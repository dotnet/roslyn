' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class SyntaxNavigator
        Inherits AbstractSyntaxNavigator

        <Flags()>
        Private Enum SyntaxKinds
            DocComments = 1
            Directives = 2
            SkippedTokens = 4
        End Enum

        Public Shared ReadOnly Instance As AbstractSyntaxNavigator = New SyntaxNavigator()

        Private ReadOnly _stepIntoFunctions As Func(Of SyntaxTrivia, Boolean)() = New Func(Of SyntaxTrivia, Boolean)() {
            Nothing,
            Function(t) t.RawKind = SyntaxKind.DocumentationCommentTrivia,
            Function(t) t.IsDirective,
            Function(t) t.IsDirective OrElse t.RawKind = SyntaxKind.DocumentationCommentTrivia,
            Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia,
            Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia OrElse t.RawKind = SyntaxKind.DocumentationCommentTrivia,
            Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia OrElse t.IsDirective,
            Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia OrElse t.IsDirective OrElse t.RawKind = SyntaxKind.DocumentationCommentTrivia
        }

        Protected Overrides Function GetStepIntoFunction(skipped As Boolean, directives As Boolean, docComments As Boolean) As Func(Of SyntaxTrivia, Boolean)
            Dim index = If(skipped, SyntaxKinds.SkippedTokens, 0) Or If(directives, SyntaxKinds.Directives, 0) Or If(docComments, SyntaxKinds.DocComments, 0)
            Return _stepIntoFunctions(index)
        End Function

    End Class
End Namespace
