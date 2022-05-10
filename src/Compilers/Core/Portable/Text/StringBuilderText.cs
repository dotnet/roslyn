// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Implementation of <see cref="SourceText"/> based on a <see cref="StringBuilder"/> input
    /// </summary>
    internal sealed partial class StringBuilderText : SourceText
    {
        /// <summary>
        /// Underlying string on which this SourceText instance is based
        /// </summary>
        private readonly StringBuilder _builder;

        private readonly Encoding? _encodingOpt;

        public StringBuilderText(StringBuilder builder, Encoding? encodingOpt, SourceHashAlgorithm checksumAlgorithm)
             : base(checksumAlgorithm: checksumAlgorithm)
        {
            RoslynDebug.Assert(builder != null);

            _builder = builder;
            _encodingOpt = encodingOpt;
        }

        public override Encoding? Encoding
        {
            get { return _encodingOpt; }
        }

        /// <summary>
        /// Underlying string which is the source of this SourceText instance
        /// </summary>
        internal StringBuilder Builder
        {
            get { return _builder; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="StringBuilderText"/>.
        /// </summary>
        public override int Length
        {
            get { return _builder.Length; }
        }

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="Length"/>.</exception>
        public override char this[int position]
        {
            get
            {
                if (position < 0 || position >= _builder.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                return _builder[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringBuilderText located within given span.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public override string ToString(TextSpan span)
        {
            if (span.End > _builder.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            return _builder.ToString(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken = default)
        {
            CheckSubSpan(span);

            if (span.Length == 0)
                return;

#if NETCOREAPP
            // Index of the current chunk within the text.
            int chunkOffset = 0;

            foreach (var chunk in this.Builder.GetChunks())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Start index of the span with respect to the current chunk.
                var startFromChunk = span.Start - chunkOffset;
                if (startFromChunk < chunk.Length)
                {
                    // End infex of the span with respect to the current chunk.
                    var endFromChunk = span.End - chunkOffset;

                    var start = Math.Max(startFromChunk, 0);
                    var end = Math.Min(endFromChunk, chunk.Length);

                    textWriter.Write(chunk.Span[start..end]);

                    if (endFromChunk <= chunk.Length)
                        break;
                }

                chunkOffset += chunk.Length;
            }
#else
            base.Write(textWriter, span, cancellationToken);
#endif
        }
    }
}
