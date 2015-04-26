// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing
{
    /// <summary>
    /// this can point to everything in the tree such as node, token and trivia
    /// </summary>
    internal struct TreeNode : IEquatable<TreeNode>
    {
        private readonly SyntaxNode _node;
        private readonly SyntaxToken _token;
        private readonly SyntaxTrivia _trivia;

        public TreeNode(SyntaxNode node) : this()
        {
            _node = node;
        }

        public TreeNode(SyntaxToken token) : this()
        {
            _token = token;
        }

        public TreeNode(SyntaxTrivia trivia) : this()
        {
            _trivia = trivia;
        }

        public TreeNode(SyntaxNodeOrToken nodeOrToken) : this()
        {
            if (nodeOrToken.IsNode)
            {
                _node = nodeOrToken.AsNode();
            }
            else if (nodeOrToken.IsToken)
            {
                _token = nodeOrToken.AsToken();
            }
        }

        public int Kind
        {
            get
            {
                if (IsNode)
                {
                    return _node.RawKind;
                }

                if (IsToken)
                {
                    return _token.RawKind;
                }

                return _trivia.RawKind;
            }
        }

        public TreeNode Parent
        {
            get
            {
                // flatten tree to node and everything else
                if (IsNode)
                {
                    if (_node.IsStructuredTrivia)
                    {
                        return new TreeNode(_node.ParentTrivia);
                    }

                    return new TreeNode(_node.Parent);
                }

                if (IsToken)
                {
                    return new TreeNode(_token.Parent);
                }

                return new TreeNode(_trivia.Token.Parent);
            }
        }

        public TextSpan Span
        {
            get
            {
                if (IsNode)
                {
                    return _node.Span;
                }

                if (IsToken)
                {
                    return _token.Span;
                }

                return _trivia.Span;
            }
        }

        public SyntaxTree SyntaxTree
        {
            get
            {
                if (IsNode)
                {
                    return _node.SyntaxTree;
                }

                if (IsToken)
                {
                    return _token.SyntaxTree;
                }

                return _trivia.SyntaxTree;
            }
        }

        public bool Valid
        {
            get
            {
                return IsNode || IsToken || IsTrivia;
            }
        }

        public bool IsLeaf
        {
            get
            {
                if (IsNode)
                {
                    return false;
                }

                if (IsToken)
                {
                    return true;
                }

                if (IsTrivia)
                {
                    if (_trivia.HasStructure)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        public bool IsNode => _node != null;
        public bool IsToken => _token.RawKind != 0;
        public bool IsTrivia => _trivia.RawKind != 0;

        public SyntaxTrivia AsTrivia()
        {
            return _trivia;
        }

        public SyntaxToken AsToken()
        {
            return _token;
        }

        public SyntaxNode AsNode()
        {
            return _node;
        }

        public IEnumerable<TreeNode> GetChildren()
        {
            // this puts trivia same level as token. 
            // we do this so that changing token doesnt say trivia is moved.
            if (IsNode)
            {
                return GetChildren(_node);
            }

            if (IsToken)
            {
                return null;
            }

            if (!_trivia.HasStructure)
            {
                return null;
            }

            return SpecializedCollections.SingletonEnumerable(new TreeNode(_trivia.GetStructure()));
        }

        private IEnumerable<TreeNode> GetChildren(SyntaxNode node)
        {
            foreach (var nodeOrToken in node.ChildNodesAndTokens())
            {
                if (nodeOrToken.IsNode)
                {
                    yield return new TreeNode(nodeOrToken.AsNode());
                }
                else
                {
                    var token = nodeOrToken.AsToken();
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        yield return new TreeNode(trivia);
                    }

                    yield return new TreeNode(token);

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        yield return new TreeNode(trivia);
                    }
                }
            }
        }

        public IEnumerable<TreeNode> GetDescendants()
        {
            // this puts trivia same level as token
            if (IsNode)
            {
                return GetDescendants(_node);
            }

            if (IsToken)
            {
                return null;
            }

            if (!_trivia.HasStructure)
            {
                return null;
            }

            return GetDescendants(_trivia);
        }

        private IEnumerable<TreeNode> GetDescendants(SyntaxNode node)
        {
            foreach (var nodeOrToken in node.DescendantNodesAndTokens())
            {
                if (nodeOrToken.IsNode)
                {
                    yield return new TreeNode(nodeOrToken.AsNode());
                }
                else
                {
                    var token = nodeOrToken.AsToken();
                    foreach (var treeNode in GetDescendants(token.LeadingTrivia))
                    {
                        yield return treeNode;
                    }

                    yield return new TreeNode(token);

                    foreach (var treeNode in GetDescendants(token.TrailingTrivia))
                    {
                        yield return treeNode;
                    }
                }
            }
        }

        private IEnumerable<TreeNode> GetDescendants(SyntaxTriviaList list)
        {
            foreach (var trivia in list)
            {
                yield return new TreeNode(trivia);

                foreach (var treeNode in GetDescendants(trivia))
                {
                    yield return treeNode;
                }
            }
        }

        private IEnumerable<TreeNode> GetDescendants(SyntaxTrivia trivia)
        {
            if (!trivia.HasStructure)
            {
                yield break;
            }

            var structure = trivia.GetStructure();
            yield return new TreeNode(structure);

            foreach (var treeNode in GetDescendants(structure))
            {
                yield return treeNode;
            }
        }

        public bool Equals(TreeNode other)
        {
            return _node == other._node && _token == other._token && _trivia == other._trivia;
        }

        public static bool operator ==(TreeNode left, TreeNode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TreeNode left, TreeNode right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is TreeNode && Equals((TreeNode)obj);
        }

        public override int GetHashCode()
        {
            if (_node != null)
            {
                return _node.GetHashCode();
            }

            if (_token.RawKind != 0)
            {
                return _token.GetHashCode();
            }

            return _trivia.GetHashCode();
        }
    }
}
