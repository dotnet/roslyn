// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Emit
{
    internal readonly struct SynthesizedDelegateKey : IEquatable<SynthesizedDelegateKey>
    {
        public readonly string Name;

        public SynthesizedDelegateKey(string name)
        {
            Name = name;
        }

        public override bool Equals(object? obj)
            => obj is SynthesizedDelegateKey other && Equals(other);

        public bool Equals(SynthesizedDelegateKey other)
            => Name.Equals(other.Name, StringComparison.Ordinal);

        public override int GetHashCode()
            => Name.GetHashCode();
    }
}
