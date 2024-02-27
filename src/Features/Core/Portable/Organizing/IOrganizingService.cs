// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.Organizing;

/// <summary>
/// internal interface used to use language specific service from common service layer
/// </summary>
internal interface IOrganizingService : ILanguageService
{
    /// <summary>
    /// return default organizers
    /// </summary>
    IEnumerable<ISyntaxOrganizer> GetDefaultOrganizers();

    /// <summary>
    /// Organize document
    /// </summary>
    Task<Document> OrganizeAsync(Document document, IEnumerable<ISyntaxOrganizer> organizers, CancellationToken cancellationToken);
}
