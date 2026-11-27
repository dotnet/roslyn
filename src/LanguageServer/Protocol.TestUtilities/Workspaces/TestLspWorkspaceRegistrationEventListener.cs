// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// Test <see cref="LspWorkspaceRegistrationEventListener"/> for the standalone (Features + Protocol) LSP test
/// composition, which contains no production concrete listener (those live in the language server and
/// EditorFeatures layers). Tracks all LSP-relevant workspace kinds so a test's single workspace is visible to
/// the test server's <see cref="LspWorkspaceRegistrationService"/>. Added to the composition via
/// <see cref="LspTestCompositions"/>.
/// </summary>
[ExportEventListener(
    WellKnownEventListeners.Workspace,
    WorkspaceKind.Host,
    WorkspaceKind.MiscellaneousFiles,
    WorkspaceKind.MetadataAsSource,
    WorkspaceKind.Interactive,
    WorkspaceKind.SemanticSearch), Shared, PartNotDiscoverable]
[Export(typeof(LspWorkspaceRegistrationEventListener))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TestLspWorkspaceRegistrationEventListener() : LspWorkspaceRegistrationEventListener;
