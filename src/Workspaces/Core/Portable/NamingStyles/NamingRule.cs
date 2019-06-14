// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal readonly struct NamingRule
    {
        public readonly SymbolSpecification SymbolSpecification;
        public readonly NamingStyle NamingStyle;
        public readonly ReportDiagnostic EnforcementLevel;

        public NamingRule(SymbolSpecification symbolSpecification, NamingStyle namingStyle, ReportDiagnostic enforcementLevel)
        {
            SymbolSpecification = symbolSpecification;
            NamingStyle = namingStyle;
            EnforcementLevel = enforcementLevel;
        }
    }
}
