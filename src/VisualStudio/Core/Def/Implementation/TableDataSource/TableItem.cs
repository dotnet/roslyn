// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal struct TableItem<T>
    {
        private readonly Func<T, int> _keyGenerator;
        private int? _deduplicationKey;

        public readonly T Primary;
        public readonly ImmutableHashSet<DocumentId> DocumentIds;

        public ImmutableHashSet<ProjectId> ProjectIds => ImmutableHashSet.CreateRange(DocumentIds.Select(d => d.ProjectId));

        public string ProjectName => GetProjectName(Extensions.GetWorkspace(Primary), ProjectIds);
        public string[] ProjectNames => GetProjectNames(Extensions.GetWorkspace(Primary), ProjectIds);
        public Guid ProjectGuid => GetProjectGuid(Extensions.GetWorkspace(Primary), PrimaryDocumentId.ProjectId);
        public Guid[] ProjectGuids => GetProjectGuids(Extensions.GetWorkspace(Primary), ProjectIds);

        public TableItem(T item, Func<T, int> keyGenerator) : this()
        {
            _deduplicationKey = null;
            _keyGenerator = keyGenerator;

            Primary = item;

            var documentId = Extensions.GetDocumentId(Primary);
            DocumentIds = documentId == null ? ImmutableHashSet<DocumentId>.Empty : ImmutableHashSet.Create(documentId);
        }

        public TableItem(T primary, int deduplicationKey, ImmutableHashSet<DocumentId> documentIds) : this()
        {
            Contract.ThrowIfFalse(documentIds.Count > 0);

            _deduplicationKey = deduplicationKey;
            _keyGenerator = null;

            Primary = primary;
            DocumentIds = documentIds;
        }

        public DocumentId PrimaryDocumentId
        {
            get
            {
                if (DocumentIds.Count == 0)
                {
                    return Extensions.GetDocumentId(Primary);
                }

                return DocumentIds.First();
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

        public static string GetProjectName(Workspace workspace, ImmutableHashSet<ProjectId> projectIds)
        {
            var projectNames = GetProjectNames(workspace, projectIds);
            if (projectNames.Length == 0)
            {
                return null;
            }

            return string.Join(", ", projectNames.OrderBy(StringComparer.CurrentCulture));
        }

        public static string GetProjectName(Workspace workspace, ProjectId projectId)
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

        public static string[] GetProjectNames(Workspace workspace, ImmutableHashSet<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectName(workspace, p)).WhereNotNull().ToArray();
        }

        public static Guid GetProjectGuid(Workspace workspace, ProjectId projectId)
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

        public static Guid[] GetProjectGuids(Workspace workspace, ImmutableHashSet<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectGuid(workspace, p)).Where(g => g != Guid.Empty).ToArray();
        }
    }
}