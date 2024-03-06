// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal enum VSTypeScriptNavigateToMatchKind
{
    Exact = 0,
    Prefix = 1,
    Substring = 2,
    Regular = 3,
    None = 4,
    CamelCaseExact = 5,
    CamelCasePrefix = 6,
    CamelCaseNonContiguousPrefix = 7,
    CamelCaseSubstring = 8,
    CamelCaseNonContiguousSubstring = 9,
    Fuzzy = 10
}
