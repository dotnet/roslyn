// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

// Host-neutral options consumed by the shared formatting engine. RazorFormattingOptions is the serialized
// Razor Workspaces contract and includes additional state for features such as paste formatting.
internal readonly record struct FormattingEngineOptions
{
    public bool InsertSpaces { get; init; } = true;
    public int TabSize { get; init; } = 4;
    public string NewLine { get; init; } = Environment.NewLine;
    public bool CodeBlockBraceOnNextLine { get; init; }
    public AttributeIndentStyle AttributeIndentStyle { get; init; } = AttributeIndentStyle.AlignWithFirst;
    public CSharpSyntaxFormattingOptions CSharpSyntaxFormattingOptions { get; init; } = CSharpSyntaxFormattingOptions.Default;

    public FormattingEngineOptions()
    {
    }

    public CSharpSyntaxFormattingOptions GetResolvedCSharpSyntaxFormattingOptions()
        => CSharpSyntaxFormattingOptions.GetResolvedCSharpSyntaxFormattingOptions(InsertSpaces, TabSize, NewLine);
}
