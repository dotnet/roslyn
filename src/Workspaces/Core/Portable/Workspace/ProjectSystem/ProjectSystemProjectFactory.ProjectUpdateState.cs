// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed partial class ProjectSystemProjectFactory
{
    /// <summary>
    /// Immutable data type that holds the current state of the project system factory as well as storing any
    /// incremental state changes in the current workspace update.
    /// 
    /// This state is updated by various project system update operations under the <see cref="_gate"/>. Importantly,
    /// this immutable type allows us to discard updates to the state that fail to apply due to interceding workspace
    /// operations.
    /// 
    /// There are two kinds of state that this type holds that need to support discarding:
    /// <list type="number">
    /// <item>Global state for the <see cref="ProjectSystemProjectFactory"/> (various maps of project information). This
    /// state must be saved between different changes.</item>
    /// <item>Incremental state for the current change being processed.  This state has information that is cannot be
    /// resilient to being applied multiple times during the workspace update, so is saved to be applied only once the
    /// workspace update is successful.</item>
    /// </list>
    /// </summary>
    /// <param name="ProjectsByOutputPath">
    /// Global state representing a multimap from an output path to the project outputting to it. Ideally, this
    /// shouldn't ever actually be a true multimap, since we shouldn't have two projects outputting to the same path,
    /// but any bug by a project adding the wrong output path means we could end up with some duplication. In that case,
    /// we'll temporarily have two until (hopefully) somebody removes it.
    /// </param>
    /// <param name="ProjectReferenceInfos">
    /// Global state containing output paths and converted project reference information for each project.
    /// </param>
    /// <param name="RemovedMetadataReferences">
    /// Incremental state containing metadata references removed in the current update.
    /// </param>
    /// <param name="AddedMetadataReferences">
    /// Incremental state containing metadata references added in the current update.
    /// </param>
    /// <param name="RemovedAnalyzerReferences">
    /// Incremental state containing analyzer references removed in the current update.
    /// </param>
    /// <param name="AddedAnalyzerReferences">
    /// Incremental state containing analyzer references added in the current update.
    /// </param>
    public sealed record class ProjectUpdateState(
        ImmutableDictionary<string, ImmutableArray<ProjectId>> ProjectsByOutputPath,
        ImmutableDictionary<ProjectId, ProjectReferenceInformation> ProjectReferenceInfos,
        ImmutableArray<PortableExecutableReference> RemovedMetadataReferences,
        ImmutableArray<PortableExecutableReference> AddedMetadataReferences,
        ImmutableArray<string> RemovedAnalyzerReferences,
        ImmutableArray<string> AddedAnalyzerReferences)
    {
        public static ProjectUpdateState Empty = new(
            ImmutableDictionary<string, ImmutableArray<ProjectId>>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase),
            ImmutableDictionary<ProjectId, ProjectReferenceInformation>.Empty, [], [], [], []);

        public ProjectUpdateState WithProjectReferenceInfo(ProjectId projectId, ProjectReferenceInformation projectReferenceInformation)
        {
            return this with
            {
                ProjectReferenceInfos = ProjectReferenceInfos.SetItem(projectId, projectReferenceInformation)
            };
        }

        public ProjectUpdateState WithProjectOutputPath(string projectOutputPath, ProjectId projectId)
        {
            return this with
            {
                ProjectsByOutputPath = AddProject(projectOutputPath, projectId, ProjectsByOutputPath)
            };

            static ImmutableDictionary<string, ImmutableArray<ProjectId>> AddProject(string path, ProjectId projectId, ImmutableDictionary<string, ImmutableArray<ProjectId>> map)
            {
                if (!map.TryGetValue(path, out var projects))
                {
                    return map.Add(path, [projectId]);
                }
                else
                {
                    return map.SetItem(path, projects.Add(projectId));
                }
            }
        }

        public ProjectUpdateState RemoveProjectOutputPath(string projectOutputPath, ProjectId projectId)
        {
            return this with
            {
                ProjectsByOutputPath = RemoveProject(projectOutputPath, projectId, ProjectsByOutputPath)
            };

            static ImmutableDictionary<string, ImmutableArray<ProjectId>> RemoveProject(string path, ProjectId projectId, ImmutableDictionary<string, ImmutableArray<ProjectId>> map)
            {
                if (map.TryGetValue(path, out var projects))
                {
                    projects = projects.Remove(projectId);
                    if (projects.IsEmpty)
                    {
                        return map.Remove(path);
                    }
                    else
                    {
                        return map.SetItem(path, projects);
                    }
                }

                return map;
            }
        }

        public ProjectUpdateState WithIncrementalMetadataReferenceRemoved(PortableExecutableReference reference)
            => this with { RemovedMetadataReferences = RemovedMetadataReferences.Add(reference) };

        public ProjectUpdateState WithIncrementalMetadataReferenceAdded(PortableExecutableReference reference)
            => this with { AddedMetadataReferences = AddedMetadataReferences.Add(reference) };

        public ProjectUpdateState WithIncrementalAnalyzerReferenceRemoved(string reference)
            => this with { RemovedAnalyzerReferences = RemovedAnalyzerReferences.Add(reference) };

        public ProjectUpdateState WithIncrementalAnalyzerReferencesRemoved(List<string> references)
            => this with { RemovedAnalyzerReferences = RemovedAnalyzerReferences.AddRange(references) };

        public ProjectUpdateState WithIncrementalAnalyzerReferenceAdded(string reference)
            => this with { AddedAnalyzerReferences = AddedAnalyzerReferences.Add(reference) };

        public ProjectUpdateState WithIncrementalAnalyzerReferencesAdded(List<string> references)
            => this with { AddedAnalyzerReferences = AddedAnalyzerReferences.AddRange(references) };

        /// <summary>
        /// Returns a new instance with any incremental state that should not be saved between updates cleared.
        /// </summary>
        public ProjectUpdateState ClearIncrementalState()
            => this with
            {
                RemovedMetadataReferences = [],
                AddedMetadataReferences = [],
                RemovedAnalyzerReferences = [],
                AddedAnalyzerReferences = [],
            };
    }

    public record struct ProjectReferenceInformation(ImmutableArray<string> OutputPaths, ImmutableArray<(string path, ProjectReference ProjectReference)> ConvertedProjectReferences)
    {
        internal ProjectReferenceInformation WithConvertedProjectReference(string path, ProjectReference projectReference)
        {
            return this with
            {
                ConvertedProjectReferences = ConvertedProjectReferences.Add((path, projectReference))
            };
        }
    }
}
