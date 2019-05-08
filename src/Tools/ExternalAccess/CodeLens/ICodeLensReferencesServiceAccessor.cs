// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    public interface ICodeLensReferencesServiceAccessor : IWorkspaceService
    {
        Task<CodeLensReferenceCountWrapper?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults, CancellationToken cancellationToken);

        Task<IEnumerable<CodeLensReferenceLocationDescriptorWrapper?>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken);

        Task<IEnumerable<CodeLensReferenceMethodDescriptorWrapper?>> FindReferenceMethodsAsync(Solution solution,
            DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken);

        Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken);
    }
}
