// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an item in the call hierarchy.
/// </summary>
internal sealed class CallHierarchyItem
{
    public CallHierarchyItem(
        ISymbol symbol,
        Project project,
        ImmutableArray<Location> callsites)
    {
        Symbol = symbol;
        Project = project;
        Callsites = callsites;
    }

    /// <summary>
    /// The symbol this item represents.
    /// </summary>
    public ISymbol Symbol { get; }

    /// <summary>
    /// The project containing the symbol.
    /// </summary>
    public Project Project { get; }

    /// <summary>
    /// The locations where this symbol is called from (for incoming calls context).
    /// </summary>
    public ImmutableArray<Location> Callsites { get; }

    /// <summary>
    /// Gets the name to display for this item.
    /// </summary>
    public string GetDisplayName()
        => Symbol.ToDisplayString(MemberNameFormat);

    /// <summary>
    /// Gets the detail text to display for this item.
    /// </summary>
    public string GetDetailText()
    {
        var containingType = Symbol.ContainingType?.ToDisplayString(ContainingTypeFormat);
        var containingNamespace = Symbol.ContainingNamespace?.ToDisplayString(ContainingNamespaceFormat);

        if (!string.IsNullOrEmpty(containingType) && !string.IsNullOrEmpty(containingNamespace))
            return $"{containingType} ({containingNamespace})";

        return containingType ?? containingNamespace ?? string.Empty;
    }

    public static readonly SymbolDisplayFormat MemberNameFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static readonly SymbolDisplayFormat ContainingTypeFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static readonly SymbolDisplayFormat ContainingNamespaceFormat =
       new(
           globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
           typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}
