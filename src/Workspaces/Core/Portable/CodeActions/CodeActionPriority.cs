// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal enum CodeActionPriority
    {
        //
        // Summary:
        //     No particular priority.
        None = 0,
        //
        // Summary:
        //     Low priority suggestion.
        Low = 1,
        //
        // Summary:
        //     Medium priority suggestion.
        Medium = 2,
        //
        // Summary:
        //     High priority suggestion.
        High = 3
    }
}
