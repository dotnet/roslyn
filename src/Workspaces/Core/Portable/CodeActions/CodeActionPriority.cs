// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
