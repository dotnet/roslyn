// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal struct SyntaxListBuilder<TNode> where TNode : CSharpSyntaxNode
    {
        private readonly SyntaxListBuilder _builder;

        public SyntaxListBuilder(int size)
            : this(new SyntaxListBuilder(size))
        {
        }
        internal SyntaxListBuilder(SyntaxListBuilder builder)
        {
            _builder = builder;
        }

        public bool IsNull
        {
            get
            {
                return _builder == null;
            }
        }

        public int Count
        {
            get
            {
                return _builder.Count;
            }
        }

        public TNode this[int index]
        {
            get
            {
                return (TNode)_builder[index];
            }

            set
            {
                _builder[index] = value;
            }
        }

        public void Clear()
        {
            _builder.Clear();
        }

        public SyntaxListBuilder<TNode> Add(TNode node)
        {
            _builder.Add(node);
            return this;
        }

        public void AddRange(TNode[] items, int offset, int length)
        {
            _builder.AddRange(items, offset, length);
        }

        public void AddRange(SyntaxList<TNode> nodes)
        {
            _builder.AddRange(nodes);
        }

        public void AddRange(SyntaxList<TNode> nodes, int offset, int length)
        {
            _builder.AddRange(nodes, offset, length);
        }

        public bool Any(SyntaxKind kind)
        {
            return _builder.Any(kind);
        }

        public SyntaxList<TNode> ToList()
        {
            return _builder.ToList();
        }

        public CSharpSyntaxNode ToListNode()
        {
            return _builder.ToListNode();
        }

        public static implicit operator SyntaxListBuilder(SyntaxListBuilder<TNode> builder)
        {
            return builder._builder;
        }

        public static implicit operator SyntaxList<TNode>(SyntaxListBuilder<TNode> builder)
        {
            if (builder._builder != null)
            {
                return builder.ToList();
            }

            return default(SyntaxList<TNode>);
        }
    }
}
