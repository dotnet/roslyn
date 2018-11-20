// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ChangeNamespace
{
    internal interface IChangeNamespaceService : ILanguageService
    {
        Task<Solution> ChangeNamespaceAsync(Solution solution, ImmutableArray<DocumentId> documentIds, string declaredNamespace, string targetNamespace, CancellationToken cancellationToken);

        bool TryGetReplacementReferenceSyntax(SyntaxNode reference, ImmutableArray<string> newNamespaceParts, ISyntaxFactsService syntaxFacts, out SyntaxNode old, out SyntaxNode @new);
    }
}
