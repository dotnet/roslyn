//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public static class TextExtensions
    {
        public static IText Replace(this IText text, TextSpan span, string newText)
        {
            return text.WithChanges(new TextChange(span, newText));
        }

        public static IText Replace(this IText text, int start, int length, string newText)
        {
            return text.Replace(new TextSpan(start, length), newText);
        }

        /// <summary>
        /// Construct a new IText with the specified changes.
        /// </summary>
        public static IText WithChanges(this IText text, params TextChange[] changes)
        {
            return text.WithChanges((IEnumerable<TextChange>)changes);
        }

        /// <summary>
        /// Constructs a new text that has the contents of this text including and after the start position.
        /// </summary>
        public static IText GetSubText(this IText text, int start)
        {
            if (start < 0 || start > text.Length)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (start == 0)
            {
                return text;
            }
            else if (start == text.Length)
            {
                return new StringText(string.Empty);
            }
            else
            {
                return text.GetSubText(new TextSpan(start, text.Length - start));
            }
        }

        /// <summary>
        /// Gets the set of TextChanges that describe how the text changed
        /// between old and new versions. Some containers keep track of changes between
        /// text instances and may report multiple detailed changes. Others many simply report
        /// a single change from old to new encompassing the entire text.
        /// </summary>
        public static IEnumerable<TextChange> GetTextChanges(this IText newText, IText oldText)
        {
            int newPosDelta = 0;

            var ranges = newText.GetChangeRanges(oldText).ToList();
            var textChanges = new List<TextChange>(ranges.Count);

            foreach (var range in ranges)
            {
                var newPos = range.Span.Start + newPosDelta;

                // determine where in the newText this text exists
                string newt;
                if (range.NewLength > 0)
                {
                    var span = new TextSpan(newPos, range.NewLength);
                    newt = newText.ToString(span);
                }
                else
                {
                    newt = string.Empty;
                }

                textChanges.Add(new TextChange(range.Span, newt));

                newPosDelta += range.NewLength - range.Span.Length;
            }

            return textChanges;
        }
    }
}