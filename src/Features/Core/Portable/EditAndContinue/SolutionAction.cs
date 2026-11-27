// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal enum SolutionAction
{
    /// <summary>
    /// No action should be taken on the solution.
    /// </summary>
    None,

    /// <summary>
    /// The solution has been committed.
    /// </summary>
    Committed,

    /// <summary>
    /// Pending solution updates have been stored and will need to be committed or discarded.
    /// </summary>
    PendingUpdate,
}
