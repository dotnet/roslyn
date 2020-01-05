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
        /// List of tuples of possible actions that can be used to transform the code the TextSpan within the original document they're applicable to.
        /// </summary>
        /// <remarks>
        /// applicableToSpan should represent a logical section within the original document that the action is 
        /// applicable to. It doesn't have to precisely represent the exact <see cref="TextSpan"/> that will get changed.
        /// </remarks>
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
