// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageService;

/// <summary>
/// Provides a uniform view of SyntaxKinds over C# and VB for constructs they have
/// in common.
/// </summary>
internal interface ISyntaxKinds
{
    TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct;
    int Convert<TSyntaxKind>(TSyntaxKind kind) where TSyntaxKind : struct;

    #region trivia

    int ConflictMarkerTrivia { get; }
    int DisabledTextTrivia { get; }
    int EndOfLineTrivia { get; }
    int SkippedTokensTrivia { get; }
    int WhitespaceTrivia { get; }
    int SingleLineCommentTrivia { get; }

    /// <summary>
    /// Gets the syntax kind for a multi-line comment.
    /// </summary>
    /// <value>
    /// The raw syntax kind for a multi-line comment; otherwise, <see langword="null"/> if the language does not
    /// support multi-line comments.
    /// </value>
    int? MultiLineCommentTrivia { get; }

    int SingleLineDocCommentTrivia { get; }
    int? MultiLineDocCommentTrivia { get; }

    int IfDirectiveTrivia { get; }
    int ElifDirectiveTrivia { get; }
    int ElseDirectiveTrivia { get; }
    int EndIfDirectiveTrivia { get; }
    int EndRegionDirectiveTrivia { get; }
    int RegionDirectiveTrivia { get; }
    int? ShebangDirectiveTrivia { get; }
    int DefineDirectiveTrivia { get; }
    int? UndefDirectiveTrivia { get; }

    #endregion

    #region keywords

    int AsyncKeyword { get; }
    int AwaitKeyword { get; }
    int DelegateKeyword { get; }
    int FalseKeyword { get; }
    int GlobalKeyword { get; }
    int? GlobalStatement { get; }
    int IfKeyword { get; }
    int NewKeyword { get; }
    int PartialKeyword { get; }
    int TrueKeyword { get; }
    int UsingKeyword { get; }

    #endregion

    #region literal tokens

    int CharacterLiteralToken { get; }
    int StringLiteralToken { get; }
    int? SingleLineRawStringLiteralToken { get; }
    int? MultiLineRawStringLiteralToken { get; }
    int? Utf8StringLiteralToken { get; }
    int? Utf8SingleLineRawStringLiteralToken { get; }
    int? Utf8MultiLineRawStringLiteralToken { get; }

    #endregion

    #region tokens

    int CloseBraceToken { get; }
    int? CloseBracketToken { get; }
    int CloseParenToken { get; }
    int CommaToken { get; }
    int ColonToken { get; }
    int DotToken { get; }
    int EndOfFileToken { get; }
    int HashToken { get; }
    int GreaterThanToken { get; }
    int IdentifierToken { get; }
    int InterpolatedStringTextToken { get; }
    int LessThanSlashToken { get; }
    int LessThanToken { get; }
    int OpenBraceToken { get; }
    int? OpenBracketToken { get; }
    int OpenParenToken { get; }
    int QuestionToken { get; }

    #endregion

    #region xml nodes and tokens

    int XmlCrefAttribute { get; }
    int XmlTextLiteralToken { get; }

    #endregion

    #region names

    int? AliasQualifiedName { get; }
    int GenericName { get; }
    int IdentifierName { get; }
    int QualifiedName { get; }

    #endregion

    #region types

    int TupleType { get; }

    #endregion

    #region literal expressions

    int CharacterLiteralExpression { get; }
    int DefaultLiteralExpression { get; }
    int FalseLiteralExpression { get; }
    int NullLiteralExpression { get; }
    int NumericLiteralExpression { get; }
    int StringLiteralExpression { get; }
    int TrueLiteralExpression { get; }

    #endregion

    #region expressions

    int AddExpression { get; }
    int AddressOfExpression { get; }
    int AnonymousObjectCreationExpression { get; }
    int ArrayCreationExpression { get; }
    int AwaitExpression { get; }
    int BaseExpression { get; }
    int? CollectionExpression { get; }
    int CollectionInitializerExpression { get; }
    int ConditionalAccessExpression { get; }
    int ConditionalExpression { get; }
    int? FieldExpression { get; }
    int? ImplicitArrayCreationExpression { get; }
    int? ImplicitObjectCreationExpression { get; }
    int? IndexExpression { get; }
    int InterpolatedStringExpression { get; }
    int InvocationExpression { get; }
    int IsTypeExpression { get; }
    int? IsNotTypeExpression { get; }
    int? IsPatternExpression { get; }
    int LogicalAndExpression { get; }
    int LogicalOrExpression { get; }
    int LogicalNotExpression { get; }
    int ObjectCreationExpression { get; }
    int ParenthesizedExpression { get; }
    int QueryExpression { get; }
    int? RangeExpression { get; }
    int? RefExpression { get; }
    int ReferenceEqualsExpression { get; }
    int ReferenceNotEqualsExpression { get; }
    int SimpleAssignmentExpression { get; }
    int SimpleMemberAccessExpression { get; }
    int? SizeOfExpression { get; }
    int? SuppressNullableWarningExpression { get; }
    int TernaryConditionalExpression { get; }
    int ThisExpression { get; }
    int? ThrowExpression { get; }
    int TupleExpression { get; }
    int TypeOfExpression { get; }

    #endregion

    #region patterns

    int? AndPattern { get; }
    int? ConstantPattern { get; }
    int? DeclarationPattern { get; }
    int? ListPattern { get; }
    int? NotPattern { get; }
    int? OrPattern { get; }
    int? ParenthesizedPattern { get; }
    int? RecursivePattern { get; }
    int? RelationalPattern { get; }
    int? TypePattern { get; }
    int? VarPattern { get; }

    #endregion

    #region statements

    int ExpressionStatement { get; }
    int ForEachStatement { get; }
    int ForStatement { get; }
    int IfStatement { get; }
    int LocalDeclarationStatement { get; }
    int? LocalFunctionStatement { get; }
    int LockStatement { get; }
    int ReturnStatement { get; }
    int ThrowStatement { get; }
    int UsingStatement { get; }
    int WhileStatement { get; }
    int YieldReturnStatement { get; }

    #endregion

    #region members/declarations

    int Attribute { get; }
    int ClassDeclaration { get; }
    int ConstructorDeclaration { get; }
    int EnumDeclaration { get; }
    int InterfaceDeclaration { get; }
    int? StructDeclaration { get; }
    int Parameter { get; }
    int TypeConstraint { get; }
    int VariableDeclarator { get; }
    int FieldDeclaration { get; }
    int PropertyDeclaration { get; }

    int IncompleteMember { get; }
    int TypeArgumentList { get; }
    int ParameterList { get; }

    #endregion

    #region clauses

    int ElseClause { get; }
    int EqualsValueClause { get; }

    #endregion

    #region other

    int? ExpressionElement { get; }
    int? ImplicitElementAccess { get; }
    int Interpolation { get; }
    int InterpolatedStringText { get; }
    int? IndexerMemberCref { get; }
    int? PrimaryConstructorBaseType { get; }

    #endregion
}
