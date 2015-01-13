// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            BoundBlock tryBlock = (BoundBlock)this.Visit(node.TryBlock);

            var origSawAwait = this.sawAwait;
            this.sawAwait = false;

            ImmutableArray<BoundCatchBlock> catchBlocks = (ImmutableArray<BoundCatchBlock>)this.VisitList(node.CatchBlocks);
            BoundBlock finallyBlockOpt = (BoundBlock)this.Visit(node.FinallyBlockOpt);

            this.sawAwaitInExceptionHandler |= this.sawAwait;
            this.sawAwait |= origSawAwait;

            return node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.PreferFaultHandler);
        }
    }
}
