// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class TableItem<T>
    {
        private int? _deduplicationKey;
        private readonly SharedInfoCache _cache;
        public readonly Workspace Workspace;

        public readonly T Primary;

        public TableItem(Workspace workspace, T item, SharedInfoCache cache)
        {
            Contract.ThrowIfNull(workspace);
            Contract.ThrowIfNull((object)item);

            Workspace = workspace;
            _deduplicationKey = null;

            _cache = cache;
            Primary = item;
        }

        public TableItem<T> WithCache(SharedInfoCache cache)
            => new TableItem<T>(Workspace, Primary, cache);

        // item must be either one of diagnostic data and todo item
        public DocumentId PrimaryDocumentId
            => (Primary is DiagnosticData diagnostic) ? diagnostic.DocumentId : ((TodoItem)(object)Primary).DocumentId;

        // item must be either one of diagnostic data and todo item
        private ProjectId GetProjectId()
            => (Primary is DiagnosticData diagnostic) ? diagnostic.ProjectId : ((TodoItem)(object)Primary).DocumentId.ProjectId;

        // item must be either one of diagnostic data and todo item
        public LinePosition GetTrackingPosition()
        {
            if (Primary is DiagnosticData diagnostic)
            {
                return new LinePosition(diagnostic.DataLocation?.OriginalStartLine ?? 0, diagnostic.DataLocation?.OriginalStartColumn ?? 0);
            }

            var todo = (TodoItem)(object)Primary;
            return new LinePosition(todo.OriginalLine, todo.OriginalColumn);
        }

        // item must be either one of diagnostic data and todo item
        public int GetDeduplicationKey()
        {
            if (Primary is DiagnosticData diagnostic)
            {
                // location-less or project level diagnostic:
                if (diagnostic.DataLocation == null ||
                    diagnostic.DataLocation.OriginalFilePath == null ||
                    diagnostic.DocumentId == null)
                {
                    return diagnostic.GetHashCode();
                }

                return
                    Hash.Combine(diagnostic.DataLocation.OriginalStartColumn,
                    Hash.Combine(diagnostic.DataLocation.OriginalStartLine,
                    Hash.Combine(diagnostic.DataLocation.OriginalEndColumn,
                    Hash.Combine(diagnostic.DataLocation.OriginalEndLine,
                    Hash.Combine(diagnostic.DataLocation.OriginalFilePath,
                    Hash.Combine(diagnostic.IsSuppressed,
                    Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode())))))));
            }

            var todo = (TodoItem)(object)Primary;
            return Hash.Combine(todo.OriginalColumn, todo.OriginalLine);
        }

        public bool EqualsModuloLocation(TableItem<T> other)
        {
            if (other is null)
            {
                return false;
            }

            if (Primary is DiagnosticData diagnostic)
            {
                var otherDiagnostic = (DiagnosticData)(object)other.Primary;

                // everything same except location
                return diagnostic.Id == otherDiagnostic.Id &&
                       diagnostic.ProjectId == otherDiagnostic.ProjectId &&
                       diagnostic.DocumentId == otherDiagnostic.DocumentId &&
                       diagnostic.Category == otherDiagnostic.Category &&
                       diagnostic.Severity == otherDiagnostic.Severity &&
                       diagnostic.WarningLevel == otherDiagnostic.WarningLevel &&
                       diagnostic.Message == otherDiagnostic.Message;
            }
            else
            {
                var todo = (TodoItem)(object)Primary;
                var otherTodo = (TodoItem)(object)other.Primary;

                return todo.DocumentId == otherTodo.DocumentId && todo.Message == otherTodo.Message;

            }
        }

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
                    _deduplicationKey = GetDeduplicationKey();
                }

                return _deduplicationKey.Value;
            }
        }
    }
}
