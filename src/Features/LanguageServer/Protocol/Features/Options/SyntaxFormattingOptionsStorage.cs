// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal interface ISyntaxFormattingOptionsStorage : ILanguageService
{
    SyntaxFormattingOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class SyntaxFormattingOptionsStorage
{
    public static SyntaxFormattingOptions.CommonOptions GetCommonSyntaxFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            LineFormatting = globalOptions.GetLineFormattingOptions(language),
            SeparateImportDirectiveGroups = globalOptions.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language),
            AccessibilityModifiersRequired = globalOptions.GetOption(CodeStyleOptions2.AccessibilityModifiersRequired, language).Value
        };

    public static ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetSyntaxFormattingOptionsAsync(globalOptions.GetSyntaxFormattingOptions(document.Project.Services), cancellationToken);

    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingOptionsStorage>().GetOptions(globalOptions);
}

