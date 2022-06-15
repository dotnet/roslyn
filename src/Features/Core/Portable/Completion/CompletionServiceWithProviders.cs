// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A subtype of <see cref="CompletionService"/> that aggregates completions from one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <remarks>
    /// Contains no implementation. Preserved for backward compatibility.
    /// </remarks>
    public abstract class CompletionServiceWithProviders : CompletionService
    {
        internal CompletionServiceWithProviders(Workspace workspace) : base(workspace)
        {
        }
    }
}
