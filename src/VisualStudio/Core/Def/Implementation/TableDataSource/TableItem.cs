// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal struct TableItem<T>
    {
        // this should let us to share common information among diagnostics reducing calculation and allocation.
        // this does produce one slight problem where project name might get staled a bit once project is renamed in error list.
        // but it should eventually get to right name as code changes happen.
        private static readonly ConcurrentLruCache<int, Cache> extraInfoCache = new ConcurrentLruCache<int, Cache>(capacity: 5);

        private readonly Func<T, int> _keyGenerator;

        private int? _deduplicationKey;
        private Cache _cache;

        public readonly T Primary;

        public TableItem(T item, Func<T, int> keyGenerator) : this()
        {
            _deduplicationKey = null;
            _keyGenerator = keyGenerator;

            _cache = null;
            Primary = item;
        }

        public TableItem(IEnumerable<TableItem<T>> items) : this()
        {
#if DEBUG
            // If code reached here,
            // There must be at least 1 item in the list
            Contract.ThrowIfFalse(items.Count() > 0);

            // There must be document id
            Contract.ThrowIfTrue(items.Any(i => Extensions.GetDocumentId(i.Primary) == null));
#endif

            var first = true;
            var collectionHash = 1;
            var count = 0;

            // Make things to be deterministic. 
            var ordereditems = items.OrderBy(i => Extensions.GetDocumentId(i.Primary).Id);
            foreach (var item in ordereditems)
            {
                count++;

                if (first)
                {
                    Primary = item.Primary;

                    _deduplicationKey = item.DeduplicationKey;
                    _keyGenerator = null;

                    first = false;
                }

                collectionHash = Hash.Combine(Extensions.GetDocumentId(item.Primary).Id.GetHashCode(), collectionHash);
            }

            if (count == 1)
            {
                _cache = null;
                return;
            }

            // order of item is important. make sure we maintain it.
            _cache = extraInfoCache.GetOrAdd(collectionHash, ordereditems, c => new Cache(c.Select(i => Extensions.GetDocumentId(i.Primary)).ToImmutableArray()));
        }

        public DocumentId PrimaryDocumentId
        {
            get
            {
                return Extensions.GetDocumentId(Primary);
            }
        }

        public string ProjectName
        {
            get
            {
                if (_cache == null)
                {
                    // return single project name
                    return GetProjectName(Extensions.GetWorkspace(Primary), PrimaryDocumentId.ProjectId);
                }

                // return joined project names
                return _cache.GetProjectName(Extensions.GetWorkspace(Primary));
            }
        }

        public string[] ProjectNames
        {
            get
            {
                if (_cache == null)
                {
                    // if this is not aggregated element, there is no projectnames.
                    return Array.Empty<string>();
                }

                return _cache.GetProjectNames(Extensions.GetWorkspace(Primary));
            }
        }

        public Guid ProjectGuid
        {
            get
            {
                if (_cache == null)
                {
                    return GetProjectGuid(Extensions.GetWorkspace(Primary), PrimaryDocumentId.ProjectId);
                }

                // if this is aggregated element, there is no projectguid
                return Guid.Empty;
            }
        }

        public Guid[] ProjectGuids
        {
            get
            {
                if (_cache == null)
                {
                    // if it is not aggregated element, there is no projectguids
                    return Array.Empty<Guid>();
                }

                return _cache.GetProjectGuids(Extensions.GetWorkspace(Primary));
            }
        }

        public int DeduplicationKey
        {
            get
            {
                if (_deduplicationKey == null)
                {
                    _deduplicationKey = _keyGenerator(Primary);
                }

                return _deduplicationKey.Value;
            }
        }

        private static string GetProjectName(Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            var projectNames = GetProjectNames(workspace, projectIds);
            if (projectNames.Length == 0)
            {
                return null;
            }

            return string.Join(", ", projectNames.OrderBy(StringComparer.CurrentCulture));
        }

        private static string GetProjectName(Workspace workspace, ProjectId projectId)
        {
            if (projectId == null)
            {
                return null;
            }

            var project = workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                return null;
            }

            return project.Name;
        }

        private static string[] GetProjectNames(Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectName(workspace, p)).WhereNotNull().ToArray();
        }

        private static Guid GetProjectGuid(Workspace workspace, ProjectId projectId)
        {
            if (projectId == null)
            {
                return Guid.Empty;
            }

            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            var project = vsWorkspace?.GetHostProject(projectId);
            if (project == null)
            {
                return Guid.Empty;
            }

            return project.Guid;
        }

        private static Guid[] GetProjectGuids(Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectGuid(workspace, p)).Where(g => g != Guid.Empty).ToArray();
        }

        private class Cache
        {
            private readonly ImmutableArray<DocumentId> _documentIds;
            private ImmutableArray<ProjectId> _doNotAccessDirectlyProjectIds;

            private string _projectName;
            private string[] _projectNames;
            private Guid[] _projectGuids;

            public Cache(ImmutableArray<DocumentId> documentIds)
            {
                _documentIds = documentIds;
            }

            public string GetProjectName(Workspace workspace)
            {
                if (_projectName == null)
                {
                    _projectName = TableItem<T>.GetProjectName(workspace, ProjectIds);
                }

                return _projectName;
            }

            public string[] GetProjectNames(Workspace workspace)
            {
                if (_projectNames == null)
                {
                    _projectNames = TableItem<T>.GetProjectNames(workspace, ProjectIds);
                }

                return _projectNames;
            }

            public Guid[] GetProjectGuids(Workspace workspace)
            {
                if (_projectGuids == null)
                {
                    _projectGuids = TableItem<T>.GetProjectGuids(workspace, ProjectIds);
                }

                return _projectGuids;
            }

            private ImmutableArray<ProjectId> ProjectIds
            {
                get
                {
                    if (_doNotAccessDirectlyProjectIds.IsDefault)
                    {
                        _doNotAccessDirectlyProjectIds = _documentIds.Select(d => d.ProjectId).OrderBy(p => p.Id).ToImmutableArray();
                    }

                    return _doNotAccessDirectlyProjectIds;
                }
            }
        }
    }
}