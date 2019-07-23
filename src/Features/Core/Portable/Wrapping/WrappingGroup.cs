// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// A group of wrapping actions placed under a common title.  For example:
    ///     Unwrap group:
    ///         unwrap option 1
    ///         unwrap option 2
    ///     Wrap all group:
    ///         wrap all option 1
    ///         wrap all optoin 2
    ///         ...
    /// </summary>
    internal readonly struct WrappingGroup
    {
        /// <summary>
        /// Whether or not the items in this group can be inlined in the topmost lightbulb.
        /// </summary>
        public readonly bool IsInlinable;

        /// <summary>
        /// The actual wrapping code actions for this group to present to the user.
        /// </summary>
        public readonly ImmutableArray<WrapItemsAction> WrappingActions;

        public WrappingGroup(bool isInlinable, ImmutableArray<WrapItemsAction> wrappingActions)
        {
            IsInlinable = isInlinable;
            WrappingActions = wrappingActions;
        }
    }
}
