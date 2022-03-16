// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpSyntaxKinds : ISyntaxKinds
    {
        public static readonly CSharpSyntaxKinds Instance = new();

        protected CSharpSyntaxKinds()
        {
        }

        // Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
        public TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct
            => (TSyntaxKind)(object)(SyntaxKind)kind;

        public int ConflictMarkerTrivia => (int)SyntaxKind.ConflictMarkerTrivia;
        public int DisabledTextTrivia => (int)SyntaxKind.DisabledTextTrivia;
        public int EndOfLineTrivia => (int)SyntaxKind.EndOfLineTrivia;
        public int SkippedTokensTrivia => (int)SyntaxKind.SkippedTokensTrivia;
        public int WhitespaceTrivia => (int)SyntaxKind.WhitespaceTrivia;
        public int SingleLineCommentTrivia => (int)SyntaxKind.SingleLineCommentTrivia;
        public int? MultiLineCommentTrivia => (int)SyntaxKind.MultiLineCommentTrivia;
        public int SingleLineDocCommentTrivia => (int)SyntaxKind.SingleLineDocumentationCommentTrivia;
        public int? MultiLineDocCommentTrivia => (int)SyntaxKind.MultiLineDocumentationCommentTrivia;
        public int? ShebangDirectiveTrivia => (int)SyntaxKind.ShebangDirectiveTrivia;

        public int CloseBraceToken => (int)SyntaxKind.CloseBraceToken;
        public int ColonToken => (int)SyntaxKind.ColonToken;
        public int CharacterLiteralToken => (int)SyntaxKind.CharacterLiteralToken;
        public int DotToken => (int)SyntaxKind.DotToken;
        public int InterpolatedStringTextToken => (int)SyntaxKind.InterpolatedStringTextToken;
        public int QuestionToken => (int)SyntaxKind.QuestionToken;
        public int StringLiteralToken => (int)SyntaxKind.StringLiteralToken;
        public int? SingleLineRawStringLiteralToken => (int)SyntaxKind.SingleLineRawStringLiteralToken;
        public int? MultiLineRawStringLiteralToken => (int)SyntaxKind.MultiLineRawStringLiteralToken;

        public int DelegateKeyword => (int)SyntaxKind.DelegateKeyword;
        public int IfKeyword => (int)SyntaxKind.IfKeyword;
        public int TrueKeyword => (int)SyntaxKind.TrueKeyword;
        public int FalseKeyword => (int)SyntaxKind.FalseKeyword;

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

        public int AnonymousObjectCreationExpression => (int)SyntaxKind.AnonymousObjectCreationExpression;
        public int ArrayCreationExpression => (int)SyntaxKind.ArrayCreationExpression;
        public int AwaitExpression => (int)SyntaxKind.AwaitExpression;
        public int BaseExpression => (int)SyntaxKind.BaseExpression;
        public int ConditionalAccessExpression => (int)SyntaxKind.ConditionalAccessExpression;
        public int ConditionalExpression => (int)SyntaxKind.ConditionalExpression;
        public int? ImplicitArrayCreationExpression => (int)SyntaxKind.ImplicitArrayCreationExpression;
        public int? ImplicitObjectCreationExpression => (int)SyntaxKind.ImplicitObjectCreationExpression;
        public int? IndexExpression => (int)SyntaxKind.IndexExpression;
        public int InvocationExpression => (int)SyntaxKind.InvocationExpression;
        public int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
        public int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
        public int LogicalNotExpression => (int)SyntaxKind.LogicalNotExpression;
        public int ObjectCreationExpression => (int)SyntaxKind.ObjectCreationExpression;
        public int ParenthesizedExpression => (int)SyntaxKind.ParenthesizedExpression;
        public int QueryExpression => (int)SyntaxKind.QueryExpression;
        public int? RangeExpression => (int)SyntaxKind.RangeExpression;
        public int ReferenceEqualsExpression => (int)SyntaxKind.EqualsExpression;
        public int ReferenceNotEqualsExpression => (int)SyntaxKind.NotEqualsExpression;
        public int SimpleMemberAccessExpression => (int)SyntaxKind.SimpleMemberAccessExpression;
        public int TernaryConditionalExpression => (int)SyntaxKind.ConditionalExpression;
        public int ThisExpression => (int)SyntaxKind.ThisExpression;
        public int? ThrowExpression => (int)SyntaxKind.ThrowExpression;
        public int TupleExpression => (int)SyntaxKind.TupleExpression;

        public int EndOfFileToken => (int)SyntaxKind.EndOfFileToken;
        public int AwaitKeyword => (int)SyntaxKind.AwaitKeyword;
        public int AsyncKeyword => (int)SyntaxKind.AsyncKeyword;
        public int IdentifierToken => (int)SyntaxKind.IdentifierToken;
        public int GlobalKeyword => (int)SyntaxKind.GlobalKeyword;
        public int IncompleteMember => (int)SyntaxKind.IncompleteMember;
        public int HashToken => (int)SyntaxKind.HashToken;

        public int ExpressionStatement => (int)SyntaxKind.ExpressionStatement;
        public int ForEachStatement => (int)SyntaxKind.ForEachStatement;
        public int LocalDeclarationStatement => (int)SyntaxKind.LocalDeclarationStatement;
        public int? LocalFunctionStatement => (int)SyntaxKind.LocalFunctionStatement;
        public int LockStatement => (int)SyntaxKind.LockStatement;
        public int ReturnStatement => (int)SyntaxKind.ReturnStatement;
        public int ThrowStatement => (int)SyntaxKind.ThrowStatement;
        public int UsingStatement => (int)SyntaxKind.UsingStatement;

        public int Attribute => (int)SyntaxKind.Attribute;
        public int ClassDeclaration => (int)SyntaxKind.ClassDeclaration;
        public int? RecordDeclaration => (int)SyntaxKind.RecordDeclaration;
        public int? RecordStructDeclaration => (int)SyntaxKind.RecordStructDeclaration;
        public int Parameter => (int)SyntaxKind.Parameter;
        public int TypeConstraint => (int)SyntaxKind.TypeConstraint;
        public int VariableDeclarator => (int)SyntaxKind.VariableDeclarator;
        public int FieldDeclaration => (int)SyntaxKind.FieldDeclaration;
        public int ParameterList => (int)SyntaxKind.ParameterList;
        public int TypeArgumentList => (int)SyntaxKind.TypeArgumentList;
        public int? GlobalStatement => (int)SyntaxKind.GlobalStatement;

        public int EqualsValueClause => (int)SyntaxKind.EqualsValueClause;

        public int Interpolation => (int)SyntaxKind.Interpolation;
        public int InterpolatedStringExpression => (int)SyntaxKind.InterpolatedStringExpression;
        public int InterpolatedStringText => (int)SyntaxKind.InterpolatedStringText;
    }
}
