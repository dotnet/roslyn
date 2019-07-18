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
    internal abstract class AbstractTableEntriesSnapshot<TItem> : ITableEntriesSnapshot
        where TItem : TableItem
    {
        // TODO : remove these once we move to new drop which contains API change from editor team
        protected const string ProjectNames = StandardTableKeyNames.ProjectName + "s";
        protected const string ProjectGuids = StandardTableKeyNames.ProjectGuid + "s";

        private readonly int _version;
        private readonly ImmutableArray<TItem> _items;
        private ImmutableArray<ITrackingPoint> _trackingPoints;

        protected AbstractTableEntriesSnapshot(int version, ImmutableArray<TItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
        {
            _version = version;
            _items = items;
            _trackingPoints = trackingPoints;
        }

        public abstract bool TryNavigateTo(int index, bool previewTab);
        public abstract bool TryGetValue(int index, string columnName, out object content);

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
            var item = GetItem(index);
            if (item == null)
            {
                return -1;
            }

            if (!(newerSnapshot is AbstractTableEntriesSnapshot<TItem> ourSnapshot) || ourSnapshot.Count == 0)
            {
                // not ours, we don't know how to track index
                return -1;
            }

            // quick path - this will deal with a case where we update data without any actual change
            if (Count == ourSnapshot.Count)
            {
                var newItem = ourSnapshot.GetItem(index);
                if (newItem != null && newItem.Equals(item))
                {
                    return index;
                }
            }

            // slow path.
            for (var i = 0; i < ourSnapshot.Count; i++)
            {
                var newItem = ourSnapshot.GetItem(i);
                if (item.EqualsIgnoringLocation(newItem))
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
            _trackingPoints = default;
        }

        public void Dispose()
        {
            StopTracking();
        }

        internal TItem GetItem(int index)
        {
            if (index < 0 || _items.Length <= index)
            {
                return default;
            }

            return _items[index];
        }

        protected LinePosition GetTrackingLineColumn(Document document, int index)
        {
            if (_trackingPoints.IsDefaultOrEmpty)
            {
                return LinePosition.Zero;
            }

            var trackingPoint = _trackingPoints[index];
            if (!document.TryGetText(out var text))
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
            return GetLinePosition(currentSnapshot, trackingPoint);
        }

        private static LinePosition GetLinePosition(ITextSnapshot snapshot, ITrackingPoint trackingPoint)
        {
            var point = trackingPoint.GetPoint(snapshot);
            var line = point.GetContainingLine();

            return new LinePosition(line.LineNumber, point.Position - line.Start);
        }

        protected static bool TryNavigateTo(Workspace workspace, DocumentId documentId, LinePosition position, bool previewTab)
        {
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService == null)
            {
                return false;
            }

            var options = workspace.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, previewTab);
            if (navigationService.TryNavigateToLineAndOffset(workspace, documentId, position.Line, position.Character, options))
            {
                return true;
            }

            return false;
        }

        protected bool TryNavigateToItem(int index, bool previewTab)
        {
            var item = GetItem(index);
            var documentId = item?.DocumentId;
            if (documentId == null)
            {
                return false;
            }

            var workspace = item.Workspace;
            var solution = workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return false;
            }

            LinePosition position;
            LinePosition trackingLinePosition;

            if (workspace.IsDocumentOpen(documentId) &&
                (trackingLinePosition = GetTrackingLineColumn(document, index)) != LinePosition.Zero)
            {
                position = trackingLinePosition;
            }
            else
            {
                position = item.GetOriginalPosition();
            }

            return TryNavigateTo(workspace, documentId, position, previewTab);
        }

        protected static string GetFileName(string original, string mapped)
        {
            return mapped == null ? original : original == null ? mapped : Combine(original, mapped);
        }

        private static string Combine(string path1, string path2)
        {
            if (TryCombine(path1, path2, out var result))
            {
                return result;
            }

            return string.Empty;
        }

        public static bool TryCombine(string path1, string path2, out string result)
        {
            try
            {
                // don't throw exception when either path1 or path2 contains illegal path char
                result = System.IO.Path.Combine(path1, path2);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
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
