// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal sealed class SemanticTokenTypes
{
    private static readonly string s_markupAttributeQuoteType = "markupAttributeQuote";
    private static readonly string s_markupAttributeType = "markupAttribute";
    private static readonly string s_markupAttributeValueType = "markupAttributeValue";
    private static readonly string s_markupCommentPunctuationType = "markupCommentPunctuation";
    private static readonly string s_markupCommentType = "markupComment";
    private static readonly string s_markupElementType = "markupElement";
    private static readonly string s_markupOperatorType = "markupOperator";
    private static readonly string s_markupTagDelimiterType = "markupTagDelimiter";
    private static readonly string s_markupTextLiteralType = "markupTextLiteral";

    private static readonly string s_razorCommentStarType = "razorCommentStar";
    private static readonly string s_razorCommentTransitionType = "razorCommentTransition";
    private static readonly string s_razorCommentType = "razorComment";
    private static readonly string s_razorComponentAttributeType = "razorComponentAttribute";
    private static readonly string s_razorComponentElementType = "razorComponentElement";
    private static readonly string s_razorDirectiveAttributeType = "razorDirectiveAttribute";
    private static readonly string s_razorDirectiveColonType = "razorDirectiveColon";
    private static readonly string s_razorDirectiveType = "razorDirective";
    private static readonly string s_razorTagHelperAttributeType = "razorTagHelperAttribute";
    private static readonly string s_razorTagHelperElementType = "razorTagHelperElement";
    private static readonly string s_razorTransitionType = "razorTransition";

    public int MarkupAttribute => _tokenTypeMap[s_markupAttributeType];
    public int MarkupAttributeQuote => _tokenTypeMap[s_markupAttributeQuoteType];
    public int MarkupAttributeValue => _tokenTypeMap[s_markupAttributeValueType];
    public int MarkupComment => _tokenTypeMap[s_markupCommentType];
    public int MarkupCommentPunctuation => _tokenTypeMap[s_markupCommentPunctuationType];
    public int MarkupElement => _tokenTypeMap[s_markupElementType];
    public int MarkupOperator => _tokenTypeMap[s_markupOperatorType];
    public int MarkupTagDelimiter => _tokenTypeMap[s_markupTagDelimiterType];
    public int MarkupTextLiteral => _tokenTypeMap[s_markupTextLiteralType];

    public int RazorComment => _tokenTypeMap[s_razorCommentType];
    public int RazorCommentStar => _tokenTypeMap[s_razorCommentStarType];
    public int RazorCommentTransition => _tokenTypeMap[s_razorCommentTransitionType];
    public int RazorComponentAttribute => _tokenTypeMap[s_razorComponentAttributeType];
    public int RazorComponentElement => _tokenTypeMap[s_razorComponentElementType];
    public int RazorDirective => _tokenTypeMap[s_razorDirectiveType];
    public int RazorDirectiveAttribute => _tokenTypeMap[s_razorDirectiveAttributeType];
    public int RazorDirectiveColon => _tokenTypeMap[s_razorDirectiveColonType];
    public int RazorTagHelperAttribute => _tokenTypeMap[s_razorTagHelperAttributeType];
    public int RazorTagHelperElement => _tokenTypeMap[s_razorTagHelperElementType];
    public int RazorTransition => _tokenTypeMap[s_razorTransitionType];

    public string[] All { get; }

    private readonly Dictionary<string, int> _tokenTypeMap;

    public SemanticTokenTypes(string[] tokenTypes)
    {
        var tokenTypeMap = new Dictionary<string, int>();
        foreach (var tokenType in tokenTypes)
        {
            tokenTypeMap.Add(tokenType, tokenTypeMap.Count);
        }

        _tokenTypeMap = tokenTypeMap;

        All = tokenTypes;
    }
}
