// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum XmlParseErrorCode
    {
        XML_RefUndefinedEntity_1,
        XML_InvalidCharEntity,
        XML_InvalidUnicodeChar,
        XML_InvalidWhitespace,
        XML_MissingEqualsAttribute,
        XML_StringLiteralNoStartQuote,
        XML_StringLiteralNoEndQuote,
        XML_StringLiteralNonAsciiQuote,
        XML_LessThanInAttributeValue,
        XML_IncorrectComment,
        XML_ElementTypeMatch,
        XML_DuplicateAttribute,
        XML_WhitespaceMissing,
        XML_EndTagNotExpected,
        XML_CDataEndTagNotAllowed,
        XML_EndTagExpected,
        XML_ExpectedIdentifier,
        XML_ExpectedEndOfTag,

        // This is the default case for when we find an unexpected token. It
        // does not correspond to any MSXML error.
        XML_InvalidToken,
        XML_ExpectedEndOfXml,
    }
}
