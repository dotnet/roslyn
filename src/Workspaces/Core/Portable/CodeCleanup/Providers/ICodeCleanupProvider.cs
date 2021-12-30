﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// A code cleaner that requires semantic information to do its job.
    /// </summary>
    internal interface ICodeCleanupProvider
    {
        /// <summary>
        /// Returns the name of this provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// This should apply its code clean up logic to the spans of the document.
        /// </summary>
        Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken = default);

        /// <summary>
        /// This will run all provided code cleaners in an order that is given to the method.
        /// 
        /// This will do cleanups that don't require any semantic information
        /// </summary>
        Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken = default);
    }
}
