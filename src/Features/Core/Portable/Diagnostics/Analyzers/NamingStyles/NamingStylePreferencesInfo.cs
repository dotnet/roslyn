// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.SymbolCategorization;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingStylePreferencesInfo
    {
        public ImmutableArray<NamingRule> NamingRules { get; }

        public NamingStylePreferencesInfo(ImmutableArray<NamingRule> namingRules)
        {
            NamingRules = namingRules;
        }

        internal bool TryGetApplicableRule(ISymbol symbol, ISymbolCategorizationService categorizationService, out NamingRule applicableRule)
        {
            if (NamingRules == null)
            {
                applicableRule = null;
                return false;
            }

            if (!IsSymbolNameAnalyzable(symbol))
            {
                applicableRule = null;
                return false;
            }

            var matchingRule = NamingRules.FirstOrDefault(r => r.AppliesTo(symbol, categorizationService));
            if (matchingRule == null)
            {
                applicableRule = null;
                return false;
            }

            applicableRule = matchingRule.GetBestMatchingRule(symbol, categorizationService);
            return true;
        }

        private bool IsSymbolNameAnalyzable(ISymbol symbol)
        {
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null && methodSymbol.MethodKind != MethodKind.Ordinary)
            {
                return false;
            }

            return true;
        }
    }
}