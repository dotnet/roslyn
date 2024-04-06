// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Structure;

[DataContract]
internal sealed record class BlockStructureOptions
{
    [DataMember] public bool ShowBlockStructureGuidesForCommentsAndPreprocessorRegions { get; init; } = false;
    [DataMember] public bool ShowBlockStructureGuidesForDeclarationLevelConstructs { get; init; } = true;
    [DataMember] public bool ShowBlockStructureGuidesForCodeLevelConstructs { get; init; } = true;
    [DataMember] public bool ShowOutliningForCommentsAndPreprocessorRegions { get; init; } = true;
    [DataMember] public bool ShowOutliningForDeclarationLevelConstructs { get; init; } = true;
    [DataMember] public bool ShowOutliningForCodeLevelConstructs { get; init; } = true;
    [DataMember] public bool CollapseRegionsWhenFirstOpened { get; init; } = true;
    [DataMember] public bool CollapseImportsWhenFirstOpened { get; init; } = false;
    [DataMember] public bool CollapseMetadataImplementationsWhenFirstOpened { get; init; } = false;
    [DataMember] public bool CollapseEmptyMetadataImplementationsWhenFirstOpened { get; init; } = true;
    [DataMember] public bool CollapseRegionsWhenCollapsingToDefinitions { get; init; } = false;
    [DataMember] public bool CollapseLocalFunctionsWhenCollapsingToDefinitions { get; init; } = false;
    [DataMember] public int MaximumBannerLength { get; init; } = 80;
    [DataMember] public bool IsMetadataAsSource { get; init; } = false;

    public static readonly BlockStructureOptions Default = new();
}
