// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ISyntaxTreeStorageService : IWorkspaceService
    {
        void EnqueueStore(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken);
        Task EnqueueStoreAsync(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken);
        Task StoreAsync(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken);
        void Store(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken);
        bool CanRetrieve(SyntaxTree tree);
        Task<SyntaxNode> RetrieveAsync(SyntaxTree tree, ISyntaxTreeFactoryService service, CancellationToken cancellationToken);
        SyntaxNode Retrieve(SyntaxTree tree, ISyntaxTreeFactoryService service, CancellationToken cancellationToken);
    }
}
