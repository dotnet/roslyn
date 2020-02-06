﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Represents a configuration code action with nested actions registered by individual <see cref="IConfigurationFixProvider"/>s.
    /// Note that the code fix/light bulb engine groups all such <see cref="AbstractConfigurationActionWithNestedActions"/> from different providers
    /// into another top level suggested action to avoid light bulb clutter. This topmost suggested action is *not* represented by this code action.
    /// </summary>
    internal abstract class AbstractConfigurationActionWithNestedActions : CodeAction.CodeActionWithNestedActions
    {
        protected AbstractConfigurationActionWithNestedActions(ImmutableArray<CodeAction> nestedActions, string title)
            : base(title, nestedActions, isInlinable: false)
        {
        }

        // Put configurations/suppressions at the end of everything.
        internal override CodeActionPriority Priority => CodeActionPriority.None;
    }
}
