// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// code cleaner that requires semantic information to do its job
    /// </summary>
    internal interface ICodeCleanupProvider
    {
        /// <summary>
        /// return name of this provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// this should apply its code clean up logic to the spans of the document
        /// </summary>
        Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// this will run all provided code cleaners in an order that is given to the method.
        /// 
        /// this will do cleanups that doesn't require any semantic information
        /// </summary>
        SyntaxNode Cleanup(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken = default(CancellationToken));
    }
}