// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Razor.Settings;
using LspFormattingOptions = Roslyn.LanguageServer.Protocol.FormattingOptions;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

[DataContract]
internal readonly record struct RazorFormattingOptions
{
    [DataMember(Order = 0)]
    public bool InsertSpaces { get; init; } = true;
    [DataMember(Order = 1)]
    public int TabSize { get; init; } = 4;
    [DataMember(Order = 2)]
    public bool CodeBlockBraceOnNextLine { get; init; } = false;
    [DataMember(Order = 3)]
    public AttributeIndentStyle AttributeIndentStyle { get; init; } = AttributeIndentStyle.AlignWithFirst;
    [DataMember(Order = 4)]
    public CSharpSyntaxFormattingOptions? CSharpSyntaxFormattingOptions { get; init; }
    [DataMember(Order = 5)]
    public bool FromPaste { get; init; } = false;

    public RazorFormattingOptions()
    {
    }

    public static RazorFormattingOptions From(LspFormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
        };

    public static RazorFormattingOptions From(LspFormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle, CSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
        };

    public static RazorFormattingOptions From(LspFormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle, CSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions, bool fromPaste)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
            FromPaste = fromPaste
        };

    public LspFormattingOptions ToLspFormattingOptions()
        => new()
        {
            InsertSpaces = InsertSpaces,
            TabSize = TabSize,
        };
}
