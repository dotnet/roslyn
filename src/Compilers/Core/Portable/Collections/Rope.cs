// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A representation of a string of characters that requires O(1) extra space to concatenate two ropes.
    /// </summary>
    internal abstract class Rope
    {
        public static readonly Rope Empty = ForString("");
        public abstract override string ToString();
        public abstract string ToString(int maxLength);
        public abstract int Length { get; }
        protected abstract IEnumerable<char> GetChars();
        private Rope() { }

        /// <summary>
        /// A rope can wrap a simple string.
        /// </summary>
        public static Rope ForString(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            return new StringRope(s);
        }

        /// <summary>
        /// A rope can be formed from the concatenation of two ropes.
        /// </summary>
        public static Rope Concat(Rope r1, Rope r2)
        {
            if (r1 == null)
                throw new ArgumentNullException(nameof(r1));

            if (r2 == null)
                throw new ArgumentNullException(nameof(r2));

            return
                r1.Length == 0 ? r2 :
                r2.Length == 0 ? r1 :
                checked(r1.Length + r2.Length < 32) ? ForString(r1.ToString() + r2.ToString()) :
                new ConcatRope(r1, r2);
        }

        /// <summary>
        /// Two ropes are "the same" if they represent the same sequence of characters.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (!(obj is Rope other) || Length != other.Length)
                return false;
            if (Length == 0)
                return true;
            var chars0 = GetChars().GetEnumerator();
            var chars1 = other.GetChars().GetEnumerator();
            while (chars0.MoveNext() && chars1.MoveNext())
            {
                if (chars0.Current != chars1.Current)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = Length;
            foreach (char c in GetChars())
                result = Hash.Combine((int)c, result);

            return result;
        }

        /// <summary>
        /// A rope that wraps a simple string.
        /// </summary>
        private sealed class StringRope : Rope
        {
            private readonly string _value;
            public StringRope(string value) => _value = value;
            public override string ToString() => _value;

            public override string ToString(int maxLength)
            {
                return ToString(maxLength, out _);
            }

            public string ToString(int maxLength, out int wrote)
            {
                if (maxLength < 0)
                {
                    throw ExceptionUtilities.UnexpectedValue(nameof(maxLength));
                }

                wrote = Math.Min(maxLength, _value.Length);
                return _value[..wrote];
            }

            public override int Length => _value.Length;
            protected override IEnumerable<char> GetChars() => _value;
        }

        /// <summary>
        /// A rope that represents the concatenation of two ropes.
        /// </summary>
        private sealed class ConcatRope : Rope
        {
            private readonly Rope _left, _right;
            public override int Length { get; }

            public ConcatRope(Rope left, Rope right)
            {
                _left = left;
                _right = right;
                Length = checked(left.Length + right.Length);
            }

            public override string ToString()
            {
                var psb = PooledStringBuilder.GetInstance();
                var stack = new Stack<Rope>();
                stack.Push(this);
                while (stack.Count != 0)
                {
                    switch (stack.Pop())
                    {
                        case StringRope s:
                            psb.Builder.Append(s.ToString());
                            break;
                        case ConcatRope c:
                            stack.Push(c._right);
                            stack.Push(c._left);
                            break;
                        case var v:
                            throw ExceptionUtilities.UnexpectedValue(v.GetType().Name);
                    }
                }

                return psb.ToStringAndFree();
            }

            public override string ToString(int maxLength)
            {
                if (maxLength < 0)
                {
                    throw ExceptionUtilities.UnexpectedValue(nameof(maxLength));
                }

                var psb = PooledStringBuilder.GetInstance();
                var stack = new Stack<Rope>();
                stack.Push(this);

                int rem = maxLength;
                while (stack.Count != 0 && rem > 0)
                {
                    switch (stack.Pop())
                    {
                        case StringRope s:
                            psb.Builder.Append(s.ToString(rem, out var wrote));
                            rem -= wrote;
                            break;
                        case ConcatRope c:
                            stack.Push(c._right);
                            stack.Push(c._left);
                            break;
                        case var v:
                            throw ExceptionUtilities.UnexpectedValue(v.GetType().Name);
                    }
                }

                return psb.ToStringAndFree();
            }

            protected override IEnumerable<char> GetChars()
            {
                var stack = new Stack<Rope>();
                stack.Push(this);
                while (stack.Count != 0)
                {
                    switch (stack.Pop())
                    {
                        case StringRope s:
                            foreach (var c in s.ToString())
                                yield return c;
                            break;
                        case ConcatRope c:
                            stack.Push(c._right);
                            stack.Push(c._left);
                            break;
                        case var v:
                            throw ExceptionUtilities.UnexpectedValue(v.GetType().Name);
                    }
                }
            }
        }
    }
}
