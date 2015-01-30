// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis
{
    internal class SolutionChangeAction : CodeAction
    {
        private readonly string title;
        private readonly Func<CancellationToken, Task<Solution>> createChangedSolution;

        public SolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
        {
            this.title = title;
            this.createChangedSolution = createChangedSolution;
        }

        public override string Title
        {
            get { return this.title; }
        }
        
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return this.createChangedSolution(cancellationToken);
        }
    }
}
