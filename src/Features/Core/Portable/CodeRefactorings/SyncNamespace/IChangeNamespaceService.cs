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
        ///    3. No partial type declared in the namespace. Otherwise its multiple declarations will
        ///       end up in different namespace.
        ///
        /// - If <paramref name="container"/> is a compilation unit node:
        ///    1. It must contain no namespace declaration
        ///    2. No partial type declared in the document. Otherwise its multiple declarations will
        ///       end up in different namespace.
        ///       
        /// - Otherwise, an <see cref="System.ArgumentException"/> will be thrown.
        ///   
        /// Returns <see langword="true"/> only when all the requirements above are met.
        /// </summary>
        /// <remarks>
        /// While this service might be used by features that change namespace based on some property of the document
        /// (e.g. Sync namespace refactoring), those logic is implemented by those individual features and isn't part 
        /// of the IChangeNamespaceService service.
        /// </remarks>
        Task<bool> CanChangeNamespaceAsync(Document document, SyntaxNode container, CancellationToken cancellationToken);

        /// <summary>
        /// Change namespace for given <paramref name="container"/> to the name specified by <paramref name="targetNamespace"/>.
        /// Everything declared in the <paramref name="container"/> will be moved to the new namespace. 
        /// Change will only be made if <see cref="CanChangeNamespaceAsync"/> returns <see langword="true"/> and <paramref name="targetNamespace"/>
        /// is a valid name for namespace. Use "" for <paramref name="targetNamespace"/> to specify the global namespace.
        /// 
        /// An <see cref="System.ArgumentException"/> will be thrown if:
        /// 1. <paramref name="container"/> is not a namespace declaration or a compilation unit node.
        /// 2. <paramref name="targetNamespace"/> is null or contains an invalid character.
        /// </summary>
        /// <remarks>
        /// If the declared namespace for <paramref name="container"/> is already identical to <paramref name="targetNamespace"/>, then it will be
        /// a no-op and original solution will be returned.
        /// </remarks>
        Task<Solution> ChangeNamespaceAsync(Document document, SyntaxNode container, string targetNamespace, CancellationToken cancellationToken);
    }
}
