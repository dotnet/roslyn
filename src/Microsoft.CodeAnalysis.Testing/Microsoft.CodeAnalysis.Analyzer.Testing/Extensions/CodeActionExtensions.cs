// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class CodeActionExtensions
    {
        private static readonly Func<CodeAction, ImmutableArray<CodeAction>> s_nestedActions =
            LightupHelpers.CreatePropertyAccessor<CodeAction, ImmutableArray<CodeAction>>(
                typeof(CodeAction),
                nameof(NestedActions),
                defaultValue: default);

        private static readonly Func<CodeAction, ImmutableArray<CodeAction>> s_nestedCodeActions =
            LightupHelpers.CreatePropertyAccessor<CodeAction, ImmutableArray<CodeAction>>(
                typeof(CodeAction),
                "NestedCodeActions",
                defaultValue: ImmutableArray<CodeAction>.Empty);

        public static ImmutableArray<CodeAction> NestedActions(this CodeAction action)
        {
            // CodeAction.NestedCodeActions was renamed to CodeAction.NestedActions in
            // https://github.com/dotnet/roslyn/pull/71327
            var nestedActions = s_nestedActions(action);
            if (nestedActions.IsDefault)
            {
                nestedActions = s_nestedCodeActions(action);
            }

            return nestedActions;
        }
    }
}
