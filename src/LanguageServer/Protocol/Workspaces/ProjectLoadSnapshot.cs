// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A captured set of project-loading operations. Waiting for completion does not select additional operations.
/// </summary>
internal readonly struct ProjectLoadSnapshot(Task completion)
{
    /// <summary>
    /// Completes when the captured operations finish, including any transitive loads owned by those operations.
    /// Await this outside serialized request dispatch.
    /// </summary>
    public Task Completion { get; } = completion;
}
