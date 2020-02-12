// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// Provides a uniform view of SyntaxKinds over C# and VB for constructs they have
    /// in common.
    /// </summary>
    internal interface ISyntaxKindsService : ILanguageService
    {
        TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct;

        int ConflictMarkerTrivia { get; }
        int DisabledTextTrivia { get; }
        int EndOfLineTrivia { get; }
        int SkippedTokensTrivia { get; }
        int WhitespaceTrivia { get; }

        int CharacterLiteralToken { get; }
        int DotToken { get; }
        int InterpolatedStringTextToken { get; }
        int QuestionToken { get; }
        int StringLiteralToken { get; }

        int IfKeyword { get; }

        int GenericName { get; }
        int IdentifierName { get; }
        int QualifiedName { get; }

        int TupleType { get; }

        int CharacterLiteralExpression { get; }
        int DefaultLiteralExpression { get; }
        int FalseLiteralExpression { get; }
        int NullLiteralExpression { get; }
        int StringLiteralExpression { get; }
        int TrueLiteralExpression { get; }

        int AnonymousObjectCreationExpression { get; }
        int AwaitExpression { get; }
        int BaseExpression { get; }
        int ConditionalAccessExpression { get; }
        int InvocationExpression { get; }

        /// <summary>
        /// A short-circuiting logical 'and'. In C#, 'LogicalAndExpression'. In VB, 'AndAlsoExpression'.
        /// </summary>
        int LogicalAndExpression { get; }

        /// <summary>
        /// A short-circuiting logical 'or'. In C#, 'LogicalOrExpression'. In VB, 'OrElseExpression'.
        /// </summary>
        int LogicalOrExpression { get; }
        int LogicalNotExpression { get; }

        int ObjectCreationExpression { get; }
        int ParenthesizedExpression { get; }
        int QueryExpression { get; }
        int ReferenceEqualsExpression { get; }
        int ReferenceNotEqualsExpression { get; }
        int SimpleMemberAccessExpression { get; }
        int TernaryConditionalExpression { get; }
        int ThisExpression { get; }
        int TupleExpression { get; }

        int EndOfFileToken { get; }
        int AwaitKeyword { get; }
        int IdentifierToken { get; }
        int GlobalKeyword { get; }
        int IncompleteMember { get; }
        int HashToken { get; }

        int ExpressionStatement { get; }
        int ForEachStatement { get; }
        int LocalDeclarationStatement { get; }
        int LockStatement { get; }
        int ReturnStatement { get; }
        int UsingStatement { get; }

        int Attribute { get; }
        int Parameter { get; }
        int TypeConstraint { get; }
        int VariableDeclarator { get; }

        int TypeArgumentList { get; }
    }
}
