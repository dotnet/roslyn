// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Classification
{
    internal enum ClassificationTypeKind
    {
        Keyword,
        Identifier,
        Operator,
        Punctuation,

        NumericLiteral,
        StringLiteral,
        VerbatimStringLiteral,

        ClassName,
        DelegateName,
        EnumName,
        InterfaceName,
        ModuleName,
        StructName,
        TypeParameterName,

        PreprocessorKeyword,
        PreprocessorText,

        Comment,
        ExcludedCode,

        XmlDocCommentAttributeName,
        XmlDocCommentAttributeQuotes,
        XmlDocCommentAttributeValue,
        XmlDocCommentCDataSection,
        XmlDocCommentComment,
        XmlDocCommentDelimiter,
        XmlDocCommentEntityReference,
        XmlDocCommentName,
        XmlDocCommentProcessingInstruction,
        XmlDocCommentText,

        XmlLiteralAttributeName,
        XmlLiteralAttributeQuotes,
        XmlLiteralAttributeValue,
        XmlLiteralCDataSection,
        XmlLiteralComment,
        XmlLiteralDelimiter,
        XmlLiteralEmbeddedExpression,
        XmlLiteralEntityReference,
        XmlLiteralName,
        XmlLiteralProcessingInstruction,
        XmlLiteralText,
    }
}