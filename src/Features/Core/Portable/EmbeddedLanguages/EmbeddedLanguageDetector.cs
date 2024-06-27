// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages;

internal readonly struct EmbeddedLanguageDetector(
    EmbeddedLanguageInfo info,
    ImmutableArray<string> languageIdentifiers,
    EmbeddedLanguageCommentDetector commentDetector)
{
    private readonly EmbeddedLanguageInfo Info = info;
    private readonly HashSet<string> LanguageIdentifiers = new(languageIdentifiers, StringComparer.OrdinalIgnoreCase);
    private readonly EmbeddedLanguageCommentDetector _commentDetector = commentDetector;

    /// <summary>
    /// Determines if <paramref name="token"/> is an embedded language token.  If the token is, the specific
    /// language indicated will be returned in <paramref name="identifier"/>.  If the token was annotated with a
    /// <c>// lang=id</c> comment, then options present in the comment (e.g. <c>// lang=id,opt1,opt2,...</c> will be
    /// returned through <paramref name="options"/>.
    /// </summary>
    public bool IsEmbeddedLanguageToken(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier,
        out IEnumerable<string>? options)
    {
        if (!IsEmbeddedLanguageTokenWorker(token, semanticModel, cancellationToken, out identifier, out options))
            return false;

        // Only succeed if the comment/attribute references one of the language identifiers we're looking for.
        return LanguageIdentifiers.Contains(identifier);
    }

    public string? TryGetEmbeddedLanguageTokenIdentifier(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        return IsEmbeddedLanguageTokenWorker(token, semanticModel, cancellationToken, out var identifier, out _)
            ? identifier
            : null;
    }

    public bool IsEmbeddedLanguageIdentifier(string identifier)
        => LanguageIdentifiers.Contains(identifier);

    private bool IsEmbeddedLanguageTokenWorker(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier,
        out IEnumerable<string>? options)
    {
        identifier = null;
        options = null;

        if (Info.IsAnyStringLiteral(token.RawKind))
            return IsEmbeddedLanguageStringLiteralToken(token, semanticModel, cancellationToken, out identifier, out options);

        if (token.RawKind == Info.SyntaxKinds.InterpolatedStringTextToken)
        {
            options = null;
            return IsEmbeddedLanguageInterpolatedStringTextToken(token, semanticModel, cancellationToken, out identifier);
        }

        return false;
    }

    private bool HasLanguageComment(
        SyntaxToken token,
        ISyntaxFacts syntaxFacts,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        if (HasLanguageComment(token.GetPreviousToken().TrailingTrivia, syntaxFacts, out identifier, out options))
            return true;

        // Check for the common case of a string literal in a large binary expression.  For example `"..." + "..." +
        // "..."` We never want to consider these as regex/json tokens as processing them would require knowing the
        // contents of every string literal, and having our lexers/parsers somehow stitch them all together.  This is
        // beyond what those systems support (and would only work for constant strings anyways).  This prevents both
        // incorrect results *and* avoids heavy perf hits walking up large binary expressions (often while a caller is
        // themselves walking down such a large expression).
        if (syntaxFacts.IsLiteralExpression(token.Parent) &&
            syntaxFacts.IsBinaryExpression(token.Parent.Parent) &&
            syntaxFacts.SyntaxKinds.AddExpression == token.Parent.Parent.RawKind)
        {
            return false;
        }

        for (var node = token.Parent; node != null; node = node.Parent)
        {
            if (HasLanguageComment(node.GetLeadingTrivia(), syntaxFacts, out identifier, out options))
                return true;

            // Stop walking up once we hit a statement.  We don't need/want statements higher up the parent chain to
            // have any impact on this token.
            if (syntaxFacts.IsStatement(node))
                break;
        }

        return false;
    }

    private bool HasLanguageComment(
        SyntaxTriviaList list,
        ISyntaxFacts syntaxFacts,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        foreach (var trivia in list)
        {
            if (HasLanguageComment(trivia, syntaxFacts, out identifier, out options))
                return true;
        }

        identifier = null;
        options = null;
        return false;
    }

    private bool HasLanguageComment(
        SyntaxTrivia trivia,
        ISyntaxFacts syntaxFacts,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        if (syntaxFacts.IsRegularComment(trivia))
        {
            // Note: ToString on SyntaxTrivia is non-allocating.  It will just return the
            // underlying text that the trivia is already pointing to.
            var text = trivia.ToString();
            if (_commentDetector.TryMatch(text, out identifier, out options))
                return true;
        }

        identifier = null;
        options = null;
        return false;
    }

    private bool IsEmbeddedLanguageInterpolatedStringTextToken(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        // If we have the format string for an interpolation (e.g. `{expr:XXX}`) then see if expr has an
        // implementation if IFormattable.Format(string, ...) and then see if that impl method has a
        // [StringSyntax] attribute on the first parameter.

        identifier = null;
        var syntaxFacts = Info.SyntaxFacts;
        var interpolationFormatClause = token.Parent;
        var interpolation = interpolationFormatClause?.Parent;
        if (interpolation?.RawKind != syntaxFacts.SyntaxKinds.Interpolation)
            return false;

        var expression = syntaxFacts.GetExpressionOfInterpolation(interpolation);
        var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        if (type == null)
            return false;

        var iformattable = type.AllInterfaces.FirstOrDefault(t => t is
        {
            Name: nameof(IFormattable),
            ContainingNamespace:
            {
                Name: nameof(System),
                ContainingNamespace.IsGlobalNamespace: true,
            }
        });
        if (iformattable == null)
            return false;

        var formatMethod = iformattable
            .GetMembers(nameof(IFormattable.ToString))
            .FirstOrDefault(
                m => m is IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_String }, ..] });
        if (formatMethod == null)
            return false;

        var impl = type.FindImplementationForInterfaceMember(formatMethod);
        if (impl is not IMethodSymbol { Parameters.Length: >= 1 } method)
            return false;

        return HasMatchingStringSyntaxAttribute(method.Parameters[0], out identifier);
    }

    private bool IsEmbeddedLanguageStringLiteralToken(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier,
        out IEnumerable<string>? options)
    {
        identifier = null;
        options = null;
        var syntaxFacts = Info.SyntaxFacts;
        if (!syntaxFacts.IsLiteralExpression(token.Parent))
            return false;

        if (HasLanguageComment(token, syntaxFacts, out identifier, out options))
            return true;

        // If we're a string used in a collection initializer, treat this as a lang string if the collection itself
        // is properly annotated.  This is for APIs that do things like DateTime.ParseExact(..., string[] formats, ...);
        var container = TryFindContainer(token);
        if (container is null)
            return false;

        if (syntaxFacts.IsArgument(container.Parent))
        {
            if (IsArgumentWithMatchingStringSyntaxAttribute(semanticModel, container.Parent, cancellationToken, out identifier))
                return true;
        }
        else if (syntaxFacts.IsAttributeArgument(container.Parent))
        {
            if (IsAttributeArgumentWithMatchingStringSyntaxAttribute(semanticModel, container.Parent, cancellationToken, out identifier))
                return true;
        }
        else if (syntaxFacts.IsNamedMemberInitializer(container.Parent))
        {
            syntaxFacts.GetPartsOfNamedMemberInitializer(container.Parent, out var name, out _);
            if (IsFieldOrPropertyWithMatchingStringSyntaxAttribute(semanticModel, name, cancellationToken, out identifier))
                return true;
        }
        else
        {
            var statement = container.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsStatement);
            if (syntaxFacts.IsSimpleAssignmentStatement(statement))
            {
                syntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out var right);
                if (container == right &&
                    IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
                        semanticModel, left, cancellationToken, out identifier))
                {
                    return true;
                }
            }

            if (syntaxFacts.IsEqualsValueClause(container.Parent))
            {
                if (syntaxFacts.IsVariableDeclarator(container.Parent.Parent))
                {
                    var variableDeclarator = container.Parent.Parent;
                    var symbol =
                        semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken) ??
                        semanticModel.GetDeclaredSymbol(syntaxFacts.GetIdentifierOfVariableDeclarator(variableDeclarator).GetRequiredParent(), cancellationToken);

                    if (IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier))
                        return true;
                }
                else if (syntaxFacts.IsEqualsValueOfPropertyDeclaration(container.Parent))
                {
                    var property = container.Parent.GetRequiredParent();
                    var symbol = semanticModel.GetDeclaredSymbol(property, cancellationToken);

                    if (IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier))
                        return true;
                }
            }
        }

        return false;
    }

    private SyntaxNode? TryFindContainer(SyntaxToken token)
    {
        var syntaxFacts = Info.SyntaxFacts;
        var node = syntaxFacts.WalkUpParentheses(token.GetRequiredParent());

        // if we're inside some collection-like initializer, find the instance actually being created. 
        if (syntaxFacts.IsAnyInitializerExpression(node.Parent, out var instance))
            node = syntaxFacts.WalkUpParentheses(instance);

        return node;
    }

    private bool IsAttributeArgumentWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode argument,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        // First, see if this is an `X = "..."` argument that is binding to a field/prop on the attribute.
        var fieldOrProperty = Info.SemanticFacts.FindFieldOrPropertyForAttributeArgument(semanticModel, argument, cancellationToken);
        if (fieldOrProperty != null)
            return HasMatchingStringSyntaxAttribute(fieldOrProperty, out identifier);

        // Otherwise, see if it's a normal named/position argument to the attribute.
        var parameter = Info.SemanticFacts.FindParameterForAttributeArgument(semanticModel, argument, allowUncertainCandidates: true, allowParams: true, cancellationToken);
        return HasMatchingStringSyntaxAttribute(parameter, out identifier);
    }

    private bool IsArgumentWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode argument,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        var fieldOrProperty = Info.SemanticFacts.FindFieldOrPropertyForArgument(semanticModel, argument, cancellationToken);
        if (fieldOrProperty != null)
            return HasMatchingStringSyntaxAttribute(fieldOrProperty, out identifier);

        var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argument, allowUncertainCandidates: true, allowParams: true, cancellationToken);
        return HasMatchingStringSyntaxAttribute(parameter, out identifier);
    }

    private bool IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode left,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        var symbol = semanticModel.GetSymbolInfo(left, cancellationToken).Symbol;
        return IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier);
    }

    private bool IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
        ISymbol? symbol, [NotNullWhen(true)] out string? identifier)
    {
        identifier = null;
        return symbol is IFieldSymbol or IPropertySymbol &&
            HasMatchingStringSyntaxAttribute(symbol, out identifier);
    }

    private bool HasMatchingStringSyntaxAttribute(
        [NotNullWhen(true)] ISymbol? symbol,
        [NotNullWhen(true)] out string? identifier)
    {
        if (symbol != null)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (IsMatchingStringSyntaxAttribute(attribute, out identifier))
                    return true;
            }
        }

        identifier = null;
        return false;
    }

    private bool IsMatchingStringSyntaxAttribute(
        AttributeData attribute,
        [NotNullWhen(true)] out string? identifier)
    {
        identifier = null;
        if (attribute.ConstructorArguments.Length == 0)
            return false;

        if (attribute.AttributeClass is not
            {
                Name: "StringSyntaxAttribute",
                ContainingNamespace:
                {
                    Name: nameof(CodeAnalysis),
                    ContainingNamespace:
                    {
                        Name: nameof(Diagnostics),
                        ContainingNamespace:
                        {
                            Name: nameof(System),
                            ContainingNamespace.IsGlobalNamespace: true,
                        }
                    }
                }
            })
        {
            return false;
        }

        var argument = attribute.ConstructorArguments[0];
        if (argument.Kind != TypedConstantKind.Primitive || argument.Value is not string argString)
            return false;

        identifier = argString;
        return true;
    }

    private static string? GetNameOfType(SyntaxNode? typeNode, ISyntaxFacts syntaxFacts)
    {
        if (syntaxFacts.IsQualifiedName(typeNode))
        {
            return GetNameOfType(syntaxFacts.GetRightSideOfDot(typeNode), syntaxFacts);
        }
        else if (syntaxFacts.IsIdentifierName(typeNode))
        {
            return syntaxFacts.GetIdentifierOfSimpleName(typeNode).ValueText;
        }

        return null;
    }

    private string? GetNameOfInvokedExpression(SyntaxNode invokedExpression)
    {
        var syntaxFacts = Info.SyntaxFacts;
        if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
        {
            return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;
        }
        else if (syntaxFacts.IsIdentifierName(invokedExpression))
        {
            return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;
        }

        return null;
    }
}
