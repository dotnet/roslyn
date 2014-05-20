// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A composite of a sequence of <see cref="SourceText"/>s.
    /// </summary>
    internal sealed class CompositeText : SourceText
    {
        private readonly ImmutableArray<SourceText> texts;
        private readonly int length;

        public CompositeText(ImmutableArray<SourceText> texts)
        {
            Debug.Assert(!texts.IsDefaultOrEmpty);
            Debug.Assert(texts.All(t => texts.First().Encoding == t.Encoding));

            this.texts = texts;
            int len = 0;
            foreach (var text in texts)
            {
                len += text.Length;
            }

            this.length = len;
        }

        public override Encoding Encoding
        {
            get { return texts[0].Encoding; }
        }

        public override int Length
        {
            get { return this.length; }
        }

        public override char this[int position]
        {
            get
            {
                int index;
                int offset;
                GetIndexAndOffset(position, out index, out offset);
                return this.texts[index][offset];
            }
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            var sourceIndex = span.Start;
            var count = span.Length;

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);

            var newTexts = ArrayBuilder<SourceText>.GetInstance();
            while (segIndex < this.texts.Length && count > 0)
            {
                var segment = this.texts[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);

                AddSegments(newTexts, segment.GetSubText(new TextSpan(segOffset, copyLength)));

                count -= copyLength;
                segIndex++;
                segOffset = 0;
            }

            if (newTexts.Count == 0)
            {
                newTexts.Free();
                return SourceText.From(string.Empty, this.Encoding);
            }
            else if (newTexts.Count == 1)
            {
                SourceText result = newTexts[0];
                newTexts.Free();
                return result;
            }
            else
            {
                return new CompositeText(newTexts.ToImmutableAndFree());
            }
        }

        private void GetIndexAndOffset(int position, out int index, out int offset)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                var segment = this.texts[i];
                if (position < segment.Length)
                {
                    index = i;
                    offset = position;
                    return;
                }
                else
                {
                    position -= segment.Length;
                }
            }

            index = 0;
            offset = 0;
            throw new ArgumentException("position");
        }

        /// <summary>
        /// Validates the arguments passed to CopyTo against the published contract.
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="destination"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        /// <returns>True if should bother to proceed with copying.</returns>
        private bool CheckCopyToArguments(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex");

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex");

            if (count < 0 || count > this.Length - sourceIndex || count > destination.Length - destinationIndex)
                throw new ArgumentOutOfRangeException("count");

            return count > 0;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!CheckCopyToArguments(sourceIndex, destination, destinationIndex, count))
                return;

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);

            while (segIndex < this.texts.Length && count > 0)
            {
                var segment = this.texts[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);

                segment.CopyTo(segOffset, destination, destinationIndex, copyLength);

                count -= copyLength;
                destinationIndex += copyLength;
                segIndex++;
                segOffset = 0;
            }
        }

        internal static void AddSegments(ArrayBuilder<SourceText> builder, SourceText text)
        {
            CompositeText composite = text as CompositeText;
            if (composite == null)
            {
                builder.Add(text);
            }
            else
            {
                builder.AddRange(composite.texts);
            }
        }
    }
}