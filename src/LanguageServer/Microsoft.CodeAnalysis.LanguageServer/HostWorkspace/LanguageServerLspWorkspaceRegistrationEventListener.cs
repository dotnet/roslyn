// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// The standalone language server's <see cref="LspWorkspaceRegistrationEventListener"/>. It shares only the
/// process-global <see cref="WorkspaceKind.MetadataAsSource"/> workspace across the (daemon-mode) servers in the
/// process; each server's own Host and miscellaneous-files workspaces are registered directly by
/// <see cref="LanguageServerWorkspaceFactory"/> so they remain isolated to that server.
/// </summary>
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.MetadataAsSource), Shared]
[Export(typeof(LspWorkspaceRegistrationEventListener))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerLspWorkspaceRegistrationEventListener() : LspWorkspaceRegistrationEventListener;
