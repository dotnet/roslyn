// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    internal sealed class CodeLensReferencesServiceAccessor : ICodeLensReferencesServiceAccessor
    {
        private readonly ICodeLensReferencesService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public CodeLensReferencesServiceAccessor(ICodeLensReferencesService implementation)
        {
            _implementation = implementation;
        }

        public async Task<IEnumerable<CodeLensReferenceLocationDescriptorWrapper?>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var result = await _implementation.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwaitRunInline();
            return result.Select(descriptor => descriptor is object ? new CodeLensReferenceLocationDescriptorWrapper(descriptor) : default(CodeLensReferenceLocationDescriptorWrapper?));
        }

        public async Task<IEnumerable<CodeLensReferenceMethodDescriptorWrapper?>> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var result = await _implementation.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwaitRunInline();
            return result.Select(descriptor => descriptor is object ? new CodeLensReferenceMethodDescriptorWrapper(descriptor) : default(CodeLensReferenceMethodDescriptorWrapper?));
        }

        public Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
            => _implementation.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken);

        public async Task<CodeLensReferenceCountWrapper?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults, CancellationToken cancellationToken)
        {
            var result = await _implementation.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwaitRunInline();
            if (result is null)
            {
                return null;
            }

            return new CodeLensReferenceCountWrapper(result);
        }
    }
}
