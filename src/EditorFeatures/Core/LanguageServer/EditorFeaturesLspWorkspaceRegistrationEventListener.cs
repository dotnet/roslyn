// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

/// <summary>
/// Visual Studio's <see cref="LspWorkspaceRegistrationEventListener"/>. VS hosts a single instance of each
/// workspace per process, so it shares all of the LSP-relevant workspace kinds process-wide (matching the set
/// the registration listener tracked before it was split per host).
/// </summary>
[ExportEventListener(
    WellKnownEventListeners.Workspace,
    WorkspaceKind.Host,
    WorkspaceKind.MiscellaneousFiles,
    WorkspaceKind.MetadataAsSource,
    WorkspaceKind.Interactive,
    WorkspaceKind.SemanticSearch), Shared]
[Export(typeof(LspWorkspaceRegistrationEventListener))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorFeaturesLspWorkspaceRegistrationEventListener() : LspWorkspaceRegistrationEventListener;
