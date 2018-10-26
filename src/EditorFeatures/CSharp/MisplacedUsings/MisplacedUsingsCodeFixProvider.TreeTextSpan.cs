// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        /// <summary>
        /// Immutable class representing a text span with a collection of children.
        /// </summary>
        private class TreeTextSpan : IEquatable<TreeTextSpan>, IComparable<TreeTextSpan>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TreeTextSpan"/> class.
            /// </summary>
            /// <param name="start">The start position for the span.</param>
            /// <param name="end">The end position for the span.</param>
            /// <param name="children">The children of the span.</param>
            internal TreeTextSpan(int start, int end, ImmutableArray<TreeTextSpan> children)
            {
                Start = start;
                End = end;
                Children = children;
            }

            internal static TreeTextSpan Empty { get; } = new TreeTextSpan(0, 0, ImmutableArray<TreeTextSpan>.Empty);

            /// <summary>
            /// Gets the start position of the span.
            /// </summary>
            /// <value>The start position within the source code.</value>
            internal int Start { get; }

            /// <summary>
            /// Gets the end position of the span.
            /// </summary>
            /// <value>The end position within the source code.</value>
            internal int End { get; }

            /// <summary>
            /// Gets the children of this span.
            /// </summary>
            /// <value>A read-only list containing the children.</value>
            internal ImmutableArray<TreeTextSpan> Children { get; }

            /// <summary>
            /// Determines if two instances of <see cref="TreeTextSpan"/> are the same.
            /// </summary>
            /// <param name="left">The first instance to compare.</param>
            /// <param name="right">The second instance to compare.</param>
            /// <returns>True if the instances are the same.</returns>
            public static bool operator ==(TreeTextSpan left, TreeTextSpan right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Determines if two instances of <see cref="TreeTextSpan"/> are the different.
            /// </summary>
            /// <param name="left">The first instance to compare.</param>
            /// <param name="right">The second instance to compare.</param>
            /// <returns>True if the instances are different.</returns>
            public static bool operator !=(TreeTextSpan left, TreeTextSpan right)
            {
                return !left.Equals(right);
            }

            /// <inheritdoc/>
            public bool Equals(TreeTextSpan other)
            {
                return (Start == other.Start) && (End == other.End);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return (obj is TreeTextSpan) && Equals((TreeTextSpan)obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Start + (End << 16);
                }
            }

            /// <inheritdoc/>
            public int CompareTo(TreeTextSpan other)
            {
                var diff = Start - other.Start;
                if (diff == 0)
                {
                    diff = End - other.End;
                }

                return diff;
            }

            /// <summary>
            /// Creates a new builder for a <see cref="TreeTextSpan"/>.
            /// </summary>
            /// <param name="start">The start of the span.</param>
            /// <returns>The created builder.</returns>
            internal static Builder CreateBuilder(int start)
            {
                return new Builder(start);
            }

            /// <summary>
            /// Checks if the given <paramref name="span"/> is contained within this span.
            /// </summary>
            /// <param name="span">The <see cref="TreeTextSpan"/> to check.</param>
            /// <returns>True if the given <paramref name="span"/> is contained.</returns>
            internal bool Contains(TreeTextSpan span)
            {
                return (span.Start >= Start) && (span.End <= End);
            }

            /// <summary>
            /// Gets smallest (child) span that contains the given <paramref name="textSpan"/>.
            /// This assumes non-overlapping children.
            /// </summary>
            /// <param name="textSpan">The span to check.</param>
            /// <returns>The <see cref="TreeTextSpan"/> that is the best match, or null if there is no match.</returns>
            internal TreeTextSpan GetContainingSpan(TextSpan textSpan)
            {
                if ((textSpan.Start < Start) || (textSpan.End > End))
                {
                    return Empty;
                }

                foreach (var span in Children)
                {
                    var childSpan = span.GetContainingSpan(textSpan);
                    if (childSpan != Empty)
                    {
                        return childSpan;
                    }
                }

                return this;
            }

            /// <summary>
            /// Helper class that can be used to construct a tree of <see cref="TreeTextSpan"/> objects.
            /// </summary>
            internal class Builder
            {
                private readonly List<Builder> _children = new List<Builder>();
                private int _start;
                private int _end = int.MaxValue;

                /// <summary>
                /// Initializes a new instance of the <see cref="Builder"/> class.
                /// </summary>
                /// <param name="start">The start of the span.</param>
                internal Builder(int start)
                {
                    _start = start;
                }

                private Builder(int start, int end)
                {
                    _start = start;
                    _end = end;
                }

                /// <summary>
                /// Sets the end of the span.
                /// </summary>
                /// <param name="end">The end of the span.</param>
                internal void SetEnd(int end)
                {
                    _end = end;
                }

                /// <summary>
                /// Add a new child to the span.
                /// </summary>
                /// <param name="start">The start of the child span.</param>
                /// <returns>The <see cref="Builder"/> for the child.</returns>
                internal Builder AddChild(int start)
                {
                    var childBuilder = new Builder(start);
                    _children.Add(childBuilder);

                    return childBuilder;
                }

                /// <summary>
                /// Makes sure that the gaps between children are filled.
                /// These extra spans are created to make sure that using statements will not be moved over directive boundaries.
                /// </summary>
                internal void FillGaps()
                {
                    Builder newFiller;

                    if (_children.Count == 0)
                    {
                        return;
                    }

                    var previousEnd = int.MaxValue;
                    for (var i = 0; i < _children.Count; i++)
                    {
                        var child = _children[i];

                        if (child._start > previousEnd)
                        {
                            newFiller = new Builder(previousEnd, child._start);
                            _children.Insert(i, newFiller);
                            i++;
                        }

                        child.FillGaps();

                        previousEnd = child._end;
                    }

                    if (previousEnd < _end)
                    {
                        newFiller = new Builder(previousEnd, _end);
                        _children.Add(newFiller);
                    }
                }

                /// <summary>
                /// Converts the builder (and its children) to a <see cref="TreeTextSpan"/> object.
                /// </summary>
                /// <returns>The created <see cref="TreeTextSpan"/> object.</returns>
                internal TreeTextSpan ToSpan()
                {
                    var children = _children.Select(x => x.ToSpan()).ToImmutableArray();

                    return new TreeTextSpan(_start, _end, children);
                }
            }
        }
    }
}
