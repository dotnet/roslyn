// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal sealed record AggregateBoundAttributeDescription(ImmutableArray<BoundAttributeDescriptionInfo> DescriptionInfos)
{
    public static readonly AggregateBoundAttributeDescription Empty = new(ImmutableArray<BoundAttributeDescriptionInfo>.Empty);
}
