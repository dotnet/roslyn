// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal interface ISyntaxFormattingOptionsStorage : ILanguageService
{
    SyntaxFormattingOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class SyntaxFormattingOptionsStorage
{
    public static ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetSyntaxFormattingOptionsAsync(globalOptions.GetSyntaxFormattingOptions(document.Project.LanguageServices), cancellationToken);

    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingOptionsStorage>().GetOptions(globalOptions);
}

