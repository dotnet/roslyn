// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableEntriesSnapshot<TData> : ITableEntriesSnapshot
    {
        // TODO: remove this once we have new drop
        protected const string ProjectGuidKey = "projectguid";

        private readonly int _version;
        private readonly ImmutableArray<TData> _items;
        private ImmutableArray<ITrackingPoint> _trackingPoints;

        protected readonly Guid ProjectGuid;

        protected AbstractTableEntriesSnapshot(int version, Guid projectGuid, ImmutableArray<TData> items, ImmutableArray<ITrackingPoint> trackingPoints)
        {
            _version = version;
            _items = items;
            _trackingPoints = trackingPoints;

            ProjectGuid = projectGuid;
        }

        public abstract object SnapshotIdentity { get; }
        public abstract bool TryNavigateTo(int index, bool previewTab);
        public abstract bool TryGetValue(int index, string columnName, out object content);
        protected abstract bool IsEquivalent(TData item1, TData item2);

        public int VersionNumber
        {
            get
            {
                return _version;
            }
        }

        public int Count
        {
            get
            {
                return _items.Length;
            }
        }

        public int TranslateTo(int index, ITableEntriesSnapshot newerSnapshot)
        {
            var item = GetItem(index);
            if (item == null)
            {
                return -1;
            }

            var ourSnapshot = newerSnapshot as AbstractTableEntriesSnapshot<TData>;
            if (ourSnapshot == null || ourSnapshot.Count == 0)
            {
                // not ours, we don't know how to track index
                return -1;
            }

            // quick path - this will deal with a case where we update data without any actual change
            if (this.Count == ourSnapshot.Count)
            {
                var newItem = ourSnapshot.GetItem(index);
                if (newItem != null && newItem.Equals(item))
                {
                    return index;
                }
            }

            // slow path.
            var bestMatch = Tuple.Create(-1, int.MaxValue);
            for (var i = 0; i < ourSnapshot.Count; i++)
            {
                var newItem = ourSnapshot.GetItem(i);
                if (IsEquivalent(item, newItem))
                {
                    return i;
                }
            }

            // no similar item exist. table control itself will try to maintain selection
            return -1;
        }

        public void StopTracking()
        {
            // remove tracking points
            _trackingPoints = default(ImmutableArray<ITrackingPoint>);
        }

        public void Dispose()
        {
            StopTracking();
        }

        protected TData GetItem(int index)
        {
            if (index < 0 || _items.Length <= index)
            {
                return default(TData);
            }

            return _items[index];
        }

        protected LinePosition GetTrackingLineColumn(Workspace workspace, DocumentId documentId, int index)
        {
            if (documentId == null || _trackingPoints.IsDefaultOrEmpty)
            {
                return LinePosition.Zero;
            }

            var solution = workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);
            if (document == null || !document.IsOpen())
            {
                return LinePosition.Zero;
            }

            var trackingPoint = _trackingPoints[index];

            SourceText text;
            if (!document.TryGetText(out text))
            {
                return LinePosition.Zero;
            }

            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot != null)
            {
                return GetLinePosition(snapshot, trackingPoint);
            }

            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer == null)
            {
                return LinePosition.Zero;
            }

            var currentSnapshot = textBuffer.CurrentSnapshot;
            return GetLinePosition(snapshot, trackingPoint);
        }

        private LinePosition GetLinePosition(ITextSnapshot snapshot, ITrackingPoint trackingPoint)
        {
            var point = trackingPoint.GetPoint(snapshot);
            var line = point.GetContainingLine();

            return new LinePosition(line.LineNumber, point.Position - line.Start);
        }

        protected bool TryNavigateTo(Workspace workspace, DocumentId documentId, int line, int column, bool previewTab)
        {
            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                // document could be already removed from the solution
                return false;
            }

            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService == null)
            {
                return false;
            }

            if (navigationService.TryNavigateToLineAndOffset(workspace, documentId, line, column, usePreviewTab: previewTab))
            {
                return true;
            }

            return false;
        }

        protected string GetFileName(string original, string mapped)
        {
            return mapped == null ? original : original == null ? mapped : Combine(original, mapped);
        }

        private string Combine(string path1, string path2)
        {
            string result;
            if (FilePathUtilities.TryCombine(path1, path2, out result))
            {
                return result;
            }

            return string.Empty;
        }

        protected string GetProjectName(Workspace workspace, ProjectId projectId)
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

        // TODO: remove this once we moved to new drop
        protected IVsHierarchy GetHierarchy(Workspace workspace, ProjectId projectId)
        {
            if (projectId == null)
            {
                return null;
            }

            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return null;
            }

            return vsWorkspace.GetHierarchy(projectId);
        }

        protected static Guid GetProjectGuid(Workspace workspace, ProjectId projectId)
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

        // we don't use these
        public object Identity(int index)
        {
            return null;
        }

        public void StartCaching()
        {
        }

        public void StopCaching()
        {
        }
    }
}
