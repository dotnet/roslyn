// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal readonly struct NamingRule(SymbolSpecification symbolSpecification, NamingStyle namingStyle, ReportDiagnostic enforcementLevel)
{
    public readonly SymbolSpecification SymbolSpecification = symbolSpecification;
    public readonly NamingStyle NamingStyle = namingStyle;
    public readonly ReportDiagnostic EnforcementLevel = enforcementLevel;
}
