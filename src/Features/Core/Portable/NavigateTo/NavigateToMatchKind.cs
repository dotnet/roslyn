// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal enum NavigateToMatchKind
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
}
