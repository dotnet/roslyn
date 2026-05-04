// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class SymbolDisplayFormats
    {
        public static readonly SymbolDisplayFormat ShortSymbolDisplayFormat = new(
                SymbolDisplayGlobalNamespaceStyle.Omitted,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
                SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
                SymbolDisplayDelegateStyle.NameAndParameters,
                SymbolDisplayExtensionMethodStyle.InstanceMethod,
                SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeOptionalBrackets | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                SymbolDisplayPropertyStyle.NameOnly,
                SymbolDisplayLocalOptions.IncludeType,
                SymbolDisplayKindOptions.None,
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static readonly SymbolDisplayFormat QualifiedTypeAndNamespaceSymbolDisplayFormat = new(
                SymbolDisplayGlobalNamespaceStyle.Omitted,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
                SymbolDisplayMemberOptions.IncludeContainingType,
                SymbolDisplayDelegateStyle.NameOnly,
                SymbolDisplayExtensionMethodStyle.InstanceMethod,
                SymbolDisplayParameterOptions.None,
                SymbolDisplayPropertyStyle.NameOnly,
                SymbolDisplayLocalOptions.IncludeType,
                SymbolDisplayKindOptions.None,
                SymbolDisplayMiscellaneousOptions.None);
    }
}
