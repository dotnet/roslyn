// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionServiceOptions
    {
        /// <summary>
        /// Indicates the requested state of the expanded completion, which corresponding to the expander button in VS.
        /// This value take effect only when <see cref="CompletionOptions.ShowItemsFromUnimportedNamespaces"/> is set to <see langword="false"/>,
        /// except when controlling timeout of long running request, in which case it might affect wheter timeout will be applied.
        /// <list type="bullet">
        ///     <item>
        ///         <term><see langword="true"/></term>
        ///         <description>Explicitly requesting expanded completion. We will not timeout and cancel import completion.</description>
        ///     </item>
        ///     <item>
        ///         <term><see langword="false"/></term>
        ///         <description>
        ///         Not explicitly requesting expanded completion, but asking for its availability.
        ///         We might cancel completion even if <see cref="CompletionOptions.ShowItemsFromUnimportedNamespaces"/> is set to <see langword="true"/>.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see langword="null"/></term>
        ///         <description>
        ///         Not explicitly requesting expanded completion nor its availability.
        ///         We might cancel completion even if <see cref="CompletionOptions.ShowItemsFromUnimportedNamespaces"/> is set to <see langword="true"/>.
        ///         </description>
        ///     </item>
        /// </list>
        /// </summary>
        public static readonly Option2<bool?> ExpandedCompletionState
            = new(nameof(CompletionServiceOptions), nameof(ExpandedCompletionState), defaultValue: false);

        /// <summary>
        /// For testing only. Changing the default value in actual product might cause perf issues.
        /// </summary>
        public static readonly Option2<bool> UsePartialSemanticForImportCompletion
            = new(nameof(CompletionServiceOptions), nameof(UsePartialSemanticForImportCompletion), defaultValue: true);

        /// <summary>
        /// Indicates if the completion should be disallowed to add imports.
        /// </summary>
        public static readonly Option2<bool> DisallowAddingImports
            = new(nameof(CompletionServiceOptions), nameof(DisallowAddingImports), defaultValue: false);

        /// <summary>
        /// Timeout value used for time-boxing completion of unimported extension methods.
        /// Value less than 0 means no timebox; value == 0 means immediate timeout (for testing purpose)
        /// </summary>
        public static readonly Option2<int> TimeoutInMillisecondsForExtensionMethodImportCompletion
            = new(nameof(CompletionServiceOptions), nameof(TimeoutInMillisecondsForExtensionMethodImportCompletion), defaultValue: 500);
    }
}
