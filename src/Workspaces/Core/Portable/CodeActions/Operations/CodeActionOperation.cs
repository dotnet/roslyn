// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Represents a single operation of a multi-operation code action.
/// </summary>
public abstract class CodeActionOperation
{
    /// <summary>
    /// A short title describing of the effect of the operation.
    /// </summary>
    public virtual string? Title => null;

    /// <summary>
    /// Called by the host environment to apply the effect of the operation.
    /// This method is guaranteed to be called on the UI thread.
    /// </summary>
    public virtual void Apply(Workspace workspace, CancellationToken cancellationToken)
    {
    }

    /// <summary>
    /// Called by the host environment to apply the effect of the operation.
    /// This method is guaranteed to be called on the UI thread.
    /// </summary>
    internal virtual async Task<bool> TryApplyAsync(Workspace workspace, Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // It is a requirement that this method be called on the UI thread.  So it's safe for us to call
        // into .Apply without any threading operations here.
        this.Apply(workspace, cancellationToken);
        return true;
    }

    /// <summary>
    /// Operations may make all sorts of changes that may not be appropriate during testing
    /// (like popping up UI). So, by default, we don't apply them unless the operation asks
    /// for that to happen.
    /// </summary>
    internal virtual bool ApplyDuringTests => false;
}
