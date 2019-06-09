﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Classification
{
    public static class ClassificationTypeNames
    {
        /// <summary>
        /// Additive classifications types supply additional context to other classifications.
        /// </summary>
        public static ImmutableArray<string> AdditiveTypeNames { get; } = ImmutableArray.Create(StaticSymbol);

        public const string Comment = "comment";
        public const string ExcludedCode = "excluded code";
        public const string Identifier = "identifier";
        public const string Keyword = "keyword";
        public const string ControlKeyword = "keyword - control";
        public const string NumericLiteral = "number";
        public const string Operator = "operator";
        public const string OperatorOverloaded = "operator - overloaded";
        public const string PreprocessorKeyword = "preprocessor keyword";
        public const string StringLiteral = "string";
        public const string WhiteSpace = "whitespace";
        public const string Text = "text";

        public const string StaticSymbol = "static symbol";

        public const string PreprocessorText = "preprocessor text";
        public const string Punctuation = "punctuation";
        public const string VerbatimStringLiteral = "string - verbatim";
        public const string StringEscapeCharacter = "string - escape character";

        public const string ClassName = "class name";
        public const string DelegateName = "delegate name";
        public const string EnumName = "enum name";
        public const string InterfaceName = "interface name";
        public const string ModuleName = "module name";
        public const string StructName = "struct name";
        public const string TypeParameterName = "type parameter name";

        public const string FieldName = "field name";
        public const string EnumMemberName = "enum member name";
        public const string ConstantName = "constant name";
        public const string LocalName = "local name";
        public const string ParameterName = "parameter name";
        public const string MethodName = "method name";
        public const string ExtensionMethodName = "extension method name";
        public const string PropertyName = "property name";
        public const string EventName = "event name";
        public const string NamespaceName = "namespace name";
        public const string LabelName = "label name";

        public const string XmlDocCommentAttributeName = "xml doc comment - attribute name";
        public const string XmlDocCommentAttributeQuotes = "xml doc comment - attribute quotes";
        public const string XmlDocCommentAttributeValue = "xml doc comment - attribute value";
        public const string XmlDocCommentCDataSection = "xml doc comment - cdata section";
        public const string XmlDocCommentComment = "xml doc comment - comment";
        public const string XmlDocCommentDelimiter = "xml doc comment - delimiter";
        public const string XmlDocCommentEntityReference = "xml doc comment - entity reference";
        public const string XmlDocCommentName = "xml doc comment - name";
        public const string XmlDocCommentProcessingInstruction = "xml doc comment - processing instruction";
        public const string XmlDocCommentText = "xml doc comment - text";

        public const string XmlLiteralAttributeName = "xml literal - attribute name";
        public const string XmlLiteralAttributeQuotes = "xml literal - attribute quotes";
        public const string XmlLiteralAttributeValue = "xml literal - attribute value";
        public const string XmlLiteralCDataSection = "xml literal - cdata section";
        public const string XmlLiteralComment = "xml literal - comment";
        public const string XmlLiteralDelimiter = "xml literal - delimiter";
        public const string XmlLiteralEmbeddedExpression = "xml literal - embedded expression";
        public const string XmlLiteralEntityReference = "xml literal - entity reference";
        public const string XmlLiteralName = "xml literal - name";
        public const string XmlLiteralProcessingInstruction = "xml literal - processing instruction";
        public const string XmlLiteralText = "xml literal - text";

        public const string RegexComment = "regex - comment";
        public const string RegexCharacterClass = "regex - character class";
        public const string RegexAnchor = "regex - anchor";
        public const string RegexQuantifier = "regex - quantifier";
        public const string RegexGrouping = "regex - grouping";
        public const string RegexAlternation = "regex - alternation";
        public const string RegexText = "regex - text";
        public const string RegexSelfEscapedCharacter = "regex - self escaped character";
        public const string RegexOtherEscape = "regex - other escape";
    }
}
