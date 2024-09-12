' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    Friend Class VisualBasicSyntaxKinds
        Implements ISyntaxKinds

        Public Shared ReadOnly Instance As New VisualBasicSyntaxKinds()

        Protected Sub New()
        End Sub

        Public Function Convert(Of TSyntaxKind As Structure)(kind As Integer) As TSyntaxKind Implements ISyntaxKinds.Convert
            ' Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
            Return CType(CType(CType(kind, SyntaxKind), Object), TSyntaxKind)
        End Function

        Public Function Convert(Of TSyntaxKind As Structure)(kind As TSyntaxKind) As Integer Implements ISyntaxKinds.Convert
            ' Boxing/Unboxing casts from Object to SyntaxKind will be erased by jit.
            Return CType(CType(CType(kind, Object), SyntaxKind), Integer)
        End Function

        Public ReadOnly Property ConflictMarkerTrivia As Integer = SyntaxKind.ConflictMarkerTrivia Implements ISyntaxKinds.ConflictMarkerTrivia
        Public ReadOnly Property DisabledTextTrivia As Integer = SyntaxKind.DisabledTextTrivia Implements ISyntaxKinds.DisabledTextTrivia
        Public ReadOnly Property EndOfLineTrivia As Integer = SyntaxKind.EndOfLineTrivia Implements ISyntaxKinds.EndOfLineTrivia
        Public ReadOnly Property SkippedTokensTrivia As Integer = SyntaxKind.SkippedTokensTrivia Implements ISyntaxKinds.SkippedTokensTrivia
        Public ReadOnly Property WhitespaceTrivia As Integer = SyntaxKind.WhitespaceTrivia Implements ISyntaxKinds.WhitespaceTrivia
        Public ReadOnly Property SingleLineCommentTrivia As Integer = SyntaxKind.CommentTrivia Implements ISyntaxKinds.SingleLineCommentTrivia
        Public ReadOnly Property MultiLineCommentTrivia As Integer? Implements ISyntaxKinds.MultiLineCommentTrivia

        Public ReadOnly Property SingleLineDocCommentTrivia As Integer = SyntaxKind.DocumentationCommentTrivia Implements ISyntaxKinds.SingleLineDocCommentTrivia
        Public ReadOnly Property MultiLineDocCommentTrivia As Integer? Implements ISyntaxKinds.MultiLineDocCommentTrivia

        Public ReadOnly Property IfDirectiveTrivia As Integer = SyntaxKind.IfDirectiveTrivia Implements ISyntaxKinds.IfDirectiveTrivia
        Public ReadOnly Property ElifDirectiveTrivia As Integer = SyntaxKind.ElseIfDirectiveTrivia Implements ISyntaxKinds.ElifDirectiveTrivia
        Public ReadOnly Property ElseDirectiveTrivia As Integer = SyntaxKind.ElseDirectiveTrivia Implements ISyntaxKinds.ElseDirectiveTrivia
        Public ReadOnly Property EndIfDirectiveTrivia As Integer = SyntaxKind.EndIfDirectiveTrivia Implements ISyntaxKinds.EndIfDirectiveTrivia
        Public ReadOnly Property RegionDirectiveTrivia As Integer = SyntaxKind.RegionDirectiveTrivia Implements ISyntaxKinds.RegionDirectiveTrivia
        Public ReadOnly Property EndRegionDirectiveTrivia As Integer = SyntaxKind.EndRegionDirectiveTrivia Implements ISyntaxKinds.EndRegionDirectiveTrivia
        Public ReadOnly Property ShebangDirectiveTrivia As Integer? Implements ISyntaxKinds.ShebangDirectiveTrivia

        Public ReadOnly Property CloseBraceToken As Integer = SyntaxKind.CloseBraceToken Implements ISyntaxKinds.CloseBraceToken
        Public ReadOnly Property CloseBracketToken As Integer? = Nothing Implements ISyntaxKinds.CloseBracketToken
        Public ReadOnly Property CloseParenToken As Integer = SyntaxKind.CloseParenToken Implements ISyntaxKinds.CloseParenToken
        Public ReadOnly Property CommaToken As Integer = SyntaxKind.CommaToken Implements ISyntaxKinds.CommaToken
        Public ReadOnly Property ColonToken As Integer = SyntaxKind.ColonToken Implements ISyntaxKinds.ColonToken
        Public ReadOnly Property CharacterLiteralToken As Integer = SyntaxKind.CharacterLiteralToken Implements ISyntaxKinds.CharacterLiteralToken
        Public ReadOnly Property DotToken As Integer = SyntaxKind.DotToken Implements ISyntaxKinds.DotToken
        Public ReadOnly Property GreaterThanToken As Integer = SyntaxKind.GreaterThanToken Implements ISyntaxKinds.GreaterThanToken
        Public ReadOnly Property InterpolatedStringTextToken As Integer = SyntaxKind.InterpolatedStringTextToken Implements ISyntaxKinds.InterpolatedStringTextToken
        Public ReadOnly Property LessThanToken As Integer = SyntaxKind.LessThanToken Implements ISyntaxKinds.LessThanToken
        Public ReadOnly Property LessThanSlashToken As Integer = SyntaxKind.LessThanSlashToken Implements ISyntaxKinds.LessThanSlashToken
        Public ReadOnly Property OpenBraceToken As Integer = SyntaxKind.OpenBraceToken Implements ISyntaxKinds.OpenBraceToken
        Public ReadOnly Property OpenBracketToken As Integer? = Nothing Implements ISyntaxKinds.OpenBracketToken
        Public ReadOnly Property OpenParenToken As Integer = SyntaxKind.OpenParenToken Implements ISyntaxKinds.OpenParenToken
        Public ReadOnly Property QuestionToken As Integer = SyntaxKind.QuestionToken Implements ISyntaxKinds.QuestionToken
        Public ReadOnly Property StringLiteralToken As Integer = SyntaxKind.StringLiteralToken Implements ISyntaxKinds.StringLiteralToken
        Public ReadOnly Property SingleLineRawStringLiteralToken As Integer? Implements ISyntaxKinds.SingleLineRawStringLiteralToken
        Public ReadOnly Property MultiLineRawStringLiteralToken As Integer? Implements ISyntaxKinds.MultiLineRawStringLiteralToken
        Public ReadOnly Property Utf8StringLiteralToken As Integer? Implements ISyntaxKinds.Utf8StringLiteralToken
        Public ReadOnly Property Utf8SingleLineRawStringLiteralToken As Integer? Implements ISyntaxKinds.Utf8SingleLineRawStringLiteralToken
        Public ReadOnly Property Utf8MultiLineRawStringLiteralToken As Integer? Implements ISyntaxKinds.Utf8MultiLineRawStringLiteralToken

        Public ReadOnly Property XmlTextLiteralToken As Integer = SyntaxKind.XmlTextLiteralToken Implements ISyntaxKinds.XmlTextLiteralToken

        Public ReadOnly Property DelegateKeyword As Integer = SyntaxKind.DelegateKeyword Implements ISyntaxKinds.DelegateKeyword
        Public ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword Implements ISyntaxKinds.IfKeyword
        Public ReadOnly Property TrueKeyword As Integer = SyntaxKind.TrueKeyword Implements ISyntaxKinds.TrueKeyword
        Public ReadOnly Property FalseKeyword As Integer = SyntaxKind.FalseKeyword Implements ISyntaxKinds.FalseKeyword
        Public ReadOnly Property UsingKeyword As Integer = SyntaxKind.UsingKeyword Implements ISyntaxKinds.UsingKeyword

        Public ReadOnly Property GenericName As Integer = SyntaxKind.GenericName Implements ISyntaxKinds.GenericName
        Public ReadOnly Property IdentifierName As Integer = SyntaxKind.IdentifierName Implements ISyntaxKinds.IdentifierName
        Public ReadOnly Property QualifiedName As Integer = SyntaxKind.QualifiedName Implements ISyntaxKinds.QualifiedName

        Public ReadOnly Property TupleType As Integer = SyntaxKind.TupleType Implements ISyntaxKinds.TupleType

        Public ReadOnly Property CharacterLiteralExpression As Integer = SyntaxKind.CharacterLiteralExpression Implements ISyntaxKinds.CharacterLiteralExpression
        Public ReadOnly Property DefaultLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKinds.DefaultLiteralExpression
        Public ReadOnly Property FalseLiteralExpression As Integer = SyntaxKind.FalseLiteralExpression Implements ISyntaxKinds.FalseLiteralExpression
        Public ReadOnly Property NullLiteralExpression As Integer = SyntaxKind.NothingLiteralExpression Implements ISyntaxKinds.NullLiteralExpression
        Public ReadOnly Property NumericLiteralExpression As Integer = SyntaxKind.NumericLiteralExpression Implements ISyntaxKinds.NumericLiteralExpression
        Public ReadOnly Property StringLiteralExpression As Integer = SyntaxKind.StringLiteralExpression Implements ISyntaxKinds.StringLiteralExpression
        Public ReadOnly Property TrueLiteralExpression As Integer = SyntaxKind.TrueLiteralExpression Implements ISyntaxKinds.TrueLiteralExpression

        Public ReadOnly Property AddressOfExpression As Integer = SyntaxKind.AddressOfExpression Implements ISyntaxKinds.AddressOfExpression
        Public ReadOnly Property AnonymousObjectCreationExpression As Integer = SyntaxKind.AnonymousObjectCreationExpression Implements ISyntaxKinds.AnonymousObjectCreationExpression
        Public ReadOnly Property ArrayCreationExpression As Integer = SyntaxKind.ArrayCreationExpression Implements ISyntaxKinds.ArrayCreationExpression
        Public ReadOnly Property AwaitExpression As Integer = SyntaxKind.AwaitExpression Implements ISyntaxKinds.AwaitExpression
        Public ReadOnly Property BaseExpression As Integer = SyntaxKind.MyBaseExpression Implements ISyntaxKinds.BaseExpression
        Public ReadOnly Property CollectionInitializerExpression As Integer = SyntaxKind.CollectionInitializer Implements ISyntaxKinds.CollectionInitializerExpression
        Public ReadOnly Property ConditionalAccessExpression As Integer = SyntaxKind.ConditionalAccessExpression Implements ISyntaxKinds.ConditionalAccessExpression
        Public ReadOnly Property ConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression Implements ISyntaxKinds.ConditionalExpression
        Public ReadOnly Property ImplicitArrayCreationExpression As Integer? Implements ISyntaxKinds.ImplicitArrayCreationExpression
        Public ReadOnly Property ImplicitObjectCreationExpression As Integer? Implements ISyntaxKinds.ImplicitObjectCreationExpression
        Public ReadOnly Property IndexExpression As Integer? Implements ISyntaxKinds.IndexExpression
        Public ReadOnly Property InvocationExpression As Integer = SyntaxKind.InvocationExpression Implements ISyntaxKinds.InvocationExpression
        Public ReadOnly Property IsTypeExpression As Integer = SyntaxKind.TypeOfIsExpression Implements ISyntaxKinds.IsTypeExpression
        Public ReadOnly Property IsNotTypeExpression As Integer? = SyntaxKind.TypeOfIsNotExpression Implements ISyntaxKinds.IsNotTypeExpression
        Public ReadOnly Property IsPatternExpression As Integer? Implements ISyntaxKinds.IsPatternExpression
        Public ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression Implements ISyntaxKinds.LogicalAndExpression
        Public ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression Implements ISyntaxKinds.LogicalOrExpression
        Public ReadOnly Property LogicalNotExpression As Integer = SyntaxKind.NotExpression Implements ISyntaxKinds.LogicalNotExpression
        Public ReadOnly Property ObjectCreationExpression As Integer = SyntaxKind.ObjectCreationExpression Implements ISyntaxKinds.ObjectCreationExpression
        Public ReadOnly Property ParenthesizedExpression As Integer = SyntaxKind.ParenthesizedExpression Implements ISyntaxKinds.ParenthesizedExpression
        Public ReadOnly Property QueryExpression As Integer = SyntaxKind.QueryExpression Implements ISyntaxKinds.QueryExpression
        Public ReadOnly Property RangeExpression As Integer? Implements ISyntaxKinds.RangeExpression
        Public ReadOnly Property RefExpression As Integer? Implements ISyntaxKinds.RefExpression
        Public ReadOnly Property ReferenceEqualsExpression As Integer = SyntaxKind.IsExpression Implements ISyntaxKinds.ReferenceEqualsExpression
        Public ReadOnly Property ReferenceNotEqualsExpression As Integer = SyntaxKind.IsNotExpression Implements ISyntaxKinds.ReferenceNotEqualsExpression
        Public ReadOnly Property SimpleMemberAccessExpression As Integer = SyntaxKind.SimpleMemberAccessExpression Implements ISyntaxKinds.SimpleMemberAccessExpression
        Public ReadOnly Property TernaryConditionalExpression As Integer = SyntaxKind.TernaryConditionalExpression Implements ISyntaxKinds.TernaryConditionalExpression
        Public ReadOnly Property ThisExpression As Integer = SyntaxKind.MeExpression Implements ISyntaxKinds.ThisExpression
        Public ReadOnly Property ThrowExpression As Integer? Implements ISyntaxKinds.ThrowExpression
        Public ReadOnly Property TupleExpression As Integer = SyntaxKind.TupleExpression Implements ISyntaxKinds.TupleExpression

        Public ReadOnly Property AndPattern As Integer? Implements ISyntaxKinds.AndPattern
        Public ReadOnly Property ConstantPattern As Integer? Implements ISyntaxKinds.ConstantPattern
        Public ReadOnly Property DeclarationPattern As Integer? Implements ISyntaxKinds.DeclarationPattern
        Public ReadOnly Property ListPattern As Integer? Implements ISyntaxKinds.ListPattern
        Public ReadOnly Property NotPattern As Integer? Implements ISyntaxKinds.NotPattern
        Public ReadOnly Property OrPattern As Integer? Implements ISyntaxKinds.OrPattern
        Public ReadOnly Property ParenthesizedPattern As Integer? Implements ISyntaxKinds.ParenthesizedPattern
        Public ReadOnly Property RecursivePattern As Integer? Implements ISyntaxKinds.RecursivePattern
        Public ReadOnly Property RelationalPattern As Integer? Implements ISyntaxKinds.RelationalPattern
        Public ReadOnly Property TypePattern As Integer? Implements ISyntaxKinds.TypePattern
        Public ReadOnly Property VarPattern As Integer? Implements ISyntaxKinds.VarPattern

        Public ReadOnly Property EndOfFileToken As Integer = SyntaxKind.EndOfFileToken Implements ISyntaxKinds.EndOfFileToken
        Public ReadOnly Property AwaitKeyword As Integer = SyntaxKind.AwaitKeyword Implements ISyntaxKinds.AwaitKeyword
        Public ReadOnly Property AsyncKeyword As Integer = SyntaxKind.AsyncKeyword Implements ISyntaxKinds.AsyncKeyword

        Public ReadOnly Property IdentifierToken As Integer = SyntaxKind.IdentifierToken Implements ISyntaxKinds.IdentifierToken
        Public ReadOnly Property GlobalKeyword As Integer = SyntaxKind.GlobalKeyword Implements ISyntaxKinds.GlobalKeyword
        Public ReadOnly Property IncompleteMember As Integer = SyntaxKind.IncompleteMember Implements ISyntaxKinds.IncompleteMember
        Public ReadOnly Property HashToken As Integer = SyntaxKind.HashToken Implements ISyntaxKinds.HashToken

        Public ReadOnly Property ExpressionStatement As Integer = SyntaxKind.ExpressionStatement Implements ISyntaxKinds.ExpressionStatement
        Public ReadOnly Property ForEachStatement As Integer = SyntaxKind.ForEachStatement Implements ISyntaxKinds.ForEachStatement
        Public ReadOnly Property ForStatement As Integer = SyntaxKind.ForStatement Implements ISyntaxKinds.ForStatement
        Public ReadOnly Property IfStatement As Integer = SyntaxKind.IfStatement Implements ISyntaxKinds.IfStatement
        Public ReadOnly Property LocalDeclarationStatement As Integer = SyntaxKind.LocalDeclarationStatement Implements ISyntaxKinds.LocalDeclarationStatement
        Public ReadOnly Property LocalFunctionStatement As Integer? Implements ISyntaxKinds.LocalFunctionStatement
        Public ReadOnly Property LockStatement As Integer = SyntaxKind.SyncLockStatement Implements ISyntaxKinds.LockStatement
        Public ReadOnly Property ReturnStatement As Integer = SyntaxKind.ReturnStatement Implements ISyntaxKinds.ReturnStatement
        Public ReadOnly Property ThrowStatement As Integer = SyntaxKind.ThrowStatement Implements ISyntaxKinds.ThrowStatement
        Public ReadOnly Property UsingStatement As Integer = SyntaxKind.UsingStatement Implements ISyntaxKinds.UsingStatement
        Public ReadOnly Property WhileStatement As Integer = SyntaxKind.WhileStatement Implements ISyntaxKinds.WhileStatement
        Public ReadOnly Property YieldReturnStatement As Integer = SyntaxKind.YieldStatement Implements ISyntaxKinds.YieldReturnStatement

        Public ReadOnly Property Attribute As Integer = SyntaxKind.Attribute Implements ISyntaxKinds.Attribute
        Public ReadOnly Property ClassDeclaration As Integer = SyntaxKind.ClassBlock Implements ISyntaxKinds.ClassDeclaration
        Public ReadOnly Property ConstructorDeclaration As Integer = SyntaxKind.ConstructorBlock Implements ISyntaxKinds.ConstructorDeclaration
        Public ReadOnly Property EnumDeclaration As Integer = SyntaxKind.EnumBlock Implements ISyntaxKinds.EnumDeclaration
        Public ReadOnly Property InterfaceDeclaration As Integer = SyntaxKind.InterfaceBlock Implements ISyntaxKinds.InterfaceDeclaration
        Public ReadOnly Property StructDeclaration As Integer? Implements ISyntaxKinds.StructDeclaration
        Public ReadOnly Property Parameter As Integer = SyntaxKind.Parameter Implements ISyntaxKinds.Parameter
        Public ReadOnly Property TypeConstraint As Integer = SyntaxKind.TypeConstraint Implements ISyntaxKinds.TypeConstraint
        Public ReadOnly Property VariableDeclarator As Integer = SyntaxKind.VariableDeclarator Implements ISyntaxKinds.VariableDeclarator
        Public ReadOnly Property FieldDeclaration As Integer = SyntaxKind.FieldDeclaration Implements ISyntaxKinds.FieldDeclaration
        Public ReadOnly Property PropertyDeclaration As Integer = SyntaxKind.PropertyBlock Implements ISyntaxKinds.PropertyDeclaration
        Public ReadOnly Property ParameterList As Integer = SyntaxKind.ParameterList Implements ISyntaxKinds.ParameterList
        Public ReadOnly Property TypeArgumentList As Integer = SyntaxKind.TypeArgumentList Implements ISyntaxKinds.TypeArgumentList
        Public ReadOnly Property GlobalStatement As Integer? Implements ISyntaxKinds.GlobalStatement

        Public ReadOnly Property ElseClause As Integer = SyntaxKind.ElseBlock Implements ISyntaxKinds.ElseClause
        Public ReadOnly Property EqualsValueClause As Integer = SyntaxKind.EqualsValue Implements ISyntaxKinds.EqualsValueClause

        Public ReadOnly Property ImplicitElementAccess As Integer? Implements ISyntaxKinds.ImplicitElementAccess
        Public ReadOnly Property Interpolation As Integer = SyntaxKind.Interpolation Implements ISyntaxKinds.Interpolation
        Public ReadOnly Property InterpolatedStringExpression As Integer = SyntaxKind.InterpolatedStringExpression Implements ISyntaxKinds.InterpolatedStringExpression
        Public ReadOnly Property InterpolatedStringText As Integer = SyntaxKind.InterpolatedStringText Implements ISyntaxKinds.InterpolatedStringText
        Public ReadOnly Property IndexerMemberCref As Integer? Implements ISyntaxKinds.IndexerMemberCref
    End Class
End Namespace
