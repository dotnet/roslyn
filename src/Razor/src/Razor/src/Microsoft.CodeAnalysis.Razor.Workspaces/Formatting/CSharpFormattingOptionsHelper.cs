// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class CSharpFormattingOptionsHelper
{
    internal static CSharpSyntaxFormattingOptions GetCSharpSyntaxFormattingOptions(
        TextDocument razorDocument,
        CancellationToken cancellationToken)
    {
        var configOptions = razorDocument.Project.State
            .GetAnalyzerOptionsForPath(razorDocument.FilePath.AssumeNotNull(), cancellationToken)
            .ConfigOptionsWithFallback;

        return new CSharpSyntaxFormattingOptions(configOptions);
    }

    internal static CSharpSyntaxFormattingOptions GetResolvedCSharpSyntaxFormattingOptions(
        RazorFormattingOptions options)
    {
        var csharpSyntaxFormattingOptions = options.CSharpSyntaxFormattingOptions;

        return csharpSyntaxFormattingOptions with
        {
            LineFormatting = csharpSyntaxFormattingOptions.LineFormatting with
            {
                UseTabs = !options.InsertSpaces,
                TabSize = options.TabSize,
                IndentationSize = options.TabSize,
                NewLine = CSharpSyntaxFormattingOptions.Default.NewLine
            }
        };
    }
}
