// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class SupportedPlatformData
    {
        // Because completion finds lots of symbols that exist in 
        // all projects, we'll instead maintain a list of projects 
        // missing the symbol.
        public readonly List<ProjectId> InvalidProjects;
        public readonly IEnumerable<ProjectId> CandidateProjects;
        public readonly Workspace Workspace;

        public SupportedPlatformData(List<ProjectId> invalidProjects, IEnumerable<ProjectId> candidateProjects, Workspace workspace)
        {
            InvalidProjects = invalidProjects;
            CandidateProjects = candidateProjects;
            Workspace = workspace;
        }

        public IList<SymbolDisplayPart> ToDisplayParts()
        {
            if (InvalidProjects == null || InvalidProjects.Count == 0)
            {
                return SpecializedCollections.EmptyList<SymbolDisplayPart>();
            }

            var builder = new List<SymbolDisplayPart>();
            builder.AddLineBreak();

            var projects = CandidateProjects.Select(p => Workspace.CurrentSolution.GetProject(p)).OrderBy(p => p.Name);
            foreach (var project in projects)
            {
                var text = string.Format(FeaturesResources._0_1, project.Name, Supported(!InvalidProjects.Contains(project.Id)));
                builder.AddText(text);
                builder.AddLineBreak();
            }

            builder.AddLineBreak();
            builder.AddText(FeaturesResources.You_can_use_the_navigation_bar_to_switch_context);

            return builder;
        }

        private static string Supported(bool supported)
            => supported ? FeaturesResources.Available : FeaturesResources.Not_Available;

        public bool HasValidAndInvalidProjects()
            => InvalidProjects.Any() && InvalidProjects.Count != CandidateProjects.Count();
    }
}
