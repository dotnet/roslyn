// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class ProjectCacheService : IProjectCacheHostService
    {
        private sealed class SimpleMRUCache
        {
            private const int CacheSize = 3;

            private readonly Node[] _nodes = new Node[CacheSize];

            public bool IsEmpty
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

            public void Touch(object? instance)
            {
                var oldIndex = -1;
                var oldTime = DateTime.MaxValue;

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

                Debug.Assert(oldIndex >= 0);
                _nodes[oldIndex] = new Node(instance, DateTime.UtcNow);
            }

            public void Clear()
                => Array.Clear(_nodes, 0, _nodes.Length);

            private struct Node
            {
                public readonly object? Data;
                public DateTime LastTouched;

                public Node(object? data, DateTime lastTouched)
                {
                    Data = data;
                    LastTouched = lastTouched;
                }
            }
        }
    }
}
