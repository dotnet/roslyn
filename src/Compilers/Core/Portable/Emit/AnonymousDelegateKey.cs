// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class AnonymousDelegateKey : IEquatable<AnonymousDelegateKey>
    {
        public abstract bool Equals(AnonymousDelegateKey? other);

        public abstract override bool Equals(object? obj);

        public abstract override int GetHashCode();
    }
}
