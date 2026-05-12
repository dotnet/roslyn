// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Settings;

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
    public RazorCSharpSyntaxFormattingOptions? CSharpSyntaxFormattingOptions { get; init; }
    [DataMember(Order = 5)]
    public bool FromPaste { get; init; } = false;

    public RazorFormattingOptions()
    {
    }

    public static RazorFormattingOptions From(FormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
        };

    public static RazorFormattingOptions From(FormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle, RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
        };

    public static RazorFormattingOptions From(FormattingOptions options, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle, RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions, bool fromPaste)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
            FromPaste = fromPaste
        };

    public RazorIndentationOptions ToIndentationOptions()
        => new(
            UseTabs: !InsertSpaces,
            TabSize: TabSize,
            IndentationSize: TabSize);

    public FormattingOptions ToLspFormattingOptions()
        => new()
        {
            InsertSpaces = InsertSpaces,
            TabSize = TabSize,
        };
}
