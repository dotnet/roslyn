// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQVoidType : RQType
    {
        public static readonly RQVoidType Singleton = new RQVoidType();
        private RQVoidType() { }

        public override SimpleTreeNode ToSimpleTree()
        {
            return new SimpleLeafNode(RQNameStrings.Void);
        }
    }
}
