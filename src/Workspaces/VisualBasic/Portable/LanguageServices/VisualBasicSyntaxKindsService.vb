' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    End Class
End Namespace
