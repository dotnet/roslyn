﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Implementation of SourceText based on a <see cref="String"/> input
    /// </summary>
    internal sealed class StringText : SourceText
    {
        private readonly string _source;
        private readonly Encoding? _encodingOpt;

        internal StringText(
            string source,
            Encoding? encodingOpt,
            ImmutableArray<byte> checksum = default(ImmutableArray<byte>),
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            ImmutableArray<byte> embeddedTextBlob = default(ImmutableArray<byte>))
            : base(checksum, checksumAlgorithm, embeddedTextBlob)
        {
            RoslynDebug.Assert(source != null);

            _source = source;
            _encodingOpt = encodingOpt;
        }

        public override Encoding? Encoding => _encodingOpt;

        /// <summary>
        /// Underlying string which is the source of this <see cref="StringText"/>instance
        /// </summary>
        public string Source => _source;

        /// <summary>
        /// The length of the text represented by <see cref="StringText"/>.
        /// </summary>
        public override int Length => _source.Length;

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
                // NOTE: we are not validating position here as that would not 
                //       add any value to the range check that string accessor performs anyways.

                return _source[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringText located within given span.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public override string ToString(TextSpan span)
        {
            if (span.End > this.Source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (span.Start == 0 && span.Length == this.Length)
            {
                return this.Source;
            }

            return this.Source.Substring(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.Source.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSubSpan(span);

            if (span.Length == 0)
                return;

            if (span.Start == 0 && span.Length == this.Length)
            {
                // Even on .NET Core, prefer the string overload of Write, because in case the writer is a custom TextWriter that
                // doesn't override the ReadOnlySpan<char> overload, it delegates to the array one under the hood.
                textWriter.Write(this.Source);
                return;
            }

#if NETCOREAPP
            textWriter.Write(this.Source.AsSpan(span.Start, span.Length));
#else
            base.Write(textWriter, span, cancellationToken);
#endif
        }
    }
}
