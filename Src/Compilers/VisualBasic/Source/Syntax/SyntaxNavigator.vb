' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        Private Shared ReadOnly CommonSyntaxTriviaSkipped As Func(Of SyntaxTrivia, Boolean) =
            Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia

        Private ReadOnly StepIntoFunctions As Func(Of SyntaxTrivia, Boolean)() = New Func(Of SyntaxTrivia, Boolean)() {
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
            Return StepIntoFunctions(index)
        End Function

        Public Shared Function ToCommon(func As Func(Of SyntaxTrivia, Boolean)) As Func(Of SyntaxTrivia, Boolean)
            If func Is SyntaxTrivia.Any Then
                Return SyntaxTrivia.Any
            End If

            If func Is SyntaxTriviaFunctions.Skipped Then
                Return CommonSyntaxTriviaSkipped
            End If

            If func Is Nothing Then
                Return Nothing
            End If

            Return Function(t) func(CType(t, SyntaxTrivia))
        End Function

        Public Shared Function ToCommon(func As Func(Of SyntaxToken, Boolean)) As Func(Of SyntaxToken, Boolean)
            If func Is SyntaxToken.Any Then
                Return SyntaxToken.Any
            End If

            If func Is SyntaxToken.NonZeroWidth Then
                Return SyntaxToken.NonZeroWidth
            End If

            If func Is Nothing Then
                Return Nothing
            End If

            Return Function(t) func(CType(t, SyntaxToken))
        End Function
    End Class
End Namespace