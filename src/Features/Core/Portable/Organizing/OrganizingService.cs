// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Organizing.Organizers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Organizing;

internal static partial class OrganizingService
{
    /// <summary>
    /// Organize the whole document.
    /// 
    /// Optionally you can provide your own organizers. otherwise, default will be used.
    /// </summary>
    public static Task<Document> OrganizeAsync(Document document, IEnumerable<ISyntaxOrganizer> organizers = null, CancellationToken cancellationToken = default)
    {
        var service = document.GetLanguageService<IOrganizingService>();
        return service.OrganizeAsync(document, organizers, cancellationToken);
    }
}
