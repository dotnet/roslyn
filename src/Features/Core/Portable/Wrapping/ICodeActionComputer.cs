// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Wrapping;

internal interface ICodeActionComputer
{
    /// <summary>
    /// Produces the actual top-level code wrapping actions for the original node provided.
    /// </summary>
    Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync();
}
