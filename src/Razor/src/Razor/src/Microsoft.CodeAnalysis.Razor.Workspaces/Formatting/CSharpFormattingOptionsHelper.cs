// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class CSharpFormattingOptionsHelper
{
    internal static CSharpSyntaxFormattingOptions GetCSharpSyntaxFormattingOptions(SolutionServices services)
        => (CSharpSyntaxFormattingOptions)services.ExportProvider.GetService<IGlobalOptionService>()
            .GetSyntaxFormattingOptions(services.GetLanguageServices(LanguageNames.CSharp));

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
