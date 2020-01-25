﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend NotInheritable Class VisualBasicSyntaxKindsService
        Inherits AbstractSyntaxKindsService

        Public Shared ReadOnly Instance As New VisualBasicSyntaxKindsService()

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property DotToken As Integer = SyntaxKind.DotToken
        Public Overrides ReadOnly Property QuestionToken As Integer = SyntaxKind.QuestionToken

        Public Overrides ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword

        Public Overrides ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression
        Public Overrides ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression
        Public Overrides ReadOnly Property EndOfFileToken As Integer = SyntaxKind.EndOfFileToken
        Public Overrides ReadOnly Property AwaitKeyword As Integer = SyntaxKind.AwaitKeyword

        Public Overrides ReadOnly Property IdentifierToken As Integer = SyntaxKind.IdentifierToken
        Public Overrides ReadOnly Property GlobalKeyword As Integer = SyntaxKind.GlobalKeyword
        Public Overrides ReadOnly Property IncompleteMember As Integer = SyntaxKind.IncompleteMember
        Public Overrides ReadOnly Property UsingStatement As Integer = SyntaxKind.UsingStatement
        Public Overrides ReadOnly Property ReturnStatement As Integer = SyntaxKind.ReturnStatement
        Public Overrides ReadOnly Property HashToken As Integer = SyntaxKind.HashToken

        Public Overrides ReadOnly Property ExpressionStatement As Integer = SyntaxKind.ExpressionStatement
    End Class
End Namespace
