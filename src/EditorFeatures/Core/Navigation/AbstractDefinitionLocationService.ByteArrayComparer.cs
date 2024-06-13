// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation;

internal abstract partial class AbstractDefinitionLocationService
{
    private sealed class ByteArrayComparer : IEqualityComparer<ImmutableArray<byte>>
    {
        public static readonly ByteArrayComparer Instance = new();

        private ByteArrayComparer() { }

        public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
            => x.SequenceEqual(y);

        public int GetHashCode(ImmutableArray<byte> obj)
            => Hash.CombineValues(obj);
    }
}
