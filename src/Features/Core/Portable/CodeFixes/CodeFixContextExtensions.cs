// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class CodeFixContextExtensions
    {
        /// <summary>
        /// Use this helper to register multiple fixes (<paramref name="actions"/>) each of which addresses / fixes the same supplied <paramref name="diagnostic"/>.
        /// </summary>
        internal static void RegisterFixes(this CodeFixContext context, IEnumerable<CodeAction> actions, Diagnostic diagnostic)
        {
            foreach (var action in actions)
            {
                context.RegisterCodeFix(action, diagnostic);
            }
        }

        /// <summary>
        /// Use this helper to register multiple fixes (<paramref name="actions"/>) each of which addresses / fixes the same set of supplied <paramref name="diagnostics"/>.
        /// </summary>
        internal static void RegisterFixes(this CodeFixContext context, IEnumerable<CodeAction> actions, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var action in actions)
            {
                context.RegisterCodeFix(action, diagnostics);
            }
        }
    }
}
