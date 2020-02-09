' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend NotInheritable Class VisualBasicSyntaxKindsService
        Inherits AbstractSyntaxKindsService

        Public Shared ReadOnly Instance As New VisualBasicSyntaxKindsService()

        Private Sub New()
        End Sub

        Public Overrides Function Convert(Of TSyntaxKind As Structure)(kind As Integer) As TSyntaxKind
            ' Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
            Return CType(CType(CType(kind, SyntaxKind), Object), TSyntaxKind)
        End Function

        Public Overrides ReadOnly Property ConflictMarkerTrivia As Integer = SyntaxKind.ConflictMarkerTrivia
        Public Overrides ReadOnly Property DisabledTextTrivia As Integer = SyntaxKind.DisabledTextTrivia
        Public Overrides ReadOnly Property EndOfLineTrivia As Integer = SyntaxKind.EndOfLineTrivia
        Public Overrides ReadOnly Property SkippedTokensTrivia As Integer = SyntaxKind.SkippedTokensTrivia

        Public Overrides ReadOnly Property DotToken As Integer = SyntaxKind.DotToken
        Public Overrides ReadOnly Property QuestionToken As Integer = SyntaxKind.QuestionToken

        Public Overrides ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword

        Public Overrides ReadOnly Property GenericName As Integer = SyntaxKind.GenericName
        Public Overrides ReadOnly Property QualifiedName As Integer = SyntaxKind.QualifiedName

        Public Overrides ReadOnly Property AnonymousObjectCreationExpression As Integer = SyntaxKind.AnonymousObjectCreationExpression
        Public Overrides ReadOnly Property ConditionalAccessExpression As Integer = SyntaxKind.ConditionalAccessExpression
        Public Overrides ReadOnly Property InvocationExpression As Integer = SyntaxKind.InvocationExpression
        Public Overrides ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression
        Public Overrides ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression
        Public Overrides ReadOnly Property ObjectCreationExpression As Integer = SyntaxKind.ObjectCreationExpression
        Public Overrides ReadOnly Property ParenthesizedExpression As Integer = SyntaxKind.ParenthesizedExpression
        Public Overrides ReadOnly Property QueryExpression As Integer = SyntaxKind.QueryExpression
        Public Overrides ReadOnly Property ReferenceEqualsExpression As Integer = SyntaxKind.IsExpression
        Public Overrides ReadOnly Property ReferenceNotEqualsExpression As Integer = SyntaxKind.IsNotExpression
        Public Overrides ReadOnly Property SimpleMemberAccessExpression As Integer = SyntaxKind.SimpleMemberAccessExpression
        Public Overrides ReadOnly Property TernaryConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression

        Public Overrides ReadOnly Property EndOfFileToken As Integer = SyntaxKind.EndOfFileToken
        Public Overrides ReadOnly Property AwaitKeyword As Integer = SyntaxKind.AwaitKeyword

        Public Overrides ReadOnly Property IdentifierToken As Integer = SyntaxKind.IdentifierToken
        Public Overrides ReadOnly Property GlobalKeyword As Integer = SyntaxKind.GlobalKeyword
        Public Overrides ReadOnly Property IncompleteMember As Integer = SyntaxKind.IncompleteMember
        Public Overrides ReadOnly Property HashToken As Integer = SyntaxKind.HashToken

        Public Overrides ReadOnly Property ExpressionStatement As Integer = SyntaxKind.ExpressionStatement
        Public Overrides ReadOnly Property LockStatement As Integer = SyntaxKind.SyncLockStatement
        Public Overrides ReadOnly Property ReturnStatement As Integer = SyntaxKind.ReturnStatement
        Public Overrides ReadOnly Property UsingStatement As Integer = SyntaxKind.UsingStatement

        Public Overrides ReadOnly Property Parameter As Integer = SyntaxKind.Parameter
        Public Overrides ReadOnly Property TypeConstraint As Integer = SyntaxKind.TypeConstraint
        Public Overrides ReadOnly Property VariableDeclarator As Integer = SyntaxKind.VariableDeclarator

        Public Overrides ReadOnly Property TypeArgumentList As Integer = SyntaxKind.TypeArgumentList
    End Class
End Namespace
