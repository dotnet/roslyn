// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Diagnostics;

// Note: This type should be kept in sync with WTE's ErrorCodes.cs
internal static class HtmlErrorCodes
{
    // 00xx syntax errors (i.e. cannot parse)
    public const string MissingClosingBracket = "HTML0001";
    public const string MissingElementNameErrorCode = "HTML0002";
    public const string MissingAttributeNameErrorCode = "HTML0003";
    public const string MissingAttributeValueErrorCode = "HTML0004";
    public const string MismatchedAttributeQuotesErrorCode = "HTML0005";

    // 01xx semantic markup errors (i.e. can parse, but not valid language grammar)
    public const string MissingEndTagErrorCode = "HTML0101";
    public const string UnexpectedEndTagErrorCode = "HTML0102";
    public const string DuplicateAttributeErrorCode = "HTML0103";

    public const string ElementMustBeLowercaseErrorCode = "HTML0104";
    public const string InvalidCasingErrorCode = "HTML0105";

    // 02xx schema errors (i.e. valid language but not allowed by schema)
    public const string SeparateClosingTagRequiredErrorCode = "HTML0201";
    public const string ElementMustBeSelfClosingErrorCode = "HTML0202";

    public const string DisallowedAncestorErrorCode = "HTML0203";
    public const string InvalidNestingErrorCode = "HTML0204";
    public const string TooManyElementsErrorCode = "HTML0205";
    public const string TooFewElementsErrorCode = "HTML0206";

    public const string MissingRequiredAttributeErrorCode = "HTML0207";
    public const string MissingCorrespondingAttributeErrorCode = "HTML0208";
    public const string UnknownAttributeValueErrorCode = "HTML0209";

    public const string DuplicateDoctypeErrorCode = "HTML0210";
    public const string InvalidDoctypeErrorCode = "HTML0211";
    public const string UnrecognizedDocTypeErrorCode = "HTML0212";
    public const string UnsupportedDocTypeErrorCode = "HTML0213";

    public const string DeprecatedAttributeErrorCode = "HTML0214";

    public const string ScriptTagDoesNotAllowSrcAndContentErrorCode = "HTML0215";
}
