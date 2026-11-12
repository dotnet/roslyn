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
    public CSharpSyntaxFormattingOptions CSharpSyntaxFormattingOptions { get; init; } = CSharpSyntaxFormattingOptions.Default;
    [DataMember(Order = 5)]
    public bool FromPaste { get; init; } = false;
    [DataMember(Order = 6)]
    public bool IgnoreIndentationInScriptOrStyleBlocksWithComplexRazor { get; init; } = true;
    [DataMember(Order = 7)]
    public int IndentSize { get; init; } = 4;

    public RazorFormattingOptions()
    {
    }

    public CSharpSyntaxFormattingOptions GetResolvedCSharpSyntaxFormattingOptions()
        => CSharpSyntaxFormattingOptions.GetResolvedCSharpSyntaxFormattingOptions(
            InsertSpaces, TabSize, IndentSize, CSharpSyntaxFormattingOptions.Default.NewLine);

    /// <summary>
    /// Returns the options to send the Html formatter. It indents by the LSP tab size, so it is given
    /// <see cref="IndentSize"/> to indent like the rest of the document.
    /// </summary>
    public LspFormattingOptions GetHtmlFormattingOptions(LspFormattingOptions requestOptions)
        => new()
        {
            InsertSpaces = requestOptions.InsertSpaces,
            TabSize = IndentSize,
            TrimTrailingWhitespace = requestOptions.TrimTrailingWhitespace,
            InsertFinalNewline = requestOptions.InsertFinalNewline,
            TrimFinalNewlines = requestOptions.TrimFinalNewlines,
            OtherOptions = requestOptions.OtherOptions,
        };

    public LspFormattingOptions ToLspFormattingOptions()
        => new()
        {
            InsertSpaces = InsertSpaces,
            TabSize = TabSize,
        };
}
