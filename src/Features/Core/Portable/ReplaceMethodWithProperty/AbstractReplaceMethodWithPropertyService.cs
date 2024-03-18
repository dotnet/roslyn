// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty;

internal abstract class AbstractReplaceMethodWithPropertyService<TMethodDeclarationSyntax> where TMethodDeclarationSyntax : SyntaxNode
{
#pragma warning disable CA1822 // Mark members as static - implements interface method for sub-types.
    public async Task<SyntaxNode?> GetMethodDeclarationAsync(CodeRefactoringContext context)
#pragma warning restore CA1822 // Mark members as static
        => await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>().ConfigureAwait(false);

    protected static string? GetWarning(GetAndSetMethods getAndSetMethods)
    {
        if (OverridesMetadataSymbol(getAndSetMethods.GetMethod) ||
            OverridesMetadataSymbol(getAndSetMethods.SetMethod))
        {
            return FeaturesResources.Warning_Method_overrides_symbol_from_metadata;
        }

        return null;
    }

    private static bool OverridesMetadataSymbol(IMethodSymbol method)
    {
        for (var current = method; current != null; current = current.OverriddenMethod)
        {
            if (current.Locations.Any(static loc => loc.IsInMetadata))
            {
                return true;
            }
        }

        return false;
    }

    protected static TPropertyDeclaration SetLeadingTrivia<TPropertyDeclaration>(
        ISyntaxFacts syntaxFacts, GetAndSetMethods getAndSetMethods, TPropertyDeclaration property) where TPropertyDeclaration : SyntaxNode
    {
        var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration;
        var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration;
        var finalLeadingTrivia = getAndSetMethods.GetMethodDeclaration.GetLeadingTrivia().ToList();

        //If there is a comment on the same line as the method it is contained in trailing trivia for the parameter list
        //If it's there we need to add it to the final comments
        //this is to fix issue 42699, https://github.com/dotnet/roslyn/issues/42699
        AddParamListTriviaIfNeeded(syntaxFacts, getMethodDeclaration, finalLeadingTrivia);

        if (setMethodDeclaration == null)
        {
            return property.WithLeadingTrivia(finalLeadingTrivia);
        }

        finalLeadingTrivia.AddRange(
            setMethodDeclaration.GetLeadingTrivia()
                                .SkipWhile(syntaxFacts.IsEndOfLineTrivia)
                                .Where(t => !t.IsDirective));

        //If there is a comment on the same line as the method it is contained in trailing trivia for the parameter list
        //If it's there we need to add it to the final comments
        AddParamListTriviaIfNeeded(syntaxFacts, setMethodDeclaration, finalLeadingTrivia);

        return property.WithLeadingTrivia(finalLeadingTrivia);
    }

    //If there is a comment on the same line as the method it is contained in trailing trivia for the parameter list
    //If it's there we need to add it to the final comments
    private static void AddParamListTriviaIfNeeded(ISyntaxFacts syntaxFacts, SyntaxNode methodDeclaration, List<SyntaxTrivia> finalLeadingTrivia)
    {
        var paramList = syntaxFacts.GetParameterList(methodDeclaration);
        if (paramList != null)
        {
            var trailingTrivia = paramList.GetTrailingTrivia();
            if (trailingTrivia.Any(syntaxFacts.IsRegularComment))
            {
                // we have a meaningful comment on the parameter list so add it to the trivia list
                finalLeadingTrivia.AddRange(trailingTrivia);
            }
        }
    }
}
