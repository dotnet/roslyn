// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Looks up Roslyn language information for a given document path or language ID from LSP client.
/// </summary>
internal interface ILanguageInfoProvider : ILspService
{
    /// <summary>
    /// Gets the Roslyn language information for a given document path or language ID from LSP client.
    /// </summary>
    /// <remarks>
    /// It is totally possible to not find language based on the file path (e.g. a newly created file that hasn't been saved to disk).
    /// In that case, we use the language Id that the LSP client gave us.
    /// </remarks>
    bool TryGetLanguageInformation(DocumentUri uri, string? lspLanguageId, [NotNullWhen(true)] out LanguageInformation? languageInformation);
}
