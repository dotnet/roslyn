// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeLens;

internal interface ICodeLensReferencesService : IWorkspaceService
{
    ValueTask<VersionStamp> GetProjectCodeLensVersionAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Given a document and syntax node, returns the number of locations where the located node is referenced.
    /// <para>
    ///     Optionally, the service supports capping the reference count to a value specified by <paramref name="maxSearchResults"/>
    ///     if <paramref name="maxSearchResults"/> is greater than 0.
    /// </para>
    /// </summary>
    Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, int maxSearchResults, CancellationToken cancellationToken);

    /// <summary>
    /// Given a document and syntax node, returns a collection of locations where the located node is referenced.
    /// </summary>
    Task<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, CancellationToken cancellationToken);

    /// <summary>
    /// Given a document and syntax node, returns a collection of locations of methods that refer to the located node.
    /// </summary>
    Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, CancellationToken cancellationToken);

    /// <summary>
    /// Given a document and syntax node, returns the fully qualified name of the located node's declaration.
    /// </summary>
    Task<string?> GetFullyQualifiedNameAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, CancellationToken cancellationToken);
}
