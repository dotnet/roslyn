// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            IList<SymbolDisplayPart> builder = new List<SymbolDisplayPart>();
            builder.AddLineBreak();

            var projects = this.CandidateProjects.Select(p => this.Workspace.CurrentSolution.GetProject(p)).OrderBy(p => p.Name);
            foreach (var project in projects)
            {
                var text = string.Format(FeaturesResources.ProjectAvailability, project.Name, Supported(!this.InvalidProjects.Contains(project.Id)));
                builder.AddText(text);
                builder.AddLineBreak();
            }

            builder.AddLineBreak();
            builder.AddText(FeaturesResources.UseTheNavigationBarToSwitchContext);

            return builder;
        }

        private static string Supported(bool supported)
        {
            return supported ? FeaturesResources.Available : FeaturesResources.NotAvailable;
        }

        public bool HasValidAndInvalidProjects()
        {
            return InvalidProjects.Any() && InvalidProjects.Count != CandidateProjects.Count();
        }
    }
}
