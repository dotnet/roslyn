// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

        /// <summary>
        /// <see langword="true"/> if the language requires a "TypeExpression"
        /// (including <see langword="var"/>) to be stated when making a 
        /// <see cref="LocalDeclarationStatement(SyntaxNode, SyntaxToken, SyntaxNode, bool)"/>.
        /// <see langword="false"/> if the language allows the type node to be entirely elided.
        /// </summary>
        internal abstract bool RequiresLocalDeclarationType();

        internal abstract SyntaxToken InterpolatedStringTextToken(string content);
        internal abstract SyntaxNode InterpolatedStringText(SyntaxToken textToken);
        internal abstract SyntaxNode Interpolation(SyntaxNode syntaxNode);
        internal abstract SyntaxNode InterpolatedStringExpression(SyntaxToken startToken, IEnumerable<SyntaxNode> content, SyntaxToken endToken);
        internal abstract SyntaxNode InterpolationAlignmentClause(SyntaxNode alignment);
        internal abstract SyntaxNode InterpolationFormatClause(string format);
        internal abstract SyntaxNode TypeParameterList(IEnumerable<string> typeParameterNames);

        #region Patterns

        internal abstract bool SupportsPatterns(ParseOptions options);
        internal abstract SyntaxNode IsPatternExpression(SyntaxNode expression, SyntaxToken isToken, SyntaxNode pattern);

        internal abstract SyntaxNode AndPattern(SyntaxNode left, SyntaxNode right);
        internal abstract SyntaxNode DeclarationPattern(INamedTypeSymbol type, string name);
        internal abstract SyntaxNode ConstantPattern(SyntaxNode expression);
        internal abstract SyntaxNode NotPattern(SyntaxNode pattern);
        internal abstract SyntaxNode OrPattern(SyntaxNode left, SyntaxNode right);
        internal abstract SyntaxNode ParenthesizedPattern(SyntaxNode pattern);
        internal abstract SyntaxNode TypePattern(SyntaxNode type);

        #endregion
    }
}
