// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Structure
{
    internal readonly record struct BlockStructureOptions(
        bool ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = false,
        bool ShowBlockStructureGuidesForDeclarationLevelConstructs = true,
        bool ShowBlockStructureGuidesForCodeLevelConstructs = true,
        bool ShowOutliningForCommentsAndPreprocessorRegions = true,
        bool ShowOutliningForDeclarationLevelConstructs = true,
        bool ShowOutliningForCodeLevelConstructs = true,
        bool CollapseRegionsWhenCollapsingToDefinitions = false,
        int MaximumBannerLength = 80,
        bool IsMetadataAsSource = false)
    {
        public BlockStructureOptions()
            : this(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions: false)
        {
        }

        public static readonly BlockStructureOptions Default = new();
    }
}
