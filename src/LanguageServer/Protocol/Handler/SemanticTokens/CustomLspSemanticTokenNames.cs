// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

/// <summary>
/// Maps <see cref="ClassificationTypeNames"/> to LSP compatible semantic token names.
/// Since these token names are effectively a public contract that users can directly interact with,
/// we need these names to be nicely formatted and compatible with LSP client rules.
/// All classification names must be explicitly mapped to an LSP semantic token type or
/// a custom token type name defined here.
/// </summary>
internal sealed class CustomLspSemanticTokenNames
{
    public const string ExcludedCode = "excludedCode";
    public const string KeywordControl = "controlKeyword";
    public const string OperatorOverloaded = "operatorOverloaded";
    public const string Whitespace = "whitespace";
    public const string Text = "text";
    public const string PreprocessorText = "preprocessorText";
    public const string Punctuation = "punctuation";
    public const string StringVerbatim = "stringVerbatim";
    public const string StringEscapeCharacter = "stringEscapeCharacter";
    public const string ArrayName = "array";
    public const string PointerName = "pointer";
    public const string FunctionPointerName = "functionPointer";
    public const string RecordClassName = "recordClass";
    public const string DelegateName = "delegate";
    public const string ModuleName = "module";
    public const string RecordStructName = "recordStruct";
    public const string FieldName = "field";
    public const string ConstantName = "constant";
    public const string ExtensionMethodName = "extensionMethod";

    public const string XmlDocCommentAttributeName = "xmlDocCommentAttributeName";
    public const string XmlDocCommentAttributeQuotes = "xmlDocCommentAttributeQuotes";
    public const string XmlDocCommentAttributeValue = "xmlDocCommentAttributeValue";
    public const string XmlDocCommentCDataSection = "xmlDocCommentCDataSection";
    public const string XmlDocCommentComment = "xmlDocCommentComment";
    public const string XmlDocCommentDelimiter = "xmlDocCommentDelimiter";
    public const string XmlDocCommentEntityReference = "xmlDocCommentEntityReference";
    public const string XmlDocCommentName = "xmlDocCommentName";
    public const string XmlDocCommentProcessingInstruction = "xmlDocCommentProcessingInstruction";
    public const string XmlDocCommentText = "xmlDocCommentText";

    public const string XmlLiteralAttributeName = "xmlLiteralAttributeName";
    public const string XmlLiteralAttributeQuotes = "xmlLiteralAttributeQuotes";
    public const string XmlLiteralAttributeValue = "xmlLiteralAttributeValue";
    public const string XmlLiteralCDataSection = "xmlLiteralCDataSection";
    public const string XmlLiteralComment = "xmlLiteralComment";
    public const string XmlLiteralDelimiter = "xmlLiteralDelimiter";
    public const string XmlLiteralEmbeddedExpression = "xmlLiteralEmbeddedExpression";
    public const string XmlLiteralEntityReference = "xmlLiteralEntityReference";
    public const string XmlLiteralName = "xmlLiteralName";
    public const string XmlLiteralProcessingInstruction = "xmlLiteralProcessingInstruction";
    public const string XmlLiteralText = "xmlLiteralText";

    public const string RegexComment = "regexComment";
    public const string RegexCharacterClass = "regexCharacterClass";
    public const string RegexAnchor = "regexAnchor";
    public const string RegexQuantifier = "regexQuantifier";
    public const string RegexGrouping = "regexGrouping";
    public const string RegexAlternation = "regexAlternation";
    public const string RegexText = "regexText";
    public const string RegexSelfEscapedCharacter = "regexSelfEscapedCharacter";
    public const string RegexOtherEscape = "regexOtherEscape";

    public const string JsonComment = "jsonComment";
    public const string JsonNumber = "jsonNumber";
    public const string JsonString = "jsonString";
    public const string JsonKeyword = "jsonKeyword";
    public const string JsonText = "jsonText";
    public const string JsonOperator = "jsonOperator";
    public const string JsonPunctuation = "jsonPunctuation";
    public const string JsonArray = "jsonArray";
    public const string JsonObject = "jsonObject";
    public const string JsonPropertyName = "jsonPropertyName";
    public const string JsonConstructorName = "jsonConstructorName";

    public const string TestCodeMarkdown = "testCodeMarkdown";

    public static ImmutableDictionary<string, string> ClassificationTypeNameToCustomTokenName = new Dictionary<string, string>
    {
        [ClassificationTypeNames.ExcludedCode] = ExcludedCode,
        [ClassificationTypeNames.ControlKeyword] = KeywordControl,
        [ClassificationTypeNames.OperatorOverloaded] = OperatorOverloaded,
        [ClassificationTypeNames.WhiteSpace] = Whitespace,
        [ClassificationTypeNames.Text] = Text,
        [ClassificationTypeNames.PreprocessorText] = PreprocessorText,
        [ClassificationTypeNames.Punctuation] = Punctuation,
        [ClassificationTypeNames.VerbatimStringLiteral] = StringVerbatim,
        [ClassificationTypeNames.StringEscapeCharacter] = StringEscapeCharacter,
        [ClassificationTypeNames.ArrayName] = ArrayName,
        [ClassificationTypeNames.PointerName] = PointerName,
        [ClassificationTypeNames.FunctionPointerName] = FunctionPointerName,
        [ClassificationTypeNames.RecordClassName] = RecordClassName,
        [ClassificationTypeNames.DelegateName] = DelegateName,
        [ClassificationTypeNames.ModuleName] = ModuleName,
        [ClassificationTypeNames.RecordStructName] = RecordStructName,
        [ClassificationTypeNames.FieldName] = FieldName,
        [ClassificationTypeNames.ConstantName] = ConstantName,
        [ClassificationTypeNames.ExtensionMethodName] = ExtensionMethodName,

        [ClassificationTypeNames.XmlDocCommentAttributeName] = XmlDocCommentAttributeName,
        [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = XmlDocCommentAttributeQuotes,
        [ClassificationTypeNames.XmlDocCommentAttributeValue] = XmlDocCommentAttributeValue,
        [ClassificationTypeNames.XmlDocCommentCDataSection] = XmlDocCommentCDataSection,
        [ClassificationTypeNames.XmlDocCommentComment] = XmlDocCommentComment,
        [ClassificationTypeNames.XmlDocCommentDelimiter] = XmlDocCommentDelimiter,
        [ClassificationTypeNames.XmlDocCommentEntityReference] = XmlDocCommentEntityReference,
        [ClassificationTypeNames.XmlDocCommentName] = XmlDocCommentName,
        [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = XmlDocCommentProcessingInstruction,
        [ClassificationTypeNames.XmlDocCommentText] = XmlDocCommentText,

        [ClassificationTypeNames.XmlLiteralAttributeName] = XmlLiteralAttributeName,
        [ClassificationTypeNames.XmlLiteralAttributeQuotes] = XmlLiteralAttributeQuotes,
        [ClassificationTypeNames.XmlLiteralAttributeValue] = XmlLiteralAttributeValue,
        [ClassificationTypeNames.XmlLiteralCDataSection] = XmlLiteralCDataSection,
        [ClassificationTypeNames.XmlLiteralComment] = XmlLiteralComment,
        [ClassificationTypeNames.XmlLiteralDelimiter] = XmlLiteralDelimiter,
        [ClassificationTypeNames.XmlLiteralEmbeddedExpression] = XmlLiteralEmbeddedExpression,
        [ClassificationTypeNames.XmlLiteralEntityReference] = XmlLiteralEntityReference,
        [ClassificationTypeNames.XmlLiteralName] = XmlLiteralName,
        [ClassificationTypeNames.XmlLiteralProcessingInstruction] = XmlLiteralProcessingInstruction,
        [ClassificationTypeNames.XmlLiteralText] = XmlLiteralText,

        [ClassificationTypeNames.RegexComment] = RegexComment,
        [ClassificationTypeNames.RegexCharacterClass] = RegexCharacterClass,
        [ClassificationTypeNames.RegexAnchor] = RegexAnchor,
        [ClassificationTypeNames.RegexQuantifier] = RegexQuantifier,
        [ClassificationTypeNames.RegexGrouping] = RegexGrouping,
        [ClassificationTypeNames.RegexAlternation] = RegexAlternation,
        [ClassificationTypeNames.RegexText] = RegexText,
        [ClassificationTypeNames.RegexSelfEscapedCharacter] = RegexSelfEscapedCharacter,
        [ClassificationTypeNames.RegexOtherEscape] = RegexOtherEscape,

        [ClassificationTypeNames.JsonComment] = JsonComment,
        [ClassificationTypeNames.JsonNumber] = JsonNumber,
        [ClassificationTypeNames.JsonString] = JsonString,
        [ClassificationTypeNames.JsonKeyword] = JsonKeyword,
        [ClassificationTypeNames.JsonText] = JsonText,
        [ClassificationTypeNames.JsonOperator] = JsonOperator,
        [ClassificationTypeNames.JsonPunctuation] = JsonPunctuation,
        [ClassificationTypeNames.JsonArray] = JsonArray,
        [ClassificationTypeNames.JsonObject] = JsonObject,
        [ClassificationTypeNames.JsonPropertyName] = JsonPropertyName,
        [ClassificationTypeNames.JsonConstructorName] = JsonConstructorName,

        [ClassificationTypeNames.TestCodeMarkdown] = TestCodeMarkdown,
    }.ToImmutableDictionary();
}
