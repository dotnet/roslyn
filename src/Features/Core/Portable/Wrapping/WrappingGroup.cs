// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Wrapping;

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
internal readonly struct WrappingGroup(bool isInlinable, ImmutableArray<WrapItemsAction> wrappingActions)
{
    /// <summary>
    /// Whether or not the items in this group can be inlined in the topmost lightbulb.
    /// </summary>
    public readonly bool IsInlinable = isInlinable;

    /// <summary>
    /// The actual wrapping code actions for this group to present to the user.
    /// </summary>
    public readonly ImmutableArray<WrapItemsAction> WrappingActions = wrappingActions;
}
