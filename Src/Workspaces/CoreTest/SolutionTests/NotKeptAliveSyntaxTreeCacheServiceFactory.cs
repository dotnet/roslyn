// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [ExportWorkspaceServiceFactory(typeof(ISyntaxTreeCacheService), "NotKeptAlive"), Shared]
    internal class NotKeptAliveSyntaxTreeCacheServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Cache();
        }

        private class Cache : ISyntaxTreeCacheService
        {
            public void AddOrAccess(SyntaxNode instance, IWeakAction<SyntaxNode> evictor)
            {
                evictor.Invoke(instance);
            }

            public void Clear()
            {
            }
        }
    }
}