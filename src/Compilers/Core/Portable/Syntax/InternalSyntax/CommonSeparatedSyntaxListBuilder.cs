// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal struct CommonSeparatedSyntaxListBuilder<TNode> where TNode : GreenNode
    {
        private readonly CommonSyntaxListBuilder _builder;

        public CommonSeparatedSyntaxListBuilder(int size)
            : this(new CommonSyntaxListBuilder(size))
        {
        }

        public static CommonSeparatedSyntaxListBuilder<TNode> Create()
        {
            return new CommonSeparatedSyntaxListBuilder<TNode>(8);
        }

        internal CommonSeparatedSyntaxListBuilder(CommonSyntaxListBuilder builder)
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

        public CommonSeparatedSyntaxListBuilder<TNode> Add(TNode node)
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

        public void AddRange(CommonSeparatedSyntaxList<TNode> nodes)
        {
            _builder.AddRange(nodes.GetWithSeparators());
        }

        public bool Any(int kind)
        {
            return _builder.Any(kind);
        }

        public CommonSeparatedSyntaxList<TNode> ToList()
        {
            return _builder == null
                ? default(CommonSeparatedSyntaxList<TNode>)
                : new CommonSeparatedSyntaxList<TNode>(new CommonSyntaxList<GreenNode>(_builder.ToListNode()));
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
        internal CommonSyntaxListBuilder UnderlyingBuilder
        {
            get { return _builder; }
        }

        public static implicit operator CommonSeparatedSyntaxList<TNode>(CommonSeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder.ToList();
        }

        public static implicit operator CommonSyntaxListBuilder(CommonSeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder._builder;
        }
    }
}