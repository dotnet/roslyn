// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SemanticModelReuse;

/// <summary>
/// a service that provides a semantic model that will re-use last known compilation if
/// semantic version hasn't changed.
/// </summary>
internal interface ISemanticModelReuseWorkspaceService : IWorkspaceService
{
    /// <summary>
    /// Don't call this directly. use <see cref="DocumentExtensions.ReuseExistingSpeculativeModelAsync(Document, SyntaxNode, CancellationToken)"/> (or an overload).
    /// </summary>
    ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
}
