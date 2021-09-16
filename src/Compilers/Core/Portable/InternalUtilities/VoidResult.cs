// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Explicitly indicates result is void
    /// </summary>
    internal readonly struct VoidResult : IEquatable<VoidResult>
    {
        public override bool Equals(object? obj)
            => obj is VoidResult;

        public override int GetHashCode()
            => 0;

        public bool Equals(VoidResult other)
            => true;
    }
}
