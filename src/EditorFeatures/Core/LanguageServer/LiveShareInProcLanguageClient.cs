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

// The C# and VB ILanguageClient should not activate on the host. When LiveShare mirrors the C# ILC to the guest,
// they will not copy the DisableUserExperience attribute, so guests will still use the C# ILC.
[DisableUserExperience(true)]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal sealed class LiveShareInProcLanguageClient(
    CSharpVisualBasicLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, exportProvider)
{
    protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

    /// <summary>
    /// Failures are catastrophic as liveshare guests will not have language features without this server.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.LiveShareLspServer;
}
