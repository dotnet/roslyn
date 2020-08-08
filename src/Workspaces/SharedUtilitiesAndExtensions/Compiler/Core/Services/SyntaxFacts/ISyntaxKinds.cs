// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// Provides a uniform view of SyntaxKinds over C# and VB for constructs they have
    /// in common.
    /// </summary>
    internal interface ISyntaxKinds
    {
        TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct;

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

        #endregion

        #region keywords

        int AwaitKeyword { get; }
        int GlobalKeyword { get; }
        int IfKeyword { get; }
        int? GlobalStatement { get; }

        #endregion

        #region literal tokens

        int CharacterLiteralToken { get; }
        int StringLiteralToken { get; }

        #endregion

        #region tokens

        int CloseBraceToken { get; }
        int ColonToken { get; }
        int DotToken { get; }
        int EndOfFileToken { get; }
        int HashToken { get; }
        int IdentifierToken { get; }
        int InterpolatedStringTextToken { get; }
        int QuestionToken { get; }

        #endregion

        #region names

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
        int StringLiteralExpression { get; }
        int TrueLiteralExpression { get; }

        #endregion

        #region expressions

        int AnonymousObjectCreationExpression { get; }
        int AwaitExpression { get; }
        int BaseExpression { get; }
        int ConditionalAccessExpression { get; }
        int ConditionalExpression { get; }
        int InvocationExpression { get; }
        int LogicalAndExpression { get; }
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

        #endregion

        #region statements

        int ExpressionStatement { get; }
        int ForEachStatement { get; }
        int LocalDeclarationStatement { get; }
        int LockStatement { get; }
        int ReturnStatement { get; }
        int UsingStatement { get; }

        #endregion

        #region members/declarations

        int Attribute { get; }
        int Parameter { get; }
        int TypeConstraint { get; }
        int VariableDeclarator { get; }

        int IncompleteMember { get; }
        int TypeArgumentList { get; }

        #endregion

        #region other

        int Interpolation { get; }

        #endregion
    }
}
