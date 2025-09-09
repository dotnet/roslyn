// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal sealed class SupportedPlatformData(Solution solution, ImmutableArray<ProjectId> invalidProjects, ImmutableArray<ProjectId> candidateProjects)
{
    // Because completion finds lots of symbols that exist in 
    // all projects, we'll instead maintain a list of projects 
    // missing the symbol.
    public readonly ImmutableArray<ProjectId> InvalidProjects = invalidProjects;
    public readonly ImmutableArray<ProjectId> CandidateProjects = candidateProjects;
    public readonly Solution Solution = solution;

    public IList<SymbolDisplayPart> ToDisplayParts()
    {
        if (InvalidProjects.Length == 0)
            return [];

        var builder = new List<SymbolDisplayPart>();
        builder.AddLineBreak();

        var projects = CandidateProjects.Select(Solution.GetRequiredProject).OrderBy(p => p.Name);
        foreach (var project in projects)
        {
            var text = string.Format(FeaturesResources._0_1, project.Name, Supported(!InvalidProjects.Contains(project.Id)));
            builder.AddSpace("    ");
            builder.AddText(text);
            builder.AddLineBreak();
        }

        builder.AddLineBreak();
        builder.AddText(FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts);

        return builder;
    }

    private static string Supported(bool supported)
        => supported ? FeaturesResources.Available : FeaturesResources.Not_Available;

    public bool HasValidAndInvalidProjects()
        => InvalidProjects.Length > 0 && InvalidProjects.Length != CandidateProjects.Length;
}
