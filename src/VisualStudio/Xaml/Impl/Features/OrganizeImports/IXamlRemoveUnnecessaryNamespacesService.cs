// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports
{
    internal interface IXamlRemoveUnnecessaryNamespacesService
    {
        /// <returns>Returns the rewritten document, or the document passed in if no changes were made. If cancellation
        /// was observed, it returns null.</returns>
        Task<Document> RemoveUnnecessaryNamespacesAsync(Document document, CancellationToken cancellationToken);
    }
}
