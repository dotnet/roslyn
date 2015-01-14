// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Compilers.Internal;

namespace Microsoft.Languages.Text
{
    /// <summary>
    /// Contains the span information for a line in the text
    /// </summary>
    public struct TextLineSpan : IEquatable<TextLineSpan>
    {
        private readonly TextSpan _textSpan;
        private readonly int _lineBreakLength;

        /// <summary>
        /// The start position of the line span.
        /// </summary>
        public int Start
        {
            get { return _textSpan.Start; }
        }

        /// <summary>
        /// The end position of the line span which does not include the line break.  
        /// </summary>
        public int End
        {
            get { return _textSpan.End; }
        }

        /// <summary>
        /// The span of the text within the line.
        /// </summary>
        public TextSpan Span
        {
            get { return _textSpan; }
        }

        /// <summary>
        /// The length of the Line which does not include line break.
        /// </summary>
        public int Length
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _textSpan.Length;
            }
        }

        /// <summary>
        /// The length of the line break. 
        /// </summary>
        /// <remarks>
        /// For all cases but the final line this will have a non-zero value. 
        /// A length of 0 indicates the end of the buffer.
        /// </remarks>
        public int LineBreakLength
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _lineBreakLength;
            }
        }

        /// <summary>
        /// The span of the line break. This may be a 0 length span for the end of a given line.
        /// </summary>
        public TextSpan LineBreakSpan
        {
            get { return new TextSpan(_textSpan.End, _lineBreakLength); }
        }

        /// <summary>
        /// The span of the entire line including the line break.
        /// </summary>
        public TextSpan SpanIncludingLineBreak
        {
            get { return new TextSpan(_textSpan.Start, LengthIncludingLineBreak); }
        }

        /// <summary>
        /// The length of the line span including line break. 
        /// </summary>
        public int LengthIncludingLineBreak
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _textSpan.Length + _lineBreakLength;
            }
        }

        /// <summary>
        /// Creates a TextLineSpan from the given span of text and with a line break having the specified length.
        /// </summary>
        /// <param name="bodySpan">the text span</param>
        /// <param name="lineBreakLength">The length of the line break.</param>
        public TextLineSpan(TextSpan bodySpan, int lineBreakLength)
        {
            Contract.Requires(lineBreakLength >= 0);
            _textSpan = bodySpan;
            _lineBreakLength = lineBreakLength;
        }

        /// <summary>
        /// Creates a new TextLineSpan from the given parameters.
        /// </summary>
        /// <param name="start">The start position of the TextLineSpan</param>
        /// <param name="length">The length of the span not including the line break</param>
        /// <param name="lineBreakLength">The length of the line break section of the line</param>
        public TextLineSpan(int start, int length, int lineBreakLength)
        {
            Contract.Requires(start >= 0);
            Contract.Requires(length >= 0);
            Contract.Requires(lineBreakLength >= 0);
            _textSpan = new TextSpan(start, length);
            _lineBreakLength = lineBreakLength;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_lineBreakLength >= 0);
        }

        /// <summary>
        /// Determines whether two specified <see cref="TextLineSpan"/> objects have the same value.
        /// </summary>
        /// <param name="left">The left object to compare</param>
        /// <param name="right">The right object to compare</param>
        /// <returns></returns>
        public static bool operator ==(TextLineSpan left, TextLineSpan right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified <see cref="TextLineSpan"/> objects have different values.
        /// </summary>
        /// <param name="left">The left object to compare</param>
        /// <param name="right">The right object to compare</param>
        /// <returns></returns>
        public static bool operator !=(TextLineSpan left, TextLineSpan right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether this instance and another specified <see cref="TextLineSpan"/> object have the same value.
        /// </summary>
        /// <param name="span">The span to compare for equal</param>
        /// <returns></returns>
        public bool Equals(TextLineSpan span)
        {
            return _textSpan == span._textSpan && _lineBreakLength == span._lineBreakLength;
        }

        /// <summary>
        /// Determines whether this instance and a specified object have the same value.
        /// </summary>
        /// <param name="obj">The specified object to compare, which must also be a <see cref="TextLineSpan"/> object,</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (obj is TextLineSpan) && Equals((TextLineSpan)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _textSpan.GetHashCode() ^ _lineBreakLength;
        }

        /// <summary>
        /// Converts the value of this instance to a <see cref="string"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0}, {1}+{2}]", this.Start, this.End, this.LineBreakLength );
        }
    }
}
