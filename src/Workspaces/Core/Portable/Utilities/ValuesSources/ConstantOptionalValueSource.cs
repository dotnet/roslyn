// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal sealed class ConstantOptionalValueSource<T> : OptionalValueSource<T>
        where T : class
    {
        private readonly T _value;

        public ConstantOptionalValueSource(T value)
        {
            _value = value;
        }

        public override bool TryGetValue(out T value)
        {
            value = _value;
            return true;
        }

        public override T? GetValue(CancellationToken cancellationToken)
            => _value;

        public override Task<T?> GetValueAsync(CancellationToken cancellationToken)
            => Task.FromResult(_value).ToNullable();
    }
}
