// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class SymbolDisplayFormats
    {
        public static readonly SymbolDisplayFormat ShortSymbolDisplayFormat = new SymbolDisplayFormat(
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

        public static readonly SymbolDisplayFormat QualifiedTypeAndNamespaceSymbolDisplayFormat = new SymbolDisplayFormat(
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
