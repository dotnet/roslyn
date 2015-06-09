// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal partial class ProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        internal partial class ProjectCacheService : IProjectCacheHostService
        {
            private class SimpleMRUCache
            {
                private const int CacheSize = 3;

                private readonly Node[] nodes = new Node[CacheSize];

                public bool Empty
                {
                    get
                    {
                        for (var i = 0; i < nodes.Length; i++)
                        {
                            if (nodes[i].Data != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }

                public void Touch(object instance)
                {
                    var oldIndex = -1;
                    var oldTime = Environment.TickCount;

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        if (instance == nodes[i].Data)
                        {
                            nodes[i].LastTouchedInMS = Environment.TickCount;
                            return;
                        }

                        if (oldTime >= nodes[i].LastTouchedInMS)
                        {
                            oldTime = nodes[i].LastTouchedInMS;
                            oldIndex = i;
                        }
                    }

                    Contract.Requires(oldIndex >= 0);
                    nodes[oldIndex] = new Node(instance, Environment.TickCount);
                }

                public void ClearExpiredItems(int expirationTimeInMS)
                {
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        if (nodes[i].Data != null && nodes[i].LastTouchedInMS < expirationTimeInMS)
                        {
                            nodes[i] = default(Node);
                        }
                    }
                }

                public void Clear()
                {
                    Array.Clear(nodes, 0, nodes.Length);
                }

                private struct Node
                {
                    public readonly object Data;
                    public int LastTouchedInMS;

                    public Node(object data, int lastTouchedInMS)
                    {
                        Data = data;
                        LastTouchedInMS = lastTouchedInMS;
                    }
                }
            }

            private class ImplicitCacheMonitor : IdleProcessor
            {
                private readonly ProjectCacheService _owner;
                private readonly SemaphoreSlim _gate;

                public ImplicitCacheMonitor(ProjectCacheService owner, int backOffTimeSpanInMS) :
                    base(AggregateAsynchronousOperationListener.CreateEmptyListener(),
                         backOffTimeSpanInMS,
                         CancellationToken.None)
                {
                    _owner = owner;
                    _gate = new SemaphoreSlim(0);

                    Start();
                }

                protected override Task ExecuteAsync()
                {
                    _owner.ClearExpiredImplicitCache(Environment.TickCount - BackOffTimeSpanInMS);

                    return SpecializedTasks.EmptyTask;
                }

                public void Touch()
                {
                    UpdateLastAccessTime();

                    if (_gate.CurrentCount == 0)
                    {
                        _gate.Release();
                    }
                }

                protected override Task WaitAsync(CancellationToken cancellationToken)
                {
                    if (_owner.IsImplicitCacheEmpty)
                    {
                        return _gate.WaitAsync(cancellationToken);
                    }

                    return SpecializedTasks.EmptyTask;
                }
            }
        }
    }
}
