// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal static class Extensions
    {
        public static ImmutableArray<TResult> ToImmutableArray<TSource, TResult>(this IList<TSource> list, Func<TSource, TResult> selector)
        {
            var builder = ImmutableArray.CreateBuilder<TResult>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                builder.Add(selector(list[i]));
            }

            return builder.ToImmutable();
        }

        public static ImmutableArray<TableItem<T>> MergeDuplicatesOrderedBy<T>(this IEnumerable<IList<TableItem<T>>> groupedItems, Func<IEnumerable<TableItem<T>>, IEnumerable<TableItem<T>>> orderer)
        {
            var builder = ImmutableArray.CreateBuilder<TableItem<T>>();
            foreach (var item in orderer(groupedItems.Select(g => g.Deduplicate())))
            {
                builder.Add(item);
            }

            return builder.ToImmutable();
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
            this Workspace workspace, DocumentId documentId,
            ImmutableArray<TableItem<TData>> items, Func<TData, ITextSnapshot, ITrackingPoint> converter)
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

            SourceText text;
            if (!document.TryGetText(out text))
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot != null)
            {
                return items.Select(d => converter(d.Primary, snapshot)).ToImmutableArray();
            }

            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var currentSnapshot = textBuffer.CurrentSnapshot;
            return items.Select(d => converter(d.Primary, currentSnapshot)).ToImmutableArray();
        }

        public static ITrackingPoint CreateTrackingPoint(this ITextSnapshot snapshot, int line, int column)
        {
            if (snapshot.Length == 0)
            {
                return snapshot.CreateTrackingPoint(0, PointTrackingMode.Negative);
            }

            if (line >= snapshot.LineCount)
            {
                return snapshot.CreateTrackingPoint(snapshot.Length, PointTrackingMode.Positive);
            }

            var adjustedLine = Math.Max(line, 0);
            var textLine = snapshot.GetLineFromLineNumber(adjustedLine);
            if (column >= textLine.Length)
            {
                return snapshot.CreateTrackingPoint(textLine.End, PointTrackingMode.Positive);
            }

            var adjustedColumn = Math.Max(column, 0);
            return snapshot.CreateTrackingPoint(textLine.Start + adjustedColumn, PointTrackingMode.Positive);
        }

        public static string GetProjectName(this Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            var projectNames = GetProjectNames(workspace, projectIds);
            if (projectNames.Length == 0)
            {
                return null;
            }

            return string.Join(", ", projectNames.OrderBy(StringComparer.CurrentCulture));
        }

        public static string GetProjectName(this Workspace workspace, ProjectId projectId)
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

        public static string[] GetProjectNames(this Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectName(workspace, p)).WhereNotNull().Distinct().ToArray();
        }

        public static Guid GetProjectGuid(this Workspace workspace, ProjectId projectId)
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

        public static Guid[] GetProjectGuids(this Workspace workspace, ImmutableArray<ProjectId> projectIds)
        {
            return projectIds.Select(p => GetProjectGuid(workspace, p)).Where(g => g != Guid.Empty).Distinct().ToArray();
        }

        public static DocumentId GetDocumentId<T>(T item)
        {
            // item must be either one of diagnostic data and todo item
            var diagnostic = item as DiagnosticData;
            if (diagnostic != null)
            {
                return diagnostic.DocumentId;
            }

            var todo = item as TodoItem;
            Contract.ThrowIfNull(todo);

            return todo.DocumentId;
        }

        public static ProjectId GetProjectId<T>(T item)
        {
            // item must be either one of diagnostic data and todo item
            var diagnostic = item as DiagnosticData;
            if (diagnostic != null)
            {
                return diagnostic.ProjectId;
            }

            var todo = item as TodoItem;
            Contract.ThrowIfNull(todo);

            return todo.DocumentId.ProjectId;
        }

        public static Workspace GetWorkspace<T>(T item)
        {
            // item must be either one of diagnostic data and todo item
            var diagnostic = item as DiagnosticData;
            if (diagnostic != null)
            {
                return diagnostic.Workspace;
            }

            var todo = item as TodoItem;
            Contract.ThrowIfNull(todo);

            return todo.Workspace;
        }
    }
}