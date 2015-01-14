// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that abstracts the accessing of a value 
    /// </summary>
    internal abstract class ValueSource<T>
    {
        public abstract bool TryGetValue(out T value);
        public abstract T GetValue(CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken));

        public bool HasValue
        {
            get
            {
                T tmp;
                return this.TryGetValue(out tmp);
            }
        }

        public static readonly ConstantValueSource<T> Empty = new ConstantValueSource<T>(default(T));
    }
}
