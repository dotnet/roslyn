// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal interface ITypeImportCompletionService : ILanguageService
    {
        /// <summary>
        /// Get approperiate completion items for all the visible top level types from given project. 
        /// This method is intended to be used for getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="GetTopLevelTypesFromPEReference"/>.
        /// </summary>
        Task<ImmutableArray<CompletionItem>> GetTopLevelTypesAsync(
            Project project,
            SyntaxContext syntaxContext,
            bool isInternalsVisible,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get approperiate completion items for all the visible top level types from given PE reference.
        /// </summary>
        ImmutableArray<CompletionItem> GetTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            SyntaxContext syntaxContext,
            bool isInternalsVisible,
            CancellationToken cancellationToken);
    }
}
