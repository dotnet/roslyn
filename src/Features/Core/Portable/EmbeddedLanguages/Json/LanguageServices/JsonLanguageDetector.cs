// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

/// <summary>
/// Helper class to detect json in string tokens in a document efficiently.
/// </summary>
internal sealed class JsonLanguageDetector(
    EmbeddedLanguageInfo info,
    ISet<INamedTypeSymbol> typesOfInterest)
    : AbstractLanguageDetector<JsonOptions, JsonTree, JsonLanguageDetector, JsonLanguageDetector.JsonInfo>(
        info, LanguageIdentifiers, CommentDetector)
{
    internal readonly struct JsonInfo : ILanguageDetectorInfo<JsonLanguageDetector>
    {
        public ImmutableArray<string> LanguageIdentifiers => ["Json"];

        public JsonLanguageDetector Create(Compilation compilation, EmbeddedLanguageInfo info)
        {
            var types = s_typeNamesOfInterest.Select(compilation.GetTypeByMetadataName).WhereNotNull().ToSet();
            return new JsonLanguageDetector(info, types);
        }
    }

    private const string JsonParameterName = "json";
    private const string ParseMethodName = "Parse";

    private static readonly HashSet<string> s_typeNamesOfInterest =
    [
        "Newtonsoft.Json.Linq.JToken",
        "Newtonsoft.Json.Linq.JObject",
        "Newtonsoft.Json.Linq.JArray",
        "System.Text.Json.JsonDocument",
    ];

    private readonly ISet<INamedTypeSymbol> _typesOfInterest = typesOfInterest;

    /// <summary>
    /// [StringSyntax(Json)] means we're targetting .net, which means we're strict by default if we don't see any
    /// options.
    /// </summary>
    protected override JsonOptions GetStringSyntaxDefaultOptions()
        => JsonOptions.Strict;

    protected override JsonTree? TryParse(VirtualCharSequence chars, JsonOptions options)
        => JsonParser.TryParse(chars, options);

    /// <inheritdoc cref="TryParseString(SyntaxToken, SemanticModel, bool, CancellationToken)"/>
    /// <summary>
    /// If <paramref name="includeProbableStrings"/> is true, then this will also succeed on a string-literal like
    /// <paramref name="token"/> that strongly appears to have JSON in it.  This allows some features to light up
    /// automatically on code that is strongly believed to be JSON, but which is not passed to a known JSON api,
    /// and does not have a comment on it stating it is JSON.
    /// </summary>
    public JsonTree? TryParseString(SyntaxToken token, SemanticModel semanticModel, bool includeProbableStrings, CancellationToken cancellationToken)
    {
        var result = TryParseString(token, semanticModel, cancellationToken);
        if (result != null)
            return result;

        if (includeProbableStrings && IsProbablyJson(token, out var tree))
            return tree;

        return null;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this string-like <paramref name="token"/> is likely a JSON literal.  As
    /// many simple strings are legal JSON (like <c>0</c>) we require enough structure here to feel confident that
    /// this truly is JSON.  Currently, this means it must have at least one <c>{ ... }</c> object literal, and that
    /// literal must have at least one <c>"prop": val</c> property in it.
    /// </summary>
    public bool IsProbablyJson(SyntaxToken token, [NotNullWhen(true)] out JsonTree? tree)
    {
        var chars = this.Info.VirtualCharService.TryConvertToVirtualChars(token);
        tree = JsonParser.TryParse(chars, JsonOptions.Loose);
        if (tree == null || !tree.Diagnostics.IsEmpty)
            return false;

        return ContainsProbableJsonObject(tree.Root);
    }

    private static bool ContainsProbableJsonObject(JsonNode node)
    {
        if (node.Kind == JsonKind.Object)
        {
            var objNode = (JsonObjectNode)node;
            if (objNode.Sequence.Length >= 1)
                return true;
        }

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                if (ContainsProbableJsonObject(child.Node))
                    return true;
            }
        }

        return false;
    }

    protected override bool IsArgumentToWellKnownAPI(
        SyntaxToken token,
        SyntaxNode argumentNode,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out JsonOptions options)
    {
        var syntaxFacts = Info.SyntaxFacts;
        var argumentList = argumentNode.GetRequiredParent();
        var invocationOrCreation = argumentList.Parent;
        if (syntaxFacts.IsInvocationExpression(invocationOrCreation))
        {
            var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
            var name = GetNameOfInvokedExpression(invokedExpression);
            if (syntaxFacts.StringComparer.Equals(name, ParseMethodName))
            {
                // Is a string argument to a method that looks like it could be a json-parsing
                // method. Need to do deeper analysis
                var symbol = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                if (symbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: true } &&
                    _typesOfInterest.Contains(symbol.ContainingType) &&
                    IsArgumentToSuitableParameter(semanticModel, argumentNode, cancellationToken))
                {
                    options = symbol.ContainingType.Name == nameof(JsonDocument) ? JsonOptions.Strict : default;
                    options |= GetOptionsFromSiblingArgument(argumentNode, semanticModel, cancellationToken) ?? default;
                    return true;
                }
            }
        }

        options = default;
        return false;
    }

    protected override bool TryGetOptions(
        SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out JsonOptions options)
    {
        options = default;

        // look for an argument of the form `new JsonDocumentOptions { AllowTrailingCommas = ..., CommentHandling = ... }`

        if (exprType.Name != nameof(JsonDocumentOptions))
            return false;

        // once we see a JsonDocumentOptions, we know this is the .net parser and we should be strict.
        options = JsonOptions.Strict;
        var syntaxFacts = Info.SyntaxFacts;
        expr = syntaxFacts.WalkDownParentheses(expr);
        if (syntaxFacts.IsObjectCreationExpression(expr) ||
            syntaxFacts.IsImplicitObjectCreationExpression(expr))
        {
            syntaxFacts.GetPartsOfBaseObjectCreationExpression(expr, out var argumentList, out var objectInitializer);
            if (syntaxFacts.IsObjectMemberInitializer(objectInitializer))
            {
                var initializers = syntaxFacts.GetInitializersOfObjectMemberInitializer(objectInitializer);
                foreach (var initializer in initializers)
                {
                    if (syntaxFacts.IsNamedMemberInitializer(initializer))
                    {
                        syntaxFacts.GetPartsOfNamedMemberInitializer(initializer, out var name, out var initExpr);
                        var propName = syntaxFacts.GetIdentifierOfIdentifierName(name).ValueText;
                        if (syntaxFacts.StringComparer.Equals(propName, nameof(JsonDocumentOptions.AllowTrailingCommas)) &&
                            semanticModel.GetConstantValue(initExpr).Value is true)
                        {
                            options |= JsonOptions.TrailingCommas;
                        }
                        else if (syntaxFacts.StringComparer.Equals(propName, nameof(JsonDocumentOptions.CommentHandling)) &&
                                 semanticModel.GetConstantValue(initExpr).Value is (byte)JsonCommentHandling.Allow or (byte)JsonCommentHandling.Skip)
                        {
                            options |= JsonOptions.Comments;
                        }
                    }
                }
            }
        }

        return true;
    }

    private bool IsArgumentToSuitableParameter(
        SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
    {
        var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, allowUncertainCandidates: true, allowParams: true, cancellationToken);
        return parameter?.Name == JsonParameterName;
    }
}
