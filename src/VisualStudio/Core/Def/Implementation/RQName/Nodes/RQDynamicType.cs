// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQDynamicType : RQType
    {
        public static readonly RQDynamicType Singleton = new RQDynamicType();
        private RQDynamicType() { }

        public override SimpleTreeNode ToSimpleTree()
        {
            return new SimpleLeafNode(RQNameStrings.Dynamic);
        }
    }
}
