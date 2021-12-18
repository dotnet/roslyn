// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
        private FrameworkElement[]? _descriptions;

        protected AbstractTableEntriesSnapshot(int version, ImmutableArray<TItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
        {
            _version = version;
            _items = items;
            _trackingPoints = trackingPoints;
        }

        public abstract bool TryNavigateTo(int index, bool previewTab, bool activate, CancellationToken cancellationToken);
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

        protected static bool TryNavigateTo(Workspace workspace, DocumentId documentId, LinePosition position, bool previewTab, bool activate, CancellationToken cancellationToken)
        {
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService == null)
            {
                return false;
            }

            var solution = workspace.CurrentSolution;
            var options = solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, previewTab)
                                          .WithChangedOption(NavigationOptions.ActivateTab, activate);
            return navigationService.TryNavigateToLineAndOffset(workspace, documentId, position.Line, position.Character, options, cancellationToken);
        }

        protected bool TryNavigateToItem(int index, bool previewTab, bool activate, CancellationToken cancellationToken)
        {
            var item = GetItem(index);
            if (item is not { DocumentId: { } documentId })
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

            return TryNavigateTo(workspace, documentId, position, previewTab, activate, cancellationToken);
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

        protected static bool CanCreateDetailsContent(int index, Func<int, DiagnosticTableItem?> getDiagnosticTableItem)
        {
            var item = getDiagnosticTableItem(index)?.Data;
            if (item == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(item.Description);
        }

        protected bool TryCreateDetailsContent(int index, Func<int, DiagnosticTableItem?> getDiagnosticTableItem, [NotNullWhen(returnValue: true)] out FrameworkElement? expandedContent)
        {
            var item = getDiagnosticTableItem(index)?.Data;
            if (item == null)
            {
                expandedContent = null;
                return false;
            }

            expandedContent = GetOrCreateTextBlock(ref _descriptions, this.Count, index, item, i => GetDescriptionTextBlock(i));
            return true;
        }

        protected static bool TryCreateDetailsStringContent(int index, Func<int, DiagnosticTableItem?> getDiagnosticTableItem, [NotNullWhen(returnValue: true)] out string? content)
        {
            var item = getDiagnosticTableItem(index)?.Data;
            if (item == null)
            {
                content = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Description))
            {
                content = null;
                return false;
            }

            content = item.Description;
            return content != null;
        }

        private static FrameworkElement GetDescriptionTextBlock(DiagnosticData item)
        {
            return new TextBlock()
            {
                Background = null,
                Padding = new Thickness(10, 6, 10, 8),
                TextWrapping = TextWrapping.Wrap,
                Text = item.Description
            };
        }

        private static FrameworkElement GetOrCreateTextBlock(
            [NotNull] ref FrameworkElement[]? caches, int count, int index, DiagnosticData item, Func<DiagnosticData, FrameworkElement> elementCreator)
        {
            if (caches == null)
            {
                caches = new FrameworkElement[count];
            }

            if (caches[index] == null)
            {
                caches[index] = elementCreator(item);
            }

            return caches[index];
        }
    }
}
