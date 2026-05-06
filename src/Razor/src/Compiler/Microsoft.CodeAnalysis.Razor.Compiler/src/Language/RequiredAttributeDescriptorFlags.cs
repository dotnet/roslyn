// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum RequiredAttributeDescriptorFlags : byte
{
    None = 0,
    CaseSensitive = 1 << 0,
    IsDirectiveAttribute = 1 << 1
}
