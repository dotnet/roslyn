// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ImmutableArray<SourceText> _texts;
        private readonly int _length;

        public CompositeText(ImmutableArray<SourceText> texts)
            : base(checksumAlgorithm: texts[0].ChecksumAlgorithm)
        {
            Debug.Assert(!texts.IsDefaultOrEmpty);
            Debug.Assert(texts.All(t => texts.First().Encoding == t.Encoding && texts.First().ChecksumAlgorithm == t.ChecksumAlgorithm));

            _texts = texts;
            int len = 0;
            foreach (var text in texts)
            {
                len += text.Length;
            }

            _length = len;
        }

        public override Encoding Encoding
        {
            get { return _texts[0].Encoding; }
        }

        public override int Length
        {
            get { return _length; }
        }

        public override char this[int position]
        {
            get
            {
                int index;
                int offset;
                GetIndexAndOffset(position, out index, out offset);
                return _texts[index][offset];
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
            while (segIndex < _texts.Length && count > 0)
            {
                var segment = _texts[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);

                AddSegments(newTexts, segment.GetSubText(new TextSpan(segOffset, copyLength)));

                count -= copyLength;
                segIndex++;
                segOffset = 0;
            }

            if (newTexts.Count == 0)
            {
                newTexts.Free();
                return SourceText.From(string.Empty, this.Encoding, this.ChecksumAlgorithm);
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
            for (int i = 0; i < _texts.Length; i++)
            {
                var segment = _texts[i];
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
        /// Validates the arguments passed to <see cref="CopyTo"/> against the published contract.
        /// </summary>
        /// <returns>True if should bother to proceed with copying.</returns>
        private bool CheckCopyToArguments(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

            if (count < 0 || count > this.Length - sourceIndex || count > destination.Length - destinationIndex)
                throw new ArgumentOutOfRangeException(nameof(count));

            return count > 0;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!CheckCopyToArguments(sourceIndex, destination, destinationIndex, count))
                return;

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);

            while (segIndex < _texts.Length && count > 0)
            {
                var segment = _texts[segIndex];
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
                builder.AddRange(composite._texts);
            }
        }
    }
}
