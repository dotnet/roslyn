// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Host.WorkspaceServices.Caching
{
    [ExportWorkspaceService(typeof(ISyntaxTreeCacheService), "Test"), Shared]
    internal class TestSyntaxTreeCacheService : ISyntaxTreeCacheService
    {
        private Dictionary<SyntaxNode, IWeakAction<SyntaxNode>> trees =
            new Dictionary<SyntaxNode, IWeakAction<SyntaxNode>>();

        public void AddOrAccess(SyntaxNode instance, IWeakAction<SyntaxNode> evictor)
        {
            if (!trees.ContainsKey(instance))
            {
                trees[instance] = evictor;
            }
        }

        public void Evict(SyntaxNode tree)
        {
            IWeakAction<SyntaxNode> evictor;
            if (trees.TryGetValue(tree, out evictor))
            {
                evictor.Invoke(tree);
            }
        }

        public void Clear()
        {
            trees.Clear();
        }
    }
}
