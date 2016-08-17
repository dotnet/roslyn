// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal class CommonSyntaxListBuilder :
        AbstractSyntaxListBuilder<GreenNode, CommonSyntaxList<GreenNode>>
    {
        public CommonSyntaxListBuilder(int size) : base(size)
        {
        }

        public static CommonSyntaxListBuilder Create()
        {
            return new CommonSyntaxListBuilder(8);
        }


        public void AddRange<TNode>(CommonSyntaxList<TNode> list) where TNode : GreenNode
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(CommonSyntaxList<TNode> list, int offset, int length) where TNode : GreenNode
        {
            base.AddRange(new CommonSyntaxList<GreenNode>(list.Node), offset, length);
        }

        internal GreenNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return Nodes[0];
                case 2:
                    return CommonSyntaxList.List(Nodes[0], Nodes[1]);
                case 3:
                    return CommonSyntaxList.List(Nodes[0], Nodes[1], Nodes[2]);
                default:
                    var tmp = new ArrayElement<GreenNode>[this.Count];
                    Array.Copy(Nodes, tmp, this.Count);
                    return CommonSyntaxList.List(tmp);
            }
        }

        //public static implicit operator CommonSyntaxList<GreenNode>(CommonSyntaxListBuilder builder)
        //{
        //    if (builder == null)
        //    {
        //        return default(CommonSyntaxList<GreenNode>);
        //    }

        //    return builder.ToList();
        //}

        public CommonSyntaxList<GreenNode> ToList()
        {
            return new CommonSyntaxList<GreenNode>(ToListNode());
        }

        public CommonSyntaxList<TNode> ToList<TNode>() where TNode : GreenNode
        {
            return new CommonSyntaxList<TNode>(ToListNode());
        }
    }
}