// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal interface ICodeLensReferencesService : IWorkspaceService
    {
        /// <summary>
        /// Given a document and syntax node, returns the number of locations where the located node is referenced.
        /// <para>
        ///     Optionally, the service supports capping the reference count to a value specified by <paramref name="maxSearchResults"/>
        ///     if <paramref name="maxSearchResults"/> is greater than 0.
        /// </para>
        /// </summary>
        Task<ReferenceCount> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults, CancellationToken cancellationToken);

        /// <summary>
        /// Given a document and syntax node, returns a collection of locations where the located node is referenced.
        /// </summary>
        Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken);

        /// <summary>
        /// Given a document and syntax node, returns a collection of locations of methods that refer to the located node.
        /// </summary>
        Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(Solution solution,
            DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken);

        /// <summary>
        /// Given a document and syntax node, returns the fully qualified name of the located node's declaration.
        /// </summary>
        Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken);
    }
}
