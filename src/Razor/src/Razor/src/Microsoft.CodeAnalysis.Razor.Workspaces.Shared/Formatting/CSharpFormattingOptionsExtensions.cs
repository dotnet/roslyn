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
        string newLine)
    {
        return csharpSyntaxFormattingOptions with
        {
            LineFormatting = csharpSyntaxFormattingOptions.LineFormatting with
            {
                UseTabs = !insertSpaces,
                TabSize = tabSize,
                IndentationSize = tabSize,
                NewLine = newLine
            }
        };
    }
}
