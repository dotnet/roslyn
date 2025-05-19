// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class MethodKindExtensions
{
    public static bool IsPropertyAccessor(this MethodKind kind)
        => kind is MethodKind.PropertyGet or MethodKind.PropertySet;
}
