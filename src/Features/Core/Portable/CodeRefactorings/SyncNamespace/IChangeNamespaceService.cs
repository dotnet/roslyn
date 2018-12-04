// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ChangeNamespace
{
    internal interface IChangeNamespaceService : ILanguageService
    {
        /// <summary>
        /// Determine whether we can change the namespace for given <paramref name="container"/> in the document.
        /// Linked documents are not supported, except for a regular document in a multi-targeting project, 
        /// where the container node must be consistent among all linked documents.
        /// Here's the additional requirements on <paramref name="container"/> to use this service:
        /// 
        /// - If <paramref name="container"/> is a namespace declaration node:
        ///    1. Doesn't contain or is nested in other namespace declarations
        ///    2. The name of the namespace is valid (i.e. no errors)
        ///    3. No partial type declared in the namespace. Otherwise its multiple declaration will
        ///       end up in different namespace.
        ///
        /// - If <paramref name="container"/> is a compilation unit node:
        ///    1. It must contain no namespace declaration
        ///    2. No partial type declared in the document. Otherwise its multiple declaration will
        ///       end up in different namespace.
        ///   
        /// Returns <see langword="true"/> only when all the requirements above are met.
        /// </summary>
        Task<bool> CanChangeNamespaceAsync(Document document, SyntaxNode container, CancellationToken cancellationToken);

        /// <summary>
        /// Change namespace for given <paramref name="container"/> to the name specified by <paramref name="targetNamespace"/>.
        /// Everything declared in the <paramref name="container"/> will be moved to the new namespace. 
        /// Change will only be made if <see cref="CanChangeNamespaceAsync"/> returns <see langword="true"/> and <paramref name="targetNamespace"/>
        /// is a valid name for namespace. Use "" for <paramref name="targetNamespace"/> to specify the global namespace.
        /// </summary>
        Task<Solution> ChangeNamespaceAsync(Document document, SyntaxNode container, string targetNamespace, CancellationToken cancellationToken);
    }
}
