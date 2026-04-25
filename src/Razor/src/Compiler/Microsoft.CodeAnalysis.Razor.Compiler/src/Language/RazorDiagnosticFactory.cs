// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    #region General Errors

    // General Errors ID Offset = 0

    internal static readonly RazorDiagnosticDescriptor Directive_BlockDirectiveCannotBeImported =
        new($"{DiagnosticPrefix}0000",
            Resources.BlockDirectiveCannotBeImported,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateDirective_BlockDirectiveCannotBeImported(string directive)
        => RazorDiagnostic.Create(Directive_BlockDirectiveCannotBeImported, directive);

    #endregion

    #region Language Errors

    // Language Errors ID Offset = 1000

    internal static readonly RazorDiagnosticDescriptor Parsing_UnterminatedStringLiteral =
        new($"{DiagnosticPrefix}1000",
            Resources.ParseError_Unterminated_String_Literal,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnterminatedStringLiteral(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_UnterminatedStringLiteral, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_BlockCommentNotTerminated =
        new($"{DiagnosticPrefix}1001",
            Resources.ParseError_BlockComment_Not_Terminated,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_BlockCommentNotTerminated(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_BlockCommentNotTerminated, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_HelperDirectiveNotAvailable =
        new($"{DiagnosticPrefix}1002",
            Resources.ParseError_HelperDirectiveNotAvailable,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_HelperDirectiveNotAvailable(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_HelperDirectiveNotAvailable, location, SyntaxConstants.CSharp.HelperKeyword);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedWhiteSpaceAtStartOfCodeBlock =
        new($"{DiagnosticPrefix}1003",
            Resources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_UnexpectedWhiteSpaceAtStartOfCodeBlock, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEndOfFileAtStartOfCodeBlock =
        new($"{DiagnosticPrefix}1004",
            Resources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_UnexpectedEndOfFileAtStartOfCodeBlock, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedCharacterAtStartOfCodeBlock =
        new($"{DiagnosticPrefix}1005",
            Resources.ParseError_Unexpected_Character_At_Start_Of_CodeBlock,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(SourceSpan location, string content)
        => RazorDiagnostic.Create(Parsing_UnexpectedCharacterAtStartOfCodeBlock, location, content);

    internal static readonly RazorDiagnosticDescriptor Parsing_ExpectedEndOfBlockBeforeEOF =
        new($"{DiagnosticPrefix}1006",
            Resources.ParseError_Expected_EndOfBlock_Before_EOF,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_ExpectedEndOfBlockBeforeEOF(SourceSpan location, string blockName, string closeBlock, string openBlock)
        => RazorDiagnostic.Create(Parsing_ExpectedEndOfBlockBeforeEOF, location, blockName, closeBlock, openBlock);

    internal static readonly RazorDiagnosticDescriptor Parsing_ReservedWord =
        new($"{DiagnosticPrefix}1007",
            Resources.ParseError_ReservedWord,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_ReservedWord(SourceSpan location, string content)
        => RazorDiagnostic.Create(Parsing_ReservedWord, location, content);

    internal static readonly RazorDiagnosticDescriptor Parsing_SingleLineControlFlowStatementsNotAllowed =
        new($"{DiagnosticPrefix}1008",
            Resources.ParseError_SingleLine_ControlFlowStatements_CannotContainMarkup,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_SingleLineControlFlowStatementsNotAllowed, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart =
        new($"{DiagnosticPrefix}1009",
            Resources.ParseError_AtInCode_Must_Be_Followed_By_Colon_Paren_Or_Identifier_Start,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedNestedCodeBlock =
        new($"{DiagnosticPrefix}1010",
            Resources.ParseError_Unexpected_Nested_CodeBlock,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedNestedCodeBlock(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_UnexpectedNestedCodeBlock, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveTokensMustBeSeparatedByWhitespace =
        new($"{DiagnosticPrefix}1011",
            Resources.DirectiveTokensMustBeSeparatedByWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveTokensMustBeSeparatedByWhitespace(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveTokensMustBeSeparatedByWhitespace, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEOFAfterDirective =
        new($"{DiagnosticPrefix}1012",
            Resources.UnexpectedEOFAfterDirective,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedEOFAfterDirective(SourceSpan location, string directiveName, string expectedToken)
        => RazorDiagnostic.Create(Parsing_UnexpectedEOFAfterDirective, location, directiveName, expectedToken);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsTypeName =
        new($"{DiagnosticPrefix}1013",
            Resources.DirectiveExpectsTypeName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsTypeName(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsTypeName, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsNamespace =
        new($"{DiagnosticPrefix}1014",
            Resources.DirectiveExpectsNamespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsNamespace(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsNamespace, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsIdentifier =
        new($"{DiagnosticPrefix}1015",
            Resources.DirectiveExpectsIdentifier,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsIdentifier(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsIdentifier, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsQuotedStringLiteral =
        new($"{DiagnosticPrefix}1016",
            Resources.DirectiveExpectsQuotedStringLiteral,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsQuotedStringLiteral(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsQuotedStringLiteral, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedDirectiveLiteral =
        new($"{DiagnosticPrefix}1017",
            Resources.UnexpectedDirectiveLiteral,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedDirectiveLiteral(SourceSpan location, string directiveName, string expected)
        => RazorDiagnostic.Create(Parsing_UnexpectedDirectiveLiteral, location, directiveName, expected);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveMustHaveValue =
        new($"{DiagnosticPrefix}1018",
            Resources.ParseError_DirectiveMustHaveValue,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveMustHaveValue(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveMustHaveValue, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_IncompleteQuotesAroundDirective =
        new($"{DiagnosticPrefix}1019",
            Resources.ParseError_IncompleteQuotesAroundDirective,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_IncompleteQuotesAroundDirective(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_IncompleteQuotesAroundDirective, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_InvalidTagHelperPrefixValue =
        new($"{DiagnosticPrefix}1020",
            Resources.InvalidTagHelperPrefixValue,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_InvalidTagHelperPrefixValue(SourceSpan location, string directiveName, char character, string prefix)
        => RazorDiagnostic.Create(Parsing_InvalidTagHelperPrefixValue, location, directiveName, character, prefix);

    internal static readonly RazorDiagnosticDescriptor Parsing_MarkupBlockMustStartWithTag =
        new($"{DiagnosticPrefix}1021",
            Resources.ParseError_MarkupBlock_Must_Start_With_Tag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_MarkupBlockMustStartWithTag(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_MarkupBlockMustStartWithTag, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_OuterTagMissingName =
        new($"{DiagnosticPrefix}1022",
            Resources.ParseError_OuterTagMissingName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_OuterTagMissingName(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_OuterTagMissingName, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_TextTagCannotContainAttributes =
        new($"{DiagnosticPrefix}1023",
            Resources.ParseError_TextTagCannotContainAttributes,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TextTagCannotContainAttributes(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_TextTagCannotContainAttributes, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnfinishedTag =
        new($"{DiagnosticPrefix}1024",
            Resources.ParseError_UnfinishedTag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnfinishedTag(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_UnfinishedTag, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_MissingEndTag =
        new($"{DiagnosticPrefix}1025",
            Resources.ParseError_MissingEndTag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_MissingEndTag(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_MissingEndTag, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEndTag =
        new($"{DiagnosticPrefix}1026",
            Resources.ParseError_UnexpectedEndTag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedEndTag(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_UnexpectedEndTag, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_ExpectedCloseBracketBeforeEOF =
        new($"{DiagnosticPrefix}1027",
            Resources.ParseError_Expected_CloseBracket_Before_EOF,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_ExpectedCloseBracketBeforeEOF(SourceSpan location, string openBrace, string closeBrace)
        => RazorDiagnostic.Create(Parsing_ExpectedCloseBracketBeforeEOF, location, openBrace, closeBrace);

    internal static readonly RazorDiagnosticDescriptor Parsing_RazorCommentNotTerminated =
        new($"{DiagnosticPrefix}1028",
            Resources.ParseError_RazorComment_Not_Terminated,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_RazorCommentNotTerminated(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_RazorCommentNotTerminated, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperIndexerAttributeNameMustIncludeKey =
        new($"{DiagnosticPrefix}1029",
            Resources.TagHelperBlockRewriter_IndexerAttributeNameMustIncludeKey,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(SourceSpan location, string attributeName, string tagName)
        => RazorDiagnostic.Create(Parsing_TagHelperIndexerAttributeNameMustIncludeKey, location, attributeName, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperAttributeListMustBeWellFormed =
        new($"{DiagnosticPrefix}1030",
            Resources.TagHelperBlockRewriter_TagHelperAttributeListMustBeWellFormed,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperAttributeListMustBeWellFormed(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_TagHelperAttributeListMustBeWellFormed, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelpersCannotHaveCSharpInTagDeclaration =
        new($"{DiagnosticPrefix}1031",
            Resources.TagHelpers_CannotHaveCSharpInTagDeclaration,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_TagHelpersCannotHaveCSharpInTagDeclaration, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperAttributesMustHaveAName =
        new($"{DiagnosticPrefix}1032",
            Resources.TagHelpers_AttributesMustHaveAName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperAttributesMustHaveAName(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_TagHelperAttributesMustHaveAName, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperMustNotHaveAnEndTag =
        new($"{DiagnosticPrefix}1033",
            Resources.TagHelperParseTreeRewriter_EndTagTagHelperMustNotHaveAnEndTag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperMustNotHaveAnEndTag(SourceSpan location, string tagName, string displayName, TagStructure tagStructure)
        => RazorDiagnostic.Create(Parsing_TagHelperMustNotHaveAnEndTag, location, tagName, displayName, tagStructure);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperFoundMalformedTagHelper =
        new($"{DiagnosticPrefix}1034",
            Resources.TagHelpersParseTreeRewriter_FoundMalformedTagHelper,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperFoundMalformedTagHelper(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_TagHelperFoundMalformedTagHelper, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperMissingCloseAngle =
        new($"{DiagnosticPrefix}1035",
            Resources.TagHelpersParseTreeRewriter_MissingCloseAngle,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_TagHelperMissingCloseAngle(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_TagHelperMissingCloseAngle, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_InvalidTagHelperLookupText =
        new($"{DiagnosticPrefix}1036",
            Resources.InvalidTagHelperLookupText,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_InvalidTagHelperLookupText(SourceSpan location, string lookupText)
        => RazorDiagnostic.Create(Parsing_InvalidTagHelperLookupText, location, lookupText);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsCSharpAttribute =
        new($"{DiagnosticPrefix}1037",
            Resources.DirectiveExpectsCSharpAttribute,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsCSharpAttribute(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsCSharpAttribute, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsBooleanLiteral =
        new($"{DiagnosticPrefix}1038",
            Resources.DirectiveExpectsBooleanLiteral,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsBooleanLiteral(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsBooleanLiteral, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_GenericTypeParameterIdentifierMismatch =
        new($"{DiagnosticPrefix}1039",
            Resources.DirectiveGenericTypeParameterIdentifierMismatch,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_GenericTypeParameterIdentifierMismatch(SourceSpan location, string directiveName, string constraintIdentifier, string originalMember)
        => RazorDiagnostic.Create(Parsing_GenericTypeParameterIdentifierMismatch, location, directiveName, constraintIdentifier, originalMember);

    internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedIdentifier =
        new($"{DiagnosticPrefix}1040",
            Resources.ParseError_Unexpected_Identifier_At_Position,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_UnexpectedIdentifier(SourceSpan location, string content, params string[] options)
        => RazorDiagnostic.Create(Parsing_UnexpectedIdentifier, location, content, string.Join(", ", options));

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsIdentifierOrExpression =
        new($"{DiagnosticPrefix}1041",
            Resources.DirectiveExpectsIdentifierOrExpression,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveExpectsIdentifierOrExpression(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveExpectsIdentifierOrExpression, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor Parsing_VoidElement =
        new($"{DiagnosticPrefix}1042",
            Resources.TagHelpersParseTreeRewriter_VoidElement,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_VoidElement(SourceSpan location, string tagName)
        => RazorDiagnostic.Create(Parsing_VoidElement, location, tagName);

    internal static readonly RazorDiagnosticDescriptor Parsing_PreprocessorDirectivesMustBeAtTheStartOfLine =
        new($"{DiagnosticPrefix}1043",
            Resources.Directives_must_be_at_the_start_of_the_line,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_PreprocessorDirectivesMustBeAtTheStartOfLine(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_PreprocessorDirectivesMustBeAtTheStartOfLine, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_PossibleMisplacedPreprocessorDirective =
        new($"{DiagnosticPrefix}1044",
            Resources.Possible_preprocessor_directive_is_misplaced,
            RazorDiagnosticSeverity.Warning);

    public static RazorDiagnostic CreateParsing_PossibleMisplacedPreprocessorDirective(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_PossibleMisplacedPreprocessorDirective, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_DefineAndUndefNotAllowed =
        new($"{DiagnosticPrefix}1045",
            Resources.Define_and_undef_cannot_be_used_in_razor_markup,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DefineAndUndefNotAllowed(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_DefineAndUndefNotAllowed, location);

    #endregion

    #region Semantic Errors

    // Semantic Errors ID Offset = 2000

    internal static readonly RazorDiagnosticDescriptor CodeTarget_UnsupportedExtension =
        new($"{DiagnosticPrefix}2000",
            Resources.Diagnostic_CodeTarget_UnsupportedExtension,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateCodeTarget_UnsupportedExtension(string documentKind, Type extensionType)
        => RazorDiagnostic.Create(CodeTarget_UnsupportedExtension, documentKind, extensionType.Name);

    internal static readonly RazorDiagnosticDescriptor Parsing_DuplicateDirective =
        new($"{DiagnosticPrefix}2001",
            Resources.DuplicateDirective,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DuplicateDirective(SourceSpan location, string directive)
        => RazorDiagnostic.Create(Parsing_DuplicateDirective, location, directive);

    internal static readonly RazorDiagnosticDescriptor Parsing_SectionsCannotBeNested =
        new($"{DiagnosticPrefix}2002",
            Resources.ParseError_Sections_Cannot_Be_Nested,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_SectionsCannotBeNested(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_SectionsCannotBeNested, location, Resources.SectionExample);

    internal static readonly RazorDiagnosticDescriptor Parsing_InlineMarkupBlocksCannotBeNested =
        new($"{DiagnosticPrefix}2003",
            Resources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_InlineMarkupBlocksCannotBeNested(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_InlineMarkupBlocksCannotBeNested, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock =
        new($"{DiagnosticPrefix}2004",
            Resources.ParseError_NamespaceImportAndTypeAlias_Cannot_Exist_Within_CodeBlock,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock(SourceSpan location)
        => RazorDiagnostic.Create(Parsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock, location);

    internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveMustAppearAtStartOfLine =
        new($"{DiagnosticPrefix}2005",
            Resources.DirectiveMustAppearAtStartOfLine,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateParsing_DirectiveMustAppearAtStartOfLine(SourceSpan location, string directiveName)
        => RazorDiagnostic.Create(Parsing_DirectiveMustAppearAtStartOfLine, location, directiveName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_CodeBlocksNotSupportedInAttributes =
        new($"{DiagnosticPrefix}2006",
            Resources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_CodeBlocksNotSupportedInAttributes(SourceSpan? location)
        => RazorDiagnostic.Create(TagHelper_CodeBlocksNotSupportedInAttributes, location);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InlineMarkupBlocksNotSupportedInAttributes =
        new($"{DiagnosticPrefix}2007",
            Resources.TagHelpers_InlineMarkupBlocks_NotSupported_InAttributes,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InlineMarkupBlocksNotSupportedInAttributes(SourceSpan? location, string expectedTypeName)
        => RazorDiagnostic.Create(TagHelper_InlineMarkupBlocksNotSupportedInAttributes, location, expectedTypeName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_EmptyBoundAttribute =
        new($"{DiagnosticPrefix}2008",
            Resources.RewriterError_EmptyTagHelperBoundAttribute,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_EmptyBoundAttribute(SourceSpan location, string attributeName, string tagName, string propertyTypeName)
        => RazorDiagnostic.Create(TagHelper_EmptyBoundAttribute, location, attributeName, tagName, propertyTypeName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_CannotHaveNonTagContent =
        new($"{DiagnosticPrefix}2009",
            Resources.TagHelperParseTreeRewriter_CannotHaveNonTagContent,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_CannotHaveNonTagContent(SourceSpan location, string tagName, string allowedChildren)
        => RazorDiagnostic.Create(TagHelper_CannotHaveNonTagContent, location, tagName, allowedChildren);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidNestedTag =
        new($"{DiagnosticPrefix}2010",
            Resources.TagHelperParseTreeRewriter_InvalidNestedTag,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidNestedTag(SourceSpan location, string tagName, string parent, string allowedChildren)
        => RazorDiagnostic.Create(TagHelper_InvalidNestedTag, location, tagName, parent, allowedChildren);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InconsistentTagStructure =
        new($"{DiagnosticPrefix}2011",
            Resources.TagHelperParseTreeRewriter_InconsistentTagStructure,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InconsistentTagStructure(SourceSpan location, string firstDescriptor, string secondDescriptor, string tagName)
        => RazorDiagnostic.Create(
            TagHelper_InconsistentTagStructure,
            location,
            firstDescriptor,
            secondDescriptor,
            tagName,
            nameof(TagMatchingRuleDescriptor.TagStructure));

    internal static readonly RazorDiagnosticDescriptor Component_EditorRequiredParameterNotSpecified =
        new($"{DiagnosticPrefix}2012",
            Resources.Component_EditorRequiredParameterNotSpecified,
            RazorDiagnosticSeverity.Warning);

    public static RazorDiagnostic CreateComponent_EditorRequiredParameterNotSpecified(SourceSpan? location, string tagName, string parameterName)
        => RazorDiagnostic.Create(Component_EditorRequiredParameterNotSpecified, location, tagName, parameterName);

    #endregion

    #region TagHelper Errors

    // TagHelper Errors ID Offset = 3000

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChildNullOrWhitespace =
        new($"{DiagnosticPrefix}3000",
            Resources.TagHelper_InvalidRestrictedChildNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(string tagHelperDisplayName)
        => RazorDiagnostic.Create(TagHelper_InvalidRestrictedChildNullOrWhitespace, tagHelperDisplayName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChild =
        new($"{DiagnosticPrefix}3001",
            Resources.TagHelper_InvalidRestrictedChild,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChild(string tagHelperDisplayName, string restrictedChild, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidRestrictedChild, tagHelperDisplayName, restrictedChild, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNullOrWhitespace =
        new($"{DiagnosticPrefix}3002",
            Resources.TagHelper_InvalidBoundAttributeNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(string tagHelperDisplayName, string propertyDisplayName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributeNullOrWhitespace, tagHelperDisplayName, propertyDisplayName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeName =
        new($"{DiagnosticPrefix}3003",
            Resources.TagHelper_InvalidBoundAttributeName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeName(string tagHelperDisplayName, string propertyDisplayName, string invalidName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributeName, tagHelperDisplayName, propertyDisplayName, invalidName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNameStartsWith =
        new($"{DiagnosticPrefix}3004",
            Resources.TagHelper_InvalidBoundAttributeNameStartsWith,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNameStartsWith(string tagHelperDisplayName, string propertyDisplayName, string invalidName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributeNameStartsWith, tagHelperDisplayName, propertyDisplayName, invalidName, "data-");

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefix =
        new($"{DiagnosticPrefix}3005",
            Resources.TagHelper_InvalidBoundAttributePrefix,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefix(string tagHelperDisplayName, string propertyDisplayName, string invalidName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributePrefix, tagHelperDisplayName, propertyDisplayName, invalidName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefixStartsWith =
        new($"{DiagnosticPrefix}3006",
            Resources.TagHelper_InvalidBoundAttributePrefixStartsWith,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefixStartsWith(string tagHelperDisplayName, string propertyDisplayName, string invalidName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributePrefixStartsWith, tagHelperDisplayName, propertyDisplayName, invalidName, "data-");

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagNameNullOrWhitespace =
        new($"{DiagnosticPrefix}3007",
            Resources.TagHelper_InvalidTargetedTagNameNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace()
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedTagNameNullOrWhitespace);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagName =
        new($"{DiagnosticPrefix}3008",
            Resources.TagHelper_InvalidTargetedTagName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagName(string invalidTagName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedTagName, invalidTagName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagNameNullOrWhitespace =
        new($"{DiagnosticPrefix}3009",
            Resources.TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace()
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedParentTagNameNullOrWhitespace);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagName =
        new($"{DiagnosticPrefix}3010",
            Resources.TagHelper_InvalidTargetedParentTagName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagName(string invalidTagName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedParentTagName, invalidTagName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeNameNullOrWhitespace =
        new($"{DiagnosticPrefix}3011",
            Resources.TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace()
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedAttributeNameNullOrWhitespace);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeName =
        new($"{DiagnosticPrefix}3012",
            Resources.TagHelper_InvalidTargetedAttributeName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeName(string invalidAttributeName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidTargetedAttributeName, invalidAttributeName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeParameterNullOrWhitespace =
        new($"{DiagnosticPrefix}3013",
            Resources.TagHelper_InvalidBoundAttributeParameterNullOrWhitespace,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(string attributeName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributeParameterNullOrWhitespace, attributeName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeParameterName =
        new($"{DiagnosticPrefix}3014",
            Resources.TagHelper_InvalidBoundAttributeParameterName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeParameterName(string attributeName, string invalidName, char invalidCharacter)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundAttributeParameterName, attributeName, invalidName, invalidCharacter);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundDirectiveAttributeName =
        new($"{DiagnosticPrefix}3015",
            Resources.TagHelper_InvalidBoundDirectiveAttributeName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundDirectiveAttributeName(string tagHelperDisplayName, string propertyDisplayName, string invalidName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundDirectiveAttributeName, tagHelperDisplayName, propertyDisplayName, invalidName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredDirectiveAttributeName =
        new($"{DiagnosticPrefix}3016",
            Resources.TagHelper_InvalidRequiredDirectiveAttributeName,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidRequiredDirectiveAttributeName(string propertyDisplayName, string invalidName)
        => RazorDiagnostic.Create(TagHelper_InvalidRequiredDirectiveAttributeName, propertyDisplayName, invalidName);

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundDirectiveAttributePrefix =
        new($"{DiagnosticPrefix}3017",
            Resources.TagHelper_InvalidBoundDirectiveAttributePrefix,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateTagHelper_InvalidBoundDirectiveAttributePrefix(string tagHelperDisplayName, string propertyDisplayName, string invalidName)
        => RazorDiagnostic.Create(TagHelper_InvalidBoundDirectiveAttributePrefix, tagHelperDisplayName, propertyDisplayName, invalidName);

    #endregion

    #region Rewriter Errors

    // Rewriter Errors ID Offset = 4000

    internal static readonly RazorDiagnosticDescriptor Rewriter_InsufficientStack =
        new($"{DiagnosticPrefix}4000",
            Resources.Rewriter_InsufficientStack,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateRewriter_InsufficientStack(SourceSpan? location = null)
        => RazorDiagnostic.Create(Rewriter_InsufficientStack, location);

    #endregion

    #region "CSS Rewriter Errors"

    // CSS Rewriter Errors ID Offset = 5000

    internal static readonly RazorDiagnosticDescriptor CssRewriting_ImportNotAllowed =
        new($"{DiagnosticPrefix}5000",
            Resources.CssRewriter_ImportNotAllowed,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateCssRewriting_ImportNotAllowed(SourceSpan location)
        => RazorDiagnostic.Create(CssRewriting_ImportNotAllowed, location);

    #endregion
}
