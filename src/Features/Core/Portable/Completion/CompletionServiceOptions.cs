// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionServiceOptions
    {
        public static readonly Option<bool> IncludeExpandedItemsOnly
            = new Option<bool>(nameof(CompletionServiceOptions), nameof(IncludeExpandedItemsOnly), defaultValue: false);
    }
}
