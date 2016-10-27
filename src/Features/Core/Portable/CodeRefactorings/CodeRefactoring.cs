// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Represents a set of transformations that can be applied to a piece of code.
    /// </summary>
    internal class CodeRefactoring
    {
        public CodeRefactoringProvider Provider { get; }

        /// <summary>
        /// List of possible actions that can be used to transform the code.
        /// </summary>
        public IReadOnlyList<CodeAction> Actions { get; }

        public CodeRefactoring(CodeRefactoringProvider provider, IEnumerable<CodeAction> actions)
        {
            Provider = provider;
            Actions = actions.ToImmutableArrayOrEmpty();

            if (Actions.Count == 0)
            {
                throw new ArgumentException(FeaturesResources.Actions_can_not_be_empty, nameof(actions));
            }
        }
    }
}