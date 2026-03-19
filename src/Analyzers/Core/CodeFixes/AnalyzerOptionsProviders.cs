// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class AnalyzerOptionsProviders
{
    public static async ValueTask<AnalyzerOptionsProvider> GetAnalyzerOptionsProviderAsync(this Document document, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var analyzerOptions = document.Project.AnalyzerOptions;
        var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader();

        return new AnalyzerOptionsProvider(configOptions, document.Project.Language);
    }
}
