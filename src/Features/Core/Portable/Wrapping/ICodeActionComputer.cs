// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Wrapping
{
    internal interface ICodeActionComputer
    {
        /// <summary>
        /// Produces the actual top-level code wrapping actions for the original node provided.
        /// </summary>
        Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync();
    }
}
