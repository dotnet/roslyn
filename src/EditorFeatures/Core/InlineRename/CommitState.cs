// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.InlineRename;

/// <summary>
/// State represents different stages when commit starts in InlineRenameSession.
/// </summary>
internal enum CommitState
{
    NotStart,
    WaitConflictResolution,
    StartApplyChanges,
    End,
}
