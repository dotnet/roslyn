// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static class ReferenceLocationExtensions
{
    public static async Task<Dictionary<ISymbol, List<Location>>> FindReferencingSymbolsAsync(
        this IEnumerable<ReferenceLocation> referenceLocations,
        CancellationToken cancellationToken)
    {
        var documentGroups = referenceLocations.GroupBy(loc => loc.Document);
        var projectGroups = documentGroups.GroupBy(g => g.Key.Project);
        var result = new Dictionary<ISymbol, List<Location>>();

        foreach (var projectGroup in projectGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var project = projectGroup.Key;
            if (project.SupportsCompilation)
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                foreach (var documentGroup in projectGroup)
                {
                    var document = documentGroup.Key;
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    AddSymbols(semanticModel, documentGroup, result);
                }

                // Keep compilation alive so that GetSemanticModelAsync remains cheap
                GC.KeepAlive(compilation);
            }
        }

        return result;
    }

    private static void AddSymbols(
        SemanticModel semanticModel,
        IEnumerable<ReferenceLocation> references,
        Dictionary<ISymbol, List<Location>> result)
    {
        foreach (var reference in references)
        {
            // Filter out references in string literals, nameof expressions, and typeof expressions
            // This fixes the most common Call Hierarchy false positives
            if (IsTokenInStringLiteralNameofOrTypeof(reference))
                continue;
            
            var containingSymbol = GetEnclosingMethodOrPropertyOrField(semanticModel, reference);
            if (containingSymbol != null)
            {
                if (!result.TryGetValue(containingSymbol, out var locations))
                {
                    locations = [];
                    result.Add(containingSymbol, locations);
                }

                locations.Add(reference.Location);
            }
        }
    }

    /// <summary>
    /// Checks if a reference location is in a string literal, nameof expression, or typeof expression.
    /// These are the most common false positives in Call Hierarchy.
    /// </summary>
    private static bool IsTokenInStringLiteralNameofOrTypeof(ReferenceLocation reference)
    {
        if (!reference.Location.IsInSource)
            return false;

        var syntaxTree = reference.Location.SourceTree;
        if (syntaxTree == null)
            return false;

        var root = syntaxTree.GetRoot();
        var token = root.FindToken(reference.Location.SourceSpan.Start, findInsideTrivia: true);

        var document = reference.Document;
        var syntaxFacts = document.GetRequiredLanguageService<Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService>();

        // Check if the token itself is a string literal
        if (syntaxFacts.IsStringLiteralOrInterpolatedStringLiteral(token))
            return true;

        // Walk up the tree to check for string literals, nameof expressions, or typeof expressions
        var current = token.Parent;
        while (current != null)
        {
            // Check if current node is a string literal expression
            if (syntaxFacts.IsLiteralExpression(current))
            {
                var literalToken = syntaxFacts.GetTokenOfLiteralExpression(current);
                if (syntaxFacts.IsStringLiteralOrInterpolatedStringLiteral(literalToken))
                    return true;
            }

            // Check for nameof expressions
            if (IsNameofExpression(syntaxFacts, current))
                return true;

            // Check for typeof expressions
            if (IsTypeOfExpression(syntaxFacts, current))
                return true;

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if a node is a nameof expression.
    /// </summary>
    private static bool IsNameofExpression(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxNode node)
    {
        try
        {
            syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out var argumentList);
            
            if (syntaxFacts.IsSimpleName(expression))
            {
                var identifier = syntaxFacts.GetIdentifierOfSimpleName(expression);
                return identifier.ValueText == "nameof";
            }
        }
        catch
        {
            // If GetPartsOfInvocationExpression throws, this is not an invocation expression
        }

        return false;
    }

    /// <summary>
    /// Checks if a node is a typeof expression.
    /// </summary>
    private static bool IsTypeOfExpression(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxNode node)
    {
        // Check if this is a typeof expression using the syntax kinds
        return node?.RawKind == syntaxFacts.SyntaxKinds.TypeOfExpression;
    }

    private static ISymbol? GetEnclosingMethodOrPropertyOrField(
        SemanticModel semanticModel,
        ReferenceLocation reference)
    {
        var enclosingSymbol = semanticModel.GetEnclosingSymbol(reference.Location.SourceSpan.Start);

        for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
        {
            if (current.Kind == SymbolKind.Field)
            {
                return current;
            }

            if (current.Kind == SymbolKind.Property)
            {
                return current;
            }

            if (current is IMethodSymbol method)
            {
                if (method.IsAccessor())
                {
                    return method.AssociatedSymbol;
                }

                if (method.MethodKind != MethodKind.AnonymousFunction)
                {
                    return method;
                }
            }
        }

        return null;
    }
}
