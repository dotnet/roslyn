// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that abstracts the accessing of a value 
    /// </summary>
    internal abstract class ValueSource<T>
    {
        public abstract bool TryGetValue([MaybeNullWhen(false)]out T value);
        public abstract T GetValue(CancellationToken cancellationToken = default);
        public abstract Task<T> GetValueAsync(CancellationToken cancellationToken = default);

        public bool HasValue => TryGetValue(out _);

        public static readonly ConstantValueSource<T> Empty = new ConstantValueSource<T>(default!);
    }
}
