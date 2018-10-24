// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal interface ISyncNamespaceService : ILanguageService
    {
        Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Try to get a new node to replace given node, which is a reference to a top-level type declared inside the 
        /// namespce to be changed. If this reference is the right side of a qualified name, the new node returned would
        /// be the entire qualified name. Depends on whether <paramref name="newNamespaceParts"/> is provided, the name 
        /// in the new node might be qualified with this new namespace instead.
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be changed, which is calculated 
        /// based on results from `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="newNamespaceParts">If specified, the namespace of original reference will be replaced with given 
        /// namespace in the replacement node.</param>
        /// <param name="old">The node to be replaced. This might be an ancestor of original </param>
        /// <param name="new">The replacement node.</param>
        bool TryGetReplacementReferenceSyntax(
            SyntaxNode reference, 
            ImmutableArray<string> newNamespaceParts, 
            ISyntaxFactsService syntaxFacts, 
            out SyntaxNode old,
            out SyntaxNode @new);
    }
}
