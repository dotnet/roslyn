// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

internal static partial class AddImportPlacementOptionsProviders
{
    internal static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, IAddImportsService addImportsService, AddImportPlacementOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
    {
#if CODE_STYLE
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return addImportsService.GetAddImportOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), allowInHiddenRegions: false, fallbackOptions: null);
#else
        return await document.GetAddImportPlacementOptionsAsync(fallbackOptionsProvider, cancellationToken).ConfigureAwait(false);
#endif
    }
}
