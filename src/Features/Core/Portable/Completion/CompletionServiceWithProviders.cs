// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A subtype of <see cref="CompletionService"/> that aggregates completions from one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <remarks>
    /// Merged into CompletionService and this is no longer used. Preserved for backward compatibility.
    /// </remarks>
    [Obsolete("Merged into CompletionService and this is no longer used. Use CompletionService instead.")]
    public abstract class CompletionServiceWithProviders : CompletionService
    {
        internal CompletionServiceWithProviders(Workspace workspace) : base(workspace)
        {
        }
    }
}
