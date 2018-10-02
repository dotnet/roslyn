// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal interface IRemoveUnnecessaryImportsService : ILanguageService
    {
        Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken);
        Task<Document> RemoveUnnecessaryImportsAsync(Document fromDocument, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken);

        /// <summary>
        /// Remove imports deemed unnecessaary in current context, i.e. document linked to <paramref name="document"/> is not considered.
        /// </summary>
        Task<Document> RemoveUnnecessaryImportsFromCurrentContextAsync(Document document, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken);
    }
}
