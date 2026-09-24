// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat s_testFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        localOptions: SymbolDisplayLocalOptions.IncludeType,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions:
            SymbolDisplayMemberOptions.IncludeAccessibility |
            SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeContainingType |
            SymbolDisplayMemberOptions.IncludeType |
            SymbolDisplayMemberOptions.IncludeRef |
            SymbolDisplayMemberOptions.IncludeExplicitInterface,
        kindOptions:
            SymbolDisplayKindOptions.IncludeMemberKeyword,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeOptionalBrackets |
            SymbolDisplayParameterOptions.IncludeDefaultValue |
            SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeExtensionThis |
            SymbolDisplayParameterOptions.IncludeType |
            SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static string ToTestDisplayString(this ISymbol symbol)
    {
#pragma warning disable RS0030 // Do not use banned APIs
        return symbol.ToDisplayString(s_testFormat);
#pragma warning restore RS0030 // Do not use banned APIs
    }
}
