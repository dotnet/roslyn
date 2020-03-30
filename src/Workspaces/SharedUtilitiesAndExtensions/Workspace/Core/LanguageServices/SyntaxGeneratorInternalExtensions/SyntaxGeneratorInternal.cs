// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// Internal extensions to <see cref="SyntaxGenerator"/>.
    /// 
    /// This interface is available in the shared CodeStyle and Workspaces layer to allow
    /// sharing internal generator methods between them. Once the methods are ready to be
    /// made public APIs, they can be moved to <see cref="SyntaxGenerator"/>.
    /// </summary>
    internal abstract class SyntaxGeneratorInternal : ILanguageService
    {
        internal abstract ISyntaxFacts SyntaxFacts { get; }

        /// <summary>
        /// Creates a statement that declares a single local variable with an optional initializer.
        /// </summary>
        internal abstract SyntaxNode LocalDeclarationStatement(
            SyntaxNode type, SyntaxToken identifier, SyntaxNode initializer = null, bool isConst = false);

        /// <summary>
        /// Creates a statement that declares a single local variable.
        /// </summary>
        internal SyntaxNode LocalDeclarationStatement(SyntaxToken name, SyntaxNode initializer)
            => LocalDeclarationStatement(null, name, initializer);

        internal abstract SyntaxNode WithInitializer(SyntaxNode variableDeclarator, SyntaxNode initializer);

        internal abstract SyntaxNode EqualsValueClause(SyntaxToken operatorToken, SyntaxNode value);

        internal abstract SyntaxToken Identifier(string identifier);

        internal abstract SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull);

        internal abstract SyntaxNode MemberBindingExpression(SyntaxNode name);

        internal abstract SyntaxNode RefExpression(SyntaxNode expression);

        /// <summary>
        /// Wraps with parens.
        /// </summary>
        internal abstract SyntaxNode AddParentheses(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true);

        /// <summary>
        /// Creates a statement that can be used to yield a value from an iterator method.
        /// </summary>
        /// <param name="expression">An expression that can be yielded.</param>
        internal abstract SyntaxNode YieldReturnStatement(SyntaxNode expression);
    }
}
