// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Base implementation of ITableEntriesSnapshot
    /// </summary>
    internal abstract class AbstractTableEntriesSnapshot<TData> : ITableEntriesSnapshot
    {
        // TODO : remove these once we move to new drop which contains API change from editor team
        protected const string ProjectNames = StandardTableKeyNames.ProjectName + "s";
        protected const string ProjectGuids = StandardTableKeyNames.ProjectGuid + "s";

        private readonly int _version;
        private readonly ImmutableArray<TableItem<TData>> _items;
        private ImmutableArray<ITrackingPoint> _trackingPoints;

        protected AbstractTableEntriesSnapshot(int version, ImmutableArray<TableItem<TData>> items, ImmutableArray<ITrackingPoint> trackingPoints)
        {
            _version = version;
            _items = items;
            _trackingPoints = trackingPoints;
        }

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

        public int IndexOf(int index, ITableEntriesSnapshot newerSnapshot)
        {
            var data = GetItem(index);
            if (data == null)
            {
                return -1;
            }

            var item = data.Primary;
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
                var newData = ourSnapshot.GetItem(index);
                if (newData != null)
                {
                    var newItem = newData.Primary;
                    if (newItem != null && newItem.Equals(item))
                    {
                        return index;
                    }
                }
            }

            // slow path.
            var bestMatch = Tuple.Create(-1, int.MaxValue);
            for (var i = 0; i < ourSnapshot.Count; i++)
            {
                var newData = ourSnapshot.GetItem(i);
                if (newData != null)
                {
                    var newItem = newData.Primary;
                    if (IsEquivalent(item, newItem))
                    {
                        return i;
                    }
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

        internal TableItem<TData> GetItem(int index)
        {
            if (index < 0 || _items.Length <= index)
            {
                return default(TableItem<TData>);
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

            var options = workspace.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, previewTab);
            if (navigationService.TryNavigateToLineAndOffset(workspace, documentId, line, column, options))
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
