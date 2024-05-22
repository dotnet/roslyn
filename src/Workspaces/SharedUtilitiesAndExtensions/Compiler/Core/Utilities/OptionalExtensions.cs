// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

internal static class OptionalExtensions
{
    internal static T GetValueOrDefault<T>(this Optional<T> optional) where T : struct
        => optional.Value;
}
