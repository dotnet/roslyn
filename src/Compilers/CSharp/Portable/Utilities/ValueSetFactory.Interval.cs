// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// An interval, which is part of an interval tree.
        /// </summary>
        private abstract class Interval
        {
            private Interval() { }

            /// <summary>
            /// A subinterval that includes all elements.
            /// </summary>
            internal sealed class Included : Interval
            {
                private Included() { }
                public static readonly Interval Instance = new Included();
            }

            /// <summary>
            /// A subinterval that excludes all elements.
            /// </summary>
            internal sealed class Excluded : Interval
            {
                private Excluded() { }
                public static readonly Interval Instance = new Excluded();
            }

            /// <summary>
            /// A mixed subinterval, in which some elements are included and some excluded.
            /// </summary>
            internal sealed class Mixed : Interval
            {
                public readonly Interval Left, Right;

                private Mixed(Interval Left, Interval Right)
                {
                    Debug.Assert(!(Left is Included && Right is Included));
                    Debug.Assert(!(Left is Excluded && Right is Excluded));
                    (this.Left, this.Right) = (Left, Right);
                }

                public static Interval Create(Interval left, Interval right) => (left, right) switch
                {
                    (Included _, Included _) => Included.Instance,
                    (Excluded _, Excluded _) => Excluded.Instance,
                    _ => new Mixed(left, right)
                };

                public void Deconstruct(out Interval Left, out Interval Right) => (Left, Right) = (this.Left, this.Right);

                public override bool Equals(object? obj) => obj is Mixed other && Left.Equals(other.Left) && Right.Equals(other.Right);

                public override int GetHashCode() => Hash.Combine(Left.GetHashCode(), Right.GetHashCode());
            }
        }
    }
}
