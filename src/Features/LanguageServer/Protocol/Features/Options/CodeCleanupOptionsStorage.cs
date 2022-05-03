// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal static class CodeCleanupOptionsStorage
{
    public static ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCodeCleanupOptionsAsync(globalOptions.GetCodeCleanupOptions(document.Project.LanguageServices), cancellationToken);

    public static CodeCleanupOptions GetCodeCleanupOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => new(
            FormattingOptions: globalOptions.GetSyntaxFormattingOptions(languageServices),
            SimplifierOptions: globalOptions.GetSimplifierOptions(languageServices),
            AddImportOptions: AddImportPlacementOptions.Default);
}
