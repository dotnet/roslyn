// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal static class RazorDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    // Razor.Language starts at 0, 1000, 2000, 3000. Therefore, we should offset by 500 to ensure we can easily
    // maintain this list of diagnostic descriptors in conjunction with the one in Razor.Language.

    #region General Errors

    // General Errors ID Offset = 500

    #endregion

    #region Language Errors

    // Language Errors ID Offset = 1500

    #endregion

    #region Semantic Errors

    // Semantic Errors ID Offset = 2500

    #endregion

    #region TagHelper Errors

    // TagHelper Errors ID Offset = 3500

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributeNameNullOrEmpty =
        new($"{DiagnosticPrefix}3500",
            CodeAnalysisResources.TagHelper_InvalidAttributeNameNotNullOrEmpty,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidAttributeNameNullOrEmpty(string tagHelperDisplayName, string propertyDisplayName)
        => RazorDiagnostic.Create(TagHelper_InvalidAttributeNameNullOrEmpty, tagHelperDisplayName, propertyDisplayName, TagHelperTypes.HtmlAttributeNameAttribute, TagHelperTypes.HtmlAttributeName.Name);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNotNull =
        new($"{DiagnosticPrefix}3501",
            CodeAnalysisResources.TagHelper_InvalidAttributePrefixNotNull,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNotNull(string tagHelperDisplayName, string propertyDisplayName)
        => RazorDiagnostic.Create(TagHelper_InvalidAttributePrefixNotNull, tagHelperDisplayName, propertyDisplayName, TagHelperTypes.HtmlAttributeNameAttribute, TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix, "IDictionary<string, TValue>");

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNull =
        new($"{DiagnosticPrefix}3502",
            CodeAnalysisResources.TagHelper_InvalidAttributePrefixNull,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNull(string tagHelperDisplayName, string propertyDisplayName)
        => RazorDiagnostic.Create(TagHelper_InvalidAttributePrefixNull, tagHelperDisplayName, propertyDisplayName, TagHelperTypes.HtmlAttributeNameAttribute, TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix, "IDictionary<string, TValue>");

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeCharacter =
        new($"{DiagnosticPrefix}3503",
            CodeAnalysisResources.TagHelper_InvalidRequiredAttributeCharacter,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeCharacter(char invalidCharacter, string requiredAttributes)
        => RazorDiagnostic.Create(TagHelper_InvalidRequiredAttributeCharacter, invalidCharacter, requiredAttributes);

    internal static readonly RazorDiagnosticDescriptor TagHelper_PartialRequiredAttributeOperator =
        new($"{DiagnosticPrefix}3504",
            CodeAnalysisResources.TagHelper_PartialRequiredAttributeOperator,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_PartialRequiredAttributeOperator(char partialOperator, string requiredAttributes)
        => RazorDiagnostic.Create(TagHelper_PartialRequiredAttributeOperator, requiredAttributes, partialOperator);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeOperator =
        new($"{DiagnosticPrefix}3505",
            CodeAnalysisResources.TagHelper_InvalidRequiredAttributeOperator,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeOperator(char invalidOperator, string requiredAttributes)
        => RazorDiagnostic.Create(TagHelper_InvalidRequiredAttributeOperator, invalidOperator, requiredAttributes);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeMismatchedQuotes =
        new($"{DiagnosticPrefix}3506",
            CodeAnalysisResources.TagHelper_InvalidRequiredAttributeMismatchedQuotes,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes(char quote, string requiredAttributes)
        => RazorDiagnostic.Create(TagHelper_InvalidRequiredAttributeMismatchedQuotes, requiredAttributes, quote);

    internal static readonly RazorDiagnosticDescriptor TagHelper_CouldNotFindMatchingEndBrace =
        new($"{DiagnosticPrefix}3507",
            CodeAnalysisResources.TagHelper_CouldNotFindMatchingEndBrace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_CouldNotFindMatchingEndBrace(string requiredAttributes)
        => RazorDiagnostic.Create(TagHelper_CouldNotFindMatchingEndBrace, requiredAttributes);


    #endregion
}
