// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Implementation of SourceText based on a <see cref="T:System.String"/> input
    /// </summary>
    internal sealed partial class StringBuilderText : SourceText
    {
        /// <summary>
        /// Underlying string on which this SourceText instance is based
        /// </summary>
        private readonly StringBuilder builder;

        private readonly Encoding encodingOpt;

        public StringBuilderText(StringBuilder builder, Encoding encodingOpt)
        {
            Debug.Assert(builder != null);

            this.builder = builder;
            this.encodingOpt = encodingOpt;
        }

        public override Encoding Encoding
        {
            get { return encodingOpt; }
        }

        /// <summary>
        /// Underlying string which is the source of this SourceText instance
        /// </summary>
        internal StringBuilder Builder
        {
            get { return this.builder; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="T:StringText"/>.
        /// </summary>
        public override int Length
        {
            get { return this.builder.Length; }
        }

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="T:ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="T:"/> length.</exception>
        public override char this[int position]
        {
            get
            {
                if (position < 0 || position >= this.builder.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                return this.builder[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringBuilderText located within given span.
        /// </summary>
        /// <exception cref="T:ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public override string ToString(TextSpan span)
        {
            if (span.End > this.builder.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            return this.builder.ToString(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
