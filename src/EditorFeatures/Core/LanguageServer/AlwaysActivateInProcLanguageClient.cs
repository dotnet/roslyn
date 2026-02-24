// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

/// <summary>
/// Language client responsible for handling C# / VB / F# LSP requests in any scenario (both local and codespaces).
/// This powers "LSP only" features (e.g. cntrl+Q code search) that do not use traditional editor APIs.
/// It is always activated whenever roslyn is activated.
/// </summary>
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[ContentType(ContentTypeNames.FSharpContentType)]
[Export(typeof(ILanguageClient))]
[Export(typeof(AlwaysActivateInProcLanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal sealed class AlwaysActivateInProcLanguageClient(
    CSharpVisualBasicLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, exportProvider)
{
    protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.AlwaysActiveVSLspServer;
}
