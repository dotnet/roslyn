// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.OrganizeImports;

internal static class OrganizeImportsOptionsStorage
{
    public static ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetOrganizeImportsOptionsAsync(globalOptions.GetOrganizeImportsOptions(document.Project.Language), cancellationToken);

    public static OrganizeImportsOptions GetOrganizeImportsOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            PlaceSystemNamespaceFirst = globalOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, language),
            SeparateImportDirectiveGroups = globalOptions.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language),
            NewLine = globalOptions.GetOption(FormattingOptions2.NewLine, language)
        };
}
