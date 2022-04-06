// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
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

        protected AbstractTableEntriesSnapshot(IThreadingContext threadingContext, int version, ImmutableArray<TItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
        {
            ThreadingContext = threadingContext;
            _version = version;
            _items = items;
            _trackingPoints = trackingPoints;
        }

        public abstract bool TryNavigateTo(int index, NavigationOptions options, CancellationToken cancellationToken);
        public abstract bool TryGetValue(int index, string columnName, [NotNullWhen(true)] out object? content);

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

        protected IThreadingContext ThreadingContext { get; }

        public int IndexOf(int index, ITableEntriesSnapshot newerSnapshot)
        {
            var item = GetItem(index);
            if (item == null)
            {
                return -1;
            }

            if (newerSnapshot is not AbstractTableEntriesSnapshot<TItem> ourSnapshot || ourSnapshot.Count == 0)
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

                // GetItem only returns null for index out of range
                RoslynDebug.AssertNotNull(newItem);

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
            => StopTracking();

        internal TItem? GetItem(int index)
        {
            if (index < 0 || _items.Length <= index)
            {
                return null;
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

        protected bool TryNavigateTo(Workspace workspace, DocumentId documentId, LinePosition position, NavigationOptions options, CancellationToken cancellationToken)
        {
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService == null)
                return false;

            return this.ThreadingContext.JoinableTaskFactory.Run(() =>
                navigationService.TryNavigateToLineAndOffsetAsync(
                    this.ThreadingContext, workspace, documentId, position.Line, position.Character, options, cancellationToken));
        }

        protected bool TryNavigateToItem(int index, NavigationOptions options, CancellationToken cancellationToken)
        {
            var item = GetItem(index);
            if (item is null)
                return false;

            var workspace = item.Workspace;
            var solution = workspace.CurrentSolution;
            var documentId = item.DocumentId;
            if (documentId is null)
            {
                if (item is { ProjectId: { } projectId }
                    && solution.GetProject(projectId) is { } project)
                {
                    // We couldn't find a document ID when the item was created, so it may be a source generator
                    // output.
                    var documents = ThreadingContext.JoinableTaskFactory.Run(() => project.GetSourceGeneratedDocumentsAsync(cancellationToken).AsTask());
                    var projectDirectory = Path.GetDirectoryName(project.FilePath);
                    documentId = documents.FirstOrDefault(document => Path.Combine(projectDirectory, document.FilePath) == item.GetOriginalFilePath())?.Id;
                    if (documentId is null)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            LinePosition position;
            var document = solution.GetDocument(documentId);
            if (document is not null
                && workspace.IsDocumentOpen(documentId)
                && GetTrackingLineColumn(document, index) is { } trackingLinePosition
                && trackingLinePosition != LinePosition.Zero)
            {
                // For normal documents already open, try to map the diagnostic location to its current position in a
                // potentially-edited document.
                position = trackingLinePosition;
            }
            else
            {
                // Otherwise navigate to the original reported location.
                position = item.GetOriginalPosition();
            }

            return TryNavigateTo(workspace, documentId, position, options, cancellationToken);
        }

        // we don't use these
#pragma warning disable IDE0060 // Remove unused parameter - Implements interface method for sub-type
        public object? Identity(int index)
#pragma warning restore IDE0060 // Remove unused parameter
            => null;

        public void StartCaching()
        {
        }

        public void StopCaching()
        {
        }
    }
}
