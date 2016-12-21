// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal struct NamingStyleRules
    {
        public ImmutableArray<NamingRule> NamingRules { get; }

        public NamingStyleRules(ImmutableArray<NamingRule> namingRules)
        {
            NamingRules = namingRules;
        }

        internal bool TryGetApplicableRule(ISymbol symbol, out NamingRule applicableRule)
        {
            if (NamingRules != null &&
                IsSymbolNameAnalyzable(symbol))
            {
                foreach (var namingRule in NamingRules)
                {
                    if (namingRule.SymbolSpecification.AppliesTo(symbol))
                    {
                        applicableRule = namingRule;
                        return true;
                    }
                }
            }

            applicableRule = default(NamingRule);
            return false;
        }

        private bool IsSymbolNameAnalyzable(ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Method)
            {
                return ((IMethodSymbol)symbol).MethodKind == MethodKind.Ordinary;
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                return !((IPropertySymbol)symbol).IsIndexer;
            }

            return true;
        }
    }
}