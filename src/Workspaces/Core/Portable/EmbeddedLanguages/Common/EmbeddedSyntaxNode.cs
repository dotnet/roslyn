// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    /// <summary>
    /// Root of the embedded language syntax hierarchy.  EmbeddedSyntaxNodes are very similar to 
    /// Roslyn Red-Nodes in concept, though there are differences for ease of implementation.
    /// 
    /// Similarities:
    /// 1. Fully representative of the original source.  All source VirtualChars are contained
    ///    in the Regex nodes.
    /// 2. Specific types for Nodes, Tokens and Trivia.
    /// 3. Uniform ways of deconstructing Nodes (i.e. ChildCount + ChildAt).
    /// 
    /// Differences:
    /// Note: these differences are not required, and can be changed if felt to be valuable.
    /// 1. No parent pointers.  These have not been needed yet.
    /// 2. No Update methods.  These have not been needed yet.
    /// 3. No direct ways to get Positions/Spans of node/token/trivia.  Instead, that information can
    ///    be acquired from the VirtualChars contained within those constructs.  This does mean that
    ///    an empty node (for example, an empty RegexSequenceNode) effect has no way to simply ascertain
    ///    its location.  So far that hasn't been a problem.
    /// 4. No null nodes.  Haven't been needed so far, and it keeps things extremely simple.  For 
    ///    example where Roslyn might have chosen an optional null child, the Regex hierarchy just
    ///    has multiple nodes.  For example there are distinct nodes to represent the very similar
    ///    {a}   {a,}    {a,b}    constructs.
    /// </summary>
    internal abstract class EmbeddedSyntaxNode<TNode> where TNode : EmbeddedSyntaxNode<TNode>
    {
        public readonly int RawKind;

        protected EmbeddedSyntaxNode(int rawKind)
        {
            Debug.Assert(rawKind != 0);
            RawKind = rawKind;
        }

        public abstract int ChildCount { get; }
        public abstract EmbeddedSyntaxNodeOrToken<TNode> ChildAt(int index);

        // public abstract void Accept(IRegexNodeVisitor visitor);

        public Enumerator GetEnumerator()
            => new Enumerator(this);

        public struct Enumerator
        {
            private readonly EmbeddedSyntaxNode<TNode> _node;
            private readonly int _childCount;
            private int _currentIndex;

            public Enumerator(EmbeddedSyntaxNode<TNode> node)
            {
                _node = node;
                _childCount = _node.ChildCount;
                _currentIndex = -1;
                Current = default;
            }

            public EmbeddedSyntaxNodeOrToken<TNode> Current { get; private set; }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex >= _childCount)
                {
                    Current = default;
                    return false;
                }

                Current = _node.ChildAt(_currentIndex);
                return true;
            }
        }
    }
}
