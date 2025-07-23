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
            // Filter out references in non-reference contexts (string literals, comments, nameof expressions, etc.)
            // This fixes the Call Hierarchy bug where method names in these contexts incorrectly appeared as method calls
            if (IsTokenInExcludedContext(semanticModel, reference))
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
    /// Checks if a reference location is in a context that should be excluded from Call Hierarchy.
    /// This includes string literals, comments, nameof expressions, typeof expressions, and attribute arguments.
    /// </summary>
    private static bool IsTokenInExcludedContext(SemanticModel semanticModel, ReferenceLocation reference)
    {
        if (!reference.Location.IsInSource)
            return false;

        var document = reference.Document;
        var syntaxFacts = document.GetRequiredLanguageService<Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService>();
        
        // Get the syntax tree and find the token at the reference location
        var syntaxTree = reference.Location.SourceTree;
        if (syntaxTree == null)
            return false;

        var root = syntaxTree.GetRoot();
        var token = root.FindToken(reference.Location.SourceSpan.Start, findInsideTrivia: true);

        // Check if the token itself is a string literal token
        if (syntaxFacts.IsStringLiteralOrInterpolatedStringLiteral(token))
            return true;

        // Check if the token is inside structured trivia (like XML documentation comments)
        if (token.IsPartOfStructuredTrivia())
            return true;

        // Walk up the tree to check for various excluded contexts
        var current = token.Parent;
        while (current != null)
        {
            // Check if current node is a string literal expression
            if (syntaxFacts.IsLiteralExpression(current))
            {
                // Get the token of the literal expression to check if it's a string literal
                var literalToken = syntaxFacts.GetTokenOfLiteralExpression(current);
                if (syntaxFacts.IsStringLiteralOrInterpolatedStringLiteral(literalToken))
                    return true;
            }

            // Check if current node is an interpolated string expression
            if (IsInterpolatedStringExpression(syntaxFacts, current))
            {
                // For interpolated strings, we need to check if the token is in the text part
                // (not in an interpolation expression like $"Hello {methodName}")
                // If the token is inside an interpolation, it's a valid reference
                if (IsTokenInInterpolatedStringText(syntaxFacts, token, current))
                    return true;
            }

            // Check for nameof expressions - these should be excluded from call hierarchy
            if (IsNameofExpression(syntaxFacts, current))
                return true;

            // Check for typeof expressions - these should be excluded from call hierarchy  
            if (IsTypeofExpression(syntaxFacts, current, document))
                return true;

            // Check if we're in an attribute argument context
            if (syntaxFacts.IsAttributeArgument(current))
                return true;

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if a node is a nameof expression (language-agnostic).
    /// </summary>
    private static bool IsNameofExpression(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxNode node)
    {
        // Check for invocation expression where the expression is an identifier with text "nameof"
        if (syntaxFacts.GetPartsOfInvocationExpression != null)
        {
            try
            {
                syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out var argumentList);
                
                // Check if the expression is a simple name with the text "nameof"
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
        }

        return false;
    }

    /// <summary>
    /// Checks if a node is a typeof expression (language-agnostic).
    /// </summary>
    private static bool IsTypeofExpression(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxNode node, Document document)
    {
        // For C#, check if it's a TypeOfExpression
        if (document.Project.Language == LanguageNames.CSharp)
        {
            return node.RawKind == syntaxFacts.SyntaxKinds.TypeOfExpression;
        }
        
        // For VB, check if it's a GetType expression (typeof equivalent in VB)
        if (document.Project.Language == LanguageNames.VisualBasic)
        {
            // In VB, GetType is an invocation expression, so we need to check differently
            try
            {
                syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out var argumentList);
                
                // Check if the expression is a simple name with the text "GetType"
                if (syntaxFacts.IsSimpleName(expression))
                {
                    var identifier = syntaxFacts.GetIdentifierOfSimpleName(expression);
                    return identifier.ValueText == "GetType";
                }
            }
            catch
            {
                // If GetPartsOfInvocationExpression throws, this is not an invocation expression
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a node is an interpolated string expression (language-agnostic).
    /// </summary>
    private static bool IsInterpolatedStringExpression(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxNode node)
    {
        // Use SyntaxKinds to check for interpolated string expressions
        var syntaxKinds = syntaxFacts.SyntaxKinds;
        
        // Check for interpolated string expression
        return node.RawKind == syntaxKinds.InterpolatedStringExpression;
    }

    /// <summary>
    /// Checks if a token is within the text portion of an interpolated string (not in an interpolation expression).
    /// </summary>
    private static bool IsTokenInInterpolatedStringText(Microsoft.CodeAnalysis.LanguageService.ISyntaxFactsService syntaxFacts, SyntaxToken token, SyntaxNode interpolatedString)
    {
        try
        {
            // Get all contents of the interpolated string
            var contents = syntaxFacts.GetContentsOfInterpolatedString(interpolatedString);
            
            // Check if our token is inside any of the interpolation expressions
            foreach (var content in contents)
            {
                // If this content contains our token
                if (content.FullSpan.Contains(token.Span))
                {
                    // Try to get the expression of interpolation - if this succeeds, 
                    // it means this content is an interpolation expression
                    try
                    {
                        var expressionNode = syntaxFacts.GetExpressionOfInterpolation(content);
                        if (expressionNode != null && expressionNode.FullSpan.Contains(token.Span))
                        {
                            // Token is inside an interpolation expression, so it's a valid reference
                            return false;
                        }
                    }
                    catch
                    {
                        // If GetExpressionOfInterpolation throws, this content is likely interpolated text,
                        // not an interpolation expression, so the token should be excluded
                    }
                    
                    // Token is in the text part of the interpolated string, so it should be excluded
                    return true;
                }
            }

            // Token is in the text part of the interpolated string (not in any interpolation)
            return true;
        }
        catch
        {
            // If any operation fails, be conservative and exclude the token
            return true;
        }
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
