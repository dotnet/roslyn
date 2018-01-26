// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private class LoadState
        {
            private Dictionary<ProjectId, ProjectInfo> _projectIdToProjectInfoMap
                = new Dictionary<ProjectId, ProjectInfo>();

            /// <summary>
            /// Used to memoize results of <see cref="ProjectAlreadyReferencesProject"/> calls.
            /// Reset any time internal state is changed.
            /// </summary>
            private Dictionary<ProjectId, Dictionary<ProjectId, bool>> _projectAlreadyReferencesProjectResultCache
                = new Dictionary<ProjectId, Dictionary<ProjectId, bool>>();

            private readonly Dictionary<string, ProjectId> _projectPathToProjectIdMap
                = new Dictionary<string, ProjectId>(PathUtilities.Comparer);

            public LoadState(IReadOnlyDictionary<string, ProjectId> projectPathToProjectIdMap)
            {
                if (projectPathToProjectIdMap != null)
                {
                    _projectPathToProjectIdMap.AddRange(projectPathToProjectIdMap);
                }
            }

            public void Add(ProjectInfo info)
            {
                _projectIdToProjectInfoMap.Add(info.Id, info);
                //Memoized results of ProjectAlreadyReferencesProject may no longer be correct;
                //reset the cache.
                _projectAlreadyReferencesProjectResultCache.Clear();
            }

            /// <summary>
            /// Returns true if the project identified by <paramref name="fromProject"/> has a reference (even indirectly)
            /// on the project identified by <paramref name="targetProject"/>.
            /// </summary>
            public bool ProjectAlreadyReferencesProject(ProjectId fromProject, ProjectId targetProject)
            {
                if (!_projectAlreadyReferencesProjectResultCache.TryGetValue(fromProject, out var fromProjectMemo))
                {
                    fromProjectMemo = new Dictionary<ProjectId, bool>();
                    _projectAlreadyReferencesProjectResultCache.Add(fromProject, fromProjectMemo);
                }

                if (!fromProjectMemo.TryGetValue(targetProject, out var answer))
                {
                    answer =
                        _projectIdToProjectInfoMap.TryGetValue(fromProject, out var info) &&
                        info.ProjectReferences.Any(pr =>
                            pr.ProjectId == targetProject ||
                            ProjectAlreadyReferencesProject(pr.ProjectId, targetProject)
                        );
                    fromProjectMemo.Add(targetProject, answer);
                }

                return answer;
            }

            public IEnumerable<ProjectInfo> Projects
            {
                get { return _projectIdToProjectInfoMap.Values; }
            }

            public ProjectId GetProjectId(string fullProjectPath)
            {
                _projectPathToProjectIdMap.TryGetValue(fullProjectPath, out var id);
                return id;
            }

            public ProjectId GetOrCreateProjectId(string fullProjectPath)
            {
                if (!_projectPathToProjectIdMap.TryGetValue(fullProjectPath, out var id))
                {
                    id = ProjectId.CreateNewId(debugName: fullProjectPath);
                    _projectPathToProjectIdMap.Add(fullProjectPath, id);
                }

                return id;
            }
        }
    }
}
