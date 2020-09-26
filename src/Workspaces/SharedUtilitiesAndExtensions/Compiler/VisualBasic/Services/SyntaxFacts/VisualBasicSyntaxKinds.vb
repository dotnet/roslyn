' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend Class VisualBasicSyntaxKinds
        Implements ISyntaxKinds

        Public Shared ReadOnly Instance As New VisualBasicSyntaxKinds()

        Protected Sub New()
        End Sub

        Public Function Convert(Of TSyntaxKind As Structure)(kind As Integer) As TSyntaxKind Implements ISyntaxKinds.Convert
            ' Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
            Return CType(CType(CType(kind, SyntaxKind), Object), TSyntaxKind)
        End Function

        Public ReadOnly Property ConflictMarkerTrivia As Integer = SyntaxKind.ConflictMarkerTrivia Implements ISyntaxKinds.ConflictMarkerTrivia
        Public ReadOnly Property DisabledTextTrivia As Integer = SyntaxKind.DisabledTextTrivia Implements ISyntaxKinds.DisabledTextTrivia
        Public ReadOnly Property EndOfLineTrivia As Integer = SyntaxKind.EndOfLineTrivia Implements ISyntaxKinds.EndOfLineTrivia
        Public ReadOnly Property SkippedTokensTrivia As Integer = SyntaxKind.SkippedTokensTrivia Implements ISyntaxKinds.SkippedTokensTrivia
        Public ReadOnly Property WhitespaceTrivia As Integer = SyntaxKind.WhitespaceTrivia Implements ISyntaxKinds.WhitespaceTrivia
        Public ReadOnly Property SingleLineCommentTrivia As Integer = SyntaxKind.CommentTrivia Implements ISyntaxKinds.SingleLineCommentTrivia
        Private ReadOnly Property MultiLineCommentTrivia As Integer? = Nothing Implements ISyntaxKinds.MultiLineCommentTrivia

        Public ReadOnly Property CloseBraceToken As Integer = SyntaxKind.CloseBraceToken Implements ISyntaxKinds.CloseBraceToken
        Public ReadOnly Property ColonToken As Integer = SyntaxKind.ColonToken Implements ISyntaxKinds.ColonToken
        Public ReadOnly Property CharacterLiteralToken As Integer = SyntaxKind.CharacterLiteralToken Implements ISyntaxKinds.CharacterLiteralToken
        Public ReadOnly Property DotToken As Integer = SyntaxKind.DotToken Implements ISyntaxKinds.DotToken
        Public ReadOnly Property InterpolatedStringTextToken As Integer = SyntaxKind.InterpolatedStringTextToken Implements ISyntaxKinds.InterpolatedStringTextToken
        Public ReadOnly Property QuestionToken As Integer = SyntaxKind.QuestionToken Implements ISyntaxKinds.QuestionToken
        Public ReadOnly Property StringLiteralToken As Integer = SyntaxKind.StringLiteralToken Implements ISyntaxKinds.StringLiteralToken

        Public ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword Implements ISyntaxKinds.IfKeyword

        Public ReadOnly Property GenericName As Integer = SyntaxKind.GenericName Implements ISyntaxKinds.GenericName
        Public ReadOnly Property IdentifierName As Integer = SyntaxKind.IdentifierName Implements ISyntaxKinds.IdentifierName
        Public ReadOnly Property QualifiedName As Integer = SyntaxKind.QualifiedName Implements ISyntaxKinds.QualifiedName

        Public ReadOnly Property TupleType As Integer = SyntaxKind.TupleType Implements ISyntaxKinds.TupleType

        Public ReadOnly Property CharacterLiteralExpression As Integer = SyntaxKind.CharacterLiteralExpression Implements ISyntaxKinds.CharacterLiteralExpression
        Public ReadOnly Property DefaultLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKinds.DefaultLiteralExpression
        Public ReadOnly Property FalseLiteralExpression As Integer = SyntaxKind.FalseLiteralExpression Implements ISyntaxKinds.FalseLiteralExpression
        Public ReadOnly Property NullLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKinds.NullLiteralExpression
        Public ReadOnly Property StringLiteralExpression As Integer = SyntaxKind.StringLiteralExpression Implements ISyntaxKinds.StringLiteralExpression
        Public ReadOnly Property TrueLiteralExpression As Integer = SyntaxKind.TrueLiteralExpression Implements ISyntaxKinds.TrueLiteralExpression

        Public ReadOnly Property AnonymousObjectCreationExpression As Integer = SyntaxKind.AnonymousObjectCreationExpression Implements ISyntaxKinds.AnonymousObjectCreationExpression
        Public ReadOnly Property AwaitExpression As Integer = SyntaxKind.AwaitExpression Implements ISyntaxKinds.AwaitExpression
        Public ReadOnly Property BaseExpression As Integer = SyntaxKind.MyBaseExpression Implements ISyntaxKinds.BaseExpression
        Public ReadOnly Property ConditionalAccessExpression As Integer = SyntaxKind.ConditionalAccessExpression Implements ISyntaxKinds.ConditionalAccessExpression
        Public ReadOnly Property ConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression Implements ISyntaxKinds.ConditionalExpression
        Public ReadOnly Property InvocationExpression As Integer = SyntaxKind.InvocationExpression Implements ISyntaxKinds.InvocationExpression
        Public ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression Implements ISyntaxKinds.LogicalAndExpression
        Public ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression Implements ISyntaxKinds.LogicalOrExpression
        Public ReadOnly Property LogicalNotExpression As Integer = SyntaxKind.NotExpression Implements ISyntaxKinds.LogicalNotExpression
        Public ReadOnly Property ObjectCreationExpression As Integer = SyntaxKind.ObjectCreationExpression Implements ISyntaxKinds.ObjectCreationExpression
        Public ReadOnly Property ParenthesizedExpression As Integer = SyntaxKind.ParenthesizedExpression Implements ISyntaxKinds.ParenthesizedExpression
        Public ReadOnly Property QueryExpression As Integer = SyntaxKind.QueryExpression Implements ISyntaxKinds.QueryExpression
        Public ReadOnly Property ReferenceEqualsExpression As Integer = SyntaxKind.IsExpression Implements ISyntaxKinds.ReferenceEqualsExpression
        Public ReadOnly Property ReferenceNotEqualsExpression As Integer = SyntaxKind.IsNotExpression Implements ISyntaxKinds.ReferenceNotEqualsExpression
        Public ReadOnly Property SimpleMemberAccessExpression As Integer = SyntaxKind.SimpleMemberAccessExpression Implements ISyntaxKinds.SimpleMemberAccessExpression
        Public ReadOnly Property TernaryConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression Implements ISyntaxKinds.TernaryConditionalExpression
        Public ReadOnly Property ThisExpression As Integer = SyntaxKind.MeExpression Implements ISyntaxKinds.ThisExpression
        Public ReadOnly Property TupleExpression As Integer = SyntaxKind.TupleExpression Implements ISyntaxKinds.TupleExpression

        Public ReadOnly Property EndOfFileToken As Integer = SyntaxKind.EndOfFileToken Implements ISyntaxKinds.EndOfFileToken
        Public ReadOnly Property AwaitKeyword As Integer = SyntaxKind.AwaitKeyword Implements ISyntaxKinds.AwaitKeyword

        Public ReadOnly Property IdentifierToken As Integer = SyntaxKind.IdentifierToken Implements ISyntaxKinds.IdentifierToken
        Public ReadOnly Property GlobalKeyword As Integer = SyntaxKind.GlobalKeyword Implements ISyntaxKinds.GlobalKeyword
        Public ReadOnly Property IncompleteMember As Integer = SyntaxKind.IncompleteMember Implements ISyntaxKinds.IncompleteMember
        Public ReadOnly Property HashToken As Integer = SyntaxKind.HashToken Implements ISyntaxKinds.HashToken

        Public ReadOnly Property ExpressionStatement As Integer = SyntaxKind.ExpressionStatement Implements ISyntaxKinds.ExpressionStatement
        Public ReadOnly Property ForEachStatement As Integer = SyntaxKind.ForEachStatement Implements ISyntaxKinds.ForEachStatement
        Public ReadOnly Property LocalDeclarationStatement As Integer = SyntaxKind.LocalDeclarationStatement Implements ISyntaxKinds.LocalDeclarationStatement
        Public ReadOnly Property LockStatement As Integer = SyntaxKind.SyncLockStatement Implements ISyntaxKinds.LockStatement
        Public ReadOnly Property ReturnStatement As Integer = SyntaxKind.ReturnStatement Implements ISyntaxKinds.ReturnStatement
        Public ReadOnly Property UsingStatement As Integer = SyntaxKind.UsingStatement Implements ISyntaxKinds.UsingStatement

        Public ReadOnly Property Attribute As Integer = SyntaxKind.Attribute Implements ISyntaxKinds.Attribute
        Public ReadOnly Property Parameter As Integer = SyntaxKind.Parameter Implements ISyntaxKinds.Parameter
        Public ReadOnly Property TypeConstraint As Integer = SyntaxKind.TypeConstraint Implements ISyntaxKinds.TypeConstraint
        Public ReadOnly Property VariableDeclarator As Integer = SyntaxKind.VariableDeclarator Implements ISyntaxKinds.VariableDeclarator

        Public ReadOnly Property TypeArgumentList As Integer = SyntaxKind.TypeArgumentList Implements ISyntaxKinds.TypeArgumentList
        Public ReadOnly Property GlobalStatement As Integer? = Nothing Implements ISyntaxKinds.GlobalStatement

        Public ReadOnly Property Interpolation As Integer = SyntaxKind.Interpolation Implements ISyntaxKinds.Interpolation
    End Class
End Namespace
