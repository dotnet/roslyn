// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.OrganizeImports
{
    internal interface IOrganizeImportsService : ILanguageService
    {
        Task<Document> OrganizeImportsAsync(Document document, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);

        string OrganizeImportsDisplayStringWithAccelerator { get; }

        string SortImportsDisplayStringWithAccelerator { get; }

        string RemoveUnusedImportsDisplayStringWithAccelerator { get; }

        string SortAndRemoveUnusedImportsDisplayStringWithAccelerator { get; }
    }
}
