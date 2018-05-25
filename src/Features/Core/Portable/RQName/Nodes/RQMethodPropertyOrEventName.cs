// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQMethodPropertyOrEventName
    {
        /// <summary>
        /// Methods, Properties, or Events either have "ordinary" names,
        /// or explicit interface names. But even explicit names have an
        /// underlying ordinary name as well. This is just the value for
        /// ordinary names, or the underlying ordinary name if this is an
        /// explicit name.
        /// </summary>
        public abstract string OrdinaryNameValue { get; }

        public abstract SimpleGroupNode ToSimpleTree();
    }
}
