// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    internal interface IResettableInteractiveEvaluator : IInteractiveEvaluator
    {
        InteractiveEvaluatorResetOptions ResetOptions { get; set; }
        Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory);
    }
}
