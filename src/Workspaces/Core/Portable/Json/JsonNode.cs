// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Json
{
    /// <summary>
    /// Root of the Json syntax hierarchy.  JsonNodes are very similar to Roslyn Red-Nodes in concept,
    /// though there are differences for ease of implementation.
    /// 
    /// Similarities:
    /// 1. Fully representative of the original source.  All source VirtualChars are contained
    ///    in the Json nodes.
    /// 2. Specific types for Nodes, Tokens and Trivia (though JsonTokens only have leading trivia).
    /// 3. Uniform ways of deconstructing Nodes (i.e. ChildCount + ChildAt).
    /// 
    /// Differences:
    /// Note: these differences are not required, and can be changed if felt to be valuable.
    /// 1. No parent pointers.  These have not been needed yet.
    /// 2. No Update methods.  These have not been needed yet.
    /// 3. No direct ways to get Positions/Spans of node/token/trivia.  Instead, that information can
    ///    be acquired from the VirtualChars contained within those constructs.  This does mean that
    ///    an empty node (for example, an empty JsonSequenceNode) effect has no way to simply ascertain
    ///    its location.  So far that hasn't been a problem.
    /// 4. No null nodes.  Haven't been needed so far, and it keeps things extremely simple.  For 
    ///    Situations where Roslyn might have chosen an optional null child, the Json hierarchy just
    ///    has multiple nodes.  For example there are distinct nodes to represent the very similar
    ///    {a}   {a,}    {a,b}    constructs.
    /// </summary>
    internal abstract class JsonNode
    {
        public readonly JsonKind Kind;

        protected JsonNode(JsonKind kind)
        {
            Debug.Assert(kind != JsonKind.None);
            Kind = kind;
        }

        public abstract int ChildCount { get; }
        public abstract JsonNodeOrToken ChildAt(int index);

        public abstract void Accept(IJsonNodeVisitor visitor);

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private readonly JsonNode _regexNode;
            private readonly int _childCount;
            private int _currentIndex;

            public Enumerator(JsonNode regexNode)
            {
                _regexNode = regexNode;
                _childCount = regexNode.ChildCount;
                _currentIndex = -1;
                Current = default;
            }

            public JsonNodeOrToken Current { get; private set; }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex >= _childCount)
                {
                    Current = default;
                    return false;
                }

                Current = _regexNode.ChildAt(_currentIndex);
                return true;
            }
        }
    }
}
