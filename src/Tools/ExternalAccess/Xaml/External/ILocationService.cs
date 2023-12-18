// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Represents a service  can be imported via MEF to provide location information.
/// </summary>
internal interface ILocationService
{
    /// <summary>
    /// Gets the locations for the given symbol in the given project.
    /// </summary>
    Task<LSP.Location[]> GetSymbolLocationsAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the definition locations for a text span in a document document.
    /// </summary>
    Task<LSP.Location?> GetLocationAsync(TextDocument document, TextSpan textSpan, CancellationToken cancellationToken);
}
