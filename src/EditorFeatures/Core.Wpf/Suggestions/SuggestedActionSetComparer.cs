// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class SuggestedActionSetComparer : IComparer<SuggestedActionSet>
    {
        private readonly TextSpan? _targetSpan;

        public SuggestedActionSetComparer(TextSpan? targetSpan)
        {
            _targetSpan = targetSpan;
        }

        private static int Distance(Span? textSpan, TextSpan? targetSpan)
        {
            // If we don't have a text span or target point we cannot calculate the distance between them
            if (!textSpan.HasValue || !targetSpan.HasValue)
            {
                return int.MaxValue;
            }

            var span = textSpan.Value;
            var target = targetSpan.Value;

            // The distance of two spans is the sum of distance of their starts and their ends
            var startsDistance = Math.Abs(span.Start - target.Start);
            var endsDistance = Math.Abs(span.End - target.End);

            return startsDistance + endsDistance;
        }

        public int Compare(SuggestedActionSet x, SuggestedActionSet y)
        {
            if (!_targetSpan.HasValue || !x.ApplicableToSpan.HasValue || !y.ApplicableToSpan.HasValue)
            {
                // Not enough data to compare, consider them equal
                return 0;
            }

            var distanceX = Distance(x.ApplicableToSpan, _targetSpan);
            var distanceY = Distance(y.ApplicableToSpan, _targetSpan);

            return distanceX.CompareTo(distanceY);
        }
    }
}
