// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.CompilerServices;
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

        int DotToken { get; }
        int QuestionToken { get; }

        int IfKeyword { get; }

        int GenericName { get; }
        int QualifiedName { get; }

        int AnonymousObjectCreationExpression { get; }
        int InvocationExpression { get; }

        /// <summary>
        /// A short-circuiting logical 'and'. In C#, 'LogicalAndExpression'. In VB, 'AndAlsoExpression'.
        /// </summary>
        int LogicalAndExpression { get; }

        /// <summary>
        /// A short-circuiting logical 'or'. In C#, 'LogicalOrExpression'. In VB, 'OrElseExpression'.
        /// </summary>
        int LogicalOrExpression { get; }

        int ObjectCreationExpression { get; }
        int ParenthesizedExpression { get; }
        int QueryExpression { get; }
        int ReferenceEqualsExpression { get; }
        int ReferenceNotEqualsExpression { get; }
        int SimpleMemberAccessExpression { get; }
        int TernaryConditionalExpression { get; }

        int EndOfFileToken { get; }
        int AwaitKeyword { get; }
        int IdentifierToken { get; }
        int GlobalKeyword { get; }
        int IncompleteMember { get; }
        int HashToken { get; }

        int ExpressionStatement { get; }
        int LockStatement { get; }
        int ReturnStatement { get; }
        int UsingStatement { get; }

        int Parameter { get; }
        int VariableDeclarator { get; }
    }

    internal abstract class AbstractSyntaxKindsService : ISyntaxKindsService
    {
        public abstract TSyntaxKind Convert<TSyntaxKind>(int kind) where TSyntaxKind : struct;

        public abstract int ConflictMarkerTrivia { get; }
        public abstract int DisabledTextTrivia { get; }
        public abstract int EndOfLineTrivia { get; }
        public abstract int SkippedTokensTrivia { get; }

        public abstract int DotToken { get; }
        public abstract int QuestionToken { get; }

        public abstract int IfKeyword { get; }

        public abstract int GenericName { get; }
        public abstract int QualifiedName { get; }

        public abstract int AnonymousObjectCreationExpression { get; }
        public abstract int InvocationExpression { get; }
        public abstract int LogicalAndExpression { get; }
        public abstract int LogicalOrExpression { get; }
        public abstract int ObjectCreationExpression { get; }
        public abstract int ParenthesizedExpression { get; }
        public abstract int QueryExpression { get; }
        public abstract int ReferenceEqualsExpression { get; }
        public abstract int ReferenceNotEqualsExpression { get; }
        public abstract int SimpleMemberAccessExpression { get; }
        public abstract int TernaryConditionalExpression { get; }

        public abstract int EndOfFileToken { get; }
        public abstract int IdentifierToken { get; }

        public abstract int AwaitKeyword { get; }
        public abstract int GlobalKeyword { get; }
        public abstract int IncompleteMember { get; }
        public abstract int HashToken { get; }

        public abstract int ExpressionStatement { get; }
        public abstract int LockStatement { get; }
        public abstract int ReturnStatement { get; }
        public abstract int UsingStatement { get; }

        public abstract int Parameter { get; }
        public abstract int VariableDeclarator { get; }
    }
}
