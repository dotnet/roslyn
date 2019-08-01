// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Features.RQName.SimpleTree
{
    internal abstract class SimpleTreeNode
    {
        public readonly string Text;

        public SimpleTreeNode(string text)
        {
            Text = text;
        }
    }
}
