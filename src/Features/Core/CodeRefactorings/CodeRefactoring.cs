// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Represents a set of transformations that can be applied to a piece of code.
    /// </summary>
    internal class CodeRefactoring : ICodeRefactoring
    {
        private readonly CodeRefactoringProvider provider;
        private readonly IReadOnlyList<CodeAction> actions;

        public CodeRefactoringProvider Provider
        {
            get { return this.provider; }
        }

        /// <summary>
        /// List of possible actions that can be used to transform the code.
        /// </summary>
        public IEnumerable<CodeAction> Actions
        {
            get
            {
                return actions;
            }
        }

        public CodeRefactoring(CodeRefactoringProvider provider, IEnumerable<CodeAction> actions)
        {
            this.provider = provider;
            this.actions = actions.ToImmutableArrayOrEmpty();

            if (this.actions.Count == 0)
            {
                throw new ArgumentException(FeaturesResources.ActionsCanNotBeEmpty, "actions");
            }
        }
    }
}
