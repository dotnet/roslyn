// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface ILspMiscellaneousFilesWorkspaceProvider : ILspService
{
    /// <summary>
    /// Returns the actual workspace that the documents are added to or removed from.
    /// </summary>
    Workspace Workspace { get; }
    /// <summary>
    /// Adds a document to the workspace. Note that the implementation of this method should not depend on anything expensive such as RPC calls.
    /// async is used here to allow taking locks asynchronously and "relatively fast" stuff like that.
    /// </summary>
    Task<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger);
    ValueTask TryRemoveMiscellaneousDocumentAsync(DocumentUri uri, bool removeFromMetadataWorkspace);
}
