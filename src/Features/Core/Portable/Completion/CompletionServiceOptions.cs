// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionServiceOptions
    {
        /// <summary>
        /// Timeout value used for time-boxing import completion.
        /// Telemetry shows that the average processing time with cache warmed up for 99th percentile is ~700ms,
        /// Therefore we set the timeout to 1s to ensure it only applies to the case that cache is cold.
        /// </summary>
        public static readonly Option<int> TimeoutInMillisecondsForImportCompletion
            = new Option<int>(nameof(CompletionServiceOptions), nameof(TimeoutInMillisecondsForImportCompletion), defaultValue: 1000);
    }
}
