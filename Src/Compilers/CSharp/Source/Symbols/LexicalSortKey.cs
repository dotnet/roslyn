// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A structure used to lexically order symbols. For performance, it's important that this be 
    /// a STRUCTURE, and be able to be returned from a symbol without doing any additional allocations (even
    /// if nothing is cached yet.)
    /// </summary>
    internal struct LexicalSortKey
    {
        private int treeOrdinal;
        private int position;

        // If -1, symbols is in metadata or otherwise not in source
        public int TreeOrdinal
        {
            get { return this.treeOrdinal; }
        }

        // Offset of location within the tree. Doesn't need to exactly that span returns by Locations, just
        // be good enough to sort. In order word, we don't need to go to extra work to return the location of the identifier,
        // just some syntax location is fine.
        // 
        // Negative value indicates that the structure was not initialized yet, is used for lazy 
        // initializations only along with LexicalSortKey.NotInitialized
        public int Position
        {
            get { return this.position; }
        }

        public static readonly LexicalSortKey NotInSource = new LexicalSortKey() { treeOrdinal = -1, position = 0 };

        public static readonly LexicalSortKey NotInitialized = new LexicalSortKey() { treeOrdinal = -1, position = -1 };

        public static readonly LexicalSortKey Last = new LexicalSortKey() { treeOrdinal = int.MaxValue, position = int.MaxValue };

        private LexicalSortKey(int treeOrdinal, int position)
        {
            Debug.Assert(position >= 0);
            Debug.Assert(treeOrdinal >= 0);
            this.treeOrdinal = treeOrdinal;
            this.position = position;
        }

        private LexicalSortKey(SyntaxTree tree, int position, CSharpCompilation compilation)
            : this(tree == null ? -1 : compilation.GetSyntaxTreeOrdinal(tree), position)
        {
        }

        public LexicalSortKey(SyntaxReference syntaxRef, CSharpCompilation compilation)
            : this(syntaxRef.SyntaxTree, syntaxRef.Span.Start, compilation)
        {
        }

        // WARNING: Only use this if the location is obtainable without allocating it (even if cached later). E.g., only
        // if the location object is stored in the constructor of the symbol.
        public LexicalSortKey(Location location, CSharpCompilation compilation)
            : this((SyntaxTree)location.SourceTree, location.SourceSpan.Start, compilation)
        {
        }

        // WARNING: Only use this if the node is obtainable without allocating it (even if cached later). E.g., only
        // if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        // call on a SyntacReference.
        public LexicalSortKey(CSharpSyntaxNode node, CSharpCompilation compilation)
            : this(node.SyntaxTree, node.SpanStart, compilation)
        {
        }

        // WARNING: Only use this if the token is obtainable without allocating it (even if cached later). E.g., only
        // if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        // call on a SyntaxReference.
        public LexicalSortKey(SyntaxToken token, CSharpCompilation compilation)
            : this((SyntaxTree)token.SyntaxTree, token.SpanStart, compilation)
        {
        }

        /// <summary>
        /// Compare two lexical sort keys in a compilation.
        /// </summary>
        public static int Compare(LexicalSortKey xSortKey, LexicalSortKey ySortKey)
        {
            int comparison;

            if (xSortKey.TreeOrdinal != ySortKey.TreeOrdinal)
            {
                if (xSortKey.TreeOrdinal < 0)
                {
                    return 1;
                }
                else if (ySortKey.TreeOrdinal < 0)
                {
                    return -1;
                }

                comparison = xSortKey.TreeOrdinal - ySortKey.TreeOrdinal;
                Debug.Assert(comparison != 0);
                return comparison;
            }

            return xSortKey.Position - ySortKey.Position;
        }

        public static LexicalSortKey First(LexicalSortKey xSortKey, LexicalSortKey ySortKey)
        {
            int comparison = Compare(xSortKey, ySortKey);
            return comparison > 0 ? ySortKey : xSortKey;
        }

        public bool IsInitialized
        {
            get
            {
                return Volatile.Read(ref this.position) >= 0;
            }
        }

        public void SetFrom(LexicalSortKey other)
        {
            Debug.Assert(other.IsInitialized);
            this.treeOrdinal = other.treeOrdinal;
            Volatile.Write(ref this.position, other.position);
        }
    }
}