// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.OrganizeImports
{
    public static partial class OrganizeImportsService
    {
        public static Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken)
            => document.GetLanguageService<IOrganizeImportsService>().OrganizeImportsAsync(document, cancellationToken);
    }
}
