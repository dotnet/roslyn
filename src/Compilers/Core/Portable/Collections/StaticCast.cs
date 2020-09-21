// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal static class StaticCast<T>
    {
        internal static ImmutableArray<T> From<TDerived>(ImmutableArray<TDerived> from) where TDerived : class, T
        {
            // Remove the pragma when we get a version with https://github.com/dotnet/runtime/issues/39799 fixed
#pragma warning disable CS8634
            return ImmutableArray<T>.CastUp(from);
#pragma warning restore CS8634
        }
    }
}
