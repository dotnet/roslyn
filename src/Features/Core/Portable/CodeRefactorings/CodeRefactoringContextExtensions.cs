// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class CodeRefactoringContextExtensions
    {
        /// <summary>
        /// Use this helper to register multiple refactorings (<paramref name="actions"/>).
        /// </summary>
        internal static void RegisterRefactorings(this CodeRefactoringContext context, IEnumerable<CodeAction> actions)
        {
            foreach (var action in actions)
            {
                context.RegisterRefactoring(action);
            }
        }
    }
}
