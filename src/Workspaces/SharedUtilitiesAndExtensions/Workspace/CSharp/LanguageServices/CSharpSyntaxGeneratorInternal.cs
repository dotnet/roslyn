// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CSharpSyntaxTokens;

[ExportLanguageService(typeof(SyntaxGeneratorInternal), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
internal sealed class CSharpSyntaxGeneratorInternal() : SyntaxGeneratorInternal
{
    public static readonly SyntaxGeneratorInternal Instance = new CSharpSyntaxGeneratorInternal();

    public override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    public override SyntaxTrivia CarriageReturnLineFeed
        => SyntaxFactory.CarriageReturnLineFeed;

    public override SyntaxTrivia ElasticCarriageReturnLineFeed
        => SyntaxFactory.ElasticCarriageReturnLineFeed;

    public override bool SupportsThrowExpression()
        => true;

    public override bool RequiresExplicitImplementationForInterfaceMembers
        => false;

    public override SyntaxTrivia EndOfLine(string text)
        => SyntaxFactory.EndOfLine(text);

    public override SyntaxTrivia SingleLineComment(string text)
        => SyntaxFactory.Comment("//" + text);

    public override SyntaxNode LocalDeclarationStatement(SyntaxNode? type, SyntaxToken name, SyntaxNode? initializer, bool isConst)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            isConst ? [ConstKeyword] : default,
             VariableDeclaration(type, name, initializer));
    }

    public override SyntaxNode WithInitializer(SyntaxNode variableDeclarator, SyntaxNode initializer)
        => ((VariableDeclaratorSyntax)variableDeclarator).WithInitializer((EqualsValueClauseSyntax)initializer);

    public override SyntaxNode WithPropertyInitializer(SyntaxNode propertyDeclaration, SyntaxNode initializer)
    {
        var property = (PropertyDeclarationSyntax)propertyDeclaration;
        return property
            .WithInitializer((EqualsValueClauseSyntax)initializer)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    public override SyntaxNode EqualsValueClause(SyntaxNode value)
        => EqualsValueClause(EqualsToken, value);

    public override SyntaxNode EqualsValueClause(SyntaxToken operatorToken, SyntaxNode value)
        => SyntaxFactory.EqualsValueClause(operatorToken, (ExpressionSyntax)value);

    internal static VariableDeclarationSyntax VariableDeclaration(SyntaxNode? type, SyntaxToken name, SyntaxNode? expression)
    {
        return SyntaxFactory.VariableDeclaration(
            type == null ? SyntaxFactory.IdentifierName("var") : (TypeSyntax)type,
                [SyntaxFactory.VariableDeclarator(
                    name, argumentList: null,
                    expression == null ? null : SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression))]);
    }

    public override SyntaxToken Identifier(string identifier)
        => SyntaxFactory.Identifier(identifier);

    public override SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
        => SyntaxFactory.ConditionalAccessExpression((ExpressionSyntax)expression, (ExpressionSyntax)whenNotNull);

    public override SyntaxNode MemberBindingExpression(SyntaxNode name)
        => SyntaxFactory.MemberBindingExpression((SimpleNameSyntax)name);

    public override SyntaxNode RefExpression(SyntaxNode expression)
        => SyntaxFactory.RefExpression((ExpressionSyntax)expression);

    public override SyntaxNode AddParentheses(SyntaxNode expressionOrPattern, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        => Parenthesize(expressionOrPattern, includeElasticTrivia, addSimplifierAnnotation);

    internal static SyntaxNode Parenthesize(SyntaxNode expressionOrPattern, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        => expressionOrPattern switch
        {
            ExpressionSyntax expression => expression.Parenthesize(includeElasticTrivia, addSimplifierAnnotation),
            PatternSyntax pattern => pattern.Parenthesize(includeElasticTrivia, addSimplifierAnnotation),
            var other => other,
        };

    public override SyntaxNode YieldReturnStatement(SyntaxNode expression)
        => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)expression);

    /// <summary>
    /// C# always requires a type to be present with a local declaration.  (Even if that type is
    /// <c>var</c>).
    /// </summary>
    public override bool RequiresLocalDeclarationType() => true;

    public override SyntaxNode InterpolatedStringExpression(SyntaxToken startToken, IEnumerable<SyntaxNode> content, SyntaxToken endToken)
        => SyntaxFactory.InterpolatedStringExpression(startToken, [.. content.Cast<InterpolatedStringContentSyntax>()], endToken);

    public override SyntaxNode InterpolatedStringText(SyntaxToken textToken)
        => SyntaxFactory.InterpolatedStringText(textToken);

    public override SyntaxToken InterpolatedStringTextToken(string content, string value)
        => SyntaxFactory.Token(
            [],
            SyntaxKind.InterpolatedStringTextToken,
            content, value,
            []);

    public override SyntaxNode Interpolation(SyntaxNode syntaxNode)
        => SyntaxFactory.Interpolation((ExpressionSyntax)syntaxNode);

    public override SyntaxNode InterpolationAlignmentClause(SyntaxNode alignment)
        => SyntaxFactory.InterpolationAlignmentClause(CommaToken, (ExpressionSyntax)alignment);

    public override SyntaxNode InterpolationFormatClause(string format)
        => SyntaxFactory.InterpolationFormatClause(
            ColonToken,
            SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, format, format, default));

    public override SyntaxNode TypeParameterList(IEnumerable<string> typeParameterNames)
        => SyntaxFactory.TypeParameterList([.. typeParameterNames.Select(SyntaxFactory.TypeParameter)]);

    internal static SyntaxTokenList GetParameterModifiers(
        IParameterSymbol parameter, bool forFunctionPointerReturnParameter = false)
        => GetParameterModifiers(ParameterIsScoped(parameter), parameter.RefKind, parameter.IsParams, forFunctionPointerReturnParameter);

    internal static SyntaxTokenList GetParameterModifiers(
        bool isScoped, RefKind refKind, bool isParams, bool forFunctionPointerReturnParameter = false)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);

        if (isParams)
            result.Add(ParamsKeyword);

        if (isScoped)
            result.Add(ScopedKeyword);

        switch (refKind)
        {
            case RefKind.Out:
                result.Add(OutKeyword);
                break;

            case RefKind.Ref:
                result.Add(RefKeyword);
                break;

            // Note: RefKind.RefReadonly == RefKind.In. Function Pointers must use the correct
            // ref kind syntax when generating for the return parameter vs other parameters.
            // The return parameter must use ref readonly, like regular methods.
            case RefKind.In when !forFunctionPointerReturnParameter:
                result.Add(InKeyword);
                break;

            case RefKind.RefReadOnly when forFunctionPointerReturnParameter:
                result.Add(RefKeyword);
                result.Add(ReadOnlyKeyword);
                break;

            case RefKind.RefReadOnlyParameter:
                result.Add(RefKeyword);
                result.Add(ReadOnlyKeyword);
                break;
        }

        return SyntaxFactory.TokenList(result);
    }

    public override SyntaxNode Type(ITypeSymbol typeSymbol, bool typeContext)
        => typeContext ? typeSymbol.GenerateTypeSyntax() : typeSymbol.GenerateExpressionSyntax();

    public override SyntaxNode NegateEquality(SyntaxGenerator generator, SyntaxNode binaryExpression, SyntaxNode left, BinaryOperatorKind negatedKind, SyntaxNode right)
        => negatedKind switch
        {
            BinaryOperatorKind.Equals => generator.ReferenceEqualsExpression(left, right),
            BinaryOperatorKind.NotEquals => generator.ReferenceNotEqualsExpression(left, right),
            _ => throw ExceptionUtilities.UnexpectedValue(negatedKind),
        };

    public override SyntaxNode IsNotTypeExpression(SyntaxNode expression, SyntaxNode type)
        => throw ExceptionUtilities.Unreachable();

    #region Patterns

    public override bool SupportsPatterns(ParseOptions options)
        => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

    public override SyntaxNode IsPatternExpression(SyntaxNode expression, SyntaxToken isKeyword, SyntaxNode pattern)
        => SyntaxFactory.IsPatternExpression(
            (ExpressionSyntax)expression,
            isKeyword == default ? IsKeyword : isKeyword,
            (PatternSyntax)pattern);

    public override SyntaxNode AndPattern(SyntaxNode left, SyntaxNode right)
        => SyntaxFactory.BinaryPattern(SyntaxKind.AndPattern, (PatternSyntax)ParenthesizeNonSimple(left), (PatternSyntax)ParenthesizeNonSimple(right));

    public override SyntaxNode ConstantPattern(SyntaxNode expression)
        => SyntaxFactory.ConstantPattern((ExpressionSyntax)expression);

    public override SyntaxNode DeclarationPattern(INamedTypeSymbol type, string name)
        => SyntaxFactory.DeclarationPattern(
            type.GenerateTypeSyntax(),
            SyntaxFactory.SingleVariableDesignation(name.ToIdentifierToken()));

    public override SyntaxNode LessThanRelationalPattern(SyntaxNode expression)
        => SyntaxFactory.RelationalPattern(LessThanToken, (ExpressionSyntax)expression);

    public override SyntaxNode LessThanEqualsRelationalPattern(SyntaxNode expression)
        => SyntaxFactory.RelationalPattern(LessThanEqualsToken, (ExpressionSyntax)expression);

    public override SyntaxNode GreaterThanRelationalPattern(SyntaxNode expression)
        => SyntaxFactory.RelationalPattern(GreaterThanToken, (ExpressionSyntax)expression);

    public override SyntaxNode GreaterThanEqualsRelationalPattern(SyntaxNode expression)
        => SyntaxFactory.RelationalPattern(GreaterThanEqualsToken, (ExpressionSyntax)expression);

    public override SyntaxNode NotPattern(SyntaxNode pattern)
        => SyntaxFactory.UnaryPattern(NotKeyword, (PatternSyntax)ParenthesizeNonSimple(pattern));

    public override SyntaxNode OrPattern(SyntaxNode left, SyntaxNode right)
        => SyntaxFactory.BinaryPattern(SyntaxKind.OrPattern, (PatternSyntax)ParenthesizeNonSimple(left), (PatternSyntax)ParenthesizeNonSimple(right));

    public override SyntaxNode ParenthesizedPattern(SyntaxNode pattern)
        => Parenthesize(pattern);

    public override SyntaxNode TypePattern(SyntaxNode type)
        => SyntaxFactory.TypePattern((TypeSyntax)type);

    public override SyntaxNode UnaryPattern(SyntaxToken operatorToken, SyntaxNode pattern)
        => SyntaxFactory.UnaryPattern(operatorToken, (PatternSyntax)ParenthesizeNonSimple(pattern));

    #endregion

    public override SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression)
        => SyntaxFactory.CastExpression((TypeSyntax)type, (ExpressionSyntax)ParenthesizeNonSimple(expression)).WithAdditionalAnnotations(Simplifier.Annotation);

    public override SyntaxNode DefaultExpression(SyntaxNode type)
        => SyntaxFactory.DefaultExpression((TypeSyntax)type).WithAdditionalAnnotations(Simplifier.Annotation);

    public override SyntaxNode DefaultExpression(ITypeSymbol type)
    {
        // If it's just a reference type, then "null" is the default expression for it.  Note:
        // this counts for actual reference type, or a type parameter with a 'class' constraint.
        // Also, if it's a nullable type, then we can use "null".
        if (type.IsReferenceType ||
            type is IPointerTypeSymbol ||
            type.IsNullable())
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0", 0));
        }

        // Default to a "default(<typename>)" expression.
        return DefaultExpression(type.GenerateTypeSyntax());
    }

    public override SyntaxNode TypeExpression(ITypeSymbol typeSymbol, RefKind refKind)
    {
        var type = typeSymbol.GenerateTypeSyntax();
        return refKind switch
        {
            RefKind.Ref => SyntaxFactory.RefType(type),
            RefKind.RefReadOnly => SyntaxFactory.RefType(RefKeyword, ReadOnlyKeyword, type),
            _ => type,
        };
    }

    public override SyntaxNode MemberAccessExpressionWorker(SyntaxNode? expression, SyntaxNode simpleName)
    {
        // can only be null in VB
        Contract.ThrowIfNull(expression);

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ParenthesizeLeft((ExpressionSyntax)expression),
            (SimpleNameSyntax)simpleName);
    }

    /// <summary>
    /// Parenthesize the left hand size of a member access, invocation or element access expression
    /// </summary>
    internal static ExpressionSyntax ParenthesizeLeft(ExpressionSyntax expression)
    {
        if (expression is TypeSyntax ||
            expression.Kind()
                is SyntaxKind.ThisExpression
                or SyntaxKind.BaseExpression
                or SyntaxKind.ParenthesizedExpression
                or SyntaxKind.SimpleMemberAccessExpression
                or SyntaxKind.InvocationExpression
                or SyntaxKind.ElementAccessExpression
                or SyntaxKind.MemberBindingExpression)
        {
            return expression;
        }

        return (ExpressionSyntax)Parenthesize(expression);
    }

    public override SyntaxNode BitwiseOrExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);

    public static SyntaxNode CreateBinaryExpression(SyntaxKind syntaxKind, SyntaxNode left, SyntaxNode right)
        => SyntaxFactory.BinaryExpression(syntaxKind, (ExpressionSyntax)ParenthesizeNonSimple(left), (ExpressionSyntax)ParenthesizeNonSimple(right));

    public override SyntaxNode IdentifierName(string identifier)
        => identifier.ToIdentifierName();

    public override SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression)
        => SyntaxFactory.CastExpression((TypeSyntax)type, (ExpressionSyntax)ParenthesizeNonSimple(expression)).WithAdditionalAnnotations(Simplifier.Annotation);

    internal static SyntaxNode ParenthesizeNonSimple(SyntaxNode expression)
        => expression is IdentifierNameSyntax
            ? expression
            : Parenthesize(expression);
}
