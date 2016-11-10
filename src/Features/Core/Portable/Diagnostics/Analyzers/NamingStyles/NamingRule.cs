// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingRule
    {
        public readonly SymbolSpecification SymbolSpecification;
        public readonly NamingStyle NamingStyle;
        public readonly DiagnosticSeverity EnforcementLevel;

        public NamingRule(SymbolSpecification symbolSpecification, NamingStyle namingStyle, DiagnosticSeverity enforcementLevel)
        {
            SymbolSpecification = symbolSpecification;
            NamingStyle = namingStyle;
            EnforcementLevel = enforcementLevel;
        }

        public bool AppliesTo(ISymbol symbol) 
            => SymbolSpecification.AppliesTo(symbol);

        public bool IsNameCompliant(string name, out string failureReason)
            => NamingStyle.IsNameCompliant(name, out failureReason);
    }
}