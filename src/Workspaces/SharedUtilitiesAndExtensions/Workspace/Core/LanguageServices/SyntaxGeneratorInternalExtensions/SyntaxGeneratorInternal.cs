// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Editing;

/// <summary>
/// Internal extensions to <see cref="SyntaxGenerator"/>.
/// 
/// This interface is available in the shared CodeStyle and Workspaces layer to allow
/// sharing internal generator methods between them. Once the methods are ready to be
/// made public APIs, they can be moved to <see cref="SyntaxGenerator"/>.
/// </summary>
internal abstract class SyntaxGeneratorInternal : ILanguageService
{
    public abstract ISyntaxFacts SyntaxFacts { get; }

    public abstract SyntaxTrivia CarriageReturnLineFeed { get; }
    public abstract SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

    public abstract SyntaxTrivia EndOfLine(string text);
    public abstract SyntaxTrivia SingleLineComment(string text);

    public abstract bool RequiresExplicitImplementationForInterfaceMembers { get; }

    public abstract bool SupportsThrowExpression();

    /// <summary>
    /// Creates a statement that declares a single local variable with an optional initializer.
    /// </summary>
    public abstract SyntaxNode LocalDeclarationStatement(
        SyntaxNode? type, SyntaxToken identifier, SyntaxNode? initializer = null, bool isConst = false);

    /// <summary>
    /// Creates a statement that declares a single local variable.
    /// </summary>
    public SyntaxNode LocalDeclarationStatement(SyntaxToken name, SyntaxNode initializer)
        => LocalDeclarationStatement(null, name, initializer);

    public abstract SyntaxNode WithInitializer(SyntaxNode variableDeclarator, SyntaxNode initializer);

    /// <summary>
    /// Adds an initializer to a property declaration.
    /// </summary>
    public abstract SyntaxNode WithPropertyInitializer(SyntaxNode propertyDeclaration, SyntaxNode initializer);

    public abstract SyntaxNode EqualsValueClause(SyntaxNode value);
    public abstract SyntaxNode EqualsValueClause(SyntaxToken operatorToken, SyntaxNode value);

    public abstract SyntaxToken Identifier(string identifier);

    public abstract SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull);

    public abstract SyntaxNode MemberBindingExpression(SyntaxNode name);

    public abstract SyntaxNode RefExpression(SyntaxNode expression);

    /// <summary>
    /// Wraps with parens.
    /// </summary>
    public abstract SyntaxNode AddParentheses(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true);

    /// <summary>
    /// Creates a statement that can be used to yield a value from an iterator method.
    /// </summary>
    /// <param name="expression">An expression that can be yielded.</param>
    public abstract SyntaxNode YieldReturnStatement(SyntaxNode expression);

    /// <summary>
    /// <see langword="true"/> if the language requires a "TypeExpression"
    /// (including <see langword="var"/>) to be stated when making a 
    /// <see cref="LocalDeclarationStatement(SyntaxNode, SyntaxToken, SyntaxNode, bool)"/>.
    /// <see langword="false"/> if the language allows the type node to be entirely elided.
    /// </summary>
    public abstract bool RequiresLocalDeclarationType();

    public abstract SyntaxToken InterpolatedStringTextToken(string content, string value);
    public abstract SyntaxNode InterpolatedStringText(SyntaxToken textToken);
    public abstract SyntaxNode Interpolation(SyntaxNode syntaxNode);
    public abstract SyntaxNode InterpolatedStringExpression(SyntaxToken startToken, IEnumerable<SyntaxNode> content, SyntaxToken endToken);
    public abstract SyntaxNode InterpolationAlignmentClause(SyntaxNode alignment);
    public abstract SyntaxNode InterpolationFormatClause(string format);
    public abstract SyntaxNode TypeParameterList(IEnumerable<string> typeParameterNames);

    /// <summary>
    /// Produces an appropriate TypeSyntax for the given <see cref="ITypeSymbol"/>.  The <paramref name="typeContext"/>
    /// flag controls how this should be created depending on if this node is intended for use in a type-only
    /// context, or an expression-level context.  In the former case, both C# and VB will create QualifiedNameSyntax
    /// nodes for dotted type names, whereas in the latter case both languages will create MemberAccessExpressionSyntax
    /// nodes.  The final stringified result will be the same in both cases.  However, the structure of the trees
    /// will be substantively different, which can impact how the compilation layers analyze the tree and how
    /// transformational passes affect it.
    /// </summary>
    /// <remarks>
    /// Passing in the right value for <paramref name="typeContext"/> is necessary for correctness and for use
    /// of compilation (and other) layers in a supported fashion.  For example, if a QualifiedTypeSyntax is
    /// sed in a place the compiler would have parsed out a MemberAccessExpression, then it is undefined behavior
    /// what will happen if that tree is passed to any other components.
    /// </remarks>
    public abstract SyntaxNode Type(ITypeSymbol typeSymbol, bool typeContext);

    public abstract SyntaxNode NegateEquality(SyntaxGenerator generator, SyntaxNode binaryExpression, SyntaxNode left, BinaryOperatorKind negatedKind, SyntaxNode right);

    public abstract SyntaxNode IsNotTypeExpression(SyntaxNode expression, SyntaxNode type);

    internal static bool ParameterIsScoped(IParameterSymbol symbol)
        => symbol is { RefKind: RefKind.Ref or RefKind.In or RefKind.RefReadOnlyParameter, ScopedKind: ScopedKind.ScopedRef }
                  or { RefKind: RefKind.None, Type.IsRefLikeType: true, ScopedKind: ScopedKind.ScopedValue };

    #region Patterns

    public abstract bool SupportsPatterns(ParseOptions options);
    public abstract SyntaxNode IsPatternExpression(SyntaxNode expression, SyntaxToken isToken, SyntaxNode pattern);

    public abstract SyntaxNode AndPattern(SyntaxNode left, SyntaxNode right);
    public abstract SyntaxNode ConstantPattern(SyntaxNode expression);
    public abstract SyntaxNode DeclarationPattern(INamedTypeSymbol type, string name);
    public abstract SyntaxNode GreaterThanRelationalPattern(SyntaxNode expression);
    public abstract SyntaxNode GreaterThanEqualsRelationalPattern(SyntaxNode expression);
    public abstract SyntaxNode LessThanRelationalPattern(SyntaxNode expression);
    public abstract SyntaxNode LessThanEqualsRelationalPattern(SyntaxNode expression);
    public abstract SyntaxNode NotPattern(SyntaxNode pattern);
    public abstract SyntaxNode OrPattern(SyntaxNode left, SyntaxNode right);
    public abstract SyntaxNode ParenthesizedPattern(SyntaxNode pattern);
    public abstract SyntaxNode TypePattern(SyntaxNode type);
    public abstract SyntaxNode UnaryPattern(SyntaxToken operatorToken, SyntaxNode pattern);

    #endregion

    public abstract SyntaxNode DefaultExpression(ITypeSymbol type);
    public abstract SyntaxNode DefaultExpression(SyntaxNode type);

    public abstract SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression);

    public SyntaxNode CastExpression(ITypeSymbol type, SyntaxNode expression)
        => CastExpression(TypeExpression(type), expression);

    public SyntaxNode TypeExpression(ITypeSymbol typeSymbol)
        => TypeExpression(typeSymbol, RefKind.None);

    public abstract SyntaxNode TypeExpression(ITypeSymbol typeSymbol, RefKind refKind);

    public abstract SyntaxNode BitwiseOrExpression(SyntaxNode left, SyntaxNode right);

    public SyntaxNode MemberAccessExpression(SyntaxNode? expression, SyntaxNode memberName)
    {
        return MemberAccessExpressionWorker(expression, memberName)
            .WithAdditionalAnnotations(Simplifier.Annotation);
    }

    public abstract SyntaxNode MemberAccessExpressionWorker(SyntaxNode? expression, SyntaxNode memberName);
    public abstract SyntaxNode IdentifierName(string identifier);

    public abstract SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression);
    public SyntaxNode ConvertExpression(ITypeSymbol type, SyntaxNode expression)
        => ConvertExpression(TypeExpression(type), expression);
}
