// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingStylePreferencesInfo
    {
        public ImmutableArray<NamingRule> NamingRules { get; }

        public NamingStylePreferencesInfo(ImmutableArray<NamingRule> namingRules)
        {
            NamingRules = namingRules;
        }

        internal bool TryGetApplicableRule(ISymbol symbol, out NamingRule applicableRule)
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
            
            foreach (var namingRule in NamingRules)
            {
                if (namingRule.AppliesTo(symbol))
                {
                    applicableRule = namingRule;
                    return true;
                }
            }

            applicableRule = null;
            return false;
        }

        private bool IsSymbolNameAnalyzable(ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Method)
            {
                var methodSymbol = symbol as IMethodSymbol;
                if (methodSymbol != null && methodSymbol.MethodKind != MethodKind.Ordinary)
                {
                    return false;
                }
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                var propertySymbol = symbol as IPropertySymbol;
                if (propertySymbol != null && propertySymbol.IsIndexer)
                {
                    return false;
                }
            }

            return true;
        }
    }
}