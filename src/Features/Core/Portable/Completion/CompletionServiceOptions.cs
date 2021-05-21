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
    }
}
