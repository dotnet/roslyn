// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal struct SeparatedSyntaxListBuilder<TNode> where TNode : SyntaxNode
    {
        private readonly SyntaxListBuilder builder;
        private bool expectedSeparator;

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
            this.builder = builder;
            this.expectedSeparator = false;
        }

        public bool IsNull
        {
            get
            {
                return this.builder == null;
            }
        }

        public int Count
        {
            get
            {
                return this.builder.Count;
            }
        }

        public void Clear()
        {
            this.builder.Clear();
        }

        private void CheckExpectedElement()
        {
            if (this.expectedSeparator)
            {
                throw new InvalidOperationException(CSharpResources.SeparatorIsExpected);
            }
        }

        private void CheckExpectedSeparator()
        {
            if (!this.expectedSeparator)
            {
                throw new InvalidOperationException(CSharpResources.ElementIsExpected);
            }
        }

        public SeparatedSyntaxListBuilder<TNode> Add(TNode node)
        {
            CheckExpectedElement();
            this.expectedSeparator = true;
            this.builder.Add(node);
            return this;
        }

        public SeparatedSyntaxListBuilder<TNode> AddSeparator(SyntaxToken separatorToken)
        {
            CheckExpectedSeparator();
            this.expectedSeparator = false;
            this.builder.AddInternal(separatorToken.Node);
            return this;
        }

        public SeparatedSyntaxListBuilder<TNode> AddRange(SeparatedSyntaxList<TNode> nodes)
        {
            CheckExpectedElement();
            SyntaxNodeOrTokenList list = nodes.GetWithSeparators();
            this.builder.AddRange(list);
            this.expectedSeparator = ((this.builder.Count & 1) != 0);
            return this;
        }

        public SeparatedSyntaxListBuilder<TNode> AddRange(SeparatedSyntaxList<TNode> nodes, int count)
        {
            CheckExpectedElement();
            SyntaxNodeOrTokenList list = nodes.GetWithSeparators();
            this.builder.AddRange(list, this.Count, Math.Min(count << 1, list.Count));
            this.expectedSeparator = ((this.builder.Count & 1) != 0);
            return this;
        }

        public SeparatedSyntaxList<TNode> ToList()
        {
            if (this.builder == null)
            {
                return new SeparatedSyntaxList<TNode>();
            }

            return this.builder.ToSeparatedList<TNode>();
        }

        public SeparatedSyntaxList<TDerived> ToList<TDerived>() where TDerived : TNode
        {
            if (this.builder == null)
            {
                return new SeparatedSyntaxList<TDerived>();
            }

            return this.builder.ToSeparatedList<TDerived>();
        }

        public static implicit operator SyntaxListBuilder(SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder.builder;
        }

        public static implicit operator SeparatedSyntaxList<TNode>(SeparatedSyntaxListBuilder<TNode> builder)
        {
            if (builder.builder != null)
            {
                return builder.ToList();
            }

            return default(SeparatedSyntaxList<TNode>);
        }
    }
}