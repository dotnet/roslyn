// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class TableItem<T>
    {
        private readonly Func<T, int> _keyGenerator;

        private int? _deduplicationKey;
        private SharedInfoCache _cache;

        public readonly T Primary;

        public TableItem(T item, Func<T, int> keyGenerator)
        {
            _deduplicationKey = null;
            _keyGenerator = keyGenerator;

            _cache = null;
            Primary = item;
        }

        public TableItem(IEnumerable<TableItem<T>> items)
        {
#if DEBUG
            // If code reached here,
            // There must be at least 1 item in the list
            Contract.ThrowIfFalse(items.Count() > 0);

            // There must be document id
            Contract.ThrowIfTrue(items.Any(i => i.PrimaryDocumentId == null));
#endif

            var first = true;
            var collectionHash = 1;
            var count = 0;

            // Make things to be deterministic. 
            var ordereditems = items.OrderBy(i => i.PrimaryDocumentId.Id);
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

                collectionHash = Hash.Combine(item.PrimaryDocumentId.Id.GetHashCode(), collectionHash);
            }

            if (count == 1)
            {
                _cache = null;
                return;
            }

            // order of item is important. make sure we maintain it.
            _cache = SharedInfoCache.GetOrAdd(collectionHash, ordereditems, c => new SharedInfoCache(c.Select(i => i.PrimaryDocumentId).ToImmutableArray()));
        }

        public DocumentId PrimaryDocumentId
        {
            get
            {
                return Extensions.GetDocumentId(Primary);
            }
        }

        private Workspace Workspace
        {
            get
            {
                return Extensions.GetWorkspace(Primary);
            }
        }

        public string ProjectName
        {
            get
            {
                var projectId = Extensions.GetProjectId(Primary);
                if (projectId == null)
                {
                    // this item doesn't have project at the first place
                    return null;
                }

                if (_cache == null)
                {
                    // return single project name
                    return Workspace.GetProjectName(projectId) ?? ServicesVSResources.Unknown;
                }

                // return joined project names
                return _cache.GetProjectName(Workspace) ?? ServicesVSResources.Unknown;
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

                return _cache.GetProjectNames(Workspace);
            }
        }

        public Guid ProjectGuid
        {
            get
            {
                var projectId = Extensions.GetProjectId(Primary);
                if (projectId == null)
                {
                    return Guid.Empty;
                }

                if (_cache == null)
                {
                    return Workspace.GetProjectGuid(projectId);
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