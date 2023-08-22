// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class LineFormattingOptionsStorage
{
    public static ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetLineFormattingOptionsAsync(globalOptions.GetLineFormattingOptions(document.Project.Language), cancellationToken);

    public static LineFormattingOptions GetLineFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => globalOptions.GetLineFormattingOptions(language, fallbackOptions: null);
}

