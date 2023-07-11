﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
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

internal interface OrganizeImportsOptionsProvider : OptionsProvider<OrganizeImportsOptions>
{
}

internal static class OrganizeImportsOptionsProviders
{
    public static OrganizeImportsOptions GetOrganizeImportsOptions(this IOptionsReader options, string language, OrganizeImportsOptions? fallbackOptions)
    {
        fallbackOptions ??= OrganizeImportsOptions.Default;

        return new()
        {
            PlaceSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, language, fallbackOptions.Value.PlaceSystemNamespaceFirst),
            SeparateImportDirectiveGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language, fallbackOptions.Value.SeparateImportDirectiveGroups),
            NewLine = options.GetOption(FormattingOptions2.NewLine, language, fallbackOptions.Value.NewLine)
        };
    }

    public static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, OrganizeImportsOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetOrganizeImportsOptions(document.Project.Language, fallbackOptions);
    }

    public static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(this Document document, OrganizeImportsOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetOrganizeImportsOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
