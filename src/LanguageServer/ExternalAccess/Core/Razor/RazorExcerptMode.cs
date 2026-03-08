// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;
#endif

internal enum RazorExcerptMode
{
    SingleLine,
    Tooltip
}
