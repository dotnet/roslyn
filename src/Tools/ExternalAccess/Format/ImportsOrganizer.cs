// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format
{
    internal static class ImportsOrganizer
    {
        public static Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken)
            => OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken);
    }
}
