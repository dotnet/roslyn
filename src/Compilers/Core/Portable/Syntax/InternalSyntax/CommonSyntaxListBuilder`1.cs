// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal struct CommonSyntaxListBuilder<TNode> where TNode : GreenNode
    {
        private readonly CommonSyntaxListBuilder _builder;

        public CommonSyntaxListBuilder(int size)
            : this(new CommonSyntaxListBuilder(size))
        {
        }

        public static CommonSyntaxListBuilder<TNode> Create()
        {
            return new CommonSyntaxListBuilder<TNode>(8);
        }

        internal CommonSyntaxListBuilder(CommonSyntaxListBuilder builder)
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

        public CommonSyntaxListBuilder<TNode> Add(TNode node)
        {
            _builder.Add(node);
            return this;
        }

        public void AddRange(TNode[] items, int offset, int length)
        {
            _builder.AddRange(items, offset, length);
        }

        public void AddRange(CommonSyntaxList<TNode> nodes)
        {
            _builder.AddRange(nodes);
        }

        public void AddRange(CommonSyntaxList<TNode> nodes, int offset, int length)
        {
            _builder.AddRange(nodes, offset, length);
        }

        public bool Any(int kind)
        {
            return _builder.Any(kind);
        }

        public CommonSyntaxList<TNode> ToList()
        {
            return _builder.ToList();
        }

        public CommonSyntaxList<TDerived> ToList<TDerived>() where TDerived : GreenNode
        {
            return new CommonSyntaxList<TDerived>(ToListNode());
        }

        public GreenNode ToListNode()
        {
            return _builder.ToListNode();
        }

        public static implicit operator CommonSyntaxListBuilder(CommonSyntaxListBuilder<TNode> builder)
        {
            return builder._builder;
        }

        public static implicit operator CommonSyntaxList<TNode>(CommonSyntaxListBuilder<TNode> builder)
        {
            if (builder._builder != null)
            {
                return builder.ToList();
            }

            return default(CommonSyntaxList<TNode>);
        }
    }
}
