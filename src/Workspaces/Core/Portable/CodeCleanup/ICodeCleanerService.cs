// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Internal code cleanup service interface.
    /// 
    /// This is not supposed to be used directly. It just provides a way to get the right service from each language.
    /// </summary>
    internal interface ICodeCleanerService : ILanguageService
    {
        /// <summary>
        /// Returns the default code cleaners.
        /// </summary>
        IEnumerable<ICodeCleanupProvider> GetDefaultProviders();

        /// <summary>
        /// This will run all provided code cleaners in an order that is given to the method.
        /// </summary>
        Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken);

        /// <summary>
        /// This will run all provided code cleaners in an order that is given to the method.
        /// 
        /// This will do cleanups that don't require any semantic information.
        /// </summary>
        Task<SyntaxNode> CleanupAsync(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken);
    }
}
