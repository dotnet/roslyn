// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal readonly struct NamingRule(
    SymbolSpecification symbolSpecification,
    NamingStyle namingStyle,
    ReportDiagnostic enforcementLevel) : IEquatable<NamingRule>
{
    public readonly SymbolSpecification SymbolSpecification = symbolSpecification;
    public readonly NamingStyle NamingStyle = namingStyle;
    public readonly ReportDiagnostic EnforcementLevel = enforcementLevel;

    public bool Equals(NamingRule other)
        => SymbolSpecification.ID == other.SymbolSpecification.ID &&
           NamingStyle.ID == other.NamingStyle.ID &&
           EnforcementLevel == other.EnforcementLevel;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is NamingStyle rule && Equals(rule);

    public override int GetHashCode()
        => Hash.Combine(SymbolSpecification.ID.GetHashCode(), Hash.Combine(NamingStyle.ID.GetHashCode(), (int)EnforcementLevel));
}
