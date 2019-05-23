// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class TableItem<T>
    {
        private readonly Func<T, int> _keyGenerator;

        private int? _deduplicationKey;
        private readonly SharedInfoCache _cache;
        public readonly Workspace Workspace;

        public readonly T Primary;

        public TableItem(Workspace workspace, T item, Func<T, int> keyGenerator)
        {
            Contract.ThrowIfNull(workspace);

            Workspace = workspace;
            _deduplicationKey = null;
            _keyGenerator = keyGenerator;

            _cache = null;
            Primary = item;
        }

        public TableItem(IEnumerable<TableItem<T>> items)
        {
            var first = true;
            var collectionHash = 1;
            var count = 0;

            // Make things to be deterministic. 
            var orderedItems = items.Where(i => i.PrimaryDocumentId != null).OrderBy(i => i.PrimaryDocumentId.Id).ToList();
            if (orderedItems.Count == 0)
            {
                // There must be at least 1 item in the list
                FatalError.ReportWithoutCrash(new ArgumentException("Contains no items", nameof(items)));
            }
            else if (orderedItems.Count != items.Count())
            {
                // There must be document id for provided items.
                FatalError.ReportWithoutCrash(new ArgumentException("Contains an item with null PrimaryDocumentId", nameof(items)));
            }

            foreach (var item in orderedItems)
            {
                count++;

                if (first)
                {
                    Primary = item.Primary;

                    _deduplicationKey = item.DeduplicationKey;
                    _keyGenerator = null;

                    first = false;
                }

                collectionHash = Hash.Combine(item.PrimaryDocumentId.Id.GetHashCode(), collectionHash);
            }

            if (count == 1)
            {
                _cache = null;
                return;
            }

            // order of item is important. make sure we maintain it.
            _cache = SharedInfoCache.GetOrAdd(collectionHash, orderedItems, c => new SharedInfoCache(c.Select(i => i.PrimaryDocumentId).ToImmutableArray()));
        }

        // item must be either one of diagnostic data and todo item
        public DocumentId PrimaryDocumentId
            => (Primary is DiagnosticData diagnostic) ? diagnostic.DocumentId : ((TodoItem)(object)Primary).DocumentId;

        // item must be either one of diagnostic data and todo item
        private ProjectId GetProjectId()
            => (Primary is DiagnosticData diagnostic) ? diagnostic.ProjectId : ((TodoItem)(object)Primary).DocumentId.ProjectId;

        public string ProjectName
        {
            get
            {
                var projectId = GetProjectId();
                if (projectId == null)
                {
                    // this item doesn't have project at the first place
                    return null;
                }

                var solution = Workspace.CurrentSolution;
                if (_cache == null)
                {
                    // return single project name
                    return solution.GetProjectName(projectId) ?? ServicesVSResources.Unknown2;
                }

                // return joined project names
                return _cache.GetProjectName(solution) ?? ServicesVSResources.Unknown2;
            }
        }

        public string[] ProjectNames
        {
            get
            {
                if (_cache == null)
                {
                    // if this is not aggregated element, there are no project names.
                    return Array.Empty<string>();
                }

                return _cache.GetProjectNames(Workspace.CurrentSolution);
            }
        }

        public Guid ProjectGuid
        {
            get
            {
                var projectId = GetProjectId();
                if (projectId == null)
                {
                    return Guid.Empty;
                }

                if (_cache == null)
                {
                    return Workspace.GetProjectGuid(projectId);
                }

                // if this is aggregated element, there is no project GUID
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

                return _cache.GetProjectGuids(Workspace);
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
    }
}
