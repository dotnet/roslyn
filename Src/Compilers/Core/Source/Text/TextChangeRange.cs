// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Represents the change to a span of text.
    /// </summary>
    public struct TextChangeRange : IEquatable<TextChangeRange>
    {
        /// <summary>
        /// The span of text before the edit which is being changed
        /// </summary>
        public TextSpan Span { get; private set; }

        /// <summary>
        /// Width of the span after the edit.  A 0 here would represent a delete
        /// </summary>
        public int NewLength { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="T:TextChangeRange"/>.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="newLength"></param>
        public TextChangeRange(TextSpan span, int newLength)
            : this()
        {
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength");
            }

            this.Span = span;
            this.NewLength = newLength;
        }

        /// <summary>
        /// Compares current instance of <see cref="T:TextChangeRange"/> to another.
        /// </summary>
        public bool Equals(TextChangeRange other)
        {
            return
                other.Span == this.Span &&
                other.NewLength == this.NewLength;
        }

        /// <summary>
        /// Compares current instance of <see cref="T:TextChangeRange"/> to another.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is TextChangeRange && Equals((TextChangeRange)obj);
        }

        /// <summary>
        /// Provides hash code for current instnce of <see cref="T:TextChangeRange"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Hash.Combine(this.NewLength, this.Span.GetHashCode());
        }

        /// <summary>
        /// Determines if two instances of <see cref="T:TextChangeRange"/> are same.
        /// </summary>
        public static bool operator ==(TextChangeRange left, TextChangeRange right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines if two instances of <see cref="T:TextChangeRange"/> are different.
        /// </summary>
        public static bool operator !=(TextChangeRange left, TextChangeRange right)
        {
            return !(left == right);
        }

        /// <summary>
        /// An empty set of changes.
        /// </summary>
        public static readonly IReadOnlyList<TextChangeRange> NoChanges = SpecializedCollections.EmptyReadOnlyList<TextChangeRange>();
    }
}