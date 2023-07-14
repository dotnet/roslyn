// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if false
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions;

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
/// <summary>
/// Internal priority used to bluntly place items in a light bulb in strict orderings.  Priorities take
/// the highest precedence when ordering items so that we can ensure very important items get top prominence,
/// and low priority items do not.
/// </summary>
internal enum CodeActionPriority
{
    Lowest = 0,
    Low = 1,
    Medium = 2,
    High = 3,

    Default = Medium
}
#endif
