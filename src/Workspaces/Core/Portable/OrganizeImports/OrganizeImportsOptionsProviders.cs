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
    extension(IOptionsReader options)
    {
        public OrganizeImportsOptions GetOrganizeImportsOptions(string language)
        => new()
        {
            PlaceSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, language),
            SeparateImportDirectiveGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language),
            NewLine = options.GetOption(FormattingOptions2.NewLine, language)
        };
    }

    extension(Document document)
    {
        public async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetOrganizeImportsOptions(document.Project.Language);
        }
    }
}
