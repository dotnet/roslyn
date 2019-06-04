// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InternalUtilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal class SharedInfoCache
    {
        // this should let us to share common information among diagnostics reducing calculation and allocation.
        private static readonly ConcurrentLruCache<int, SharedInfoCache> s_documentInfoCache = new ConcurrentLruCache<int, SharedInfoCache>(capacity: 5);

        private readonly ImmutableArray<DocumentId> _documentIds;
        private ProjectInfoCache _cache;

        public static SharedInfoCache GetOrAdd<TArg>(int key, TArg arg, Func<TArg, SharedInfoCache> creator)
        {
            return s_documentInfoCache.GetOrAdd(key, arg, creator);
        }

        public SharedInfoCache(ImmutableArray<DocumentId> documentIds)
        {
            _documentIds = documentIds;
        }

        public string GetProjectName(Solution solution)
        {
            return GetCache(solution).GetProjectName(solution);
        }

        public string[] GetProjectNames(Solution solution)
        {
            return GetCache(solution).GetProjectNames(solution);
        }

        public Guid[] GetProjectGuids(Workspace workspace)
        {
            return GetCache(workspace.CurrentSolution).GetProjectGuids(workspace);
        }

        private ProjectInfoCache GetCache(Solution solution)
        {
            if (_cache == null)
            {
                // make sure this is deterministic
                var orderedItems = _documentIds.Select(d => d.ProjectId).Distinct().OrderBy(p => p.Id);
                _cache = ProjectInfoCache.GetOrAdd(GetHashCode(solution, orderedItems), orderedItems, c => new ProjectInfoCache(orderedItems.ToImmutableArray()));
            }

            return _cache;
        }

        private int GetHashCode(Solution solution, IEnumerable<ProjectId> ordereditems)
        {
            var collectionHash = 1;
            foreach (var item in ordereditems)
            {
                collectionHash = Hash.Combine(solution.GetProjectName(item), Hash.Combine(item.GetHashCode(), collectionHash));
            }

            return collectionHash;
        }

        private class ProjectInfoCache
        {
            private static readonly ConcurrentLruCache<int, ProjectInfoCache> s_projectInfoCache = new ConcurrentLruCache<int, ProjectInfoCache>(capacity: 2);

            private readonly ImmutableArray<ProjectId> _projectIds;

            private string _projectName;
            private string[] _projectNames;
            private Guid[] _projectGuids;

            public static ProjectInfoCache GetOrAdd<TArg>(int key, TArg items, Func<TArg, ProjectInfoCache> creator)
            {
                return s_projectInfoCache.GetOrAdd(key, items, creator);
            }

            public ProjectInfoCache(ImmutableArray<ProjectId> projectIds)
            {
                _projectIds = projectIds;
            }

            public string GetProjectName(Solution solution)
            {
                if (_projectName == null)
                {
                    _projectName = solution.GetProjectName(_projectIds);
                }

                return _projectName;
            }

            public string[] GetProjectNames(Solution solution)
            {
                if (_projectNames == null)
                {
                    _projectNames = solution.GetProjectNames(_projectIds);
                }

                return _projectNames;
            }

            public Guid[] GetProjectGuids(Workspace workspace)
            {
                if (_projectGuids == null)
                {
                    _projectGuids = workspace.GetProjectGuids(_projectIds);
                }

                return _projectGuids;
            }
        }
    }
}
