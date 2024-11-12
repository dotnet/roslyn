// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SemanticSearch;

/// <summary>
/// Provides access to <see cref="SemanticSearchWorkspace"/> singleton.
/// </summary>
internal interface ISemanticSearchWorkspaceHost
{
    SemanticSearchWorkspace Workspace { get; }
}
