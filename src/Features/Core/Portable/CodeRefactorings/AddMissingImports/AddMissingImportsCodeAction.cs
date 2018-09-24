// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal class AddMissingImportsCodeAction : SolutionChangeAction
    {
        public AddMissingImportsCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
            : base(FeaturesResources.Add_missing_imports, createChangedSolution)
        {
        }
    }
}
