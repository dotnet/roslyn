// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionServiceOptions
    {
        /// <summary>
        /// Indicates if the completion is trigger by toggle the expander.
        /// </summary>
        public static readonly Option2<bool> IsExpandedCompletion
            = new(nameof(CompletionServiceOptions), nameof(IsExpandedCompletion), defaultValue: false);

        /// <summary>
        /// For testing only. Changing the default value in actual product might cause perf issues.
        /// </summary>
        public static readonly Option2<bool> UsePartialSemanticForImportCompletion
            = new(nameof(CompletionServiceOptions), nameof(UsePartialSemanticForImportCompletion), defaultValue: true);

        /// <summary>
        /// Timeout value used for time-boxing completion of unimported extension methods.
        /// Value less than 0 means no timebox; value == 0 means immediate timeout (for testing purpose)
        /// </summary>
        public static readonly Option2<int> TimeoutInMillisecondsForExtensionMethodImportCompletion
            = new(nameof(CompletionServiceOptions), nameof(TimeoutInMillisecondsForExtensionMethodImportCompletion), defaultValue: 500);

        /// <summary>
        /// Indicate if we should wait for all completion providers to be available in synchronous operations.
        /// MEF importing providers might be expensive and we don't want to block in certain scenarios (e.g. when on UI thread)
        /// </summary>
        public static readonly Option2<bool> WaitForProviderCreation
            = new(nameof(CompletionServiceOptions), nameof(WaitForProviderCreation), defaultValue: false);
    }
}
