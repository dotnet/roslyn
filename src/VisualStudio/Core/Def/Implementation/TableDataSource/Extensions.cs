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
    }
}
