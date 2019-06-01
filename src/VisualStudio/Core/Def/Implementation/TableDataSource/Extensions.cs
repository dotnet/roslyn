// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal static class Extensions
    {
        public static ImmutableArray<TResult> ToImmutableArray<TSource, TResult>(this IList<TSource> list, Func<TSource, TResult> selector)
        {
            var builder = ArrayBuilder<TResult>.GetInstance(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                builder.Add(selector(list[i]));
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TItem> MergeDuplicatesOrderedBy<TItem>(this IEnumerable<IList<TItem>> groupedItems, Func<IEnumerable<TItem>, IEnumerable<TItem>> orderer)
            where TItem : TableItem
        {
            var builder = ArrayBuilder<TItem>.GetInstance();
            foreach (var item in orderer(groupedItems.Select(Deduplicate)))
            {
                builder.Add(item);
            }

            return builder.ToImmutableAndFree();
        }

        private static TItem Deduplicate<TItem>(this IList<TItem> items)
            where TItem : TableItem
        {
            if (items.Count == 1)
            {
                return items[0];
            }

            Contract.ThrowIfFalse(items.Count == 0);
            Contract.ThrowIfTrue(items.Any(i => i.PrimaryDocumentId == null), "Contains an item with null PrimaryDocumentId");

#if DEBUG
            var key = items[0].DeduplicationKey;
            foreach (var item in items)
            {
                Contract.ThrowIfFalse(item.DeduplicationKey == key);
            }
#endif
            // deterministic ordering
            var orderedItems = items.OrderBy(i => i.PrimaryDocumentId.Id).ToList();

            // order of item is important. make sure we maintain it.
            int collectionHash = Hash.CombineValues(orderedItems.Select(item => item.PrimaryDocumentId.Id));
            var cache = SharedInfoCache.GetOrAdd(collectionHash, orderedItems, c => new SharedInfoCache(c.Select(i => i.PrimaryDocumentId).ToImmutableArray()));
            return (TItem)orderedItems[0].WithCache(cache);
        }

        public static ImmutableArray<ITrackingPoint> CreateTrackingPoints<TItem>(this Workspace workspace, DocumentId documentId, ImmutableArray<TItem> items)
            where TItem : TableItem
        {
            if (documentId == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var solution = workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);
            if (document == null || !document.IsOpen())
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            if (!document.TryGetText(out var text))
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot != null)
            {
                return items.SelectAsArray(CreateTrackingPoint, snapshot);
            }

            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            return items.SelectAsArray(CreateTrackingPoint, textBuffer.CurrentSnapshot);
        }

        private static ITrackingPoint CreateTrackingPoint(TableItem item, ITextSnapshot snapshot)
        {
            if (snapshot.Length == 0)
            {
                return snapshot.CreateTrackingPoint(0, PointTrackingMode.Negative);
            }

            var position = item.GetOriginalPosition();
            if (position.Line >= snapshot.LineCount)
            {
                return snapshot.CreateTrackingPoint(snapshot.Length, PointTrackingMode.Positive);
            }

            var adjustedLine = Math.Max(position.Line, 0);
            var textLine = snapshot.GetLineFromLineNumber(adjustedLine);
            if (position.Character >= textLine.Length)
            {
                return snapshot.CreateTrackingPoint(textLine.End, PointTrackingMode.Positive);
            }

            var adjustedColumn = Math.Max(position.Character, 0);
            return snapshot.CreateTrackingPoint(textLine.Start + adjustedColumn, PointTrackingMode.Positive);
        }

        public static string GetProjectName(this Solution solution, ImmutableArray<ProjectId> projectIds)
        {
            var projectNames = GetProjectNames(solution, projectIds);
            if (projectNames.Length == 0)
            {
                return null;
            }

            return string.Join(", ", projectNames.OrderBy(StringComparer.CurrentCulture));
        }

        public static string GetProjectName(this Solution solution, ProjectId projectId)
        {
            if (projectId == null)
            {
                return null;
            }

            var project = solution.GetProject(projectId);
            if (project == null)
            {
                return null;
            }

            return project.Name;
        }

        public static string[] GetProjectNames(this Solution solution, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectName(solution, p)).WhereNotNull().Distinct().ToArray();
        }

        public static Guid GetProjectGuid(this Workspace workspace, ProjectId projectId)
        {
            if (projectId == null)
            {
                return Guid.Empty;
            }

            var vsWorkspace = workspace as VisualStudioWorkspace;
            return vsWorkspace?.GetProjectGuid(projectId) ?? Guid.Empty;
        }

        public static Guid[] GetProjectGuids(this Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectGuid(workspace, p)).Where(g => g != Guid.Empty).Distinct().ToArray();
        }
    }
}
