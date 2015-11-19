﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal partial class ProjectCacheService : IProjectCacheHostService
    {
        private class SimpleMRUCache
        {
            private const int CacheSize = 3;

            private readonly Node[] _nodes = new Node[CacheSize];

            public bool Empty
            {
                get
                {
                    for (var i = 0; i < _nodes.Length; i++)
                    {
                        if (_nodes[i].Data != null)
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
                var oldTime = DateTime.UtcNow;

                for (var i = 0; i < _nodes.Length; i++)
                {
                    if (instance == _nodes[i].Data)
                    {
                        _nodes[i].LastTouched = DateTime.UtcNow;
                        return;
                    }

                    if (oldTime >= _nodes[i].LastTouched)
                    {
                        oldTime = _nodes[i].LastTouched;
                        oldIndex = i;
                    }
                }

                Contract.Requires(oldIndex >= 0);
                _nodes[oldIndex] = new Node(instance, DateTime.UtcNow);
            }

            public void ClearExpiredItems(DateTime expirationTime)
            {
                for (var i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].Data != null && _nodes[i].LastTouched < expirationTime)
                    {
                        _nodes[i] = default(Node);
                    }
                }
            }

            public void Clear()
            {
                Array.Clear(_nodes, 0, _nodes.Length);
            }

            private struct Node
            {
                public readonly object Data;
                public DateTime LastTouched;

                public Node(object data, DateTime lastTouched)
                {
                    Data = data;
                    LastTouched = lastTouched;
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
                _owner.ClearExpiredImplicitCache(DateTime.UtcNow - TimeSpan.FromMilliseconds(BackOffTimeSpanInMS));

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
