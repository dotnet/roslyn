// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.PreferFrameworkType;

internal static class PreferFrameworkTypeConstants
{
    public const string PreferFrameworkType = nameof(PreferFrameworkType);
    public static readonly ImmutableDictionary<string, string?> Properties =
        ImmutableDictionary<string, string?>.Empty.Add(PreferFrameworkType, "");
}
