// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
public enum BoundAttributeFlags : byte
{
    CaseSensitive = 1 << 0,
    HasIndexer = 1 << 1,
    IsEnum = 1 << 2,
    IsEditorRequired = 1 << 3,
    IsDirectiveAttribute = 1 << 4,
    IsWeaklyTyped = 1 << 5
}
