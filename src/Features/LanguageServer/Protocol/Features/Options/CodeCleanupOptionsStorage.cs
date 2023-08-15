// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal static class CodeCleanupOptionsStorage
{
    public static ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCodeCleanupOptionsAsync(globalOptions.GetCodeCleanupOptions(document.Project.Services), cancellationToken);

    public static CodeCleanupOptions GetCodeCleanupOptions(this IOptionsReader globalOptions, LanguageServices languageServices)
        => globalOptions.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null);
}
