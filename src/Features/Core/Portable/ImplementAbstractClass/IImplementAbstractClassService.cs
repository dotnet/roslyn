// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal interface IImplementAbstractClassService : ILanguageService
    {
        Task<bool> CanImplementAbstractClassAsync(Document document, SyntaxNode classNode, CancellationToken cancellationToken);
        Task<Document> ImplementAbstractClassAsync(Document document, SyntaxNode classNode, CancellationToken cancellationToken);
    }
}
