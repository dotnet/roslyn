// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

#if STATS
using System.Threading;
#endif
namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    /// <summary>
    /// Provides caching functionality for green nonterminals with up to 3 children.
    /// Example:
    ///     When constructing a node with given kind, flags, child1 and child2, we can look up 
    ///     in the cache whether we already have a node that contains same kind, flags, 
    ///     child1 and child2 and use that.
    ///     
    ///     For the purpose of children comparison, reference equality is used as a much cheaper 
    ///     alternative to the structural/recursive equality. This implies that in order to de-duplicate
    ///     a node to a cache node, the children of two nodes must be already de-duplicated.     
    ///     When adding a node to the cache we verify that cache does contain node's children,
    ///     since otherwise there is no reason for the node to be used.
    ///     Tokens/nulls are for this purpose considered deduplicated. Indeed most of the tokens
    ///     are deduplicated via quick-scanner caching, so we just assume they all are.
    ///     
    ///     As a result of above, "fat" nodes with 4 or more children or their recursive parents
    ///     will never be in the cache. This naturally limits the typical single cache item to be 
    ///     a relatively simple expression. We do not want the cache to be completely unbounded 
    ///     on the item size. 
    ///     While it still may be possible to store a gigantic nested binary expression, 
    ///     it should be a rare occurrence.
    ///     
    ///     We only consider "normal" nodes to be cacheable. 
    ///     Nodes with diagnostics/annotations/directives/skipped, etc... have more complicated identity 
    ///     and are not likely to be repetitive.
    /// </summary>
    /// <remarks>
    /// The use of <see cref="GreenNode.RawKind"/>, <see cref="GreenNode.NodeFlags"/> (and any provided child nodes)
    /// ensures during lookup that we only return a node that is identical in all relevant aspects to the node that
    /// we're about to create otherwise.  This is a brittle guarantee.  However, given how locked down green nodes are,
    /// this seems acceptable for now.  Great care needs to be taken if new properties are added to green nodes that
    /// would affect their identity.
    /// <para/>
    /// Only nodes created through SyntaxFactory methods are cached.  Nodes created directly through their constructors
    /// are not cached.  This is fairly intuitive as a constructor would not be able to somehow return some other
    /// instance different than the one being constructed.  A subtle aspect of this though is that this is what ensures
    /// that nodes with diagnostics or annotations on them are not cached.  These nodes are created starting with
    /// another node and forking it to add diagnostics/annotations.  This forking always calls through a constructor and
    /// not a factory method.  And as such, never comes through here.
    /// </remarks>
    internal class GreenStats
    {
        // TODO: remove when done tweaking this cache.
#if STATS
        private static GreenStats stats = new GreenStats();

        private int greenNodes;
        private int greenTokens;
        private int nontermsAdded;
        private int cacheableNodes;
        private int cacheHits;

        internal static void NoteGreen(GreenNode node)
        {
            Interlocked.Increment(ref stats.greenNodes);
            if (node.IsToken)
            {
                Interlocked.Increment(ref stats.greenTokens);
            }
        }

        internal static void ItemAdded()
        {
            Interlocked.Increment(ref stats.nontermsAdded);
        }
        
        internal static void ItemCacheable()
        {
            Interlocked.Increment(ref stats.cacheableNodes);
        }

        internal static void CacheHit()
        {
            Interlocked.Increment(ref stats.cacheHits);
        }

        ~GreenStats()
        {
            Console.WriteLine("Green: " + greenNodes);
            Console.WriteLine("GreenTk: " + greenTokens);
            Console.WriteLine("Nonterminals added: " + nontermsAdded);
            Console.WriteLine("Nonterminals cacheable: " + cacheableNodes);
            Console.WriteLine("CacheHits: " + cacheHits);
            Console.WriteLine("RateOfAll: " + (cacheHits * 100 / (cacheHits + greenNodes - greenTokens)) + "%");
            Console.WriteLine("RateOfCacheable: " + (cacheHits * 100 / (cacheableNodes)) + "%");
        }
#else
        internal static void NoteGreen(GreenNode _)
        {
        }

        [Conditional("DEBUG")]
        internal static void ItemAdded()
        {
        }

        [Conditional("DEBUG")]
        internal static void ItemCacheable()
        {
        }

        [Conditional("DEBUG")]
        internal static void CacheHit()
        {
        }
#endif
    }

    internal static class SyntaxNodeCache
    {
        private const int CacheSizeBits = 16;
        private const int CacheSize = 1 << CacheSizeBits;
        private const int CacheMask = CacheSize - 1;

        /// <summary>
        /// Simply array indexed by the hash of the cached node.  Note that unlike a typical dictionary/hashtable, this
        /// does not exercise any form of collision resolution.  If two different nodes hash to the same index, the
        /// latter will overwrite the former.  This is acceptable since this is just an opportunistic cache.  Reads from
        /// the cache validate that the node they get back is actually the one they were looking for.   See the comments
        /// in <see cref="TryGetNode(int, GreenNode?, out int)"/> for more details.
        /// </summary>
        private static readonly GreenNode[] s_cache = new GreenNode[CacheSize];

        internal static void AddNode(GreenNode node, int hash)
        {
            if (AllChildrenInCache(node) && !node.IsMissing)
            {
                GreenStats.ItemAdded();

                Debug.Assert(GetCacheHash(node) == hash);

                var idx = hash & CacheMask;
                s_cache[idx] = node;
            }
        }

        private static bool CanBeCached(GreenNode? child1)
        {
            return child1 == null || IsCacheable(child1);
        }

        private static bool CanBeCached(GreenNode? child1, GreenNode? child2)
        {
            return CanBeCached(child1) && CanBeCached(child2);
        }

        private static bool CanBeCached(GreenNode? child1, GreenNode? child2, GreenNode? child3)
        {
            return CanBeCached(child1) && CanBeCached(child2) && CanBeCached(child3);
        }

        private static bool ChildInCache(GreenNode? child)
        {
            // for the purpose of this function consider that 
            // null nodes, tokens and trivias are cached somewhere else.
            // TODO: should use slotCount
            if (child == null || child.SlotCount == 0) return true;

            int hash = GetCacheHash(child);
            int idx = hash & CacheMask;
            return s_cache[idx] == child;
        }

        private static bool AllChildrenInCache(GreenNode node)
        {
            // TODO: should use slotCount
            var cnt = node.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                if (!ChildInCache(node.GetSlot(i)))
                {
                    return false;
                }
            }

            return true;
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, out int hash)
        {
            return TryGetNode(kind, child1, GetDefaultNodeFlags(), out hash);
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, GreenNode.NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1))
            {
                GreenStats.ItemCacheable();

                // Determine the hash for the node being created, given its kind, flags, and optional single child. Then
                // grab out a potential cached node from the cache based on that hash.  Note that this may not actually
                // be a viable match due to potential hash collisions, where we have 'last one wins' semantics.  So if
                // we do see a node in the cache, we have to validate that it is actually equivalent to the data
                // being used populate the cache entry.  This is what IsCacheEquivalent is for.  It allows us to check
                // that the node already there has that same kind, flags, and the same child (by reference).
                int h = hash = GetCacheHash(kind, flags, child1);
                var e = s_cache[h & CacheMask];
                if (IsCacheEquivalent(e, kind, flags, child1))
                {
                    GreenStats.CacheHit();
                    return e;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, GreenNode? child2, out int hash)
        {
            return TryGetNode(kind, child1, child2, GetDefaultNodeFlags(), out hash);
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, GreenNode? child2, GreenNode.NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1, child2))
            {
                GreenStats.ItemCacheable();

                int h = hash = GetCacheHash(kind, flags, child1, child2);
                var e = s_cache[h & CacheMask];
                if (IsCacheEquivalent(e, kind, flags, child1, child2))
                {
                    GreenStats.CacheHit();
                    return e;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, GreenNode? child2, GreenNode? child3, out int hash)
        {
            return TryGetNode(kind, child1, child2, child3, GetDefaultNodeFlags(), out hash);
        }

        internal static GreenNode? TryGetNode(int kind, GreenNode? child1, GreenNode? child2, GreenNode? child3, GreenNode.NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1, child2, child3))
            {
                GreenStats.ItemCacheable();

                int h = hash = GetCacheHash(kind, flags, child1, child2, child3);
                var e = s_cache[h & CacheMask];
                if (IsCacheEquivalent(e, kind, flags, child1, child2, child3))
                {
                    GreenStats.CacheHit();
                    return e;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        public static GreenNode.NodeFlags GetDefaultNodeFlags()
        {
            return GreenNode.NodeFlags.IsNotMissing;
        }

        private static int GetCacheHash(int kind, GreenNode.NodeFlags flags, GreenNode? child1)
        {
            int code = (int)(flags) ^ kind;
            code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);

            // ensure nonnegative hash
            return code & Int32.MaxValue;
        }

        private static int GetCacheHash(int kind, GreenNode.NodeFlags flags, GreenNode? child1, GreenNode? child2)
        {
            int code = (int)(flags) ^ kind;

            if (child1 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);
            }
            if (child2 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code);
            }

            // ensure nonnegative hash
            return code & Int32.MaxValue;
        }

        private static int GetCacheHash(int kind, GreenNode.NodeFlags flags, GreenNode? child1, GreenNode? child2, GreenNode? child3)
        {
            int code = (int)(flags) ^ kind;

            if (child1 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);
            }
            if (child2 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code);
            }
            if (child3 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child3), code);
            }

            // ensure nonnegative hash
            return code & Int32.MaxValue;
        }

        private const int MaxCachedChildNum = 3;

        private static bool IsCacheable(GreenNode node)
        {
            return ((node.Flags & GreenNode.NodeFlags.InheritMask) == GreenNode.NodeFlags.IsNotMissing) &&
                node.SlotCount <= MaxCachedChildNum;
        }

        /// <summary>
        /// Internal for testing purposes only.  Do not use outside of this type or tests.
        /// </summary>
        internal static int GetCacheHash(GreenNode node)
        {
            Debug.Assert(IsCacheable(node));

            int code = (int)(node.Flags) ^ node.RawKind;
            int cnt = node.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = node.GetSlot(i);
                if (child != null)
                {
                    code = Hash.Combine(RuntimeHelpers.GetHashCode(child), code);
                }
            }

            return code & Int32.MaxValue;
        }

        private static bool IsCacheEquivalent(GreenNode? parent, int kind, GreenNode.NodeFlags flags, GreenNode? child1)
        {
            if (parent is null)
                return false;

            Debug.Assert(IsCacheable(parent));

            return parent.RawKind == kind &&
                parent.Flags == flags &&
                parent.SlotCount == 1 &&
                parent.GetSlot(0) == child1;
        }

        private static bool IsCacheEquivalent(GreenNode? parent, int kind, GreenNode.NodeFlags flags, GreenNode? child1, GreenNode? child2)
        {
            if (parent is null)
                return false;

            Debug.Assert(IsCacheable(parent));

            return parent.RawKind == kind &&
                parent.Flags == flags &&
                parent.SlotCount == 2 &&
                parent.GetSlot(0) == child1 &&
                parent.GetSlot(1) == child2;
        }

        private static bool IsCacheEquivalent(GreenNode? parent, int kind, GreenNode.NodeFlags flags, GreenNode? child1, GreenNode? child2, GreenNode? child3)
        {
            if (parent is null)
                return false;

            Debug.Assert(IsCacheable(parent));

            return parent.RawKind == kind &&
                parent.Flags == flags &&
                parent.SlotCount == 3 &&
                parent.GetSlot(0) == child1 &&
                parent.GetSlot(1) == child2 &&
                parent.GetSlot(2) == child3;
        }
    }
}
