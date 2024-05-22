// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Features.Workspaces;

namespace Microsoft.CodeAnalysis.LanguageServer
{
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
        /// <exception cref="InvalidOperationException">Thrown when the language information cannot be determined.</exception>
        LanguageInformation GetLanguageInformation(string documentPath, string? lspLanguageId);
    }
}
