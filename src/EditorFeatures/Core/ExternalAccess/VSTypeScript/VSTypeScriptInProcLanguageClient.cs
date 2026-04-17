// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

/// <summary>
/// Language client to handle TS LSP requests.
/// Allows us to move features to LSP without being blocked by TS as well
/// as ensures that TS LSP features use correct solution snapshots.
/// </summary>
[ContentType(ContentTypeNames.TypeScriptContentTypeName)]
[ContentType(ContentTypeNames.JavaScriptContentTypeName)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal sealed class VSTypeScriptInProcLanguageClient(
    VSTypeScriptLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, exportProvider)
{
    protected override ImmutableArray<string> SupportedLanguages => [InternalLanguageNames.TypeScript];

    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.RoslynTypeScriptLspServer;
}
