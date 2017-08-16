﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    internal abstract class AbstractReplaceMethodWithPropertyService
    {
        protected static string GetWarning(GetAndSetMethods getAndSetMethods)
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
                if (current.Locations.Any(loc => loc.IsInMetadata))
                {
                    return true;
                }
            }

            return false;
        }

        protected static TPropertyDeclaration SetLeadingTrivia<TPropertyDeclaration>(
            ISyntaxFactsService syntaxFacts, GetAndSetMethods getAndSetMethods, TPropertyDeclaration property) where TPropertyDeclaration : SyntaxNode
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration;
            var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration;
            if (setMethodDeclaration == null)
            {
                return property.WithLeadingTrivia(getMethodDeclaration.GetLeadingTrivia());
            }

            var finalLeadingTrivia = getAndSetMethods.GetMethodDeclaration.GetLeadingTrivia().ToList();

            finalLeadingTrivia.AddRange(
                setMethodDeclaration.GetLeadingTrivia()
                                    .SkipWhile(t => syntaxFacts.IsEndOfLineTrivia(t))
                                    .Where(t => !t.IsDirective));

            return property.WithLeadingTrivia(finalLeadingTrivia);
        }
    }
}
