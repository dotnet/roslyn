// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports
{
    internal interface IXamlOrganizeNamespacesService
    {
        /// <returns>Returns the rewritten document, or the document passed in if no changes were made. If cancellation
        /// was observed, it returns null.</returns>
        Task<Document> OrganizeNamespacesAsync(Document document, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);
    }
}
