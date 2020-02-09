// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal sealed class CSharpSyntaxKindsService : AbstractSyntaxKindsService
    {
        public static readonly CSharpSyntaxKindsService Instance = new CSharpSyntaxKindsService();

        private CSharpSyntaxKindsService()
        {
        }

        // Boxing/Unboxing casts from Object to TSyntaxKind will be erased by jit.
        public override TSyntaxKind Convert<TSyntaxKind>(int kind)
            => (TSyntaxKind)(object)(SyntaxKind)kind;

        public override int ConflictMarkerTrivia => (int)SyntaxKind.ConflictMarkerTrivia;
        public override int DisabledTextTrivia => (int)SyntaxKind.DisabledTextTrivia;
        public override int EndOfLineTrivia => (int)SyntaxKind.EndOfLineTrivia;
        public override int SkippedTokensTrivia => (int)SyntaxKind.SkippedTokensTrivia;

        public override int DotToken => (int)SyntaxKind.DotToken;
        public override int InterpolatedStringTextToken => (int)SyntaxKind.InterpolatedStringTextToken;
        public override int QuestionToken => (int)SyntaxKind.QuestionToken;
        public override int StringLiteralToken => (int)SyntaxKind.StringLiteralToken;

        public override int IfKeyword => (int)SyntaxKind.IfKeyword;

        public override int GenericName => (int)SyntaxKind.GenericName;
        public override int IdentifierName => (int)SyntaxKind.IdentifierName;
        public override int QualifiedName => (int)SyntaxKind.QualifiedName;

        public override int TupleType => (int)SyntaxKind.TupleType;

        public override int AnonymousObjectCreationExpression => (int)SyntaxKind.AnonymousObjectCreationExpression;
        public override int AwaitExpression => (int)SyntaxKind.AwaitExpression;
        public override int CharacterLiteralExpression => (int)SyntaxKind.CharacterLiteralExpression;
        public override int ConditionalAccessExpression => (int)SyntaxKind.ConditionalAccessExpression;
        public override int InvocationExpression => (int)SyntaxKind.InvocationExpression;
        public override int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
        public override int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
        public override int ObjectCreationExpression => (int)SyntaxKind.ObjectCreationExpression;
        public override int ParenthesizedExpression => (int)SyntaxKind.ParenthesizedExpression;
        public override int QueryExpression => (int)SyntaxKind.QueryExpression;
        public override int ReferenceEqualsExpression => (int)SyntaxKind.EqualsExpression;
        public override int ReferenceNotEqualsExpression => (int)SyntaxKind.NotEqualsExpression;
        public override int SimpleMemberAccessExpression => (int)SyntaxKind.SimpleMemberAccessExpression;
        public override int StringLiteralExpression => (int)SyntaxKind.StringLiteralExpression;
        public override int TernaryConditionalExpression => (int)SyntaxKind.ConditionalExpression;
        public override int ThisExpression => (int)SyntaxKind.ThisExpression;
        public override int TupleExpression => (int)SyntaxKind.TupleExpression;

        public override int EndOfFileToken => (int)SyntaxKind.EndOfFileToken;
        public override int AwaitKeyword => (int)SyntaxKind.AwaitKeyword;
        public override int IdentifierToken => (int)SyntaxKind.IdentifierToken;
        public override int GlobalKeyword => (int)SyntaxKind.GlobalKeyword;
        public override int IncompleteMember => (int)SyntaxKind.IncompleteMember;
        public override int HashToken => (int)SyntaxKind.HashToken;

        public override int ExpressionStatement => (int)SyntaxKind.ExpressionStatement;
        public override int LocalDeclarationStatement => (int)SyntaxKind.LocalDeclarationStatement;
        public override int LockStatement => (int)SyntaxKind.LockStatement;
        public override int ReturnStatement => (int)SyntaxKind.ReturnStatement;
        public override int UsingStatement => (int)SyntaxKind.UsingStatement;

        public override int Attribute => (int)SyntaxKind.Attribute;
        public override int Parameter => (int)SyntaxKind.Parameter;
        public override int TypeConstraint => (int)SyntaxKind.TypeConstraint;
        public override int VariableDeclarator => (int)SyntaxKind.VariableDeclarator;

        public override int TypeArgumentList => (int)SyntaxKind.TypeArgumentList;
    }
}
