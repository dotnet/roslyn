// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableEntriesSource<TData>
    {
        public AbstractTableEntriesSource()
        {
        }

        public abstract ImmutableArray<TData> GetItems();
        public abstract ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TData> items);
        public abstract AbstractTableEntriesSnapshot<TData> CreateSnapshot(int version, ImmutableArray<TData> items, ImmutableArray<ITrackingPoint> trackingPoints);

        protected ImmutableArray<ITrackingPoint> CreateTrackingPoints(
            Workspace workspace, DocumentId documentId,
            ImmutableArray<TData> items, Func<TData, ITextSnapshot, ITrackingPoint> converter)
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
                return items.Select(d => converter(d, snapshot)).ToImmutableArray();
            }

            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var currentSnapshot = textBuffer.CurrentSnapshot;
            return items.Select(d => converter(d, currentSnapshot)).ToImmutableArray();
        }

        protected ITrackingPoint CreateTrackingPoint(ITextSnapshot snapshot, int line, int column)
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
    }
}
