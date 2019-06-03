// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class TableItem
    {
        private int? _deduplicationKey;
        private readonly SharedInfoCache _cache;
        public readonly Workspace Workspace;

        public TableItem(Workspace workspace, SharedInfoCache cache)
        {
            Contract.ThrowIfNull(workspace);

            Workspace = workspace;
            _deduplicationKey = null;
            _cache = cache;
        }

        public abstract TableItem WithCache(SharedInfoCache cache);

        public abstract DocumentId DocumentId { get; }
        public abstract ProjectId ProjectId { get; }

        public abstract int GetDeduplicationKey();

        public abstract LinePosition GetOriginalPosition();
        public abstract string GetOriginalFilePath();
        public abstract bool EqualsIgnoringLocation(TableItem other);

        public string ProjectName
        {
            get
            {
                var projectId = ProjectId;
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
                var projectId = ProjectId;
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
                    _deduplicationKey = GetDeduplicationKey();
                }

                return _deduplicationKey.Value;
            }
        }
    }
}
