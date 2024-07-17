// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.OrganizeImports;

[DataContract]
internal readonly record struct OrganizeImportsOptions
{
    [DataMember] public bool PlaceSystemNamespaceFirst { get; init; } = AddImportPlacementOptions.Default.PlaceSystemNamespaceFirst;
    [DataMember] public bool SeparateImportDirectiveGroups { get; init; } = SyntaxFormattingOptions.CommonDefaults.SeparateImportDirectiveGroups;
    [DataMember] public string NewLine { get; init; } = LineFormattingOptions.Default.NewLine;

    public OrganizeImportsOptions()
    {
    }

    public static readonly OrganizeImportsOptions Default = new();
}

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
