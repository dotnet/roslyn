// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class CacheOptions
    {
        internal const string FeatureName = "Cache Options";

        internal static readonly Option<int> RecoverableTreeLengthThreshold = new Option<int>(FeatureName, "RecoverableTreeLengthThreshold", defaultValue: 4096);
    }
}
