// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.OrganizeImports;

[DataContract]
internal readonly record struct OrganizeImportsOptions
{
    [DataMember] public bool PlaceSystemNamespaceFirst { get; init; } = AddImportPlacementOptions.Default.PlaceSystemNamespaceFirst;
    [DataMember] public bool SeparateImportDirectiveGroups { get; init; } = SyntaxFormattingOptions.CommonOptions.Default.SeparateImportDirectiveGroups;
    [DataMember] public string NewLine { get; init; } = LineFormattingOptions.Default.NewLine;

    public OrganizeImportsOptions()
    {
    }

    public static readonly OrganizeImportsOptions Default = new();
}

internal interface OrganizeImportsOptionsProvider : OptionsProvider<OrganizeImportsOptions>
{
}

internal static class OrganizeImportsOptionsProviders
{
    public static OrganizeImportsOptions GetOrganizeImportsOptions(this AnalyzerConfigOptions options, OrganizeImportsOptions? fallbackOptions)
    {
        fallbackOptions ??= OrganizeImportsOptions.Default;

        return new()
        {
            PlaceSystemNamespaceFirst = options.GetEditorConfigOption(GenerationOptions.PlaceSystemNamespaceFirst, fallbackOptions.Value.PlaceSystemNamespaceFirst),
            SeparateImportDirectiveGroups = options.GetEditorConfigOption(GenerationOptions.SeparateImportDirectiveGroups, fallbackOptions.Value.SeparateImportDirectiveGroups),
            NewLine = options.GetEditorConfigOption(FormattingOptions2.NewLine, fallbackOptions.Value.NewLine)
        };
    }

    public static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, OrganizeImportsOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetOrganizeImportsOptions(fallbackOptions);
    }

    public static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, OrganizeImportsOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetOrganizeImportsOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
