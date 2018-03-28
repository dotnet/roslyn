// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class SuggestedActionSetComparer : IComparer<SuggestedActionSet>
    {
        private readonly SnapshotPoint? _targetPoint;

        public SuggestedActionSetComparer(SnapshotPoint? targetPoint)
        {
            _targetPoint = targetPoint;
        }

        private static int Distance(Span? textSpan, SnapshotPoint? targetPoint)
        {
            // If we don't have a text span or target point we cannot calculate the distance between them
            if (!textSpan.HasValue || !targetPoint.HasValue)
            {
                return int.MaxValue;
            }

            var span = textSpan.Value;
            var position = targetPoint.Value.Position;

            if (position < span.Start)
            {
                return span.Start - position;
            }
            else if (position > span.End)
            {
                return position - span.End;
            }
            else
            {
                return 0;
            }
        }

        public int Compare(SuggestedActionSet x, SuggestedActionSet y)
        {
            if (!_targetPoint.HasValue || !x.ApplicableToSpan.HasValue || !y.ApplicableToSpan.HasValue)
            {
                // Not enough data to compare, consider them equal
                return 0;
            }

            var distanceX = Distance(x.ApplicableToSpan, _targetPoint);
            var distanceY = Distance(y.ApplicableToSpan, _targetPoint);

            if (distanceX != 0 || distanceY != 0)
            {
                return distanceX.CompareTo(distanceY);
            }

            // This is the case when both actions sets' spans contain the trigger point.
            // Now we compare first by start position then by end position. 
            var targetPosition = _targetPoint.Value.Position;

            var distanceToStartX = targetPosition - x.ApplicableToSpan.Value.Start;
            var distanceToStartY = targetPosition - y.ApplicableToSpan.Value.Start;

            if (distanceToStartX != distanceToStartY)
            {
                return distanceToStartX.CompareTo(distanceToStartY);
            }

            var distanceToEndX = x.ApplicableToSpan.Value.End - targetPosition;
            var distanceToEndY = y.ApplicableToSpan.Value.End - targetPosition;

            return distanceToEndX.CompareTo(distanceToEndY);
        }
    }
}
