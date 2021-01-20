// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    internal static partial class TestExtensionsMethods
    {
        internal static IDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, nameof(dictionary));

            return (IDictionary<TKey, TValue>)dictionary;
        }

        internal static IDictionary<TKey, TValue> ToBuilder<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, nameof(dictionary));

            var hashDictionary = dictionary as ImmutableDictionary<TKey, TValue>;
            if (hashDictionary != null)
            {
                return hashDictionary.ToBuilder();
            }

            var sortedDictionary = dictionary as ImmutableSortedDictionary<TKey, TValue>;
            if (sortedDictionary != null)
            {
                return sortedDictionary.ToBuilder();
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Verifies that a binary tree is balanced according to AVL rules.
        /// </summary>
        /// <param name="node">The root node of the binary tree.</param>
        internal static void VerifyBalanced(this IBinaryTree node)
        {
            if (node.Left != null)
            {
                VerifyBalanced(node.Left);
            }

            if (node.Right != null)
            {
                VerifyBalanced(node.Right);
            }

            if (node.Right != null && node.Left != null)
            {
                Assert.InRange(node.Left.Height - node.Right.Height, -1, 1);
            }
            else if (node.Right != null)
            {
                Assert.InRange(node.Right.Height, 0, 1);
            }
            else if (node.Left != null)
            {
                Assert.InRange(node.Left.Height, 0, 1);
            }
        }

        /// <summary>
        /// Verifies that a binary tree is no taller than necessary to store the data if it were optimally balanced.
        /// </summary>
        /// <param name="node">The root node.</param>
        /// <param name="count">The number of nodes in the tree. May be <c>null</c> if <see cref="IBinaryTree.Count"/> is functional.</param>
        internal static void VerifyHeightIsWithinTolerance(this IBinaryTree node, int? count = null)
        {
            // http://en.wikipedia.org/wiki/AVL_tree
            double heightMustBeLessThan = Math.Log(2, s_GoldenRatio) * Math.Log(Math.Sqrt(5) * ((count ?? node.Count) + 2), 2) - 2;
            Assert.True(node.Height < heightMustBeLessThan);
        }
    }
}
