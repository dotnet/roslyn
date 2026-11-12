// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class CSharpFormattingOptionsExtensions
{
    internal static CSharpSyntaxFormattingOptions GetCSharpSyntaxFormattingOptions(
        this TextDocument razorDocument,
        CancellationToken cancellationToken)
    {
        var configOptions = razorDocument.Project.State
            .GetAnalyzerOptionsForPath(razorDocument.FilePath.AssumeNotNull(), cancellationToken)
            .ConfigOptionsWithFallback;

        return new CSharpSyntaxFormattingOptions(configOptions);
    }

    internal static CSharpSyntaxFormattingOptions GetResolvedCSharpSyntaxFormattingOptions(
        this CSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions,
        bool insertSpaces,
        int tabSize,
        int indentSize,
        string newLine)
    {
        return csharpSyntaxFormattingOptions with
        {
            LineFormatting = csharpSyntaxFormattingOptions.LineFormatting with
            {
                UseTabs = !insertSpaces,
                TabSize = tabSize,
                IndentationSize = indentSize,
                NewLine = newLine
            }
        };
    }

    /// <summary>
    /// Returns the number of columns Razor indents by per level. With tabs a level is one tab, so it is the tab size.
    /// A non-positive indent size, which Roslyn accepts from .editorconfig, also falls back to the tab size.
    /// </summary>
    internal static int GetIndentSize(bool insertSpaces, int tabSize, int indentSize)
        => insertSpaces && indentSize > 0 ? indentSize : tabSize;
}
