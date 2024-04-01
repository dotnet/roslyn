// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal class CSharpSyntaxKinds : ISyntaxKinds
{
    public static readonly CSharpSyntaxKinds Instance = new();

    protected CSharpSyntaxKinds()
    {
    }

    // Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
    public TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct
        => (TSyntaxKind)(object)(SyntaxKind)kind;

    // Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
    public int Convert<TSyntaxKind>(TSyntaxKind kind) where TSyntaxKind : struct
        => (int)(SyntaxKind)(object)kind;

    public int ConflictMarkerTrivia => (int)SyntaxKind.ConflictMarkerTrivia;
    public int DisabledTextTrivia => (int)SyntaxKind.DisabledTextTrivia;
    public int EndOfLineTrivia => (int)SyntaxKind.EndOfLineTrivia;
    public int SkippedTokensTrivia => (int)SyntaxKind.SkippedTokensTrivia;
    public int WhitespaceTrivia => (int)SyntaxKind.WhitespaceTrivia;
    public int SingleLineCommentTrivia => (int)SyntaxKind.SingleLineCommentTrivia;
    public int? MultiLineCommentTrivia => (int)SyntaxKind.MultiLineCommentTrivia;
    public int SingleLineDocCommentTrivia => (int)SyntaxKind.SingleLineDocumentationCommentTrivia;
    public int? MultiLineDocCommentTrivia => (int)SyntaxKind.MultiLineDocumentationCommentTrivia;

    public int IfDirectiveTrivia => (int)SyntaxKind.IfDirectiveTrivia;
    public int ElifDirectiveTrivia => (int)SyntaxKind.ElifDirectiveTrivia;
    public int ElseDirectiveTrivia => (int)SyntaxKind.ElseDirectiveTrivia;
    public int EndIfDirectiveTrivia => (int)SyntaxKind.EndIfDirectiveTrivia;
    public int RegionDirectiveTrivia => (int)SyntaxKind.RegionDirectiveTrivia;
    public int EndRegionDirectiveTrivia => (int)SyntaxKind.EndRegionDirectiveTrivia;
    public int? ShebangDirectiveTrivia => (int)SyntaxKind.ShebangDirectiveTrivia;

    public int CloseBraceToken => (int)SyntaxKind.CloseBraceToken;
    public int? CloseBracketToken => (int)SyntaxKind.CloseBracketToken;
    public int CloseParenToken => (int)SyntaxKind.CloseParenToken;
    public int CommaToken => (int)SyntaxKind.CommaToken;
    public int ColonToken => (int)SyntaxKind.ColonToken;
    public int CharacterLiteralToken => (int)SyntaxKind.CharacterLiteralToken;
    public int DotToken => (int)SyntaxKind.DotToken;
    public int GreaterThanToken => (int)SyntaxKind.GreaterThanToken;
    public int InterpolatedStringTextToken => (int)SyntaxKind.InterpolatedStringTextToken;
    public int LessThanToken => (int)SyntaxKind.LessThanToken;
    public int LessThanSlashToken => (int)SyntaxKind.LessThanSlashToken;
    public int OpenBraceToken => (int)SyntaxKind.OpenBraceToken;
    public int? OpenBracketToken => (int)SyntaxKind.OpenBracketToken;
    public int OpenParenToken => (int)SyntaxKind.OpenParenToken;
    public int QuestionToken => (int)SyntaxKind.QuestionToken;
    public int StringLiteralToken => (int)SyntaxKind.StringLiteralToken;
    public int? SingleLineRawStringLiteralToken => (int)SyntaxKind.SingleLineRawStringLiteralToken;
    public int? MultiLineRawStringLiteralToken => (int)SyntaxKind.MultiLineRawStringLiteralToken;
    public int? Utf8StringLiteralToken => (int)SyntaxKind.Utf8StringLiteralToken;
    public int? Utf8SingleLineRawStringLiteralToken => (int)SyntaxKind.Utf8SingleLineRawStringLiteralToken;
    public int? Utf8MultiLineRawStringLiteralToken => (int)SyntaxKind.Utf8MultiLineRawStringLiteralToken;

    public int XmlTextLiteralToken => (int)SyntaxKind.XmlTextLiteralToken;

    public int DelegateKeyword => (int)SyntaxKind.DelegateKeyword;
    public int IfKeyword => (int)SyntaxKind.IfKeyword;
    public int TrueKeyword => (int)SyntaxKind.TrueKeyword;
    public int FalseKeyword => (int)SyntaxKind.FalseKeyword;
    public int UsingKeyword => (int)SyntaxKind.UsingKeyword;

    public int GenericName => (int)SyntaxKind.GenericName;
    public int IdentifierName => (int)SyntaxKind.IdentifierName;
    public int QualifiedName => (int)SyntaxKind.QualifiedName;

    public int TupleType => (int)SyntaxKind.TupleType;

    public int CharacterLiteralExpression => (int)SyntaxKind.CharacterLiteralExpression;
    public int DefaultLiteralExpression => (int)SyntaxKind.DefaultLiteralExpression;
    public int FalseLiteralExpression => (int)SyntaxKind.FalseLiteralExpression;
    public int NullLiteralExpression => (int)SyntaxKind.NullLiteralExpression;
    public int NumericLiteralExpression => (int)SyntaxKind.NumericLiteralExpression;
    public int StringLiteralExpression => (int)SyntaxKind.StringLiteralExpression;
    public int TrueLiteralExpression => (int)SyntaxKind.TrueLiteralExpression;

    public int AddExpression => (int)SyntaxKind.AddExpression;
    public int AddressOfExpression => (int)SyntaxKind.AddressOfExpression;
    public int AnonymousObjectCreationExpression => (int)SyntaxKind.AnonymousObjectCreationExpression;
    public int ArrayCreationExpression => (int)SyntaxKind.ArrayCreationExpression;
    public int AwaitExpression => (int)SyntaxKind.AwaitExpression;
    public int BaseExpression => (int)SyntaxKind.BaseExpression;
    public int CollectionInitializerExpression => (int)SyntaxKind.CollectionInitializerExpression;
    public int ConditionalAccessExpression => (int)SyntaxKind.ConditionalAccessExpression;
    public int ConditionalExpression => (int)SyntaxKind.ConditionalExpression;
    public int? ImplicitArrayCreationExpression => (int)SyntaxKind.ImplicitArrayCreationExpression;
    public int? ImplicitObjectCreationExpression => (int)SyntaxKind.ImplicitObjectCreationExpression;
    public int? IndexExpression => (int)SyntaxKind.IndexExpression;
    public int InvocationExpression => (int)SyntaxKind.InvocationExpression;
    public int? IsPatternExpression => (int)SyntaxKind.IsPatternExpression;
    public int IsTypeExpression => (int)SyntaxKind.IsExpression;
    public int? IsNotTypeExpression => null;
    public int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
    public int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
    public int LogicalNotExpression => (int)SyntaxKind.LogicalNotExpression;
    public int ObjectCreationExpression => (int)SyntaxKind.ObjectCreationExpression;
    public int ParenthesizedExpression => (int)SyntaxKind.ParenthesizedExpression;
    public int QueryExpression => (int)SyntaxKind.QueryExpression;
    public int? RangeExpression => (int)SyntaxKind.RangeExpression;
    public int? RefExpression => (int)SyntaxKind.RefExpression;
    public int ReferenceEqualsExpression => (int)SyntaxKind.EqualsExpression;
    public int ReferenceNotEqualsExpression => (int)SyntaxKind.NotEqualsExpression;
    public int SimpleMemberAccessExpression => (int)SyntaxKind.SimpleMemberAccessExpression;
    public int TernaryConditionalExpression => (int)SyntaxKind.ConditionalExpression;
    public int ThisExpression => (int)SyntaxKind.ThisExpression;
    public int? ThrowExpression => (int)SyntaxKind.ThrowExpression;
    public int TupleExpression => (int)SyntaxKind.TupleExpression;

    public int? AndPattern => (int)SyntaxKind.AndPattern;
    public int? ConstantPattern => (int)SyntaxKind.ConstantPattern;
    public int? DeclarationPattern => (int)SyntaxKind.DeclarationPattern;
    public int? ListPattern => (int)SyntaxKind.ListPattern;
    public int? NotPattern => (int)SyntaxKind.NotPattern;
    public int? OrPattern => (int)SyntaxKind.OrPattern;
    public int? ParenthesizedPattern => (int)SyntaxKind.ParenthesizedPattern;
    public int? RecursivePattern => (int)SyntaxKind.RecursivePattern;
    public int? RelationalPattern => (int)SyntaxKind.RelationalPattern;
    public int? TypePattern => (int)SyntaxKind.TypePattern;
    public int? VarPattern => (int)SyntaxKind.VarPattern;

    public int EndOfFileToken => (int)SyntaxKind.EndOfFileToken;
    public int AwaitKeyword => (int)SyntaxKind.AwaitKeyword;
    public int AsyncKeyword => (int)SyntaxKind.AsyncKeyword;
    public int IdentifierToken => (int)SyntaxKind.IdentifierToken;
    public int GlobalKeyword => (int)SyntaxKind.GlobalKeyword;
    public int IncompleteMember => (int)SyntaxKind.IncompleteMember;
    public int HashToken => (int)SyntaxKind.HashToken;

    public int ExpressionStatement => (int)SyntaxKind.ExpressionStatement;
    public int ForEachStatement => (int)SyntaxKind.ForEachStatement;
    public int ForStatement => (int)SyntaxKind.ForStatement;
    public int IfStatement => (int)SyntaxKind.IfStatement;
    public int LocalDeclarationStatement => (int)SyntaxKind.LocalDeclarationStatement;
    public int? LocalFunctionStatement => (int)SyntaxKind.LocalFunctionStatement;
    public int LockStatement => (int)SyntaxKind.LockStatement;
    public int ReturnStatement => (int)SyntaxKind.ReturnStatement;
    public int ThrowStatement => (int)SyntaxKind.ThrowStatement;
    public int UsingStatement => (int)SyntaxKind.UsingStatement;
    public int WhileStatement => (int)SyntaxKind.WhileStatement;
    public int YieldReturnStatement => (int)SyntaxKind.YieldReturnStatement;
    public int Attribute => (int)SyntaxKind.Attribute;
    public int ClassDeclaration => (int)SyntaxKind.ClassDeclaration;
    public int ConstructorDeclaration => (int)SyntaxKind.ConstructorDeclaration;
    public int EnumDeclaration => (int)SyntaxKind.EnumDeclaration;
    public int InterfaceDeclaration => (int)SyntaxKind.InterfaceDeclaration;
    public int? StructDeclaration => (int)SyntaxKind.StructDeclaration;
    public int Parameter => (int)SyntaxKind.Parameter;
    public int TypeConstraint => (int)SyntaxKind.TypeConstraint;
    public int VariableDeclarator => (int)SyntaxKind.VariableDeclarator;
    public int FieldDeclaration => (int)SyntaxKind.FieldDeclaration;
    public int PropertyDeclaration => (int)SyntaxKind.PropertyDeclaration;
    public int ParameterList => (int)SyntaxKind.ParameterList;
    public int TypeArgumentList => (int)SyntaxKind.TypeArgumentList;
    public int? GlobalStatement => (int)SyntaxKind.GlobalStatement;

    public int ElseClause => (int)SyntaxKind.ElseClause;
    public int EqualsValueClause => (int)SyntaxKind.EqualsValueClause;

    public int? ImplicitElementAccess => (int)SyntaxKind.ImplicitElementAccess;
    public int Interpolation => (int)SyntaxKind.Interpolation;
    public int InterpolatedStringExpression => (int)SyntaxKind.InterpolatedStringExpression;
    public int InterpolatedStringText => (int)SyntaxKind.InterpolatedStringText;
    public int? IndexerMemberCref => (int)SyntaxKind.IndexerMemberCref;
}
