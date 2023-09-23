// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Represents the reasons why an insertion operation cannot be performed.
/// </summary>
internal enum InvalidInsertionReason
{
    /// <summary>
    /// The reason for the failure is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// The identifier being inserted already exists in the target context.
    /// </summary>
    InsertIdentifierAlreadyExistsOnTarget,

    /// <summary>
    /// The identifier being replaced does not exist in the target context.
    /// </summary>
    ReplaceIdentifierMissingOnTarget,
}
