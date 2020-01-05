// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal struct SeparatedSyntaxListBuilder<TNode> where TNode : GreenNode
    {
        private readonly SyntaxListBuilder _builder;

        public SeparatedSyntaxListBuilder(int size)
            : this(new SyntaxListBuilder(size))
        {
        }

        public static SeparatedSyntaxListBuilder<TNode> Create()
        {
            return new SeparatedSyntaxListBuilder<TNode>(8);
        }

        internal SeparatedSyntaxListBuilder(SyntaxListBuilder builder)
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

        public GreenNode this[int index]
        {
            get
            {
                return _builder[index];
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

        public void RemoveLast()
        {
            this._builder.RemoveLast();
        }

        public SeparatedSyntaxListBuilder<TNode> Add(TNode node)
        {
            _builder.Add(node);
            return this;
        }

        public void AddSeparator(GreenNode separatorToken)
        {
            _builder.Add(separatorToken);
        }

        public void AddRange(TNode[] items, int offset, int length)
        {
            _builder.AddRange(items, offset, length);
        }

        public void AddRange(in SeparatedSyntaxList<TNode> nodes)
        {
            _builder.AddRange(nodes.GetWithSeparators());
        }

        public void AddRange(in SeparatedSyntaxList<TNode> nodes, int count)
        {
            var list = nodes.GetWithSeparators();
            this._builder.AddRange(list, this.Count, Math.Min(count * 2, list.Count));
        }

        public bool Any(int kind)
        {
            return _builder.Any(kind);
        }

        public SeparatedSyntaxList<TNode> ToList()
        {
            return _builder == null
                ? default(SeparatedSyntaxList<TNode>)
                : new SeparatedSyntaxList<TNode>(new SyntaxList<GreenNode>(_builder.ToListNode()));
        }

        /// <summary>
        /// WARN WARN WARN: This should be used with extreme caution - the underlying builder does
        /// not give any indication that it is from a separated syntax list but the constraints
        /// (node, token, node, token, ...) should still be maintained.
        /// </summary>
        /// <remarks>
        /// In order to avoid creating a separate pool of SeparatedSyntaxListBuilders, we expose
        /// our underlying SyntaxListBuilder to SyntaxListPool.
        /// </remarks>
        internal SyntaxListBuilder UnderlyingBuilder
        {
            get { return _builder; }
        }

        public static implicit operator SeparatedSyntaxList<TNode>(in SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder.ToList();
        }

        public static implicit operator SyntaxListBuilder(in SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder._builder;
        }
    }
}
