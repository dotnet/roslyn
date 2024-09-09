// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.OrganizeImports;

internal static class OrganizeImportsOptionsProviders
{
    public static OrganizeImportsOptions GetOrganizeImportsOptions(this IOptionsReader options, string language)
        => new()
        {
            PlaceSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, language),
            SeparateImportDirectiveGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language),
            NewLine = options.GetOption(FormattingOptions2.NewLine, language)
        };

    public static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetOrganizeImportsOptions(document.Project.Language);
    }
}
