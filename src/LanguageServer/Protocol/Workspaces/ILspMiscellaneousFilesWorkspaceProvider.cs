﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface ILspMiscellaneousFilesWorkspaceProvider : ILspService
{
    /// <summary>
    /// Returns the actual workspace that the documents are added to or removed from.
    /// </summary>
    Workspace Workspace { get; }
    TextDocument? AddMiscellaneousDocument(Uri uri, SourceText documentText, string languageId, ILspLogger logger);
    void TryRemoveMiscellaneousDocument(Uri uri, bool removeFromMetadataWorkspace);
}
