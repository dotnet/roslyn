// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
public enum TagHelperFlags : byte
{
    CaseSensitive = 1 << 0,
    IsFullyQualifiedNameMatch = 1 << 1,
    ClassifyAttributesOnly = 1 << 2
}
