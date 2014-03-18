// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// internal code cleanup service interface
    /// 
    /// this is not supposed to be used directly. it just provide a way to get the right service from each language
    /// </summary>
    internal interface ICodeCleanerService : ILanguageService
    {
        /// <summary>
        /// returns default code cleaners
        /// </summary>
        IEnumerable<ICodeCleanupProvider> GetDefaultProviders();

        /// <summary>
        /// this will run all provided code cleaners in an order that is given to the method.
        /// </summary>
        Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken);

        /// <summary>
        /// this will run all provided code cleaners in an order that is given to the method.
        /// 
        /// this will do cleanups that doesn't require any semantic information
        /// </summary>
        SyntaxNode Cleanup(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken);
    }
}
