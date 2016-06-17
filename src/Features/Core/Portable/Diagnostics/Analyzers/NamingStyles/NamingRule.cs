// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.SymbolCategorization;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingRule
    {
        public readonly string Title;
        public readonly ImmutableArray<NamingRule> Children;
        public readonly SymbolSpecification SymbolSpecification;
        public readonly NamingStyle NamingStyle;
        public readonly DiagnosticSeverity EnforcementLevel;

        public NamingRule(string title, ImmutableArray<NamingRule> children, SymbolSpecification symbolSpecification, NamingStyle namingStyle, DiagnosticSeverity enforcementLevel)
        {
            Title = title;
            Children = children;
            SymbolSpecification = symbolSpecification;
            NamingStyle = namingStyle;
            EnforcementLevel = enforcementLevel;
        }

        public bool AppliesTo(ISymbol symbol, ISymbolCategorizationService categorizationService)
        {
            return SymbolSpecification.AppliesTo(symbol, categorizationService);
        }

        internal NamingRule GetBestMatchingRule(ISymbol symbol, ISymbolCategorizationService categorizationService)
        {
            Debug.Assert(SymbolSpecification.AppliesTo(symbol, categorizationService));
            var matchingChild = Children.FirstOrDefault(r => r.AppliesTo(symbol, categorizationService));
            return matchingChild?.GetBestMatchingRule(symbol, categorizationService) ?? this;
        }

        public bool IsNameCompliant(string name, out string failureReason)
        {
            return NamingStyle.IsNameCompliant(name, out failureReason);
        }
    }
}