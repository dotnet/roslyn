// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that abstracts the accessing of a value that might not be available anymore when queried.
    /// </summary>
    internal abstract class OptionalValueSource<T> where T : class
    {
        public abstract bool TryGetValue(out T value);
        public abstract T? GetValue(CancellationToken cancellationToken = default);
        public abstract Task<T?> GetValueAsync(CancellationToken cancellationToken = default);

        public bool HasValue => TryGetValue(out _);
    }
}
