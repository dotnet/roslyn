// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Describes a single change when a particular span is replaced with a new text.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public readonly struct TextChange : IEquatable<TextChange>
    {
        /// <summary>
        /// The original span of the changed text. 
        /// </summary>
        [DataMember(Order = 0)]
        public TextSpan Span { get; }

        /// <summary>
        /// The new text.
        /// </summary>
        [DataMember(Order = 1)]
        public string? NewText { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TextChange"/>
        /// </summary>
        /// <param name="span">The original span of the changed text.</param>
        /// <param name="newText">The new text.</param>
        public TextChange(TextSpan span, string newText)
            : this()
        {
            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            this.Span = span;
            this.NewText = newText;
        }

        /// <summary>
        /// Provides a string representation for <see cref="TextChange"/>.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}: {{ {1}, \"{2}\" }}", this.GetType().Name, Span, NewText);
        }

        public override bool Equals(object? obj)
        {
            return obj is TextChange && this.Equals((TextChange)obj);
        }

        public bool Equals(TextChange other)
        {
            return
                EqualityComparer<TextSpan>.Default.Equals(this.Span, other.Span) &&
                EqualityComparer<string>.Default.Equals(this.NewText, other.NewText);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Span.GetHashCode(), this.NewText?.GetHashCode() ?? 0);
        }

        public static bool operator ==(TextChange left, TextChange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextChange left, TextChange right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Converts a <see cref="TextChange"/> to a <see cref="TextChangeRange"/>.
        /// </summary>
        /// <param name="change"></param>
        public static implicit operator TextChangeRange(TextChange change)
        {
            Debug.Assert(change.NewText is object);
            return new TextChangeRange(change.Span, change.NewText.Length);
        }

        /// <summary>
        /// An empty set of changes.
        /// </summary>
        public static IReadOnlyList<TextChange> NoChanges => SpecializedCollections.EmptyReadOnlyList<TextChange>();

        internal string GetDebuggerDisplay()
        {
            var newTextDisplay = NewText switch
            {
                null => "null",
                { Length: < 10 } => $"\"{NewText}\"",
                { Length: var length } => $"(NewLength = {length})"
            };
            return $"new TextChange(new TextSpan({Span.Start}, {Span.Length}), {newTextDisplay})";
        }
    }
}
