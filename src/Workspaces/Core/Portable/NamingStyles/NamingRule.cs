// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
