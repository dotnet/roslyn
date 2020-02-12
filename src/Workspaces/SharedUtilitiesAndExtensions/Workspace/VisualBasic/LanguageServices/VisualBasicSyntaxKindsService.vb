' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend NotInheritable Class VisualBasicSyntaxKindsService
        Implements ISyntaxKindsService

        Public Shared ReadOnly Instance As New VisualBasicSyntaxKindsService()

        Private Sub New()
        End Sub

        Public Function Convert(Of TSyntaxKind As Structure)(kind As Integer) As TSyntaxKind Implements ISyntaxKindsService.Convert
            ' Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
            Return CType(CType(CType(kind, SyntaxKind), Object), TSyntaxKind)
        End Function

        Public ReadOnly Property ConflictMarkerTrivia As Integer = SyntaxKind.ConflictMarkerTrivia Implements ISyntaxKindsService.ConflictMarkerTrivia
        Public ReadOnly Property DisabledTextTrivia As Integer = SyntaxKind.DisabledTextTrivia Implements ISyntaxKindsService.DisabledTextTrivia
        Public ReadOnly Property EndOfLineTrivia As Integer = SyntaxKind.EndOfLineTrivia Implements ISyntaxKindsService.EndOfLineTrivia
        Public ReadOnly Property SkippedTokensTrivia As Integer = SyntaxKind.SkippedTokensTrivia Implements ISyntaxKindsService.SkippedTokensTrivia
        Public ReadOnly Property WhitespaceTrivia As Integer = SyntaxKind.WhitespaceTrivia Implements ISyntaxKindsService.WhitespaceTrivia

        Public ReadOnly Property CharacterLiteralToken As Integer = SyntaxKind.CharacterLiteralToken Implements ISyntaxKindsService.CharacterLiteralToken
        Public ReadOnly Property DotToken As Integer = SyntaxKind.DotToken Implements ISyntaxKindsService.DotToken
        Public ReadOnly Property InterpolatedStringTextToken As Integer = SyntaxKind.InterpolatedStringTextToken Implements ISyntaxKindsService.InterpolatedStringTextToken
        Public ReadOnly Property QuestionToken As Integer = SyntaxKind.QuestionToken Implements ISyntaxKindsService.QuestionToken
        Public ReadOnly Property StringLiteralToken As Integer = SyntaxKind.StringLiteralToken Implements ISyntaxKindsService.StringLiteralToken

        Public ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword Implements ISyntaxKindsService.IfKeyword

        Public ReadOnly Property GenericName As Integer = SyntaxKind.GenericName Implements ISyntaxKindsService.GenericName
        Public ReadOnly Property IdentifierName As Integer = SyntaxKind.IdentifierName Implements ISyntaxKindsService.IdentifierName
        Public ReadOnly Property QualifiedName As Integer = SyntaxKind.QualifiedName Implements ISyntaxKindsService.QualifiedName

        Public ReadOnly Property TupleType As Integer = SyntaxKind.TupleType Implements ISyntaxKindsService.TupleType

        Public ReadOnly Property CharacterLiteralExpression As Integer = SyntaxKind.CharacterLiteralExpression Implements ISyntaxKindsService.CharacterLiteralExpression
        Public ReadOnly Property DefaultLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKindsService.DefaultLiteralExpression
        Public ReadOnly Property FalseLiteralExpression As Integer = SyntaxKind.FalseLiteralExpression Implements ISyntaxKindsService.FalseLiteralExpression
        Public ReadOnly Property NullLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKindsService.NullLiteralExpression
        Public ReadOnly Property StringLiteralExpression As Integer = SyntaxKind.StringLiteralExpression Implements ISyntaxKindsService.StringLiteralExpression
        Public ReadOnly Property TrueLiteralExpression As Integer = SyntaxKind.TrueLiteralExpression Implements ISyntaxKindsService.TrueLiteralExpression

        Public ReadOnly Property AnonymousObjectCreationExpression As Integer = SyntaxKind.AnonymousObjectCreationExpression Implements ISyntaxKindsService.AnonymousObjectCreationExpression
        Public ReadOnly Property AwaitExpression As Integer = SyntaxKind.AwaitExpression Implements ISyntaxKindsService.AwaitExpression
        Public ReadOnly Property BaseExpression As Integer = SyntaxKind.MyBaseExpression Implements ISyntaxKindsService.BaseExpression
        Public ReadOnly Property ConditionalAccessExpression As Integer = SyntaxKind.ConditionalAccessExpression Implements ISyntaxKindsService.ConditionalAccessExpression
        Public ReadOnly Property InvocationExpression As Integer = SyntaxKind.InvocationExpression Implements ISyntaxKindsService.InvocationExpression
        Public ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression Implements ISyntaxKindsService.LogicalAndExpression
        Public ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression Implements ISyntaxKindsService.LogicalOrExpression
        Public ReadOnly Property LogicalNotExpression As Integer = SyntaxKind.NotExpression Implements ISyntaxKindsService.LogicalNotExpression
        Public ReadOnly Property ObjectCreationExpression As Integer = SyntaxKind.ObjectCreationExpression Implements ISyntaxKindsService.ObjectCreationExpression
        Public ReadOnly Property ParenthesizedExpression As Integer = SyntaxKind.ParenthesizedExpression Implements ISyntaxKindsService.ParenthesizedExpression
        Public ReadOnly Property QueryExpression As Integer = SyntaxKind.QueryExpression Implements ISyntaxKindsService.QueryExpression
        Public ReadOnly Property ReferenceEqualsExpression As Integer = SyntaxKind.IsExpression Implements ISyntaxKindsService.ReferenceEqualsExpression
        Public ReadOnly Property ReferenceNotEqualsExpression As Integer = SyntaxKind.IsNotExpression Implements ISyntaxKindsService.ReferenceNotEqualsExpression
        Public ReadOnly Property SimpleMemberAccessExpression As Integer = SyntaxKind.SimpleMemberAccessExpression Implements ISyntaxKindsService.SimpleMemberAccessExpression
        Public ReadOnly Property TernaryConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression Implements ISyntaxKindsService.TernaryConditionalExpression
        Public ReadOnly Property ThisExpression As Integer = SyntaxKind.MeExpression Implements ISyntaxKindsService.ThisExpression
        Public ReadOnly Property TupleExpression As Integer = SyntaxKind.TupleExpression Implements ISyntaxKindsService.TupleExpression

        Public ReadOnly Property EndOfFileToken As Integer = SyntaxKind.EndOfFileToken Implements ISyntaxKindsService.EndOfFileToken
        Public ReadOnly Property AwaitKeyword As Integer = SyntaxKind.AwaitKeyword Implements ISyntaxKindsService.AwaitKeyword

        Public ReadOnly Property IdentifierToken As Integer = SyntaxKind.IdentifierToken Implements ISyntaxKindsService.IdentifierToken
        Public ReadOnly Property GlobalKeyword As Integer = SyntaxKind.GlobalKeyword Implements ISyntaxKindsService.GlobalKeyword
        Public ReadOnly Property IncompleteMember As Integer = SyntaxKind.IncompleteMember Implements ISyntaxKindsService.IncompleteMember
        Public ReadOnly Property HashToken As Integer = SyntaxKind.HashToken Implements ISyntaxKindsService.HashToken

        Public ReadOnly Property ExpressionStatement As Integer = SyntaxKind.ExpressionStatement Implements ISyntaxKindsService.ExpressionStatement
        Public ReadOnly Property ForEachStatement As Integer = SyntaxKind.ForEachStatement Implements ISyntaxKindsService.ForEachStatement
        Public ReadOnly Property LocalDeclarationStatement As Integer = SyntaxKind.LocalDeclarationStatement Implements ISyntaxKindsService.LocalDeclarationStatement
        Public ReadOnly Property LockStatement As Integer = SyntaxKind.SyncLockStatement Implements ISyntaxKindsService.LockStatement
        Public ReadOnly Property ReturnStatement As Integer = SyntaxKind.ReturnStatement Implements ISyntaxKindsService.ReturnStatement
        Public ReadOnly Property UsingStatement As Integer = SyntaxKind.UsingStatement Implements ISyntaxKindsService.UsingStatement

        Public ReadOnly Property Attribute As Integer = SyntaxKind.Attribute Implements ISyntaxKindsService.Attribute
        Public ReadOnly Property Parameter As Integer = SyntaxKind.Parameter Implements ISyntaxKindsService.Parameter
        Public ReadOnly Property TypeConstraint As Integer = SyntaxKind.TypeConstraint Implements ISyntaxKindsService.TypeConstraint
        Public ReadOnly Property VariableDeclarator As Integer = SyntaxKind.VariableDeclarator Implements ISyntaxKindsService.VariableDeclarator

        Public ReadOnly Property TypeArgumentList As Integer = SyntaxKind.TypeArgumentList Implements ISyntaxKindsService.TypeArgumentList
    End Class
End Namespace
