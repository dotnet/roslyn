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

        public static ImmutableArray<TableItem<T>> MergeDuplicatesOrderedBy<T>(this IEnumerable<IList<TableItem<T>>> groupedItems, Func<IEnumerable<TableItem<T>>, IEnumerable<TableItem<T>>> orderer)
        {
            var builder = ArrayBuilder<TableItem<T>>.GetInstance();
            foreach (var item in orderer(groupedItems.Select(g => g.Deduplicate())))
            {
                builder.Add(item);
            }

            return builder.ToImmutableAndFree();
        }

        private static TableItem<T> Deduplicate<T>(this IList<TableItem<T>> duplicatedItems)
        {
            if (duplicatedItems.Count == 1)
            {
                return duplicatedItems[0];
            }

#if DEBUG
            var key = duplicatedItems[0].DeduplicationKey;
            foreach (var item in duplicatedItems)
            {
                Contract.ThrowIfFalse(item.DeduplicationKey == key);
            }
#endif

            return new TableItem<T>(duplicatedItems);
        }

        public static ImmutableArray<ITrackingPoint> CreateTrackingPoints<TData>(
            this Workspace workspace, DocumentId documentId, ImmutableArray<TableItem<TData>> items)
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

        private static ITrackingPoint CreateTrackingPoint<TData>(TableItem<TData> item, ITextSnapshot snapshot)
        {
            if (snapshot.Length == 0)
            {
                return snapshot.CreateTrackingPoint(0, PointTrackingMode.Negative);
            }

            var position = item.GetTrackingPosition();
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
