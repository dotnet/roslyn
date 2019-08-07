// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

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
        public ImmutableArray<(CodeAction action, TextSpan? applicableToSpan)> CodeActions { get; }

        public CodeRefactoring(CodeRefactoringProvider provider, ImmutableArray<(CodeAction, TextSpan?)> actions)
        {
            Provider = provider;
            CodeActions = actions.NullToEmpty();

            if (CodeActions.Length == 0)
            {
                throw new ArgumentException(FeaturesResources.Actions_can_not_be_empty, nameof(actions));
            }
        }
    }
}
