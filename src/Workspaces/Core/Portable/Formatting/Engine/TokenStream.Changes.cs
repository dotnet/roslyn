// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        /// <summary>
        /// thread-safe collection that holds onto changes
        /// </summary>
        private class Changes
        {
            public const int BeginningOfTreeKey = -1;
            public const int EndOfTreeKey = -2;

            private readonly ConcurrentDictionary<int, TriviaData> _map;

            public Changes()
            {
                _map = new ConcurrentDictionary<int, TriviaData>();
            }

            public bool Contains(int key)
            {
                return _map.ContainsKey(key);
            }

            public TriviaData this[int key]
            {
                get
                {
                    return _map[key];
                }
            }

            public void Add(int key, TriviaData triviaInfo)
            {
                Contract.ThrowIfTrue(this.Contains(key));

                _map.TryAdd(key, triviaInfo);
            }

            public void Replace(int key, TriviaData triviaInfo)
            {
                Contract.ThrowIfFalse(this.Contains(key));

                _map[key] = triviaInfo;
            }

            public void Remove(int pairIndex)
            {
                TriviaData temp;
                _map.TryRemove(pairIndex, out temp);
            }

            public void AddOrReplace(int key, TriviaData triviaInfo)
            {
                if (this.Contains(key))
                {
                    Replace(key, triviaInfo);
                    return;
                }

                Add(key, triviaInfo);
            }
        }
    }
}
