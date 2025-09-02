// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Holds state information from the client about a tracked LSP document.  See <see cref="LspWorkspaceManager"/> 
/// </summary>
/// <param name="SourceText">A snapshot of the text as seen by LSP.</param>
/// <param name="LanguageId">The language id for the text, from <see cref="TextDocumentItem.LanguageId"/> </param>
/// <param name="LspVersion">The client version associated with the text, from <see cref="VersionedTextDocumentIdentifier.Version"/></param>
internal record struct TrackedDocumentInfo(SourceText SourceText, string LanguageId, int LspVersion);
